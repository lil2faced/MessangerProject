namespace MessangerBackend.DTOs;

public class AuthResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
}