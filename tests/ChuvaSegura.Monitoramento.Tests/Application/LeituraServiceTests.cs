using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Services;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Domain.Services;
using ChuvaSegura.Monitoramento.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ChuvaSegura.Monitoramento.Tests.Application;

public class LeituraServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePontoRepository _pontos = new();
    private readonly FakeLeituraRepository _leituras = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeNotificador _notificador = new();
    private readonly LeituraService _servico;
    private readonly Ponto _ponto;

    public LeituraServiceTests()
    {
        _ponto = new Ponto("Ponto Teste", "Ipiranga", -23.58, -46.61, 100, Agora.UtcDateTime);
        _pontos.AdicionarAsync(_ponto).GetAwaiter().GetResult();

        _servico = new LeituraService(
            _pontos,
            _leituras,
            _unitOfWork,
            new ClassificadorRiscoPorLimites(30, 60),
            _notificador,
            new RelogioFixo(Agora),
            NullLogger<LeituraService>.Instance);
    }

    private static RegistrarLeituraRequest Leitura(int pontoId, double distancia)
        => new() { PontoId = pontoId, DistanciaCm = distancia };

    [Fact]
    public async Task RegistrarAsync_MudancaParaAlerta_SalvaENotificaUmaVez()
    {
        var resultado = await _servico.RegistrarAsync(Leitura(_ponto.Id, 35));

        Assert.True(resultado.Aceita);
        Assert.Equal(NivelRisco.Alerta, resultado.Leitura.Risco);
        Assert.Single(_leituras.Itens);
        Assert.Equal(1, _unitOfWork.Commits);

        var notificacao = Assert.Single(_notificador.Enviadas);
        Assert.Equal(_ponto.Id, notificacao.PontoId);
        Assert.Equal("Ipiranga", notificacao.Bairro);
        Assert.Equal(NivelRisco.Alerta, notificacao.Risco);
        Assert.Equal(65.0, notificacao.NivelAguaCm);
    }

    [Fact]
    public async Task RegistrarAsync_MesmoRiscoVariasVezes_NotificaApenasNaMudanca()
    {
        await _servico.RegistrarAsync(Leitura(_ponto.Id, 35)); // Normal -> Alerta (notifica)
        await _servico.RegistrarAsync(Leitura(_ponto.Id, 30)); // continua Alerta (não notifica)
        await _servico.RegistrarAsync(Leitura(_ponto.Id, 90)); // Alerta -> Normal (notifica)

        Assert.Equal(3, _leituras.Itens.Count);
        Assert.Equal(2, _notificador.Enviadas.Count);
    }

    [Fact]
    public async Task RegistrarAsync_LeituraInvalida_GuardaMasNaoNotificaENaoAlteraORisco()
    {
        var resultado = await _servico.RegistrarAsync(Leitura(_ponto.Id, 450));

        Assert.False(resultado.Aceita);
        Assert.False(resultado.Leitura.Valida);
        Assert.Single(_leituras.Itens);
        Assert.Empty(_notificador.Enviadas);
        Assert.Equal(NivelRisco.Normal, _ponto.RiscoAtual);
    }

    [Fact]
    public async Task RegistrarAsync_FalhaNaNotificacao_MantemALeituraSalva()
    {
        _notificador.Falhar = true;

        var resultado = await _servico.RegistrarAsync(Leitura(_ponto.Id, 35));

        Assert.True(resultado.Aceita);
        Assert.Single(_leituras.Itens);
        Assert.Equal(NivelRisco.Alerta, _ponto.RiscoAtual);
    }

    [Fact]
    public async Task RegistrarAsync_PontoInexistente_LancaNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.RegistrarAsync(Leitura(999, 35)));
    }

    [Fact]
    public async Task RegistrarAsync_PontoInativo_LancaConflito()
    {
        _ponto.Desativar();

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.RegistrarAsync(Leitura(_ponto.Id, 35)));
    }

    [Fact]
    public async Task RegistrarAsync_SemDistancia_LancaDominioException()
    {
        var request = new RegistrarLeituraRequest { PontoId = _ponto.Id, DistanciaCm = null };

        await Assert.ThrowsAsync<DominioException>(() => _servico.RegistrarAsync(request));
    }

    [Fact]
    public async Task ObterEstatisticasAsync_IncluiAsTresChavesDeRisco()
    {
        await _servico.RegistrarAsync(Leitura(_ponto.Id, 35));

        var estatisticas = await _servico.ObterEstatisticasAsync(_ponto.Id, 24);

        Assert.Equal(1, estatisticas.LeiturasValidas);
        Assert.Equal(65.0, estatisticas.NivelMaximoCm!.Value);
        Assert.Equal(1, estatisticas.LeiturasPorRisco["Alerta"]);
        Assert.Equal(0, estatisticas.LeiturasPorRisco["Normal"]);
        Assert.Equal(0, estatisticas.LeiturasPorRisco["Atencao"]);
    }

    [Fact]
    public async Task ListarAsync_PeriodoInvertido_LancaDominioException()
    {
        var de = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc);
        var ate = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<DominioException>(
            () => _servico.ListarAsync(_ponto.Id, de, ate, null, 1, 20));
    }
}
