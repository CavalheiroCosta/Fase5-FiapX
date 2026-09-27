using Auth.Application.Usuarios;
using Auth.Domain.Usuarios;
using Auth.Tests.Suporte;

namespace Auth.Tests;

public class CadastroUseCaseTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly HasherFalso _hasher = new();

    [Fact]
    public async Task Cria_consulta_lista_altera_e_remove()
    {
        var criar = new CriarUsuarioUseCase(_repositorio, _hasher);
        var criado = await criar.ExecutarAsync("ana", "senha-ana", "Ana", "ana@email.com", CancellationToken.None);

        Assert.True(criado.Sucesso);
        Assert.NotEqual(Guid.Empty, criado.Valor!.Id);
        Assert.Equal("ana", criado.Valor.Login);
        Assert.Equal("Ana", criado.Valor.Nome);
        Assert.Equal("ana@email.com", criado.Valor.Email);
        Assert.DoesNotContain("senha", criado.Valor.ToString(), StringComparison.OrdinalIgnoreCase);

        var obtido = await new ObterUsuarioUseCase(_repositorio).ExecutarAsync(criado.Valor.Id, CancellationToken.None);
        Assert.Equal(criado.Valor.Id, obtido.Valor!.Id);

        var lista = await new ListarUsuariosUseCase(_repositorio).ExecutarAsync(CancellationToken.None);
        Assert.Contains(lista, item => item.Id == criado.Valor.Id);

        var alterado = await new AlterarUsuarioUseCase(_repositorio, _hasher).ExecutarAsync(
            criado.Valor.Id,
            "Ana Costa",
            "ana.costa@email.com",
            "senha-nova",
            CancellationToken.None);

        Assert.True(alterado.Sucesso);
        Assert.Equal("ana", alterado.Valor!.Login);
        Assert.Equal("Ana Costa", alterado.Valor.Nome);
        Assert.Equal("ana.costa@email.com", alterado.Valor.Email);
        Assert.DoesNotContain("senha", alterado.Valor.ToString(), StringComparison.OrdinalIgnoreCase);

        var remocao = await new RemoverUsuarioUseCase(_repositorio).ExecutarAsync(criado.Valor.Id, CancellationToken.None);
        Assert.True(remocao.Sucesso);
        Assert.False((await new ObterUsuarioUseCase(_repositorio).ExecutarAsync(criado.Valor.Id, CancellationToken.None)).Sucesso);
    }

    [Fact]
    public async Task Recusa_login_repetido()
    {
        var criar = new CriarUsuarioUseCase(_repositorio, _hasher);
        await criar.ExecutarAsync("ana", "senha-ana", "Ana", "ana@email.com", CancellationToken.None);

        var repetido = await criar.ExecutarAsync("ana", "outra", "Ana Dois", "outro@email.com", CancellationToken.None);

        Assert.False(repetido.Sucesso);
        Assert.Equal(CodigosFalha.LoginRepetido, repetido.Falha!.Codigo);
    }

    [Fact]
    public async Task Recusa_email_repetido_na_criacao_e_na_alteracao()
    {
        var criar = new CriarUsuarioUseCase(_repositorio, _hasher);
        var ana = await criar.ExecutarAsync("ana", "senha-ana", "Ana", "ana@email.com", CancellationToken.None);
        await criar.ExecutarAsync("bia", "senha-bia", "Bia", "bia@email.com", CancellationToken.None);

        var repetido = await criar.ExecutarAsync("cia", "senha-cia", "Cia", "ana@email.com", CancellationToken.None);
        Assert.Equal(CodigosFalha.EmailRepetido, repetido.Falha!.Codigo);

        var alterado = await new AlterarUsuarioUseCase(_repositorio, _hasher).ExecutarAsync(
            ana.Valor!.Id,
            "Ana",
            "bia@email.com",
            "senha-ana",
            CancellationToken.None);

        Assert.Equal(CodigosFalha.EmailRepetido, alterado.Falha!.Codigo);
    }

    [Fact]
    public async Task Recusa_remover_a_conta_administradora()
    {
        var administrador = Usuario.Criar("Adm", "Administrador", "adm@adm.com", "hash:Adm", administrador: true);
        await _repositorio.AdicionarAsync(administrador.Valor!, CancellationToken.None);

        var remocao = await new RemoverUsuarioUseCase(_repositorio).ExecutarAsync(administrador.Valor!.Id, CancellationToken.None);

        Assert.Equal(CodigosFalha.AdministradorProtegido, remocao.Falha!.Codigo);
        Assert.NotNull(await _repositorio.ObterPorLoginAsync("Adm", CancellationToken.None));
    }

    [Theory]
    [InlineData("", "senha", "Ana", "ana@email.com", "Login é obrigatório.")]
    [InlineData("ana", "", "Ana", "ana@email.com", "Senha é obrigatória.")]
    [InlineData("ana", "senha", "", "ana@email.com", "Nome é obrigatório.")]
    [InlineData("ana", "senha", "Ana", "", "E-mail é obrigatório.")]
    [InlineData("ana", "senha", "Ana", "ana-email", "E-mail inválido.")]
    public async Task Recusa_cadastro_invalido(string login, string senha, string nome, string email, string mensagem)
    {
        var resultado = await new CriarUsuarioUseCase(_repositorio, _hasher)
            .ExecutarAsync(login, senha, nome, email, CancellationToken.None);

        Assert.Equal(CodigosFalha.Validacao, resultado.Falha!.Codigo);
        Assert.Equal(mensagem, resultado.Falha.Mensagem);
    }

    [Fact]
    public async Task Recusa_consulta_alteracao_e_remocao_de_usuario_inexistente()
    {
        var id = Guid.NewGuid();

        var obtido = await new ObterUsuarioUseCase(_repositorio).ExecutarAsync(id, CancellationToken.None);
        var alterado = await new AlterarUsuarioUseCase(_repositorio, _hasher)
            .ExecutarAsync(id, "Ana", "ana@email.com", "senha", CancellationToken.None);
        var removido = await new RemoverUsuarioUseCase(_repositorio).ExecutarAsync(id, CancellationToken.None);

        Assert.Equal(CodigosFalha.NaoEncontrado, obtido.Falha!.Codigo);
        Assert.Equal(CodigosFalha.NaoEncontrado, alterado.Falha!.Codigo);
        Assert.Equal(CodigosFalha.NaoEncontrado, removido.Falha!.Codigo);
    }

    [Fact]
    public async Task Recusa_alteracao_sem_nome_email_ou_senha()
    {
        var criado = await new CriarUsuarioUseCase(_repositorio, _hasher)
            .ExecutarAsync("ana", "senha", "Ana", "ana@email.com", CancellationToken.None);
        var alterar = new AlterarUsuarioUseCase(_repositorio, _hasher);

        var semSenha = await alterar.ExecutarAsync(criado.Valor!.Id, "Ana", "ana@email.com", " ", CancellationToken.None);
        var semNome = await alterar.ExecutarAsync(criado.Valor.Id, " ", "ana@email.com", "senha", CancellationToken.None);
        var semEmail = await alterar.ExecutarAsync(criado.Valor.Id, "Ana", "invalido", "senha", CancellationToken.None);

        Assert.Equal("Senha é obrigatória.", semSenha.Falha!.Mensagem);
        Assert.Equal("Nome é obrigatório.", semNome.Falha!.Mensagem);
        Assert.Equal("E-mail inválido.", semEmail.Falha!.Mensagem);
    }

    [Fact]
    public void Usuario_rejeita_hash_vazio_e_id_vazio()
    {
        var semHash = Usuario.Criar("ana", "Ana", "ana@email.com", " ");
        Assert.Equal("Senha é obrigatória.", semHash.Falha!.Mensagem);

        var criado = Usuario.Criar("ana", "Ana", "ana@email.com", "hash");
        Assert.Throws<ArgumentException>(() => criado.Valor!.AtribuirId(Guid.Empty));
    }

    [Fact]
    public async Task Mantem_o_email_do_proprio_usuario_na_alteracao()
    {
        var criado = await new CriarUsuarioUseCase(_repositorio, _hasher)
            .ExecutarAsync("ana", "senha", "Ana", "ana@email.com", CancellationToken.None);

        var alterado = await new AlterarUsuarioUseCase(_repositorio, _hasher).ExecutarAsync(
            criado.Valor!.Id,
            "Ana Costa",
            "ana@email.com",
            "senha-nova",
            CancellationToken.None);

        Assert.True(alterado.Sucesso);
        Assert.Equal("ana@email.com", alterado.Valor!.Email);
    }
}
