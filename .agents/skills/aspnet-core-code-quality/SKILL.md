---
name: aspnet-core-code-quality
description: Apply repository ASP.NET Core project and code-quality conventions when creating, configuring, writing, modifying, refactoring, or reviewing ASP.NET Core web APIs.
---

# ASP.NET Core Code Quality

Apply these conventions to all ASP.NET Core projects and web API code in scope.

## Package management

Use NuGet Central Package Management. Define package versions in a repository-level `Directory.Packages.props` file and omit versions from project-level `PackageReference` items.

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
  </ItemGroup>
</Project>
```

```xml
<!-- Project file -->
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
</ItemGroup>
```

## Compiler warnings

Enable warnings as errors for every project. Prefer defining this once in a repository-level `Directory.Build.props` file so all projects use the same policy.

```xml
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

## Endpoint style

Use Minimal APIs instead of controllers. Define endpoints with route-handler methods rather than controller classes and action methods.

## Dependency injection entry point

Create `DependencyInjection.cs` at the application project root. Keep every extension method used to register application services in this file so service registration has one discoverable entry point. Call these extension methods from `Program.cs` rather than defining registration logic there or scattering registration extensions across feature files.

```csharp
internal static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ExampleService>();

        return services;
    }
}
```

## Endpoint entry point

Create `Endpoints.cs` at the application project root as the entry point for all application endpoints. Define `IEndpoint` in this file, implement it on each endpoint, and register every endpoint through `MapEndpoints`. Call `app.MapEndpoints()` from `Program.cs` rather than mapping individual endpoints there.

```csharp
internal static class Endpoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(string.Empty)
            .MapEndpoint<CreatePersistenceProbeEndpoint>();

        return endpoints;
    }

    public static IEndpointRouteBuilder MapEndpoint<TEndpoint>(
        this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);

        return app;
    }
}

internal interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}
```

## Vertical slice architecture

Organize each use case as its own vertical slice within its module. A slice owns its endpoint, request and response contracts, handler, and use-case-specific supporting code so it can evolve independently.

Start each use case with the simple, single-file structure. Refactor it into its own directory only when the use case grows complex enough to need multiple files. Allow a complex use case to contain as many internal services and supporting files as it needs. Do not introduce the complex structure speculatively, force every slice into the same structure, or extract slice-specific code into shared abstractions merely to reduce its size.

```text
<module>/
├── Services/               # Shared services used by multiple slices in the module
├── SimpleEndpoint.cs       # Endpoint, request, response, and handler
└── ComplexEndpoint/
    ├── Endpoint.cs
    ├── Services/
    └── <other use-case-specific code>
```

Place code in `Services` only when it is genuinely reused by multiple slices in the module. Keep all other code inside the slice that owns the use case. Do not organize use-case code into cross-cutting technical folders that couple independently evolving slices.

## Input binding

Group endpoint inputs into request records. Rely on Minimal API binding inference and do not add source attributes such as `[FromBody]`, `[FromRoute]`, `[FromQuery]`, or `[FromServices]` when the framework already infers the intended source. Add a source attribute only when it changes or disambiguates the inferred binding, such as binding a property from a specifically named header.

```csharp
// Bad: the complex request is already inferred from the request body.
static void Handle([FromBody] ExampleHttpRequest request)
{
}

// Good
static void Handle(ExampleHttpRequest request)
{
}
```

Use `[AsParameters]` only when:

- The HTTP method does not support inferred body binding and the request groups route, query, or header values.
- The request composes values from multiple sources, such as route values and a request body.

Do not use `[AsParameters]` for a body-only request.

In the following example, `Id` is inferred from the matching route parameter, while `[FromHeader]` is required because Minimal APIs do not infer values from request headers:

```csharp
app.MapGet("/posts/{id}", GetPost);

static Results<Ok<GetPostHttpResponse>, NotFound> GetPost(
    [AsParameters] GetPostHttpRequest request)
{
    // Endpoint implementation.
}

public record GetPostHttpRequest
{
    public required int Id { get; init; }

    [FromHeader(Name = "X-Correlation-Id")]
    public required string CorrelationId { get; init; }
}
```

## HTTP contracts

- Define request and response contracts as immutable `record` types.
- Use `init` only properties. Do not use `set` properties.
- Add the `required` keyword to every required property. Without it, OpenAPI schema generation marks the property as optional.
- Suffix request contract names with `HttpRequest`.
- Suffix response contract names with `HttpResponse`.

```csharp
public record GetPostsHttpRequest
{
    public required int Page { get; init; }
}

public record GetPostsHttpResponse
{
    public required IReadOnlyList<PostHttpResponse> Posts { get; init; }
}
```

## Typed responses

Return strongly typed responses with `TypedResults`.

When an endpoint can return multiple response types, declare its return type with `Results<TResponse1, TResponse2>` or the corresponding higher-arity `Results` type.

```csharp
static Results<Ok<GetPostHttpResponse>, NotFound> GetPost(
    [AsParameters] GetPostHttpRequest request)
{
    GetPostHttpResponse? response = FindPost(request.Id);
    if (response is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(response);
}
```

When reviewing ASP.NET Core code, identify each violation and propose or apply a correction that follows these conventions.
