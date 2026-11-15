using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChuvaSegura.Monitoramento.Api.Controllers;

/// <summary>Consultas de risco por bairro (usadas pelo aplicativo do cidadão).</summary>
[ApiController]
[Route("api/bairros")]
[Produces("application/json")]
public class BairrosController : ControllerBase
{
    private readonly IRiscoService _risco;

    public BairrosController(IRiscoService risco)
    {
        _risco = risco;
    }

    /// <summary>Risco do bairro: o maior risco entre os pontos ativos dele.</summary>
    /// <param name="bairro">Nome do bairro (sem diferenciar maiúsculas de minúsculas).</param>
    [HttpGet("{bairro}/risco")]
    [ProducesResponseType(typeof(RiscoBairroResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RiscoBairroResponse>> ObterRisco(string bairro, CancellationToken ct)
    {
        return Ok(await _risco.ObterDoBairroAsync(bairro, ct));
    }
}
