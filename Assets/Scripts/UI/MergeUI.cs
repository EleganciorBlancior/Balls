// MergeUI.cs (v4) – akordeonowa lista (lewo) + panel szczegółów (prawo)
//
// UKŁAD SCENY (ustawić w Unity Editor):
//
//  Canvas
//  ├── TopBar
//  │   ├── TitleRect (TMP, bobbing)
//  │   ├── GoldText (TMP)
//  │   └── InfoText (TMP)
//  ├── LeftPanel  (1/3 szerokości) ← ScrollRect → Viewport → Content (VLG)
//  │   ├── BasicHeaderBtn   (Button + TMP "Scal bazowe ▼")
//  │   ├── BasicContent     (VerticalLayoutGroup)
//  │   ├── UpgradeHeaderBtn (Button + TMP "Ulepsz scalone ▼")
//  │   ├── UpgradeContent   (VerticalLayoutGroup)
//  │   ├── OwnedHeaderBtn   (Button + TMP "Twoje scalone ▼")
//  │   └── OwnedContent     (VerticalLayoutGroup)
//  └── RightPanel (2/3 szerokości)
//      ├── EmptyHint (TMP "← Wybierz kulkę")
//      └── DetailView (domyślnie inactive)
//          ├── DetailIcon (Image, 80×80)
//          ├── DetailName (TMP Bold)
//          ├── DetailFlavor (TMP Italic)
//          ├── DetailStats (TMP monospace)
//          └── ActionButton (Button)
//              └── ActionLabel (TMP)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MergeUI : MonoBehaviour
{
    // ── Top bar ───────────────────────────────────────────────────────────────
    [Header("Top bar")]
    public TMP_Text      goldText;
    public TMP_Text      infoText;
    public RectTransform titleRect;

    // ── Akordeony ─────────────────────────────────────────────────────────
    [Header("Accordion – Scal bazowe")]
    public Transform basicContent;
    public TMP_Text  basicToggleLabel;

    [Header("Accordion – Ulepsz scalone")]
    public Transform upgradeContent;
    public TMP_Text  upgradeToggleLabel;

    [Header("Accordion – Twoje scalone")]
    public Transform ownedContent;
    public TMP_Text  ownedToggleLabel;

    [Header("Prefab wiersza")]
    public GameObject listRowPrefab;

    // ── Panel szczegółów ──────────────────────────────────────────────────
    [Header("Panel szczegółów")]
    public GameObject emptyHint;
    public GameObject detailView;
    public Image      detailIcon;
    public TMP_Text   detailName;
    public TMP_Text   detailFlavor;
    public TMP_Text   detailStats;
    public Button     actionButton;
    public TMP_Text   actionLabel;

    // ── Przyciski nawigacyjne (opcjonalne) ────────────────────────────────
    [Header("Przyciski nawigacyjne (opcjonalne)")]
    public TMP_Text shopButtonLabel;
    public TMP_Text backButtonLabel;

    [Header("Przycisk sprzedaży (opcjonalne)")]
    public Button   sellButton;
    public TMP_Text sellButtonLabel;

    [Header("Mistrzostwo (opcjonalne)")]
    public TMP_Text masteryText;


    // ── Klasy ─────────────────────────────────────────────────────────────
    [Header("Klasy (16 assetów)")]
    public List<ClassConfig> allClassConfigs;

    // ── Stan ──────────────────────────────────────────────────────────────
    private bool _basicOpen   = false;
    private bool _upgradeOpen = false;
    private bool _ownedOpen   = false;
    private float _bobTimer;

    private enum SelType { None, Basic, Upgrade, OwnedInfo }
    private int         _selOwnedLevel;
    private SelType     _selType      = SelType.None;
    private ClassConfig _selCfg;
    private int         _selFromLevel;
    private bool        _selCanMerge;
    private bool        _selNeedsTier;

    // Hold-to-merge / hold-to-sell
    const float HOLD_INITIAL_DELAY  = 0.5f;
    const float HOLD_INTERVAL_START = 0.15f;
    const float HOLD_INTERVAL_MIN   = 0.002f;
    const float HOLD_RAMP_TIME      = 3f;

    private bool  _actionHeld;
    private float _holdTimer;
    private float _holdHeld;
    private bool  _holdFired;
    private bool  _holdWasUsed; // true = hold był aktywny, zablokuj onClick przy puszczeniu

    private bool  _sellHeld;
    private float _sellHoldTimer;
    private float _sellHoldHeld;
    private bool  _sellHoldFired;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    private void Start()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData"); go.AddComponent<GameData>();
        }

        // Przyciski nawigacyjne
        if (shopButtonLabel != null) shopButtonLabel.text = LocalizationManager.MainMenuShop;
        if (backButtonLabel != null) backButtonLabel.text = LocalizationManager.Back;
        if (sellButtonLabel != null) sellButtonLabel.text = LocalizationManager.Sell;
        if (sellButton      != null)
        {
            sellButton.onClick.AddListener(TrySellMerged);
            sellButton.gameObject.SetActive(false);
        }

        RefreshTopBar();

        // Przywróć stan akordeonów
        _basicOpen   = GameData.Instance.mergeBasicOpen;
        _upgradeOpen = GameData.Instance.mergeUpgradeOpen;
        _ownedOpen   = GameData.Instance.mergeOwnedOpen;

        BuildLists();

        if (basicToggleLabel   != null)
            basicToggleLabel.text   = LocalizationManager.MergeBasic   + " " + (_basicOpen   ? "▲" : "▼");
        if (upgradeToggleLabel != null)
            upgradeToggleLabel.text = LocalizationManager.MergeUpgrade + " " + (_upgradeOpen ? "▲" : "▼");
        if (ownedToggleLabel   != null)
            ownedToggleLabel.text   = LocalizationManager.OwnedMerged  + " " + (_ownedOpen   ? "▲" : "▼");

        // Przywróć poprzednią selekcję
        RestoreSelection();
        SetupHoldToMerge();
    }

    void RestoreSelection()
    {
        switch (GameData.Instance.mergeSelType)
        {
            case 1:
                var cfg = GetCfg(GameData.Instance.mergeSelClass);
                if (cfg != null) SelectBasic(cfg);
                else ShowEmpty();
                break;
            case 2:
                var cfg2 = GetCfg(GameData.Instance.mergeSelClass);
                if (cfg2 != null) SelectUpgrade(cfg2, GameData.Instance.mergeSelFromLevel);
                else ShowEmpty();
                break;
            default:
                ShowEmpty();
                break;
        }
    }

    void SetupHoldToMerge()
    {
        if (actionButton != null)
        {
            var trigger = actionButton.gameObject.GetComponent<EventTrigger>()
                       ?? actionButton.gameObject.AddComponent<EventTrigger>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => { _actionHeld = true; _holdTimer = 0f; _holdHeld = 0f; _holdFired = false; _holdWasUsed = false; });
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => { _holdWasUsed = _holdFired; _actionHeld = false; _holdTimer = 0f; _holdHeld = 0f; _holdFired = false; });
            trigger.triggers.Add(up);
        }

        if (sellButton != null)
        {
            var trigger = sellButton.gameObject.GetComponent<EventTrigger>()
                       ?? sellButton.gameObject.AddComponent<EventTrigger>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => { _sellHeld = true; _sellHoldTimer = 0f; _sellHoldHeld = 0f; _sellHoldFired = false; });
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => { _sellHeld = false; _sellHoldTimer = 0f; _sellHoldHeld = 0f; _sellHoldFired = false; });
            trigger.triggers.Add(up);
        }
    }

    private void Update()
    {
        if (titleRect != null)
        {
            _bobTimer += Time.deltaTime;
            titleRect.anchoredPosition = new Vector2(
                titleRect.anchoredPosition.x,
                Mathf.Sin(_bobTimer * 1.5f) * 16f - 55f);
        }

        if (UnityEngine.InputSystem.Mouse.current != null
            && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        {
            _actionHeld = false; _holdTimer = 0f; _holdHeld = 0f; _holdFired = false;
            _sellHeld   = false; _sellHoldTimer = 0f; _sellHoldHeld = 0f; _sellHoldFired = false;
        }

        // Hold-to-merge
        if (_actionHeld && actionButton != null && actionButton.interactable && _selType != SelType.None)
        {
            _holdTimer += Time.deltaTime;
            _holdHeld  += Time.deltaTime;

            if (!_holdFired && _holdTimer >= HOLD_INITIAL_DELAY)
            {
                _holdFired = true; _holdTimer = 0f;
                OnActionClicked();
            }
            else if (_holdFired)
            {
                float rampProgress = Mathf.Clamp01((_holdHeld - HOLD_INITIAL_DELAY) / HOLD_RAMP_TIME);
                float interval = Mathf.Lerp(HOLD_INTERVAL_START, HOLD_INTERVAL_MIN, rampProgress);
                if (_holdTimer >= interval)
                {
                    _holdTimer = 0f;
                    OnActionClicked();
                }
            }
        }

        // Hold-to-sell
        if (_sellHeld && sellButton != null && sellButton.interactable && _selType != SelType.None)
        {
            _sellHoldTimer += Time.deltaTime;
            _sellHoldHeld  += Time.deltaTime;

            if (!_sellHoldFired && _sellHoldTimer >= HOLD_INITIAL_DELAY)
            {
                _sellHoldFired = true; _sellHoldTimer = 0f;
                TrySellMerged();
            }
            else if (_sellHoldFired)
            {
                float rampProgress = Mathf.Clamp01((_sellHoldHeld - HOLD_INITIAL_DELAY) / HOLD_RAMP_TIME);
                float interval = Mathf.Lerp(HOLD_INTERVAL_START, HOLD_INTERVAL_MIN, rampProgress);
                if (_sellHoldTimer >= interval)
                {
                    _sellHoldTimer = 0f;
                    TrySellMerged();
                }
            }
        }
    }

    // ── Budowanie list ────────────────────────────────────────────────────
    void BuildLists()
    {
        BuildBasicList();
        BuildUpgradeList();
        BuildOwnedList();
    }

    void BuildBasicList()
    {
        if (basicContent == null || listRowPrefab == null) return;
        ClearContainer(basicContent);

        bool any = false;
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null) continue;
            int owned = GameData.Instance.CountBasicBalls(cfg.ballClass);
            if (owned == 0) continue;
            any = true;

            bool canMerge = GameData.Instance.CanMergeBasic(cfg.ballClass);
            bool hasTier  = GameData.Instance.HasMergeTier(1);
            string badge  = owned + "/5";
            Color dotCol  = canMerge ? new Color(1f, 0.85f, 0.1f) : cfg.color;

            var row = SpawnRow(basicContent);
            if (row == null) continue;
            row.Setup(dotCol, LocalizationManager.GetClassName(cfg.ballClass), badge, readyToMerge: canMerge);

            var capturedCfg = cfg;
            if (row.btn != null)
                row.btn.onClick.AddListener(() => SelectBasic(capturedCfg));
        }

        if (!any)
        {
            var row = SpawnRow(basicContent);
            row.Setup(Color.gray, LocalizationManager.BuyBallsHint, "–", false);
        }

        if (basicContent != null)
            basicContent.gameObject.SetActive(_basicOpen);
    }

    void BuildUpgradeList()
    {
        if (upgradeContent == null || listRowPrefab == null) return;
        ClearContainer(upgradeContent);

        var groups = new Dictionary<(BallClass, int), int>();
        foreach (var m in GameData.Instance.mergedBalls)
        {
            var key = (m.ballClass, m.mergeLevel);
            groups[key] = groups.ContainsKey(key) ? groups[key] + 1 : 1;
        }

        bool any = false;
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null) continue;
            var levels = new List<int>();
            foreach (var key in groups.Keys)
                if (key.Item1 == cfg.ballClass) levels.Add(key.Item2);
            levels.Sort();

            foreach (int level in levels)
            {
                if (level >= 5) continue;  // Lv5 = max, nie można dalej scalać

                int count = groups[(cfg.ballClass, level)];
                any = true;

                bool canMerge = GameData.Instance.CanMergeUp(cfg.ballClass, level);
                Color dotCol = canMerge ? new Color(1f, 0.85f, 0.1f) : new Color(0.8f, 0.6f, 0.1f);

                var row = SpawnRow(upgradeContent);
                if (row == null) continue;
                row.Setup(dotCol,
                          LocalizationManager.GetClassName(cfg.ballClass) + " " + LocalizationManager.LevelPrefix + level,
                          count + "/5",
                          readyToMerge: canMerge);

                var capCfg   = cfg;
                int capLevel = level;
                if (row.btn != null)
                    row.btn.onClick.AddListener(() => SelectUpgrade(capCfg, capLevel));
            }
        }

        if (!any)
        {
            var row = SpawnRow(upgradeContent);
            row.Setup(Color.gray, LocalizationManager.NoMergeAvail, "–", false);
        }

        if (upgradeContent != null)
            upgradeContent.gameObject.SetActive(_upgradeOpen);
    }

    void BuildOwnedList()
    {
        if (ownedContent == null || listRowPrefab == null) return;
        ClearContainer(ownedContent);

        var groups = new Dictionary<(BallClass, int), int>();
        foreach (var m in GameData.Instance.mergedBalls)
        {
            var key = (m.ballClass, m.mergeLevel);
            groups[key] = groups.ContainsKey(key) ? groups[key] + 1 : 1;
        }

        bool any = false;
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null) continue;
            var levels = new List<int>();
            foreach (var key in groups.Keys)
                if (key.Item1 == cfg.ballClass) levels.Add(key.Item2);
            levels.Sort();

            foreach (int level in levels)
            {
                any = true;
                int count = groups[(cfg.ballClass, level)];
                bool isMastery = level == 5 && GameData.Instance.HasMastery(cfg.ballClass);
                string prefix  = isMastery ? LocalizationManager.MasteryPrefix : LocalizationManager.SuperPrefix;
                Color rowColor = isMastery ? new Color(0.4f, 1f, 0.8f) : new Color(1f, 0.85f, 0.1f);

                var row = SpawnRow(ownedContent);
                if (row == null) continue;
                row.Setup(rowColor,
                          prefix + LocalizationManager.GetClassName(cfg.ballClass),
                          LocalizationManager.LevelPrefix + level + "  x" + count);

                var capCfg   = cfg;
                int capLevel = level;
                int capCount = count;
                if (row.btn != null)
                    row.btn.onClick.AddListener(() => SelectOwnedGroup(capCfg, capLevel, capCount));
            }
        }

        if (!any)
        {
            var empty = SpawnRow(ownedContent);
            empty.Setup(Color.gray, LocalizationManager.NoOwnedMerged, "–", false);
        }

        if (ownedContent != null)
            ownedContent.gameObject.SetActive(_ownedOpen);
    }

    ShopListRow SpawnRow(Transform parent)
    {
        var go  = Instantiate(listRowPrefab, parent);
        var row = go.GetComponent<ShopListRow>();
        if (row != null) row.Init(detailView);
        return row;
    }

    void ClearContainer(Transform t)
    {
        foreach (Transform child in t) Destroy(child.gameObject);
    }

    // ── Akordeony ─────────────────────────────────────────────────────────
    public void ToggleBasic()
    {
        _basicOpen = !_basicOpen;
        GameData.Instance.mergeBasicOpen = _basicOpen;
        if (basicContent != null) basicContent.gameObject.SetActive(_basicOpen);
        if (basicToggleLabel != null)
            basicToggleLabel.text = LocalizationManager.MergeBasic + " " + (_basicOpen ? "▲" : "▼");
    }

    public void ToggleUpgrade()
    {
        _upgradeOpen = !_upgradeOpen;
        GameData.Instance.mergeUpgradeOpen = _upgradeOpen;
        if (upgradeContent != null) upgradeContent.gameObject.SetActive(_upgradeOpen);
        if (upgradeToggleLabel != null)
            upgradeToggleLabel.text = LocalizationManager.MergeUpgrade + " " + (_upgradeOpen ? "▲" : "▼");
    }

    public void ToggleOwned()
    {
        _ownedOpen = !_ownedOpen;
        GameData.Instance.mergeOwnedOpen = _ownedOpen;
        if (ownedContent != null) ownedContent.gameObject.SetActive(_ownedOpen);
        if (ownedToggleLabel != null)
            ownedToggleLabel.text = LocalizationManager.OwnedMerged + " " + (_ownedOpen ? "▲" : "▼");
    }

    // ── Selekcja ──────────────────────────────────────────────────────────
    void SelectBasic(ClassConfig cfg)
    {
        _selType = SelType.Basic;
        _selCfg  = cfg;
        GameData.Instance.mergeSelType  = 1;
        GameData.Instance.mergeSelClass = cfg.ballClass;

        int owned      = GameData.Instance.CountBasicBalls(cfg.ballClass);
        bool hasTier   = GameData.Instance.HasMergeTier(1);
        bool canMerge  = GameData.Instance.CanMergeBasic(cfg.ballClass);
        _selCanMerge   = canMerge;
        _selNeedsTier  = !hasTier;

        string btnText;
        bool btnEnabled;
        if (!hasTier)
        {
            btnText    = LocalizationManager.MergeNeedArenaUpgrade;
            btnEnabled = false;
        }
        else if (canMerge)
        {
            btnText    = LocalizationManager.MergeBtn;
            btnEnabled = true;
        }
        else
        {
            btnText    = LocalizationManager.BuyMoreBalls;
            btnEnabled = true;  // kliknięcie przeniesie do sklepu
        }

        int refund      = GameData.GetBallPrice(cfg.ballClass);
        bool canSell    = owned > 1 || (!GameData.IsBaseClass(cfg.ballClass) && owned > 0);
        // Bazowa klasa z 1 egzemplarzem (niekonsumowalnym) – nie da się sprzedać ostatniej
        if (GameData.IsBaseClass(cfg.ballClass) && !GameData.Instance.consumedBaseBalls.Contains(cfg.ballClass) && owned <= 1)
            canSell = false;

        if (sellButton != null) sellButtonLabel.text = LocalizationManager.SellBtn;

        ShowDetail(
            canMerge ? new Color(1f, 0.85f, 0.1f) : cfg.color,
            LocalizationManager.GetClassName(cfg.ballClass),
            LocalizationManager.MergeFlavor,
            LocalizationManager.OwnedCount(owned) + LocalizationManager.ValueLine(refund),
            btnText,
            btnEnabled,
            isSellMode: false
        );

        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(canSell);
            sellButton.interactable = canSell;
        }
    }

    void SelectUpgrade(ClassConfig cfg, int fromLevel)
    {
        _selType      = SelType.Upgrade;
        _selCfg       = cfg;
        _selFromLevel = fromLevel;
        GameData.Instance.mergeSelType      = 2;
        GameData.Instance.mergeSelClass     = cfg.ballClass;
        GameData.Instance.mergeSelFromLevel = fromLevel;

        int count      = GameData.Instance.CountMergedOfLevel(cfg.ballClass, fromLevel);
        bool hasTier   = GameData.Instance.HasMergeTier(fromLevel + 1);
        bool canMerge  = GameData.Instance.CanMergeUp(cfg.ballClass, fromLevel);
        _selCanMerge   = canMerge;
        _selNeedsTier  = !hasTier;
        int nextLevel  = fromLevel + 1;

        string btnText;
        bool btnEnabled;
        if (!hasTier)
        {
            btnText    = LocalizationManager.MergeNeedArenaUpgrade;
            btnEnabled = false;
        }
        else if (canMerge)
        {
            btnText    = LocalizationManager.MergeBtn;
            btnEnabled = true;
        }
        else
        {
            btnText    = LocalizationManager.BuyMoreBalls;
            btnEnabled = true;  // kliknięcie przeniesie do sklepu
        }

        int upgradeRefund = GameData.GetMergedBallRefund(cfg.ballClass, fromLevel);
        ShowDetail(
            canMerge ? new Color(1f, 0.85f, 0.1f) : new Color(0.8f, 0.6f, 0.1f),
            LocalizationManager.GetClassName(cfg.ballClass) + "  " + LocalizationManager.LevelPrefix + fromLevel + " → " + LocalizationManager.LevelPrefix + nextLevel,
            LocalizationManager.MergeUpgFlavor,
            LocalizationManager.OwnedCountLevel(fromLevel, count) + LocalizationManager.ValueLine(upgradeRefund),
            btnText,
            btnEnabled
        );
    }

    void SelectOwnedGroup(ClassConfig cfg, int level, int count)
    {
        _selType      = SelType.OwnedInfo;
        _selCfg       = cfg;
        _selOwnedLevel= level;

        var sample = GameData.Instance.mergedBalls.Find(m => m.ballClass == cfg.ballClass && m.mergeLevel == level);
        if (sample == null) return;

        int mult     = (int)sample.statMultiplier;
        int goldMult = sample.goldMultiplier;
        int refund   = GameData.GetMergedBallRefund(cfg.ballClass, level);

        bool isMastery = level == 5 && GameData.Instance.HasMastery(cfg.ballClass);
        string prefix  = isMastery ? LocalizationManager.MasteryPrefix : LocalizationManager.SuperPrefix;
        Color iconCol  = isMastery ? new Color(0.4f, 1f, 0.8f) : (level == 5 ? new Color(1f, 0.6f, 0.1f) : new Color(1f, 0.85f, 0.1f));

        string stats   = LocalizationManager.MergedStatText(mult, goldMult, level) + "\n" + LocalizationManager.OwnedCount(count) + LocalizationManager.ValueLine(refund);
        string mastery = null;
        if (level == 5)
            mastery = LocalizationManager.PassiveUnlocked + "\n\n" + BuildCrownInfo(cfg.ballClass);

        ShowDetail(
            iconCol,
            prefix + LocalizationManager.GetClassName(cfg.ballClass) + "  (" + LocalizationManager.LevelPrefix + level + ")",
            LocalizationManager.SuperFlavor,
            stats,
            LocalizationManager.SellBtn,
            true,
            mastery,
            isSellMode: true
        );
    }

    string BuildCrownInfo(BallClass cls)
    {
        var crowns = GameData.Instance.GetOrCreateCrowns(cls);
        return LocalizationManager.CrownProgress(crowns);
    }

    void ShowDetail(Color iconCol, string name, string flavor,
                    string stats, string btnLabel, bool btnEnabled,
                    string mastery = null, bool isSellMode = false)
    {
        if (emptyHint  != null) emptyHint.SetActive(false);
        if (detailView != null) detailView.SetActive(true);

        if (detailIcon   != null) detailIcon.color  = iconCol;
        if (detailName   != null) detailName.text   = name;
        if (detailFlavor != null) detailFlavor.text = flavor;
        if (detailStats  != null) detailStats.text  = stats;

        // Jeśli jest osobny sellButton – rozdziel akcje
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(isSellMode);
            if (isSellMode && sellButtonLabel != null)
                sellButtonLabel.text = btnLabel;

            if (actionButton != null) actionButton.gameObject.SetActive(!isSellMode);
            if (!isSellMode)
            {
                if (actionLabel  != null) actionLabel.text  = btnLabel;
                if (actionButton != null) actionButton.interactable = btnEnabled;
            }
        }
        else
        {
            if (actionLabel  != null) actionLabel.text  = btnLabel;
            if (actionButton != null) actionButton.interactable = btnEnabled;
        }

        if (masteryText != null)
        {
            masteryText.text = mastery ?? "";
            masteryText.gameObject.SetActive(!string.IsNullOrEmpty(mastery));
        }
    }

    void ShowEmpty()
    {
        if (emptyHint    != null) emptyHint.SetActive(true);
        if (detailView   != null) detailView.SetActive(false);
        if (masteryText  != null) masteryText.gameObject.SetActive(false);
        if (sellButton   != null) sellButton.gameObject.SetActive(false);
        if (actionButton != null) actionButton.gameObject.SetActive(true);
        _selType    = SelType.None;
        _actionHeld = false; _holdTimer = 0f; _holdHeld = 0f; _holdFired = false;
        GameData.Instance.mergeSelType = 0;
    }

    // ── Akcja przycisku ───────────────────────────────────────────────────
    public void OnActionClicked()
    {
        // Jeśli hold był aktywny — zignoruj onClick przy puszczeniu (żeby nie przenosić do sklepu)
        if (_holdWasUsed) { _holdWasUsed = false; return; }

        switch (_selType)
        {
            case SelType.Basic:     TryMergeBasic();   break;
            case SelType.Upgrade:   TryMergeUpgrade(); break;
            case SelType.OwnedInfo: TrySellMerged();   break;
        }
    }

    void TryMergeBasic()
    {
        if (_selCfg == null) return;

        // Brakuje tiera areny – nic nie rób (przycisk powinien być wyłączony)
        if (!GameData.Instance.HasMergeTier(1))
        {
            ShowMsg(LocalizationManager.MergeNeedArenaUpgrade);
            return;
        }

        // Nie ma wystarczająco kulek
        if (!GameData.Instance.CanMergeBasic(_selCfg.ballClass))
        {
            // Jeśli trzyma przycisk (hold-to-merge) — po prostu przestań, nie przenoś do sklepu
            if (_actionHeld)
            {
                _actionHeld = false;
                return;
            }
            // Kliknięcie — idź do sklepu z tą klasą
            GameData.Instance.shopPendingBall      = true;
            GameData.Instance.shopPendingBallClass = _selCfg.ballClass;
            GameData.Instance.shopBallsOpen        = true;
            SceneTransition.ExitTo("ShopScene");
            return;
        }

        if (GameData.Instance.TryMergeBasic(_selCfg.ballClass))
        {
            GameData.Instance.Save();
            ShowMsg(LocalizationManager.MergeSuccessBasic(LocalizationManager.GetClassName(_selCfg.ballClass)));
            RefreshTopBar(); BuildLists();
            SelectBasic(_selCfg);
        }
    }

    void TryMergeUpgrade()
    {
        if (_selCfg == null) return;

        if (!GameData.Instance.HasMergeTier(_selFromLevel + 1))
        {
            ShowMsg(LocalizationManager.MergeNeedArenaUpgrade);
            return;
        }

        // Nie ma wystarczająco scalonych
        if (!GameData.Instance.CanMergeUp(_selCfg.ballClass, _selFromLevel))
        {
            // Jeśli trzyma przycisk (hold-to-merge) — po prostu przestań, nie przenoś do sklepu
            if (_actionHeld)
            {
                _actionHeld = false;
                return;
            }
            // Kliknięcie — idź do sklepu z tą klasą
            GameData.Instance.shopPendingBall      = true;
            GameData.Instance.shopPendingBallClass = _selCfg.ballClass;
            GameData.Instance.shopBallsOpen        = true;
            SceneTransition.ExitTo("ShopScene");
            return;
        }

        if (GameData.Instance.TryMergeUp(_selCfg.ballClass, _selFromLevel))
        {
            GameData.Instance.Save();
            ShowMsg(LocalizationManager.MergeSuccessUp(
                LocalizationManager.GetClassName(_selCfg.ballClass), _selFromLevel));
            RefreshTopBar(); BuildLists();
            SelectUpgrade(_selCfg, _selFromLevel);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void RefreshTopBar()
    {
        if (goldText != null) goldText.text = LocalizationManager.GoldPrefix + GameData.Instance.gold;
        if (infoText != null) infoText.text = LocalizationManager.MergeInfoLine;
    }

    void ShowMsg(string msg) { if (infoText != null) infoText.text = msg; }

    ClassConfig GetCfg(BallClass cls)
    {
        var cfg = allClassConfigs.Find(c => c.ballClass == cls);
        if (cfg == null) cfg = Resources.Load<ClassConfig>("ClassConfigs/" + cls);
        return cfg;
    }

    // ── Kompatybilność z ShopItem (stary prefab) ─────────────────────────
    public void TryMerge(BallClass cls, ClassConfig cfg)
    {
        _selCfg  = cfg;
        _selType = SelType.Basic;
        TryMergeBasic();
    }

    public void TryMergeUp(BallClass cls, ClassConfig cfg, int fromLevel)
    {
        _selCfg       = cfg;
        _selFromLevel = fromLevel;
        _selType      = SelType.Upgrade;
        TryMergeUpgrade();
    }

    void TrySellMerged()
    {
        if (_selCfg == null) return;

        if (_selType == SelType.Basic)
        {
            TrySellBasic();
            return;
        }

        int level = _selOwnedLevel;
        int idx = GameData.Instance.mergedBalls.FindLastIndex(
            m => m.ballClass == _selCfg.ballClass && m.mergeLevel == level);
        if (idx < 0) return;

        GameData.Instance.mergedBalls.RemoveAt(idx);
        int refund = GameData.GetMergedBallRefund(_selCfg.ballClass, level);
        GameData.Instance.gold += refund;
        GameData.Instance.Save();
        AudioController.Instance?.PlayShopBuy();
        ShowMsg(LocalizationManager.SoldBall(LocalizationManager.GetClassName(_selCfg.ballClass)));
        RefreshTopBar(); BuildLists();

        if (GameData.Instance.CountMergedOfLevel(_selCfg.ballClass, level) > 0)
            SelectOwnedGroup(_selCfg, level, GameData.Instance.CountMergedOfLevel(_selCfg.ballClass, level));
        else
            ShowEmpty();
    }

    void TrySellBasic()
    {
        var gd    = GameData.Instance;
        var cls   = _selCfg.ballClass;
        int owned = gd.CountBasicBalls(cls);
        int price = GameData.GetBallPrice(cls);

        bool isBase       = GameData.IsBaseClass(cls);
        bool baseConsumed = gd.consumedBaseBalls.Contains(cls);

        // Nie możemy sprzedać jeśli zostałaby 0 kulek
        // (bazowa niekonsumowana liczy jako 1 darmowa)
        if (isBase && !baseConsumed && owned <= 1) return;
        if (owned <= 0) return;

        // Usuń: najpierw z purchasedBalls, jeśli nie ma – konsumuj bazową
        int idx = gd.purchasedBalls.FindLastIndex(b => b == cls);
        if (idx >= 0)
        {
            gd.purchasedBalls.RemoveAt(idx);
        }
        else if (isBase && !baseConsumed)
        {
            // Sprzedaż "darmowej" bazowej = konsumujemy ją i nie oddajemy kasy
            // (gracz nigdy jej nie kupował, więc refund = 0)
            gd.consumedBaseBalls.Add(cls);
            price = 0;
        }

        gd.gold += price;
        gd.Save();
        AudioController.Instance?.PlayShopBuy();
        ShowMsg(LocalizationManager.SoldBall(LocalizationManager.GetClassName(cls)));
        RefreshTopBar(); BuildLists();

        if (gd.CountBasicBalls(cls) > 0)
            SelectBasic(_selCfg);
        else
            ShowEmpty();
    }

    // ── Nawigacja ─────────────────────────────────────────────────────────
    public void GoToShop()     => SceneTransition.ExitTo("ShopScene");
    public void GoToGame()     => SceneTransition.ExitTo("GameScene");
    public void GoToMainMenu() => SceneTransition.ExitTo("MainMenu");
    public void OpenSettings() => SettingsPanel.Open();
}
