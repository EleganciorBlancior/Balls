// AudioController.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("SFX")]
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

    [Header("Muzyka")]
    public AudioClip musicArena;

    [Header("Głośność")]
    [Range(0f, 1f)] public float sfxVolume   = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    private AudioSource _sfx;
    private AudioSource _music;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfx             = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;

        _music              = gameObject.AddComponent<AudioSource>();
        _music.loop         = true;
        _music.playOnAwake  = false;
        _music.volume       = musicVolume;
    }

    private void Start()
    {
        ArenaEvents.OnBallDied += (_, __) => Play(ballDeath);
        ArenaEvents.OnGameEnd  += ()      => Play(roundEnd);
        if (musicArena != null)
        {
            _music.clip   = musicArena;
            _music.volume = musicVolume;
            _music.Play();
        }
    }

    // ── SFX publiczne ────────────────────────────────────────────────────────
    public void PlayBallCollision()   => Play(ballCollision);
    public void PlayButtonClick()     => Play(buttonClick);
    public void PlayMeleeHit()        => Play(meleeHit);
    public void PlayProjectileFire()  => Play(projectileFire);
    public void PlayProjectileHit()   => Play(projectileHit);
    public void PlayMariachiBullet()  => Play(mariachiBullet);
    public void PlayRogueTeleport()   => Play(rogueTeleport);
    public void PlayTitanQuake()      => Play(titanQuake);
    public void PlayShopBuy()         => Play(shopBuy);
    public void PlayRoundStart()      => Play(roundStart);

    void Play(AudioClip clip)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip, sfxVolume);
    }
}
