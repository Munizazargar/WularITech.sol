using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WularItech_solutions.Interfaces;
using WularItech_solutions.Models;

namespace WularItech_solutions.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _secretKey;

        public TokenService(IConfiguration configuration)
        {
            _secretKey = configuration["Jwt:SecretKey"] 
                         ?? throw new Exception("JWT SecretKey not configured.");
        }

        public string CreateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),// Create a claim that stores the user's unique ID in the token// This allows us to know which user is making requests later
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
               new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Guid VerifyTokenAndGetId(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            var principal = handler.ValidateToken(token, validationParams, out _);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new Exception("UserId claim not found in token.");

            return Guid.Parse(userIdClaim.Value);
        }
         // ⚡ New method: check if the user is admin
        public bool IsAdmin(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            var principal = handler.ValidateToken(token, validationParams, out _);

            var adminClaim = principal.FindFirst("IsAdmin");
            if (adminClaim == null)
                return false;

            return adminClaim.Value.ToLower() == "true";
        }
    }
}
