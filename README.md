# PickleCrew

## Local development

Create `infra/web/.env.local` with the WorkOS AuthKit configuration before starting the app:

```dotenv
WORKOS_CLIENT_ID=client_your_client_id
WORKOS_API_KEY=sk_test_your_api_key
WORKOS_COOKIE_PASSWORD=replace_with_at_least_32_random_characters
```

Get the client ID and API key from the WorkOS dashboard. Add
`http://localhost:*/api/auth/callback` to the application's allowed redirect URIs and
`http://localhost:*/` to its sign-out redirect URIs. The cookie password must be at least 32
characters. Generate one locally rather than reusing another secret.

Aspire assigns the web resource a dynamic port and injects the matching `WORKOS_REDIRECT_URI` at
startup. The environment file is located under `infra/web` because the Cloudflare Vite plugin loads
local Worker variables relative to `infra/web/wrangler.jsonc`. It is ignored by Git and must not be
committed.

Start the app:

```shell
aspire start
```

Open the web resource URL shown in the Aspire dashboard.

Stop the app:

```shell
aspire stop
```

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

The redirect URIs are non-secret Worker variables in `infra/web/wrangler.jsonc`. During deployment,
the pipeline validates the WorkOS settings and uploads the other values as Cloudflare Worker
secrets in the same operation as the Worker code. Pushes to `main` deploy development; stable
`vMAJOR.MINOR.PATCH` tags deploy production.

## Generate the API client

Build the API OpenAPI document, then generate the typed TypeScript client:

```shell
dotnet build src/PickleCrew.Api/PickleCrew.Api.csproj
pnpm --dir src/PickleCrew.Web api:generate
```

Commit changes to `src/PickleCrew.Web/src/lib/api`, including `openapi.json` and the `generated` directory. These files are generated artifacts and must not be edited manually.
