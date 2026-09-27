using Auth.Application;
using Auth.Infra;
using Auth.Infra.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PrepararAuthHostedService>();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program;
