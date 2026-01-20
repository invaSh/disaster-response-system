using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly RolesSvc _rolesService;

        public RolesController(RolesSvc rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _rolesService.GetAllRolesAsync();
                var roleDTOs = roles.Select(r => new RoleDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    RoleType = r.RoleType
                }).ToList();

                return Ok(roleDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving roles.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            try
            {
                var role = await _rolesService.GetRoleByIdAsync(id);
                if (role == null)
                    return NotFound(new { message = "Role not found." });

                var roleDTO = new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name,
                    RoleType = role.RoleType
                };

                return Ok(roleDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the role.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDTO request)
        {
            try
            {
                var role = await _rolesService.CreateRoleAsync(request.Name, request.RoleType);
                var roleDTO = new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name,
                    RoleType = role.RoleType
                };

                return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, roleDTO);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                var deleted = await _rolesService.DeleteRoleAsync(id);
                if (!deleted)
                    return NotFound(new { message = "Role not found." });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the role.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleDTO request)
        {
            try
            {
                var role = await _rolesService.UpdateRoleAsync(id, request.Name, request.RoleType);
                if (role == null)
                    return NotFound(new { message = "Role not found." });

                var roleDTO = new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name,
                    RoleType = role.RoleType
                };

                return Ok(roleDTO);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
