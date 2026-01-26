using AuthService.DTOs.Auth;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthSvc _authService;

        public AuthController(AuthSvc authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            var user = await _authService.RegisterAsync(request.Email, request.Password, request.RoleType);
            return Ok(new
            {
                user.Id,
                user.Email,
                RoleType = request.RoleType
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(result);
        }
    }
}
