// MusicManager.cs — Shuffled looping background music with crossfade.
// Attach to the GameManager object (which is DontDestroyOnLoad) so music
// persists across state transitions.
// Tracks are wired by SceneBuilder from Assets/Audio/Music/.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks (wired by SceneBuilder)")]
    public AudioClip[] tracks;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume          = 0.45f;
    public float crossfadeDuration = 2.5f;   // seconds to blend between tracks
    public float trackStartDelay   = 1.0f;   // pause before first track starts

    // ── Two AudioSources for crossfading ──────────────────────────────────────
    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private AudioSource _active;   // whichever source is currently the "foreground"

    // ── Playlist ──────────────────────────────────────────────────────────────
    private readonly List<int> _playlist = new List<int>();
    private int _playlistIndex;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Don't run during editor scene building — only initialise at runtime.
        if (!Application.isPlaying) return;

        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceB = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { _sourceA, _sourceB })
        {
            src.loop        = false;
            src.playOnAwake = false;
            src.volume      = 0f;
        }

        _active = _sourceA;
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        if (tracks == null || tracks.Length == 0) return;
        BuildPlaylist();
        StartCoroutine(PlayRoutine());
    }

    // ── Playlist helpers ──────────────────────────────────────────────────────
    private void BuildPlaylist()
    {
        _playlist.Clear();
        for (int i = 0; i < tracks.Length; i++) _playlist.Add(i);

        // Fisher-Yates shuffle
        for (int i = _playlist.Count - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            int tmp = _playlist[i];
            _playlist[i] = _playlist[j];
            _playlist[j] = tmp;
        }
        _playlistIndex = 0;
    }

    // ── Main music loop ───────────────────────────────────────────────────────
    private IEnumerator PlayRoutine()
    {
        yield return new WaitForSeconds(trackStartDelay);

        while (true)
        {
            if (_playlistIndex >= _playlist.Count)
                BuildPlaylist();

            var clip = tracks[_playlist[_playlistIndex++]];
            if (clip == null) continue;

            // Start this track (crossfade in)
            yield return StartCoroutine(CrossfadeTo(clip));

            // Wait until it's time to start the crossfade INTO the next track.
            // The crossfade itself takes crossfadeDuration, so we begin it that
            // many seconds before the current clip ends.
            float holdTime = clip.length - crossfadeDuration;
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);
            // If clip is shorter than crossfade time we just fall straight through.
        }
    }

    // ── Crossfade ─────────────────────────────────────────────────────────────
    private IEnumerator CrossfadeTo(AudioClip clip)
    {
        var next = _active == _sourceA ? _sourceB : _sourceA;

        next.clip   = clip;
        next.volume = 0f;
        next.Play();

        float elapsed  = 0f;
        float startVol = _active.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);

            next.volume    = volume * t;
            _active.volume = startVol * (1f - t);

            yield return null;
        }

        next.volume    = volume;
        _active.volume = 0f;
        _active.Stop();
        _active = next;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>Adjust playback volume at runtime (0–1).</summary>
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_active != null) _active.volume = volume;
    }

    /// <summary>Immediately crossfade to a new random track.</summary>
    public void Skip()
    {
        StopAllCoroutines();
        if (tracks == null || tracks.Length == 0) return;
        BuildPlaylist();
        StartCoroutine(PlayRoutine());
    }
}
