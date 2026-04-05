// AudioController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public AudioClip accelerationWarning;

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

    [Header("Throttling dźwięków")]
    [Tooltip("Minimalny czas (s) między kolejnymi odtworzeniami tego samego klipu")]
    public float defaultCooldown     = 0.05f;
    [Tooltip("Maks. liczba dźwięków SFX odtworzonych w jednej klatce")]
    public int   maxSoundsPerFrame   = 6;

    [Header("Głośność master")]
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    private AudioSource _sfx;
    private AudioSource _sfxAccel;
    private AudioSource _music;
    private int         _musicIndex = 0;

    // Throttling
    private readonly Dictionary<AudioClip, float> _lastPlayTime = new Dictionary<AudioClip, float>();
    private int   _frameSoundCount;
    private int   _lastThrottleFrame = -1;

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

    // ── Pętla muzyki ─────────────────────────────────────────────────────────
    private IEnumerator MusicLoop()
    {
        AudioClip[] tracks = musicArena2 != null
            ? new[] { musicArena, musicArena2 }
            : new[] { musicArena };

        while (true)
        {
            AudioClip clip = tracks[_musicIndex % tracks.Length];
            _music.clip    = clip;
            _music.volume  = musicVolume;
            _music.Play();
            yield return new WaitUntil(() => !_music.isPlaying);
            _musicIndex++;
        }
    }

    // ── Głośność ─────────────────────────────────────────────────────────────
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

    // ── Publiczne play ────────────────────────────────────────────────────────
    public void PlayBallCollision()  => Play(ballCollision,   normBallCollision);
    public void PlayButtonClick()    => Play(buttonClick,     normButtonClick,  0f);   // UI – bez cooldownu
    public void PlayMeleeHit()       => Play(meleeHit,        normMeleeHit);
    public void PlayProjectileFire() => Play(projectileFire,  normProjectileFire);
    public void PlayProjectileHit()  => Play(projectileHit,   normProjectileHit);
    public void PlayMariachiBullet() => Play(mariachiBullet,   normMariachiBullet);
    public void PlayRogueTeleport()  => Play(rogueTeleport,   normRogueTeleport);
    public void PlayTitanQuake()     => Play(titanQuake,      normTitanQuake,   0.3f); // rzadki dźwięk – dłuższy cooldown
    public void PlayShopBuy()        => Play(shopBuy,         normShopBuy,      0f);
    public void PlayRoundStart()     => Play(roundStart,      normRoundStart,   0f);

    public void PlayAccelerationWarning()
    {
        if (accelerationWarning == null || _sfxAccel == null) return;
        _sfxAccel.PlayOneShot(accelerationWarning, sfxVolume * normAccelWarning);
    }

    public void StopAccelerationWarning() => _sfxAccel?.Stop();

    /// <summary>Odgrywa losowy dźwięk walki — używane przez symulator gdy jest dużo kulek.</summary>
    public void PlayBattleSound()
    {
        // Zbierz dostępne klipy walki
        var clips = new System.Collections.Generic.List<(AudioClip clip, float norm)>();
        if (ballCollision  != null) clips.Add((ballCollision,  normBallCollision));
        if (ballCollision  != null) clips.Add((ballCollision,  normBallCollision)); // częściej
        if (meleeHit       != null) clips.Add((meleeHit,       normMeleeHit));
        if (projectileFire != null) clips.Add((projectileFire, normProjectileFire));
        if (projectileHit  != null) clips.Add((projectileHit,  normProjectileHit));
        if (ballDeath      != null) clips.Add((ballDeath,      normBallDeath));
        if (clips.Count == 0 || _sfx == null) return;
        var (clip, norm) = clips[UnityEngine.Random.Range(0, clips.Count)];
        _sfx.PlayOneShot(clip, sfxVolume * norm * 0.6f);
    }

    // ── Wewnętrzny throttled Play ─────────────────────────────────────────────
    /// <param name="cooldown">Minimalny czas między odtworzeniami. -1 = użyj defaultCooldown.</param>
    void Play(AudioClip clip, float normMultiplier = 1f, float cooldown = -1f)
    {
        if (clip == null || _sfx == null) return;

        float cd = cooldown < 0f ? defaultCooldown : cooldown;

        // Cooldown per klip
        if (cd > 0f)
        {
            float lastTime;
            if (_lastPlayTime.TryGetValue(clip, out lastTime))
            {
                if (Time.unscaledTime - lastTime < cd) return;
            }
        }

        // Budżet per klatka (tylko dla dźwięków z cooldownem – tj. dźwięków walki)
        if (cd > 0f)
        {
            int frame = Time.frameCount;
            if (frame != _lastThrottleFrame) { _frameSoundCount = 0; _lastThrottleFrame = frame; }
            if (_frameSoundCount >= maxSoundsPerFrame) return;
            _frameSoundCount++;
        }

        _lastPlayTime[clip] = Time.unscaledTime;
        _sfx.PlayOneShot(clip, sfxVolume * normMultiplier);
    }
}
