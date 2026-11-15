using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Options;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace ChuvaSegura.Monitoramento.Application.Services;

/// <summary>Consultas de risco usadas pelo aplicativo do cidadão.</summary>
public sealed class RiscoService : IRiscoService
{
    private readonly IPontoRepository _pontos;
    private readonly RegrasRiscoOptions _opcoes;
    private readonly TimeProvider _relogio;

    public RiscoService(IPontoRepository pontos, IOptions<RegrasRiscoOptions> opcoes, TimeProvider relogio)
    {
        _pontos = pontos;
        _opcoes = opcoes.Value;
        _relogio = relogio;
    }

    public async Task<RiscoPontoResponse> ObterDoPontoAsync(int pontoId, CancellationToken ct = default)
    {
        var ponto = await _pontos.ObterPorIdAsync(pontoId, ct)
            ?? throw new NaoEncontradoException($"Ponto {pontoId} não encontrado.");

        return Montar(ponto, _relogio.GetUtcNow().UtcDateTime);
    }

    public async Task<RiscoBairroResponse> ObterDoBairroAsync(string bairro, CancellationToken ct = default)
    {
        var pontos = await _pontos.ListarAtivosPorBairroAsync(bairro, ct);
        if (pontos.Count == 0)
        {
            throw new NaoEncontradoException($"Nenhum ponto ativo encontrado no bairro '{bairro}'.");
        }

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var detalhes = pontos.Select(p => Montar(p, agora)).ToList();

        // O risco do bairro é o pior risco entre os seus pontos ativos.
        var maiorRisco = detalhes.Max(d => d.Risco);

        return new RiscoBairroResponse(
            pontos[0].Bairro,
            maiorRisco,
            TextosRisco.Orientacao(maiorRisco),
            detalhes.Any(d => d.Desatualizado),
            detalhes);
    }

    private RiscoPontoResponse Montar(Ponto ponto, DateTime agoraUtc)
    {
        var limite = TimeSpan.FromMinutes(_opcoes.MinutosParaDadoDesatualizado);
        var desatualizado = ponto.UltimaLeituraEm is null || agoraUtc - ponto.UltimaLeituraEm.Value > limite;

        return new RiscoPontoResponse(
            ponto.Id,
            ponto.Nome,
            ponto.Bairro,
            ponto.RiscoAtual,
            TextosRisco.Orientacao(ponto.RiscoAtual),
            ponto.UltimoNivelAguaCm,
            ponto.UltimaLeituraEm.HasValue ? Mapeamentos.ComoUtc(ponto.UltimaLeituraEm.Value) : null,
            desatualizado);
    }
}
