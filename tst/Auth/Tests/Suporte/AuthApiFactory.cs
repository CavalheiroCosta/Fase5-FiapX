using Auth.Domain.Usuarios;
using Auth.Infra.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Tests.Suporte;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(servicos =>
        {
            var remover = servicos.Where(descritor =>
                descritor.ServiceType == typeof(IUsuarioRepository) ||
                descritor.ServiceType == typeof(AuthDbContext)).ToList();

            foreach (var descritor in remover)
                servicos.Remove(descritor);

            servicos.AddSingleton<IUsuarioRepository, UsuarioRepositorioMemoria>();
        });
    }
}
