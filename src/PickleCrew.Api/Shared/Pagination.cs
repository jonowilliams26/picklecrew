using Microsoft.EntityFrameworkCore;

namespace PickleCrew.Api.Shared;

public interface IPaginationOptions
{
    int? Page { get; }

    int? PageSize { get; }
}

public record PaginatedHttpRequest : IPaginationOptions
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public sealed record PaginatedHttpResponse<T>
{
    public required T[] Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}

internal static class Pagination
{
    internal const int DefaultPage = 1;
    internal const int DefaultPageSize = 10;
    private const int MaximumPageSize = 100;

    public static Dictionary<string, string[]> Validate(IPaginationOptions options)
    {
        var errors = new Dictionary<string, string[]>();

        if (options.Page is < 1)
        {
            errors[nameof(options.Page)] = ["Page must be at least 1."];
        }

        if (options.PageSize is < 1 or > MaximumPageSize)
        {
            errors[nameof(options.PageSize)] = [
                $"Page size must be between 1 and {MaximumPageSize}.",
            ];
        }

        return errors;
    }

    public static Task<PaginatedHttpResponse<T>> PaginateAsync<T>(
        this IQueryable<T> source,
        IPaginationOptions options,
        CancellationToken cancellationToken = default)
    {
        return source.PaginateAsync(
            options,
            static item => item,
            cancellationToken
        );
    }

    public static async Task<PaginatedHttpResponse<TResult>> PaginateAsync<T, TResult>(
        this IQueryable<T> source,
        IPaginationOptions options,
        Func<T, TResult> selector,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var requestedPage = options.Page ?? DefaultPage;
        var pageSize = options.PageSize ?? DefaultPageSize;
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling((double)totalCount / pageSize)
        );
        var page = Math.Min(requestedPage, totalPages);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PaginatedHttpResponse<TResult>
        {
            Items = items.Select(selector).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }

    public static PaginatedHttpResponse<T> Paginate<T>(
        this IEnumerable<T> source,
        IPaginationOptions options)
    {
        var items = source.ToArray();
        var requestedPage = options.Page ?? DefaultPage;
        var pageSize = options.PageSize ?? DefaultPageSize;
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling((double)items.Length / pageSize)
        );
        var page = Math.Min(requestedPage, totalPages);
        var paginatedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PaginatedHttpResponse<T>
        {
            Items = paginatedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = items.Length,
            TotalPages = totalPages,
        };
    }
}
