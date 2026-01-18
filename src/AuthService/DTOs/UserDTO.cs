using AuthService.Enums;

namespace AuthService.DTOs
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public UserStatus Status { get; set; }
    }
}
