using UnityEngine;
using System;

/// <summary>
/// Manages encrypted data storage in PlayerPrefs with checksum validation and versioning.
/// Supports the original 7-bit layout for backward compatibility and flexible layouts for larger data.
/// </summary>
public static class EncryptedPlayerPrefsManager
{
    // --- Bit Configuration ---

    // Original layout (for 7-bit data): 7 data + 10 checksum + 15 version = 32 bits
    private const int ORIGINAL_DATA_BITS_COUNT = 7;
    private const int ORIGINAL_CHECKSUM_SHIFT = 7;
    private const int ORIGINAL_VERSION_SHIFT = 17;
    private const int ORIGINAL_VERSION_BITS_COUNT = 15;
    private const int ORIGINAL_VERSION_MAX_VALUE = (1 << ORIGINAL_VERSION_BITS_COUNT) - 1; // 32767

    // Flexible layout (for larger data): configurable data + 10 checksum + remaining version bits
    private const int MAX_DATA_BITS_COUNT = 16;
    private const int CHECKSUM_BITS_COUNT = 10;

    /// <summary>
    /// Gets a magic salt value from version value for checksum calculation.
    /// </summary>
    private static int GetMagicSaltFromVersionValue(int versionValue)
    {
        return (versionValue * 13 + 7) & 0xFFFF; // Returns a 16-bit salt (0 to 65535)
    }

    /// <summary>
    /// Calculates a checksum for the data using version-dependent salt.
    /// </summary>
    private static int CalculateChecksum(int data, int salt)
    {
        // Simple algorithm: (data + salt) mod (2^10 - 1) to fit in 10 bits.
        int maxChecksumValue = (1 << CHECKSUM_BITS_COUNT) - 1; // 1023
        return (data + salt) % maxChecksumValue;
    }

    /// <summary>
    /// Calculates a checksum with additional maxDataValue salt for bool arrays.
    /// </summary>
    private static int CalculateChecksumWithSize(int data, int salt, int maxDataValue)
    {
        // Use maxDataValue as additional salt component for better encryption
        int combinedSalt = (salt + maxDataValue) & 0xFFFF;
        int maxChecksumValue = (1 << CHECKSUM_BITS_COUNT) - 1; // 1023
        return (data + combinedSalt) % maxChecksumValue;
    }

    /// <summary>
    /// Gets current version value from Application.version.
    /// </summary>
    private static int GetCurrentVersionValue(bool useOriginalLayout = false)
    {
        string version = Application.version;
        string cleanedVersion = version.Replace(".", "");

        if (int.TryParse(cleanedVersion, out int versionInt))
        {
            if (useOriginalLayout)
            {
                // Ensure the version value fits within 15 bits (original layout)
                return versionInt & ORIGINAL_VERSION_MAX_VALUE;
            }
            else
            {
                // For flexible layout, calculate available version bits
                // This will be calculated per call based on dataBitsCount
                return versionInt & 0xFFFF; // Use lower 16 bits, will be masked later
            }
        }
        return 0;
    }

