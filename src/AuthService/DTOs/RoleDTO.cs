using AuthService.Enums;

namespace AuthService.DTOs
{
    public class RoleDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public RoleType RoleType { get; set; }
    }
}
