# 01 — Authentication

OAuth2 **client credentials**. Create an API client in Ditio Web → **Company Setup → Integration** (Administrator access); you get a `client_id` + `client_secret` (the secret is shown once).

## Set your environment

```bash
# Production
IDENTITY=https://identity.ditio.app
BASE_URL=https://integration.ditio.no

# Test — comment out the two production lines above and use these instead
# IDENTITY=https://identity.ditio.dev
# BASE_URL=https://core-api.ditio.dev/core
```

## Get a token

```bash
TOKEN=$(curl -s -X POST $IDENTITY/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=YOUR_CLIENT_ID" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=ditioapiv3" | jq -r '.access_token')
```

Send `-H "Authorization: Bearer $TOKEN"` on every call.

## Token lifetime & reuse

Tokens a client fetches itself are **short-lived** (~30 minutes by default, configurable per client up to 24h). **Cache and reuse** the token; fetch a new one shortly before it expires (read `expires_in`). Don't request a fresh token per call.

## Troubleshooting a 401

If a Core API call returns `401`, first check whether the Bearer token is missing, expired, or invalid. Supply a valid token if needed; refresh an expired token and retry the call once. A still-valid token can also receive `401` when its API client has been deleted, disabled, reached its configured `ValidUntil`, has no allowed scopes, or has an emulation user that is missing or disabled. Fetching another token does not repair those client settings. Ask a company administrator to restore or replace the client, renew its validity, grant the required scopes, or fix its emulation user. Avoid repeated token refreshes for the same `401`.

## Scopes

| Scope | API |
|-------|-----|
| `ditioapiv3` | Integration + core API (`api/v4/integration/*`, `api/v5/integration/*`, `api/file`) |
| `reportingapiv1` | Reporting / data-extraction API (`v1/*`) |

Request multiple in one token by space-separating: `scope=ditioapiv3 reportingapiv1`.

**C#:** [`AuthenticationExample.cs`](AuthenticationExample.cs) — `DitioTokenProvider` caches a token per scope and refreshes near expiry.
