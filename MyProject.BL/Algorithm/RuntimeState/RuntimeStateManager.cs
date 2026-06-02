using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.RuntimeState;

/// <summary>
/// ניהול מצב בזמן ריצה — מחזיק את מצב החלוקה הנוכחי ומסנכרן עם מעקב מצבים שביקרו.
/// </summary>
/// <remarks>
/// <para>תפקיד: עטיפה ל-<see cref="AssignmentState"/> הנוכחי ול-<see cref="VisitedStateTracker"/> — מקור אמת יחיד במהלך לולאת Local Search.</para>
/// <para>נקרא מ-: מתזמר Local Search עתידי — אין שימוש חיצוני כרגע.</para>
/// </remarks>
public sealed class RuntimeStateManager
{
    private readonly VisitedStateTracker _visitedStateTracker;

    /// <summary>
    /// מאתחל מנהל מצב ריצה עם מצב התחלתי ומעקב ביקורים.
    /// </summary>
    /// <param name="initialState">מצב החלוקה ההתחלתי (מ-<see cref="Initialization.AssignmentStateFactory"/>).</param>
    /// <param name="visitedStateTracker">עוקב מצבים שביקרו — משותף לכל החיפוש.</param>
    /// <remarks>
    /// <para>נקרא מ-: מתזמר Local Search עתידי — אין קריאות חיצוניות כרגע.</para>
    /// </remarks>
    public RuntimeStateManager(
        AssignmentState initialState,
        VisitedStateTracker visitedStateTracker)
    {
        CurrentState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        _visitedStateTracker = visitedStateTracker ?? throw new ArgumentNullException(nameof(visitedStateTracker));
    }

    /// <summary>
    /// מצב החלוקה הנוכחי — מתעדכן לאחר כל move שמתקבל.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: גישה למיפויים, ציון ו-hash של הפתרון הפעיל.</para>
    /// <para>נקרא מ-: MoveExecutor, MoveEvaluator ו-RuntimeDataBuilder עתידיים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public AssignmentState CurrentState { get; private set; }

    /// <summary>
    /// מסמן את מצב החלוקה הנוכחי כמצב שכבר נבדק.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: לולאת Local Search עתידית — לאחר הערכת move וקבלתו.</para>
    /// </remarks>
    public void MarkCurrentAsVisited() =>
        // CurrentHash — מזהה ייחודי למצב החלוקה הנוכחי.
        _visitedStateTracker.MarkSeen(CurrentState.CurrentHash);

    /// <summary>
    /// בודק האם מצב החלוקה הנוכחי כבר נבדק בעבר.
    /// </summary>
    /// <returns>true אם hash הנוכחי כבר ב-VisitedStateTracker; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveEvaluator עתידי — לדילוג על moves שמובילים למצב שכבר נראה.</para>
    /// </remarks>
    public bool HasSeenCurrentState() =>
        _visitedStateTracker.HasSeen(CurrentState.CurrentHash);
}
