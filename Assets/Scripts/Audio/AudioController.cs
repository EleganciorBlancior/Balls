// AudioController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("SFX – Klipy")]
    public AudioClip ballCollision;
    public AudioClip ballDeath;
    public AudioClip buttonClick;
    public AudioClip mariachiBullet;
    public AudioClip meleeHit;
    public AudioClip projectileFire;
    public AudioClip projectileHit;
    public AudioClip rogueTeleport;
    public AudioClip roundEnd;
    public AudioClip roundStart;
    public AudioClip shopBuy;
    public AudioClip titanQuake;
    public AudioClip accelerationWarning;  // wskaźnik przyspieszenia kulek

    [Header("Muzyka")]
    public AudioClip musicArena;
    public AudioClip musicArena2;

    [Header("Normalizacja SFX (mnożniki na klip – ustaw w Inspectorze)")]
    [Range(0f, 2f)] public float normBallCollision   = 0.7f;
    [Range(0f, 2f)] public float normBallDeath        = 1.0f;
    [Range(0f, 2f)] public float normButtonClick      = 0.6f;
    [Range(0f, 2f)] public float normMariachiBullet   = 0.8f;
    [Range(0f, 2f)] public float normMeleeHit         = 0.9f;
    [Range(0f, 2f)] public float normProjectileFire   = 0.7f;
    [Range(0f, 2f)] public float normProjectileHit    = 0.8f;
    [Range(0f, 2f)] public float normRogueTeleport    = 0.7f;
    [Range(0f, 2f)] public float normRoundEnd         = 1.0f;
    [Range(0f, 2f)] public float normRoundStart       = 1.0f;
    [Range(0f, 2f)] public float normShopBuy          = 0.6f;
    [Range(0f, 2f)] public float normTitanQuake       = 1.0f;
    [Range(0f, 2f)] public float normAccelWarning     = 1.0f;

    [Header("Głośność master")]
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    private AudioSource _sfx;
    private AudioSource _sfxAccel;
    private AudioSource _music;
    private int _musicIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfx             = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;

        _sfxAccel             = gameObject.AddComponent<AudioSource>();
        _sfxAccel.playOnAwake = false;

        _music             = gameObject.AddComponent<AudioSource>();
        _music.loop        = false;
        _music.playOnAwake = false;
    }

    private void Start()
    {
        // Załaduj głośności z GameData jeśli dostępne
        if (GameData.Instance != null)
        {
            sfxVolume   = GameData.Instance.sfxVolume;
            musicVolume = GameData.Instance.musicVolume;
        }

        _music.volume = musicVolume;

        ArenaEvents.OnBallDied += (_, __) => Play(ballDeath, normBallDeath);
        ArenaEvents.OnGameEnd  += ()      => Play(roundEnd,  normRoundEnd);

        if (musicArena != null)
            StartCoroutine(MusicLoop());
    }

    // ── Pętla muzyki naprzemiennej ───────────────────────────────────────────
    private IEnumerator MusicLoop()
    {
        AudioClip[] tracks = musicArena2 != null
            ? new[] { musicArena, musicArena2 }
            : new[] { musicArena };

        while (true)
        {
            AudioClip clip = tracks[_musicIndex % tracks.Length];
            _music.clip   = clip;
            _music.volume = musicVolume;
            _music.Play();
            yield return new WaitUntil(() => !_music.isPlaying);
            _musicIndex++;
        }
    }

    // ── Publiczne settery głośności ───────────────────────────────────────────
    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (GameData.Instance != null) GameData.Instance.sfxVolume = sfxVolume;
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (_music != null) _music.volume = musicVolume;
        if (GameData.Instance != null) GameData.Instance.musicVolume = musicVolume;
    }

    // ── SFX publiczne ────────────────────────────────────────────────────────
    public void PlayBallCollision()       => Play(ballCollision,        normBallCollision);
    public void PlayButtonClick()         => Play(buttonClick,          normButtonClick);
    public void PlayMeleeHit()            => Play(meleeHit,             normMeleeHit);
    public void PlayProjectileFire()      => Play(projectileFire,       normProjectileFire);
    public void PlayProjectileHit()       => Play(projectileHit,        normProjectileHit);
    public void PlayMariachiBullet()      => Play(mariachiBullet,       normMariachiBullet);
    public void PlayRogueTeleport()       => Play(rogueTeleport,        normRogueTeleport);
    public void PlayTitanQuake()          => Play(titanQuake,           normTitanQuake);
    public void PlayShopBuy()             => Play(shopBuy,              normShopBuy);
    public void PlayRoundStart()          => Play(roundStart,           normRoundStart);
    public void PlayAccelerationWarning()
    {
        if (accelerationWarning == null || _sfxAccel == null) return;
        _sfxAccel.PlayOneShot(accelerationWarning, sfxVolume * normAccelWarning);
    }

    public void StopAccelerationWarning() => _sfxAccel?.Stop();

    void Play(AudioClip clip, float normMultiplier = 1f)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, sfxVolume * normMultiplier);
    }
}
