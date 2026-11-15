using ChuvaSegura.Monitoramento.Domain.Enums;

namespace ChuvaSegura.Monitoramento.Application;

/// <summary>Textos curtos e simples para o cidadão (o cidadão real não é especialista).</summary>
public static class TextosRisco
{
    public static string Orientacao(NivelRisco risco)
    {
        return risco switch
        {
            NivelRisco.Alerta => "Risco alto de alagamento. Proteja seus pertences e evite sair pela rua.",
            NivelRisco.Atencao => "O nível da água está subindo. Fique atento e prepare-se.",
            _ => "Sem risco de alagamento no momento. Continue acompanhando."
        };
    }
}
