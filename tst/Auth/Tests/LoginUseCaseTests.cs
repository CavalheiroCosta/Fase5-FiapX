using Auth.Application.Login;
using Auth.Application.Usuarios;
using Auth.Domain.Usuarios;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class LoginUseCaseTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly HasherFalso _hasher = new();
    private readonly AssinaturaFalsa _assinatura = new();
    private readonly LoginUseCase _login;

    public LoginUseCaseTests()
    {
        var tokens = new Auth.Domain.Tokens.TokenService(_assinatura, TimeProvider.System);
        _login = new LoginUseCase(_repositorio, _hasher, tokens);
    }

    [Fact]
    public async Task Senha_certa_emite_token_sem_senha()
    {
        await new CriarUsuarioUseCase(_repositorio, _hasher).ExecutarAsync(
            "ana",
            "senha-ana",
            "Ana",
            "ana@email.com",
            CancellationToken.None);

        var resultado = await _login.ExecutarAsync("ana", "senha-ana", CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("valido", resultado.Valor!.Token);
        Assert.Equal(1, _assinatura.Emissoes);
        Assert.Equal("ana", _assinatura.Identidade!.Login);
        Assert.Equal("ana@email.com", _assinatura.Identidade.Email);
        Assert.DoesNotContain("senha", resultado.Valor.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Senha_errada_e_usuario_inexistente_recusam_igual_e_nao_emitem_token()
    {
        await new CriarUsuarioUseCase(_repositorio, _hasher).ExecutarAsync(
            "ana",
            "senha-ana",
            "Ana",
            "ana@email.com",
            CancellationToken.None);

        var senhaErrada = await _login.ExecutarAsync("ana", "errada", CancellationToken.None);
        var inexistente = await _login.ExecutarAsync("ninguem", "senha-ana", CancellationToken.None);

        Assert.False(senhaErrada.Sucesso);
        Assert.False(inexistente.Sucesso);
        Assert.Null(senhaErrada.Valor);
        Assert.Null(inexistente.Valor);
        Assert.Equal(CodigosFalha.CredencialInvalida, senhaErrada.Falha!.Codigo);
        Assert.Equal(senhaErrada.Falha.Codigo, inexistente.Falha!.Codigo);
        Assert.Equal(senhaErrada.Falha.Mensagem, inexistente.Falha.Mensagem);
        Assert.Equal(0, _assinatura.Emissoes);
    }

    [Fact]
    public async Task Login_ou_senha_em_branco_nao_emite_token()
    {
        var resultado = await _login.ExecutarAsync("  ", null, CancellationToken.None);

        Assert.Equal(CodigosFalha.Validacao, resultado.Falha!.Codigo);
        Assert.Equal(0, _assinatura.Emissoes);
    }
}
