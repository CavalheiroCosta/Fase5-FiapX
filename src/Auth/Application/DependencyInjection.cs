using Auth.Application.Login;
using Auth.Application.Usuarios;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CriarUsuarioUseCase>();
        services.AddScoped<ObterUsuarioUseCase>();
        services.AddScoped<ListarUsuariosUseCase>();
        services.AddScoped<AlterarUsuarioUseCase>();
        services.AddScoped<RemoverUsuarioUseCase>();
        services.AddScoped<LoginUseCase>();
        return services;
    }
}
