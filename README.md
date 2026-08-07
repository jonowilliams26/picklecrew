# PickleCrew

Start the app locally:

```shell
aspire start
```

Stop the app:

```shell
aspire stop
```

## Generate the API client

Build the API OpenAPI document, then generate the typed TypeScript client:

```shell
dotnet build src/PickleCrew.Api/PickleCrew.Api.csproj
pnpm --dir src/PickleCrew.Web api:generate
```

Commit changes to `src/PickleCrew.Web/src/lib/api`, including `openapi.json` and the `generated` directory. These files are generated artifacts and must not be edited manually.
