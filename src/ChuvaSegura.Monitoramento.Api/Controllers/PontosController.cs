using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Models;
using ChuvaSegura.Monitoramento.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChuvaSegura.Monitoramento.Api.Controllers;

/// <summary>Cadastro dos pontos de monitoramento e consultas por ponto.</summary>
[ApiController]
[Route("api/pontos")]
[Produces("application/json")]
public class PontosController : ControllerBase
{
    private readonly IPontoService _pontos;
    private readonly ILeituraService _leituras;
    private readonly IRiscoService _risco;

    public PontosController(IPontoService pontos, ILeituraService leituras, IRiscoService risco)
    {
        _pontos = pontos;
        _leituras = leituras;
        _risco = risco;
    }

    /// <summary>Lista os pontos (paginado), com filtros opcionais por bairro e status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaResultado<PontoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<PontoResponse>>> Listar(
        [FromQuery] string? bairro,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        return Ok(await _pontos.ListarAsync(bairro, ativo, pagina, tamanho, ct));
    }

    /// <summary>Obtém um ponto pelo id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PontoResponse>> ObterPorId(int id, CancellationToken ct)
    {
        return Ok(await _pontos.ObterPorIdAsync(id, ct));
    }

    /// <summary>Cadastra um novo ponto de monitoramento.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PontoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PontoResponse>> Criar([FromBody] SalvarPontoRequest request, CancellationToken ct)
    {
        var criado = await _pontos.CriarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
    }

    /// <summary>Atualiza os dados de um ponto.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PontoResponse>> Atualizar(int id, [FromBody] SalvarPontoRequest request, CancellationToken ct)
    {
        return Ok(await _pontos.AtualizarAsync(id, request, ct));
    }

    /// <summary>Ativa um ponto (volta a aceitar leituras).</summary>
    [HttpPost("{id:int}/ativar")]
    [ProducesResponseType(typeof(PontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PontoResponse>> Ativar(int id, CancellationToken ct)
    {
        return Ok(await _pontos.AlterarStatusAsync(id, true, ct));
    }

    /// <summary>Desativa um ponto (deixa de aceitar leituras).</summary>
    [HttpPost("{id:int}/desativar")]
    [ProducesResponseType(typeof(PontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PontoResponse>> Desativar(int id, CancellationToken ct)
    {
        return Ok(await _pontos.AlterarStatusAsync(id, false, ct));
    }

    /// <summary>Remove um ponto e todo o seu histórico de leituras.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await _pontos.RemoverAsync(id, ct);
        return NoContent();
    }

    /// <summary>Risco atual de um ponto, com orientação em linguagem simples.</summary>
    [HttpGet("{id:int}/risco")]
    [ProducesResponseType(typeof(RiscoPontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RiscoPontoResponse>> ObterRisco(int id, CancellationToken ct)
    {
        return Ok(await _risco.ObterDoPontoAsync(id, ct));
    }

    /// <summary>Histórico de leituras de um ponto (mais recentes primeiro), paginado.</summary>
    /// <param name="id">Id do ponto.</param>
    /// <param name="de">Início do período (UTC, opcional).</param>
    /// <param name="ate">Fim do período (UTC, opcional).</param>
    /// <param name="apenasValidas">Quando verdadeiro, omite leituras rejeitadas.</param>
    [HttpGet("{id:int}/leituras")]
    [ProducesResponseType(typeof(PaginaResultado<LeituraResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaResultado<LeituraResponse>>> ListarLeituras(
        int id,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] bool? apenasValidas,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default)
    {
        return Ok(await _leituras.ListarAsync(id, de, ate, apenasValidas, pagina, tamanho, ct));
    }

    /// <summary>Estatísticas das leituras (nível máximo, médio e contagem por risco) em uma janela de horas.</summary>
    /// <param name="id">Id do ponto.</param>
    /// <param name="horas">Janela em horas (1 a 720). Padrão: 24.</param>
    [HttpGet("{id:int}/estatisticas")]
    [ProducesResponseType(typeof(EstatisticasPontoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstatisticasPontoResponse>> ObterEstatisticas(
        int id,
        [FromQuery] int horas = 24,
        CancellationToken ct = default)
    {
        return Ok(await _leituras.ObterEstatisticasAsync(id, horas, ct));
    }
}
