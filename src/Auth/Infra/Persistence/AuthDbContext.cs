using Auth.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infra.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var usuario = modelBuilder.Entity<Usuario>();
        usuario.ToTable("usuarios");
        usuario.HasKey(item => item.Id);
        usuario.Property(item => item.Login).IsRequired();
        usuario.Property(item => item.Nome).IsRequired();
        usuario.Property(item => item.Email).IsRequired();
        usuario.Property(item => item.SenhaHash).IsRequired();
        usuario.HasIndex(item => item.Login).IsUnique();
        usuario.HasIndex(item => item.Email).IsUnique();
    }
}
