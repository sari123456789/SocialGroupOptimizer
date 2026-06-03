using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;

/// <summary>
/// אסטרטגיית בחירה אקראית מבוקרת.
/// </summary>
/// <remarks>
/// <para>בעיה: סטגנציה — החיפוש נתקע במינימום מקומי ואין מועמדים מכוונים טובים.</para>
/// <para>פתרון: ייצור מספר מצומצם של החלפות אקראיות בין קבוצות, ל-diversification.</para>
/// <para>שימוש: fallback בלבד — עדיפות נמוכה ביותר; seed קבוע ל-reproducibility.</para>
/// <para>אסור: הערכת ציון, אימות חוקיות או ביצוע ה-move.</para>
/// </remarks>
public sealed class ControlledRandomStrategy : IMoveCandidateStrategy
{
    // seed ברירת מחדל — ריצות חוזרות מייצרות אותה סדרה אקראית.
    private const int DefaultSeed = 12345;

    // מספר מצומצם של מועמדים אקראיים — diversification בלי הצפה.
    private const int MaxRandomCandidates = 8;

    // עדיפות נמוכה ביותר — מבטיח שהאקראי ידורג מתחת לכל מהלך מכוון.
    private const double FallbackPriority = -1.0;

    private readonly Random _random;

    /// <summary>
    /// יוצר את האסטרטגיה עם seed ברירת המחדל.
    /// </summary>
    public ControlledRandomStrategy()
        : this(DefaultSeed)
    {
    }

    /// <summary>
    /// יוצר את האסטרטגיה עם seed מפורש — לשליטה ב-reproducibility בבדיקות.
    /// </summary>
    /// <param name="seed">זרע למחולל האקראי.</param>
    public ControlledRandomStrategy(int seed)
    {
        _random = new Random(seed);
    }

    /// <inheritdoc />
    public CandidateSource Source => CandidateSource.ControlledRandom;

    /// <inheritdoc />
    public bool IsApplicable(RuntimeDataSnapshot snapshot, MoveGenerationContext context)
    {
        if (snapshot is null || context is null)
        {
            return false;
        }

        // Count עם lambda — סופר קבוצות עם לפחות משתתף אחד; צריך ≥2 להחלפה בין קבוצות.
        return snapshot.GroupProfiles.Values.Count(group => group.Participants.Count > 0) >= 2;
    }

    /// <inheritdoc />
    public IReadOnlyList<CandidateProposal> Generate(
        AssignmentState state,
        RuntimeDataSnapshot snapshot,
        MoveGenerationContext context)
    {
        if (state is null || snapshot is null || context is null)
        {
            return Array.Empty<CandidateProposal>();
        }

        // רק קבוצות לא-ריקות מועמדות להחלפה.
        var nonEmptyGroups = snapshot.GroupProfiles.Values
            .Where(group => group.Participants.Count > 0)
            .OrderBy(group => group.GroupId.Value)
            .ToList();

        if (nonEmptyGroups.Count < 2)
        {
            return Array.Empty<CandidateProposal>();
        }

        var proposals = new List<CandidateProposal>();

        for (var attempt = 0; attempt < MaxRandomCandidates; attempt++)
        {
            // Random.Next(upperBound) — מחזיר [0, upperBound) שלם; אינדקס לרשימת הקבוצות.
            // אם אותו אינדקס — continue מדלג על ניסיון (לא Swap בתוך אותה קבוצה).
            var firstGroupIndex = _random.Next(nonEmptyGroups.Count);
            var secondGroupIndex = _random.Next(nonEmptyGroups.Count);
            if (firstGroupIndex == secondGroupIndex)
            {
                continue;
            }

            var firstGroup = nonEmptyGroups[firstGroupIndex];
            var secondGroup = nonEmptyGroups[secondGroupIndex];

            // משתתף אקראי מכל קבוצה.
            var firstParticipant = firstGroup.Participants[_random.Next(firstGroup.Participants.Count)];
            var secondParticipant = secondGroup.Participants[_random.Next(secondGroup.Participants.Count)];

            var move = MoveCandidate.Swap(
                firstParticipant,
                secondParticipant,
                firstGroup.GroupId,
                secondGroup.GroupId);

            var reason = $"Controlled random swap between groups {firstGroup.GroupId.Value} and {secondGroup.GroupId.Value} (diversification).";
            proposals.Add(new CandidateProposal(move, CandidateSource.ControlledRandom, reason, FallbackPriority));
        }

        return proposals;
    }
}
