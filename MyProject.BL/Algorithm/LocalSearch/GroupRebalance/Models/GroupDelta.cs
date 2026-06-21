using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;

/// <summary>
/// שינוי נטו במספר המשתתפים בקבוצה אחרי רצף העברות.
/// ערך חיובי = הקבוצה קיבלה משתתפים; שלילי = איבדה.
/// </summary>
public sealed class GroupDelta
{
    public GroupDelta(GroupId groupId, int delta)
    {
        GroupId = groupId;
        Delta = delta;
    }

    public GroupId GroupId { get; }

    public int Delta { get; }
}
