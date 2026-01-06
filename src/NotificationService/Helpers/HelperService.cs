using Microsoft.EntityFrameworkCore;
using NotificationService.Middlewares;
using System.Net;

namespace NotificationService.Helpers
{
    public static class HelperService
    {
        public static string GenerateNotificationCode()
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpper();
            return $"NOT-{suffix}";
        }

        public static StatusException MapToStatusException(Exception ex)
        {
            return ex switch
            {
                DbUpdateException => new StatusException(
                    HttpStatusCode.Conflict,
                    "DatabaseError",
                    "Failed to update notification due to database constraint.",
                    new Dictionary<string, string[]>
                    {
                        { "error", new[] { ex.InnerException?.Message ?? ex.Message } }
                    }
                ),

                StatusException se => se,

                _ => new StatusException(
                    HttpStatusCode.InternalServerError,
                    "InternalServer",
                    "Unexpected server error.",
                    new Dictionary<string, string[]>
                    {
                        { "error", new[] { ex.Message } }
                    }
                )
            };
        }
    }
}
