using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Processor.Domain.Processamento;
using Processor.Infra.Storage;
using Processor.Tests.Suporte;

namespace Processor.Tests;

public class ProcessorHostTests : IClassFixture<ProcessorApiFactory>
{
    private readonly ProcessorApiFactory _factory;
    private readonly HttpClient _client;

    public ProcessorHostTests(ProcessorApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public void Host_de_teste_fica_em_memoria()
    {
        Assert.IsType<ArmazenamentoProcessamentoMemoria>(
            _factory.Services.GetRequiredService<IArmazenamentoProcessamento>());
    }

    [Fact]
    public async Task Host_responde_a_requisicao()
    {
        var resposta = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Metrics_responde_sem_token()
    {
        await _client.GetAsync("/");

        var resposta = await _client.GetAsync("/metrics");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("http_server_request_duration", corpo);
        Assert.Contains("fiapx_processor_em_andamento", corpo);
    }
}
