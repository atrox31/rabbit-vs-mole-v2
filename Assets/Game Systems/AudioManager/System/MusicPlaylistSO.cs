using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines a music playlist. 
/// This allows designers to create and manage randomized playlists 
/// via the Unity Inspector and Addressables system.
/// </summary>
[CreateAssetMenu(fileName = "MusicPlaylist", menuName = "Audio/Music Playlist", order = 1)]
public class MusicPlaylistSO : ScriptableObject
{
    [Tooltip("List of Addressable Asset References to the AudioClip files.")]
    public List<AudioClip> musicTracks = new List<AudioClip>();

    [Tooltip("If true, the playlist logic will try to avoid playing the same track twice in a row.")]
    public bool AvoidImmediateRepeat = true;
}