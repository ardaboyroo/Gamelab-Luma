using System.Collections.Generic;
using UnityEngine;

/// Remembers which tutorials the player has already read.
/// By default only for the current play session; turn on SaveBetweenSessions to keep it on the device.
public static class TutorialProgress
{
    const string KeyPrefix = "tutorial_seen_";

    public static bool SaveBetweenSessions;

    static readonly HashSet<string> seen = new();

    // Also reset when 'Enter Play Mode Options' skip the domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        seen.Clear();
        SaveBetweenSessions = false;
    }

    public static bool HasSeen(TutorialEntry entry)
    {
        if (entry == null) return true;
        if (seen.Contains(entry.name)) return true;
        return SaveBetweenSessions && PlayerPrefs.GetInt(KeyPrefix + entry.name, 0) == 1;
    }

    public static void MarkSeen(TutorialEntry entry)
    {
        if (entry == null) return;
        seen.Add(entry.name);

        if (SaveBetweenSessions)
        {
            PlayerPrefs.SetInt(KeyPrefix + entry.name, 1);
            PlayerPrefs.Save();
        }
    }

    public static void ResetAll(IEnumerable<TutorialEntry> entries)
    {
        seen.Clear();
        if (entries == null) return;
        foreach (var e in entries)
            if (e != null) PlayerPrefs.DeleteKey(KeyPrefix + e.name);
        PlayerPrefs.Save();
    }
}
