using Auth.Application.Login;
using Auth.Domain.Usuarios;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("login")]
public sealed class LoginController(LoginUseCase login) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Entrar([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var resultado = await login.ExecutarAsync(request.Login, request.Senha, cancellationToken);
        if (!resultado.Sucesso || resultado.Valor is null)
            return ParaErro(resultado.Falha!);

        return Ok(resultado.Valor);
    }

    private static IActionResult ParaErro(Falha falha) => falha.Codigo switch
    {
        CodigosFalha.Validacao => new BadRequestObjectResult(new ErroResposta(falha.Mensagem)),
        _ => new UnauthorizedObjectResult(new ErroResposta(falha.Mensagem))
    };
}

public sealed record LoginRequest(string? Login, string? Senha);
