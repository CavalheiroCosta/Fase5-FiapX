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

    private static string TokenValido() =>
        TokenDeTeste.Emitir(Chave, "ana", "ana@email.com", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(30));

    private static async Task<string> Erro(HttpResponseMessage resposta)
    {
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("erro").GetString()!;
    }
}
