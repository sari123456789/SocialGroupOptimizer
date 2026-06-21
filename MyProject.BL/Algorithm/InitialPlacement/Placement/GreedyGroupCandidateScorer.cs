using MyProject.Core.Domain.ValueObjects;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Algorithm.InitialPlacement.Placement;

/// <summary>
/// תפקיד המחלקה: מתן ציון לקבוצת שיבוץ מועמדת בזמן האלגוריתם החמדן.
/// המחלקה משתתפת בשלב בניית החלוקה הראשונית — רק במצב גמיש (לא פריסה קבועה).
/// </summary>
internal sealed class GreedyGroupCandidateScorer
{
    private const double IsolationReductionBonus = 1.0; // בונוס קטן למשתתפים שמצמצמים בידוד (יש העד פנימיות)
    private const double EarlyFullGroupPenalty = 0.25;// עונש קטן לקבוצה שמלאה מוקדם מדי (שומר גמישות לשיבוץ הבא)

    private readonly IReadOnlyDictionary<ParticipantId, Participant> _participantsById;// מזהה משתתף  פרטי משתתף (לבדיקת תקינות והימנעות מטעויות במיפוי העד)
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ParticipantId, int>> _outgoingPreferencesByParticipant;// מזהה משתתף  (מזהי משתתפים אחרים + דירוג העדפה) — העד יוצאות של המשתתף
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ParticipantId, int>> _requestedByParticipant;// מזהה משתתף  (מזהי משתתפים אחרים + דירוג העדפה) — העד נכנסות של המשתתף

    public GreedyGroupCandidateScorer(
        IReadOnlyDictionary<ParticipantId, Participant> participantsById,
        IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ParticipantId, int>> outgoingPreferencesByParticipant,
        IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ParticipantId, int>> requestedByParticipant)
    {
        _participantsById = participantsById
            ?? throw new ArgumentNullException(nameof(participantsById));
        _outgoingPreferencesByParticipant = outgoingPreferencesByParticipant
            ?? throw new ArgumentNullException(nameof(outgoingPreferencesByParticipant));
        _requestedByParticipant = requestedByParticipant
            ?? throw new ArgumentNullException(nameof(requestedByParticipant));
    }

    /// <summary>
    /// תפקיד הפונקציה: מחשבת ציון משולב לקבוצה מועמדת.
    /// קלט עיקרי: חברי יחידת השיבוץ, חברי הקבוצה המועמדת, קיבולת ומספר משתתפים שנותרו.
    /// פלט עיקרי: ציון מספרי — גבוה יותר = קבוצה עדיפה יותר.
    /// </summary>
    public double ScoreCandidate(
        IReadOnlyList<ParticipantId> unitParticipantIds,
        IReadOnlyList<ParticipantId> targetGroupMembers,
        int targetGroupCapacity,
        int remainingParticipantsToAssign)
    {
        if (unitParticipantIds is null)
        {
            throw new ArgumentNullException(nameof(unitParticipantIds));
        }

        if (targetGroupMembers is null)
        {
            throw new ArgumentNullException(nameof(targetGroupMembers));
        }

        var outgoingPreferenceScore = ScoreOutgoingPreferences(unitParticipantIds, targetGroupMembers);// סכום משקל העדפות יוצאות של יחידת השיבוץ כלפי חברי הקבוצה המועמדת
        var incomingPreferenceScore = ScoreIncomingPreferences(unitParticipantIds, targetGroupMembers);//הפוך....
        var isolationReductionScore = outgoingPreferenceScore > 0 || incomingPreferenceScore > 0 //בונוס קטן למשתתפים שמצמצמים בידוד (יש העדפות פנימיות)
            ? IsolationReductionBonus
            : 0.0;
        var capacityShapeScore = ScoreCapacityShape( // סכום משקל צורת הקבוצה
            unitParticipantIds.Count,
            targetGroupMembers.Count,
            targetGroupCapacity,
            remainingParticipantsToAssign);

        return outgoingPreferenceScore
            + incomingPreferenceScore
            + isolationReductionScore
            + capacityShapeScore;
    }

