namespace MyProject.API.Placement;

public sealed class AddClassificationConstraintRequest
{
    public string DimensionCode { get; set; } = string.Empty;

    public string RuleType { get; set; } = string.Empty;
}
