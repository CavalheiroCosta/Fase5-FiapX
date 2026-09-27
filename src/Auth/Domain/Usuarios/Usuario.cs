namespace Auth.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario()
    {
        Login = "";
        Nome = "";
        Email = "";
        SenhaHash = "";
    }

    public Guid Id { get; private set; }

    public string Login { get; private set; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public string SenhaHash { get; private set; }

    public bool Acesso { get; private set; }

    public bool Administrador { get; private set; }

    public static Resultado<Usuario> Criar(
        string? login,
        string? nome,
        string? email,
        string? senhaHash,
        bool administrador = false)
    {
        var falha = Validar(login, nome, email, senhaHash);
        if (falha is not null)
            return Resultado<Usuario>.Erro(falha.Codigo, falha.Mensagem);

        return Resultado<Usuario>.Ok(new Usuario
        {
            Login = login!.Trim(),
            Nome = nome!.Trim(),
            Email = email!.Trim(),
            SenhaHash = senhaHash!.Trim(),
            Acesso = true,
            Administrador = administrador
        });
    }

    public Resultado<bool> Alterar(string? nome, string? email, string? senhaHash)
    {
        var falha = Validar(Login, nome, email, senhaHash);
        if (falha is not null)
            return Resultado<bool>.Erro(falha.Codigo, falha.Mensagem);

        Nome = nome!.Trim();
        Email = email!.Trim();
        SenhaHash = senhaHash!.Trim();
        return Resultado<bool>.Ok(true);
    }

    public Resultado<bool> Remover()
    {
        if (Administrador)
        {
            return Resultado<bool>.Erro(
                CodigosFalha.AdministradorProtegido,
                "A conta administradora não pode ser removida.");
        }

        return Resultado<bool>.Ok(true);
    }

    internal void AtribuirId(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O identificador do usuário não pode ser vazio.", nameof(id));

        Id = id;
    }

    private static Falha? Validar(string? login, string? nome, string? email, string? senhaHash)
    {
        if (string.IsNullOrWhiteSpace(login))
            return new Falha(CodigosFalha.Validacao, "Login é obrigatório.");

        if (string.IsNullOrWhiteSpace(nome))
            return new Falha(CodigosFalha.Validacao, "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            return new Falha(CodigosFalha.Validacao, "E-mail é obrigatório.");

        var emailNormalizado = email.Trim();
        var arroba = emailNormalizado.IndexOf('@');
        if (arroba <= 0 || arroba == emailNormalizado.Length - 1 || emailNormalizado.Contains(' '))
            return new Falha(CodigosFalha.Validacao, "E-mail inválido.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            return new Falha(CodigosFalha.Validacao, "Senha é obrigatória.");

        return null;
    }
}