    /// <summary>
    /// תפקיד הפונקציה: מסכמת משקל העדפות יוצאות של יחידת השיבוץ כלפי חברי הקבוצה המועמדת.
    /// קלט עיקרי: מזהי משתתפי היחידה ורשימת חברי הקבוצה.
    /// פלט עיקרי: סכום משקלי דירוג (1/דירוג) להעדפות שמצביעות לתוך הקבוצה.
    /// </summary>
    private double ScoreOutgoingPreferences(
        IReadOnlyList<ParticipantId> unitParticipantIds,
        IReadOnlyList<ParticipantId> targetGroupMembers)
    {
        if (targetGroupMembers.Count == 0)
        {
            return 0.0;
        }

        var targetMemberSet = targetGroupMembers.ToHashSet();
        var score = 0.0;

        foreach (var participantId in unitParticipantIds)
        {
            if (!_participantsById.ContainsKey(participantId))
            {
                continue;
            }

            if (!_outgoingPreferencesByParticipant.TryGetValue(participantId, out var preferences))
            {
                continue;
            }

            foreach (var targetMember in targetMemberSet)
            {
                if (!_participantsById.ContainsKey(targetMember))
                {
                    continue;
                }

                if (preferences.TryGetValue(targetMember, out var rank))
                {
                    score += RankToScore(rank);
                }
            }
        }

        return score;
    }

    /// <summary>
    /// תפקיד הפונקציה: מסכמת משקל העדפות נכנסות — חברי הקבוצה שביקשו את משתתפי היחידה.
    /// קלט עיקרי: מזהי משתתפי היחידה ורשימת חברי הקבוצה.
    /// פלט עיקרי: סכום משקלי דירוג להעדפות נכנסות.
    /// </summary>
    private double ScoreIncomingPreferences(
        IReadOnlyList<ParticipantId> unitParticipantIds,
        IReadOnlyList<ParticipantId> targetGroupMembers)
    {
        if (targetGroupMembers.Count == 0)
        {
            return 0.0;
        }

        var targetMemberSet = targetGroupMembers.ToHashSet();
        var score = 0.0;

        foreach (var unitParticipantId in unitParticipantIds)
        {
            if (!_participantsById.ContainsKey(unitParticipantId))
            {
                continue;
            }

            if (!_requestedByParticipant.TryGetValue(unitParticipantId, out var requesters))
            {
                continue;
            }

            foreach (var targetMember in targetMemberSet)
            {
                if (!_participantsById.ContainsKey(targetMember))
                {
                    continue;
                }

                if (requesters.TryGetValue(targetMember, out var rank))
                {
                    score += RankToScore(rank);
                }
            }
        }

        return score;
    }

    /// <summary>
    /// תפקיד הפונקציה: מעניש קבוצה שתתמלא מוקדם מדי — שומר גמישות לשיבוץים הבאים.
    /// קלט עיקרי: גודל היחידה הנכנסת, מצב הקבוצה, קיבולת ומשתתפים שנותרו.
    /// פלט עיקרי: 0 או עונש שלילי קטן.
    /// </summary>
    private static double ScoreCapacityShape(
        int incomingCount,// גודל היחידה הנכנסת
        int currentGroupCount,// כמות המשתתפים בקבוצה הנוכחית
        int targetGroupCapacity,// קיבולת הקבוצה
        int remainingParticipantsToAssign)// כמות המשתתפים שעדיין לא שובצו
    {
        if (targetGroupCapacity == int.MaxValue || remainingParticipantsToAssign <= 0)
        {
            return 0.0;
        }

        var remainingCapacityAfterPlacement = targetGroupCapacity - currentGroupCount - incomingCount;
        return remainingCapacityAfterPlacement == 0
            ? -EarlyFullGroupPenalty
            : 0.0;
    }

    private static double RankToScore(int rank) => rank > 0 ? 1.0 / rank : 0.0;
}
