namespace MyProject.API.Placement;

/// <summary>
/// בקשה ליצירת חלוקה חדשה מרשימת משתתפים שנבחרו בממשק.
/// </summary>
public sealed class CreateAssignmentFromParticipantsRequest
{
    public string AssignmentName { get; set; } = "חלוקה חדשה";

    public int GroupCount { get; set; } = 2;

    public int MinGroupSize { get; set; } = 2;

    public int MaxGroupSize { get; set; } = 2;

    public List<SelectedParticipantForAssignmentDto> Participants { get; set; } = new();
}

/// <summary>
/// משתתף שנבחר לחלוקה חדשה — ת.ז., שם וסיווגים.
/// </summary>
public sealed class SelectedParticipantForAssignmentDto
{
    public string ParticipantId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public Dictionary<string, string> Classifications { get; set; } = new();
}
