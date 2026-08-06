# Infrastructure

PickleCrew uses ASP.NET Core 10 for the API, Docker for its image, a TypeScript Cloudflare Worker to manage a Cloudflare Container, and PlanetScale Postgres for deployed data. Aspire remains local-development-only.

## Architecture

```text
Client -> Cloudflare Custom Domain -> Worker -> Container:8080 -> PlanetScale Postgres
```

| Environment | Worker | Domain | Database isolation |
|---|---|---|---|
| `development` | `picklecrew-api-development` | `api.dev.picklecrew.app` | Development PlanetScale database and credentials |
| `production` | `picklecrew-api-production` | `api.picklecrew.app` | Production PlanetScale database and credentials |

Each environment has one named `basic` container instance. Its Durable Object uses an Oceania location hint, and container placement is constrained to Oceania with APAC fallback because Cloudflare does not allow the limited-capacity Oceania region as the only constraint. Durable Object placement is fixed after first use, so changing the intended region requires a new instance name. The Worker passes its `DATABASE_CONNECTION_STRING` secret to the container as `ConnectionStrings__picklecrew`, which ASP.NET Core reads as `ConnectionStrings:picklecrew`.

## PlanetScale access

Use a separate PlanetScale database and database-specific credentials for each deployment environment. Development and production do not share a database or rely on branches of the same database for isolation. In each database, use the `picklecrew_app` application role with `pg_read_all_data` and `pg_write_all_data`. Do not grant DDL permissions and do not use the default `postgres` role for the API.

Runtime application traffic uses PlanetScale PgBouncer on port `6432` with an Npgsql connection string:

```text
Host=<host>;Port=6432;Database=<database>;Username=<role>.<branch-id>;Password=<password>;SSL Mode=VerifyFull
```

The `<branch-id>` suffix remains part of PlanetScale's generated username format; it is not the development/production isolation boundary. Do not add `sslrootcert=system`; it is a libpq parameter rather than an Npgsql certificate path. Verify each environment's credential independently with `SELECT 1` before deployment.

Schema migrations use a separate, stable `picklecrew_migrator` role in each database. CI connects directly to PlanetScale on port `5432` with that environment's libpq URL. Migration credentials are never used by the application or sent to Cloudflare.

## Health endpoints

- `/alive` is process-only liveness. It does not connect to the database and is used by the Cloudflare Container health probe.
- `/health` is dependency readiness. It runs all registered health checks, including the Npgsql connectivity check, and is used by the post-deployment smoke test.

Health responses do not expose connection strings or exception details. Database readiness checks have a five-second request timeout and successful responses are cached in the API process for ten seconds to limit repeated database work. `/alive` is not cached.

The Worker limits `/health` to 10 requests per minute per client IP before it looks up or starts the container. Development and production use separate rate-limiter namespace IDs. Rejected requests return `429`; `/alive` and all other API endpoints are not rate-limited.

Worker rate-limit counters are per Cloudflare location, permissive, and eventually consistent. This protects the container and database, but it does not prevent the Worker itself from being invoked. A WAF rate-limiting rule managed through Terraform remains an optional future enhancement for enforcement before Worker invocation.

## Deployments

The repository pipeline is defined in `.github/workflows/ci-cd.yml`. CI and deployment run as separate jobs so deployment credentials are only available to the deployment job.

- Pull requests run CI only.
- Pushes to `main` run CI and deploy development.
- Stable SemVer tags such as `v1.0.0` run CI and deploy production.

CI generates an idempotent EF Core SQL migration script from the same Release build that it tests. Every pull request, `main` push, and release tag uploads the script as the `ef-migrations` workflow artifact so it can be inspected. The production environment approval occurs only after CI has generated this reviewable artifact.

The deployment job installs the Worker dependencies and PostgreSQL client, downloads and validates the migration artifact produced by its own workflow run, and applies it immediately before `wrangler deploy`. A missing secret, missing or empty artifact, database connection failure, or SQL error stops the job before Cloudflare is changed.

The deployment writes the runtime database connection string to an owner-readable temporary file in `$RUNNER_TEMP`, passes it to `wrangler deploy --secrets-file`, and removes it through a shell cleanup trap. The value is stored as a Cloudflare Worker secret and is not committed to source or configuration. The separate migration URL is scoped only to the `psql` step and is never written to a file or passed to Wrangler.

Wrangler provisions the Custom Domains and their certificates. `workers.dev` and preview URLs are disabled.

## Required GitHub Environments

The `development` and `production` GitHub Environments each require:

- secret `CLOUDFLARE_API_TOKEN`
- secret `DATABASE_CONNECTION_STRING`
- secret `MIGRATIONS_DATABASE_URL`
- variable `CLOUDFLARE_ACCOUNT_ID`

`DATABASE_CONNECTION_STRING` must contain that environment's database-specific runtime credentials for PgBouncer port `6432`. `MIGRATIONS_DATABASE_URL` must contain that environment's direct PlanetScale URL for the `picklecrew_migrator` role on port `5432`. Development and production values must differ. Do not create repository-level database secrets. Keep production approval rules enabled.

Use a separate Cloudflare token for each environment. Create each token from the **Edit Cloudflare Workers** template, restrict it to the PickleCrew account and the `picklecrew.app` zone, and ensure it permits Worker deployment and secret updates.

## Database migrations

EF Core migrations are generated in CI as idempotent SQL and retained as a workflow artifact for seven days. Deployments apply the exact artifact from their CI job with `psql` and `ON_ERROR_STOP` before running `wrangler deploy`. Re-running a deployment is safe because the script checks `__EFMigrationsHistory` before applying each completed migration.

Deployed application instances never run migrations during startup. Local-development database initialization remains unchanged. Runtime traffic continues through PgBouncer on port `6432` with the non-DDL `picklecrew_app` role, while migrations connect directly on port `5432` with `picklecrew_migrator`. `MIGRATIONS_DATABASE_URL` is available only to the migration step and never reaches the Worker or container.

A failed migration prevents application deployment, but application rollback does not roll back database changes. Schema changes must use expand-and-contract compatibility so the current and next application versions can both operate during rollout. Destructive changes may run only after deployed code no longer reads or writes the affected objects. Recover from migration problems with a forward fix or the established PlanetScale backup and branch restoration process.
