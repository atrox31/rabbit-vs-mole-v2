using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interface
{
    public static class MainMenuDefaultLogic
    {

        public static void HandleResolutionChange(int index)
        {
            Resolution[] resolutions = Screen.resolutions;
            if (index >= 0 && index < resolutions.Length)
            {
                Resolution res = resolutions[index];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
                Debug.Log($"Resolution changed to: {res.width}x{res.height}");
            }
            PlayerPrefs.SetInt(PlayerPrefsConst.RESOLUTION_WIDTH, Screen.width);
            PlayerPrefs.SetInt(PlayerPrefsConst.RESOLUTION_HEIGHT, Screen.height);
            PlayerPrefs.SetInt(PlayerPrefsConst.RESOLUTION_INDEX, index);
            PlayerPrefs.Save();
        }

        public static List<string> GetAvailableResolutions()
        {
            List<string> resolutions = new List<string>();
            foreach (var res in Screen.resolutions)
            {
                resolutions.Add($"{res.width}x{res.height}");
            }
            return resolutions;
        }

        public static int GetCurrentResolutionIndex()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.RESOLUTION_INDEX))
            {
                int savedIndex = PlayerPrefs.GetInt(PlayerPrefsConst.RESOLUTION_INDEX);
                if (savedIndex >= 0 && savedIndex < Screen.resolutions.Length)
                {
                    Resolution savedRes = Screen.resolutions[savedIndex];
                    if (savedRes.width == Screen.width && savedRes.height == Screen.height)
                    {
                        return savedIndex;
                    }
                }
            }

            Resolution current = Screen.currentResolution;
            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == current.width && resolutions[i].height == current.height)
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
            Debug.Log($"FullScreen changed to: {isFullScreen}");
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
            Debug.Log($"Quality changed to: {QualitySettings.names[qualityIndex]}");
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
            Debug.Log($"Master volume changed to: {volume}");
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
            Debug.Log($"Music volume changed to: {volume}");
        }

        public static void HandleSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat(PlayerPrefsConst.SFX_VOLUME, volume);
            PlayerPrefs.Save();
            AudioManager.SetSFXVolume(volume);
            Debug.Log($"SFX volume changed to: {volume}");
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
            Debug.Log($"Dialogue volume changed to: {volume}");
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
            Debug.Log($"Ambient volume changed to: {volume}");
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
            Debug.Log($"VSync changed to: {value}");
        }

        public static bool GetVSync()
        {
            if (PlayerPrefs.HasKey(PlayerPrefsConst.VSYNC))
            {
                return PlayerPrefs.GetInt(PlayerPrefsConst.VSYNC) == 1;
            }
            return QualitySettings.vSyncCount > 0;
        }
    }
}
