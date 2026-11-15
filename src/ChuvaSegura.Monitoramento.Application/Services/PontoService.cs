using ChuvaSegura.Monitoramento.Application.Abstractions;
using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Exceptions;

namespace ChuvaSegura.Monitoramento.Application.Services;

/// <summary>Casos de uso de cadastro dos pontos de monitoramento.</summary>
public sealed class PontoService : IPontoService
{
    private readonly IPontoRepository _pontos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _relogio;

    public PontoService(IPontoRepository pontos, IUnitOfWork unitOfWork, TimeProvider relogio)
    {
        _pontos = pontos;
        _unitOfWork = unitOfWork;
        _relogio = relogio;
    }

    public async Task<PaginaResultado<PontoResponse>> ListarAsync(string? bairro, bool? ativo, int pagina, int tamanho, CancellationToken ct = default)
    {
        var paginaValida = Math.Max(1, pagina);
        var tamanhoValido = Math.Clamp(tamanho, 1, 100);

        var resultado = await _pontos.ListarAsync(bairro, ativo, paginaValida, tamanhoValido, ct);
        return resultado.Mapear(p => p.ToResponse());
    }

    public async Task<PontoResponse> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);
        return ponto.ToResponse();
    }

    public async Task<PontoResponse> CriarAsync(SalvarPontoRequest request, CancellationToken ct = default)
    {
        if (await _pontos.ExisteComNomeAsync(request.Nome, null, ct))
        {
            throw new ConflitoException($"Já existe um ponto com o nome '{request.Nome.Trim()}'.");
        }

        var ponto = new Ponto(
            request.Nome,
            request.Bairro,
            request.Latitude,
            request.Longitude,
            request.AlturaInstalacaoCm,
            _relogio.GetUtcNow().UtcDateTime);

        await _pontos.AdicionarAsync(ponto, ct);
        await _unitOfWork.CommitAsync(ct);

        return ponto.ToResponse();
    }

    public async Task<PontoResponse> AtualizarAsync(int id, SalvarPontoRequest request, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);

        if (await _pontos.ExisteComNomeAsync(request.Nome, id, ct))
        {
            throw new ConflitoException($"Já existe um ponto com o nome '{request.Nome.Trim()}'.");
        }

        ponto.Atualizar(request.Nome, request.Bairro, request.Latitude, request.Longitude, request.AlturaInstalacaoCm);
        await _unitOfWork.CommitAsync(ct);

        return ponto.ToResponse();
    }

    public async Task<PontoResponse> AlterarStatusAsync(int id, bool ativo, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);

        if (ativo)
        {
            ponto.Ativar();
        }
        else
        {
            ponto.Desativar();
        }

        await _unitOfWork.CommitAsync(ct);
        return ponto.ToResponse();
    }

    public async Task RemoverAsync(int id, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);
        _pontos.Remover(ponto);
        await _unitOfWork.CommitAsync(ct);
    }

    private async Task<Ponto> BuscarAsync(int id, CancellationToken ct)
    {
        return await _pontos.ObterPorIdAsync(id, ct)
            ?? throw new NaoEncontradoException($"Ponto {id} não encontrado.");
    }
}
