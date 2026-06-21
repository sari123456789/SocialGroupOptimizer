using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד המחלקה: בניית יחידות חובה מזוגות חובה באמצעות Union-Find — מבנה איחוד קבוצות.
/// המחלקה משתתפת בשלב הכנה — לפני שיבוץ וגרף קונפליקטים.
/// </summary>
/// <remarks>
/// שרשרת זוגות חובה הופכת ליחידת שיבוץ אחת שאסור לפצל.
/// </remarks>
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
        // מפתח = מזהה משתתף, ערך = ההורה שלו בעץ איחוד.
        //
        // בהתחלה כל משתתף הוא שורש של עצמו:
        // כלומר כל משתתף הוא יחידה נפרדת.

        // למה זה חשוב?
        // כי בהמשך כל "זוג חובה" יאחד שני עצים בתוך אותו מילון.
        Dictionary<ParticipantId, ParticipantId> parentByParticipant = input.Participants
            .Select(participant => participant.Id)
            .Distinct()
            // כל משתתף מתחיל כשהורה שלו הוא הוא עצמו.
            .ToDictionary(id => id, id => id);

        // 1) EnsureParticipant - מוודאים ששני המשתתפים קיימים במילון העבודה.
        // 2) Union - מאחדים את שתי הקבוצות לקבוצה אחת.
        foreach (MandatoryPairConstraint pair in mandatoryPairs)
        {
            EnsureParticipant(parentByParticipant, pair.ParticipantA);
            EnsureParticipant(parentByParticipant, pair.ParticipantB);
            // התחביר הוא קריאת מתודה רגילה:
            // שם-מתודה(פרמטר1, פרמטר2, ...).
            Union(parentByParticipant, pair.ParticipantA, pair.ParticipantB);
        }



        //לאיזו יחידה שייך משתתף מסוים? - מילון זה ייבנה תוך כדי מעבר על המשתתפים.
        Dictionary<ParticipantId, int> unitIdByParticipant = new Dictionary<ParticipantId, int>();
        // מי משתתף ביחידה מסוימת? - מילון זה ייבנה תוך כדי מעבר על המשתתפים.
        Dictionary<int, List<ParticipantId>> units = new Dictionary<int, List<ParticipantId>>();
        //מיפוי משתתף לאבא שלו, כדי לתת מספר יחידה רציף לכל שורש.
        Dictionary<ParticipantId, int> rootToUnitId = new Dictionary<ParticipantId, int>();

        // עוברים על כל המשתתפים שכבר מוכרים במבנה.
        // ToList יוצר צילום של המפתחות לרגע הנתון.
        // זה מונע בעיות אם המילון היה משתנה תוך כדי מעבר.
        foreach (ParticipantId participantId in parentByParticipant.Keys.ToList())
        {

            ParticipantId root = Find(parentByParticipant, participantId);

            if (!rootToUnitId.TryGetValue(root, out int unitId))
            {
                // אם זה שורש חדש, נותנים לו מזהה רציף חדש.
                unitId = rootToUnitId.Count;
                rootToUnitId[root] = unitId;
                units[unitId] = new List<ParticipantId>();
            }

            // שיוך דו-כיווני:
            unitIdByParticipant[participantId] = unitId;
            units[unitId].Add(participantId);
        }

        // כאן נבנה אובייקט הפלט הסופי של שלב זה.

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
    /// תפקיד הפונקציה: מוצאת שורש הקבוצה ב-Union-Find — עם דחיסת נתיב.
    /// קלט עיקרי: מילון הורים, מזהה משתתף.
    /// פלט עיקרי: מזהה שורש היחידה.
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
    /// תפקיד הפונקציה: מאחדת שתי קבוצות משתתפים ליחידת חובה אחת.
    /// קלט עיקרי: מילון הורים ושני מזהי משתתפים.
    /// פלט עיקרי: עדכון in-place של מבנה Union-Find.
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
