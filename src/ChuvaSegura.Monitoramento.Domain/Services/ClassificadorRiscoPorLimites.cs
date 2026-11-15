using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;

namespace ChuvaSegura.Monitoramento.Domain.Services;

/// <summary>
/// Regra padrão: Normal abaixo do limite de atenção, Atenção entre os limites e Alerta a partir do limite de alerta.
/// Valores de referência: atenção = 30 cm, alerta = 60 cm (configuráveis).
/// </summary>
public sealed class ClassificadorRiscoPorLimites : IClassificadorRisco
{
    public ClassificadorRiscoPorLimites(double limiteAtencaoCm, double limiteAlertaCm)
    {
        if (double.IsNaN(limiteAtencaoCm) || limiteAtencaoCm <= 0)
        {
            throw new DominioException("O limite de atenção deve ser maior que zero.");
        }

        if (double.IsNaN(limiteAlertaCm) || limiteAlertaCm <= limiteAtencaoCm)
        {
            throw new DominioException("O limite de alerta deve ser maior que o limite de atenção.");
        }

        LimiteAtencaoCm = limiteAtencaoCm;
        LimiteAlertaCm = limiteAlertaCm;
    }

    public double LimiteAtencaoCm { get; }

    public double LimiteAlertaCm { get; }

    public NivelRisco Classificar(double nivelAguaCm)
    {
        if (nivelAguaCm >= LimiteAlertaCm)
        {
            return NivelRisco.Alerta;
        }

        if (nivelAguaCm >= LimiteAtencaoCm)
        {
            return NivelRisco.Atencao;
        }

        return NivelRisco.Normal;
    }
}
