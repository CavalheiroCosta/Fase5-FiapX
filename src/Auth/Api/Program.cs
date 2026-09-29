using Auth.Application;
using Auth.Infra;
using Auth.Infra.Persistence;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PrepararAuthHostedService>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metricas => metricas
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program;
