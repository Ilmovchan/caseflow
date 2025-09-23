using System.Diagnostics;
using CaseFlow.BLL.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CaseFlow.PAGES.ExceptionHandlers;

public class GlobalExceptionHandler (ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync
        (HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        logger.LogError(exception,
            "Could not process a request on machine {MachineName}. TraceId: {TraceId}",
            Environment.MachineName,
            traceId);

        var (statusCode, title) = MapException(exception); 
        
        await Results.Problem(
            title: title,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
        {
            {"traceId", traceId}
        }
        ).ExecuteAsync(httpContext);
        
        return true;
    }

    private static (int StatusCode, string Title) MapException(Exception exception)
    {
        if (exception is DbUpdateException dbUpdateEx && dbUpdateEx.InnerException is PostgresException pgEx)
        {
            var constraint = pgEx.ConstraintName;
            var detail = pgEx.Detail;
            var message = $"Database constraint violation '{constraint}'. Details: {detail}";

            if (pgEx.SqlState is PostgresErrorCodes.ForeignKeyViolation)
            {
                return (StatusCodes.Status409Conflict, message);
            }
            
            return (StatusCodes.Status400BadRequest, message);
        }
        
        return exception switch
        {
            EntityDeleteConflictException => (StatusCodes.Status409Conflict, exception.Message),
            DbUpdateException => (StatusCodes.Status400BadRequest, exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            EntityNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "500 Internal Server Error"),
        };
    }
}