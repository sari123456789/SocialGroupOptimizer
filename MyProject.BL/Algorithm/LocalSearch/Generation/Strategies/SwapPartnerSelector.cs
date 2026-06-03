using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;

namespace MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;

/// <summary>
/// עזר לבחירת בן-זוג להחלפה בקבוצה — החבר עם התרומה הנמוכה ביותר.
/// </summary>
/// <remarks>
/// <para>תפקיד: בחירה דטרמיניסטית — מינימום PreferredInsideCount, שובר שוויון לפי מזהה.</para>
/// <para>נקרא מ-: NearMissStrategy, LowScoreGroupStrategy.</para>
/// <para>internal static — נגיש רק בתוך האסמבלי BL, לא API חיצוני.</para>
/// </remarks>
internal static class SwapPartnerSelector
{
    /// <summary>
    /// בוחר את החבר בקבוצה עם הכי מעט מועדפים-בפנים, שאינו ב-excludedIds.
    /// </summary>
    /// <param name="groupId">קבוצת היעד.</param>
    /// <param name="snapshot">פרופילי קבוצה ומשתתף.</param>
    /// <param name="excludedIds">HashSet — Contains ב-O(1); משתתפים שלא מותר להחליף.</param>
    /// <returns>ParticipantId? — nullable; null אם אין חבר מתאים.</returns>
    public static ParticipantId? SelectLowestContributionMember(
        GroupId groupId,
        RuntimeDataSnapshot snapshot,
        HashSet<ParticipantId> excludedIds)
    {
        // TryGetValue — לא זורק אם הקבוצה חסרה; out var מחזיר את הפרופיל.
        if (!snapshot.GroupProfiles.TryGetValue(groupId, out var groupProfile))
        {
            return null;
        }

        ParticipantId? bestPartner = null;
        var bestInsideCount = int.MaxValue;

        foreach (var memberId in groupProfile.Participants)
        {
            if (excludedIds.Contains(memberId))
            {
                continue;
            }

            // תנאי תלתי מקונן: אם אין פרופיל — 0 (תרומה מינימלית לצורך השוואה).
            var insideCount = snapshot.ParticipantProfiles.TryGetValue(memberId, out var memberProfile)
                ? memberProfile.PreferredInsideCount
                : 0;

            // isBetter: פחות מועדפים-בפנים = טוב יותר; בשוויון — מזהה קטן יותר (Ordinal).
            // pattern "is not null" — C# 9; מונע השוואה כש-bestPartner עדיין null.
            var isBetter = insideCount < bestInsideCount
                || (insideCount == bestInsideCount
                    && bestPartner is not null
                    && string.CompareOrdinal(memberId.Value, bestPartner.Value.Value) < 0);

            if (isBetter)
            {
                bestInsideCount = insideCount;
                bestPartner = memberId;
            }
        }

        return bestPartner;
    }
}
