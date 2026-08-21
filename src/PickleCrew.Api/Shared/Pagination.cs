namespace PickleCrew.Api.Shared;

public record PaginationOptions
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

internal sealed record PaginatedResult<T>
{
    public required T[] Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}

internal static class Pagination
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 100;

    public static Dictionary<string, string[]> Validate(PaginationOptions parameters)
    {
        var errors = new Dictionary<string, string[]>();

        if (parameters.Page is < 1)
        {
            errors[nameof(parameters.Page)] = ["Page must be at least 1."];
        }

        if (parameters.PageSize is < 1 or > MaximumPageSize)
        {
            errors[nameof(parameters.PageSize)] = [
                $"Page size must be between 1 and {MaximumPageSize}.",
            ];
        }

        return errors;
    }

    public static PaginatedResult<T> Paginate<T>(
        this IEnumerable<T> source,
        PaginationOptions parameters)
    {
        var items = source.ToArray();
        var requestedPage = parameters.Page ?? DefaultPage;
        var pageSize = parameters.PageSize ?? DefaultPageSize;
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling((double)items.Length / pageSize)
        );
        var page = Math.Min(requestedPage, totalPages);
        var paginatedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PaginatedResult<T>
        {
            Items = paginatedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = items.Length,
            TotalPages = totalPages,
        };
    }
}
