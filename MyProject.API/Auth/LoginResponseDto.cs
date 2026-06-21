namespace MyProject.API.Auth;

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public int ManagerId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string ManagerName { get; set; } = string.Empty;
}
