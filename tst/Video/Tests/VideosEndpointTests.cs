using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Video.Tests.Suporte;

namespace Video.Tests;

public class VideoHostTests : IClassFixture<VideoApiFactory>
{
    private readonly HttpClient _client;

    public VideoHostTests(VideoApiFactory factory)
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

public class VideosEndpointTests : IClassFixture<VideoApiFactory>
{
    private const string Chave = "fiapx-auth-chave-local-desenvolvimento";

    private readonly VideoApiFactory _factory;

    public VideosEndpointTests(VideoApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var client = _factory.CreateClient();
        using var pedido = Pedido(null, [1, 2], "aula.mp4");

        var resposta = await client.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("Acesso recusado.", await Erro(resposta));
    }

    [Fact]
    public async Task Token_invalido_responde_401()
    {
        var client = _factory.CreateClient();
        using var pedido = Pedido("nao-e-jwt", [1], "aula.mp4");

        var resposta = await client.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Arquivo_vazio_responde_400_e_nao_grava()
    {
        var client = _factory.CreateClient();
        var armazenamento = _factory.Services.GetRequiredService<Video.Infra.Storage.ArmazenamentoMemoria>();
        var antes = armazenamento.Contagem;
        using var pedido = Pedido(TokenValido(), [], "vazio.mp4");

        var resposta = await client.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(antes, armazenamento.Contagem);
    }

    [Fact]
    public async Task Envia_com_o_token_e_deixa_arquivo_e_mensagem()
    {
        var client = _factory.CreateClient();
        using var pedido = Pedido(TokenValido(), [1, 2, 3, 4], "aula.mp4");

        var resposta = await client.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();
        var caminho = json.RootElement.GetProperty("caminho").GetString();
        Assert.Equal("aguardando_processamento", json.RootElement.GetProperty("status").GetString());
        Assert.Equal($"videos/{id:D}/aula.mp4", caminho);

        var armazenamento = _factory.Services.GetRequiredService<Video.Infra.Storage.ArmazenamentoMemoria>();
        Assert.True(armazenamento.Contem(caminho!));
        Assert.Equal(4, armazenamento.Tamanho(caminho!));

        var fila = _factory.Services.GetRequiredService<Video.Infra.Filas.FilaProcessamentoMemoria>();
        Assert.Contains(fila.Mensagens, mensagem => mensagem.Id == id && mensagem.Caminho == caminho);

        var repositorio = _factory.Services.GetRequiredService<Video.Infra.Persistence.VideoRepositorioMemoria>();
        var video = Assert.Single(repositorio.Listar(), item => item.Id == id);
        Assert.Equal("ana", video.Login);
        Assert.Equal("ana@email.com", video.Email);
    }

    [Fact]
    public async Task Lista_sem_token_responde_401()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/videos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("Acesso recusado.", await Erro(resposta));
    }

    [Fact]
    public async Task Lista_so_os_videos_do_login_e_repete_do_redis()
    {
        var client = _factory.CreateClient();
        using var pedido = Pedido(TokenValido(), [1, 2], "aula.mp4");
        var enviado = await client.SendAsync(pedido);
        using var jsonEnvio = JsonDocument.Parse(await enviado.Content.ReadAsStringAsync());
        var id = jsonEnvio.RootElement.GetProperty("id").GetGuid();

        var lista = _factory.Services.GetRequiredService<Video.Infra.Redis.ListaVideosMemoria>();
        Assert.Null(await lista.ObterAsync("ana", CancellationToken.None));

        var primeira = await client.SendAsync(Listar(TokenValido()));
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        using var json = JsonDocument.Parse(await primeira.Content.ReadAsStringAsync());
        var item = json.RootElement.EnumerateArray().Single(elemento => elemento.GetProperty("id").GetGuid() == id);
        Assert.Equal("aguardando_processamento", item.GetProperty("status").GetString());
        Assert.False(item.TryGetProperty("caminhoZip", out _));

        var repositorio = _factory.Services.GetRequiredService<Video.Infra.Persistence.VideoRepositorioMemoria>();
        var video = Assert.Single(repositorio.Listar(), gravado => gravado.Id == id);
        Assert.Equal(Video.Domain.Videos.EfeitoStatus.Alterado, video.Aplicar(Video.Domain.Videos.MomentoStatus.Sucesso, $"videos/{id:D}/{id:D}.zip"));

        var segunda = await client.SendAsync(Listar(TokenValido()));
        using var jsonSegunda = JsonDocument.Parse(await segunda.Content.ReadAsStringAsync());
        var itemSegunda = jsonSegunda.RootElement.EnumerateArray().Single(elemento => elemento.GetProperty("id").GetGuid() == id);
        Assert.Equal("aguardando_processamento", itemSegunda.GetProperty("status").GetString());

        var deOutro = await client.SendAsync(Listar(TokenDe("bia", "bia@email.com")));
        Assert.Equal(HttpStatusCode.OK, deOutro.StatusCode);
        using var jsonOutro = JsonDocument.Parse(await deOutro.Content.ReadAsStringAsync());
        Assert.Empty(jsonOutro.RootElement.EnumerateArray());
    }

    private static HttpRequestMessage Pedido(string? token, byte[] bytes, string nome)
    {
        var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(bytes);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        conteudo.Add(arquivo, "arquivo", nome);
        var pedido = new HttpRequestMessage(HttpMethod.Post, "/videos") { Content = conteudo };
        if (token is not null)
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return pedido;
    }

    private static HttpRequestMessage Listar(string? token)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, "/videos");
        if (token is not null)
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return pedido;
    }

    private static string TokenValido() => TokenDe("ana", "ana@email.com");

    private static string TokenDe(string login, string email) =>
        TokenDeTeste.Emitir(Chave, login, email, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(30));

    private static async Task<string> Erro(HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("erro").GetString()!;
    }
}
