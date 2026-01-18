using AuthService.Domain;
using AuthService.DTOs.Auth;
using AuthService.Enums;
using AuthService.Helpers;
using AuthService.Persistence;
using Microsoft.EntityFrameworkCore;

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
            // Validate RoleType is either Admin (1) or User (2)
            if (roleType != RoleType.Admin && roleType != RoleType.User)
                throw new Exception("RoleType must be either Admin (1) or User (2).");

            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
                throw new Exception("User already exists.");

            // Find the role based on RoleType
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleType == roleType);
            if (role == null)
                throw new Exception($"Role with RoleType {roleType} not found. Please ensure roles are seeded in the database.");

            var user = new User
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Status = UserStatus.Active
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create UserRole relationship
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<AuthResponseDTO> LoginAsync(string email, string password)
        {
            var user = await _context.Users.Include(u => u.UserRoles)
                                           .ThenInclude(ur => ur.Role)
                                           .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            var roles = user.UserRoles.Select(ur => ur.Role.Name);
            var token = _jwtGenerator.Generate(user, roles);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            return new AuthResponseDTO
            {
                AccessToken = token,
                RefreshToken = refreshToken.Token
            };
        }

    }

}
