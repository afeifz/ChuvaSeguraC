namespace ChuvaSegura.Monitoramento.Tests.Fakes;

/// <summary>Relógio controlável para testes (substitui o horário real).</summary>
internal sealed class RelogioFixo : TimeProvider
{
    private DateTimeOffset _agora;

    public RelogioFixo(DateTimeOffset agora)
    {
        _agora = agora;
    }

    public override DateTimeOffset GetUtcNow() => _agora;

    public void Avancar(TimeSpan tempo) => _agora += tempo;
}
