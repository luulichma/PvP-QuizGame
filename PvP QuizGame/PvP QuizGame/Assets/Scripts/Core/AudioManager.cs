using UnityEngine;
using System;

/// <summary>
/// Quản lý toàn bộ âm thanh Nhạc nền (BGM) và Hiệu ứng (SFX) của game.
/// Thiết kế theo chuẩn Singleton và tồn tại xuyên suốt giữa các Scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips (Nhạc nền)")]
    public AudioClip bgmHome;
    public AudioClip bgmGame;

    [Header("SFX Clips (Hiệu ứng)")]
    public AudioClip btnClickSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    // Trạng thái Bật/Tắt âm thanh
    public bool IsMusicEnabled { get; private set; } = true;
    public bool IsSFXEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Đảm bảo có AudioSource
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        sfxSource.loop = false;

        LoadSettings();
    }

    private void Start()
    {
        // Chạy thử nhạc Home khi vừa khởi động nếu có
        PlayBGM(bgmHome);
    }

    private void LoadSettings()
    {
        IsMusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        IsSFXEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        bgmSource.mute = !IsMusicEnabled;
        sfxSource.mute = !IsSFXEnabled;
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        IsMusicEnabled = isEnabled;
        bgmSource.mute = !IsMusicEnabled;
        PlayerPrefs.SetInt("MusicEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"[AudioManager] Nhạc nền (Music): {(isEnabled ? "BẬT" : "TẮT")}");
    }

    public void SetSFXEnabled(bool isEnabled)
    {
        IsSFXEnabled = isEnabled;
        sfxSource.mute = !IsSFXEnabled;
        PlayerPrefs.SetInt("SFXEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"[AudioManager] Hiệu ứng (SFX): {(isEnabled ? "BẬT" : "TẮT")}");
    }

    /// <summary>
    /// Phát nhạc nền mới. Nếu đang phát bài này rồi thì bỏ qua.
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>
    /// Phát một hiệu ứng âm thanh.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || !IsSFXEnabled) return;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Tiện ích: Phát tiếng click nút.
    /// </summary>
    public void PlayClickSound()
    {
        PlaySFX(btnClickSound);
    }

    /// <summary>
    /// Tiện ích: Phát nhạc khi thắng/thua.
    /// </summary>
    public void PlayResultSound(bool isWin)
    {
        PlaySFX(isWin ? winSound : loseSound);
    }
}
