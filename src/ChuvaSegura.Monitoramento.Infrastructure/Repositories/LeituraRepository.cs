using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChuvaSegura.Monitoramento.Infrastructure.Repositories;

public sealed class LeituraRepository : ILeituraRepository
{
    private readonly AppDbContext _contexto;

    public LeituraRepository(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task AdicionarAsync(Leitura leitura, CancellationToken ct = default)
    {
        await _contexto.Leituras.AddAsync(leitura, ct);
    }

    public async Task<PaginaResultado<Leitura>> ListarPorPontoAsync(
        int pontoId,
        DateTime? deUtc,
        DateTime? ateUtc,
        bool? apenasValidas,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        var consulta = _contexto.Leituras
            .AsNoTracking()
            .Where(l => l.PontoId == pontoId);

        if (deUtc.HasValue)
        {
            var de = deUtc.Value;
            consulta = consulta.Where(l => l.DataHora >= de);
        }

        if (ateUtc.HasValue)
        {
            var ate = ateUtc.Value;
            consulta = consulta.Where(l => l.DataHora <= ate);
        }

        if (apenasValidas == true)
        {
            consulta = consulta.Where(l => l.Valida);
        }

        var total = await consulta.CountAsync(ct);

        var itens = await consulta
            .OrderByDescending(l => l.DataHora)
            .ThenByDescending(l => l.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaResultado<Leitura>(itens, pagina, tamanho, total);
    }

    /// <summary>Agregados (contagem, máximo, média e total por risco) calculados direto no banco com LINQ.</summary>
    public async Task<EstatisticasLeituras> ObterEstatisticasAsync(int pontoId, DateTime desdeUtc, CancellationToken ct = default)
    {
        var janela = _contexto.Leituras
            .AsNoTracking()
            .Where(l => l.PontoId == pontoId && l.DataHora >= desdeUtc);

        var invalidas = await janela.CountAsync(l => !l.Valida, ct);

        var validas = janela.Where(l => l.Valida);

        var agregado = await validas
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Maximo = g.Max(l => l.NivelAguaCm),
                Media = g.Average(l => l.NivelAguaCm)
            })
            .FirstOrDefaultAsync(ct);

        var porRisco = await validas
            .GroupBy(l => l.Risco)
            .Select(g => new { Risco = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var mapa = porRisco
            .Where(x => x.Risco.HasValue)
            .ToDictionary(x => x.Risco!.Value, x => x.Total);

        return new EstatisticasLeituras(
            agregado?.Total ?? 0,
            invalidas,
            agregado?.Maximo,
            agregado?.Media,
            mapa);
    }
}
