namespace ChuvaSegura.Monitoramento.Infrastructure.Notificacoes;

/// <summary>Configuração da integração com o serviço de Cadastro e Alertas (Java Spring Boot).</summary>
public sealed class AlertasServiceOptions
{
    public const string Secao = "AlertasService";

    /// <summary>Quando falso, as notificações são apenas registradas em log (útil sem o serviço Java no ar).</summary>
    public bool Habilitado { get; set; }

    public string BaseUrl { get; set; } = "http://localhost:8080";

    public string Caminho { get; set; } = "/api/alertas/notificacoes";

    public int TimeoutSegundos { get; set; } = 5;
}
