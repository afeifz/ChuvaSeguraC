using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Domain.Services;
using Microsoft.Extensions.Logging;

namespace ChuvaSegura.Monitoramento.Application.Services;

/// <summary>
/// Caso de uso principal: receber a medição do sensor, classificar o risco, salvar e,
/// quando o risco mudar, avisar o serviço de Cadastro e Alertas.
/// </summary>
public sealed class LeituraService : ILeituraService
{
    private readonly IPontoRepository _pontos;
    private readonly ILeituraRepository _leituras;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClassificadorRisco _classificador;
    private readonly INotificadorAlerta _notificador;
    private readonly TimeProvider _relogio;
    private readonly ILogger<LeituraService> _logger;

    public LeituraService(
        IPontoRepository pontos,
        ILeituraRepository leituras,
        IUnitOfWork unitOfWork,
        IClassificadorRisco classificador,
        INotificadorAlerta notificador,
        TimeProvider relogio,
        ILogger<LeituraService> logger)
    {
        _pontos = pontos;
        _leituras = leituras;
        _unitOfWork = unitOfWork;
        _classificador = classificador;
        _notificador = notificador;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<ResultadoRegistroLeitura> RegistrarAsync(RegistrarLeituraRequest request, CancellationToken ct = default)
    {
        var distancia = request.DistanciaCm
            ?? throw new DominioException("A distância é obrigatória.");

        var ponto = await _pontos.ObterPorIdAsync(request.PontoId, ct)
            ?? throw new NaoEncontradoException($"Ponto {request.PontoId} não encontrado.");

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var resultado = ponto.RegistrarLeitura(distancia, _classificador, agora);

        await _leituras.AdicionarAsync(resultado.Leitura, ct);
        await _unitOfWork.CommitAsync(ct);

        if (resultado.Leitura.Valida)
        {
            _logger.LogInformation(
                "Leitura do ponto {PontoId}: nível {Nivel} cm, risco {Risco}.",
                ponto.Id,
                resultado.Leitura.NivelAguaCm,
                resultado.Leitura.Risco);
        }
        else
        {
            _logger.LogWarning(
                "Leitura inválida no ponto {PontoId}: {Motivo}",
                ponto.Id,
                resultado.Leitura.MotivoRejeicao);
        }

        // Só notificamos quando o risco MUDA (evita enviar o mesmo alerta a cada medição do sensor).
        if (resultado.Leitura.Valida && resultado.RiscoMudou)
        {
            await NotificarMudancaAsync(ponto, resultado.Leitura, ct);
        }

        return new ResultadoRegistroLeitura(resultado.Leitura.ToResponse(), resultado.Leitura.Valida);
    }

    public async Task<PaginaResultado<LeituraResponse>> ListarAsync(
        int pontoId,
        DateTime? de,
        DateTime? ate,
        bool? apenasValidas,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        await GarantirPontoAsync(pontoId, ct);

        var deUtc = de.HasValue ? Mapeamentos.ComoUtc(de.Value) : (DateTime?)null;
        var ateUtc = ate.HasValue ? Mapeamentos.ComoUtc(ate.Value) : (DateTime?)null;

        if (deUtc.HasValue && ateUtc.HasValue && deUtc.Value > ateUtc.Value)
        {
            throw new DominioException("O parâmetro 'de' não pode ser maior que 'ate'.");
        }

        var paginaValida = Math.Max(1, pagina);
        var tamanhoValido = Math.Clamp(tamanho, 1, 100);

        var resultado = await _leituras.ListarPorPontoAsync(pontoId, deUtc, ateUtc, apenasValidas, paginaValida, tamanhoValido, ct);
        return resultado.Mapear(l => l.ToResponse());
    }

    public async Task<EstatisticasPontoResponse> ObterEstatisticasAsync(int pontoId, int horas, CancellationToken ct = default)
    {
        await GarantirPontoAsync(pontoId, ct);

        var janela = Math.Clamp(horas, 1, 24 * 30);
        var desde = _relogio.GetUtcNow().UtcDateTime.AddHours(-janela);

        var estatisticas = await _leituras.ObterEstatisticasAsync(pontoId, desde, ct);

        // Garante as três chaves na resposta, mesmo quando não houve leitura naquele risco.
        var porRisco = Enum.GetValues<NivelRisco>()
            .ToDictionary(r => r.ToString(), r => estatisticas.PorRisco.GetValueOrDefault(r));

        return new EstatisticasPontoResponse(
            pontoId,
            desde,
            janela,
            estatisticas.LeiturasValidas,
            estatisticas.LeiturasInvalidas,
            estatisticas.NivelMaximoCm,
            estatisticas.NivelMedioCm,
            porRisco);
    }

    private async Task GarantirPontoAsync(int pontoId, CancellationToken ct)
    {
        if (!await _pontos.ExisteAsync(pontoId, ct))
        {
            throw new NaoEncontradoException($"Ponto {pontoId} não encontrado.");
        }
    }

    private async Task NotificarMudancaAsync(Ponto ponto, Leitura leitura, CancellationToken ct)
    {
        var notificacao = new NotificacaoRisco(
            ponto.Id,
            ponto.Bairro,
            leitura.Risco!.Value,
            leitura.NivelAguaCm!.Value,
            leitura.DataHora);

        try
        {
            await _notificador.NotificarAsync(notificacao, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A leitura já foi salva. Uma falha no serviço de alertas não pode derrubar a ingestão do sensor.
            _logger.LogWarning(ex, "Não foi possível notificar o serviço de alertas sobre o ponto {PontoId}.", ponto.Id);
        }
    }
}
