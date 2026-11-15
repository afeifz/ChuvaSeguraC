using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChuvaSegura.Monitoramento.Infrastructure.Repositories;

public sealed class PontoRepository : IPontoRepository
{
    private readonly AppDbContext _contexto;

    public PontoRepository(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Ponto?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        return _contexto.Pontos.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> ExisteAsync(int id, CancellationToken ct = default)
    {
        return _contexto.Pontos.AnyAsync(p => p.Id == id, ct);
    }

    public Task<bool> ExisteComNomeAsync(string nome, int? ignorarId, CancellationToken ct = default)
    {
        var nomeNormalizado = nome.Trim().ToLower();

        return _contexto.Pontos.AnyAsync(
            p => p.Nome.ToLower() == nomeNormalizado && (ignorarId == null || p.Id != ignorarId),
            ct);
    }

    public async Task<PaginaResultado<Ponto>> ListarAsync(string? bairro, bool? ativo, int pagina, int tamanho, CancellationToken ct = default)
    {
        var consulta = _contexto.Pontos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            var bairroNormalizado = bairro.Trim().ToLower();
            consulta = consulta.Where(p => p.Bairro.ToLower() == bairroNormalizado);
        }

        if (ativo.HasValue)
        {
            var valor = ativo.Value;
            consulta = consulta.Where(p => p.Ativo == valor);
        }

        var total = await consulta.CountAsync(ct);

        var itens = await consulta
            .OrderBy(p => p.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);

        return new PaginaResultado<Ponto>(itens, pagina, tamanho, total);
    }

    public async Task<IReadOnlyList<Ponto>> ListarAtivosPorBairroAsync(string bairro, CancellationToken ct = default)
    {
        var bairroNormalizado = bairro.Trim().ToLower();

        return await _contexto.Pontos
            .AsNoTracking()
            .Where(p => p.Ativo && p.Bairro.ToLower() == bairroNormalizado)
            .OrderBy(p => p.Nome)
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(Ponto ponto, CancellationToken ct = default)
    {
        await _contexto.Pontos.AddAsync(ponto, ct);
    }

    public void Remover(Ponto ponto)
    {
        _contexto.Pontos.Remove(ponto);
    }
}
