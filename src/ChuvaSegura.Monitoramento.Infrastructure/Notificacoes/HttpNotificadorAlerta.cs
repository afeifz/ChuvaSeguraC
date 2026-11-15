using System.Net.Http.Json;
using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChuvaSegura.Monitoramento.Infrastructure.Notificacoes;

/// <summary>
/// Chama o serviço de Cadastro e Alertas (Java) por HTTP quando o risco de um ponto muda.
/// Contrato: POST {BaseUrl}{Caminho} com JSON
/// { "pontoId", "bairro", "risco" (NORMAL|ATENCAO|ALERTA), "nivelAguaCm", "dataHoraUtc" }.
/// </summary>
public sealed class HttpNotificadorAlerta : INotificadorAlerta
{
    private readonly HttpClient _httpClient;
    private readonly AlertasServiceOptions _opcoes;
    private readonly ILogger<HttpNotificadorAlerta> _logger;

    public HttpNotificadorAlerta(
        HttpClient httpClient,
        IOptions<AlertasServiceOptions> opcoes,
        ILogger<HttpNotificadorAlerta> logger)
    {
        _httpClient = httpClient;
        _opcoes = opcoes.Value;
        _logger = logger;
    }

    public async Task NotificarAsync(NotificacaoRisco notificacao, CancellationToken ct = default)
    {
        if (!_opcoes.Habilitado)
        {
            _logger.LogInformation(
                "Integração com o serviço de alertas desabilitada. Ponto {PontoId} mudou para {Risco}.",
                notificacao.PontoId,
                notificacao.Risco);
            return;
        }

        var corpo = new
        {
            pontoId = notificacao.PontoId,
            bairro = notificacao.Bairro,
            risco = notificacao.Risco.ToString().ToUpperInvariant(),
            nivelAguaCm = notificacao.NivelAguaCm,
            dataHoraUtc = notificacao.DataHoraUtc
        };

        using var resposta = await _httpClient.PostAsJsonAsync(_opcoes.Caminho, corpo, ct);
        resposta.EnsureSuccessStatusCode();
    }
}
