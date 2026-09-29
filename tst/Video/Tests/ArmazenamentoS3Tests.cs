using Video.Domain.Videos;
using Video.Infra.Storage;

namespace Video.Tests;

public class ArmazenamentoS3Tests
{
    [Fact]
    public async Task Grava_no_bucket_e_devolve_o_caminho()
    {
        var cliente = new ClienteObjetoFalso();
        var armazenamento = new ArmazenamentoS3(cliente, CaminhoVideo.BucketPadrao);
        var id = Guid.NewGuid();

        var caminho = await armazenamento.SalvarAsync(id, "aula.mp4", new MemoryStream([1, 2]), CancellationToken.None);

        Assert.Equal($"videos/{id:D}/aula.mp4", caminho);
        Assert.Equal((CaminhoVideo.BucketPadrao, $"{id:D}/aula.mp4"), cliente.Gravados.Single());
    }

    [Fact]
    public async Task Remove_pela_chave_e_prepara_o_bucket()
    {
        var cliente = new ClienteObjetoFalso();
        var armazenamento = new ArmazenamentoS3(cliente, CaminhoVideo.BucketPadrao);
        var caminho = CaminhoVideo.Montar(CaminhoVideo.BucketPadrao, Guid.NewGuid(), "aula.mp4");

        await armazenamento.RemoverAsync(caminho, CancellationToken.None);
        await armazenamento.PrepararAsync(CancellationToken.None);

        Assert.Equal(CaminhoVideo.Chave(CaminhoVideo.BucketPadrao, caminho), cliente.Apagados.Single().Chave);
        Assert.Equal(1, cliente.Buckets);
    }

    [Fact]
    public async Task Abre_o_zip_pela_chave()
    {
        var cliente = new ClienteObjetoFalso { Conteudo = new MemoryStream([9, 8]) };
        var armazenamento = new ArmazenamentoS3(cliente, CaminhoVideo.BucketPadrao);
        var id = Guid.NewGuid();
        var caminho = $"videos/{id:D}/{id:D}.zip";

        await using var fluxo = await armazenamento.AbrirAsync(caminho, CancellationToken.None);

        Assert.Equal((CaminhoVideo.BucketPadrao, $"{id:D}/{id:D}.zip"), cliente.Lidos.Single());
        Assert.Equal(9, fluxo.ReadByte());
    }

    [Fact]
    public async Task Memoria_guarda_e_apaga_o_arquivo()
    {
        var armazenamento = new ArmazenamentoMemoria(CaminhoVideo.BucketPadrao);
        var id = Guid.NewGuid();

        var caminho = await armazenamento.SalvarAsync(id, "aula.mp4", new MemoryStream([1, 2, 3]), CancellationToken.None);

        Assert.True(armazenamento.Contem(caminho));
        Assert.Equal(3, armazenamento.Tamanho(caminho));
        Assert.Equal(1, armazenamento.Contagem);
        Assert.Equal(0, armazenamento.Tamanho("videos/ausente"));

        await using var aberto = await armazenamento.AbrirAsync(caminho, CancellationToken.None);
        Assert.Equal(1, aberto.ReadByte());

        await armazenamento.RemoverAsync(caminho, CancellationToken.None);

        Assert.False(armazenamento.Contem(caminho));
        Assert.Equal(0, armazenamento.Contagem);
        await Assert.ThrowsAsync<IOException>(() => armazenamento.AbrirAsync(caminho, CancellationToken.None));
    }

    private sealed class ClienteObjetoFalso : IClienteObjeto
    {
        public List<(string Bucket, string Chave)> Gravados { get; } = [];
        public List<(string Bucket, string Chave)> Lidos { get; } = [];
        public List<(string Bucket, string Chave)> Apagados { get; } = [];
        public int Buckets { get; private set; }
        public Stream Conteudo { get; set; } = new MemoryStream();

        public Task GarantirBucketAsync(string bucket, CancellationToken cancellationToken)
        {
            Buckets++;
            return Task.CompletedTask;
        }

        public Task GravarAsync(string bucket, string chave, Stream conteudo, CancellationToken cancellationToken)
        {
            Gravados.Add((bucket, chave));
            return Task.CompletedTask;
        }

        public Task<Stream> LerAsync(string bucket, string chave, CancellationToken cancellationToken)
        {
            Lidos.Add((bucket, chave));
            return Task.FromResult(Conteudo);
        }

        public Task ApagarAsync(string bucket, string chave, CancellationToken cancellationToken)
        {
            Apagados.Add((bucket, chave));
            return Task.CompletedTask;
        }
    }
}
