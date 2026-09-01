using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Caching;

internal static class HybridCacheExtensions
{
    public static ValueTask<T> GetOrCreateAsync<T>(
        this HybridCache cache,
        ICacheOption cacheOption,
        CrewUserContext crewUserContext,
        IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default)
    {
        return cache.GetOrCreateAsync(
            cacheOption,
            crewUserContext,
            scopeFactory,
            factory,
            static (services, scopedFactory, factoryCancellationToken) =>
                scopedFactory(services, factoryCancellationToken),
            cancellationToken
        );
    }

    public static ValueTask<T> GetOrCreateAsync<T, TState>(
        this HybridCache cache,
        ICacheOption cacheOption,
        CrewUserContext crewUserContext,
        IServiceScopeFactory scopeFactory,
        TState state,
        Func<IServiceProvider, TState, CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default)
    {
        var crewContext = CrewUserContextSnapshot.Create(crewUserContext);

        return cache.GetOrCreateAsync(
            cacheOption.Key,
            async factoryCancellationToken =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                crewContext.ApplyTo(
                    scope.ServiceProvider.GetRequiredService<CrewUserContext>()
                );

                return await factory(
                    scope.ServiceProvider,
                    state,
                    factoryCancellationToken
                );
            },
            tags: cacheOption.Tags.Select(tag => tag.Value),
            cancellationToken: cancellationToken
        );
    }

    public static ValueTask RemoveByTagAsync(
        this HybridCache cache,
        ICacheTag[] tags,
        CancellationToken cancellationToken = default)
    {
        return cache.RemoveByTagAsync(
            tags.Select(tag => tag.Value),
            cancellationToken
        );
    }
}

internal sealed record CrewUserContextSnapshot
{
    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }

    public required string OrganizationId { get; init; }

    public required string Role { get; init; }

    public required string UserId { get; init; }

    public static CrewUserContextSnapshot Create(CrewUserContext context)
    {
        return new CrewUserContextSnapshot
        {
            CrewId = context.CrewId,
            CrewUuid = context.CrewUuid,
            OrganizationId = context.OrganizationId,
            Role = context.Role,
            UserId = context.UserId,
        };
    }

    public void ApplyTo(CrewUserContext context)
    {
        context.CrewId = CrewId;
        context.CrewUuid = CrewUuid;
        context.OrganizationId = OrganizationId;
        context.Role = Role;
        context.UserId = UserId;
    }
}
