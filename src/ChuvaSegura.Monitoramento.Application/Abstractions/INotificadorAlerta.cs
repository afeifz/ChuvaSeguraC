using ChuvaSegura.Monitoramento.Application.Models;

namespace ChuvaSegura.Monitoramento.Application.Abstractions;

/// <summary>Avisa o serviço de Cadastro e Alertas que o risco de um ponto mudou.</summary>
public interface INotificadorAlerta
{
    Task NotificarAsync(NotificacaoRisco notificacao, CancellationToken ct = default);
}
