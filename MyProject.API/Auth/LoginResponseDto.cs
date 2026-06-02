namespace MyProject.API.Auth;

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public int ManagerId { get; set; }

    public string ManagerName { get; set; } = string.Empty;
}
