using System.Text;
using System.Text.Json;
using Video.Infra.Filas;

namespace Video.Tests;

public class FilaProcessamentoTests
{
    [Fact]
    public async Task Publica_id_e_caminho_e_declara_a_fila()
    {
        var publicador = new PublicadorFalso();
        var fila = new FilaProcessamento(publicador, "processamento");
        var id = Guid.NewGuid();

        await fila.PublicarAsync(id, "videos/x/aula.mp4", CancellationToken.None);
        await fila.PrepararAsync(CancellationToken.None);

        Assert.Equal("processamento", publicador.Declaradas.Single());
        Assert.Equal("processamento", publicador.Publicadas.Single().Fila);
        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(publicador.Publicadas.Single().Corpo));
        Assert.Equal(id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("videos/x/aula.mp4", json.RootElement.GetProperty("caminho").GetString());
    }

    [Fact]
    public async Task Memoria_guarda_a_mensagem()
    {
        var fila = new FilaProcessamentoMemoria();
        var id = Guid.NewGuid();

        await fila.PublicarAsync(id, "videos/x/aula.mp4", CancellationToken.None);

        var mensagem = Assert.Single(fila.Mensagens);
        Assert.Equal(id, mensagem.Id);
        Assert.Equal("videos/x/aula.mp4", mensagem.Caminho);
    }

    private sealed class PublicadorFalso : IPublicadorFila
    {
        public List<string> Declaradas { get; } = [];
        public List<(string Fila, byte[] Corpo)> Publicadas { get; } = [];

        public Task DeclararAsync(string fila, CancellationToken cancellationToken)
        {
            Declaradas.Add(fila);
            return Task.CompletedTask;
        }

        public Task PublicarAsync(string fila, ReadOnlyMemory<byte> corpo, CancellationToken cancellationToken)
        {
            Publicadas.Add((fila, corpo.ToArray()));
            return Task.CompletedTask;
        }
    }
}
