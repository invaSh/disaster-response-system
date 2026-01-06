using System.Net;

namespace NotificationService.Helpers
{
    [Serializable]
    public class StatusException : Exception
    {
        private HttpStatusCode badRequest;
        private string v1;
        private string v2;
        private Dictionary<string, string[]> errors;

        public StatusException()
        {
        }

        public StatusException(string? message) : base(message)
        {
        }

        public StatusException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public StatusException(HttpStatusCode badRequest, string v1, string v2, Dictionary<string, string[]> errors)
        {
            this.badRequest = badRequest;
            this.v1 = v1;
            this.v2 = v2;
            this.errors = errors;
        }
    }
}