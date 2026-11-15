using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Application.Models;

/// <summary>Agregados calculados no banco (via LINQ) sobre as leituras de um ponto.</summary>
public sealed record EstatisticasLeituras(
    int LeiturasValidas,
    int LeiturasInvalidas,
    double? NivelMaximoCm,
    double? NivelMedioCm,
    IReadOnlyDictionary<NivelRisco, int> PorRisco);
