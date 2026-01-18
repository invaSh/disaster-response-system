using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserSvc _userService;

        public UsersController(UserSvc userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var userDTOs = users.Select(u => new UserDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Status = u.Status
                }).ToList();

                return Ok(userDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving users.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                var userDTO = new UserDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Status = user.Status
                };

                return Ok(userDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the user.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var deleted = await _userService.DeleteUserAsync(id);
                if (!deleted)
                    return NotFound(new { message = "User not found." });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the user.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequestDTO request)
        {
            try
            {
                // Get the current user's ID from the JWT token (stored as "sub" claim)
                var currentUserIdClaim = User.FindFirst("sub") 
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                
                if (currentUserIdClaim == null || !Guid.TryParse(currentUserIdClaim.Value, out var currentUserId))
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }

                // Check if user is Admin OR updating their own profile
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && currentUserId != id)
                {
                    return Forbid("You can only update your own profile.");
                }

                var user = await _userService.UpdateUserAsync(id, request.Email, request.Status);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                var userDTO = new UserDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Status = user.Status
                };

                return Ok(userDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the user.", error = ex.Message });
            }
        }
    }
}
