# Infrastructure

PickleCrew uses TanStack Start on Cloudflare Workers for the web application, ASP.NET Core 10 on Fly.io for the API, Docker for the API image, and PlanetScale Postgres for deployed data. Cloudflare also provides DNS, TLS proxying, WAF, and DDoS protection. Aspire remains local-development-only.

## Architecture

```text
Client -> Cloudflare Web Worker
Client -> Cloudflare Proxy -> Fly.io:8080 -> PlanetScale Postgres
```

| Environment | Web Worker | Web hostname | API app | API hostname |
|---|---|---|---|---|
| `development` | `picklecrew-web-development` | `dev.picklecrew.app` | `picklecrew-api-development` | `api.dev.picklecrew.app` |
| `production` | `picklecrew-web-production` | `picklecrew.app` | `picklecrew-api-production` | `api.picklecrew.app` |

| API environment | Fly hostname | Region | Minimum running Machines |
|---|---|---|---|
| `development` | `picklecrew-api-development.fly.dev` | `syd` | 0 |
| `production` | `picklecrew-api-production.fly.dev` | `syd` | 0 |

Both Fly API apps use one shared CPU, 512 MB memory, port `8080`, automatic Machine start and stop, and `/alive` routing health checks. Fly's default remote builder and high-availability behavior are used. Fly may create more than one Machine during initial deployment or rollout, which can affect billing.

## PlanetScale access

Use a separate PlanetScale database and database-specific credentials for each deployment environment. Development and production do not share a database or rely on branches of the same database for isolation. In each database, use the `picklecrew_app` application role with `pg_read_all_data` and `pg_write_all_data`. Do not grant DDL permissions and do not use the default `postgres` role for the API.

Runtime application traffic uses PlanetScale PgBouncer on port `6432` with an Npgsql connection string:

```text
Host=<host>;Port=6432;Database=<database>;Username=<role>.<branch-id>;Password=<password>;SSL Mode=VerifyFull
```

The `<branch-id>` suffix remains part of PlanetScale's generated username format; it is not the development/production isolation boundary. Do not add `sslrootcert=system`; it is a libpq parameter rather than an Npgsql certificate path. Verify each environment's credential independently with `SELECT 1` before deployment.

Schema migrations use a separate, stable `picklecrew_migrator` role in each database. CI connects directly to PlanetScale on port `5432` with that environment's `psql`-compatible libpq URL. Migration credentials are available only to the migration step and are never stored in Fly.

## Health endpoints

- `/alive` is process-only liveness. It does not connect to the database and is the Fly routing health check.
- `/health` is dependency readiness. It runs all registered health checks, including Npgsql connectivity, and is used by deployment smoke tests.

Health responses do not expose connection strings or exception details. Database readiness checks have a five-second request timeout and successful responses are cached in the API process for ten seconds. `/alive` is not cached.

## One-time Fly.io setup

1. Create or sign in to Fly.io, enable billing, select an organization, and record its slug.
2. Reserve the empty applications without deploying Machines:

   ```bash
   fly apps create picklecrew-api-development --org <organization-slug>
   fly apps create picklecrew-api-production --org <organization-slug>
   ```

3. Create a separate app-scoped deployment token for each application. Do not use `fly auth token`:

   ```bash
   fly tokens create deploy \
     --app picklecrew-api-development \
     --name "picklecrew-development-github" \
     --expiry <duration>

   fly tokens create deploy \
     --app picklecrew-api-production \
     --name "picklecrew-production-github" \
     --expiry <duration>
   ```

4. Record each expiration date and create a rotation reminder before expiry.
5. Retain the `development` and `production` GitHub Environments. Store the corresponding app and database credentials as environment secrets:

   ```text
   FLY_API_TOKEN
   DATABASE_CONNECTION_STRING
   MIGRATIONS_DATABASE_URL
   ```

   No Fly-related GitHub variables are required. `DATABASE_CONNECTION_STRING` must use the environment's non-DDL application role, PgBouncer port `6432`, Npgsql syntax, and `SSL Mode=VerifyFull`. `MIGRATIONS_DATABASE_URL` must use the environment's migrator role, direct port `5432`, and `psql`-compatible URL syntax. Development and production database credentials must differ. Keep production approval rules enabled.
6. Configure `CLOUDFLARE_API_TOKEN` as a repository or environment secret. Configure `CLOUDFLARE_ACCOUNT_ID` as a repository or environment variable; a secret with the same name is also supported. Create the token from the **Edit Cloudflare Workers** template and restrict it to the PickleCrew account and `picklecrew.app` zone. Environment-scoped values can be used if separate Cloudflare tokens are required.

If an app name changes, update both files in `infra/api/`, the static app hostnames and URLs in `.github/workflows/ci-cd.yml`, and this document.

## One-time Cloudflare and certificate setup

Attach each public hostname to its Fly app:

```bash
fly certs add api.dev.picklecrew.app \
  --app picklecrew-api-development
fly certs setup api.dev.picklecrew.app \
  --app picklecrew-api-development

fly certs add api.picklecrew.app \
  --app picklecrew-api-production
fly certs setup api.picklecrew.app \
  --app picklecrew-api-production
```

Use the exact DNS values returned by Fly. In the `picklecrew.app` Cloudflare zone:

1. Confirm that no stale Worker Custom Domain, Worker route, or DNS record owns either API hostname.
2. Add each Fly-provided `_fly-ownership` TXT record.
3. Create each API CNAME with the Fly-provided target.
4. Enable Cloudflare proxying on each CNAME (orange cloud).
5. Set zone SSL/TLS mode to **Full (strict)**.
6. Optionally enable Always Use HTTPS.

