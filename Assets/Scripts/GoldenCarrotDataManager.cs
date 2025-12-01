using System;
using System.Collections.Generic;
using System.Linq;

public static class GoldenCarrotDataManager
{
    // Bits 0-6: Data (7 days of the week)
    private const int DATA_BITS_COUNT = 7;

    /// <summary>
    /// Loads the Golden Carrot collection status from PlayerPrefs.
    /// Includes logic to check integrity and migrate data from older versions.
    /// </summary>
    /// <returns>A dictionary containing the collection status for each DayOfWeek.</returns>
    public static Dictionary<DayOfWeek, bool> LoadStatus()
    {
        int data = EncryptedPlayerPrefsManager.LoadEncryptedInt(PlayerPrefsConst.GOLDEN_CARROT_DATA, 0, DATA_BITS_COUNT);

        Dictionary<DayOfWeek, bool> status = new Dictionary<DayOfWeek, bool>();
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            int dayMask = 1 << (int)day;
            bool gcStatus = (data & dayMask) != 0;
            status.Add(day, gcStatus);
        }

        return status;
    }

    /// <summary>
    /// Saves the Golden Carrot collection status to PlayerPrefs, including a versioned checksum.
    /// </summary>
    /// <param name="status">The dictionary containing the collection status for each DayOfWeek.</param>
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

        EncryptedPlayerPrefsManager.SaveEncryptedInt(PlayerPrefsConst.GOLDEN_CARROT_DATA, data, DATA_BITS_COUNT);
    }
}
