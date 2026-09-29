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

    [Fact]
    public async Task Metrics_responde_sem_token()
    {
        await _client.GetAsync("/");

        var resposta = await _client.GetAsync("/metrics");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("http_server_request_duration", corpo);
    }
}
