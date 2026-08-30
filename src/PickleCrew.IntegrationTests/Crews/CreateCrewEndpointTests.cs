using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Crews;
using PickleCrew.Api.Testing;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class CreateCrewEndpointTests(AppHostFixture fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task CreateCrew_WhenUserHeaderIsMissingOrBlank_ShouldReturnUnauthorized(
        string? userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/crews")
        {
            Content = JsonContent.Create(new CreateCrewHttpRequest
            {
                RequestId = Guid.NewGuid(),
                Name = "Kitchen Legends",
            }),
        };

        if (userId is not null)
        {
            request.Headers.TryAddWithoutValidation(
                TestAuthenticationDefaults.UserIdHeaderName,
                userId
            );
        }

        using var response = await fixture.ApiClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCrew_WhenAuthenticatedWithSubjectOnly_ShouldProvisionAndPersistCrew()
    {
        var identity = TestIdentity.CreateSubjectOnly();
        using var apiClient = fixture.CreateApiClient(identity);
        var requestId = Guid.NewGuid();

        using var response = await apiClient.PostAsJsonAsync(
            "/crews",
            new CreateCrewHttpRequest
            {
                RequestId = requestId,
                Name = "  Kitchen Legends  ",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdCrew = await response.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        Assert.NotNull(createdCrew);
        Assert.Equal("Kitchen Legends", createdCrew.Name);
        Assert.Equal($"/crews/{createdCrew.CrewId}", response.Headers.Location?.ToString());

        await using var dbContext = fixture.CreateDbContext();
        var storedCrew = await dbContext.Crews
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Uuid == createdCrew.CrewId);
        var storedAttempt = await dbContext.CrewProvisioningAttempts
            .AsNoTracking()
            .SingleAsync(candidate => candidate.CrewUuid == createdCrew.CrewId);

        Assert.NotEqual(0, storedCrew.Id);
        Assert.Equal(createdCrew.CrewId, storedCrew.Uuid);
        Assert.Equal(createdCrew.Name, storedCrew.Name);
        Assert.Equal(createdCrew.OrganizationId, storedCrew.WorkOsOrganizationId);
        Assert.Equal(requestId, storedAttempt.RequestId);
        Assert.Equal(identity.UserId, storedAttempt.WorkOsUserId);
        Assert.Equal(createdCrew.OrganizationId, storedAttempt.WorkOsOrganizationId);
        Assert.False(string.IsNullOrWhiteSpace(storedAttempt.WorkOsMembershipId));
        Assert.NotNull(storedAttempt.CompletedAtUtc);
    }

    [Fact]
    public async Task CreateCrew_WhenWorkOsOperationFails_ShouldResumeOnRetry()
    {
        var identity = TestIdentity.CreateSubjectOnly();
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        using var failingApiClient = fixture.CreateApiClient(identity, scenarioId);
        using var retryingApiClient = fixture.CreateApiClient(identity);

        await fakeWorkOs.FailOperationAsync(
            scenarioId,
            FakeWorkOsOperation.CreateOrganization
        );

        var request = new CreateCrewHttpRequest
        {
            RequestId = Guid.NewGuid(),
            Name = "Kitchen Legends",
        };
        using var failedResponse = await failingApiClient.PostAsJsonAsync(
            "/crews",
            request
        );
        using var retriedResponse = await retryingApiClient.PostAsJsonAsync(
            "/crews",
            request
        );

        Assert.Equal(HttpStatusCode.InternalServerError, failedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retriedResponse.StatusCode);

        await using var dbContext = fixture.CreateDbContext();
        var attempt = await dbContext.CrewProvisioningAttempts
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.WorkOsUserId == identity.UserId &&
                candidate.RequestId == request.RequestId
            );

        Assert.NotNull(attempt.WorkOsOrganizationId);
        Assert.NotNull(attempt.WorkOsMembershipId);
        Assert.NotNull(attempt.CompletedAtUtc);
    }

    [Fact]
    public async Task CreateCrew_WhenUserAlreadyCreatedCrew_ShouldProvisionDistinctAdditionalCrew()
    {
        var identity = TestIdentity.CreateSubjectOnly();
        using var apiClient = fixture.CreateApiClient(identity);
        var firstRequest = new CreateCrewHttpRequest
        {
            RequestId = Guid.NewGuid(),
            Name = "Kitchen Legends",
        };
        var secondRequest = new CreateCrewHttpRequest
        {
            RequestId = Guid.NewGuid(),
            Name = "Kitchen Legends",
        };

        using var firstResponse = await apiClient.PostAsJsonAsync("/crews", firstRequest);
        using var secondResponse = await apiClient.PostAsJsonAsync("/crews", secondRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var firstCrew = await firstResponse.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        var secondCrew = await secondResponse.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        Assert.NotNull(firstCrew);
        Assert.NotNull(secondCrew);
        Assert.NotEqual(firstCrew.CrewId, secondCrew.CrewId);
        Assert.NotEqual(firstCrew.OrganizationId, secondCrew.OrganizationId);
        Assert.Equal(firstCrew.Name, secondCrew.Name);

        await using var database = fixture.CreateDbContext();
        var attempts = await database.CrewProvisioningAttempts
            .AsNoTracking()
            .Where(attempt => attempt.WorkOsUserId == identity.UserId)
            .ToListAsync();

        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, attempt => Assert.NotNull(attempt.CompletedAtUtc));

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        Assert.NotNull(await fakeWorkOs.FindActiveMembershipAsync(
            identity.UserId,
            firstCrew.OrganizationId
        ));
        Assert.NotNull(await fakeWorkOs.FindActiveMembershipAsync(
            identity.UserId,
            secondCrew.OrganizationId
        ));
    }

    [Fact]
    public async Task CreateCrew_WhenCompletedRequestIsRetried_ShouldReturnOriginalCrew()
    {
        var identity = TestIdentity.CreateSubjectOnly();
        using var apiClient = fixture.CreateApiClient(identity);
        var request = new CreateCrewHttpRequest
        {
            RequestId = Guid.NewGuid(),
            Name = "Kitchen Legends",
        };

        using var firstResponse = await apiClient.PostAsJsonAsync("/crews", request);
        using var retryResponse = await apiClient.PostAsJsonAsync("/crews", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);

        var firstCrew = await firstResponse.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        var retriedCrew = await retryResponse.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        Assert.NotNull(firstCrew);
        Assert.NotNull(retriedCrew);
        Assert.Equal(firstCrew, retriedCrew);
    }

    [Fact]
    public async Task CreateCrew_WhenScenariosRunConcurrently_ShouldKeepStateIsolated()
    {
        var identities = Enumerable.Range(0, 4)
            .Select(_ => TestIdentity.CreateSubjectOnly())
            .ToArray();

        var scenarios = await Task.WhenAll(
            identities.Select(CreateCrewAsync)
        );

        Assert.Equal(scenarios.Length, scenarios.Select(
            scenario => scenario.Response.CrewId
        ).Distinct().Count());
        Assert.Equal(scenarios.Length, scenarios.Select(
            scenario => scenario.Response.OrganizationId
        ).Distinct().Count());

        var userIds = identities.Select(identity => identity.UserId).ToArray();
        var crewIds = scenarios.Select(scenario => scenario.Response.CrewId).ToArray();

        await using var dbContext = fixture.CreateDbContext();
        var storedCrews = await dbContext.Crews
            .AsNoTracking()
            .Where(crew => crewIds.Contains(crew.Uuid))
            .ToListAsync();
        var storedAttempts = await dbContext.CrewProvisioningAttempts
            .AsNoTracking()
            .Where(attempt => userIds.Contains(attempt.WorkOsUserId))
            .ToListAsync();

        Assert.Equal(scenarios.Length, storedCrews.Count);
        Assert.Equal(scenarios.Length, storedAttempts.Count);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        foreach (var scenario in scenarios)
        {
            var storedCrew = Assert.Single(
                storedCrews,
                crew => crew.Uuid == scenario.Response.CrewId
            );
            var storedAttempt = Assert.Single(
                storedAttempts,
                attempt => attempt.WorkOsUserId == scenario.Identity.UserId
            );
            var fakeOrganizationId = await fakeWorkOs.FindOrganizationByExternalIdAsync(
                scenario.Response.CrewId.ToString()
            );
            var fakeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
                scenario.Identity.UserId,
                scenario.Response.OrganizationId
            );

            Assert.Equal(scenario.Response.OrganizationId, storedCrew.WorkOsOrganizationId);
            Assert.Equal(scenario.Response.OrganizationId, storedAttempt.WorkOsOrganizationId);
            Assert.Equal(scenario.Response.OrganizationId, fakeOrganizationId);
            Assert.Equal(storedAttempt.WorkOsMembershipId, fakeMembershipId);
        }
    }

    private async Task<CreateCrewScenario> CreateCrewAsync(TestIdentity identity)
    {
        using var client = fixture.CreateApiClient(identity);
        using var response = await client.PostAsJsonAsync(
            "/crews",
            new CreateCrewHttpRequest
            {
                RequestId = Guid.NewGuid(),
                Name = $"Crew {identity.UserId}",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdCrew = await response.Content.ReadFromJsonAsync<CreateCrewHttpResponse>();
        Assert.NotNull(createdCrew);

        return new CreateCrewScenario
        {
            Identity = identity,
            Response = createdCrew,
        };
    }

    private sealed record CreateCrewScenario
    {
        public required TestIdentity Identity { get; init; }

        public required CreateCrewHttpResponse Response { get; init; }
    }
}
