using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Models;

namespace ChuvaSegura.Monitoramento.Application.Services;

public interface IPontoService
{
    Task<PaginaResultado<PontoResponse>> ListarAsync(string? bairro, bool? ativo, int pagina, int tamanho, CancellationToken ct = default);

    Task<PontoResponse> ObterPorIdAsync(int id, CancellationToken ct = default);

    Task<PontoResponse> CriarAsync(SalvarPontoRequest request, CancellationToken ct = default);

    Task<PontoResponse> AtualizarAsync(int id, SalvarPontoRequest request, CancellationToken ct = default);

    Task<PontoResponse> AlterarStatusAsync(int id, bool ativo, CancellationToken ct = default);

    Task RemoverAsync(int id, CancellationToken ct = default);
}
