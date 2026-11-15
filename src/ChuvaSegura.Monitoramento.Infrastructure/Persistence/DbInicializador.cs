using ChuvaSegura.Monitoramento.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChuvaSegura.Monitoramento.Infrastructure.Persistence;

/// <summary>Cria o banco SQLite na primeira execução e, opcionalmente, insere um ponto de exemplo.</summary>
public static class DbInicializador
{
    public static async Task InicializarAsync(IServiceProvider services, bool semear, CancellationToken ct = default)
    {
        using var escopo = services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        await contexto.Database.EnsureCreatedAsync(ct);

        if (!semear || await contexto.Pontos.AnyAsync(ct))
        {
            return;
        }

        var relogio = escopo.ServiceProvider.GetRequiredService<TimeProvider>();

        // Dados de exemplo (simulados). As coordenadas são aproximadas e servem apenas para a demonstração.
        contexto.Pontos.Add(new Ponto(
            "Ponto Ipiranga 01 (simulado)",
            "Ipiranga",
            -23.5875,
            -46.6095,
            100,
            relogio.GetUtcNow().UtcDateTime));

        await contexto.SaveChangesAsync(ct);
    }
}
