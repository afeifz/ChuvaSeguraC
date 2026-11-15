using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChuvaSegura.Monitoramento.Tests.Api;

/// <summary>
/// Sobe a API inteira em memória para os testes de integração, com um banco SQLite temporário
/// e um notificador falso (assim não dependemos do serviço Java).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _caminhoBanco = Path.Combine(Path.GetTempPath(), $"chuvasegura-testes-{Guid.NewGuid():N}.db");

    public FakeNotificador Notificador { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_caminhoBanco}",
                ["Seed:Habilitado"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<INotificadorAlerta>();
            services.AddSingleton<INotificadorAlerta>(Notificador);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            try
            {
                File.Delete(_caminhoBanco);
            }
            catch (IOException)
            {
                // Arquivo temporário: se não der para apagar agora, o sistema limpa depois.
            }
        }
    }
}
