using MessangerBackend.Models;

namespace MessangerBackend.Services;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string username, string password);
    string GenerateJwtToken(User user);
    Task<User?> RegisterAsync(string name, string username, string password);
}