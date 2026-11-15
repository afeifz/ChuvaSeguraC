namespace ChuvaSegura.Monitoramento.Domain.Exceptions;

/// <summary>Violação de uma regra do domínio (dados inválidos). Vira HTTP 400.</summary>
public class DominioException : Exception
{
    public DominioException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>Recurso inexistente. Vira HTTP 404.</summary>
public sealed class NaoEncontradoException : DominioException
{
    public NaoEncontradoException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>Operação em conflito com o estado atual do recurso. Vira HTTP 409.</summary>
public sealed class ConflitoException : DominioException
{
    public ConflitoException(string mensagem) : base(mensagem)
    {
    }
}
