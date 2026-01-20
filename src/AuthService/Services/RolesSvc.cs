using AuthService.Persistence;
using AuthService.Domain;
using AuthService.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services
{
    public class RolesSvc
    {
        private readonly AuthDbContext _context;
        public RolesSvc(AuthDbContext context) => _context = context;

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(Guid id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<Role> CreateRoleAsync(string name, RoleType roleType)
        {
            // Validate RoleType is either Admin (1) 
            if (roleType != RoleType.Admin)
                throw new Exception("RoleType must be  Admin (1) ");

            var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
            if (existing != null)
                throw new Exception("Role with this name already exists.");

            var role = new Role
            {
                Name = name,
                RoleType = roleType
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<bool> DeleteRoleAsync(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Role?> UpdateRoleAsync(Guid id, string? name, RoleType? roleType)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return null;

            if (name != null)
            {
                // Check if name is already taken by another role
                var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name && r.Id != id);
                if (existing != null)
                    throw new Exception("Role with this name already exists.");

                role.Name = name;
            }

            if (roleType.HasValue)
            {
                // Validate RoleType is either Admin (1) or User (2)
                if (roleType.Value != RoleType.Admin)
                    throw new Exception("RoleType must be either Admin (1) ");

                role.RoleType = roleType.Value;
            }

            await _context.SaveChangesAsync();
            return role;
        }
    }
}
