using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class LoginEndpointTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public LoginEndpointTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_certo_devolve_token_sem_senha()
    {
        var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/login", new { login = "Adm", senha = "Adm" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var propriedade = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("token", propriedade.Name);
        Assert.False(string.IsNullOrWhiteSpace(propriedade.Value.GetString()));
    }

    [Fact]
    public async Task Senha_errada_e_usuario_inexistente_respondem_igual_e_sem_token()
    {
        var client = _factory.CreateClient();
        var senhaErrada = await client.PostAsJsonAsync("/login", new { login = "Adm", senha = "errada" });
        var inexistente = await client.PostAsJsonAsync("/login", new { login = "ninguem", senha = "Adm" });

        Assert.Equal(HttpStatusCode.Unauthorized, senhaErrada.StatusCode);
        Assert.Equal(senhaErrada.StatusCode, inexistente.StatusCode);
        var corpoErrado = await senhaErrada.Content.ReadAsStringAsync();
        var corpoInexistente = await inexistente.Content.ReadAsStringAsync();
        Assert.Equal(corpoErrado, corpoInexistente);
        using var json = JsonDocument.Parse(corpoErrado);
        var propriedade = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("erro", propriedade.Name);
        Assert.DoesNotContain("token", propriedade.Value.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_em_branco_responde_400()
    {
        var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/login", new { login = "", senha = "" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
