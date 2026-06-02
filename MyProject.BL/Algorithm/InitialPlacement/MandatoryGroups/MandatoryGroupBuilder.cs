using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// בונה יחידות חובה מזוגות חובה.
/// </summary>
public static class MandatoryGroupBuilder
{
    /// <summary>
    /// בונה יחידות חובה לפי קשרים מחוברים.
    /// </summary>
    /// <remarks>
    /// אם א חייב להיות עם ב, וב חייב להיות עם ג,
    /// אז א, ב וג הופכים ליחידת חובה אחת.
    /// </remarks>
    public static MandatoryUnitMap Build(
        InitialPlacementInput input,
        IReadOnlyList<MandatoryPairConstraint> mandatoryPairs)
    {
        // המילון הזה הוא "מצב העבודה" של האלגוריתם:
        // מפתח = מזהה משתתף, ערך = ההורה שלו בעץ איחוד.
        //
        // בהתחלה כל משתתף הוא שורש של עצמו:
        // כלומר כל משתתף הוא יחידה נפרדת.
        //
        // תחביר השרשור:
        // 1) Select  - לוקח מכל אובייקט משתתף רק את המזהה.
        // 2) Distinct - מוודא שאין כפילויות מזהים.
        // 3) ToDictionary - בונה מילון key/value.
        //
        // למה זה חשוב?
        // כי בהמשך כל "זוג חובה" יאחד שני עצים בתוך אותו מילון.
        Dictionary<ParticipantId, ParticipantId> parentByParticipant = input.Participants
            .Select(participant => participant.Id)
            .Distinct()
            // כל משתתף מתחיל כשהורה שלו הוא הוא עצמו.
            .ToDictionary(id => id, id => id);

        // כאן עוברים על כל אילוצי "חייבים להיות יחד".
        // לכל זוג עושים שני צעדים:
        // 1) EnsureParticipant - מוודאים ששני המשתתפים קיימים במילון העבודה.
        //    אם אחד לא קיים, זורקים חריגה (לא מוסיפים "בשקט").
        // 2) Union - מאחדים את שתי הקבוצות לקבוצה אחת.
        // המתודות האלה ממומשות בהמשך אותו קובץ.
        foreach (MandatoryPairConstraint pair in mandatoryPairs)
        {
            EnsureParticipant(parentByParticipant, pair.ParticipantA);
            EnsureParticipant(parentByParticipant, pair.ParticipantB);
            // התחביר הוא קריאת מתודה רגילה:
            // שם-מתודה(פרמטר1, פרמטר2, ...).
            Union(parentByParticipant, pair.ParticipantA, pair.ParticipantB);
        }

        // עד כאן יש לנו "מבנה עצים" פנימי.
        // מכאן ממירים אותו לפלט ברור:
        // - עבור כל משתתף: מה מזהה היחידה שלו.
        // - עבור כל יחידה: מי המשתתפים שבתוכה.
        //
        // למה צריך שלושה מילונים?
        // 1) unitIdByParticipant:
        //    מענה לשאלה "לאיזו יחידה שייך משתתף מסוים?"
        // 2) units:
        //    מענה לשאלה "מי החברים של יחידה מסוימת?"
        // 3) rootToUnitId:
        //    מיפוי זמני כדי לתת מספר רציף לכל שורש שמתגלה.

        Dictionary<ParticipantId, int> unitIdByParticipant = new Dictionary<ParticipantId, int>();
        Dictionary<int, List<ParticipantId>> units = new Dictionary<int, List<ParticipantId>>();
        Dictionary<ParticipantId, int> rootToUnitId = new Dictionary<ParticipantId, int>();

        // עוברים על כל המשתתפים שכבר מוכרים במבנה.
        // ToList יוצר צילום של המפתחות לרגע הנתון.
        // זה מונע בעיות אם המילון היה משתנה תוך כדי מעבר.
        foreach (ParticipantId participantId in parentByParticipant.Keys.ToList())
        {
            // Find (ממומשת בהמשך) מחזירה את השורש של המשתתף.
            // כל מי שמגיע לאותו שורש שייך לאותה יחידה.
            ParticipantId root = Find(parentByParticipant, participantId);

            // TryGetValue:
            // בודקת אם כבר נתנו מספר יחידה לשורש הזה.
            // out var unitId = ערך שיוחזר אם נמצא.
            // הסימן ! לפני הקריאה אומר "אם לא נמצא".
            if (!rootToUnitId.TryGetValue(root, out int unitId))
            {
                // אם זה שורש חדש, נותנים לו מזהה רציף חדש.
                unitId = rootToUnitId.Count;
                rootToUnitId[root] = unitId;
                units[unitId] = new List<ParticipantId>();
            }

            // שיוך דו-כיווני:
            // משתתף -> יחידה
            // יחידה -> הוספת משתתף לרשימת חברים
            unitIdByParticipant[participantId] = unitId;
            units[unitId].Add(participantId);
        }

        // כאן נבנה אובייקט הפלט הסופי של שלב זה.
        // המחלקה MandatoryUnitMap מוגדרת בקובץ נפרד
        // ומשמשת אחר כך בשלבי גרף קונפליקטים, שיבוץ ותיקון.
        return new MandatoryUnitMap(unitIdByParticipant, units);
    }

