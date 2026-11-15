using System.Collections.Concurrent;
using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Models;

namespace ChuvaSegura.Monitoramento.Tests.Fakes;

/// <summary>Guarda as notificações recebidas (e pode simular falha no serviço de alertas).</summary>
public sealed class FakeNotificador : INotificadorAlerta
{
    private readonly ConcurrentQueue<NotificacaoRisco> _enviadas = new();

    public bool Falhar { get; set; }

    public IReadOnlyCollection<NotificacaoRisco> Enviadas => _enviadas.ToArray();

    public Task NotificarAsync(NotificacaoRisco notificacao, CancellationToken ct = default)
    {
        if (Falhar)
        {
            throw new HttpRequestException("Serviço de alertas indisponível (simulado).");
        }

        _enviadas.Enqueue(notificacao);
        return Task.CompletedTask;
    }
}
