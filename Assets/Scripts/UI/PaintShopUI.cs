// PaintShopUI.cs – scena PaintScene
//
// UKŁAD SCENY (ustawić w Unity Editor):
//
//  Canvas
//  ├── LeftPanel
//  │   ├── BallListContent (ScrollRect + VerticalLayoutGroup)
//  │   └── BackButton
//  └── RightPanel
//      ├── EmptyHint (TMP "← Wybierz kulkę")
//      └── EditorView (domyślnie inactive)
//          ├── PreviewImage (Image)
//          ├── BallNameLabel (TMP)
//          ├── PatternDropdown (TMP_Dropdown)
//          │
//          ├── ImageColor (zwykły GameObject / Image)
//          │   ├── BtnPanel (HorizontalLayoutGroup)
//          │   │   ├── C1TabButton  (Button + Image, label "Kolor 1")
//          │   │   ├── C2TabButton  (Button + Image, label "Kolor 2")
//          │   │   └── C3TabButton  (Button + Image, label "Kolor 3" / "Paski")
//          │   └── ColorPicker (RawImage + komponent ColorWheelPicker)
//          │       ├── WheelCursor       (Image, child)
//          │       ├── BrightnessSlider  (Slider 0–1)
//          │       └── ColorPreview      (Image)
//          │
//          ├── StripeRow  (Slider prążków – active gdy wzór stripe/dots I tab3 aktywny)
//          │   ├── StripeLabel (TMP)
//          │   └── StripeSlider (Slider 2–12)
//          │
//          ├── ResetButton
//          └── SaveButton

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PaintShopUI : MonoBehaviour
{
    // ── Lista kulek ──────────────────────────────────────────────────────
    [Header("Lista kulek")]
    public Transform  ballListContent;
    public GameObject listRowPrefab;

    // ── Podgląd ──────────────────────────────────────────────────────────
    [Header("Edytor")]
    public GameObject emptyHint;
    public GameObject editorView;
    public Image      previewImage;
    public TMP_Text   ballNameLabel;

    // ── Wzór ─────────────────────────────────────────────────────────────
    [Header("Wzór")]
    public TMP_Dropdown patternDropdown;

    // ── Color picker ──────────────────────────────────────────────────────
    [Header("Color Picker")]
    public ColorWheelPicker colorPicker;

    // ── Zakładki (przyciski używają własnego Image) ───────────────────────
    [Header("Zakładki kolorów")]
    public Button   c1TabButton;
    public Button   c2TabButton;
    public Button   c3TabButton;
    public Image    tabPreview;

    [Header("Brightness Slider")]
    public Slider   brightnessSlider;

    // ── Paski (pokazywane gdy tab3 aktywny + wzór stripe/dots) ───────────
    [Header("Paski")]
    public GameObject stripeRow;
    public Slider     stripeSlider;
    public TMP_Text   stripeLabel;

    // ── Przyciski ─────────────────────────────────────────────────────────
    [Header("Przyciski")]
    public Button saveButton;
    public Button resetButton;

    // ── Przyciski nawigacyjne (opcjonalne) ────────────────────────────────
    [Header("Przyciski nawigacyjne (opcjonalne)")]
    public TMP_Text backButtonLabel;

    // ── Klasy ─────────────────────────────────────────────────────────────
    [Header("Klasy (16 assetów)")]
    public List<ClassConfig> allClassConfigs;

    // ── Kolory tła zakładek ───────────────────────────────────────────────
    static readonly Color TAB_ACTIVE   = new Color(0.153f, 0.153f, 0.153f, 90f / 255f);
    static readonly Color TAB_INACTIVE = new Color(0.396f, 0.396f, 0.396f, 90f / 255f);

    // ── Stan ──────────────────────────────────────────────────────────────
    private ClassConfig  _selCfg;
    private Color        _c1, _c2, _c3;
    private BallPattern  _pattern;
    private int          _stripes;
    private int          _activeColor = 0;

    private Image    _c1Img,  _c2Img,  _c3Img;
    private TMP_Text _c1Lbl,  _c2Lbl,  _c3Lbl;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Start()
    {
        if (GameData.Instance == null)
            new GameObject("GameData").AddComponent<GameData>();

        // Lokalizacja przycisków Save/Reset
        if (saveButton  != null) { var lbl = saveButton.GetComponentInChildren<TMP_Text>();  if (lbl) lbl.text = LocalizationManager.PaintSave; }
        if (resetButton != null) { var lbl = resetButton.GetComponentInChildren<TMP_Text>(); if (lbl) lbl.text = LocalizationManager.PaintReset; }
        if (backButtonLabel != null) backButtonLabel.text = LocalizationManager.Back;

        CacheButtonComponents();

        // Lokalizacja zakładek kolorów
        if (_c1Lbl != null) _c1Lbl.text = LocalizationManager.PaintTab1;
        if (_c2Lbl != null) _c2Lbl.text = LocalizationManager.PaintTab2;
        // c3 label zależy od wzoru – ustawiamy w RefreshTabsAndWheel

        if (colorPicker != null && brightnessSlider != null)
            colorPicker.HookBrightnessSlider(brightnessSlider);

        BuildBallList();
        ShowEmpty();
        SetupPatternDropdown();
        HookColorPicker();
        HookColorTabs();
        HookStripeSlider();

        // Przywróć poprzednią selekcję
        if (GameData.Instance.paintHasSelection)
        {
            var cfg = GetCfg(GameData.Instance.paintSelClass);
            if (cfg != null) SelectBall(cfg);
        }
    }

    void CacheButtonComponents()
    {
        if (c1TabButton != null)
        {
            _c1Img = c1TabButton.GetComponent<Image>();
            _c1Lbl = c1TabButton.GetComponentInChildren<TMP_Text>();
        }
        if (c2TabButton != null)
        {
            _c2Img = c2TabButton.GetComponent<Image>();
            _c2Lbl = c2TabButton.GetComponentInChildren<TMP_Text>();
        }
        if (c3TabButton != null)
        {
            _c3Img = c3TabButton.GetComponent<Image>();
            _c3Lbl = c3TabButton.GetComponentInChildren<TMP_Text>();
        }
    }

    // ── Lista kulek ───────────────────────────────────────────────────────
    void BuildBallList()
    {
        if (ballListContent == null || listRowPrefab == null) return;
        foreach (Transform c in ballListContent) Destroy(c.gameObject);

        var gd = GameData.Instance;
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null || !IsAvailable(cfg.ballClass)) continue;
            var row = SpawnRow();
            if (row == null) continue;
            row.Setup(cfg.color, LocalizationManager.GetClassName(cfg.ballClass), "");
            var cap = cfg;
            if (row.btn != null) row.btn.onClick.AddListener(() => SelectBall(cap));
        }
        var mergedGroups = new Dictionary<BallClass, int>();
        foreach (var merged in gd.mergedBalls)
            mergedGroups[merged.ballClass] = mergedGroups.ContainsKey(merged.ballClass)
                ? mergedGroups[merged.ballClass] + 1 : 1;
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null || !mergedGroups.ContainsKey(cfg.ballClass)) continue;
            var row = SpawnRow();
            if (row == null) continue;
            row.Setup(new Color(1f, 0.85f, 0.1f),
                      LocalizationManager.SuperPrefix + LocalizationManager.GetClassName(cfg.ballClass),
                      "");
            var cap = cfg;
            if (row.btn != null) row.btn.onClick.AddListener(() => SelectBall(cap));
        }
    }

    bool IsAvailable(BallClass cls)
    {
        var gd = GameData.Instance;
        if (GameData.IsBaseClass(cls) && !gd.consumedBaseBalls.Contains(cls)) return true;
        foreach (var b in gd.purchasedBalls) if (b == cls) return true;
        return false;
    }

    ShopListRow SpawnRow()
    {
        var go  = Instantiate(listRowPrefab, ballListContent);
        var row = go.GetComponent<ShopListRow>();
        if (row != null) row.Init(editorView);
        return row;
    }

    // ── Selekcja kulki ────────────────────────────────────────────────────
    void SelectBall(ClassConfig cfg)
    {
        _selCfg = cfg;
        GameData.Instance.paintHasSelection = true;
        GameData.Instance.paintSelClass     = cfg.ballClass;

        var custom = GameData.Instance.GetCustomization(cfg.ballClass);
        if (custom != null)
        {
            _c1 = custom.color1; _c2 = custom.color2; _c3 = custom.color3;
            _pattern = custom.pattern; _stripes = custom.stripeCount;
        }
        else
        {
            _c1 = cfg.color; _c2 = cfg.color2; _c3 = cfg.color3;
            _pattern = cfg.pattern; _stripes = cfg.stripeCount;
        }

        if (emptyHint  != null) emptyHint.SetActive(false);
        if (editorView != null) editorView.SetActive(true);
        if (ballNameLabel != null) ballNameLabel.text = LocalizationManager.GetClassName(cfg.ballClass);

        _activeColor = 0;
        RefreshDropdown();
        RefreshTabsAndWheel();
        RefreshPreview();
    }

    // ── Color picker ──────────────────────────────────────────────────────
    void HookColorPicker()
    {
        if (colorPicker == null) return;
        colorPicker.OnColorChanged += col =>
        {
            switch (_activeColor)
            {
                case 0: _c1 = col; break;
                case 1: _c2 = col; break;
                case 2: _c3 = col; break;
            }
            RefreshTabColors();
            RefreshPreview();
        };
    }

    void HookColorTabs()
    {
        if (c1TabButton != null) c1TabButton.onClick.AddListener(() => SwitchTab(0));
        if (c2TabButton != null) c2TabButton.onClick.AddListener(() => SwitchTab(1));
        if (c3TabButton != null) c3TabButton.onClick.AddListener(() => SwitchTab(2));
    }

    void HookStripeSlider()
    {
        if (stripeSlider == null) return;
        stripeSlider.minValue    = 2;
        stripeSlider.maxValue    = 12;
        stripeSlider.wholeNumbers = true;
        stripeSlider.onValueChanged.AddListener(v =>
        {
            _stripes = (int)v;
            if (stripeLabel != null) stripeLabel.text = StripeTabLabel() + ": " + _stripes;
            RefreshPreview();
        });
    }

    void SwitchTab(int idx)
    {
        _activeColor = idx;
        RefreshTabsAndWheel();
    }

    void RefreshTabsAndWheel()
    {
        bool useStripes = ShowsStripes();
        bool useColor3  = ShowsColor3();
        bool useColor2  = ShowsColor2();

        if (c1TabButton != null) c1TabButton.gameObject.SetActive(true);
        if (c2TabButton != null) c2TabButton.gameObject.SetActive(useColor2);
        if (c3TabButton != null) c3TabButton.gameObject.SetActive(useColor2 && (useStripes || useColor3));

        // Label zakładki 1 i 2
        if (_c1Lbl != null) _c1Lbl.text = LocalizationManager.PaintTab1;
        if (_c2Lbl != null) _c2Lbl.text = LocalizationManager.PaintTab2;
        // Label zakładki 3 – Paski / Kropki / Kolor 3
        if (_c3Lbl != null) _c3Lbl.text = useStripes ? StripeTabLabel() : LocalizationManager.PaintTab3Color;

        if (_activeColor == 2 && !(useStripes || useColor3)) _activeColor = 0;
        if (_activeColor == 1 && !useColor2)                 _activeColor = 0;

        bool tabIsStripe = (_activeColor == 2 && useStripes);
        if (colorPicker != null) colorPicker.gameObject.SetActive(!tabIsStripe);
        if (stripeRow   != null) stripeRow.SetActive(tabIsStripe);

        if (tabIsStripe)
        {
            if (stripeSlider != null) stripeSlider.SetValueWithoutNotify(_stripes);
            if (stripeLabel  != null) stripeLabel.text = StripeTabLabel() + ": " + _stripes;
        }
        else
        {
            Color toLoad = _activeColor == 0 ? _c1 : _activeColor == 1 ? _c2 : _c3;
            if (colorPicker != null) colorPicker.SetColor(toLoad, false);
        }

        RefreshTabColors();
    }

    void RefreshTabColors()
    {
        SetTabImg(_c1Img, 0);
        SetTabImg(_c2Img, 1);
        SetTabImg(_c3Img, 2);
        if (tabPreview != null)
            tabPreview.color = _activeColor == 0 ? _c1 : _activeColor == 1 ? _c2 : _c3;
    }

    void SetTabImg(Image img, int tabIdx)
    {
        if (img == null) return;
        img.color = (_activeColor == tabIdx) ? TAB_ACTIVE : TAB_INACTIVE;
    }

    // ── Dropdown wzorów ───────────────────────────────────────────────────
    void SetupPatternDropdown()
    {
        if (patternDropdown == null) return;
        patternDropdown.ClearOptions();
        var options = new List<string>();
        for (int i = 0; i < System.Enum.GetValues(typeof(BallPattern)).Length; i++)
            options.Add(LocalizationManager.GetPatternName((BallPattern)i));
        patternDropdown.AddOptions(options);
        patternDropdown.onValueChanged.AddListener(OnPatternChanged);
    }

    void RefreshDropdown()
    {
        if (patternDropdown == null) return;
        patternDropdown.SetValueWithoutNotify((int)_pattern);
    }

    void OnPatternChanged(int idx)
    {
        _pattern = (BallPattern)idx;
        RefreshTabsAndWheel();
        RefreshPreview();
    }

    string StripeTabLabel()
        => _pattern == BallPattern.Dots ? LocalizationManager.PaintDots : LocalizationManager.PaintStripes;

    bool ShowsColor2()  => _pattern != BallPattern.Solid;
    bool ShowsColor3()  => _pattern == BallPattern.Wedge || _pattern == BallPattern.Ring;
    bool ShowsStripes() => _pattern == BallPattern.HorizontalStripes
                        || _pattern == BallPattern.DiagonalStripes
                        || _pattern == BallPattern.Dots;

    // ── Podgląd ───────────────────────────────────────────────────────────
    void RefreshPreview()
    {
        if (previewImage == null || _selCfg == null) return;
        if (_pattern == BallPattern.Solid)
        {
            previewImage.sprite = BallArenaUtils.CircleSprite;
            previewImage.color  = _c1;
        }
        else
        {
            previewImage.sprite = BallArenaUtils.CreatePatternSprite(_c1, _c2, _c3, _pattern, _stripes);
            previewImage.color  = Color.white;
        }
    }

    void ShowEmpty()
    {
        if (emptyHint  != null) emptyHint.SetActive(true);
        if (editorView != null) editorView.SetActive(false);
    }

    // ── Zapisz / Resetuj ──────────────────────────────────────────────────
    public void OnSaveClicked()
    {
        if (_selCfg == null) return;
        GameData.Instance.SaveCustomization(_selCfg.ballClass, _c1, _c2, _c3, _pattern, _stripes);
        GameData.Instance.Save();
        StartCoroutine(FlashSaveBtn());
    }

    public void OnResetClicked()
    {
        if (_selCfg == null) return;
        GameData.Instance.ballCustomizations.RemoveAll(c => c.ballClass == _selCfg.ballClass);
        SelectBall(_selCfg);
    }

    System.Collections.IEnumerator FlashSaveBtn()
    {
        if (saveButton == null) yield break;
        var img = saveButton.GetComponent<Image>();
        if (img == null) yield break;
        Color orig = img.color;
        img.color = new Color(0.2f, 0.8f, 0.2f);
        yield return new WaitForSeconds(0.4f);
        img.color = orig;
    }

    // ── Nawigacja ─────────────────────────────────────────────────────────
    public void GoToShop()     => SceneTransition.ExitTo("ShopScene");
    public void GoToMainMenu() => SceneTransition.ExitTo("MainMenu");

    // ── Helpers ───────────────────────────────────────────────────────────
    ClassConfig GetCfg(BallClass cls)
    {
        var cfg = allClassConfigs.Find(c => c.ballClass == cls);
        if (cfg == null) cfg = Resources.Load<ClassConfig>("ClassConfigs/" + cls);
        return cfg;
    }
}
