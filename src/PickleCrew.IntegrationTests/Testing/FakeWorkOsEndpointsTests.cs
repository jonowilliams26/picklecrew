using System.Net;
using Xunit;

namespace PickleCrew.IntegrationTests.Testing;

[Collection(nameof(AppHostCollection))]
public sealed class FakeWorkOsEndpointsTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Setup_WhenRequestIsValid_ShouldCreateOrganizationUserAndMembership()
    {
        var identity = TestIdentity.Create();
        var externalId = $"crew_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        var setup = await client.SetupAsync(
            identity.UserId,
            "Kitchen Legends",
            externalId,
            FakeMembershipRole.Member
        );

        Assert.Equal(identity.UserId, setup.UserId);
        Assert.StartsWith("org_", setup.OrganizationId);
        Assert.StartsWith("om_", setup.MembershipId);
        Assert.Equal(
            setup.OrganizationId,
            await client.FindOrganizationByExternalIdAsync(externalId)
        );
        Assert.Equal(
            setup.MembershipId,
            await client.FindActiveMembershipAsync(identity.UserId, setup.OrganizationId)
        );
    }

    [Fact]
    public async Task CreateEndpoints_WhenMembershipIsActive_ShouldMakeStateFindable()
    {
        var identity = TestIdentity.Create();
        var externalId = $"crew_test_{Guid.NewGuid():N}";
        var membershipId = $"om_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        var createdUserId = await client.CreateUserAsync(identity.UserId);
        var createdOrganizationId = await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            externalId
        );
        var createdMembershipId = await client.CreateMembershipAsync(
            membershipId,
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        Assert.Equal(identity.UserId, createdUserId);
        Assert.Equal(identity.OrganizationId, createdOrganizationId);
        Assert.Equal(membershipId, createdMembershipId);
        Assert.Equal(
            identity.OrganizationId,
            await client.FindOrganizationByExternalIdAsync(externalId)
        );
        Assert.Equal(
            membershipId,
            await client.FindActiveMembershipAsync(
                identity.UserId,
                identity.OrganizationId!
            )
        );
    }

    [Fact]
    public async Task FindActiveMembership_WhenMembershipIsInactive_ShouldReturnNotFound()
    {
        var identity = TestIdentity.Create();
        var membershipId = $"om_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        await client.CreateUserAsync(identity.UserId);
        await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await client.CreateMembershipAsync(
            membershipId,
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Inactive
        );

        var foundMembershipId = await client.FindActiveMembershipAsync(
            identity.UserId,
            identity.OrganizationId!
        );

        Assert.Null(foundMembershipId);
    }

    [Theory]
    [InlineData(FakeWorkOsOperation.FindOrganizationByExternalId)]
    [InlineData(FakeWorkOsOperation.CreateOrganization)]
    [InlineData(FakeWorkOsOperation.FindActiveMembership)]
    [InlineData(FakeWorkOsOperation.CreateMembership)]
    public async Task ProvisioningOperation_WhenFailureOutcomeExists_ShouldKeepFailureScoped(
        FakeWorkOsOperation operation)
    {
        using var client = fixture.CreateFakeWorkOsClient();
        var failedScenario = await CreateScenarioAsync(client);
        var healthyScenario = await CreateScenarioAsync(client);
        var failureScope = GetOutcomeScope(operation, failedScenario);

        await client.CreateOutcomeAsync(operation, failureScope);

        using var failedResponse = await SendOperationAsync(
            client,
            operation,
            failedScenario
        );
        using var healthyResponse = await SendOperationAsync(
            client,
            operation,
            healthyScenario
        );

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedResponse.StatusCode);
        Assert.True(healthyResponse.IsSuccessStatusCode);
    }

    private static async Task<FakeWorkOsScenario> CreateScenarioAsync(
        FakeWorkOsClient client)
    {
        var identity = TestIdentity.Create();
        var externalId = $"crew_test_{Guid.NewGuid():N}";

        await client.CreateUserAsync(identity.UserId);
        await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            externalId
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        return new FakeWorkOsScenario
        {
            UserId = identity.UserId,
            OrganizationId = identity.OrganizationId!,
            OrganizationExternalId = externalId,
        };
    }

    private static string GetOutcomeScope(
        FakeWorkOsOperation operation,
        FakeWorkOsScenario scenario)
    {
        return operation switch
        {
            FakeWorkOsOperation.FindOrganizationByExternalId
                or FakeWorkOsOperation.CreateOrganization => scenario.OrganizationExternalId,
            FakeWorkOsOperation.FindActiveMembership
                or FakeWorkOsOperation.CreateMembership => scenario.UserId,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };
    }

    private static Task<HttpResponseMessage> SendOperationAsync(
        FakeWorkOsClient client,
        FakeWorkOsOperation operation,
        FakeWorkOsScenario scenario)
    {
        return operation switch
        {
            FakeWorkOsOperation.FindOrganizationByExternalId =>
                client.GetOrganizationByExternalIdAsync(scenario.OrganizationExternalId),
            FakeWorkOsOperation.CreateOrganization => client.SendCreateOrganizationAsync(
                "Kitchen Legends",
                scenario.OrganizationExternalId
            ),
            FakeWorkOsOperation.FindActiveMembership => client.GetActiveMembershipAsync(
                scenario.UserId,
                scenario.OrganizationId
            ),
            FakeWorkOsOperation.CreateMembership => client.SendCreateMembershipAsync(
                scenario.UserId,
                scenario.OrganizationId,
                FakeMembershipRole.Member
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };
    }

    private sealed record FakeWorkOsScenario
    {
        public required string UserId { get; init; }

        public required string OrganizationId { get; init; }

        public required string OrganizationExternalId { get; init; }
    }
}
