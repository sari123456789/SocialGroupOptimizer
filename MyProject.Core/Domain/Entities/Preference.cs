using System;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Entities;

/// <summary>
/// העדפה חברתית מדורגת של משתתף.
/// </summary>
public sealed class Preference
{
    /// <summary>
    /// מאתחל מופע חדש של המחלקה <see cref="Preference"/>.
    /// </summary>
    /// <param name="preferredParticipantId">מזהה המשתתף המועדף.</param>
    /// <param name="rank">דרגת ההעדפה. ערכים נמוכים יותר מייצגים העדפה חזקה יותר.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="rank"/> קטן או שווה לאפס.</exception>
    public Preference(ParticipantId preferredParticipantId, int rank)
    {
        if (rank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), "Preference rank must be greater than zero.");
        }

        PreferredParticipantId = preferredParticipantId;
        Rank = rank;
    }

    /// <summary>
    /// מחזיר את מזהה המשתתף המועדף.
    /// </summary>
    public ParticipantId PreferredParticipantId { get; }

    /// <summary>
    /// מחזיר את דרגת ההעדפה.
    /// </summary>
    public int Rank { get; }
}
