using System;
using UnityEngine;

public enum DifficultyMode
{
    Easy = 0,
    Hard = 1,
}

public static class Difficulty
{
    private const string PrefsKey = "difficulty";

    private static DifficultyMode? _cached;

    public static event Action Changed;

    public static DifficultyMode Current
    {
        get
        {
            if (!_cached.HasValue)
            {
                int stored = PlayerPrefs.GetInt(PrefsKey, (int)DifficultyMode.Easy);
                _cached = stored == (int)DifficultyMode.Hard ? DifficultyMode.Hard : DifficultyMode.Easy;
            }

            return _cached.Value;
        }
    }

    public static bool IsEasy => Current == DifficultyMode.Easy;

    public static void Set(DifficultyMode mode)
    {
        bool changed = Current != mode;
        _cached = mode;
        PlayerPrefs.SetInt(PrefsKey, (int)mode);
        PlayerPrefs.Save();

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public static void Toggle()
    {
        Set(IsEasy ? DifficultyMode.Hard : DifficultyMode.Easy);
    }
}
