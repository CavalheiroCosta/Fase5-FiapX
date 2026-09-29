using Video.Domain.Tokens;
using Video.Domain.Videos;

namespace Video.Application.Envio;

public sealed record VideoResposta(Guid Id, string Status, string Caminho);

public sealed class EnviarVideoUseCase(
    ILeitorToken leitor,
    IArmazenamentoVideo armazenamento,
    IVideoRepository repositorio,
    IFilaProcessamento fila,
    long limiteBytes)
{
    public const long LimitePadraoBytes = 200L * 1024 * 1024;

    public async Task<Resultado<VideoResposta>> ExecutarAsync(
        string? token,
        string? nomeArquivo,
        Stream? conteudo,
        long tamanho,
        CancellationToken cancellationToken)
    {
        if (leitor.Ler(token) is not { } identidade)
            return Resultado<VideoResposta>.Erro(CodigosFalha.AcessoRecusado, "Acesso recusado.");

        if (conteudo is null || tamanho <= 0 || CaminhoVideo.NomeSeguro(nomeArquivo) is not { } nome)
            return Resultado<VideoResposta>.Erro(CodigosFalha.Validacao, "O arquivo do vídeo é obrigatório.");

        if (tamanho > limiteBytes)
            return Resultado<VideoResposta>.Erro(CodigosFalha.Validacao, "O arquivo passa do limite de envio.");

        var id = Guid.NewGuid();
        string caminho;
        try
        {
            caminho = await armazenamento.SalvarAsync(id, nome, conteudo, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Resultado<VideoResposta>.Erro(CodigosFalha.Storage, "Não foi possível gravar o arquivo.");
        }

        var criado = global::Video.Domain.Videos.Video.Registrar(id, identidade.Login, identidade.Email, caminho);
        if (!criado.Sucesso || criado.Valor is null)
        {
            await RemoverSilenciosoAsync(caminho, cancellationToken);
            return Resultado<VideoResposta>.Erro(criado.Falha!.Codigo, criado.Falha.Mensagem);
        }

        try
        {
            await repositorio.AdicionarAsync(criado.Valor, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RemoverSilenciosoAsync(caminho, cancellationToken);
            return Resultado<VideoResposta>.Erro(CodigosFalha.Persistencia, "Não foi possível registrar o vídeo.");
        }

        try
        {
            await fila.PublicarAsync(id, caminho, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Resultado<VideoResposta>.Erro(
                CodigosFalha.Fila,
                $"O vídeo {id:D} ficou aguardando processamento.");
        }

        return Resultado<VideoResposta>.Ok(new VideoResposta(id, criado.Valor.Status, caminho));
    }

    private async Task RemoverSilenciosoAsync(string caminho, CancellationToken cancellationToken)
    {
        try
        {
            await armazenamento.RemoverAsync(caminho, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }
    }
}
