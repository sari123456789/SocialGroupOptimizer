namespace MyProject.API.Placement;

public sealed class UpdateAssignmentRequest
{
    public string? AssignmentName { get; set; }

    public AssignmentSettingsDto? Settings { get; set; }
}
