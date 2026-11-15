using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;

namespace ChuvaSegura.Monitoramento.Application.Abstractions;

public interface IPontoRepository
{
    Task<Ponto?> ObterPorIdAsync(int id, CancellationToken ct = default);

    Task<bool> ExisteAsync(int id, CancellationToken ct = default);

    Task<bool> ExisteComNomeAsync(string nome, int? ignorarId, CancellationToken ct = default);

    Task<PaginaResultado<Ponto>> ListarAsync(string? bairro, bool? ativo, int pagina, int tamanho, CancellationToken ct = default);

    Task<IReadOnlyList<Ponto>> ListarAtivosPorBairroAsync(string bairro, CancellationToken ct = default);

    Task AdicionarAsync(Ponto ponto, CancellationToken ct = default);

    void Remover(Ponto ponto);
}
