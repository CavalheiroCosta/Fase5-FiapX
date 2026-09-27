using Auth.Application.Usuarios;
using Auth.Domain.Tokens;
using Auth.Domain.Usuarios;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("usuarios")]
public sealed class UsuariosController(
    CriarUsuarioUseCase criar,
    ObterUsuarioUseCase obter,
    ListarUsuariosUseCase listar,
    AlterarUsuarioUseCase alterar,
    RemoverUsuarioUseCase remover,
    TokenService tokens) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] UsuarioCadastroRequest request, CancellationToken cancellationToken)
    {
        var recusa = ExigirAdministrador();
        if (recusa is not null)
            return recusa;

        var resultado = await criar.ExecutarAsync(
            request.Login,
            request.Senha,
            request.Nome,
            request.Email,
            cancellationToken);

        if (!resultado.Sucesso || resultado.Valor is null)
            return ParaErro(resultado.Falha!);

        return Created($"/usuarios/{resultado.Valor.Id}", resultado.Valor);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var recusa = ExigirAdministrador();
        if (recusa is not null)
            return recusa;

        return Ok(await listar.ExecutarAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var recusa = ExigirAdministrador();
        if (recusa is not null)
            return recusa;

        var resultado = await obter.ExecutarAsync(id, cancellationToken);
        if (!resultado.Sucesso || resultado.Valor is null)
            return ParaErro(resultado.Falha!);

        return Ok(resultado.Valor);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] UsuarioAlteracaoRequest request, CancellationToken cancellationToken)
    {
        var recusa = ExigirAdministrador();
        if (recusa is not null)
            return recusa;

        var resultado = await alterar.ExecutarAsync(id, request.Nome, request.Email, request.Senha, cancellationToken);
        if (!resultado.Sucesso || resultado.Valor is null)
            return ParaErro(resultado.Falha!);

        return Ok(resultado.Valor);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        var recusa = ExigirAdministrador();
        if (recusa is not null)
            return recusa;

        var resultado = await remover.ExecutarAsync(id, cancellationToken);
        if (!resultado.Sucesso)
            return ParaErro(resultado.Falha!);

        return NoContent();
    }

    private IActionResult? ExigirAdministrador()
    {
        var identidade = tokens.Ler(Bearer());
        if (identidade is null || !identidade.EhAdministradora)
            return Unauthorized(new ErroResposta("Acesso recusado."));

        return null;
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
        CodigosFalha.NaoEncontrado => new NotFoundObjectResult(new ErroResposta(falha.Mensagem)),
        CodigosFalha.Validacao => new BadRequestObjectResult(new ErroResposta(falha.Mensagem)),
        _ => new ConflictObjectResult(new ErroResposta(falha.Mensagem))
    };
}

public sealed record UsuarioCadastroRequest(string? Login, string? Senha, string? Nome, string? Email);

public sealed record UsuarioAlteracaoRequest(string? Nome, string? Email, string? Senha);

public sealed record ErroResposta(string Erro);
