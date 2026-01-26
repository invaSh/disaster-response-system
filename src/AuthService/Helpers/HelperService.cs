using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AuthService.Helpers
{
    public static class HelperService
    {
        public static StatusException MapToStatusException(Exception ex)
        {
            return ex switch
            {
                DbUpdateException => new StatusException(
                    HttpStatusCode.Conflict,
                    "DatabaseError",
                    "Failed to update database due to constraint violation",
                    ex.StackTrace ?? string.Empty
                ),

                StatusException se => se,

                _ => new StatusException(
                    HttpStatusCode.InternalServerError,
                    "InternalServer",
                    ex.Message,
                    ex.StackTrace ?? string.Empty
                )
            };
        }
    }
}
