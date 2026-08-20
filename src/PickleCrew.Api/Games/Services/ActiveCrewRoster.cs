using PickleCrew.Api.WorkOs;
using WorkOS;

namespace PickleCrew.Api.Games.Services;

internal static class ActiveCrewRoster
{
    public static async Task<ActiveCrewMember[]?> TryListAsync(
        IWorkOsService workOs,
        string organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await workOs.ListActiveCrewMembersAsync(
                organizationId,
                cancellationToken
            );
        }
        catch (ApiException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
