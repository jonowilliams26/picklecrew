using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Onboarding;
using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.IntegrationTests.Onboarding;

[Collection(nameof(AppHostCollection))]
public sealed class CreateCrewHandlerTests(AppHostFixture fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsBlank_ShouldReturnValidationError(string? name)
    {
        var errors = CreateCrewEndpoint.Validate(
            new CreateCrewHttpRequest { Name = name! }
        );

        Assert.Contains(nameof(CreateCrewHttpRequest.Name), errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_WhenNameLengthIsValid_ShouldReturnNoValidationErrors(int length)
    {
        var errors = CreateCrewEndpoint.Validate(
            new CreateCrewHttpRequest { Name = $" {new string('a', length)} " }
        );

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ShouldReturnValidationError()
    {
        var errors = CreateCrewEndpoint.Validate(
            new CreateCrewHttpRequest { Name = $" {new string('a', 101)} " }
        );

        Assert.Contains(nameof(CreateCrewHttpRequest.Name), errors);
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsValid_ShouldProvisionAndPersistCrew()
    {
        var userId = $"user_{Guid.NewGuid():N}";
        var workOsUserContext = new WorkOsUserContext { UserId = userId };
        var workOs = new FakeWorkOsProvisioningService();

        await using var dbContext = fixture.CreateDbContext();
        var created = await CreateCrewEndpoint.HandleAsync(
            new CreateCrewHttpRequest { Name = "  Kitchen Legends  " },
            workOsUserContext,
            dbContext,
            workOs,
            TimeProvider.System,
            CancellationToken.None
        );
        var response = Assert.IsType<CreateCrewHttpResponse>(created.Value);

        Assert.Equal("Kitchen Legends", response.Name);
        Assert.Equal(response.CrewId.ToString(), workOs.OrganizationExternalId);
        Assert.Equal(response.Name, workOs.OrganizationName);
        Assert.Equal(userId, workOs.MembershipUserId);
        Assert.Equal(workOs.OrganizationId, workOs.MembershipOrganizationId);
        Assert.True(workOs.AdminMembershipCreated);

        var storedCrew = await dbContext.Crews
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Uuid == response.CrewId,
                CancellationToken.None
            );
        var storedAttempt = await dbContext.CrewProvisioningAttempts
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.CrewUuid == response.CrewId,
                CancellationToken.None
            );

        Assert.NotEqual(0, storedCrew.Id);
        Assert.Equal(response.CrewId, storedCrew.Uuid);
        Assert.Equal("Kitchen Legends", storedCrew.Name);
        Assert.Equal(workOs.OrganizationId, storedCrew.WorkOsOrganizationId);
        Assert.Equal(userId, storedAttempt.WorkOsUserId);
        Assert.Equal(workOs.MembershipId, storedAttempt.WorkOsMembershipId);
        Assert.NotNull(storedAttempt.CompletedAtUtc);
    }

    private sealed class FakeWorkOsProvisioningService : IWorkOsProvisioningService
    {
        public string OrganizationId { get; } = $"org_{Guid.NewGuid():N}";

        public string MembershipId { get; } = $"om_{Guid.NewGuid():N}";

        public string? OrganizationExternalId { get; private set; }

        public string? OrganizationName { get; private set; }

        public string? MembershipUserId { get; private set; }

        public string? MembershipOrganizationId { get; private set; }

        public bool AdminMembershipCreated { get; private set; }

        public Task<string?> FindOrganizationByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken) => Task.FromResult<string?>(null);

        public Task<string> CreateOrganizationAsync(
            string name,
            string externalId,
            CancellationToken cancellationToken)
        {
            OrganizationName = name;
            OrganizationExternalId = externalId;
            return Task.FromResult(OrganizationId);
        }

        public Task<string?> FindActiveMembershipIdAsync(
            string userId,
            string organizationId,
            CancellationToken cancellationToken) => Task.FromResult<string?>(null);

        public Task<string> CreateAdminMembershipAsync(
            string userId,
            string organizationId,
            CancellationToken cancellationToken)
        {
            MembershipUserId = userId;
            MembershipOrganizationId = organizationId;
            AdminMembershipCreated = true;
            return Task.FromResult(MembershipId);
        }
    }
}
