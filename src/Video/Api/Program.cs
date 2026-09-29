using OpenTelemetry.Metrics;
using Video.Api;
using Video.Application;
using Video.Infra;
using Video.Infra.Persistence;

var builder = WebApplication.CreateBuilder(args);

var limite = LimiteUpload.Ler(builder.Configuration);
builder.WebHost.ConfigureKestrel(opcoes => opcoes.Limits.MaxRequestBodySize = limite);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opcoes =>
    opcoes.MultipartBodyLengthLimit = limite);

builder.Services.AddControllers();
builder.Services.AddApplication(limite);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PrepararVideoHostedService>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metricas => metricas
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

await app.RunAsync();

public partial class Program;
