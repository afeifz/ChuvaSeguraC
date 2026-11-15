using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Application.Models;

/// <summary>Mensagem enviada ao serviço de Cadastro e Alertas (Java) quando o risco de um ponto muda.</summary>
public sealed record NotificacaoRisco(int PontoId, string Bairro, NivelRisco Risco, double NivelAguaCm, DateTime DataHoraUtc);
