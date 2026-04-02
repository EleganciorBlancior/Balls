// LogoScreen.cs – ekran studia HoodedBadger
// Animacje czysto coroutine, zero zewnętrznych zależności poza TMPro + InputSystem
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LogoScreen : MonoBehaviour
{
    [Header("Elementy UI")]
    public TMP_Text studioLabel;   // "HoodedBadger"
    public Image    logoImage;     // sprite z logo
    public TMP_Text warningText;   // ostrzeżenie o braku zapisu

    [Header("Dźwięki")]
    public AudioClip swooshClip;   // przy wjeździe napisu
    public AudioClip blingClip;    // przy pojawieniu się loga

    [Header("Ustawienia")]
    public string nextScene    = "MainMenu";
    public float  holdDuration = 2.2f;

    // ── Prywatne ─────────────────────────────────────────────────────────────
    private AudioSource _audio;
    private bool        _skip;
    private bool        _done;

    // Pozycje docelowe – zapamiętane przed animacją
    private Vector2 _labelTarget;
    private Vector2 _warningTarget;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _audio            = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
    }

    private void Start()
    {
        // Ustaw disclaimer na podstawie języka (jeśli GameData istnieje)
        if (warningText != null && GameData.Instance != null)
        {
            LocalizationManager.SetLanguage(GameData.Instance.language);
            warningText.text = LocalizationManager.DisclaimerText;
        }

        // Zapamiętaj docelowe pozycje przed ukryciem elementów
        if (studioLabel != null)
        {
            _labelTarget             = studioLabel.rectTransform.anchoredPosition;
            studioLabel.alpha        = 0f;
            studioLabel.maxVisibleCharacters = 0;
            studioLabel.rectTransform.anchoredPosition = _labelTarget + Vector2.up * 140f;
        }
        if (logoImage != null)
        {
            logoImage.color                  = new Color(1f, 1f, 1f, 0f);
            logoImage.rectTransform.localScale = Vector3.zero;
        }
        if (warningText != null)
        {
            _warningTarget           = warningText.rectTransform.anchoredPosition;
            warningText.alpha        = 0f;
            warningText.rectTransform.anchoredPosition = _warningTarget + Vector2.down * 35f;
        }

        StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (_done) return;
        bool anyKey   = Keyboard.current?.anyKey.wasPressedThisFrame     == true;
        bool click    = Mouse.current?.leftButton.wasPressedThisFrame     == true;
        bool touch    = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true;
        if (anyKey || click || touch) _skip = true;
    }

    // ── Główna sekwencja ──────────────────────────────────────────────────────
    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(0.2f);

        // 1. "HoodedBadger" wjeżdża z góry + typewriter
        Play(swooshClip);
        if (studioLabel != null)
            yield return StartCoroutine(AnimateLabel());

        if (_skip) { yield return FadeOutAll(0.12f); Load(); yield break; }

        yield return new WaitForSeconds(0.1f);

        // 2. Logo wyskakuje z efektem pop
        Play(blingClip);
        if (logoImage != null)
            yield return StartCoroutine(AnimateLogo());

        if (_skip) { yield return FadeOutAll(0.12f); Load(); yield break; }

        yield return new WaitForSeconds(0.08f);

        // 3. Ostrzeżenie wjeżdża z dołu
        if (warningText != null)
            yield return StartCoroutine(AnimateWarning());

        // 4. Hold – możliwe pominięcie
        float held = 0f;
        while (held < holdDuration && !_skip)
        { held += Time.deltaTime; yield return null; }

        // 5. Fade out i wyjście
        yield return FadeOutAll(0.45f);
        Load();
    }

    // ── Animacje poszczególnych elementów ─────────────────────────────────────

    // Napis: slide z góry + EaseOutBack + typewriter liter
    IEnumerator AnimateLabel()
    {
        var     rt         = studioLabel.rectTransform;
        Vector2 startPos   = _labelTarget + Vector2.up * 140f;
        int     totalChars = studioLabel.text.Length;
        float   dur        = 0.5f;
        float   t          = 0f;

        while (t < dur && !_skip)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float e = EaseOutBack(p, 1.85f);

            rt.anchoredPosition              = Vector2.Lerp(startPos, _labelTarget, e);
            studioLabel.alpha                = Mathf.Clamp01(p * 4f);
            studioLabel.maxVisibleCharacters = Mathf.RoundToInt(e * totalChars);
            yield return null;
        }

        rt.anchoredPosition              = _labelTarget;
        studioLabel.alpha                = 1f;
        studioLabel.maxVisibleCharacters = totalChars;
    }

    // Logo: scale 0 → 1.25 → 1 + fade + mały obrót
    IEnumerator AnimateLogo()
    {
        var   rt  = logoImage.rectTransform;
        float dur = 0.38f;
        float t   = 0f;

        while (t < dur && !_skip)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float s = Mathf.Max(0f, EaseOutBack(p, 1.4f));

            rt.localScale   = Vector3.one * s;
            logoImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(p * 3f));
            yield return null;
        }

        rt.localScale   = Vector3.one;
        logoImage.color = Color.white;
    }

    // Ostrzeżenie: fade + slide z dołu
    IEnumerator AnimateWarning()
    {
        var rt = warningText.rectTransform;
        Vector2 startPos = _warningTarget + Vector2.down * 35f;
        float dur = 0.45f;
        float t   = 0f;

        while (t < dur && !_skip)
        {
            t += Time.deltaTime;
            float e = EaseOutCubic(Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.Lerp(startPos, _warningTarget, e);
            warningText.alpha   = e;
            yield return null;
        }

        rt.anchoredPosition = _warningTarget;
        warningText.alpha   = 1f;

        // Tekst lekko pulsuje raz żeby przyciągnąć wzrok
        yield return StartCoroutine(PulseAlpha(warningText, 0.4f, 0.3f));
    }

    // ── Helpery animacji ──────────────────────────────────────────────────────

    // Drganie na osi X po wylądowaniu
    IEnumerator Wobble(RectTransform rt, Vector2 basePos, int cycles, float amplitude, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float decay = 1f - t / duration;
            float x     = Mathf.Sin(t / duration * Mathf.PI * 2f * cycles) * amplitude * decay;
            rt.anchoredPosition = basePos + Vector2.right * x;
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    // Lekki obrót "sprężyna" w osi Z
    IEnumerator SpinPunch(RectTransform rt, float degrees, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float angle = Mathf.Sin(t / duration * Mathf.PI) * degrees * (1f - t / duration);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        rt.localRotation = Quaternion.identity;
    }

    // Jednokrotne pulsowanie alpha
    IEnumerator PulseAlpha(TMP_Text tmp, float minAlpha, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tmp.alpha = Mathf.Lerp(1f, minAlpha, Mathf.Sin(t / duration * Mathf.PI));
            yield return null;
        }
        tmp.alpha = 1f;
    }

    // Fade out wszystkich elementów naraz
    IEnumerator FadeOutAll(float duration)
    {
        float startLabel   = studioLabel  != null ? studioLabel.alpha      : 0f;
        float startLogo    = logoImage    != null ? logoImage.color.a       : 0f;
        float startWarning = warningText  != null ? warningText.alpha       : 0f;
        float t            = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = 1f - Mathf.Clamp01(t / duration);
            if (studioLabel != null) studioLabel.alpha  = startLabel   * p;
            if (logoImage   != null) logoImage.color    = new Color(1f, 1f, 1f, startLogo * p);
            if (warningText != null) warningText.alpha  = startWarning * p;
            yield return null;
        }

        if (studioLabel != null) studioLabel.alpha  = 0f;
        if (logoImage   != null) logoImage.color    = new Color(1f, 1f, 1f, 0f);
        if (warningText != null) warningText.alpha  = 0f;
    }

    // ── Easing ───────────────────────────────────────────────────────────────
    static float EaseOutBack(float t, float overshoot = 1.70158f)
    {
        t -= 1f;
        return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
    }

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    void Play(AudioClip clip) { if (clip != null) _audio.PlayOneShot(clip); }
    void Load()               { _done = true; SceneTransition.ExitTo(nextScene); }
}
