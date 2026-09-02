using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class ChangeCrewMemberRoleEndpointTests(AppHostFixture fixture)
{
    [Theory]
    [InlineData(CrewMemberRole.Member, CrewMemberRole.Admin)]
    [InlineData(CrewMemberRole.Admin, CrewMemberRole.Member)]
    public async Task ChangeCrewMemberRole_WhenAdministratorChangesAnotherActiveMember_ShouldUpdateRole(
        CrewMemberRole initialRole,
        CrewMemberRole newRole)
    {
        var initialWorkOsRole = initialRole == CrewMemberRole.Admin
            ? FakeMembershipRole.Admin
            : FakeMembershipRole.Member;
        var context = await fixture.CreateCrewBuilder()
            .WithMember(role: initialWorkOsRole)
            .BuildAsync();
        var member = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PatchAsJsonAsync(
            $"/crews/members/{Uri.EscapeDataString(member.UserId)}/role",
            new ChangeCrewMemberRoleHttpRequestBody { Role = newRole },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembers = await fakeWorkOs.ListActiveMembersAsync(
            context.OrganizationId
        );
        var updatedMember = Assert.Single(
            activeMembers,
            candidate => candidate.WorkOsUserId == member.UserId
        );
        Assert.Equal(
            newRole == CrewMemberRole.Admin
                ? FakeMembershipRole.Admin
                : FakeMembershipRole.Member,
            updatedMember.Role
        );
    }

    [Fact]
    public async Task ChangeCrewMemberRole_WhenAdministratorChangesThemselves_ShouldReturnForbiddenAndKeepRole()
    {
        var context = await fixture.CreateCrewBuilder().BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PatchAsJsonAsync(
            $"/crews/members/{Uri.EscapeDataString(context.Administrator.UserId)}/role",
            new ChangeCrewMemberRoleHttpRequestBody
            {
                Role = CrewMemberRole.Member,
            },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembers = await fakeWorkOs.ListActiveMembersAsync(
            context.OrganizationId
        );
        var administrator = Assert.Single(
            activeMembers,
            candidate => candidate.WorkOsUserId == context.Administrator.UserId
        );
        Assert.Equal(FakeMembershipRole.Admin, administrator.Role);
    }

    [Fact]
    public async Task ChangeCrewMemberRole_WhenCallerIsNotAdministrator_ShouldReturnForbidden()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var caller = context.Members[0];
        var target = context.Members[1];
        using var apiClient = fixture.CreateApiClient(caller);

        using var response = await apiClient.PatchAsJsonAsync(
            $"/crews/members/{Uri.EscapeDataString(target.UserId)}/role",
            new ChangeCrewMemberRoleHttpRequestBody
            {
                Role = CrewMemberRole.Admin,
            },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembers = await fakeWorkOs.ListActiveMembersAsync(
            context.OrganizationId
        );
        var unchangedMember = Assert.Single(
            activeMembers,
            candidate => candidate.WorkOsUserId == target.UserId
        );
        Assert.Equal(FakeMembershipRole.Member, unchangedMember.Role);
    }

    [Fact]
    public async Task ChangeCrewMemberRole_WhenTargetIsNotAnActiveCrewMember_ShouldReturnNotFound()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(status: FakeMembershipStatus.Inactive)
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PatchAsJsonAsync(
            $"/crews/members/{Uri.EscapeDataString(context.Members[0].UserId)}/role",
            new ChangeCrewMemberRoleHttpRequestBody
            {
                Role = CrewMemberRole.Admin,
            },
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
