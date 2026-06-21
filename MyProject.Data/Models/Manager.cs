namespace MyProject.Data.Models;

public class Manager
{
    public int ManagerId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string ManagerName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
