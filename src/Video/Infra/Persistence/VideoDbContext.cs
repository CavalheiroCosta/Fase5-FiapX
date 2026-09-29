using Microsoft.EntityFrameworkCore;

namespace Video.Infra.Persistence;

public sealed class VideoDbContext(DbContextOptions<VideoDbContext> options) : DbContext(options)
{
    public DbSet<Video.Domain.Videos.Video> Videos => Set<Video.Domain.Videos.Video>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var video = modelBuilder.Entity<Video.Domain.Videos.Video>();
        video.ToTable("videos");
        video.HasKey(item => item.Id);
        video.Property(item => item.Id).HasColumnName("id");
        video.Property(item => item.Login).HasColumnName("login").IsRequired();
        video.Property(item => item.Email).HasColumnName("email").IsRequired();
        video.Property(item => item.Status).HasColumnName("status").IsRequired();
        video.Property(item => item.CaminhoOriginal).HasColumnName("caminho_original").IsRequired();
        video.Property(item => item.CaminhoZip).HasColumnName("caminho_zip");
    }
}
