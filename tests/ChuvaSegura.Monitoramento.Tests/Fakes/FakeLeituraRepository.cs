using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Tests.Fakes;

internal sealed class FakeLeituraRepository : ILeituraRepository
{
    public List<Leitura> Itens { get; } = new();

    public Task AdicionarAsync(Leitura leitura, CancellationToken ct = default)
    {
        Itens.Add(leitura);
        return Task.CompletedTask;
    }

    public Task<PaginaResultado<Leitura>> ListarPorPontoAsync(
        int pontoId,
        DateTime? deUtc,
        DateTime? ateUtc,
        bool? apenasValidas,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        var lista = Itens.Where(l => l.PontoId == pontoId).OrderByDescending(l => l.DataHora).ToList();
        var itens = lista.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        return Task.FromResult(new PaginaResultado<Leitura>(itens, pagina, tamanho, lista.Count));
    }

    public Task<EstatisticasLeituras> ObterEstatisticasAsync(int pontoId, DateTime desdeUtc, CancellationToken ct = default)
    {
        var validas = Itens.Where(l => l.PontoId == pontoId && l.Valida && l.DataHora >= desdeUtc).ToList();
        var invalidas = Itens.Count(l => l.PontoId == pontoId && !l.Valida && l.DataHora >= desdeUtc);

        IReadOnlyDictionary<NivelRisco, int> porRisco = validas
            .Where(l => l.Risco.HasValue)
            .GroupBy(l => l.Risco!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(new EstatisticasLeituras(
            validas.Count,
            invalidas,
            validas.Count == 0 ? null : validas.Max(l => l.NivelAguaCm),
            validas.Count == 0 ? null : validas.Average(l => l.NivelAguaCm),
            porRisco));
    }
}
