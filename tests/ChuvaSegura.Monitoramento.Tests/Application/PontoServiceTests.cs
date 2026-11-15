using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Services;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Tests.Fakes;
using Xunit;

namespace ChuvaSegura.Monitoramento.Tests.Application;

public class PontoServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePontoRepository _pontos = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly PontoService _servico;

    public PontoServiceTests()
    {
        _servico = new PontoService(_pontos, _unitOfWork, new RelogioFixo(Agora));
    }

    private static SalvarPontoRequest Request(string nome, string bairro = "Ipiranga")
        => new() { Nome = nome, Bairro = bairro, Latitude = -23.58, Longitude = -46.61, AlturaInstalacaoCm = 100 };

    [Fact]
    public async Task CriarAsync_DadosValidos_SalvaERetornaOPonto()
    {
        var criado = await _servico.CriarAsync(Request("Ponto A"));

        Assert.True(criado.Id > 0);
        Assert.Equal("Ponto A", criado.Nome);
        Assert.True(criado.Ativo);
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task CriarAsync_NomeDuplicado_LancaConflito()
    {
        await _servico.CriarAsync(Request("Ponto A"));

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.CriarAsync(Request("  ponto a ")));
    }

    [Fact]
    public async Task AtualizarAsync_NomeDeOutroPonto_LancaConflito()
    {
        await _servico.CriarAsync(Request("Ponto A"));
        var segundo = await _servico.CriarAsync(Request("Ponto B"));

        await Assert.ThrowsAsync<ConflitoException>(() => _servico.AtualizarAsync(segundo.Id, Request("Ponto A")));
    }

    [Fact]
    public async Task AtualizarAsync_MantendoOMesmoNome_Funciona()
    {
        var criado = await _servico.CriarAsync(Request("Ponto A"));

        var atualizado = await _servico.AtualizarAsync(criado.Id, Request("Ponto A", "Mooca"));

        Assert.Equal("Mooca", atualizado.Bairro);
    }

    [Fact]
    public async Task ObterPorIdAsync_Inexistente_LancaNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ObterPorIdAsync(123));
    }

    [Fact]
    public async Task RemoverAsync_Existente_RemoveDoRepositorio()
    {
        var criado = await _servico.CriarAsync(Request("Ponto A"));

        await _servico.RemoverAsync(criado.Id);

        Assert.Empty(_pontos.Itens);
    }

    [Fact]
    public async Task AlterarStatusAsync_Desativa()
    {
        var criado = await _servico.CriarAsync(Request("Ponto A"));

        var resposta = await _servico.AlterarStatusAsync(criado.Id, false);

        Assert.False(resposta.Ativo);
    }

    [Fact]
    public async Task ListarAsync_NormalizaPaginacaoInvalida()
    {
        await _servico.CriarAsync(Request("Ponto A"));

        var pagina = await _servico.ListarAsync(null, null, pagina: -3, tamanho: 0);

        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(1, pagina.TamanhoPagina);
        Assert.Equal(1, pagina.Total);
    }
}
