using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Domain.Entities;

/// <summary>
/// Uma medição enviada pelo sensor. Leituras fora da faixa do sensor são guardadas como inválidas
/// (para auditoria), mas nunca alteram o risco do ponto.
/// </summary>
public class Leitura
{
    // Construtor exigido pelo EF Core.
    private Leitura()
    {
    }

    public int Id { get; private set; }

    public int PontoId { get; private set; }

    /// <summary>Distância medida pelo sensor até a superfície da água, em cm.</summary>
    public double DistanciaCm { get; private set; }

    /// <summary>Altura da água calculada (altura de instalação − distância). Nulo em leituras inválidas.</summary>
    public double? NivelAguaCm { get; private set; }

    /// <summary>Risco calculado. Nulo em leituras inválidas.</summary>
    public NivelRisco? Risco { get; private set; }

    public bool Valida { get; private set; }

    public string? MotivoRejeicao { get; private set; }

    /// <summary>Momento do registro, em UTC.</summary>
    public DateTime DataHora { get; private set; }

    public static Leitura Aceita(int pontoId, double distanciaCm, double nivelAguaCm, NivelRisco risco, DateTime dataHoraUtc)
    {
        return new Leitura
        {
            PontoId = pontoId,
            DistanciaCm = distanciaCm,
            NivelAguaCm = nivelAguaCm,
            Risco = risco,
            Valida = true,
            DataHora = dataHoraUtc
        };
    }

    public static Leitura Rejeitada(int pontoId, double distanciaCm, string motivo, DateTime dataHoraUtc)
    {
        return new Leitura
        {
            PontoId = pontoId,
            // Valores não numéricos (NaN/infinito) não cabem no banco; guardamos 0 e o motivo explica.
            DistanciaCm = double.IsFinite(distanciaCm) ? distanciaCm : 0,
            Valida = false,
            MotivoRejeicao = motivo,
            DataHora = dataHoraUtc
        };
    }
}
