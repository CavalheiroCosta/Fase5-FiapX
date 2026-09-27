using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class UsuariosEndpointTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public UsuariosEndpointTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Cadastro_nao_devolve_senha_e_recusa_duplicidade_e_remocao_do_adm()
    {
        var client = _factory.CreateClient();
        await AutenticarAdmAsync(client);

        var listaInicial = await client.GetFromJsonAsync<List<UsuarioJson>>("/usuarios");
        var adm = Assert.Single(listaInicial!, item => item.Login == "Adm");
        Assert.Equal("Administrador", adm.Nome);
        Assert.Equal("adm@adm.com", adm.Email);
        Assert.False(JsonSerializer.Serialize(adm).Contains("senha", StringComparison.OrdinalIgnoreCase));

        var criado = await client.PostAsJsonAsync("/usuarios", new
        {
            login = "ana",
            senha = "senha-ana",
            nome = "Ana",
            email = "ana@email.com"
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var ana = await criado.Content.ReadFromJsonAsync<UsuarioJson>();
        Assert.NotEqual(Guid.Empty, ana!.Id);
        Assert.False(JsonSerializer.Serialize(ana).Contains("senha", StringComparison.OrdinalIgnoreCase));

        var consulta = await client.GetFromJsonAsync<UsuarioJson>($"/usuarios/{ana.Id}");
        Assert.Equal("ana", consulta!.Login);

        var loginRepetido = await client.PostAsJsonAsync("/usuarios", new
        {
            login = "ana",
            senha = "outra",
            nome = "Outra",
            email = "outra@email.com"
        });
        Assert.Equal(HttpStatusCode.Conflict, loginRepetido.StatusCode);

        var emailRepetido = await client.PostAsJsonAsync("/usuarios", new
        {
            login = "bia",
            senha = "senha-bia",
            nome = "Bia",
            email = "ana@email.com"
        });
        Assert.Equal(HttpStatusCode.Conflict, emailRepetido.StatusCode);

        var invalido = await client.PostAsJsonAsync("/usuarios", new
        {
            login = "",
            senha = "senha",
            nome = "Ana",
            email = "ana@email.com"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var alterado = await client.PutAsJsonAsync($"/usuarios/{ana.Id}", new
        {
            nome = "Ana Costa",
            email = "ana.costa@email.com",
            senha = "senha-nova"
        });
        Assert.Equal(HttpStatusCode.OK, alterado.StatusCode);
        var anaAlterada = await alterado.Content.ReadFromJsonAsync<UsuarioJson>();
        Assert.Equal("ana", anaAlterada!.Login);
        Assert.Equal("Ana Costa", anaAlterada.Nome);

        var inexistente = await client.GetAsync($"/usuarios/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);

        var removeAdm = await client.DeleteAsync($"/usuarios/{adm.Id}");
        Assert.Equal(HttpStatusCode.Conflict, removeAdm.StatusCode);

        var removeAna = await client.DeleteAsync($"/usuarios/{ana.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeAna.StatusCode);

        var depois = await client.GetAsync($"/usuarios/{ana.Id}");
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
    }

    [Fact]
    public async Task Cadastro_sem_token_ou_com_token_invalido_responde_401()
    {
        var semToken = _factory.CreateClient();
        var invalido = _factory.CreateClient();
        invalido.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "nao-e-jwt");

        await RecusaCadastroAsync(semToken);
        await RecusaCadastroAsync(invalido);
    }

    [Fact]
    public async Task Cadastro_com_token_de_outro_usuario_nao_acontece()
    {
        var adm = _factory.CreateClient();
        await AutenticarAdmAsync(adm);
        var criado = await adm.PostAsJsonAsync("/usuarios", new
        {
            login = "lia",
            senha = "senha-lia",
            nome = "Lia",
            email = "lia@email.com"
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var lia = await criado.Content.ReadFromJsonAsync<UsuarioJson>();

        var tokenLia = await TokenAsync(_factory.CreateClient(), "lia", "senha-lia");
        var outro = _factory.CreateClient();
        outro.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenLia);

        var antes = await adm.GetFromJsonAsync<List<UsuarioJson>>("/usuarios");
        await RecusaCadastroAsync(outro, lia!.Id);
        var depois = await adm.GetFromJsonAsync<List<UsuarioJson>>("/usuarios");
        Assert.Equal(antes!.Count, depois!.Count);
    }

    private async Task AutenticarAdmAsync(HttpClient client)
    {
        var token = await TokenAsync(client, "Adm", "Adm");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> TokenAsync(HttpClient client, string login, string senha)
    {
        var resposta = await client.PostAsJsonAsync("/login", new { login, senha });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<TokenJson>();
        return corpo!.Token;
    }

    private static async Task RecusaCadastroAsync(HttpClient client, Guid? id = null)
    {
        var alvo = id ?? Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/usuarios/{alvo}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/usuarios", new
        {
            login = "teo",
            senha = "senha-teo",
            nome = "Teo",
            email = "teo@email.com"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"/usuarios/{alvo}", new
        {
            nome = "Teo",
            email = "teo@email.com",
            senha = "senha-teo"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/usuarios/{alvo}")).StatusCode);
    }

    private sealed record UsuarioJson(Guid Id, string Login, string Nome, string Email);

    private sealed record TokenJson(string Token);
}
