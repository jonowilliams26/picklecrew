using Microsoft.Extensions.Caching.Hybrid;

namespace PickleCrew.Api.Caching;

internal static class HybridCacheExtensions
{
    public static ValueTask<T> GetOrCreateAsync<T>(
        this HybridCache cache,
        ICacheOption cacheOption,
        Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default)
    {
        return cache.GetOrCreateAsync(
            cacheOption.Key,
            factory,
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
