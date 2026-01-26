using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RolesSvc _rolesService;

        public RolesController(RolesSvc rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var role = await _rolesService.GetRoleByIdAsync(id);
            var roleDTO = new RoleDTO
            {
                Id = role.Id,
                Name = role.Name,
                RoleType = role.RoleType
            };

            return Ok(roleDTO);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDTO request)
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            await _rolesService.DeleteRoleAsync(id);
            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleDTO request)
        {
            var role = await _rolesService.UpdateRoleAsync(id, request.Name, request.RoleType);
            var roleDTO = new RoleDTO
            {
                Id = role.Id,
                Name = role.Name,
                RoleType = role.RoleType
            };

            return Ok(roleDTO);
        }
    }
}
