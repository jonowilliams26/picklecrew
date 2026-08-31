# Picklecrew Project Context
- A pickleball stat tracking application.
- Target audience is social friend groups wanting to track their stats for bragging rights.
- Not targeting professional pickleball players.
- Domain documentation and ubiquitous language are available at `docs/domain.md`

## Conversational Style

- Keep answers short and concise
- No emojis in commits, issues, PR comments, or code
- No fluff or cheerful filler text (e.g., "Thanks @user" not "Thanks so much @user!")
- Technical prose only, be direct
- When the user asks a question, answer it first before making edits or running implementation commands.
- When responding to user feedback or an analysis, explicitly say whether you agree or disagree before saying what you changed.

## Tech Stack

Backend:
- ASP.NET Core Web API
- Minimal APIs
- Entity Framework Core
- PostgreSQL
- Hosted on Fly.io

Frontend:
- React 19, TypeScript, and TanStack Start
- Vite, Tailwind CSS 4, and shadcn/ui with Base UI
- WorkOS AuthKit authentication
- Hosted on Cloudflare Workers

Local Development Orchestration:
- Aspire
- Aspire is used for local development only and is not used for deployments to cloud environments.

## Frontend Conventions

- Use sentence case for user-facing copy; capitalize domain terms only when grammar requires it.
- Style TanStack `Link` elements with `buttonVariants`; do not render links through Base UI `Button`.

## Naming Conventions

- Name injected `PickleCrewDbContext` instances `database`.

## Authorization

- Do not re-check whether the caller is active in endpoint handlers or services; the authorization pipeline validates this from the access token.

## Generated Files

- Never resolve merge conflicts in generated artifacts manually. Resolve conflicts in their source files and regenerate.

### OpenAPI Schema

- Treat `src/PickleCrew.Web/src/lib/api/openapi.json` and `src/PickleCrew.Web/src/lib/api/generated` as read-only generated artifacts.
- After any API change, including changes to endpoints, HTTP contracts, OpenAPI metadata, or OpenAPI transformers, regenerate and commit the web API client:
  ```shell
  dotnet build src/PickleCrew.Api/PickleCrew.Api.csproj
  pnpm --dir src/PickleCrew.Web api:generate
  ```

### Wrangler Config

- Treat `src/PickleCrew.Web/worker-configuration.d.ts` as a read-only generated artifact.
- After any Wrangler configuration changes, regenerate and commit using:
  ```shell
  pnpm --dir src/PickleCrew.Web cf-typegen
  ```

## Verification

- Run the complete validation sequence from the repository root before completing implementation work:
  ```shell
  # Build the API and generate its OpenAPI document.
  dotnet build src/PickleCrew.Api/PickleCrew.Api.csproj

  # Regenerate the frontend API client from OpenAPI.
  pnpm --dir src/PickleCrew.Web api:generate

  # Run API unit tests first.
  dotnet test src/PickleCrew.UnitTests/PickleCrew.UnitTests.csproj

  # Verify the EF Core model does not require a migration.
  dotnet ef migrations has-pending-model-changes --project src/PickleCrew.Api/PickleCrew.Api.csproj --startup-project src/PickleCrew.Api/PickleCrew.Api.csproj --context PickleCrewDbContext

  # Run Aspire-backed API integration tests.
  dotnet test src/PickleCrew.IntegrationTests/PickleCrew.IntegrationTests.csproj

  # Run all frontend tests.
  pnpm --dir src/PickleCrew.Web test

  # Build and type-check the frontend.
  pnpm --dir src/PickleCrew.Web build

  # Verify generated Cloudflare binding types are current.
  pnpm --dir src/PickleCrew.Web cf-typegen --check
  ```
- Confirm generated API artifacts remain current after generation.
- Do not run the frontend outside Aspire for manual verification.
