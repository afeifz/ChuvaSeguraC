using ChuvaSegura.Monitoramento.Application.Abstractions;

namespace ChuvaSegura.Monitoramento.Tests.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task<int> CommitAsync(CancellationToken ct = default)
    {
        Commits++;
        return Task.FromResult(1);
    }
}
