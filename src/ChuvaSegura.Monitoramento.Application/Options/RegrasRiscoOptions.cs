namespace ChuvaSegura.Monitoramento.Application.Options;

/// <summary>Configuração das regras de risco (seção "RegrasRisco" do appsettings.json).</summary>
public sealed class RegrasRiscoOptions
{
    public const string Secao = "RegrasRisco";

    /// <summary>A partir deste nível (cm) o risco é Atenção.</summary>
    public double AtencaoCm { get; set; } = 30;

    /// <summary>A partir deste nível (cm) o risco é Alerta.</summary>
    public double AlertaCm { get; set; } = 60;

    /// <summary>Depois de quantos minutos sem leitura o dado é considerado desatualizado.</summary>
    public int MinutosParaDadoDesatualizado { get; set; } = 15;
}
