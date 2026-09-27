using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class UsuariosEndpointTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public UsuariosEndpointTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cadastro_nao_devolve_senha_e_recusa_duplicidade_e_remocao_do_adm()
    {
        var listaInicial = await _client.GetFromJsonAsync<List<UsuarioJson>>("/usuarios");
        var adm = Assert.Single(listaInicial!, item => item.Login == "Adm");
        Assert.Equal("Administrador", adm.Nome);
        Assert.Equal("adm@adm.com", adm.Email);
        Assert.False(JsonSerializer.Serialize(adm).Contains("senha", StringComparison.OrdinalIgnoreCase));

        var criado = await _client.PostAsJsonAsync("/usuarios", new
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

        var consulta = await _client.GetFromJsonAsync<UsuarioJson>($"/usuarios/{ana.Id}");
        Assert.Equal("ana", consulta!.Login);

        var loginRepetido = await _client.PostAsJsonAsync("/usuarios", new
        {
            login = "ana",
            senha = "outra",
            nome = "Outra",
            email = "outra@email.com"
        });
        Assert.Equal(HttpStatusCode.Conflict, loginRepetido.StatusCode);

        var emailRepetido = await _client.PostAsJsonAsync("/usuarios", new
        {
            login = "bia",
            senha = "senha-bia",
            nome = "Bia",
            email = "ana@email.com"
        });
        Assert.Equal(HttpStatusCode.Conflict, emailRepetido.StatusCode);

        var invalido = await _client.PostAsJsonAsync("/usuarios", new
        {
            login = "",
            senha = "senha",
            nome = "Ana",
            email = "ana@email.com"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var alterado = await _client.PutAsJsonAsync($"/usuarios/{ana.Id}", new
        {
            nome = "Ana Costa",
            email = "ana.costa@email.com",
            senha = "senha-nova"
        });
        Assert.Equal(HttpStatusCode.OK, alterado.StatusCode);
        var anaAlterada = await alterado.Content.ReadFromJsonAsync<UsuarioJson>();
        Assert.Equal("ana", anaAlterada!.Login);
        Assert.Equal("Ana Costa", anaAlterada.Nome);

        var inexistente = await _client.GetAsync($"/usuarios/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);

        var removeAdm = await _client.DeleteAsync($"/usuarios/{adm.Id}");
        Assert.Equal(HttpStatusCode.Conflict, removeAdm.StatusCode);

        var removeAna = await _client.DeleteAsync($"/usuarios/{ana.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeAna.StatusCode);

        var depois = await _client.GetAsync($"/usuarios/{ana.Id}");
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
    }

    private sealed record UsuarioJson(Guid Id, string Login, string Nome, string Email);
}