    /// <summary>
    /// מוודא שמשתתף קיים במבנה Union-Find.
    /// </summary>
    /// <remarks>
    /// אם אילוץ מפנה למשתתף שלא קיים ברשימת המשתתפים,
    /// זו סתירת קלט ולכן זורקים חריגה.
    /// </remarks>
    private static void EnsureParticipant(IDictionary<ParticipantId, ParticipantId> parentByParticipant, ParticipantId participantId)
    {
        // ContainsKey בודקת אם מפתח כבר קיים במילון.
        if (!parentByParticipant.ContainsKey(participantId))
        {
            throw new InvalidOperationException(
                $"Mandatory pair references unknown participant '{participantId}'.");
        }
    }

    /// <summary>
    /// מוצא את שורש הקבוצה של משתתף, עם דחיסת נתיב.
    /// </summary>
    private static ParticipantId Find(IDictionary<ParticipantId, ParticipantId> parentByParticipant, ParticipantId participantId)
    {
        // גישה לערך במילון לפי מפתח.
        ParticipantId parent = parentByParticipant[participantId];
        if (parent == participantId)
        {
            // תנאי עצירה של הרקורסיה:
            // אם ההורה הוא עצמו, הגענו לשורש.
            return participantId;
        }

        // קריאה רקורסיבית: ממשיכים לעלות בעץ עד שמוצאים שורש.
        ParticipantId root = Find(parentByParticipant, parent);

        // דחיסת נתיב:
        // אחרי שמצאנו שורש, מקצרים את הדרך ישירות אליו.
        // זה משפר ביצועים משמעותית בקריאות Find עתידיות.
        parentByParticipant[participantId] = root;
        return root;
    }

    /// <summary>
    /// מאחד שתי קבוצות משתתפים לאותה יחידת חובה.
    /// </summary>
    private static void Union(
        IDictionary<ParticipantId, ParticipantId> parentByParticipant,
        ParticipantId firstParticipantId,
        ParticipantId secondParticipantId)
    {
        // Find לשני המשתתפים:
        // כל אחד מקבל את השורש של הקבוצה שאליה הוא שייך כרגע.
        ParticipantId firstRoot = Find(parentByParticipant, firstParticipantId);
        ParticipantId secondRoot = Find(parentByParticipant, secondParticipantId);

        // אם השורשים שונים, אלה שתי קבוצות שונות ולכן מאחדים אותן.
        if (firstRoot != secondRoot)
        {
            // פעולת האיחוד:
            // הופכים את secondRoot לילד של firstRoot.
            // המשמעות: שתי הקבוצות הפכו לקבוצה אחת.
            parentByParticipant[secondRoot] = firstRoot;
        }
    }
}
