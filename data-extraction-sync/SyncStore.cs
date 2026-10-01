using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Ditio.Samples.Examples.DataExtractionSync;

/// <summary>
/// The local copy: one table per endpoint holding each record's raw JSON, a <c>sync_state</c> table
/// holding each endpoint's watermark, and one view per endpoint for Power BI.
///
/// Everything here goes through <see cref="DbConnection"/>, so moving to SQL Server or PostgreSQL
/// means changing the connection (<see cref="Open"/>) and the few statements marked
/// <b>dialect</b>: the upserts (<c>ON CONFLICT</c> works on PostgreSQL; SQL Server needs
/// <c>MERGE</c>) and the views (<c>json_extract</c> is <c>JSON_VALUE</c> on SQL Server and
/// <c>json-&gt;&gt;</c> on PostgreSQL).
/// </summary>
internal sealed class SyncStore : IAsyncDisposable
{
    private readonly DbConnection _connection;

    private SyncStore(DbConnection connection) => _connection = connection;

    public static async Task<SyncStore> Open(string databasePath)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        await connection.OpenAsync();

        // WAL lets Power BI (or anything else) read the file while a sync is writing to it.
        await Execute(connection, null, "PRAGMA journal_mode = WAL;");

        await Execute(connection, null, """
            CREATE TABLE IF NOT EXISTS sync_state (
                endpoint                TEXT PRIMARY KEY,
                last_sync_started_utc   TEXT NOT NULL,
                last_sync_completed_utc TEXT NOT NULL,
                last_full_sync_utc      TEXT
            );
            """);

        return new SyncStore(connection);
    }

    public async Task EnsureTable(SyncEndpoint endpoint)
    {
        await Execute(_connection, null, $"""
            CREATE TABLE IF NOT EXISTS {endpoint.Table} (
                id       TEXT PRIMARY KEY,
                modified TEXT,
                deleted  INTEGER NOT NULL DEFAULT 0,
                json     TEXT NOT NULL
            );
            """);

        // Recreated on every run so a changed column list in the code takes effect. Deleted rows are
        // kept in the table (deleted = 1) but never reach the view. Dialect: json_extract.
        var columns = string.Join(",\n    ", endpoint.ViewColumns.Select(column => $"json_extract(json, '$.{column}') AS \"{column}\""));
        await Execute(_connection, null, $"DROP VIEW IF EXISTS {endpoint.View};");
        await Execute(_connection, null, $"""
            CREATE VIEW {endpoint.View} AS
            SELECT
                id,
                {columns}
            FROM {endpoint.Table}
            WHERE deleted = 0;
            """);
    }

    /// <summary>The previous successful run of this endpoint, or null when it has never completed.</summary>
    public async Task<(DateTime LastSyncStartedUtc, DateTime? LastFullSyncUtc)?> GetState(SyncEndpoint endpoint)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT last_sync_started_utc, last_full_sync_utc FROM sync_state WHERE endpoint = @endpoint;";
        AddParameter(command, "@endpoint", endpoint.Path);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        DateTime? lastFull = reader.IsDBNull(1) ? null : ParseUtc(reader.GetString(1));
        return (ParseUtc(reader.GetString(0)), lastFull);
    }

    public Task<DbTransaction> BeginTransaction() => _connection.BeginTransactionAsync().AsTask();

    /// <summary>A full sync is the complete truth, so it replaces whatever the table held before.</summary>
    public Task Clear(DbTransaction transaction, SyncEndpoint endpoint) =>
        Execute(_connection, transaction, $"DELETE FROM {endpoint.Table};");

    /// <summary>Inserts or replaces a live record. Dialect: ON CONFLICT.</summary>
    public Task Upsert(DbTransaction transaction, SyncEndpoint endpoint, string id, string? modified, string json) =>
        Execute(_connection, transaction, $"""
            INSERT INTO {endpoint.Table} (id, modified, deleted, json) VALUES (@id, @modified, 0, @json)
            ON CONFLICT (id) DO UPDATE SET modified = excluded.modified, deleted = 0, json = excluded.json;
            """,
            ("@id", id), ("@modified", modified), ("@json", json));

    /// <summary>
    /// Marks a record deleted. A deletion arrives as a tombstone — the id, <c>isDeleted</c> and
    /// <c>deletedDateTime</c>, with the other fields empty — so the row keeps the last version we
    /// saw and only flips the flag. A tombstone for a record we never had is stored as-is.
    /// Dialect: ON CONFLICT.
    /// </summary>
    public Task MarkDeleted(DbTransaction transaction, SyncEndpoint endpoint, string id, string? modified, string json) =>
        Execute(_connection, transaction, $"""
            INSERT INTO {endpoint.Table} (id, modified, deleted, json) VALUES (@id, @modified, 1, @json)
            ON CONFLICT (id) DO UPDATE SET modified = excluded.modified, deleted = 1;
            """,
            ("@id", id), ("@modified", modified), ("@json", json));

    /// <summary>Moves the watermark. Called inside the endpoint's transaction, as its last write.</summary>
    public Task SaveState(DbTransaction transaction, SyncEndpoint endpoint, DateTime startedUtc, bool wasFullSync) =>
        Execute(_connection, transaction, """
            INSERT INTO sync_state (endpoint, last_sync_started_utc, last_sync_completed_utc, last_full_sync_utc)
            VALUES (@endpoint, @started, @completed, @full)
            ON CONFLICT (endpoint) DO UPDATE SET
                last_sync_started_utc   = excluded.last_sync_started_utc,
                last_sync_completed_utc = excluded.last_sync_completed_utc,
                last_full_sync_utc      = COALESCE(excluded.last_full_sync_utc, sync_state.last_full_sync_utc);
            """,
            ("@endpoint", endpoint.Path),
            ("@started", FormatUtc(startedUtc)),
            ("@completed", FormatUtc(DateTime.UtcNow)),
            ("@full", wasFullSync ? FormatUtc(startedUtc) : null));

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    public static string FormatUtc(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

    private static async Task Execute(DbConnection connection, DbTransaction? transaction, string sql, params (string Name, string? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            AddParameter(command, name, value);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameter(DbCommand command, string name, string? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = (object?)value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
