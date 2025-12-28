using RabbitVsMole;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Interface
{
    public static class MainMenuDefaultLogic
    {
        // Helper class to store resolution with available refresh rates
        private class ResolutionData
        {
            public int width;
            public int height;
            public List<int> refreshRates = new List<int>();
            public Resolution representativeResolution; // One of the resolutions with this width/height
            
            public ResolutionData(Resolution res)
            {
                width = res.width;
                height = res.height;
                representativeResolution = res;
                refreshRates.Add((int)res.refreshRateRatio.value);
            }
        }
        
        // Static list to store filtered resolutions
        private static List<ResolutionData> _filteredResolutions = null;
        private static int _maxRefreshRate = 60; // Default fallback
        
        // Initialize filtered resolutions list
        private static void InitializeFilteredResolutions()
        {
            if (_filteredResolutions != null) return;
            
            _filteredResolutions = new List<ResolutionData>();
            Resolution[] allResolutions = Screen.resolutions;
            
            foreach (var res in allResolutions)
            {
                // Find existing resolution with same width and height
                var existing = _filteredResolutions.FirstOrDefault(r => r.width == res.width && r.height == res.height);
                
                if (existing != null)
                {
                    // Add refresh rate if not already present
                    int refreshRate = (int)res.refreshRateRatio.value;
                    if (!existing.refreshRates.Contains(refreshRate))
                    {
                        existing.refreshRates.Add(refreshRate);
                    }
                }
                else
                {
                    // Create new resolution entry
                    _filteredResolutions.Add(new ResolutionData(res));
                }
            }
            
            // Sort refresh rates for each resolution and find max refresh rate
            _maxRefreshRate = 60;
            foreach (var resData in _filteredResolutions)
            {
                resData.refreshRates.Sort();
                if (resData.refreshRates.Count > 0)
                {
                    int maxRate = resData.refreshRates[resData.refreshRates.Count - 1];
                    if (maxRate > _maxRefreshRate)
                    {
                        _maxRefreshRate = maxRate;
                    }
                }
            }
        }
        
        public static int GetMaxRefreshRate()
        {
            InitializeFilteredResolutions();
            return _maxRefreshRate;
        }

        public static void HandleResolutionChange(int index)
        {
            InitializeFilteredResolutions();
            
            if (index >= 0 && index < _filteredResolutions.Count)
            {
                ResolutionData resData = _filteredResolutions[index];
                
                // Get the refresh rate from PlayerPrefs or use the highest available
                int refreshRate = (int)resData.representativeResolution.refreshRateRatio.value;
                if (PlayerPrefs.HasKey(PlayerPrefsConst.REFRESH_RATE))
                {
                    int savedRefreshRate = PlayerPrefs.GetInt(PlayerPrefsConst.REFRESH_RATE);
                    // Use saved refresh rate if it's available for this resolution
                    if (resData.refreshRates.Contains(savedRefreshRate))
                    {
                        refreshRate = savedRefreshRate;
                    }
                    else
                    {
                        // Use the highest available refresh rate
                        refreshRate = resData.refreshRates[resData.refreshRates.Count - 1];
                    }
                }
                
                FullScreenMode fullScreenMode = Screen.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                RefreshRate refreshRateObj = new RefreshRate { numerator = (uint)refreshRate, denominator = 1 };
                Screen.SetResolution(resData.width, resData.height, fullScreenMode, refreshRateObj);
                PlayerPrefs.SetInt(PlayerPrefsConst.REFRESH_RATE, refreshRate);
                DebugHelper.Log(null, $"Resolution changed to: {resData.width}x{resData.height} @ {refreshRate}Hz");
            }
            
            PlayerPrefs.SetInt(PlayerPrefsConst.RESOLUTION_WIDTH, Screen.width);
            PlayerPrefs.SetInt(PlayerPrefsConst.RESOLUTION_HEIGHT, Screen.height);
            PlayerPrefs.SetInt(PlayerPrefsConst.RESOLUTION_INDEX, index);
            PlayerPrefs.Save();
        }

        public static List<string> GetAvailableResolutions()
        {
            InitializeFilteredResolutions();
            
            List<string> resolutions = new List<string>();
            foreach (var resData in _filteredResolutions)
            {
                resolutions.Add($"{resData.width}x{resData.height}");
            }
            return resolutions;
        }

        public static int GetCurrentResolutionIndex()
        {
            InitializeFilteredResolutions();
            
            if (PlayerPrefs.HasKey(PlayerPrefsConst.RESOLUTION_INDEX))
            {
                int savedIndex = PlayerPrefs.GetInt(PlayerPrefsConst.RESOLUTION_INDEX);
                if (savedIndex >= 0 && savedIndex < _filteredResolutions.Count)
                {
                    ResolutionData savedResData = _filteredResolutions[savedIndex];
                    if (savedResData.width == Screen.width && savedResData.height == Screen.height)
                    {
                        return savedIndex;
                    }
                }
            }

            Resolution current = Screen.currentResolution;
            for (int i = 0; i < _filteredResolutions.Count; i++)
            {
                if (_filteredResolutions[i].width == current.width && _filteredResolutions[i].height == current.height)
                {
                    return i;
                }
            }
            return 0;
        }

        public static void HandleFullScreen(bool isFullScreen)
        {
            Screen.fullScreen = isFullScreen;
            PlayerPrefs.SetInt(PlayerPrefsConst.FULLSCREEN, isFullScreen ? 1 : 0);
            PlayerPrefs.Save();
            DebugHelper.Log(null, $"FullScreen changed to: {isFullScreen}");
        }

        public static bool GetFullScreenCurrentMode()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.FULLSCREEN))
            {
                return PlayerPrefs.GetInt(PlayerPrefsConst.FULLSCREEN) == 1;
            }
            return Screen.fullScreen;
        }

        public static void HandleQualityChange(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            PlayerPrefs.SetInt(PlayerPrefsConst.GRAPHICS_QUALITY, qualityIndex);
            PlayerPrefs.Save();
            DebugHelper.Log(null, $"Quality changed to: {QualitySettings.names[qualityIndex]}");
        }

        public static List<string> GetAvailableQualitySettings()
        {
            List<string> qualities = new List<string>();
            foreach (var name in QualitySettings.names)
            {
                qualities.Add(name);
            }
            return qualities;
        }

        public static int GetCurrentQualityIndex()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.GRAPHICS_QUALITY))
            {
                int savedQuality = PlayerPrefs.GetInt(PlayerPrefsConst.GRAPHICS_QUALITY);
                if (savedQuality >= 0 && savedQuality < QualitySettings.names.Length)
                {
                    return savedQuality;
                }
            }
            return QualitySettings.GetQualityLevel();
        }

        public static void HandleMasterVolumeChange(float volume)
        {
            PlayerPrefs.SetFloat(PlayerPrefsConst.MASTER_VOLUME, volume);
            PlayerPrefs.Save();
            AudioManager.SetMasterVolume(volume);
            DebugHelper.Log(null, $"Master volume changed to: {volume}");
        }

        public static float GetMasterVolume()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.MASTER_VOLUME))
            {
                return PlayerPrefs.GetFloat(PlayerPrefsConst.MASTER_VOLUME);
            }
            return 1f;
        }

        public static void HandleMusicVolumeChange(float volume)
        {
            PlayerPrefs.SetFloat(PlayerPrefsConst.MUSIC_VOLUME, volume);
            PlayerPrefs.Save();
            AudioManager.SetMusicVolume(volume);
            DebugHelper.Log(null, $"Music volume changed to: {volume}");
        }

        public static void HandleSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat(PlayerPrefsConst.SFX_VOLUME, volume);
            PlayerPrefs.Save();
            AudioManager.SetSFXVolume(volume);
            DebugHelper.Log(null, $"SFX volume changed to: {volume}");
        }

        public static float GetMusicVolume()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.MUSIC_VOLUME))
            {
                return PlayerPrefs.GetFloat(PlayerPrefsConst.MUSIC_VOLUME);
            }
            return 1f;
        }
        public static float GetSFXVolume()
        {
            if(PlayerPrefs.HasKey(PlayerPrefsConst.SFX_VOLUME))
            {
                return PlayerPrefs.GetFloat(PlayerPrefsConst.SFX_VOLUME);
            }
            return 1f;
        }

        public static void HandleDialogueVolumeChange(float volume)
        {
            PlayerPrefs.SetFloat(PlayerPrefsConst.DIALOGUES_VOLUME, volume);
            PlayerPrefs.Save();
            AudioManager.SetDialoguesVolume(volume);
            DebugHelper.Log(null, $"Dialogue volume changed to: {volume}");
        }

        public static float GetDialogueVolume()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.DIALOGUES_VOLUME))
            {
                return PlayerPrefs.GetFloat(PlayerPrefsConst.DIALOGUES_VOLUME);
            }
            return 1f;
        }

        public static void HandleAmbientVolumeChange(float volume)
        {
            PlayerPrefs.SetFloat(PlayerPrefsConst.AMBIENT_VOLUME, volume);
            PlayerPrefs.Save();
            AudioManager.SetAmbientVolume(volume);
            DebugHelper.Log(null, $"Ambient volume changed to: {volume}");
        }

        public static float GetAmbientVolume()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.AMBIENT_VOLUME))
            {
                return PlayerPrefs.GetFloat(PlayerPrefsConst.AMBIENT_VOLUME);
            }
            return 1f;
        }

        public static void HandleVSync(bool value)
        {
            PlayerPrefs.SetInt(PlayerPrefsConst.VSYNC, value ? 1 : 0);
            PlayerPrefs.Save();
            QualitySettings.vSyncCount = value ? 1 : 0;
            DebugHelper.Log(null, $"VSync changed to: {value}");
        }

        public static bool GetVSync()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.VSYNC))
            {
                return PlayerPrefs.GetInt(PlayerPrefsConst.VSYNC) == 1;
            }
            return QualitySettings.vSyncCount > 0;
        }

        public static void HandleTargetFPSChange(float normalizedValue)
        {
            // normalizedValue is 0.0 to 1.0
            // Map: 0.0 = unlimited (0), 0.01-1.0 = 10 to maxRefreshRate
            InitializeFilteredResolutions();
            
            int targetFPS;
            if (normalizedValue <= 0.01f)
            {
                // Unlimited (at the start of slider)
                targetFPS = 0; // 0 means unlimited in Unity
            }
            else
            {
                // Map from 10 to maxRefreshRate
                const int minFPS = 10;
                float range = _maxRefreshRate - minFPS;
                // Map 0.01-1.0 to 10-maxRefreshRate
                float mappedValue = (normalizedValue - 0.01f) / 0.99f; // Scale to 0.0-1.0
                targetFPS = Mathf.RoundToInt(minFPS + mappedValue * range);
                targetFPS = Mathf.Clamp(targetFPS, minFPS, _maxRefreshRate);
            }
            
            Application.targetFrameRate = targetFPS;
            PlayerPrefs.SetInt(PlayerPrefsConst.TARGET_FPS, targetFPS);
            PlayerPrefs.Save();
            
            string fpsDisplay = targetFPS == 0 ? "Unlimited" : targetFPS.ToString();
            DebugHelper.Log(null, $"Target FPS changed to: {fpsDisplay}");
        }

        public static float GetTargetFPS()
        {
            InitializeFilteredResolutions();
            
            int targetFPS = 0; // Default to unlimited
            if (PlayerPrefs.HasKey(PlayerPrefsConst.TARGET_FPS))
            {
                targetFPS = PlayerPrefs.GetInt(PlayerPrefsConst.TARGET_FPS);
            }
            else
            {
                // If not set, default to unlimited (0)
                targetFPS = 0;
            }
            
            // Map back to normalized value (0.0 to 1.0)
            if (targetFPS == 0)
            {
                return 0.0f; // Unlimited = 0.0 (at the start of slider)
            }
            
            const int minFPS = 10;
            float range = _maxRefreshRate - minFPS;
            // Map from 10-maxRefreshRate to 0.01-1.0
            float mappedValue = ((float)(targetFPS - minFPS) / range) * 0.99f; // Scale to 0.0-0.99
            float normalized = 0.01f + mappedValue; // Shift to 0.01-1.0
            return Mathf.Clamp01(normalized);
        }

        public static string FormatTargetFPS(float normalizedValue)
        {
            InitializeFilteredResolutions();
            
            if (normalizedValue <= 0.01f)
            {
                return "Unlimited";
            }
            
            const int minFPS = 10;
            float range = _maxRefreshRate - minFPS;
            // Map 0.01-1.0 to 10-maxRefreshRate
            float mappedValue = (normalizedValue - 0.01f) / 0.99f; // Scale to 0.0-1.0
            int targetFPS = Mathf.RoundToInt(minFPS + mappedValue * range);
            targetFPS = Mathf.Clamp(targetFPS, minFPS, _maxRefreshRate);
            return targetFPS.ToString() + " FPS";
        }

        public static void InitializeTargetFPS()
        {
            // Apply saved FPS setting at game start, default to 0 (unlimited)
            int targetFPS = 0; // Default to unlimited
            if (PlayerPrefs.HasKey(PlayerPrefsConst.TARGET_FPS))
            {
                targetFPS = PlayerPrefs.GetInt(PlayerPrefsConst.TARGET_FPS);
            }
            Application.targetFrameRate = targetFPS;
        }

        public static void HandleLanguageChange(int index)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (index >= 0 && index < locales.Count)
            {
                LocalizationSettings.SelectedLocale = locales[index];
                PlayerPrefs.SetString(PlayerPrefsConst.LANGUAGE, locales[index].Identifier.Code);
                PlayerPrefs.Save();
                DebugHelper.Log(null, $"Language changed to: {locales[index].Identifier}");
            }
        }

        public static List<string> GetAvailableLanguages()
        {
            List<string> languages = new List<string>();
            var locales = LocalizationSettings.AvailableLocales.Locales;
            
            if (locales != null && locales.Count > 0)
            {
                foreach (var locale in locales)
                {
                    if (locale != null)
                    {
                        // Try to get native name, fallback to code
                        string name = locale.Identifier.CultureInfo != null 
                            ? locale.Identifier.CultureInfo.NativeName 
                            : locale.Identifier.Code;
                        languages.Add(name);
                    }
                }
            }
            
            return languages;
        }

        public static int GetCurrentLanguageIndex()
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales == null || locales.Count == 0)
            {
                // Fallback - check PlayerPrefs
                string savedCode = PlayerPrefs.GetString(PlayerPrefsConst.LANGUAGE, "pl");
                return savedCode == "pl" ? 0 : 1;
            }

            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale != null)
            {
                for (int i = 0; i < locales.Count; i++)
                {
                    if (locales[i] != null && locales[i].Identifier == currentLocale.Identifier)
                    {
                        return i;
                    }
                }
            }

            // Fallback to PlayerPrefs
            string code = PlayerPrefs.GetString(PlayerPrefsConst.LANGUAGE, "");
            if (!string.IsNullOrEmpty(code))
            {
                for (int i = 0; i < locales.Count; i++)
                {
                    if (locales[i] != null && locales[i].Identifier.Code == code)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }
    }
}
