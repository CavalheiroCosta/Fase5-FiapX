using Auth.Application.Senhas;
using Auth.Domain.Usuarios;
using Auth.Infra.Persistence;
using Auth.Infra.Senhas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infra;

public static class DependencyInjection
{
    public const string ProvedorMemoria = "Memory";
    public const string ProvedorPostgres = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISenhaHasher, SenhaHasher>();
        services.AddScoped<AdministradorSeed>();

        var provedor = configuration["Persistence:Provider"] ?? ProvedorPostgres;
        if (provedor.Equals(ProvedorMemoria, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IUsuarioRepository, UsuarioRepositorioMemoria>();
            return services;
        }

        var conexao = configuration.GetConnectionString("Usuarios");
        services.AddDbContext<AuthDbContext>(opcoes => opcoes.UseNpgsql(conexao));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        return services;
    }
}
