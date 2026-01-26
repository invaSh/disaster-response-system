using AuthService.Persistence;
using AuthService.Domain;
using AuthService.Enums;
using AuthService.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AuthService.Services
{
    public class UserSvc
    {
        private readonly AuthDbContext _context;
        public UserSvc(AuthDbContext context) => _context = context;

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new StatusException(HttpStatusCode.NotFound, "NotFound", "User not found.", new { Id = "User does not exist." });
            return user;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "User not found.", new { Id = "User does not exist." });

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<User?> UpdateUserAsync(Guid id, string? email, UserStatus? status)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "User not found.", new { Id = "User does not exist." });

                if (email != null)
                {
                    var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Id != id);
                    if (existing != null)
                        throw new StatusException(HttpStatusCode.Conflict, "Duplicate", "Email already exists.", new { Email = "Email must be unique." });
                    
                    user.Email = email;
                }

                if (status.HasValue)
                    user.Status = status.Value;

                await _context.SaveChangesAsync();
                return user;
            }
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }
    }

}
