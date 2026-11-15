using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Infrastructure.Persistence;

namespace ChuvaSegura.Monitoramento.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _contexto;

    public UnitOfWork(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
    {
        return _contexto.SaveChangesAsync(ct);
    }
}
