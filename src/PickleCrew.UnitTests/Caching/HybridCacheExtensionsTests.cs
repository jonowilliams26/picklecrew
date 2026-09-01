using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Services;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace PickleCrew.UnitTests.Caching;

public sealed class HybridCacheExtensionsTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenFactoryRuns_ShouldUseDedicatedInitializedScope()
    {
        var services = new ServiceCollection();
        services.AddFusionCache().AsHybridCache();
        services.AddScoped<CrewUserContext>();
        services.AddScoped<ScopedMarker>();

        await using var serviceProvider = services.BuildServiceProvider();
        await using var requestScope = serviceProvider.CreateAsyncScope();
        var requestServices = requestScope.ServiceProvider;
        var requestContext = requestServices.GetRequiredService<CrewUserContext>();
        requestContext.CrewId = 42;
        requestContext.CrewUuid = Guid.NewGuid();
        requestContext.OrganizationId = "org_crew";
        requestContext.Role = "member";
        requestContext.UserId = "user_crew";
        var requestMarker = requestServices.GetRequiredService<ScopedMarker>();
        var cache = requestServices.GetRequiredService<HybridCache>();

        var result = await cache.GetOrCreateAsync(
            new GamesHistoryCacheOption(requestContext.CrewUuid, 1, 10),
            requestContext,
            requestServices.GetRequiredService<IServiceScopeFactory>(),
            static (factoryServices, _) =>
            {
                var factoryContext = factoryServices
                    .GetRequiredService<CrewUserContext>();

                return ValueTask.FromResult(new FactoryResult
                {
                    Context = CrewUserContextSnapshot.Create(factoryContext),
                    Marker = factoryServices.GetRequiredService<ScopedMarker>(),
                });
            }
        );

        Assert.NotSame(requestMarker, result.Marker);
        Assert.False(requestMarker.IsDisposed);
        Assert.True(result.Marker.IsDisposed);
        Assert.Equal(requestContext.CrewId, result.Context.CrewId);
        Assert.Equal(requestContext.CrewUuid, result.Context.CrewUuid);
        Assert.Equal(requestContext.OrganizationId, result.Context.OrganizationId);
        Assert.Equal(requestContext.Role, result.Context.Role);
        Assert.Equal(requestContext.UserId, result.Context.UserId);
    }
}

internal sealed record FactoryResult
{
    public required CrewUserContextSnapshot Context { get; init; }

    public required ScopedMarker Marker { get; init; }
}

internal sealed class ScopedMarker : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
