using Amazon.S3;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace NexoDesk.API.Filters;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AmazonS3Exception storageException)
        {
            logger.LogError(storageException,
                "Falha no armazenamento R2 ao processar {Path}. Código: {ErrorCode}, Status: {StatusCode}.",
                httpContext.Request.Path,
                storageException.ErrorCode,
                storageException.StatusCode);

            await EscreverProblemaAsync(
                httpContext,
                StatusCodes.Status502BadGateway,
                "Falha ao armazenar o anexo",
                "O serviço de arquivos recusou o envio. Verifique o endpoint, o bucket e as permissões do token R2.",
                cancellationToken);

            return true;
        }

        if (exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            }
        })
        {
            logger.LogWarning(exception,
                "Conflito de dados ao persistir requisição em {Path}.",
                httpContext.Request.Path);

            await EscreverProblemaAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflito de dados",
                "Já existe um registro com um dos dados informados.",
                cancellationToken);

            return true;
        }

        logger.LogError(exception,
            "Erro não tratado ao processar requisição em {Path}.",
            httpContext.Request.Path);

        await EscreverProblemaAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Erro interno",
            "Ocorreu um erro inesperado ao processar a solicitação.",
            cancellationToken);

        return true;
    }

    private static Task EscreverProblemaAsync(
        HttpContext httpContext,
        int status,
        string titulo,
        string detalhe,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = status;

        return httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe
        }, cancellationToken);
    }
}
