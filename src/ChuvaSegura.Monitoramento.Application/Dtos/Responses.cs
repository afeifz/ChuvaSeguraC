using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Application.Dtos;

public sealed record PontoResponse(
    int Id,
    string Nome,
    string Bairro,
    double Latitude,
    double Longitude,
    double AlturaInstalacaoCm,
    bool Ativo,
    NivelRisco RiscoAtual,
    double? UltimoNivelAguaCm,
    DateTime? UltimaLeituraEm);

public sealed record LeituraResponse(
    int Id,
    int PontoId,
    double DistanciaCm,
    double? NivelAguaCm,
    NivelRisco? Risco,
    bool Valida,
    string? MotivoRejeicao,
    DateTime DataHora);

/// <summary>Resultado do registro de uma leitura. Quando <c>Aceita</c> é falso, a API responde 422.</summary>
public sealed record ResultadoRegistroLeitura(LeituraResponse Leitura, bool Aceita);

/// <summary>Risco de um ponto, em linguagem simples para o cidadão.</summary>
public sealed record RiscoPontoResponse(
    int PontoId,
    string Nome,
    string Bairro,
    NivelRisco Risco,
    string Orientacao,
    double? NivelAguaCm,
    DateTime? UltimaLeituraEm,
    bool Desatualizado);

/// <summary>Risco de um bairro: o maior risco entre os pontos ativos dele.</summary>
public sealed record RiscoBairroResponse(
    string Bairro,
    NivelRisco Risco,
    string Orientacao,
    bool AlgumDadoDesatualizado,
    IReadOnlyList<RiscoPontoResponse> Pontos);

public sealed record EstatisticasPontoResponse(
    int PontoId,
    DateTime DesdeUtc,
    int JanelaHoras,
    int LeiturasValidas,
    int LeiturasInvalidas,
    double? NivelMaximoCm,
    double? NivelMedioCm,
    IReadOnlyDictionary<string, int> LeiturasPorRisco);
