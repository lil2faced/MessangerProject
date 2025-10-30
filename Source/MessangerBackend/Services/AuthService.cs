using MessangerBackend.Data;
using MessangerBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MessangerBackend.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName == username);
        if (user == null) return null;

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            return null;

        user.LastActivity = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.Name)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<User?> RegisterAsync(string name, string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.UserName == username))
            return null;

        var user = new User
        {
            Name = name,
            UserName = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            RegistrationDate = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
