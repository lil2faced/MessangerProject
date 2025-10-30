using Microsoft.AspNetCore.Mvc;
using MessangerBackend.Services;
using MessangerBackend.DTOs;

namespace MessangerBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.UserName, request.Password);
        if (user == null)
            return Unauthorized("Invalid username or password");

        var token = _authService.GenerateJwtToken(user);

        return new AuthResponse
        {
            Id = user.Id,
            Name = user.Name,
            UserName = user.UserName,
            Token = token,
            RegistrationDate = user.RegistrationDate
        };
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request.Name, request.UserName, request.Password);
        if (user == null)
            return BadRequest("Username already exists");

        var token = _authService.GenerateJwtToken(user);

        return new AuthResponse
        {
            Id = user.Id,
            Name = user.Name,
            UserName = user.UserName,
            Token = token,
            RegistrationDate = user.RegistrationDate
        };
    }
}