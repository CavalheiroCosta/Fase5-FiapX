using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Auth.Tests;

public class AuthHostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthHostTests(WebApplicationFactory<Program> factory)
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
