using ChuvaSegura.Monitoramento.Application.Options;
using ChuvaSegura.Monitoramento.Application.Services;
using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Domain.Services;
using ChuvaSegura.Monitoramento.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChuvaSegura.Monitoramento.Tests.Application;

public class RiscoServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly IClassificadorRisco Classificador = new ClassificadorRiscoPorLimites(30, 60);

    private readonly FakePontoRepository _pontos = new();
    private readonly RelogioFixo _relogio = new(Agora);
    private readonly RiscoService _servico;

    public RiscoServiceTests()
    {
        var opcoes = Microsoft.Extensions.Options.Options.Create(new RegrasRiscoOptions { MinutosParaDadoDesatualizado = 15 });
        _servico = new RiscoService(_pontos, opcoes, _relogio);
    }

    private Ponto AdicionarPonto(string nome, string bairro = "Ipiranga")
    {
        var ponto = new Ponto(nome, bairro, -23.58, -46.61, 100, Agora.UtcDateTime);
        _pontos.AdicionarAsync(ponto).GetAwaiter().GetResult();
        return ponto;
    }

    [Fact]
    public async Task ObterDoBairroAsync_SemPontosAtivos_LancaNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterDoBairroAsync("Bairro Vazio"));
    }

    [Fact]
    public async Task ObterDoBairroAsync_RetornaOMaiorRiscoEntreOsPontos()
    {
        var a = AdicionarPonto("Ponto A");
        var b = AdicionarPonto("Ponto B");
        a.RegistrarLeitura(90, Classificador, Agora.UtcDateTime); // nível 10 -> Normal
        b.RegistrarLeitura(35, Classificador, Agora.UtcDateTime); // nível 65 -> Alerta

        var resposta = await _servico.ObterDoBairroAsync("ipiranga");

        Assert.Equal(NivelRisco.Alerta, resposta.Risco);
        Assert.Equal(2, resposta.Pontos.Count);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Orientacao));
    }

    [Fact]
    public async Task ObterDoBairroAsync_IgnoraPontosInativos()
    {
        var a = AdicionarPonto("Ponto A");
        var b = AdicionarPonto("Ponto B");
        b.RegistrarLeitura(35, Classificador, Agora.UtcDateTime); // Alerta
        b.Desativar();
        a.RegistrarLeitura(90, Classificador, Agora.UtcDateTime); // Normal

        var resposta = await _servico.ObterDoBairroAsync("Ipiranga");

        Assert.Equal(NivelRisco.Normal, resposta.Risco);
        Assert.Single(resposta.Pontos);
    }

    [Fact]
    public async Task ObterDoPontoAsync_SemLeituras_EstaDesatualizado()
    {
        var ponto = AdicionarPonto("Ponto A");

        var resposta = await _servico.ObterDoPontoAsync(ponto.Id);

        Assert.True(resposta.Desatualizado);
        Assert.Equal(NivelRisco.Normal, resposta.Risco);
    }

    [Fact]
    public async Task ObterDoPontoAsync_LeituraRecente_NaoEstaDesatualizado()
    {
        var ponto = AdicionarPonto("Ponto A");
        ponto.RegistrarLeitura(35, Classificador, Agora.UtcDateTime);

        var resposta = await _servico.ObterDoPontoAsync(ponto.Id);

        Assert.False(resposta.Desatualizado);
        Assert.Equal(NivelRisco.Alerta, resposta.Risco);
    }

    [Fact]
    public async Task ObterDoPontoAsync_LeituraAntiga_FicaDesatualizado()
    {
        var ponto = AdicionarPonto("Ponto A");
        ponto.RegistrarLeitura(35, Classificador, Agora.UtcDateTime);
        _relogio.Avancar(TimeSpan.FromMinutes(20));

        var resposta = await _servico.ObterDoPontoAsync(ponto.Id);

        Assert.True(resposta.Desatualizado);
    }

    [Fact]
    public async Task ObterDoPontoAsync_Inexistente_LancaNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterDoPontoAsync(404));
    }
}
