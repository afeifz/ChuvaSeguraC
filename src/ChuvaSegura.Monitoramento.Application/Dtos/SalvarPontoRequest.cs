using System.ComponentModel.DataAnnotations;

namespace ChuvaSegura.Monitoramento.Application.Dtos;

/// <summary>Dados para criar ou atualizar um ponto de monitoramento.</summary>
public sealed record SalvarPontoRequest
{
    /// <example>Ponto Ipiranga 01 (simulado)</example>
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres.")]
    public string Nome { get; init; } = string.Empty;

    /// <example>Ipiranga</example>
    [Required(ErrorMessage = "O bairro é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O bairro deve ter entre 2 e 100 caracteres.")]
    public string Bairro { get; init; } = string.Empty;

    /// <example>-23.5875</example>
    [Range(-90, 90, ErrorMessage = "A latitude deve estar entre -90 e 90.")]
    public double Latitude { get; init; }

    /// <example>-46.6095</example>
    [Range(-180, 180, ErrorMessage = "A longitude deve estar entre -180 e 180.")]
    public double Longitude { get; init; }

    /// <summary>Altura do sensor em relação ao fundo/solo, em cm.</summary>
    /// <example>100</example>
    [Range(10, 400, ErrorMessage = "A altura de instalação deve estar entre 10 e 400 cm.")]
    public double AlturaInstalacaoCm { get; init; }
}
