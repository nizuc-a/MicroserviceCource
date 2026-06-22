using EventService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Middleware;

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
            KeyNotFoundException => "https://datatracker.ietf.org/doc/html/rfc9110#name-404-not-found",
            ArgumentException => "https://datatracker.ietf.org/doc/html/rfc9110#name-400-bad-request",
            NoAvailableSeatsException => "https://datatracker.ietf.org/doc/html/rfc9110#name-409-conflict",
            EventExpiredException => "https://datatracker.ietf.org/doc/html/rfc9110#name-400-bad-request",
            ActiveBookingLimitExceededException => "https://datatracker.ietf.org/doc/html/rfc9110#name-409-conflict",
            BookingAlreadyCancelledException => "https://datatracker.ietf.org/doc/html/rfc9110#name-409-conflict",
            PermissionDeniedException => "https://datatracker.ietf.org/doc/html/rfc9110#name-403-forbidden",
            UserNotFoundException => "https://datatracker.ietf.org/doc/html/rfc9110#name-404-not-found",
            AuthenticationFailedException => "https://datatracker.ietf.org/doc/html/rfc9110#name-401-unauthorized",
            _ => "https://datatracker.ietf.org/doc/html/rfc9110"
        };

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            KeyNotFoundException=> StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            NoAvailableSeatsException => StatusCodes.Status409Conflict,
            EventExpiredException => StatusCodes.Status400BadRequest,
            ActiveBookingLimitExceededException => StatusCodes.Status409Conflict,
            BookingAlreadyCancelledException => StatusCodes.Status409Conflict,
            PermissionDeniedException => StatusCodes.Status403Forbidden,
            UserNotFoundException => StatusCodes.Status404NotFound,
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
}