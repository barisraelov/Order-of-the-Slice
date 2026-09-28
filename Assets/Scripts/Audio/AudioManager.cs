using UnityEngine;

/// <summary>
/// The only DontDestroyOnLoad object. Duplicate scene instances destroy themselves.
/// Mute is session-only and never writes PlayerPrefs.
/// </summary>
[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip musicMenu;
    [SerializeField] private AudioClip musicGameplay;
    [SerializeField] private AudioClip[] sliceClips;
    [SerializeField] private AudioClip bombClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip cancelClip;

    private bool muted;
    private int sliceIndex;

    public AudioSource MusicSource => musicSource;
    public AudioSource SfxSource => sfxSource;
    public bool Muted => muted;

    public event System.Action<bool> OnMuteChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.AdoptMissingClipsFrom(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        ApplyMuteVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
        musicSource.loop = true;
        sfxSource.loop = false;
        musicSource.spatialBlend = 0f;
        sfxSource.spatialBlend = 0f;
        // One-shots must survive Pause and the same-frame Game Over transition.
        sfxSource.ignoreListenerPause = true;
        sfxSource.priority = 64;
    }

    private void AdoptMissingClipsFrom(AudioManager other)
    {
        if (other == null)
        {
            return;
        }

        if (musicMenu == null)
        {
            musicMenu = other.musicMenu;
        }

        if (musicGameplay == null)
        {
            musicGameplay = other.musicGameplay;
        }

        if (bombClip == null)
        {
            bombClip = other.bombClip;
        }

        if (clickClip == null)
        {
            clickClip = other.clickClip;
        }

        if (confirmClip == null)
        {
            confirmClip = other.confirmClip;
        }

        if (cancelClip == null)
        {
            cancelClip = other.cancelClip;
        }

        if ((sliceClips == null || sliceClips.Length == 0) && other.sliceClips != null)
        {
            sliceClips = other.sliceClips;
        }
    }

    public void SetMuted(bool value)
    {
        if (muted == value)
        {
            return;
        }

        muted = value;
        ApplyMuteVolumes();
        OnMuteChanged?.Invoke(muted);
    }

    public void ToggleMute()
    {
        SetMuted(!muted);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (muted || clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
        ApplyMuteVolumes();
    }

    public void PlayMenuMusic()
    {
        PlayMusic(musicMenu);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(musicGameplay);
    }

    public void PlaySlice()
    {
        if (sliceClips == null || sliceClips.Length == 0)
        {
            return;
        }

        var clip = sliceClips[sliceIndex];
        sliceIndex = (sliceIndex + 1) % sliceClips.Length;
        PlaySfx(clip);
    }

    public void PlayBomb()
    {
        // Same one-shot path as other SFX. bombClip is the trimmed explosion excerpt.
        PlaySfx(bombClip);
    }

    public void PlayClick()
    {
        PlaySfx(clickClip);
    }

    public void PlayConfirm()
    {
        PlaySfx(confirmClip);
    }

    public void PlayCancel()
    {
        PlaySfx(cancelClip);
    }

    private void ApplyMuteVolumes()
    {
        var volume = muted ? 0f : 1f;
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
    }
}
