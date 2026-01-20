using AuthService.Enums;

namespace AuthService.DTOs
{
    public class UpdateUserRequestDTO
    {
        public string? Email { get; set; }
        public UserStatus? Status { get; set; }
    }
}
