# Picklecrew Project Context
- A pickleball stat tracking application.
- Target audience is social friend groups wanting to track their stats for bragging rights.
- Not targeting professional pickleball players.

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

Local Development Orchestration:
- Aspire
- Aspire is used for local development only and is not used for deployments to cloud environments.

## Generated API Client

- After any API change, including changes to endpoints, HTTP contracts, OpenAPI metadata, or OpenAPI transformers, regenerate and commit the web API client:
  ```shell
  dotnet build src/PickleCrew.Api/PickleCrew.Api.csproj
  pnpm --dir src/PickleCrew.Web api:generate
  ```
- Treat `src/PickleCrew.Web/src/lib/api/openapi.json` and `src/PickleCrew.Web/src/lib/api/generated` as read-only generated artifacts.
