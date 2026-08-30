# PickleCrew

## Local development

Configure the WorkOS AuthKit values in the AppHost's .NET user secrets. Get the client ID and API
key from the WorkOS dashboard, then run these commands from the repository root:

```shell
dotnet user-secrets set "Parameters:workos-client-id" "client_your_client_id" --project src/PickleCrew.AppHost/PickleCrew.AppHost.csproj
dotnet user-secrets set "Parameters:workos-api-key" "sk_test_your_api_key" --project src/PickleCrew.AppHost/PickleCrew.AppHost.csproj
openssl rand -base64 32
dotnet user-secrets set "Parameters:workos-cookie-password" "paste_the_generated_value" --project src/PickleCrew.AppHost/PickleCrew.AppHost.csproj
```

The cookie password must be at least 32 characters and must not reuse another secret. Verify the
values were saved. This command displays them in the terminal, so do not share its output:

```shell
dotnet user-secrets list --project src/PickleCrew.AppHost/PickleCrew.AppHost.csproj
```

Aspire reads these values as parameters. It passes the client ID and API key to the API as
`WorkOS__ClientId` and `WorkOS__ApiKey`; the API uses them to validate access tokens and provision
crews. It passes the AuthKit variables to the web resource as `WORKOS_CLIENT_ID`, `WORKOS_API_KEY`,
and `WORKOS_COOKIE_PASSWORD`. It also supplies the API endpoint as `API_BASE_URL`, assigns the web
resource a dynamic port, and injects the matching `WORKOS_REDIRECT_URI` at startup. Local WorkOS
configuration must not be stored in `.env` files. Remove an existing `infra/web/.env.local` after
migrating its values to user secrets.

Add `http://localhost:*/api/auth/callback` to the WorkOS application's allowed redirect URIs and
`http://localhost:*/` to its sign-out redirect URIs.

Start the app:

```shell
aspire start
```

Open the web resource URL shown in the Aspire dashboard.

Stop the app:

```shell
aspire stop
```

## Rebuild statistics

Set `REBUILD_STATISTICS=true` to rebuild statistics from recorded games during API startup.

## Cloud deployment

The deployment environments map to WorkOS as follows:

| GitHub and Cloudflare environment | WorkOS environment | Redirect URI | Sign-in endpoint | Sign-out URI |
| --- | --- | --- | --- | --- |
| `development` | Staging | `https://dev.picklecrew.app/api/auth/callback` | `https://dev.picklecrew.app/api/auth/sign-in` | `https://dev.picklecrew.app/` |
| `production` | Production | `https://picklecrew.app/api/auth/callback` | `https://picklecrew.app/api/auth/sign-in` | `https://picklecrew.app/` |

Configure these values in the [WorkOS Redirects settings](https://dashboard.workos.com/redirects).
The sign-in endpoints are required for WorkOS-initiated flows such as impersonation.

Configure these values on both the `development` and `production` GitHub environments, using
credentials from the corresponding WorkOS environment:

| Name | GitHub type | Value |
| --- | --- | --- |
| `WORKOS_CLIENT_ID` | Environment variable | WorkOS client ID beginning with `client_` |
| `WORKOS_API_KEY` | Environment secret | WorkOS API key beginning with `sk_` |
| `WORKOS_COOKIE_PASSWORD` | Environment secret | Independently generated value containing at least 32 characters |

Generate a different cookie password for each environment:

```shell
openssl rand -base64 32
```

The API base URL and redirect URIs are non-secret Worker variables in
`infra/web/wrangler.jsonc`. During deployment, the pipeline stages `WORKOS_CLIENT_ID` and
`WORKOS_API_KEY` as Fly secrets for the API. It also validates the WorkOS settings and uploads the
client ID, API key, and cookie password as Cloudflare Worker secrets in the same operation as the
Worker code. Pushes to `main` deploy development; stable `vMAJOR.MINOR.PATCH` tags deploy
production.

## Production releases

Use the project-local pi command to create a GitHub release from `main` with generated release
notes:

```text
/release major
/release minor
/release patch
```

The command calculates the next stable semantic version from the repository's remote tags and asks
for confirmation before publishing. It requires an authenticated GitHub CLI.

## Generate the API client

Build the API OpenAPI document, then generate the typed TypeScript client:

```shell
dotnet build src/PickleCrew.Api/PickleCrew.Api.csproj
pnpm --dir src/PickleCrew.Web api:generate
```

Commit changes to `src/PickleCrew.Web/src/lib/api`, including `openapi.json` and the `generated` directory. These files are generated artifacts and must not be edited manually.
