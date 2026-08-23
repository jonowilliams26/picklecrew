using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Crews;
using PickleCrew.Api.Shared;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class GetCrewMembersEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetCrewMembers_WhenCrewHasMultipleMembers_ShouldReturnAlphabeticalMembersWithPagination()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(displayName: "Zoe Admin", role: FakeMembershipRole.Admin)
            .WithMember(
                displayName: "Charlie Kitchen",
                profilePictureUrl: "https://example.test/charlie.jpg"
            )
            .WithMember(displayName: "Amy Dink")
            .WithMember(
                displayName: "Aaron Inactive",
                status: FakeMembershipStatus.Inactive
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var firstPageResponse = await apiClient.GetAsync(
            "/crews/members?page=1&pageSize=2"
        );
        using var secondPageResponse = await apiClient.GetAsync(
            "/crews/members?page=2&pageSize=2"
        );

        firstPageResponse.EnsureSuccessStatusCode();
        secondPageResponse.EnsureSuccessStatusCode();
        var firstPage = await firstPageResponse.Content
            .ReadFromJsonAsync<PaginatedHttpResponse<CrewMemberHttpResponse>>();
        var secondPage = await secondPageResponse.Content
            .ReadFromJsonAsync<PaginatedHttpResponse<CrewMemberHttpResponse>>();
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(1, firstPage.Page);
        Assert.Collection(
            firstPage.Items,
            member =>
            {
                Assert.Equal("Amy Dink", member.DisplayName);
                Assert.Equal("member", member.Role);
            },
            member =>
            {
                Assert.Equal("Charlie Kitchen", member.DisplayName);
                Assert.Equal("member", member.Role);
                Assert.Equal(
                    "https://example.test/charlie.jpg",
                    member.ProfilePictureUrl
                );
                Assert.EndsWith(
                    "@example.test",
                    member.EmailAddress,
                    StringComparison.Ordinal
                );
            }
        );
        Assert.Equal(2, secondPage.Page);
        Assert.All(
            secondPage.Items,
            member => Assert.Equal("admin", member.Role)
        );
        Assert.Equal("Zoe Admin", secondPage.Items[1].DisplayName);
    }

    [Fact]
    public async Task GetCrewMembers_WhenPaginationIsInvalid_ShouldReturnBadRequest()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/crews/members?page=0&pageSize=101"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
