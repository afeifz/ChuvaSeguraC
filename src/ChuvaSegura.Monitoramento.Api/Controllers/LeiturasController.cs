using ChuvaSegura.Monitoramento.Application.Dtos;
using ChuvaSegura.Monitoramento.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChuvaSegura.Monitoramento.Api.Controllers;

/// <summary>Entrada de dados dos sensores (ESP32).</summary>
[ApiController]
[Route("api/leituras")]
[Produces("application/json")]
public class LeiturasController : ControllerBase
{
    private readonly ILeituraService _leituras;

    public LeiturasController(ILeituraService leituras)
    {
        _leituras = leituras;
    }

    /// <summary>Registra uma medição do sensor.</summary>
    /// <remarks>
    /// Calcula o nível da água (altura de instalação − distância) e classifica o risco:
    /// Normal (&lt; 30 cm), Atenção (30 a 59 cm) ou Alerta (≥ 60 cm).
    /// Distâncias fora da faixa do sensor (2 a 400 cm) são rejeitadas com 422 e não alteram o risco.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(LeituraResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarLeituraRequest request, CancellationToken ct)
    {
        var resultado = await _leituras.RegistrarAsync(request, ct);

        if (!resultado.Aceita)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Leitura inválida",
                Detail = resultado.Leitura.MotivoRejeicao
            });
        }

        return Created($"/api/pontos/{resultado.Leitura.PontoId}/leituras", resultado.Leitura);
    }
}
