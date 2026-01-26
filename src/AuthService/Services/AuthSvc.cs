using AuthService.Domain;
using AuthService.DTOs.Auth;
using AuthService.Enums;
using AuthService.Helpers;
using AuthService.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AuthService.Services
{
    public class AuthSvc
    {
        private readonly AuthDbContext _context;
        private readonly JwtTokenGenerator _jwtGenerator;
        private readonly TokenSvc _tokenService;


        public AuthSvc(AuthDbContext context, JwtTokenGenerator jwtGenerator, TokenSvc tokenService)
        {
            _context = context;
            _jwtGenerator = jwtGenerator;
            _tokenService = tokenService;
        }

        public async Task<User> RegisterAsync(string email, string password, RoleType roleType)
        {
            try
            {
                // validimi
                if (roleType != RoleType.Admin && roleType != RoleType.User && roleType != RoleType.IncMan && roleType != RoleType.DisMan)
                    throw new StatusException(HttpStatusCode.BadRequest, "ValidationError", "RoleType must be either Admin (1) or User (2) or IncMan (3) or DisMan (4).", new { RoleType = "Invalid role type." });

                var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (existing != null)
                    throw new StatusException(HttpStatusCode.Conflict, "Duplicate", "User already exists.", new { Email = "Email is already registered." });

                // gjeje bazuar ne rol 
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleType == roleType);
                if (role == null)
                    throw new StatusException(HttpStatusCode.NotFound, "NotFound", $"Role with RoleType {roleType} not found. Please ensure roles are seeded in the database.", new { RoleType = "Role does not exist." });

                var user = new User
                {
                    Email = email,
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = UserStatus.Active
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                return user;
            }
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

        public async Task<AuthResponseDTO> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _context.Users.Include(u => u.UserRoles)
                                               .ThenInclude(ur => ur.Role)
                                               .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                    throw new StatusException(HttpStatusCode.Unauthorized, "InvalidCredentials", "Invalid credentials", new { Email = "Email or password is incorrect." });

                var roles = user.UserRoles.Select(ur => ur.Role.Name);
                var token = _jwtGenerator.Generate(user, roles);
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

                return new AuthResponseDTO
                {
                    AccessToken = token,
                    RefreshToken = refreshToken.Token
                };
            }
            catch (StatusException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw HelperService.MapToStatusException(ex);
            }
        }

    }

}
