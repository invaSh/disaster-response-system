using System.Net;

namespace AuthService.Helpers
{
    public class StatusException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorType { get; }
        public object? Errors { get; }
        public string? Error { get; }

        public StatusException(HttpStatusCode statusCode, string errorType, string message, object errors)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Errors = errors;
            Error = null;
        }

        public StatusException(HttpStatusCode statusCode, string errorType, string message, string stack)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Errors = null;
            Error = stack;
        }
    }
}
