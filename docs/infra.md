# Infrastructure

PickleCrew uses ASP.NET Core 10 for the API, Docker for its image, and a TypeScript Cloudflare Worker to manage a Cloudflare Container. Aspire remains local-development-only.

## Architecture

```text
Client -> Cloudflare Custom Domain -> Worker -> Container:8080 -> PickleCrew.Api
```

| Environment | Worker | Domain |
|---|---|---|
| `development` | `picklecrew-api-development` | `api.dev.picklecrew.app` |
| `production` | `picklecrew-api-production` | `api.picklecrew.app` |

Each environment has one named `basic` container instance. `/alive` is the process-only container health endpoint.

## Deployments

- Pull requests run `.github/workflows/ci.yml`.
- Pushes to `main` run CI and deploy development.
- Stable SemVer tags such as `v1.0.0` run CI and deploy production. Tags may be created from `main` or a release branch.

Wrangler provisions the Custom Domains and their certificates. `workers.dev` and preview URLs are disabled.

## Required GitHub Environments

Create `development` and `production` environments. Each requires:

- secret `CLOUDFLARE_API_TOKEN`
- variable `CLOUDFLARE_ACCOUNT_ID`

Use a separate Cloudflare token for each environment. Create each token from the **Edit Cloudflare Workers** template and restrict it to the PickleCrew account and the `picklecrew.app` zone.

PostgreSQL is not currently deployed. No database connection strings, secrets, migrations, or database health checks are part of this infrastructure.
