using System.Collections.Generic;
using System.Linq;
using MyProject.BL.Transparency;

namespace MyProject.API.Placement;

/// <summary>
/// DTO להסבר שיבוץ משתתף — נשלח ללקוח (Read Only).
/// </summary>
// אובייקט תשובה להסבר שיבוץ
public sealed class AssignmentExplanationDto
{
    // תעודת זהות של המשתתף
    public string ParticipantId { get; set; } = string.Empty;

    // שם תצוגה — מתווסף בשכבת API
    public string? ParticipantDisplayName { get; set; }

    // הקבוצה הנוכחית של המשתתף
    public int CurrentGroupId { get; set; }

    // ציון הקבוצה הנוכחית
    public double CurrentGroupScore { get; set; }

    // האם אפשר להעביר לקבוצה אחרת
    public bool CanMove { get; set; }

    // הקבוצה החלופית הכי טובה
    public int? BestAlternativeGroupId { get; set; }

    // כמה הציון ישתנה אם עוברים לקבוצה החלופית
    public double? EstimatedScoreChange { get; set; }

    // סיבות להסבר הנוכחי
    public List<ExplanationReasonDto> Reasons { get; set; } = new();

    // הערכות לכל קבוצה חלופית
    public List<AlternativeGroupEvaluationDto> Alternatives { get; set; } = new();

    // האם אפשר להחליף עם משתתף אחר
    public bool CanSwap { get; set; }

    // המשתתף המומלץ להחלפה
    public string? BestSwapWithParticipantId { get; set; }

    // שם תצוגה של משתתף ההחלפה
    public string? BestSwapWithParticipantDisplayName { get; set; }

    // קבוצת יעד של ההחלפה המומלצת
    public int? BestSwapTargetGroupId { get; set; }

    // שינוי ציון משוער בהחלפה המומלצת
    public double? BestSwapScoreChange { get; set; }

    // הערכות לכל אפשרות החלפה
    public List<AlternativeSwapEvaluationDto> SwapAlternatives { get; set; } = new();

    /// <summary>
    /// ממיר מודל BL ל-DTO.
    /// </summary>
    // בונה DTO ממודל ההסבר של BL
    public static AssignmentExplanationDto FromExplanation(ParticipantPlacementExplanation explanation)
    {
        return new AssignmentExplanationDto
        {
            ParticipantId = explanation.ParticipantId.Value,
            CurrentGroupId = explanation.CurrentGroupId.Value,
            CurrentGroupScore = explanation.CurrentGroupScore,
            CanMove = explanation.CanMove,
            BestAlternativeGroupId = explanation.BestAlternativeGroupId?.Value,
            EstimatedScoreChange = explanation.EstimatedScoreChange,
            CanSwap = explanation.CanSwap,
            BestSwapWithParticipantId = explanation.BestSwapWithParticipantId?.Value,
            BestSwapTargetGroupId = explanation.BestSwapTargetGroupId?.Value,
            BestSwapScoreChange = explanation.BestSwapScoreChange,
            Reasons = explanation.Reasons
                .Select(ExplanationReasonDto.FromReason)
                .ToList(),
            Alternatives = explanation.Alternatives
                .Select(AlternativeGroupEvaluationDto.FromEvaluation)
                .ToList(),
            SwapAlternatives = explanation.SwapAlternatives
                .Select(AlternativeSwapEvaluationDto.FromEvaluation)
                .ToList(),
        };
    }

    /// <summary>
    /// מוסיף שמות תצוגה מהמסד ל-DTO (שכבת API בלבד).
    /// </summary>
    // מוסיף שמות תצוגה מהמסד
    public void EnrichWithDisplayNames(IReadOnlyDictionary<string, string?> displayNameByIdentity)
    {
        // שם המשתתף הראשי
        if (displayNameByIdentity.TryGetValue(ParticipantId, out var participantName))
        {
            ParticipantDisplayName = participantName;
        }

        // שם משתתף ההחלפה המומלץ
        if (BestSwapWithParticipantId is not null
            && displayNameByIdentity.TryGetValue(BestSwapWithParticipantId, out var bestSwapName))
        {
            BestSwapWithParticipantDisplayName = bestSwapName;
        }

        // שמות לכל חלופות ההחלפה
        foreach (var swap in SwapAlternatives)
        {
            if (displayNameByIdentity.TryGetValue(swap.SwapWithParticipantId, out var swapName))
            {
                swap.SwapWithParticipantDisplayName = swapName;
            }
        }
    }
}

