using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Domain.Entities;

/// <summary>Resultado de registrar uma leitura em um ponto.</summary>
/// <param name="Leitura">A leitura criada (válida ou rejeitada).</param>
/// <param name="RiscoMudou">Verdadeiro quando a leitura é válida e o risco do ponto mudou.</param>
/// <param name="RiscoAnterior">Risco do ponto antes da leitura.</param>
public sealed record ResultadoLeitura(Leitura Leitura, bool RiscoMudou, NivelRisco RiscoAnterior);
