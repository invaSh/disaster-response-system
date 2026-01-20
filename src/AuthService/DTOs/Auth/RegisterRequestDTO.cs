using AuthService.Enums;

namespace AuthService.DTOs.Auth
{
    public class RegisterRequestDTO
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public RoleType RoleType { get; set; }
    }
}
