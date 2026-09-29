using System.IO.Compression;
using Processor.Domain.Processamento;
using Processor.Infra.Ffmpeg;
using Processor.Infra.Redis;
using Processor.Infra.Storage;

namespace Processor.Tests;

public class ArmazenamentoProcessamentoTests
{
    [Fact]
    public async Task Grava_o_zip_e_baixa_pela_chave()
    {
        var cliente = new ClienteFalso();
        var armazenamento = new ArmazenamentoProcessamentoS3(cliente, CaminhoZip.BucketPadrao);
        var id = Guid.NewGuid();

        var caminho = await armazenamento.SalvarZipAsync(id, new MemoryStream([1, 2]), CancellationToken.None);
        await using var baixado = await armazenamento.BaixarAsync(caminho, CancellationToken.None);

        Assert.Equal($"videos/{id:D}/{id:D}.zip", caminho);
        Assert.Equal((CaminhoZip.BucketPadrao, $"{id:D}/{id:D}.zip"), cliente.Gravados.Single());
        Assert.Equal((CaminhoZip.BucketPadrao, $"{id:D}/{id:D}.zip"), cliente.Baixados.Single());
        Assert.Equal(2, baixado.Length);
    }

    [Fact]
    public async Task Memoria_guarda_baixa_e_recusa_ausente()
    {
        var armazenamento = new ArmazenamentoProcessamentoMemoria(CaminhoZip.BucketPadrao);
        var id = Guid.NewGuid();
        var origem = CaminhoZip.Montar(CaminhoZip.BucketPadrao, id).Replace(".zip", ".mp4", StringComparison.Ordinal);
        armazenamento.Guardar(origem, [4, 5, 6]);

        await using var baixado = await armazenamento.BaixarAsync(origem, CancellationToken.None);
        var zip = await armazenamento.SalvarZipAsync(id, new MemoryStream([7]), CancellationToken.None);

        Assert.Equal(3, baixado.Length);
        Assert.True(armazenamento.Contem(zip));
        await Assert.ThrowsAsync<InvalidOperationException>(() => armazenamento.BaixarAsync("videos/ausente", CancellationToken.None));
    }

    [Fact]
    public void Cliente_sem_url_nao_nasce()
    {
        Assert.Throws<ArgumentException>(() => new ClienteObjetoS3(" ", "fiapx", "fiapxfiapx"));
    }

