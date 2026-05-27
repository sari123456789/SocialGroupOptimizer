using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Entities;

/// <summary>
/// משתתף והעדפותיו החברתיות המדורגות.
/// </summary>
public sealed class Participant
{
    /// <summary>
    /// מאתחל מופע חדש של המחלקה <see cref="Participant"/>.
    /// </summary>
    /// <param name="id">מזהה המשתתף.</param>
    /// <param name="classifications">מיפוי מימד סיווג לרמת ערך (לפחות מימד אחד).</param>
    /// <param name="preferences">רשימת העדפות מדורגות של המשתתף.</param>
    public Participant(
        ParticipantId id,
        IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode> classifications,
        IEnumerable<Preference> preferences)
    {
        if (classifications is null)
        {
            throw new ArgumentNullException(nameof(classifications));
        }

        if (preferences is null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }

        if (classifications.Count == 0)
        {
            throw new ArgumentException("Participant must have at least one classification dimension.", nameof(classifications));
        }

        var classificationsCopy = new Dictionary<ClassificationDimensionCode, ClassificationLevelCode>(classifications.Count);
        foreach (var (dimension, level) in classifications)
        {
            if (classificationsCopy.ContainsKey(dimension))
            {
                throw new ArgumentException("Participant classifications cannot contain duplicate dimensions.", nameof(classifications));
            }

            classificationsCopy[dimension] = level;
        }

        var preferencesList = preferences.ToList();

        if (preferencesList.Any(p => p is null))
        {
            throw new ArgumentException("Participant preferences cannot contain null entries.", nameof(preferences));
        }

        if (preferencesList.Any(p => p.PreferredParticipantId == id))
        {
            throw new ArgumentException("Participant cannot prefer themselves.", nameof(preferences));
        }

        if (preferencesList.Select(p => p.PreferredParticipantId).Distinct().Count() != preferencesList.Count)
        {
            throw new ArgumentException("Participant preferences cannot contain duplicate preferred participant ids.", nameof(preferences));
        }

        if (preferencesList.Select(p => p.Rank).Distinct().Count() != preferencesList.Count)
        {
            throw new ArgumentException("Participant preferences cannot contain duplicate ranks.", nameof(preferences));
        }

        Id = id;
        Classifications = new ReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>(classificationsCopy);
        Preferences = new ReadOnlyCollection<Preference>(preferencesList);
    }

    /// <summary>
    /// מחזיר את מזהה המשתתף.
    /// </summary>
    public ParticipantId Id { get; }

    /// <summary>
    /// סיווגי המשתתף: מפתח = מימד (למשל "רמה"), ערך = רמה בתוך המימד (למשל "מתחיל").
    /// </summary>
    /// <remarks>נבנה מ־Data דרך <c>ParticipantMapper</c> לפי שורות <c>ParticipantClassification</c> של אותה ריצת שיבוץ.</remarks>
    public IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode> Classifications { get; }

    /// <summary>
    /// מחזיר את רשימת ההעדפות המדורגות של המשתתף.
    /// </summary>
    public IReadOnlyList<Preference> Preferences { get; }
}
