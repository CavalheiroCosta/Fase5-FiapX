namespace Auth.Domain.Tokens;

public static class ContaAdministradora
{
    public const string Login = "Adm";

    public static bool Eh(string? login) => login == Login;
}