/// <summary>
/// DTO לסיבה בודדת בהסבר.
/// </summary>
// סיבה אחת בהסבר השיבוץ
public sealed class ExplanationReasonDto
{
    // קוד הסיבה
    public string Code { get; set; } = string.Empty;

    // הודעת הסבר
    public string Message { get; set; } = string.Empty;

    // משקל הסיבה בניקוד
    public double Weight { get; set; }

    // בונה DTO ממודל BL
    public static ExplanationReasonDto FromReason(ExplanationReason reason)
    {
        return new ExplanationReasonDto
        {
            Code = reason.Code.ToString(),
            Message = reason.Message,
            Weight = reason.Weight,
        };
    }
}

/// <summary>
/// DTO להערכת קבוצה חלופית.
/// </summary>
// הערכת מעבר לקבוצה חלופית
public sealed class AlternativeGroupEvaluationDto
{
    // מזהה הקבוצה החלופית
    public int GroupId { get; set; }

    // האם המעבר חוקי
    public bool IsLegal { get; set; }

    // האם דורש אישור חריגה
    public bool RequiresOverride { get; set; }

    // שינוי ציון משוער למשתתף
    public double EstimatedParticipantScoreDelta { get; set; }

    // שינוי ציון משוער לכל החלוקה
    public double EstimatedAssignmentScoreDelta { get; set; }

    // סיבות להערכה הזו
    public List<ExplanationReasonDto> Reasons { get; set; } = new();

    // בונה DTO ממודל BL
    public static AlternativeGroupEvaluationDto FromEvaluation(AlternativeGroupEvaluation evaluation)
    {
        return new AlternativeGroupEvaluationDto
        {
            GroupId = evaluation.GroupId.Value,
            IsLegal = evaluation.IsLegal,
            RequiresOverride = evaluation.RequiresOverride,
            EstimatedParticipantScoreDelta = evaluation.EstimatedParticipantScoreDelta,
            EstimatedAssignmentScoreDelta = evaluation.EstimatedAssignmentScoreDelta,
            Reasons = evaluation.Reasons
                .Select(ExplanationReasonDto.FromReason)
                .ToList(),
        };
    }
}

/// <summary>
/// DTO להערכת החלפה עם משתתף מקבוצה אחרת.
/// </summary>
// הערכת החלפה עם משתתף אחר
public sealed class AlternativeSwapEvaluationDto
{
    // קבוצת יעד של ההחלפה
    public int TargetGroupId { get; set; }

    // תעודת זהות של משתתף ההחלפה
    public string SwapWithParticipantId { get; set; } = string.Empty;

    // שם תצוגה — מתווסף בהעשרה
    public string? SwapWithParticipantDisplayName { get; set; }

    // האם ההחלפה חוקית
    public bool IsLegal { get; set; }

    // האם דורשת אישור חריגה
    public bool RequiresOverride { get; set; }

    // שינוי ציון משוער למשתתף
    public double EstimatedParticipantScoreDelta { get; set; }

    // שינוי ציון משוער לכל החלוקה
    public double EstimatedAssignmentScoreDelta { get; set; }

    // סיבות להערכה הזו
    public List<ExplanationReasonDto> Reasons { get; set; } = new();

    // בונה DTO ממודל BL
    public static AlternativeSwapEvaluationDto FromEvaluation(AlternativeSwapEvaluation evaluation)
    {
        return new AlternativeSwapEvaluationDto
        {
            TargetGroupId = evaluation.TargetGroupId.Value,
            SwapWithParticipantId = evaluation.SwapWithParticipantId.Value,
            IsLegal = evaluation.IsLegal,
            RequiresOverride = evaluation.RequiresOverride,
            EstimatedParticipantScoreDelta = evaluation.EstimatedParticipantScoreDelta,
            EstimatedAssignmentScoreDelta = evaluation.EstimatedAssignmentScoreDelta,
            Reasons = evaluation.Reasons
                .Select(ExplanationReasonDto.FromReason)
                .ToList(),
        };
    }
}