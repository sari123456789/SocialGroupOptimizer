namespace MyProject.Data.Models;

/// <summary>
/// ארכיון שיא לריצת אלגוריתם.
/// </summary>
public class BestSolutionArchive
{
    public Guid RunId { get; set; }

    public double BestScore { get; set; }

    public string SolutionStateBlob { get; set; } = string.Empty;

    public bool WasDiversified { get; set; }
}
