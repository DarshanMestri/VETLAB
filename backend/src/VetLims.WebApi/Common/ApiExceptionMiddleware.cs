using System.Net;
using System.Text.Json;

namespace VetLims.WebApi.Common;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<object>.FailResult(
                "An unexpected error occurred.",
                context.Response.StatusCode,
                new[] { ex.Message });

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}

public static class ApiExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiExceptionMiddleware>();
    }
}
