using ChuvaSegura.Monitoramento.Application.Dtos;

namespace ChuvaSegura.Monitoramento.Application.Services;

public interface IRiscoService
{
    Task<RiscoPontoResponse> ObterDoPontoAsync(int pontoId, CancellationToken ct = default);

    Task<RiscoBairroResponse> ObterDoBairroAsync(string bairro, CancellationToken ct = default);
}
