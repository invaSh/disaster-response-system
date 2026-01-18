using AuthService.Enums;

namespace AuthService.DTOs
{
    public class CreateRoleRequestDTO
    {
        public string Name { get; set; } = null!;
        public RoleType RoleType { get; set; }
    }
}
