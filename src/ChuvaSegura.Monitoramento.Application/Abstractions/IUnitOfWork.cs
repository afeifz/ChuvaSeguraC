namespace ChuvaSegura.Monitoramento.Application.Abstractions;

/// <summary>Confirma (persiste) todas as alterações feitas durante a requisição.</summary>
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}
