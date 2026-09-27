namespace Auth.Application.Usuarios;

public sealed record UsuarioResposta(Guid Id, string Login, string Nome, string Email);
