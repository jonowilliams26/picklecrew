using PickleCrew.Api.Shared;
using Xunit;

namespace PickleCrew.UnitTests.Shared;

public sealed class PaginationTests
{
    [Fact]
    public void Validate_WhenPageAndPageSizeAreInvalid_ShouldReturnBothErrors()
    {
        var parameters = new PaginationOptions
        {
            Page = 0,
            PageSize = 101,
        };

        var errors = Pagination.Validate(parameters);

        Assert.Equal(
            "Page must be at least 1.",
            Assert.Single(errors[nameof(parameters.Page)])
        );
        Assert.Equal(
            "Page size must be between 1 and 100.",
            Assert.Single(errors[nameof(parameters.PageSize)])
        );
    }

    [Fact]
    public void Paginate_WhenRequestedPageExceedsLastPage_ShouldReturnLastPage()
    {
        var parameters = new PaginationOptions
        {
            Page = 4,
            PageSize = 2,
        };

        var result = Enumerable.Range(1, 5).Paginate(parameters);

        Assert.Equal([5], result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }
}
