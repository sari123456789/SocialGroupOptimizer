using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Algorithm.InitialPlacement.Placement;

/// <summary>
/// תפקיד: מעטפת תוצאה לניסיון בנייה חמדנית — מציינת האם נוצרו קבוצות ואילו שגיאות נאספו.
/// </summary>
/// <param name="Groups">קבוצות שנבנו; null כשהבנייה נכשלה לפני השלמת חלוקה.</param>
/// <param name="Errors">הודעות כשלון; ריקה כשהבנייה הצליחה.</param>
/// <remarks>
/// נקרא מ- <see cref="GreedyPlacementBuilder.TryBuild"/> — מחזיר מופע זה בכל מסלול הצלחה או כישלון.
/// </remarks>
public sealed record GreedyBuildResult(IReadOnlyList<Group>? Groups, IReadOnlyList<string> Errors);