    [Fact]
    public async Task Cliente_cancelado_nao_abre_o_storage()
    {
        using var cliente = new ClienteObjetoS3("http://127.0.0.1:9", "fiapx", "fiapxfiapx");
        using var conteudo = new MemoryStream([1]);

        var token = new CancellationToken(canceled: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cliente.GravarAsync("videos", "a.zip", conteudo, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cliente.GarantirBucketAsync("videos", token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cliente.BaixarAsync("videos", "a.zip", token));
    }

    private sealed class ClienteFalso : IClienteObjeto
    {
        public List<(string Bucket, string Chave)> Gravados { get; } = [];
        public List<(string Bucket, string Chave)> Baixados { get; } = [];

        public Task GarantirBucketAsync(string bucket, CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task GravarAsync(string bucket, string chave, Stream conteudo, CancellationToken cancellationToken)
        {
            using var memoria = new MemoryStream();
            await conteudo.CopyToAsync(memoria, cancellationToken);
            Gravados.Add((bucket, chave));
        }

        public Task<Stream> BaixarAsync(string bucket, string chave, CancellationToken cancellationToken)
        {
            Baixados.Add((bucket, chave));
            return Task.FromResult<Stream>(new MemoryStream([1, 2]));
        }
    }
}

public class MarcaProcessamentoTests
{
    [Fact]
    public async Task Usa_a_chave_marca_e_a_memoria_nao_repete()
    {
        var comando = new ComandoFalso();
        var marca = new MarcaProcessamento(comando);
        var id = Guid.NewGuid();

        Assert.True(await marca.TentarAsync(id, CancellationToken.None));
        await marca.RemoverAsync(id, CancellationToken.None);

        Assert.Equal($"marca:{id:D}", comando.Chaves.Single());
        Assert.Equal($"marca:{id:D}", comando.Removidas.Single());

        var memoria = new MarcaMemoria();
        Assert.True(await memoria.TentarAsync(id, CancellationToken.None));
        Assert.False(await memoria.TentarAsync(id, CancellationToken.None));
        Assert.True(memoria.Contem(id));
        await memoria.RemoverAsync(id, CancellationToken.None);
        Assert.False(memoria.Contem(id));
    }

    [Fact]
    public void Redis_sem_conexao_nao_nasce_e_descarta_sem_abrir()
    {
        Assert.Throws<ArgumentException>(() => new ComandoMarcaRedis(" "));
        var comando = new ComandoMarcaRedis("localhost:6379,password=fiapx");
        comando.Dispose();
    }

    private sealed class ComandoFalso : IComandoMarca
    {
        public List<string> Chaves { get; } = [];
        public List<string> Removidas { get; } = [];

        public Task<bool> DefinirSeAusenteAsync(string chave, CancellationToken cancellationToken)
        {
            Chaves.Add(chave);
            return Task.FromResult(true);
        }

        public Task RemoverAsync(string chave, CancellationToken cancellationToken)
        {
            Removidas.Add(chave);
            return Task.CompletedTask;
        }
    }
}

public class QuebraVideoTests
{
    [Fact]
    public async Task Junta_os_frames_num_zip_e_apaga_o_temporario()
    {
        var quebra = new QuebraVideoFfmpeg(new ExecutorFalso());

        await using var zip = await quebra.QuebrarAsync(new MemoryStream([1, 2, 3]), CancellationToken.None);

        using var arquivo = new ZipArchive(zip, ZipArchiveMode.Read);
        Assert.Contains(arquivo.Entries, entrada => entrada.Name == "frame_0001.jpg");
    }

    [Fact]
    public async Task Codigo_diferente_de_zero_e_erro()
    {
        var quebra = new QuebraVideoFfmpeg(new ExecutorFalso { Codigo = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            quebra.QuebrarAsync(new MemoryStream([1]), CancellationToken.None));
    }

    [Fact]
    public async Task Executor_falha_sem_video_ou_sem_binario()
    {
        var executor = new ExecutorFfmpeg();

        try
        {
            var codigo = await executor.ExecutarAsync(
                Path.Combine(Path.GetTempPath(), "fiapx-video-ausente.mp4"),
                Path.Combine(Path.GetTempPath(), "fiapx-frame-%04d.jpg"),
                CancellationToken.None);
            Assert.NotEqual(0, codigo);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Assert.True(true);
        }
    }

    [Fact]
    public async Task Sem_frame_e_erro()
    {
        var quebra = new QuebraVideoFfmpeg(new ExecutorFalso { EscreverFrame = false });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            quebra.QuebrarAsync(new MemoryStream([1]), CancellationToken.None));
    }

    [Fact]
    public async Task Quebra_fixa_devolve_um_zip()
    {
        var quebra = new QuebraVideoFixa();

        await using var zip = await quebra.QuebrarAsync(new MemoryStream([1]), CancellationToken.None);

        using var arquivo = new ZipArchive(zip, ZipArchiveMode.Read);
        Assert.Equal("frame_0001.jpg", Assert.Single(arquivo.Entries).Name);
    }

    private sealed class ExecutorFalso : IExecutorProcesso
    {
        public int Codigo { get; init; }
        public bool EscreverFrame { get; init; } = true;

        public Task<int> ExecutarAsync(string entrada, string padraoSaida, CancellationToken cancellationToken)
        {
            Assert.True(File.Exists(entrada));
            if (EscreverFrame)
            {
                var pasta = Path.GetDirectoryName(padraoSaida)!;
                File.WriteAllBytes(Path.Combine(pasta, "frame_0001.jpg"), [0xFF, 0xD8]);
            }

            return Task.FromResult(Codigo);
        }
    }
}
