using Microsoft.EntityFrameworkCore;

namespace PickleCrew.IntegrationTests.Testing;

internal static class CrewTestDataBuilderExtensions
{
    public static CrewTestDataBuilder CreateCrewBuilder(this AppHostFixture fixture)
    {
        return new CrewTestDataBuilder(fixture);
    }
}

internal sealed class CrewTestDataBuilder(AppHostFixture fixture)
{
    private readonly List<CrewMemberDefinition> memberDefinitions = [];
    private CrewMemberDefinition? administratorDefinition;
    private string crewName = "Kitchen Legends";

    public CrewTestDataBuilder WithName(string name)
    {
        crewName = name;

        return this;
    }

    public CrewTestDataBuilder WithAdministrator(
        string? id = null,
        string? emailAddress = null,
        string? displayName = null,
        string? profilePictureUrl = null
    )
    {
        var administratorId = id ?? $"user_test_{Guid.NewGuid():N}";
        var administratorEmailAddress = emailAddress ?? $"{administratorId}@example.test";
        administratorDefinition = new CrewMemberDefinition
        {
            Id = administratorId,
            EmailAddress = administratorEmailAddress,
            DisplayName = displayName ?? administratorEmailAddress,
            ProfilePictureUrl = profilePictureUrl,
            Role = FakeMembershipRole.Admin,
            Status = FakeMembershipStatus.Active,
        };

        return this;
    }

    public CrewTestDataBuilder WithMember(
        string? id = null,
        FakeMembershipRole role = FakeMembershipRole.Member,
        FakeMembershipStatus status = FakeMembershipStatus.Active,
        string? emailAddress = null,
        string? displayName = null,
        string? profilePictureUrl = null
    )
    {
        var memberId = id ?? $"user_test_{Guid.NewGuid():N}";
        var memberEmailAddress = emailAddress ?? $"{memberId}@example.test";
        memberDefinitions.Add(new CrewMemberDefinition
        {
            Id = memberId,
            EmailAddress = memberEmailAddress,
            DisplayName = displayName ?? memberEmailAddress,
            ProfilePictureUrl = profilePictureUrl,
            Role = role,
            Status = status,
        });

        return this;
    }

    public async Task<BuiltCrewTestData> BuildAsync(
        CancellationToken cancellationToken = default
    )
    {
        var administrator = administratorDefinition is null
            ? TestIdentity.CreateSubjectOnly()
            : new TestIdentity
            {
                UserId = administratorDefinition.Id,
                EmailAddress = administratorDefinition.EmailAddress,
            };
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();

        if (administratorDefinition is not null)
        {
            await fakeWorkOs.CreateUserAsync(
                administratorDefinition.Id,
                administratorDefinition.EmailAddress,
                displayName: administratorDefinition.DisplayName,
                profilePictureUrl: administratorDefinition.ProfilePictureUrl,
                cancellationToken: cancellationToken
            );
        }

        var crew = await fixture.CreateCrewAsync(administrator, crewName);
        administrator = administrator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        var members = new List<TestIdentity>(memberDefinitions.Count);

        foreach (var definition in memberDefinitions)
        {
            await fakeWorkOs.CreateUserAsync(
                definition.Id,
                definition.EmailAddress,
                displayName: definition.DisplayName,
                profilePictureUrl: definition.ProfilePictureUrl,
                cancellationToken: cancellationToken
            );
            await fakeWorkOs.CreateMembershipAsync(
                $"membership_test_{Guid.NewGuid():N}",
                definition.Id,
                crew.OrganizationId,
                definition.Role,
                definition.Status,
                cancellationToken
            );
            members.Add(new TestIdentity
            {
                UserId = definition.Id,
                EmailAddress = definition.EmailAddress,
                OrganizationId = crew.OrganizationId,
                Role = definition.Role.ToString().ToLowerInvariant(),
            });
        }

        await using var database = fixture.CreateDbContext();
        var databaseId = await database.Crews
            .Where(candidate => candidate.Uuid == crew.CrewId)
            .Select(candidate => candidate.Id)
            .SingleAsync(cancellationToken);

        return new BuiltCrewTestData
        {
            Id = databaseId,
            Uuid = crew.CrewId,
            OrganizationId = crew.OrganizationId,
            Administrator = administrator,
            Members = members.ToArray(),
        };
    }

    private sealed record CrewMemberDefinition
    {
        public required string Id { get; init; }

        public required string EmailAddress { get; init; }

        public required string DisplayName { get; init; }

        public string? ProfilePictureUrl { get; init; }

        public required FakeMembershipRole Role { get; init; }

        public required FakeMembershipStatus Status { get; init; }
    }
}

internal sealed record BuiltCrewTestData
{
    public required long Id { get; init; }

    public required Guid Uuid { get; init; }

    public required string OrganizationId { get; init; }

    public required TestIdentity Administrator { get; init; }

    public required TestIdentity[] Members { get; init; }
}
