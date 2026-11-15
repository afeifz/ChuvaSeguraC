using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Models;

namespace ChuvaSegura.Monitoramento.Application.Services;

public interface ILeituraService
{
    Task<ResultadoRegistroLeitura> RegistrarAsync(RegistrarLeituraRequest request, CancellationToken ct = default);

    Task<PaginaResultado<LeituraResponse>> ListarAsync(
        int pontoId,
        DateTime? de,
        DateTime? ate,
        bool? apenasValidas,
        int pagina,
        int tamanho,
        CancellationToken ct = default);

    Task<EstatisticasPontoResponse> ObterEstatisticasAsync(int pontoId, int horas, CancellationToken ct = default);
}
