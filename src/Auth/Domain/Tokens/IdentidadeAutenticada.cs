namespace Auth.Domain.Tokens;

public sealed record IdentidadeAutenticada(string Login, string Email)
{
    public bool EhAdministradora => ContaAdministradora.Eh(Login);
}
