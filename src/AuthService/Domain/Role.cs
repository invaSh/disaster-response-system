using AuthService.Enums;

namespace AuthService.Domain
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public RoleType RoleType { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}
