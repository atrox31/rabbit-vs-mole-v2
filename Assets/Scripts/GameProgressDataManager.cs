using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class GameProgressDataManager
{
    // Bits 0-6: Data (7 days of the week)
    private const int DATA_BITS_COUNT = 7;

    public static Dictionary<DayOfWeek, bool> LoadStatus()
    {
        int data = EncryptedPlayerPrefsManager.LoadEncryptedInt(PlayerPrefsConst.RABBIT_STORY_PROGRESS, 0, DATA_BITS_COUNT);

        Dictionary<DayOfWeek, bool> status = new Dictionary<DayOfWeek, bool>();
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            int dayMask = 1 << (int)day;
            bool gcStatus = (data & dayMask) != 0;
            status.Add(day, gcStatus);
        }

        return status;
    }

    public static void SaveStatus(Dictionary<DayOfWeek, bool> status)
    {
        // Calculate Data
        int data = 0;
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>())
        {
            if (status.TryGetValue(day, out bool isPicked) && isPicked)
            {
                int dayMask = 1 << (int)day;
                data |= dayMask;
            }
        }

        EncryptedPlayerPrefsManager.SaveEncryptedInt(PlayerPrefsConst.RABBIT_STORY_PROGRESS, data, DATA_BITS_COUNT);
    }
}
