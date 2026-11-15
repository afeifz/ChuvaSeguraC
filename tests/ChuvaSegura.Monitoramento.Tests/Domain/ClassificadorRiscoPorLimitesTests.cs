using ChuvaSegura.Monitoramento.Domain.Enums;
using ChuvaSegura.Monitoramento.Domain.Exceptions;
using ChuvaSegura.Monitoramento.Domain.Services;
using Xunit;

namespace ChuvaSegura.Monitoramento.Tests.Domain;

public class ClassificadorRiscoPorLimitesTests
{
    private readonly ClassificadorRiscoPorLimites _classificador = new(30, 60);

    [Theory]
    [InlineData(0.0, NivelRisco.Normal)]
    [InlineData(29.9, NivelRisco.Normal)]
    [InlineData(30.0, NivelRisco.Atencao)]
    [InlineData(59.9, NivelRisco.Atencao)]
    [InlineData(60.0, NivelRisco.Alerta)]
    [InlineData(100.0, NivelRisco.Alerta)]
    public void Classificar_RespeitaOsLimites(double nivelCm, NivelRisco esperado)
    {
        Assert.Equal(esperado, _classificador.Classificar(nivelCm));
    }

    [Theory]
    [InlineData(0.0, 60.0)]
    [InlineData(-5.0, 60.0)]
    [InlineData(60.0, 30.0)]
    [InlineData(30.0, 30.0)]
    public void Construtor_ComLimitesInvalidos_LancaDominioException(double atencao, double alerta)
    {
        Assert.Throws<DominioException>(() => new ClassificadorRiscoPorLimites(atencao, alerta));
    }
}
