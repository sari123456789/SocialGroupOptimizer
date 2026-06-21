namespace MyProject.BL.Algorithm.LocalSearch.Engine;

/// <summary>
/// חוזה למנוע חיפוש מקומי — שיפור AssignmentState קיים.
/// </summary>
public interface ILocalSearchEngine
{
    /// <summary>
    /// מריץ לולאת חיפוש מקומי ומחזיר תוצאה עם מצב סופי וסיבת עצירה.
    /// </summary>
    /// <param name="input">קלט — מצב, משתתפים, אילוצים ומשקלות.</param>
    /// <returns>תוצאת החיפוש.</returns>
    LocalSearchResult Improve(LocalSearchInput input);

    
}