    /// <summary>
    /// Saves encrypted integer data to PlayerPrefs with checksum and version.
    /// Uses original layout for 7-bit data for backward compatibility.
    /// </summary>
    /// <param name="key">PlayerPrefs key</param>
    /// <param name="data">Data to save (must fit in dataBitsCount bits)</param>
    /// <param name="dataBitsCount">Number of bits to use for data (1-16)</param>
    public static void SaveEncryptedInt(string key, int data, int dataBitsCount = 7)
    {
        if (dataBitsCount < 1 || dataBitsCount > MAX_DATA_BITS_COUNT)
        {
            Debug.LogError($"EncryptedPlayerPrefsManager: dataBitsCount must be between 1 and {MAX_DATA_BITS_COUNT}");
            return;
        }

        int dataMask = (1 << dataBitsCount) - 1;
        data = data & dataMask;

        int currentVersionValue = GetCurrentVersionValue(dataBitsCount == ORIGINAL_DATA_BITS_COUNT);
        int currentSalt = GetMagicSaltFromVersionValue(currentVersionValue);
        int checksum = CalculateChecksum(data, currentSalt);

        int finalValue = 0;

        if (dataBitsCount == ORIGINAL_DATA_BITS_COUNT)
        {
            // Use original layout for backward compatibility
            finalValue |= (data & dataMask);
            finalValue |= (checksum << ORIGINAL_CHECKSUM_SHIFT);
            finalValue |= (currentVersionValue << ORIGINAL_VERSION_SHIFT);
        }
        else
        {
            // Flexible layout: data + 10-bit checksum + remaining bits for version
            int checksumShift = dataBitsCount;
            int versionShift = checksumShift + CHECKSUM_BITS_COUNT;
            int availableVersionBits = 32 - versionShift;
            int versionMask = availableVersionBits > 0 ? (1 << availableVersionBits) - 1 : 0;
            int maskedVersion = currentVersionValue & versionMask;

            finalValue |= (data & dataMask);
            finalValue |= (checksum << checksumShift);
            if (availableVersionBits > 0)
            {
                finalValue |= (maskedVersion << versionShift);
            }
        }

        PlayerPrefs.SetInt(key, finalValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads encrypted integer data from PlayerPrefs with validation.
    /// </summary>
    /// <param name="key">PlayerPrefs key</param>
    /// <param name="defaultValue">Default value if data is invalid or missing</param>
    /// <param name="dataBitsCount">Number of bits used for data (must match save)</param>
    /// <returns>Loaded data or defaultValue if invalid</returns>
    public static int LoadEncryptedInt(string key, int defaultValue = 0, int dataBitsCount = 7)
    {
        if (dataBitsCount < 1 || dataBitsCount > MAX_DATA_BITS_COUNT)
        {
            Debug.LogError($"EncryptedPlayerPrefsManager: dataBitsCount must be between 1 and {MAX_DATA_BITS_COUNT}");
            return defaultValue;
        }

        int loadedValue = PlayerPrefs.GetInt(key, 0);
        if (loadedValue == 0)
        {
            return defaultValue;
        }

        int dataMask = (1 << dataBitsCount) - 1;
        int data = loadedValue & dataMask;

        int storedChecksum;
        int storedVersionValue;
        bool useOriginalLayout = (dataBitsCount == ORIGINAL_DATA_BITS_COUNT);

        if (useOriginalLayout)
        {
            // Original layout
            int checksumMask = ((1 << CHECKSUM_BITS_COUNT) - 1) << ORIGINAL_CHECKSUM_SHIFT;
            int versionMask = ORIGINAL_VERSION_MAX_VALUE << ORIGINAL_VERSION_SHIFT;
            storedChecksum = (loadedValue & checksumMask) >> ORIGINAL_CHECKSUM_SHIFT;
            storedVersionValue = (loadedValue & versionMask) >> ORIGINAL_VERSION_SHIFT;
        }
        else
        {
            // Flexible layout
            int checksumShift = dataBitsCount;
            int versionShift = checksumShift + CHECKSUM_BITS_COUNT;
            int availableVersionBits = 32 - versionShift;
            int checksumMask = ((1 << CHECKSUM_BITS_COUNT) - 1) << checksumShift;
            int versionMask = availableVersionBits > 0 ? ((1 << availableVersionBits) - 1) << versionShift : 0;
            storedChecksum = (loadedValue & checksumMask) >> checksumShift;
            storedVersionValue = availableVersionBits > 0 ? (loadedValue & versionMask) >> versionShift : 0;
        }

        int currentVersionValue = GetCurrentVersionValue(useOriginalLayout);

        // Validate
        int requiredSalt = GetMagicSaltFromVersionValue(storedVersionValue);
        int expectedChecksum = CalculateChecksum(data, requiredSalt);

        if (storedChecksum == expectedChecksum)
        {
            // Data is valid
            if (storedVersionValue != currentVersionValue)
            {
                DebugHelper.LogWarning(null, $"EncryptedPlayerPrefsManager: Data for '{key}' accepted from older version ({storedVersionValue}). Ready for re-save.");
                // Optionally auto-save with new version
                SaveEncryptedInt(key, data, dataBitsCount);
            }
            return data;
        }
        else
        {
            DebugHelper.LogWarning(null, $"EncryptedPlayerPrefsManager: Data for '{key}' is corrupt (Stored Version: {storedVersionValue}, Checksum Fail). Returning default.");
            return defaultValue;
        }
    }

    /// <summary>
    /// Saves a bool array to PlayerPrefs with encryption.
    /// The maxSize parameter is used as part of the encryption salt for additional security.
    /// </summary>
    /// <param name="key">PlayerPrefs key</param>
    /// <param name="boolArray">Array of bools to save</param>
    /// <param name="maxSize">Maximum size of the array (used for encryption, must match on load, 1-16)</param>
    public static void SaveBoolArray(string key, bool[] boolArray, int maxSize)
    {
        if (maxSize < 1 || maxSize > MAX_DATA_BITS_COUNT)
        {
            Debug.LogError($"EncryptedPlayerPrefsManager: maxSize must be between 1 and {MAX_DATA_BITS_COUNT}");
            return;
        }

        if (boolArray == null || boolArray.Length == 0)
        {
            DebugHelper.LogWarning(null, $"EncryptedPlayerPrefsManager: boolArray is null or empty for key '{key}'");
            SaveEncryptedInt(key, 0, maxSize);
            return;
        }

        if (boolArray.Length > maxSize)
        {
            DebugHelper.LogWarning(null, $"EncryptedPlayerPrefsManager: boolArray length ({boolArray.Length}) exceeds maxSize ({maxSize}). Truncating.");
        }

        // Pack bools into bits
        int data = 0;
        int bitsToUse = Mathf.Min(boolArray.Length, maxSize);
        for (int i = 0; i < bitsToUse; i++)
        {
            if (boolArray[i])
            {
                data |= (1 << i);
            }
        }

        // Use maxSize in checksum calculation for additional encryption
        int dataMask = (1 << maxSize) - 1;
        data = data & dataMask;

        int currentVersionValue = GetCurrentVersionValue(false);
        int currentSalt = GetMagicSaltFromVersionValue(currentVersionValue);
        int checksum = CalculateChecksumWithSize(data, currentSalt, maxSize);

        int finalValue = 0;

        // Flexible layout for bool arrays (always use flexible layout, not original)
        int checksumShift = maxSize;
        int versionShift = checksumShift + CHECKSUM_BITS_COUNT;
        int availableVersionBits = 32 - versionShift;
        int versionMask = availableVersionBits > 0 ? (1 << availableVersionBits) - 1 : 0;
        int maskedVersion = currentVersionValue & versionMask;

        finalValue |= (data & dataMask);
        finalValue |= (checksum << checksumShift);
        if (availableVersionBits > 0)
        {
            finalValue |= (maskedVersion << versionShift);
        }

        PlayerPrefs.SetInt(key, finalValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads a bool array from PlayerPrefs with decryption.
    /// </summary>
    /// <param name="key">PlayerPrefs key</param>
    /// <param name="maxSize">Maximum size of the array (must match save, 1-16)</param>
    /// <returns>Loaded bool array or array of false values if invalid</returns>
    public static bool[] LoadBoolArray(string key, int maxSize)
    {
        if (maxSize < 1 || maxSize > MAX_DATA_BITS_COUNT)
        {
            Debug.LogError($"EncryptedPlayerPrefsManager: maxSize must be between 1 and {MAX_DATA_BITS_COUNT}");
            return new bool[maxSize];
        }

        int loadedValue = PlayerPrefs.GetInt(key, 0);
        if (loadedValue == 0)
        {
            return new bool[maxSize];
        }

        int dataMask = (1 << maxSize) - 1;
        int data = loadedValue & dataMask;

        // Extract components using flexible layout
        int checksumShift = maxSize;
        int versionShift = checksumShift + CHECKSUM_BITS_COUNT;
        int availableVersionBits = 32 - versionShift;
        int checksumMask = ((1 << CHECKSUM_BITS_COUNT) - 1) << checksumShift;
        int versionMask = availableVersionBits > 0 ? ((1 << availableVersionBits) - 1) << versionShift : 0;
        int storedChecksum = (loadedValue & checksumMask) >> checksumShift;
        int storedVersionValue = availableVersionBits > 0 ? (loadedValue & versionMask) >> versionShift : 0;

        int currentVersionValue = GetCurrentVersionValue(false);

        // Validate with maxSize in checksum
        int requiredSalt = GetMagicSaltFromVersionValue(storedVersionValue);
        int expectedChecksum = CalculateChecksumWithSize(data, requiredSalt, maxSize);

        if (storedChecksum == expectedChecksum)
        {
            // Data is valid
            if (storedVersionValue != currentVersionValue)
            {
                DebugHelper.LogWarning(null, $"EncryptedPlayerPrefsManager: Bool array data for '{key}' accepted from older version ({storedVersionValue}). Ready for re-save.");
            }

            // Unpack bits into bool array
            bool[] result = new bool[maxSize];
            for (int i = 0; i < maxSize; i++)
            {
                result[i] = (data & (1 << i)) != 0;
            }
            return result;
        }
        else
        {
            DebugHelper.LogWarning(null, $"EncryptedPlayerPrefsManager: Bool array data for '{key}' is corrupt (Stored Version: {storedVersionValue}, Checksum Fail). Returning default.");
            return new bool[maxSize];
        }
    }
}

