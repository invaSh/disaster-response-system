using AuthService.Persistence;
using AuthService.Domain;
using AuthService.Enums;
using AuthService.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

        public async Task<Role> GetRoleByIdAsync(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Role not found.", new { Id = "Role does not exist." });
            return role;
        }

        public async Task<Role> CreateRoleAsync(string name, RoleType roleType)
        {
            try
            {
                if (roleType != RoleType.Admin)
                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "RoleType must be Admin (1)", new { RoleType = "Invalid role type." });

                var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
                if (existing != null)
                    throw new StatusException(HttpStatusCode.Conflict, "Duplicate", "Role with this name already exists.", new { Name = "Role name must be unique." });

                var role = new Role
                {
                    Name = name,
                    RoleType = roleType
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return role;
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
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", "Role not found.", new { Id = "Role does not exist." });

                if (name != null)
                {
                    var existing = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name && r.Id != id);
                    if (existing != null)
                        throw new StatusException(HttpStatusCode.Conflict, "Duplicate", "Role with this name already exists.", new { Name = "Role name must be unique." });

                    role.Name = name;
                }

                if (roleType.HasValue)
                {
                    if (roleType.Value != RoleType.Admin)
                        throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "RoleType must be Admin (1)", new { RoleType = "Invalid role type." });

                    role.RoleType = roleType.Value;
                }

                await _context.SaveChangesAsync();
                return role;
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
