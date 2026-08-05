using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PickleCrew.IntegrationTests.PersistenceProbe;

public sealed class CreatePersistenceProbeTests(AppHostFixture fixture)
    : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task CreatePersistenceProbe_WhenValueIsBlank_ShouldReturnValidationProblem()
    {
        var request = new CreatePersistenceProbeHttpRequest
        {
            Value = " ",
        };

        using var httpResponse = await fixture.ApiClient.PostAsJsonAsync(
            "/persistence-probes",
            request
        );

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);

        var response =
            await httpResponse.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(response);
        Assert.True(response.Errors.ContainsKey("value"));
    }

    [Fact]
    public async Task CreatePersistenceProbe_WhenRequestIsValid_ShouldPersistProbeInDatabase()
    {
        var request = new CreatePersistenceProbeHttpRequest
        {
            Value = "PostgreSQL is working",
        };

        using var httpResponse = await fixture.ApiClient.PostAsJsonAsync(
            "/persistence-probes",
            request
        );

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

        var response =
            await httpResponse.Content.ReadFromJsonAsync<CreatePersistenceProbeHttpResponse>();

        Assert.NotNull(response);
        Assert.True(response.Id > 0);
        Assert.Equal(request.Value, response.Value);

        await using var dbContext = fixture.CreateDbContext();
        var storedProbe = await dbContext.PersistenceProbes
            .AsNoTracking()
            .SingleAsync(probe => probe.Id == response.Id);

        Assert.Equal(response.Value, storedProbe.Value);
        Assert.Equal(
            response.CreatedAtUtc,
            storedProbe.CreatedAtUtc,
            TimeSpan.FromMicroseconds(1)
        );
    }
}

internal sealed record CreatePersistenceProbeHttpRequest
{
    public required string Value { get; init; }
}

internal sealed record CreatePersistenceProbeHttpResponse
{
    public required long Id { get; init; }

    public required string Value { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
