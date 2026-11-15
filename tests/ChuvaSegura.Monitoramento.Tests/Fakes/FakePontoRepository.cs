using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;

namespace ChuvaSegura.Monitoramento.Tests.Fakes;

/// <summary>Repositório em memória usado nos testes unitários dos serviços.</summary>
internal sealed class FakePontoRepository : IPontoRepository
{
    private int _proximoId = 1;

    public List<Ponto> Itens { get; } = new();

    public Task<Ponto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<bool> ExisteAsync(int id, CancellationToken ct = default)
        => Task.FromResult(Itens.Any(p => p.Id == id));

    public Task<bool> ExisteComNomeAsync(string nome, int? ignorarId, CancellationToken ct = default)
    {
        var existe = Itens.Any(p =>
            string.Equals(p.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase)
            && (ignorarId is null || p.Id != ignorarId));
        return Task.FromResult(existe);
    }

    public Task<PaginaResultado<Ponto>> ListarAsync(string? bairro, bool? ativo, int pagina, int tamanho, CancellationToken ct = default)
    {
        var consulta = Itens.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            consulta = consulta.Where(p => string.Equals(p.Bairro, bairro.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (ativo.HasValue)
        {
            consulta = consulta.Where(p => p.Ativo == ativo.Value);
        }

        var lista = consulta.OrderBy(p => p.Nome).ToList();
        var itens = lista.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        return Task.FromResult(new PaginaResultado<Ponto>(itens, pagina, tamanho, lista.Count));
    }

    public Task<IReadOnlyList<Ponto>> ListarAtivosPorBairroAsync(string bairro, CancellationToken ct = default)
    {
        IReadOnlyList<Ponto> lista = Itens
            .Where(p => p.Ativo && string.Equals(p.Bairro, bairro.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Nome)
            .ToList();
        return Task.FromResult(lista);
    }

    public Task AdicionarAsync(Ponto ponto, CancellationToken ct = default)
    {
        DefinirId(ponto, _proximoId++);
        Itens.Add(ponto);
        return Task.CompletedTask;
    }

    public void Remover(Ponto ponto) => Itens.Remove(ponto);

    /// <summary>Simula o que o banco faz ao gerar a chave primária.</summary>
    public static void DefinirId(Ponto ponto, int id)
    {
        typeof(Ponto).GetProperty(nameof(Ponto.Id))!.SetValue(ponto, id);
    }
}
