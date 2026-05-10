namespace MyProject.Data.Models;

public class ManagementGroup
{
    public int ManagementGroupId { get; set; }

    public int ManagerId { get; set; }

    public string ManagementGroupName { get; set; } = string.Empty;
}
