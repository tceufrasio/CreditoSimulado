using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            ArgumentException =>
                StatusCodes.Status400BadRequest,

            InvalidOperationException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Erro não tratado durante a requisição.");
        }

        httpContext.Response.StatusCode = statusCode;

        var (title, detail) = statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                (
                    "Requisição inválida",
                    exception.Message
                ),

            StatusCodes.Status409Conflict =>
                (
                    "Conflito na operação",
                    exception.Message
                ),

            _ =>
                (
                    "Erro interno do servidor",
                    "Ocorreu um erro inesperado ao processar a requisição."
                )
        };

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail
                }
            });
    }
}
