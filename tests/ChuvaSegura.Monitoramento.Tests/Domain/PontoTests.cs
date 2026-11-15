using ChuvaSegura.Monitoramento.Domain.Entities;
using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Domain.Services;
using Xunit;

namespace ChuvaSegura.Monitoramento.Tests.Domain;

public class PontoTests
{
    private static readonly DateTime Agora = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
    private static readonly IClassificadorRisco Classificador = new ClassificadorRiscoPorLimites(30, 60);

    private static Ponto NovoPonto(double altura = 100)
        => new("Ponto Teste", "Ipiranga", -23.58, -46.61, altura, Agora);

    [Fact]
    public void RegistrarLeitura_NivelAcimaDoLimite_ClassificaComoAlerta()
    {
        var ponto = NovoPonto();

        var resultado = ponto.RegistrarLeitura(35, Classificador, Agora);

        Assert.True(resultado.Leitura.Valida);
        Assert.Equal(65.0, resultado.Leitura.NivelAguaCm!.Value);
        Assert.Equal(NivelRisco.Alerta, resultado.Leitura.Risco);
        Assert.True(resultado.RiscoMudou);
        Assert.Equal(NivelRisco.Alerta, ponto.RiscoAtual);
        Assert.Equal(65.0, ponto.UltimoNivelAguaCm!.Value);
        Assert.Equal(Agora, ponto.UltimaLeituraEm);
    }

    [Fact]
    public void RegistrarLeitura_MesmoRiscoDuasVezes_SoMudaNaPrimeira()
    {
        var ponto = NovoPonto();

        var primeira = ponto.RegistrarLeitura(35, Classificador, Agora);
        var segunda = ponto.RegistrarLeitura(30, Classificador, Agora.AddMinutes(1));

        Assert.True(primeira.RiscoMudou);
        Assert.False(segunda.RiscoMudou);
        Assert.Equal(NivelRisco.Alerta, segunda.RiscoAnterior);
    }

    [Fact]
    public void RegistrarLeitura_PrimeiraLeituraNormal_NaoContaComoMudanca()
    {
        var ponto = NovoPonto();

        var resultado = ponto.RegistrarLeitura(90, Classificador, Agora); // nível 10 cm

        Assert.Equal(NivelRisco.Normal, resultado.Leitura.Risco);
        Assert.False(resultado.RiscoMudou);
    }

    [Theory]
    [InlineData(450.0)]
    [InlineData(400.1)]
    [InlineData(1.99)]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RegistrarLeitura_ForaDaFaixaDoSensor_RejeitaENaoAlteraORisco(double distancia)
    {
        var ponto = NovoPonto();

        var resultado = ponto.RegistrarLeitura(distancia, Classificador, Agora);

        Assert.False(resultado.Leitura.Valida);
        Assert.False(resultado.RiscoMudou);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Leitura.MotivoRejeicao));
        Assert.Null(resultado.Leitura.NivelAguaCm);
        Assert.Null(resultado.Leitura.Risco);
        Assert.Equal(NivelRisco.Normal, ponto.RiscoAtual);
        Assert.Null(ponto.UltimaLeituraEm);
    }

    [Theory]
    [InlineData(2.0)]
    [InlineData(400.0)]
    public void RegistrarLeitura_NosLimitesDaFaixa_EhValida(double distancia)
    {
        var ponto = NovoPonto();

        var resultado = ponto.RegistrarLeitura(distancia, Classificador, Agora);

        Assert.True(resultado.Leitura.Valida);
    }

    [Fact]
    public void RegistrarLeitura_DistanciaMaiorQueAAlturaDeInstalacao_NivelFicaZero()
    {
        var ponto = NovoPonto(altura: 100);

        var resultado = ponto.RegistrarLeitura(150, Classificador, Agora);

        Assert.True(resultado.Leitura.Valida);
        Assert.Equal(0.0, resultado.Leitura.NivelAguaCm!.Value);
        Assert.Equal(NivelRisco.Normal, resultado.Leitura.Risco);
    }

    [Fact]
    public void RegistrarLeitura_PontoInativo_LancaConflito()
    {
        var ponto = NovoPonto();
        ponto.Desativar();

        Assert.Throws<ConflitoException>(() => ponto.RegistrarLeitura(35, Classificador, Agora));
    }

    [Fact]
    public void Ativar_AposDesativar_VoltaAAceitarLeituras()
    {
        var ponto = NovoPonto();
        ponto.Desativar();
        ponto.Ativar();

        var resultado = ponto.RegistrarLeitura(35, Classificador, Agora);

        Assert.True(resultado.Leitura.Valida);
    }

    [Theory]
    [InlineData("", "Ipiranga", -23.5, -46.6, 100.0)]
    [InlineData("ab", "Ipiranga", -23.5, -46.6, 100.0)]
    [InlineData("Ponto Válido", "", -23.5, -46.6, 100.0)]
    [InlineData("Ponto Válido", "Ipiranga", -91.0, -46.6, 100.0)]
    [InlineData("Ponto Válido", "Ipiranga", -23.5, 181.0, 100.0)]
    [InlineData("Ponto Válido", "Ipiranga", -23.5, -46.6, 5.0)]
    [InlineData("Ponto Válido", "Ipiranga", -23.5, -46.6, 401.0)]
    public void Construtor_ComDadosInvalidos_LancaDominioException(string nome, string bairro, double lat, double lon, double altura)
    {
        Assert.Throws<DominioException>(() => new Ponto(nome, bairro, lat, lon, altura, Agora));
    }

    [Fact]
    public void Construtor_AparaEspacosDoNomeEDoBairro()
    {
        var ponto = new Ponto("  Ponto Teste  ", "  Ipiranga ", -23.5, -46.6, 100, Agora);

        Assert.Equal("Ponto Teste", ponto.Nome);
        Assert.Equal("Ipiranga", ponto.Bairro);
    }
}
