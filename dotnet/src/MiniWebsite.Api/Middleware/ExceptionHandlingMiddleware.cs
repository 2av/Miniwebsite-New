using System.Net;
using System.Text.Json;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var expose = _env.IsDevelopment()
                || _configuration.GetValue("App:ExposeExceptionDetails", false);

            ApiResult payload;
            if (expose)
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["exceptionType"] = [ex.GetType().FullName ?? ex.GetType().Name],
                    ["exception"] = [ex.Message],
                    ["stackTrace"] = [ex.StackTrace ?? string.Empty]
                };

                if (ex.InnerException != null)
                {
                    errors["innerExceptionType"] =
                        [ex.InnerException.GetType().FullName ?? ex.InnerException.GetType().Name];
                    errors["innerException"] = [ex.InnerException.Message];
                }

                payload = ApiResult.Fail(ex.Message, errors);
            }
            else
            {
                payload = ApiResult.Fail("An unexpected error occurred.");
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}
