using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// אינדקס לרכיבים קשירים בגרף קשרים הדדיים.
/// </summary>
/// <remarks>
/// <para>תפקיד: חלוקה לקלאסטרים (connected components) בגרף הדדי — לזיהוי קבוצות חברים סגורות.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (BuildClosedFriendGroupProfiles).</para>
/// </remarks>
public sealed class FriendClusterIndex
{
    private readonly Dictionary<ParticipantId, int> _clusterIdByParticipant;
    private readonly Dictionary<int, IReadOnlyList<ParticipantId>> _clustersById;

    private FriendClusterIndex(
        Dictionary<ParticipantId, int> clusterIdByParticipant,
        Dictionary<int, IReadOnlyList<ParticipantId>> clustersById)
    {
        _clusterIdByParticipant = clusterIdByParticipant;
        _clustersById = clustersById;
    }

    /// <summary>
    /// בונה אינדקס קלאסטרים מגרף קשרים הדדיים — DFS על רכיבים קשירים.
    /// </summary>
    /// <param name="mutualPreferenceIndex">אינדקס הדדיות — מגדיר את קשתות הגרף.</param>
    /// <param name="participants">כל המשתתפים — כולל בודדים ללא קשרים.</param>
    /// <returns>אינדקס עם מיפוי משתתף→clusterId ו-clusterId→חברים.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (שלב שלישי — אחרי MutualPreferenceIndex).</para>
    /// </remarks>
    public static FriendClusterIndex Create(
        MutualPreferenceIndex mutualPreferenceIndex,
        IReadOnlyList<Participant> participants)
    {
        if (mutualPreferenceIndex is null)
        {
            throw new ArgumentNullException(nameof(mutualPreferenceIndex));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        var adjacency = BuildAdjacency(mutualPreferenceIndex, participants);
        var clusterIdByParticipant = new Dictionary<ParticipantId, int>();
        var clustersById = new Dictionary<int, IReadOnlyList<ParticipantId>>();
        var visited = new HashSet<ParticipantId>();
        var nextClusterId = 0;

        // DFS על כל משתתף שלא בוקר — כל רכיב קשיר = קלאסטר.
        foreach (var participant in participants)
        {
            if (visited.Contains(participant.Id))
            {
                continue;
            }

            var clusterMembers = CollectConnectedComponent(participant.Id, adjacency, visited);
            clustersById[nextClusterId] = clusterMembers;

            foreach (var memberId in clusterMembers)
            {
                clusterIdByParticipant[memberId] = nextClusterId;
            }

            nextClusterId++;
        }

        return new FriendClusterIndex(clusterIdByParticipant, clustersById);
    }

    /// <summary>
    /// מחזיר את כל חברי הקלאסטר של participantId.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <returns>רשימת חברי הקלאסטר; ריקה אם המשתתף לא נמצא.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבחירת moves בתוך/בין קלאסטרים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> GetClusterOfParticipant(ParticipantId participantId)
    {
        if (!_clusterIdByParticipant.TryGetValue(participantId, out var clusterId))
        {
            return Array.Empty<ParticipantId>();
        }

        return _clustersById[clusterId];
    }

    /// <summary>
    /// מחזיר את כל הקלאסטרים — ממוינים לפי clusterId.
    /// </summary>
    /// <returns>רשימת קלאסטרים; כל קלאסטר הוא רשימת ParticipantId.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (BuildClosedFriendGroupProfiles).</para>
    /// </remarks>
    public IReadOnlyList<IReadOnlyList<ParticipantId>> GetClusters()
    {
        return _clustersById
            .OrderBy(entry => entry.Key)
            .Select(entry => entry.Value)
            .ToList();
    }

    /// <summary>
    /// בודק האם שני משתתפים באותו קלאסטר חברים.
    /// </summary>
    /// <param name="participantA">משתתף ראשון.</param>
    /// <param name="participantB">משתתף שני.</param>
    /// <returns>true אם שניהם באותו connected component; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לסינון moves שמפרידים קלאסטר — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public bool IsSameFriendCluster(ParticipantId participantA, ParticipantId participantB)
    {
        // TryGetValue על שני המשתתפים — clusterA==clusterB אם באותו רכיב קשיר.
        return _clusterIdByParticipant.TryGetValue(participantA, out var clusterA)
            && _clusterIdByParticipant.TryGetValue(participantB, out var clusterB)
            && clusterA == clusterB;
    }

    internal int GetClusterId(ParticipantId participantId)
    {
        return _clusterIdByParticipant.TryGetValue(participantId, out var clusterId)
            ? clusterId
            : -1;
    }

    private static Dictionary<ParticipantId, HashSet<ParticipantId>> BuildAdjacency(
        MutualPreferenceIndex mutualPreferenceIndex,
        IReadOnlyList<Participant> participants)
    {
        var adjacency = new Dictionary<ParticipantId, HashSet<ParticipantId>>();

        // לכל משתתף — HashSet של שכנים (קשרים הדדיים) ל-DFS.
        foreach (var participant in participants)
        {
            adjacency[participant.Id] = mutualPreferenceIndex
                .GetMutualPreferences(participant.Id)
                .ToHashSet();
        }

        return adjacency;
    }

    private static IReadOnlyList<ParticipantId> CollectConnectedComponent(
        ParticipantId startParticipantId,
        Dictionary<ParticipantId, HashSet<ParticipantId>> adjacency,
        HashSet<ParticipantId> visited)
    {
        var clusterMembers = new List<ParticipantId>();
        var stack = new Stack<ParticipantId>();
        stack.Push(startParticipantId);

        while (stack.Count > 0)
        {
            var currentId = stack.Pop();

            // visited.Add מחזיר false אם כבר בוקר — מדלגים על צומת כפול.
            if (!visited.Add(currentId))
            {
                continue;
            }

            clusterMembers.Add(currentId);

            if (!adjacency.TryGetValue(currentId, out var neighbors))
            {
                continue;
            }

            foreach (var neighborId in neighbors)
            {
                if (!visited.Contains(neighborId))
                {
                    stack.Push(neighborId);
                }
            }
        }

        // OrderBy — סדר יציב לרשימת חברי הקלאסטר.
        return clusterMembers
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToList();
    }
}
