using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.EndpointFilters;
using PersistenceProbeEntity = PickleCrew.Api.Data.PersistenceProbe;

namespace PickleCrew.Api.PersistenceProbe;

public sealed record CreatePersistenceProbeHttpRequest
{
    public required string Value { get; init; }
}

public sealed record CreatePersistenceProbeHttpResponse
{
    public required long Id { get; init; }

    public required string Value { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}

internal sealed class CreatePersistenceProbeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/persistence-probes", HandleAsync)
            .WithName("CreatePersistenceProbe")
            .WithSummary("Creates a persistence probe.")
            .WithRequestValidation<CreatePersistenceProbeHttpRequest>(Validate);
    }

    private static Dictionary<string, string[]> Validate(
        CreatePersistenceProbeHttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.Value)] = ["Value must not be blank."],
            };
        }

        return [];
    }

    private static async Task<Created<CreatePersistenceProbeHttpResponse>> HandleAsync(
        CreatePersistenceProbeHttpRequest request,
        PickleCrewDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var probe = new PersistenceProbeEntity
        {
            Value = request.Value,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        };

        dbContext.PersistenceProbes.Add(probe);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreatePersistenceProbeHttpResponse
        {
            Id = probe.Id,
            Value = probe.Value,
            CreatedAtUtc = probe.CreatedAtUtc,
        };

        return TypedResults.Created($"/persistence-probes/{probe.Id}", response);
    }
}
