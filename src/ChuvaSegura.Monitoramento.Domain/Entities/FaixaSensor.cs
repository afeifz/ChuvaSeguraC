namespace ChuvaSegura.Monitoramento.Domain.Entities;

/// <summary>Faixa de medição do sensor ultrassônico HC-SR04 usado no ESP32 (em centímetros).</summary>
public static class FaixaSensor
{
    public const double DistanciaMinimaCm = 2;
    public const double DistanciaMaximaCm = 400;
}
