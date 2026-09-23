using System.Net;
using ClinicBook.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBook.Api.Middleware;

/// <summary>
/// One place that turns exceptions into HTTP responses, so no controller needs try/catch.
/// Our own exception types carry the meaning (not found, not allowed, rule broken) and this
/// middleware translates each one into the matching status code and a ProblemDetails body.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        // If the response has already started, its status code and headers are gone to the client
        // and we cannot replace them. Rethrowing keeps the real exception in the logs instead of
        // hiding it behind an "cannot modify the response" error.
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception,
                "Unhandled exception after the response had started for {Method} {Path}.",
                context.Request.Method, context.Request.Path);

            throw exception;
        }

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Not found", exception.Message),
            BusinessRuleException => (HttpStatusCode.Conflict, "Request could not be completed", exception.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, "Not allowed", exception.Message),
            UnauthorizedException => (HttpStatusCode.Unauthorized, "Authentication failed", exception.Message),
            // Anything unexpected: log the details, but never show them to the caller.
            _ => (HttpStatusCode.InternalServerError, "Unexpected error",
                "Something went wrong while handling your request.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while handling {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogInformation("Request rejected with {StatusCode}: {Message}",
                (int)statusCode, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
