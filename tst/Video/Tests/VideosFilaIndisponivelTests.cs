using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Video.Domain.Videos;
using Video.Infra.Storage;
using Video.Tests.Suporte;

namespace Video.Tests;

public class VideosFilaIndisponivelTests : IClassFixture<VideoApiFactoryFilaIndisponivel>
{
    private readonly VideoApiFactoryFilaIndisponivel _factory;

    public VideosFilaIndisponivelTests(VideoApiFactoryFilaIndisponivel factory) => _factory = factory;

    [Fact]
    public async Task Falha_da_fila_responde_503_e_mantem_o_arquivo()
    {
        var client = _factory.CreateClient();
        using var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent([9, 8, 7]);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        conteudo.Add(arquivo, "arquivo", "preso.mp4");
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "/videos") { Content = conteudo };
        pedido.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TokenDeTeste.Emitir(
                "fiapx-auth-chave-local-desenvolvimento",
                "ana",
                "ana@email.com",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddMinutes(30)));

        var resposta = await client.SendAsync(pedido);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var erro = json.RootElement.GetProperty("erro").GetString();
        Assert.Contains("aguardando processamento", erro, StringComparison.OrdinalIgnoreCase);

        var armazenamento = _factory.Services.GetRequiredService<ArmazenamentoMemoria>();
        Assert.True(armazenamento.Contagem >= 1);
    }
}

public sealed class VideoApiFactoryFilaIndisponivel : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicos =>
        {
            VideoApiFactory.SubstituirInfraestrutura(servicos);
            var alvos = servicos.Where(descritor => descritor.ServiceType == typeof(IFilaProcessamento)).ToList();
            foreach (var descritor in alvos)
                servicos.Remove(descritor);

            servicos.AddSingleton<IFilaProcessamento, FilaQueFalha>();
        });
    }

    private sealed class FilaQueFalha : IFilaProcessamento
    {
        public Task PublicarAsync(Guid id, string caminho, CancellationToken cancellationToken) =>
            throw new IOException("fila");
    }
}
