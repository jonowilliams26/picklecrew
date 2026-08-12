# Infrastructure

## Environments

| Environment | Deployment trigger | Web | API |
|---|---|---|---|
| `development` | Push to `main` | `https://dev.picklecrew.app` | `https://api.dev.picklecrew.app` |
| `production` | Stable SemVer tag such as `v1.0.0` | `https://picklecrew.app` | `https://api.picklecrew.app` |

## Architecture

- **Web:** TanStack Start hosted on Cloudflare Workers.
- **API:** ASP.NET Core container hosted on Fly.io.
- **Database:** PlanetScale Postgres, with a separate database for each environment.
- **Edge:** Cloudflare provides DNS, TLS, proxying, WAF, and DDoS protection.

## GitHub configuration

### Environments

- `development`
- `production`

### Variables

| Name | Purpose |
|---|---|
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare account containing the Workers and `picklecrew.app` zone |
| `WORKOS_CLIENT_ID` | Client ID from the matching WorkOS environment |

### Secrets

| Name | Purpose |
|---|---|
| `CLOUDFLARE_API_TOKEN` | Token used to deploy the web Worker |
| `FLY_API_TOKEN` | App-scoped token used to deploy the API |
| `DATABASE_CONNECTION_STRING` | Npgsql connection string used by the API |
| `MIGRATIONS_DATABASE_URL` | PostgreSQL URL used to apply database migrations |
| `WORKOS_API_KEY` | API key from the matching WorkOS environment |
| `WORKOS_COOKIE_PASSWORD` | Unique random value of at least 32 characters |

Do not reuse database, WorkOS, Fly, or cookie credentials between environments.

## One-time setup

1. **Create the databases.** Use the PlanetScale CLI to create a Postgres database for each environment in Sydney:

   ```shell
   pscale database create picklecrew-development --engine postgresql --region aws-ap-southeast-2 --wait
   pscale database create picklecrew-production --engine postgresql --region aws-ap-southeast-2 --wait
   ```

   After both databases are ready, create an application role and a migration role on each database's `main` branch:

   ```shell
   pscale role create picklecrew-development main picklecrew_app --inherited-roles pg_read_all_data,pg_write_all_data
   pscale role create picklecrew-development main picklecrew_migrator --inherited-roles postgres

   pscale role create picklecrew-production main picklecrew_app --inherited-roles pg_read_all_data,pg_write_all_data
   pscale role create picklecrew-production main picklecrew_migrator --inherited-roles postgres
   ```

   Save the credentials returned when each role is created. Use them to build `DATABASE_CONNECTION_STRING` and `MIGRATIONS_DATABASE_URL`.

2. **Create the Fly.io applications.**

   ```shell
   fly apps create picklecrew-api-development --org <organization-slug>
   fly apps create picklecrew-api-production --org <organization-slug>
   ```

   Create an app-scoped deployment token for each app and save it as that environment's `FLY_API_TOKEN`:

   ```shell
   fly tokens create deploy --app picklecrew-api-development
   fly tokens create deploy --app picklecrew-api-production
   ```

3. **Configure the API domains.** Add `api.dev.picklecrew.app` and `api.picklecrew.app` to their matching Fly apps:

   ```shell
   fly certs add api.dev.picklecrew.app --app picklecrew-api-development
   fly certs add api.picklecrew.app --app picklecrew-api-production
   ```

   Add the DNS records requested by Fly to Cloudflare, enable proxying, and set Cloudflare SSL/TLS mode to **Full (strict)**.

4. **Configure Cloudflare.** Create an API token from the **Edit Cloudflare Workers** template, restricted to the PickleCrew account and zone. Remove any existing DNS record or Worker route for `dev.picklecrew.app` and `picklecrew.app`; Wrangler creates these Worker custom domains during deployment.

5. **Configure WorkOS.** Configure these URLs:

   | Environment | Redirect URI | Sign-in endpoint | Sign-out URI |
   |---|---|---|---|
   | Development | `https://dev.picklecrew.app/api/auth/callback` | `https://dev.picklecrew.app/api/auth/sign-in` | `https://dev.picklecrew.app/` |
   | Production | `https://picklecrew.app/api/auth/callback` | `https://picklecrew.app/api/auth/sign-in` | `https://picklecrew.app/` |
