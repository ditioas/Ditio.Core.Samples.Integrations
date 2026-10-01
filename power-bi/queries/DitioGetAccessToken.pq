// Returns AccessToken if set, otherwise requests a client-credentials token with the reportingapiv1 scope.
() as text =>
let
    UseProvidedToken = AccessToken <> null and AccessToken <> "",
    Token =
        if UseProvidedToken then
            AccessToken
        else if ClientId = null or ClientId = "" or ClientSecret = null or ClientSecret = "" then
            error Error.Record("Ditio credentials missing", "Set the ClientId and ClientSecret parameters, or AccessToken.")
        else
            let
                Response = Web.Contents(
                    IdentityUrl,
                    [
                        RelativePath = "connect/token",
                        Headers = [#"Content-Type" = "application/x-www-form-urlencoded", Accept = "application/json"],
                        Content = Text.ToBinary(
                            Uri.BuildQueryString(
                                [
                                    grant_type = "client_credentials",
                                    client_id = ClientId,
                                    client_secret = ClientSecret,
                                    scope = "reportingapiv1"
                                ]
                            )
                        ),
                        ManualStatusHandling = {400, 401, 403}
                    ]
                ),
                Body = Binary.Buffer(Response),
                Status = Value.Metadata(Response)[Response.Status]
            in
                if Status >= 400 then
                    error Error.Record("Ditio token request failed", "HTTP " & Text.From(Status) & " from " & IdentityUrl, Text.FromBinary(Body))
                else
                    Json.Document(Body)[access_token]
in
    Token
