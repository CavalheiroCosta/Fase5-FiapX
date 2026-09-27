using System.Net;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class AuthHostTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthHostTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Host_responde_a_requisicao()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
