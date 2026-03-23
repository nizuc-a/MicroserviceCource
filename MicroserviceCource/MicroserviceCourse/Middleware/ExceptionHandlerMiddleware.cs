using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceCourse.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Request.Headers["x-request-id"]);

        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var error = new ProblemDetails()
        {
            Type = MapTypeLink(ex),
            Title = "An unhandled exception occurred",
            Status = MapStatusCode(ex),
            Detail = ex.Message,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.ContentType = "application/problem+json; charset=utf-8";
        httpContext.Response.StatusCode = MapStatusCode(ex);
        await httpContext.Response.WriteAsJsonAsync(error);
    }

    private static string MapTypeLink(Exception ex)
        => ex switch
        {
            KeyNotFoundException => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
            ArgumentOutOfRangeException => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
            _ => "https://datatracker.ietf.org/doc/html/rfc9110"
        };

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            KeyNotFoundException=> StatusCodes.Status404NotFound,
            ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
}