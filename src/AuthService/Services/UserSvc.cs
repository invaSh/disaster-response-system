using AuthService.Persistence;
using AuthService.Domain;
using AuthService.Enums;
using Microsoft.EntityFrameworkCore;

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

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> UpdateUserAsync(Guid id, string? email, UserStatus? status)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            if (email != null)
                user.Email = email;

            if (status.HasValue)
                user.Status = status.Value;

            await _context.SaveChangesAsync();
            return user;
        }
    }

}
