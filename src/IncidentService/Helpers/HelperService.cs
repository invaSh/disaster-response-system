using Microsoft.EntityFrameworkCore;
using System.Net;

namespace IncidentService.Helpers
{
    public static class HelperService
    {
        public static string GenerateIncidentCode()
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpper();
            return $"INC-{suffix}";
        }

        public static StatusException MapToStatusException(Exception ex)
        {
            return ex switch
            {
                DbUpdateException => new StatusException(
                    HttpStatusCode.Conflict,
                    "DatabaseError",
                    "Failed to update incident due to database constraint",
                    ex.StackTrace
                ),

                StatusException se => se,

                _ => new StatusException(
                    HttpStatusCode.InternalServerError,
                    "InternalServer",
                    ex.Message,
                    ex.StackTrace
                )
            };
        }
    }
}
