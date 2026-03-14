// SFXManager.cs — Singleton for one-shot sound effects.
// Attach to the GameManager object alongside MusicManager.
// Clips are wired by SceneBuilder from Assets/Audio/SFX/.
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Tile placement sounds (one picked at random each play)")]
    public AudioClip[] tilePlaceClips;

    [Range(0f, 1f)] public float volume = 0.75f;

    private AudioSource _source;

    private void Awake()
    {
        if (!Application.isPlaying) return;
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        _source             = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop        = false;
        _source.volume      = volume;
    }

    /// <summary>Play a random tile-placement click.</summary>
    public void PlayTilePlace()
    {
        if (_source == null || tilePlaceClips == null || tilePlaceClips.Length == 0) return;
        var clip = tilePlaceClips[Random.Range(0, tilePlaceClips.Length)];
        if (clip != null) _source.PlayOneShot(clip, volume);
    }
}
