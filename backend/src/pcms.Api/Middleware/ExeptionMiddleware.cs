using System.Net;
using System.Text.Json;

namespace pcms.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            if (ex is not ArgumentException)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception while processing {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
            }

            await HandleExceptionAsync(
                context,
                ex);
        }
    }


    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        context.Response.ContentType =
            "application/json";


        var isValidationError =
            exception is ArgumentException;

        context.Response.StatusCode = isValidationError
            ? (int)HttpStatusCode.BadRequest
            : (int)HttpStatusCode.InternalServerError;


        var response = new
        {
            success = false,
            message = isValidationError
                ? exception.Message
                : "Ocurrió un error interno. Intenta nuevamente."
        };


        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}
