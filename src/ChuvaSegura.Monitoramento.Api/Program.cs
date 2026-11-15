using System.Reflection;
using System.Text.Json.Serialization;
using ChuvaSegura.Monitoramento.Api.Middleware;
using ChuvaSegura.Monitoramento.Application;
using ChuvaSegura.Monitoramento.Infrastructure;
using ChuvaSegura.Monitoramento.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(opcoes => opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChuvaSegura - Serviço de Monitoramento",
        Version = "v1",
        Description = "Recebe as leituras de nível de água dos sensores (ESP32), classifica o risco de alagamento "
            + "e expõe o risco por ponto e por bairro para o aplicativo do cidadão. "
            + "Global Solution FIAP 2026.2 - CitizenTech."
    });

    var arquivoXml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(arquivoXml))
    {
        opcoes.IncludeXmlComments(arquivoXml);
    }
});

// Erros padronizados (ProblemDetails) + tratamento central de exceções.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Permite que o app Expo (versão web) e o painel chamem a API durante o desenvolvimento.
builder.Services.AddCors(opcoes => opcoes.AddDefaultPolicy(politica =>
    politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

await DbInicializador.InicializarAsync(app.Services, app.Configuration.GetValue("Seed:Habilitado", true));

app.UseExceptionHandler();
app.UseCors();

// Swagger fica ligado em qualquer ambiente: é a documentação dos endpoints exigida na entrega.
app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "ChuvaSegura Monitoramento v1");
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
