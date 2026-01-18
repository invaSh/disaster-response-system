namespace AuthService.Application.Auth
{
    public class Login
    {
        public class Command
        {
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        public class Result
        {
            public string AccessToken { get; set; } = null!;
            public string RefreshToken { get; set; } = null!;
        }
    }
}
