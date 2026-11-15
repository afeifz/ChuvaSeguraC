using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;

namespace ChuvaSegura.Monitoramento.Application.Abstractions;

public interface ILeituraRepository
{
    Task AdicionarAsync(Leitura leitura, CancellationToken ct = default);

    Task<PaginaResultado<Leitura>> ListarPorPontoAsync(
        int pontoId,
        DateTime? deUtc,
        DateTime? ateUtc,
        bool? apenasValidas,
        int pagina,
        int tamanho,
        CancellationToken ct = default);

    Task<EstatisticasLeituras> ObterEstatisticasAsync(int pontoId, DateTime desdeUtc, CancellationToken ct = default);
}
