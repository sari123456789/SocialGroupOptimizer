using System.Globalization;
using MyProject.BL.Algorithm.LocalSearch.Moves;

namespace MyProject.BL.Algorithm.LocalSearch.Generation;

/// <summary>
/// מנקה כפילויות בין הצעות move מאסטרטגיות שונות.
/// </summary>
/// <remarks>
/// <para>תפקיד: מפתח קנוני לכל move — שומר את ההצעה הראשונה (כולל Source/Priority של האסטרטגיה הראשונה).</para>
/// <para>נקרא מ-: <see cref="SwapMoveGenerator"/> (אחרי איחוד, לפני דירוג).</para>
/// <para>Swap(A,B) ≡ Swap(B,A) — המפתח לא תלוי בסדר המשתתפים.</para>
/// </remarks>
public static class CandidateDeduplicator
{
    /// <summary>
    /// מסיר הצעות כפולות לפי מפתח קנוני; שומר את הראשונה לכל מפתח.
    /// </summary>
    public static IReadOnlyList<CandidateProposal> Deduplicate(IReadOnlyList<CandidateProposal> proposals)
    {
        if (proposals is null)
        {
            throw new ArgumentNullException(nameof(proposals));
        }

        // HashSet עם StringComparer.Ordinal — השוואת מפתחות תלוי-רישיות אנגלית, מהיר ועקבי.
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<CandidateProposal>(proposals.Count);

        foreach (var proposal in proposals)
        {
            var key = BuildKey(proposal.Move);

            // HashSet.Add: מוסיף את המפתח ומחזיר true; אם כבר קיים — false (מדלגים על כפילות).
            if (seenKeys.Add(key))
            {
                result.Add(proposal);
            }
        }

        return result;
    }

    /// <summary>
    /// בונה מפתח קנוני ל-move — בלתי-תלוי בסדר עבור Swap.
    /// </summary>
    private static string BuildKey(MoveCandidate move)
    {
        // switch expression — מחזיר ערך לפי MoveType; _ הוא default שזורק חריגה.
        return move.MoveType switch
        {
            // Transfer — כיווני: T|משתתף|מקור|יעד (סדר קבוע).
            MoveType.Transfer => string.Create(
                CultureInfo.InvariantCulture,
                $"T|{move.FirstParticipantId.Value}|{move.SourceGroupId.Value}|{move.TargetGroupId.Value}"),

            // Swap — קורא לפונקציה נפרדת כי הלוגיקה מורכבת (מיון שני קצוות).
            MoveType.Swap => BuildSwapKey(move),

            _ => throw new ArgumentOutOfRangeException(nameof(move), move.MoveType, "Unsupported move type."),
        };
    }

    /// <summary>
    /// מפתח Swap קנוני — מסדר את שני הקצוות לפי מזהה המשתתף.
    /// </summary>
    private static string BuildSwapKey(MoveCandidate move)
    {
        // ?? throw — אם SecondParticipantId הוא null, זורקים (Swap חייב שני משתתפים).
        var secondId = move.SecondParticipantId
            ?? throw new ArgumentException("Swap candidate must have a second participant.", nameof(move));

        // tuple ללא שם — (string, string) לקצה; named tuple ב-deconstruction למטה.
        var firstEndpoint = (Participant: move.FirstParticipantId.Value, Group: move.SourceGroupId.Value);
        var secondEndpoint = (Participant: secondId.Value, Group: move.TargetGroupId.Value);

        // deconstruction עם תנאי: (low, high) מקבלים את הזוג הממוין לפי Participant.
        // string.CompareOrdinal — השוואה בייט-אחר-בייט, לא תרבותית.
        var (low, high) = string.CompareOrdinal(firstEndpoint.Participant, secondEndpoint.Participant) <= 0
            ? (firstEndpoint, secondEndpoint)
            : (secondEndpoint, firstEndpoint);

        // string.Create + InvariantCulture — בניית מחרוזת בלי הקצאות ביניים מיותרות.
        return string.Create(
            CultureInfo.InvariantCulture,
            $"S|{low.Participant}@{low.Group}|{high.Participant}@{high.Group}");
    }
}
