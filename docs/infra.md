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

Each environment has one named `basic` container instance. The Worker passes its `DATABASE_CONNECTION_STRING` secret to the container as `ConnectionStrings__picklecrew`, which ASP.NET Core reads as `ConnectionStrings:picklecrew`.

## PlanetScale access

Use a separate PlanetScale database and database-specific credentials for each deployment environment. Development and production do not share a database or rely on branches of the same database for isolation. In each database, use the `picklecrew_app` application role with `pg_read_all_data` and `pg_write_all_data`. Do not grant DDL permissions and do not use the default `postgres` role for the API.

Use PlanetScale PgBouncer on port `6432` with an Npgsql connection string:

```text
Host=<host>;Port=6432;Database=<database>;Username=<role>.<branch-id>;Password=<password>;SSL Mode=VerifyFull
```

The `<branch-id>` suffix remains part of PlanetScale's generated username format; it is not the development/production isolation boundary. Do not add `sslrootcert=system`; it is a libpq parameter rather than an Npgsql certificate path. Verify each environment's credential independently with `SELECT 1` before deployment.

## Health endpoints

- `/alive` is process-only liveness. It does not connect to the database and is used by the Cloudflare Container health probe.
- `/health` is dependency readiness. It runs all registered health checks, including the Npgsql connectivity check, and is used by the post-deployment smoke test.

Health responses do not expose connection strings or exception details.

## Deployments

The repository pipeline is defined in `.github/workflows/ci-cd.yml`. CI and deployment run as separate jobs so deployment credentials are only available to the deployment job.

- Pull requests run CI only.
- Pushes to `main` run CI and deploy development.
- Stable SemVer tags such as `v1.0.0` run CI and deploy production.

The deployment writes the database connection string to an owner-readable temporary file in `$RUNNER_TEMP`, passes it to `wrangler deploy --secrets-file`, and removes it through a shell cleanup trap. The value is stored as a Cloudflare Worker secret and is not committed to source or configuration.

Wrangler provisions the Custom Domains and their certificates. `workers.dev` and preview URLs are disabled.

## Required GitHub Environments

The `development` and `production` GitHub Environments each require:

- secret `CLOUDFLARE_API_TOKEN`
- secret `DATABASE_CONNECTION_STRING`
- variable `CLOUDFLARE_ACCOUNT_ID`

`DATABASE_CONNECTION_STRING` must contain that environment's database-specific PlanetScale credentials. Do not create a repository-level database secret. Keep production approval rules enabled.

Use a separate Cloudflare token for each environment. Create each token from the **Edit Cloudflare Workers** template, restrict it to the PickleCrew account and the `picklecrew.app` zone, and ensure it permits Worker deployment and secret updates.

## Database migrations

Database migrations remain outside the deployment pipeline and are not executed by Wrangler or GitHub Actions. This infrastructure change only configures credentials and verifies connectivity; applying PlanetScale schema changes is out of scope.