Check issuance and DNS status with:

```bash
fly certs check api.dev.picklecrew.app \
  --app picklecrew-api-development
fly certs check api.picklecrew.app \
  --app picklecrew-api-production
```

Certificate issuance can remain pending until the first automated deployment creates application Machines. If issuance fails, compare the current TXT and CNAME records with `fly certs setup`, remove stale Worker routes or conflicting records, verify that the CNAME is proxied and Full (strict) is enabled, then run `fly certs check` again. Retry the failed GitHub Actions deployment after Fly and Cloudflare finish provisioning.

## Web Worker setup

`infra/web/wrangler.jsonc` defines isolated `development` and `production` Workers. Wrangler provisions these Custom Domains and their certificates:

- `development`: `dev.picklecrew.app`
- `production`: `picklecrew.app`

`workers.dev` and preview URLs are disabled. Before the first deployment, remove any DNS record, Worker route, or Worker Custom Domain that already owns either web hostname. The API hostnames remain proxied to Fly and must not be changed.

The Cloudflare Vite plugin selects and flattens the Wrangler environment at build time through `CLOUDFLARE_ENV`. Deploy the generated Worker without a Wrangler `--env` argument. Automated deployments set this correctly; do not deploy the web application locally.

## Automated deployments

The pipeline is `.github/workflows/ci-cd.yml`:

| Git event | Result |
|---|---|
| Pull request | CI only; no deployment |
| Push or merge to `main` | Deploy development API and Web Worker |
| Stable SemVer tag such as `v1.0.0` | Deploy production API and Web Worker after GitHub Environment approval |

CI restores tools and dependencies, builds and tests Release, checks for pending EF Core model changes, generates and uploads an idempotent migration artifact, validates the API Docker build, checks generated Worker types, and builds and dry-runs both Web Worker environments.

The API and Web Worker deployment jobs use static environment selection from the Git ref and run independently after CI. Both validate production tags. The API job installs PostgreSQL tools, downloads and validates the migration artifact, and applies that artifact with `psql` before making any Fly change. A migration failure prevents the API deployment.

After migrations, CI installs the official Fly CLI, stages `DATABASE_CONNECTION_STRING` as the Fly secret `ConnectionStrings__picklecrew`, and deploys from the repository root. The commands used by CI are:

```bash
flyctl secrets set \
  --stage \
  --config "$FLY_CONFIG" \
  "ConnectionStrings__picklecrew=${DATABASE_CONNECTION_STRING}"

flyctl deploy . --config "$FLY_CONFIG"
```

Fly's default remote builder, deployment strategy, and high-availability behavior remain enabled. CI then uses bounded retries to test `/alive` and `/health` first through the app's `fly.dev` hostname and then through the Cloudflare-proxied public hostname. Secret staging, deployment, Fly health checks, either direct smoke test, or either proxied smoke test failing causes the workflow to fail.

The Web Worker job sets `CLOUDFLARE_ENV`, rebuilds the application, and runs `wrangler deploy` with the environment's `CLOUDFLARE_ACCOUNT_ID` and `CLOUDFLARE_API_TOKEN`. It then smoke-tests the selected Custom Domain. There is no local or manual API or Web deployment procedure. The first deployment and all later deployments run through GitHub Actions.

## Database migrations

EF Core migrations are generated in CI as idempotent SQL and retained as a workflow artifact for seven days. Deployments apply the exact artifact from their CI job with `psql` and `ON_ERROR_STOP` before staging the Fly runtime secret and deploying. Re-running a deployment is safe because the script checks `__EFMigrationsHistory` before applying each completed migration.

Deployed application instances never run migrations during startup. Local-development database initialization remains unchanged. Runtime traffic uses PgBouncer on port `6432` with the non-DDL `picklecrew_app` role, while migrations connect directly on port `5432` with `picklecrew_migrator`.

Application rollback does not roll back database changes. Schema changes must use expand-and-contract compatibility so the current and next application versions can both operate during rollout. Destructive changes may run only after deployed code no longer reads or writes the affected objects. Recover from migration problems with a forward fix or the established PlanetScale backup and branch restoration process.

## Operations

Use Fly CLI inspection commands for the appropriate app:

```bash
fly status --app picklecrew-api-development
fly logs --app picklecrew-api-development
fly checks list --app picklecrew-api-development
fly secrets list --app picklecrew-api-development
```

Substitute `picklecrew-api-production` for production. `fly secrets list` shows secret names and digests, not plaintext values. Use the GitHub Actions run as the deployment audit trail and inspect Fly logs and checks when health verification fails.

Retry a transient failure by re-running the failed GitHub Actions jobs after correcting the underlying issue. Do not deploy locally. Rotate an expiring token by creating a replacement app-scoped deploy token, updating only that environment's `FLY_API_TOKEN`, confirming the normal pipeline succeeds, and revoking the old token.

Rollback is automated: revert the application commit and trigger the normal pipeline. A push to `main` rolls development back; create a new stable SemVer tag from the reverted code to roll production back after approval. Use a forward database fix when a migration is not backward compatible.

## Removed infrastructure

The API no longer uses a Cloudflare Worker, Durable Object, Cloudflare Container, or Worker health rate limiter. Cloudflare Workers and Wrangler are used only for the TanStack Start web application. Cloudflare remains the API DNS, TLS, WAF, DDoS, and HTTP proxy layer.
