using UnityEngine;

namespace RabbitVsMole
{
    /// <summary>
    /// Manages game audio playback including music for different game states.
    /// Handles volume settings persistence.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private MusicPlaylistSO musicForGameplay;
        [SerializeField] private AudioClip musicForMainMenu;
        [SerializeField] private AudioClip musicForVictory;
        [SerializeField] private AudioClip musicForDefeat;

        public enum MusicType
        {
            Gameplay,
            Victory,
            Defeat,
            MainMenu
        }

        /// <summary>
        /// Plays music based on the specified type.
        /// </summary>
        public void PlayMusic(MusicType type)
        {
            switch (type)
            {
                case MusicType.Gameplay:
                    if (musicForGameplay != null)
                        AudioManager.PlayMusicPlaylist(musicForGameplay);
                    else
                        DebugHelper.LogWarning(this, "GameAudioManager.PlayMusic: musicForGameplay is not assigned");
                    break;

                case MusicType.Victory:
                    if (musicForVictory != null)
                        AudioManager.PlayMusic(musicForVictory);
                    else
                        DebugHelper.LogWarning(this, "GameAudioManager.PlayMusic: musicForVictory is not assigned");
                    break;

                case MusicType.Defeat:
                    if (musicForDefeat != null)
                        AudioManager.PlayMusic(musicForDefeat);
                    else
                        DebugHelper.LogWarning(this, "GameAudioManager.PlayMusic: musicForDefeat is not assigned");
                    break;

                case MusicType.MainMenu:
                    if (musicForMainMenu != null)
                        AudioManager.PlayMusic(musicForMainMenu);
                    else
                        DebugHelper.LogWarning(this, "GameAudioManager.PlayMusic: musicForMainMenu is not assigned");
                    break;

                default:
                    DebugHelper.LogWarning(this, $"GameAudioManager.PlayMusic: Unknown music type '{type}'");
                    break;
            }
        }
    }
}
