// SettingsPanel.cs
// Podepnij na prefabie SettingsPanel (Canvas overlay, Sort Order 100).
//
// Prefab layout:
//  SettingsPanel (Canvas, Screen Space Overlay, Sort Order 100)
//  └── Backdrop (Image pełny ekran, #000 alpha 0.7)
//      └── Panel (Image, ~700x600, wycentrowany)
//          ├── Title          (TMP)
//          ├── SFXLabel       (TMP)
//          ├── SFXSlider      (Slider 0–1)
//          ├── MusicLabel     (TMP)
//          ├── MusicSlider    (Slider 0–1)
//          ├── LangLabel      (TMP)
//          ├── LangDropdown   (TMP_Dropdown: "Polski", "English")
//          ├── GfxLabel       (TMP)
//          ├── GfxDropdown    (TMP_Dropdown: "Niska", "Średnia", "Wysoka")
//          ├── ResLabel       (TMP)
//          ├── ResDropdown    (TMP_Dropdown – wypełniany kodem)
//          └── BackButton     (Button)
//              └── BackLabel  (TMP)
//
// Użycie: SettingsPanel.Open(optionalCallback);
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("Etykiety")]
    public TMP_Text titleLabel;
    public RectTransform titleRect;  
    private float titleBobTimer = 0f;  
    public TMP_Text sfxLabel;
    public TMP_Text musicLabel;
    public TMP_Text langLabel;
    public TMP_Text gfxLabel;
    public TMP_Text resLabel;
    public TMP_Text backLabel;

    [Header("Suwaki")]
    public Slider sfxSlider;
    public Slider musicSlider;

    [Header("Dropdowny")]
    public TMP_Dropdown langDropdown;
    public TMP_Dropdown gfxDropdown;
    public TMP_Dropdown resDropdown;

    [Header("Zamknij")]
    public Button backButton;

    private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();

    // ── Statyczne API ─────────────────────────────────────────────────────────
    private static SettingsPanel _instance;
    private static Action        _onClose;

    public static void Open(Action onClose = null)
    {
        if (_instance != null) return;
        _onClose = onClose;
        var prefab = Resources.Load<GameObject>("SettingsPanel");
        if (prefab == null) { Debug.LogError("Brak Resources/SettingsPanel.prefab"); return; }
        var go = Instantiate(prefab);
        _instance = go.GetComponent<SettingsPanel>();
    }

    // ── Rozdzielczości ────────────────────────────────────────────────────────
    private Resolution[] _resolutions;
    private int          _currentResIdx;


    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        _instance = this;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c.rootCanvas == c && c.gameObject != gameObject && c.enabled)
            {
                c.enabled = false;
                _hiddenCanvases.Add(c);
            }
        }
    }

    private void Start()
    {
        // Suwaki
        if (GameData.Instance != null)
        {
            if (sfxSlider   != null) sfxSlider.value   = GameData.Instance.sfxVolume;
            if (musicSlider != null) musicSlider.value  = GameData.Instance.musicVolume;
        }
        if (sfxSlider   != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);

        // Język dropdown
        SetupLangDropdown();

        // Grafika dropdown
        SetupGfxDropdown();

        // Rozdzielczość dropdown
        SetupResDropdown();

        // Back
        if (backButton != null) backButton.onClick.AddListener(Close);

        RefreshLabels();
    }    
    
    private void Update()
    {
        if (titleRect != null)
        {
            titleBobTimer += Time.deltaTime;
            titleRect.anchoredPosition = new Vector2(
                titleRect.anchoredPosition.x,
                (Mathf.Sin(titleBobTimer * 1.5f) * 16f) + 100);
        }
    }

    // ── Suwaki ────────────────────────────────────────────────────────────────
    void OnSFXChanged(float v)
    {
        if (GameData.Instance != null) GameData.Instance.sfxVolume = v;
        AudioController.Instance?.SetSFXVolume(v);
    }

    void OnMusicChanged(float v)
    {
        if (GameData.Instance != null) GameData.Instance.musicVolume = v;
        AudioController.Instance?.SetMusicVolume(v);
    }

    // ── Język ─────────────────────────────────────────────────────────────────
    void SetupLangDropdown()
    {
        if (langDropdown == null) return;
        langDropdown.ClearOptions();
        langDropdown.AddOptions(new List<string> { "Polski", "English" });
        langDropdown.value = LocalizationManager.Language == GameLanguage.PL ? 0 : 1;
        langDropdown.RefreshShownValue();
        langDropdown.onValueChanged.AddListener(OnLangChanged);
    }

    void OnLangChanged(int idx)
    {
        var lang = idx == 0 ? GameLanguage.PL : GameLanguage.EN;
        LocalizationManager.SetLanguage(lang);
        if (GameData.Instance != null) GameData.Instance.language = lang;
        RefreshLabels();
    }

    // ── Grafika ───────────────────────────────────────────────────────────────
    void SetupGfxDropdown()
    {
        if (gfxDropdown == null) return;
        gfxDropdown.ClearOptions();
        gfxDropdown.AddOptions(new List<string>
        {
            LocalizationManager.QualityLow,
            LocalizationManager.QualityMed,
            LocalizationManager.QualityHigh
        });
        int q = GameData.Instance != null ? GameData.Instance.qualityLevel : 1;
        gfxDropdown.value = Mathf.Clamp(q, 0, 2);
        gfxDropdown.RefreshShownValue();
        gfxDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    void OnQualityChanged(int idx)
    {
        if (GameData.Instance != null) GameData.Instance.qualityLevel = idx;
        QualitySettings.SetQualityLevel(idx, true);
    }

    // ── Rozdzielczość ─────────────────────────────────────────────────────────
    void SetupResDropdown()
    {
        if (resDropdown == null) return;

        _resolutions = Screen.resolutions;
        var options  = new List<string>();
        _currentResIdx = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            var r = _resolutions[i];
            options.Add(r.width + " × " + r.height + "  " + Mathf.RoundToInt((float)r.refreshRateRatio.value) + "Hz");
            if (r.width  == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
                _currentResIdx = i;
        }

        resDropdown.ClearOptions();
        resDropdown.AddOptions(options);
        resDropdown.value = _currentResIdx;
        resDropdown.RefreshShownValue();
        resDropdown.onValueChanged.AddListener(OnResChanged);
    }

    void OnResChanged(int idx)
    {
        if (_resolutions == null || idx >= _resolutions.Length) return;
        var r = _resolutions[idx];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode, r.refreshRateRatio);
    }

    // ── Etykiety ──────────────────────────────────────────────────────────────
    void RefreshLabels()
    {
        if (titleLabel != null) titleLabel.text = LocalizationManager.SettingsTitle;
        if (sfxLabel   != null) sfxLabel.text   = LocalizationManager.SFXVolumeLabel;
        if (musicLabel != null) musicLabel.text  = LocalizationManager.MusicVolumeLabel;
        if (langLabel  != null) langLabel.text   = LocalizationManager.LanguageLabel;
        if (gfxLabel   != null) gfxLabel.text    = LocalizationManager.GraphicsLabel;
        if (resLabel   != null) resLabel.text    = LocalizationManager.ResolutionLabel;
        if (backLabel  != null) backLabel.text   = LocalizationManager.Back;

        // Odśwież opcje grafiki (zmienił się język)
        if (gfxDropdown != null)
        {
            int prev = gfxDropdown.value;
            gfxDropdown.ClearOptions();
            gfxDropdown.AddOptions(new List<string>
            {
                LocalizationManager.QualityLow,
                LocalizationManager.QualityMed,
                LocalizationManager.QualityHigh
            });
            gfxDropdown.value = prev;
            gfxDropdown.RefreshShownValue();
        }
    }

    // ── Zamknięcie ────────────────────────────────────────────────────────────
    public void Close()
    {
        foreach (var c in _hiddenCanvases)
            if (c != null) c.enabled = true;
        _hiddenCanvases.Clear();
        _instance = null;
        var cb = _onClose;
        _onClose = null;
        Destroy(gameObject);
        cb?.Invoke();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
