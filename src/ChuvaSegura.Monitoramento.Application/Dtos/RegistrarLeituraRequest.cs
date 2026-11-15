using System.ComponentModel.DataAnnotations;

namespace ChuvaSegura.Monitoramento.Application.Dtos;

/// <summary>Corpo enviado pelo ESP32 a cada medição.</summary>
public sealed record RegistrarLeituraRequest
{
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "Informe um pontoId válido.")]
    public int PontoId { get; init; }

    /// <summary>Distância em cm entre o sensor e a superfície da água (o sensor mede de 2 a 400 cm).</summary>
    /// <example>35</example>
    [Required(ErrorMessage = "A distância é obrigatória.")]
    public double? DistanciaCm { get; init; }
}
