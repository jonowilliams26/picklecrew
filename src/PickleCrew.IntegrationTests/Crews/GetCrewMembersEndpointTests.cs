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
    public async Task GetCrewMembers_WhenCrewHasMembersAndInvitations_ShouldReturnAlphabeticalRosterWithOnlyPendingInvitations()
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
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var pendingInvitation = await fakeWorkOs.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            context.OrganizationId,
            "pending@example.test",
            FakeMembershipRole.Member,
            context.Administrator.UserId
        );
        await fakeWorkOs.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            context.OrganizationId,
            "accepted@example.test",
            FakeMembershipRole.Member,
            context.Administrator.UserId,
            FakeInvitationState.Accepted
        );
        await fakeWorkOs.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            context.OrganizationId,
            "revoked@example.test",
            FakeMembershipRole.Member,
            context.Administrator.UserId,
            FakeInvitationState.Revoked
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var firstPageResponse = await apiClient.GetAsync(
            "/crews/members?page=1&pageSize=3"
        );
        using var secondPageResponse = await apiClient.GetAsync(
            "/crews/members?page=2&pageSize=3"
        );

        firstPageResponse.EnsureSuccessStatusCode();
        secondPageResponse.EnsureSuccessStatusCode();
        var firstPage = await firstPageResponse.Content
            .ReadFromJsonAsync<PaginatedHttpResponse<CrewMemberHttpResponse>>(
                TestJsonSerializerOptions.Default
            );
        var secondPage = await secondPageResponse.Content
            .ReadFromJsonAsync<PaginatedHttpResponse<CrewMemberHttpResponse>>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(1, firstPage.Page);
        Assert.Collection(
            firstPage.Items,
            member =>
            {
                Assert.Equal("Amy Dink", member.DisplayName);
                Assert.Equal("member", member.Role);
                Assert.Equal(CrewRosterEntryStatus.Active, member.Status);
            },
            member =>
            {
                Assert.Equal("Charlie Kitchen", member.DisplayName);
                Assert.Equal("member", member.Role);
                Assert.Equal(CrewRosterEntryStatus.Active, member.Status);
                Assert.Equal(
                    "https://example.test/charlie.jpg",
                    member.ProfilePictureUrl
                );
                Assert.EndsWith(
                    "@example.test",
                    member.EmailAddress,
                    StringComparison.Ordinal
                );
            },
            invitation =>
            {
                Assert.Equal(pendingInvitation.Id, invitation.Id);
                Assert.Equal("pending@example.test", invitation.DisplayName);
                Assert.Equal("pending@example.test", invitation.EmailAddress);
                Assert.Equal(CrewRosterEntryStatus.Pending, invitation.Status);
                Assert.Null(invitation.Role);
                Assert.Null(invitation.ProfilePictureUrl);
            }
        );
        Assert.Equal(2, secondPage.Page);
        Assert.All(
            secondPage.Items,
            member => Assert.Equal(CrewRosterEntryStatus.Active, member.Status)
        );
        Assert.Equal("Zoe Admin", secondPage.Items[1].DisplayName);
    }

    [Fact]
    public async Task GetCrewMembers_WhenCallerIsActiveMember_ShouldIncludePendingInvitation()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(emailAddress: "member@example.test", displayName: "Active Member")
            .BuildAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            context.OrganizationId,
            "pending@example.test",
            FakeMembershipRole.Member,
            context.Administrator.UserId
        );
        using var apiClient = fixture.CreateApiClient(context.Members[0]);

        using var response = await apiClient.GetAsync("/crews/members");

        response.EnsureSuccessStatusCode();
        var roster = await response.Content
            .ReadFromJsonAsync<PaginatedHttpResponse<CrewMemberHttpResponse>>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(roster);
        Assert.Contains(
            roster.Items,
            entry => entry.Status == CrewRosterEntryStatus.Pending
                && entry.EmailAddress == "pending@example.test"
        );
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
