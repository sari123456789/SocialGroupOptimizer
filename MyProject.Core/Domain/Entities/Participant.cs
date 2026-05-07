using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyProject.Core.Domain.Enums;
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
    /// <param name="classifications">רשימת סיווגי המשתתף.</param>
    /// <param name="preferences">רשימת העדפות מדורגות של המשתתף.</param>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="classifications"/> או <paramref name="preferences"/> הוא null.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר מצב המשתתף אינו חוקי.</exception>
    public Participant(
        ParticipantId id,
        IEnumerable<ClassificationType> classifications,
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

        var classificationsList = classifications.ToList();
        var preferencesList = preferences.ToList();

        if (classificationsList.Count == 0)
        {
            throw new ArgumentException("Participant must have at least one classification.", nameof(classifications));
        }

        if (classificationsList.Any(c => c == ClassificationType.Unspecified))
        {
            throw new ArgumentException("Participant classifications cannot include Unspecified.", nameof(classifications));
        }

        if (classificationsList.Distinct().Count() != classificationsList.Count)
        {
            throw new ArgumentException("Participant classifications cannot contain duplicates.", nameof(classifications));
        }

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
        Classifications = new ReadOnlyCollection<ClassificationType>(classificationsList);
        Preferences = new ReadOnlyCollection<Preference>(preferencesList);
    }

    /// <summary>
    /// מחזיר את מזהה המשתתף.
    /// </summary>
    public ParticipantId Id { get; }

    /// <summary>
    /// מחזיר את סיווגי המשתתף.
    /// </summary>
    public IReadOnlyList<ClassificationType> Classifications { get; }

    /// <summary>
    /// מחזיר את רשימת ההעדפות המדורגות של המשתתף.
    /// </summary>
    public IReadOnlyList<Preference> Preferences { get; }
}
