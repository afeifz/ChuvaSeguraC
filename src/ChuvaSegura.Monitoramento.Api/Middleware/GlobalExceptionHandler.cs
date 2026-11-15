using ChuvaSegura.Monitoramento.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ChuvaSegura.Monitoramento.Api.Middleware;

/// <summary>
/// Converte exceções em respostas padronizadas (ProblemDetails, RFC 7807).
/// Assim os controllers ficam limpos e o cliente sempre recebe o mesmo formato de erro.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // A ordem importa: as exceções específicas vêm antes da genérica DominioException.
        var (status, titulo) = exception switch
        {
            NaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflitoException => (StatusCodes.Status409Conflict, "Conflito com o estado atual"),
            DominioException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor")
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Erro não tratado ao processar {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("{Titulo}: {Mensagem}", titulo, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            // Em erros 500 não expomos detalhes internos ao cliente.
            Detail = status >= 500 ? null : exception.Message
        };

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception
        });
    }
}
