using AuthService.Domain;
using AuthService.Persistence;

namespace AuthService.Services
{
    public class TokenSvc
    {
        private readonly AuthDbContext _context;

        public TokenSvc(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(User user)
        {
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }
    }

}
