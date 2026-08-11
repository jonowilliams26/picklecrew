using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.IntegrationTests.WorkOs;

public sealed class WorkOsTokenEndpointsTests
{
    [Fact]
    public void GetIssuer_WhenClientIdIsProvided_ShouldReturnClientSpecificIssuer()
    {
        const string clientId = "client_123";

        var issuer = WorkOsTokenEndpoints.GetIssuer(clientId);

        Assert.Equal(
            "https://api.workos.com/user_management/client_123",
            issuer
        );
    }

    [Fact]
    public void GetJwksAddress_WhenClientIdIsProvided_ShouldReturnClientSpecificAddress()
    {
        const string clientId = "client_123";

        var address = WorkOsTokenEndpoints.GetJwksAddress(clientId);

        Assert.Equal("https://api.workos.com/sso/jwks/client_123", address);
    }
}
