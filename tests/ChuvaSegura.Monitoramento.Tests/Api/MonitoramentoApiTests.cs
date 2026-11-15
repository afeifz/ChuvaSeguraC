using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Domain.Enums;
using Xunit;

namespace ChuvaSegura.Monitoramento.Tests.Api;

/// <summary>Testes de integração (caixa-preta): chamam os endpoints HTTP de verdade, com banco SQLite.</summary>
public class MonitoramentoApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public MonitoramentoApiTests(ApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
    }

    private async Task<PontoResponse> CriarPontoAsync(string? bairro = null, double altura = 100)
    {
        var request = new SalvarPontoRequest
        {
            Nome = $"Ponto {Guid.NewGuid():N}",
            Bairro = bairro ?? $"Bairro{Guid.NewGuid():N}",
            Latitude = -23.58,
            Longitude = -46.61,
            AlturaInstalacaoCm = altura
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/pontos", request, Json);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return (await resposta.Content.ReadFromJsonAsync<PontoResponse>(Json))!;
    }

    private Task<HttpResponseMessage> EnviarLeituraAsync(int pontoId, double distancia)
        => _cliente.PostAsJsonAsync("/api/leituras", new { pontoId, distanciaCm = distancia }, Json);

    [Fact]
    public async Task Health_RetornaOk()
    {
        var resposta = await _cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarPonto_DadosValidos_Retorna201ComLocation()
    {
        var ponto = await CriarPontoAsync();

        var resposta = await _cliente.GetAsync($"/api/pontos/{ponto.Id}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var lido = await resposta.Content.ReadFromJsonAsync<PontoResponse>(Json);
        Assert.Equal(ponto.Nome, lido!.Nome);
        Assert.Equal(NivelRisco.Normal, lido.RiscoAtual);
    }

    [Fact]
    public async Task CriarPonto_SemNome_Retorna400()
    {
        var corpo = new { nome = "", bairro = "Ipiranga", latitude = -23.5, longitude = -46.6, alturaInstalacaoCm = 100 };

        var resposta = await _cliente.PostAsJsonAsync("/api/pontos", corpo, Json);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarPonto_NomeDuplicado_Retorna409()
    {
        var ponto = await CriarPontoAsync();
        var duplicado = new SalvarPontoRequest
        {
            Nome = ponto.Nome,
            Bairro = "Ipiranga",
            Latitude = -23.5,
            Longitude = -46.6,
            AlturaInstalacaoCm = 100
        };

        var resposta = await _cliente.PostAsJsonAsync("/api/pontos", duplicado, Json);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task ObterPonto_Inexistente_Retorna404()
    {
        var resposta = await _cliente.GetAsync("/api/pontos/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Leitura_NivelDeAlerta_Retorna201_AtualizaORiscoENotifica()
    {
        var ponto = await CriarPontoAsync();

        var resposta = await EnviarLeituraAsync(ponto.Id, 35); // nível 65 cm

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var leitura = await resposta.Content.ReadFromJsonAsync<LeituraResponse>(Json);
        Assert.True(leitura!.Valida);
        Assert.Equal(65.0, leitura.NivelAguaCm!.Value);
        Assert.Equal(NivelRisco.Alerta, leitura.Risco);

        var risco = await _cliente.GetFromJsonAsync<RiscoPontoResponse>($"/api/pontos/{ponto.Id}/risco", Json);
        Assert.Equal(NivelRisco.Alerta, risco!.Risco);
        Assert.False(risco.Desatualizado);

        var notificacao = Assert.Single(_fabrica.Notificador.Enviadas, n => n.PontoId == ponto.Id);
        Assert.Equal(NivelRisco.Alerta, notificacao.Risco);
    }

    [Fact]
    public async Task Leitura_ForaDaFaixa_Retorna422_ENaoAlteraORisco()
    {
        var ponto = await CriarPontoAsync();

        var resposta = await EnviarLeituraAsync(ponto.Id, 450);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);

        var risco = await _cliente.GetFromJsonAsync<RiscoPontoResponse>($"/api/pontos/{ponto.Id}/risco", Json);
        Assert.Equal(NivelRisco.Normal, risco!.Risco);
        Assert.DoesNotContain(_fabrica.Notificador.Enviadas, n => n.PontoId == ponto.Id);

        // A leitura rejeitada fica registrada para auditoria.
        var historico = await _cliente.GetFromJsonAsync<PaginaResultado<LeituraResponse>>(
            $"/api/pontos/{ponto.Id}/leituras", Json);
        var registrada = Assert.Single(historico!.Itens);
        Assert.False(registrada.Valida);
    }

    [Fact]
    public async Task Leitura_PontoInexistente_Retorna404()
    {
        var resposta = await EnviarLeituraAsync(999999, 35);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Leitura_SemDistancia_Retorna400()
    {
        var ponto = await CriarPontoAsync();

        var resposta = await _cliente.PostAsJsonAsync("/api/leituras", new { pontoId = ponto.Id }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Leitura_PontoInativo_Retorna409()
    {
        var ponto = await CriarPontoAsync();
        var desativar = await _cliente.PostAsync($"/api/pontos/{ponto.Id}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var resposta = await EnviarLeituraAsync(ponto.Id, 35);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task RiscoDoBairro_RetornaOMaiorRiscoEntreOsPontos()
    {
        var bairro = $"Bairro{Guid.NewGuid():N}";
        var a = await CriarPontoAsync(bairro);
        var b = await CriarPontoAsync(bairro);
        await EnviarLeituraAsync(a.Id, 90); // nível 10 -> Normal
        await EnviarLeituraAsync(b.Id, 55); // nível 45 -> Atenção

        var risco = await _cliente.GetFromJsonAsync<RiscoBairroResponse>($"/api/bairros/{bairro}/risco", Json);

        Assert.Equal(NivelRisco.Atencao, risco!.Risco);
        Assert.Equal(2, risco.Pontos.Count);
    }

    [Fact]
    public async Task RiscoDoBairro_SemPontos_Retorna404()
    {
        var resposta = await _cliente.GetAsync("/api/bairros/BairroQueNaoExiste/risco");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Estatisticas_CalculaMaximoMediaEContagemPorRisco()
    {
        var ponto = await CriarPontoAsync();
        await EnviarLeituraAsync(ponto.Id, 90); // 10 cm  -> Normal
        await EnviarLeituraAsync(ponto.Id, 50); // 50 cm  -> Atenção
        await EnviarLeituraAsync(ponto.Id, 30); // 70 cm  -> Alerta
        await EnviarLeituraAsync(ponto.Id, 450); // inválida

        var estatisticas = await _cliente.GetFromJsonAsync<EstatisticasPontoResponse>(
            $"/api/pontos/{ponto.Id}/estatisticas?horas=24", Json);

        Assert.Equal(3, estatisticas!.LeiturasValidas);
        Assert.Equal(1, estatisticas.LeiturasInvalidas);
        Assert.Equal(70.0, estatisticas.NivelMaximoCm!.Value, 3);
        Assert.Equal(43.333, estatisticas.NivelMedioCm!.Value, 3);
        Assert.Equal(1, estatisticas.LeiturasPorRisco["Normal"]);
        Assert.Equal(1, estatisticas.LeiturasPorRisco["Atencao"]);
        Assert.Equal(1, estatisticas.LeiturasPorRisco["Alerta"]);
    }

    [Fact]
    public async Task ListarLeituras_RetornaMaisRecentesPrimeiroEPagina()
    {
        var ponto = await CriarPontoAsync();
        for (var i = 0; i < 5; i++)
        {
            await EnviarLeituraAsync(ponto.Id, 90 - i);
        }

        var pagina = await _cliente.GetFromJsonAsync<PaginaResultado<LeituraResponse>>(
            $"/api/pontos/{ponto.Id}/leituras?pagina=1&tamanho=2", Json);

        Assert.Equal(5, pagina!.Total);
        Assert.Equal(2, pagina.Itens.Count);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.True(pagina.Itens[0].DataHora >= pagina.Itens[1].DataHora);
    }

    [Fact]
    public async Task RemoverPonto_ApagaOPontoEAsLeituras()
    {
        var ponto = await CriarPontoAsync();
        await EnviarLeituraAsync(ponto.Id, 35);

        var remocao = await _cliente.DeleteAsync($"/api/pontos/{ponto.Id}");
        var consulta = await _cliente.GetAsync($"/api/pontos/{ponto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }
}
