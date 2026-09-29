using System.Net;
using Processor.Tests.Suporte;

namespace Processor.Tests;

public class ProcessorHostTests : IClassFixture<ProcessorApiFactory>
{
    private readonly HttpClient _client;

    public ProcessorHostTests(ProcessorApiFactory factory)
    {
        _client = factory.CreateClient();
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
