using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class GetCrewMembersEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetCrewMembers_WhenCrewHasMultipleMembers_ShouldReturnAlphabeticalMembersWithPagination()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            "Zoe Admin",
            FakeMembershipRole.Admin
        );
        await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            "Charlie Kitchen",
            FakeMembershipRole.Member,
            "https://example.test/charlie.jpg"
        );
        await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            "Amy Dink",
            FakeMembershipRole.Member
        );
        await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            "Aaron Inactive",
            FakeMembershipRole.Member,
            status: FakeMembershipStatus.Inactive
        );
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
            .ReadFromJsonAsync<GetCrewMembersHttpResponse>();
        var secondPage = await secondPageResponse.Content
            .ReadFromJsonAsync<GetCrewMembersHttpResponse>();
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(1, firstPage.Page);
        Assert.Collection(
            firstPage.Members,
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
            secondPage.Members,
            member => Assert.Equal("admin", member.Role)
        );
        Assert.Equal("Zoe Admin", secondPage.Members[1].DisplayName);
    }

    [Fact]
    public async Task GetCrewMembers_WhenPaginationIsInvalid_ShouldReturnBadRequest()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/crews/members?page=0&pageSize=101"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task CreateMemberAsync(
        FakeWorkOsClient fakeWorkOs,
        string organizationId,
        string displayName,
        FakeMembershipRole role,
        string? profilePictureUrl = null,
        FakeMembershipStatus status = FakeMembershipStatus.Active)
    {
        var member = TestIdentity.Create();
        await fakeWorkOs.CreateUserAsync(
            member.UserId,
            member.EmailAddress,
            displayName: displayName,
            profilePictureUrl: profilePictureUrl
        );
        await fakeWorkOs.CreateMembershipAsync(
            $"membership_test_{Guid.NewGuid():N}",
            member.UserId,
            organizationId,
            role,
            status
        );
    }
}
