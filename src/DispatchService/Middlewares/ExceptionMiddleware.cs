using System.Net;
using DispatchService.Helpers;

namespace DispatchService.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            catch (StatusException ex)
            {
                _logger.LogWarning(
                    "Handled exception: {ErrorType} - {Message}",
                    ex.ErrorType,
                    ex.Message
                );

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)ex.StatusCode;

                var response = new
                {
                    StatusCode = context.Response.StatusCode,
                    ErrorType = ex.ErrorType,
                    Message = ex.Message,
                    Errors = ex.Errors
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = new
                {
                    StatusCode = context.Response.StatusCode,
                    ErrorType = "InternalServerError",
                    Message = "An unexpected error occurred. Please try again later.",
                    Errors = (object?)null
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
