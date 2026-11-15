using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Infrastructure.Notificacoes;
using ChuvaSegura.Monitoramento.Infrastructure.Persistence;
using ChuvaSegura.Monitoramento.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChuvaSegura.Monitoramento.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra banco de dados (SQLite), repositórios e o cliente HTTP do serviço de alertas.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // A connection string é lida de forma "preguiçosa" (quando o contexto é criado),
        // o que permite sobrescrevê-la nos testes de integração.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var configuracao = sp.GetRequiredService<IConfiguration>();
            var conexao = configuracao.GetConnectionString("Default") ?? "Data Source=chuvasegura-monitoramento.db";
            options.UseSqlite(conexao);
        });

        services.AddScoped<IPontoRepository, PontoRepository>();
        services.AddScoped<ILeituraRepository, LeituraRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddOptions<AlertasServiceOptions>().BindConfiguration(AlertasServiceOptions.Secao);

        services.AddHttpClient<INotificadorAlerta, HttpNotificadorAlerta>((sp, cliente) =>
        {
            var opcoes = sp.GetRequiredService<IOptions<AlertasServiceOptions>>().Value;

            if (Uri.TryCreate(opcoes.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                cliente.BaseAddress = baseUri;
            }

            cliente.Timeout = TimeSpan.FromSeconds(Math.Max(1, opcoes.TimeoutSegundos));
        });

        return services;
    }
}
