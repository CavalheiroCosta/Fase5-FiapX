using Video.Application.Download;
using Video.Application.Envio;
using Video.Application.Listagem;
using Video.Domain.Videos;
using Microsoft.AspNetCore.Mvc;

namespace Video.Api.Controllers;

[ApiController]
[Route("videos")]
public sealed class VideosController(EnviarVideoUseCase enviar, ListarVideosUseCase listar, BaixarZipUseCase baixar) : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Baixar(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await baixar.ExecutarAsync(Bearer(), id, cancellationToken);
        if (!resultado.Sucesso || resultado.Valor is null)
            return ParaErro(resultado.Falha!);

        return File(resultado.Valor.Conteudo, "application/zip", resultado.Valor.NomeArquivo);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var resultado = await listar.ExecutarAsync(Bearer(), cancellationToken);
        if (!resultado.Sucesso)
            return ParaErro(resultado.Falha!);

        return Ok(resultado.Valor);
    }

    [HttpPost]
    [RequestSizeLimit(EnviarVideoUseCase.LimitePadraoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = EnviarVideoUseCase.LimitePadraoBytes)]
    public async Task<IActionResult> Enviar(IFormFile? arquivo, CancellationToken cancellationToken)
    {
        Stream? conteudo = null;
        if (arquivo is not null)
            conteudo = arquivo.OpenReadStream();

        try
        {
            var resultado = await enviar.ExecutarAsync(
                Bearer(),
                arquivo?.FileName,
                conteudo,
                arquivo?.Length ?? 0,
                cancellationToken);

            if (!resultado.Sucesso || resultado.Valor is null)
                return ParaErro(resultado.Falha!);

            return Created($"/videos/{resultado.Valor.Id}", resultado.Valor);
        }
        finally
        {
            if (conteudo is not null)
                await conteudo.DisposeAsync();
        }
    }

    private string? Bearer()
    {
        var cabecalho = Request.Headers.Authorization.ToString();
        const string prefixo = "Bearer ";
        if (!cabecalho.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            return null;

        return cabecalho[prefixo.Length..].Trim();
    }

    private static IActionResult ParaErro(Falha falha) => falha.Codigo switch
    {
        CodigosFalha.Validacao => new BadRequestObjectResult(new ErroResposta(falha.Mensagem)),
        CodigosFalha.AcessoRecusado => new UnauthorizedObjectResult(new ErroResposta(falha.Mensagem)),
        CodigosFalha.NaoEncontrado => new NotFoundObjectResult(new ErroResposta(falha.Mensagem)),
        _ => new ObjectResult(new ErroResposta(falha.Mensagem)) { StatusCode = StatusCodes.Status503ServiceUnavailable }
    };
}

public sealed record ErroResposta(string Erro);
