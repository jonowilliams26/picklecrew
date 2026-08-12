using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace PickleCrew.Api.WorkOs;

internal sealed class WorkOsJwksConfigurationRetriever(string issuer)
    : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address,
        IDocumentRetriever retriever,
        CancellationToken cancellationToken)
    {
        var document = await retriever.GetDocumentAsync(address, cancellationToken);
        var webKeySet = new JsonWebKeySet(document);
        var configuration = new OpenIdConnectConfiguration
        {
            Issuer = issuer,
            JsonWebKeySet = webKeySet,
        };

        foreach (var signingKey in webKeySet.GetSigningKeys())
        {
            configuration.SigningKeys.Add(signingKey);
        }

        return configuration;
    }
}
