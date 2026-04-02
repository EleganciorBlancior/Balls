// ShopUI.cs (v8) – akordeonowa lista (lewo) + panel szczegółów (prawo)
//
// UKŁAD SCENY (ustawić w Unity Editor):
//
//  Canvas
//  ├── TopBar
//  │   ├── TitleRect (TMP, bobbing)
//  │   ├── GoldText (TMP)
//  │   └── InfoText (TMP)
//  ├── LeftPanel  (1/3 szerokości) ← ScrollRect → Viewport → Content (VLG)
//  │   ├── BallsHeaderBtn (Button + TMP "Kulki ▼")
//  │   ├── BallsContent (VerticalLayoutGroup, ContentSizeFitter)
//  │   ├── UpgradesHeaderBtn (Button + TMP "Ulepszenia ▼")
//  │   └── UpgradesContent (VerticalLayoutGroup, ContentSizeFitter)
//  └── RightPanel (2/3 szerokości)
//      ├── EmptyHint (TMP "← Wybierz coś z listy")
//      └── DetailView (domyślnie inactive)
//          ├── DetailIcon (Image, 80×80)
//          ├── DetailName (TMP Bold)
//          ├── DetailFlavor (TMP Italic)
//          ├── DetailStats (TMP monospace)
//          └── ActionButton (Button)
//              └── ActionLabel (TMP)
//
// Do prefabu listRowPrefab podepnij ShopListRow.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    // ── Top bar ───────────────────────────────────────────────────────────────
    [Header("Top bar")]
    public TMP_Text      goldText;
    public TMP_Text      infoText;
    public RectTransform titleRect;

    // ── Lewa kolumna – akordeony ───────────────────────────────────────────
    [Header("Accordion – Kulki")]
    public Transform ballsContent;
    public TMP_Text  ballsToggleLabel;

    [Header("Accordion – Ulepszenia")]
    public Transform upgradesContent;
    public TMP_Text  upgradesToggleLabel;

    [Header("Prefab wiersza")]
    public GameObject listRowPrefab;

    // ── Prawa kolumna – panel szczegółów ──────────────────────────────────
    [Header("Panel szczegółów")]
    public GameObject emptyHint;
    public GameObject detailView;
    public Image      detailIcon;
    public TMP_Text   detailName;
    public TMP_Text   detailFlavor;
    public TMP_Text   detailStats;
    public Button     actionButton;
    public TMP_Text   actionLabel;

    // ── Przyciski nawigacyjne (opcjonalne – podpnij TMP_Text z przycisku) ────
    [Header("Przyciski nawigacyjne (opcjonalne)")]
    public TMP_Text playButtonLabel;
    public TMP_Text mergeButtonLabel;
    public TMP_Text backButtonLabel;

    // ── Klasy ─────────────────────────────────────────────────────────────
    [Header("Klasy (16 assetów)")]
    public List<ClassConfig> allClassConfigs;

    // ── Stan ──────────────────────────────────────────────────────────────
    private bool _ballsOpen     = false;
    private bool _upgradesOpen  = false;
    private float _bobTimer;

    private enum SelType { None, Ball, Arena, Pull }
    private SelType      _selType  = SelType.None;
    private ClassConfig  _selCfg;
    private int          _selTierIdx;

    // Hold-to-buy z płynnym przyspieszaniem
    const float HOLD_INITIAL_DELAY  = 0.5f;
    const float HOLD_INTERVAL_START = 0.15f;
    const float HOLD_INTERVAL_MIN   = 0.002f;
    const float HOLD_RAMP_TIME      = 3f;    // sekundy do osiągnięcia max prędkości

    private bool  _actionHeld;
    private float _holdTimer;   // czas od ostatniego fire
    private float _holdHeld;    // łączny czas trzymania (do rampy)
    private bool  _holdFired;   // czy initial delay minął

    // ── Unity lifecycle ───────────────────────────────────────────────────
    private void Start()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData"); go.AddComponent<GameData>();
        }

        // Przyciski nawigacyjne
        if (playButtonLabel  != null) playButtonLabel.text  = LocalizationManager.MainMenuPlay;
        if (mergeButtonLabel != null) mergeButtonLabel.text = LocalizationManager.MainMenuLab;
        if (backButtonLabel  != null) backButtonLabel.text  = LocalizationManager.Back;

        RefreshTopBar();

        // Przywróć stan akordeonów
        _ballsOpen    = GameData.Instance.shopBallsOpen;
        _upgradesOpen = GameData.Instance.shopUpgradesOpen;

        BuildLists();

        // Nagłówki akordeonów
        if (ballsToggleLabel    != null)
            ballsToggleLabel.text    = LocalizationManager.BallsSection    + " " + (_ballsOpen    ? "▲" : "▼");
        if (upgradesToggleLabel != null)
            upgradesToggleLabel.text = LocalizationManager.UpgradesSection + " " + (_upgradesOpen ? "▲" : "▼");

        // Sprawdź nawigację z MergeUI (pending ball)
        if (GameData.Instance.shopPendingBall)
        {
            GameData.Instance.shopPendingBall = false;
            var pendingClass = GameData.Instance.shopPendingBallClass;
            var cfg = allClassConfigs.Find(c => c.ballClass == pendingClass);
            if (cfg != null)
            {
                if (!_ballsOpen) ToggleBalls();
                SelectBall(cfg);
                GameData.Instance.shopSelType  = 1;
                GameData.Instance.shopSelClass = pendingClass;
            }
            else ShowEmpty();
        }
        else
        {
            // Przywróć poprzednią selekcję
            RestoreSelection();
        }

        SetupHoldToBuy();
    }

    void RestoreSelection()
    {
        switch (GameData.Instance.shopSelType)
        {
            case 1:
                var cfg = allClassConfigs.Find(c => c.ballClass == GameData.Instance.shopSelClass);
                if (cfg != null) SelectBall(cfg);
                else ShowEmpty();
                break;
            case 2:
                if (!GameData.Instance.IsMaxTier)
                    SelectArena(GameData.Instance.shopSelTierIdx);
                else ShowEmpty();
                break;
            case 3:
                if (GameData.Instance.pullUpgradeLevel < GameData.PULL_MAX_LEVEL)
                    SelectPull();
                else ShowEmpty();
                break;
            default:
                ShowEmpty();
                break;
        }
    }

    void SetupHoldToBuy()
    {
        if (actionButton == null) return;
        var trigger = actionButton.gameObject.GetComponent<EventTrigger>()
                   ?? actionButton.gameObject.AddComponent<EventTrigger>();

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => { _actionHeld = true; _holdTimer = 0f; _holdHeld = 0f; _holdFired = false; });
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => { _actionHeld = false; _holdTimer = 0f; _holdHeld = 0f; _holdFired = false; });
        trigger.triggers.Add(up);
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

        if (_actionHeld && UnityEngine.InputSystem.Mouse.current != null
            && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        {
            _actionHeld = false;
            _holdTimer  = 0f;
            _holdHeld   = 0f;
            _holdFired  = false;
        }

        if (_actionHeld && actionButton != null && actionButton.interactable && _selType != SelType.None)
        {
            _holdTimer += Time.deltaTime;
            _holdHeld  += Time.deltaTime;

            if (!_holdFired && _holdTimer >= HOLD_INITIAL_DELAY)
            {
                _holdFired = true;
                _holdTimer = 0f;
                OnActionClicked();
            }
            else if (_holdFired)
            {
                // Płynna rampa przyspieszenia
                float rampProgress = Mathf.Clamp01((_holdHeld - HOLD_INITIAL_DELAY) / HOLD_RAMP_TIME);
                float interval = Mathf.Lerp(HOLD_INTERVAL_START, HOLD_INTERVAL_MIN, rampProgress);
                if (_holdTimer >= interval)
                {
                    _holdTimer = 0f;
                    OnActionClicked();
                }
            }
        }
    }

    // ── Budowanie list ────────────────────────────────────────────────────
    void BuildLists()
    {
        BuildBallsList();
        BuildUpgradesList();
    }

    void BuildBallsList()
    {
        if (ballsContent == null || listRowPrefab == null) return;
        ClearContainer(ballsContent);

        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null) continue;

            int price = GameData.GetBallPrice(cfg.ballClass);

            var row = SpawnRow(ballsContent);
            if (row == null) continue;
            row.Setup(cfg.color, LocalizationManager.GetClassName(cfg.ballClass), price + "g");

            var capturedCfg = cfg;
            if (row.btn != null)
                row.btn.onClick.AddListener(() => SelectBall(capturedCfg));
        }

        if (ballsContent != null)
            ballsContent.gameObject.SetActive(_ballsOpen);
    }

    void BuildUpgradesList()
    {
        if (upgradesContent == null || listRowPrefab == null) return;
        ClearContainer(upgradesContent);

        // Arena
        if (!GameData.Instance.IsMaxTier)
        {
            int nextIdx  = GameData.Instance.arenaTierIndex + 1;
            var nextTier = GameData.ArenaTiers[nextIdx];
            var row = SpawnRow(upgradesContent);
            if (row != null)
            {
                row.Setup(new Color(0.8f, 0.6f, 0.1f),
                          "Arena: " + LocalizationManager.GetArenaTierName(nextIdx),
                          nextTier.upgradeCost + "g");
                if (row.btn != null)
                    row.btn.onClick.AddListener(() => SelectArena(nextIdx));
            }
        }
        else
        {
            var row = SpawnRow(upgradesContent);
            if (row != null)
                row.Setup(new Color(0.4f, 0.4f, 0.4f), LocalizationManager.ArenaMax, "MAX", false);
        }

        // Pull upgrade
        int pullLvl = GameData.Instance.pullUpgradeLevel;
        if (pullLvl < GameData.PULL_MAX_LEVEL)
        {
            int nextCost = GameData.GetPullUpgradeCost(pullLvl);
            var row = SpawnRow(upgradesContent);
            if (row != null)
            {
                row.Setup(new Color(0.2f, 0.6f, 0.9f),
                          LocalizationManager.PullUpgradeTitle + " " + LocalizationManager.LevelPrefix + (pullLvl + 1),
                          nextCost + "g");
                if (row.btn != null)
                    row.btn.onClick.AddListener(() => SelectPull());
            }
        }
        else
        {
            var row = SpawnRow(upgradesContent);
            if (row != null)
                row.Setup(new Color(0.4f, 0.4f, 0.4f),
                          LocalizationManager.PullUpgradeTitle,
                          "MAX", false);
        }

        if (upgradesContent != null)
            upgradesContent.gameObject.SetActive(_upgradesOpen);
    }

    ShopListRow SpawnRow(Transform parent)
    {
        var go  = Instantiate(listRowPrefab, parent);
        var row = go.GetComponent<ShopListRow>();
        if (row != null) row.Init(detailView);
        if (row?.btn != null) UIAudioHook.HookButton(row.btn);
        return row;
    }

    void ClearContainer(Transform t)
    {
        foreach (Transform child in t) Destroy(child.gameObject);
    }

    // ── Akordeony ─────────────────────────────────────────────────────────
    public void ToggleBalls()
    {
        _ballsOpen = !_ballsOpen;
        GameData.Instance.shopBallsOpen = _ballsOpen;
        if (ballsContent != null) ballsContent.gameObject.SetActive(_ballsOpen);
        if (ballsToggleLabel != null)
            ballsToggleLabel.text = LocalizationManager.BallsSection + " " + (_ballsOpen ? "▲" : "▼");
    }

    public void ToggleUpgrades()
    {
        _upgradesOpen = !_upgradesOpen;
        GameData.Instance.shopUpgradesOpen = _upgradesOpen;
        if (upgradesContent != null) upgradesContent.gameObject.SetActive(_upgradesOpen);
        if (upgradesToggleLabel != null)
            upgradesToggleLabel.text = LocalizationManager.UpgradesSection + " " + (_upgradesOpen ? "▲" : "▼");
    }

    // ── Selekcja ──────────────────────────────────────────────────────────
    void SelectBall(ClassConfig cfg)
    {
        _selType = SelType.Ball;
        _selCfg  = cfg;
        GameData.Instance.shopSelType  = 1;
        GameData.Instance.shopSelClass = cfg.ballClass;

        int  price     = GameData.GetBallPrice(cfg.ballClass);
        var  tier      = GameData.Instance.CurrentTier;
        int  totalOwned = GameData.Instance.TotalOwnedBalls();
        int  totalMax  = 5 + tier.maxExtraBalls;
        bool canBuy = GameData.Instance.gold >= price && totalOwned < totalMax;

        string btnText = canBuy
            ? LocalizationManager.BuyBtn(price)
            : (GameData.Instance.gold < price
                ? LocalizationManager.NotEnoughGold
                : LocalizationManager.FullArena);

        ShowDetail(
            cfg.color,
            LocalizationManager.GetClassName(cfg.ballClass),
            LocalizationManager.GetFlavor(cfg.ballClass),
            GetStats(cfg),
            btnText,
            canBuy
        );
    }

    void SelectArena(int tierIdx)
    {
        _selType    = SelType.Arena;
        _selTierIdx = tierIdx;
        GameData.Instance.shopSelType    = 2;
        GameData.Instance.shopSelTierIdx = tierIdx;
        var tier    = GameData.ArenaTiers[tierIdx];
        bool canBuy = GameData.Instance.gold >= tier.upgradeCost;

        ShowDetail(
            new Color(0.8f, 0.6f, 0.1f),
            "Arena: " + LocalizationManager.GetArenaTierName(tierIdx),
            LocalizationManager.ArenaMoreSpace,
            LocalizationManager.BallLimitLabel  + (5 + tier.maxExtraBalls) + "\n" +
            LocalizationManager.BallScaleLabel  + tier.ballScaleMultiplier.ToString("F2"),
            canBuy ? LocalizationManager.BuyArenaBtn(tier.upgradeCost) : LocalizationManager.NotEnoughGold,
            canBuy
        );
    }

    void SelectPull()
    {
        _selType = SelType.Pull;
        GameData.Instance.shopSelType = 3;
        int pullLvl  = GameData.Instance.pullUpgradeLevel;
        int nextCost = GameData.GetPullUpgradeCost(pullLvl);
        float force  = GameData.GetPullForce(pullLvl + 1);
        float dur    = GameData.GetPullDuration(pullLvl + 1);
        bool canBuy  = GameData.Instance.gold >= nextCost;

        ShowDetail(
            new Color(0.2f, 0.6f, 0.9f),
            LocalizationManager.PullUpgradeTitle + " → " + LocalizationManager.LevelPrefix + (pullLvl + 1),
            LocalizationManager.PullFlavorText,
            LocalizationManager.PullStats(pullLvl + 1, force, dur),
            canBuy ? LocalizationManager.BuyPullBtn(nextCost) : LocalizationManager.NotEnoughGold,
            canBuy
        );
    }

    void ShowDetail(Color iconCol, string name, string flavor,
                    string stats, string btnLabel, bool btnEnabled)
    {
        if (emptyHint  != null) emptyHint.SetActive(false);
        if (detailView != null) detailView.SetActive(true);

        if (detailIcon   != null) detailIcon.color  = iconCol;
        if (detailName   != null) detailName.text   = name;
        if (detailFlavor != null) detailFlavor.text = flavor;
        if (detailStats  != null) detailStats.text  = stats;
        if (actionLabel  != null) actionLabel.text  = btnLabel;
        if (actionButton != null) actionButton.interactable = btnEnabled;
    }

    void ShowEmpty()
    {
        if (emptyHint  != null) emptyHint.SetActive(true);
        if (detailView != null) detailView.SetActive(false);
        _selType    = SelType.None;
        _actionHeld = false;
        _holdTimer  = 0f;
        _holdHeld   = 0f;
        _holdFired  = false;
        GameData.Instance.shopSelType = 0;
    }

    // ── Akcja przycisku ───────────────────────────────────────────────────
    public void OnActionClicked()
    {
        switch (_selType)
        {
            case SelType.Ball:  TryBuyBall();  break;
            case SelType.Arena: TryBuyArena(); break;
            case SelType.Pull:  TryBuyPull();  break;
        }
    }

    private void TryBuyBall()
    {
        if (_selCfg == null) return;
        var  tier       = GameData.Instance.CurrentTier;
        int  totalOwned = GameData.Instance.TotalOwnedBalls();
        int  totalMax   = 5 + tier.maxExtraBalls;
        int  price      = GameData.GetBallPrice(_selCfg.ballClass);

        if (totalOwned >= totalMax) { ShowMsg(LocalizationManager.UpgradeArena); return; }
        if (GameData.Instance.gold < price) { ShowMsg(LocalizationManager.NotEnoughGold); return; }

        GameData.Instance.gold -= price;
        GameData.Instance.purchasedBalls.Add(_selCfg.ballClass);
        GameData.Instance.Save();
        AudioController.Instance?.PlayShopBuy();
        ShowMsg(LocalizationManager.BoughtBall(LocalizationManager.GetClassName(_selCfg.ballClass)));
        RefreshTopBar(); BuildLists();
        SelectBall(_selCfg);
    }

    void TryBuyArena()
    {
        var tier = GameData.ArenaTiers[_selTierIdx];
        if (GameData.Instance.gold < tier.upgradeCost) { ShowMsg(LocalizationManager.NotEnoughGold); return; }
        GameData.Instance.gold -= tier.upgradeCost;
        GameData.Instance.arenaTierIndex++;
        GameData.Instance.Save();
        AudioController.Instance?.PlayShopBuy();
        ShowMsg(LocalizationManager.ArenaUpgraded);
        RefreshTopBar();
        BuildUpgradesList();
        if (!GameData.Instance.IsMaxTier)
            SelectArena(GameData.Instance.arenaTierIndex + 1);
        else
            ShowEmpty();
    }

    void TryBuyPull()
    {
        int pullLvl = GameData.Instance.pullUpgradeLevel;
        if (pullLvl >= GameData.PULL_MAX_LEVEL) { ShowMsg(LocalizationManager.MaxUpgrade); return; }
        int cost = GameData.GetPullUpgradeCost(pullLvl);
        if (GameData.Instance.gold < cost) { ShowMsg(LocalizationManager.NotEnoughGold); return; }

        GameData.Instance.gold -= cost;
        GameData.Instance.pullUpgradeLevel++;
        GameData.Instance.Save();
        AudioController.Instance?.PlayShopBuy();
        ShowMsg(LocalizationManager.PullUpgraded);
        RefreshTopBar();
        BuildUpgradesList();

        if (GameData.Instance.pullUpgradeLevel < GameData.PULL_MAX_LEVEL)
            SelectPull();
        else
            ShowEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void RefreshTopBar()
    {
        if (goldText != null) goldText.text = LocalizationManager.GoldPrefix + GameData.Instance.gold;
        if (infoText != null)
            infoText.text = LocalizationManager.ShopInfoLine(
                GameData.Instance.TotalOwnedBalls(),
                LocalizationManager.GetArenaTierName(GameData.Instance.arenaTierIndex));
    }

    void ShowMsg(string msg) { if (infoText != null) infoText.text = msg; }

    string GetStats(ClassConfig cfg)
    {
        return
            LocalizationManager.StatsHP        + cfg.maxHP + "\n" +
            LocalizationManager.StatsSpeed     + cfg.moveSpeed.ToString("F1") + "\n" +
            LocalizationManager.StatsRange     + cfg.attackRange.ToString("F1") + "\n" +
            LocalizationManager.StatsCooldown  + cfg.attackCooldown.ToString("F1") + "s\n" +
            LocalizationManager.StatsCollision + cfg.collisionDamage.ToString("F0");
    }

    // ── Nawigacja ─────────────────────────────────────────────────────────
    public void TryBuyBall(ClassConfig cfg)
    {
        _selCfg  = cfg;
        _selType = SelType.Ball;
        TryBuyBall();
    }

    public void TryUpgradeArena(ArenaTier tier, int cost)
    {
        int idx = System.Array.IndexOf(GameData.ArenaTiers, tier);
        if (idx < 0) return;
        _selTierIdx = idx;
        _selType    = SelType.Arena;
        TryBuyArena();
    }

    public void OnStartGameClicked() => SceneTransition.ExitTo("GameScene");
    public void OnMergeClicked()     => SceneTransition.ExitTo("MergeScene");
    public void OnBackClicked()      => SceneTransition.ExitTo("MainMenu");
    public void OpenSettings()       => SettingsPanel.Open();
}
