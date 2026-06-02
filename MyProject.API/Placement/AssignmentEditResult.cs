namespace MyProject.API.Placement;

public sealed class AssignmentEditResult
{
    public bool Success { get; init; }

    public List<string> Errors { get; init; } = new();

    public AssignmentDetailDto? Detail { get; init; }

    public static AssignmentEditResult Ok(AssignmentDetailDto detail) =>
        new() { Success = true, Detail = detail };

    public static AssignmentEditResult Fail(params string[] errors) =>
        new() { Success = false, Errors = errors.ToList() };

    public static AssignmentEditResult Fail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };
}
