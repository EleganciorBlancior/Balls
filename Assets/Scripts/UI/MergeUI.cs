// MergeUI.cs (v3) – akordeonowa lista (lewo) + panel szczegółów (prawo)
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

    // ── Klasy ─────────────────────────────────────────────────────────────
    [Header("Klasy (16 assetów)")]
    public List<ClassConfig> allClassConfigs;

    // ── Stan ──────────────────────────────────────────────────────────────
    private bool _basicOpen   = false;
    private bool _upgradeOpen = false;
    private bool _ownedOpen   = false;
    private float _bobTimer;

    private enum SelType { None, Basic, Upgrade, OwnedInfo }
    private SelType     _selType      = SelType.None;
    private ClassConfig _selCfg;
    private int         _selFromLevel;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    private void Start()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData"); go.AddComponent<GameData>();
        }
        RefreshTopBar();
        BuildLists();
        ShowEmpty();
        if (basicToggleLabel   != null) basicToggleLabel.text   = LocalizationManager.MergeBasic   + " ▼";
        if (upgradeToggleLabel != null) upgradeToggleLabel.text = LocalizationManager.MergeUpgrade + " ▼";
        if (ownedToggleLabel   != null) ownedToggleLabel.text   = LocalizationManager.OwnedMerged  + " ▼";
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
        // Iterujemy w kolejności allClassConfigs (ta sama co sklep)
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null) continue;
            int owned = GameData.Instance.CountBasicBalls(cfg.ballClass);
            if (owned == 0) continue;
            any = true;

            bool canMerge = owned >= 5;
            string badge  = owned + "/5";
            Color dotCol  = canMerge ? new Color(1f, 0.85f, 0.1f) : cfg.color;

            var row = SpawnRow(basicContent);
            if (row == null) continue;
            row.Setup(dotCol, cfg.className, badge, readyToMerge: canMerge);

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

        // Grupuj po (klasa, poziom)
        var groups = new Dictionary<(BallClass, int), int>();
        foreach (var m in GameData.Instance.mergedBalls)
        {
            var key = (m.ballClass, m.mergeLevel);
            groups[key] = groups.ContainsKey(key) ? groups[key] + 1 : 1;
        }

        bool any = false;
        // Iterujemy w kolejności allClassConfigs, a dla każdej klasy rosnąco po poziomie
        foreach (var cfg in allClassConfigs)
        {
            if (cfg == null) continue;
            // Zbierz poziomy tej klasy i posortuj
            var levels = new List<int>();
            foreach (var key in groups.Keys)
                if (key.Item1 == cfg.ballClass) levels.Add(key.Item2);
            levels.Sort();

            foreach (int level in levels)
            {
                int count = groups[(cfg.ballClass, level)];
                any = true;

                bool canMerge = count >= 5;
                Color dotCol = canMerge ? new Color(1f, 0.85f, 0.1f) : new Color(0.8f, 0.6f, 0.1f);

                var row = SpawnRow(upgradeContent);
                if (row == null) continue;
                row.Setup(dotCol,
                          cfg.className + " " + LocalizationManager.LevelPrefix + level,
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

        if (GameData.Instance.mergedBalls.Count == 0)
        {
            var empty = SpawnRow(ownedContent);
            empty.Setup(Color.gray, LocalizationManager.NoOwnedMerged, "–", false);
        }
        else
        {
            foreach (var merged in GameData.Instance.mergedBalls)
            {
                var cfg = GetCfg(merged.ballClass);
                if (cfg == null) continue;

                var row = SpawnRow(ownedContent);
                if (row == null) continue;
                row.Setup(new Color(1f, 0.85f, 0.1f),
                          LocalizationManager.SuperPrefix + cfg.className,
                          LocalizationManager.LevelPrefix + merged.mergeLevel);

                var capCfg    = cfg;
                var capMerged = merged;
                if (row.btn != null)
                    row.btn.onClick.AddListener(() => SelectOwned(capCfg, capMerged));
            }
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
        if (basicContent != null) basicContent.gameObject.SetActive(_basicOpen);
        if (basicToggleLabel != null)
            basicToggleLabel.text = LocalizationManager.MergeBasic + " " + (_basicOpen ? "▲" : "▼");
    }

    public void ToggleUpgrade()
    {
        _upgradeOpen = !_upgradeOpen;
        if (upgradeContent != null) upgradeContent.gameObject.SetActive(_upgradeOpen);
        if (upgradeToggleLabel != null)
            upgradeToggleLabel.text = LocalizationManager.MergeUpgrade + " " + (_upgradeOpen ? "▲" : "▼");
    }

    public void ToggleOwned()
    {
        _ownedOpen = !_ownedOpen;
        if (ownedContent != null) ownedContent.gameObject.SetActive(_ownedOpen);
        if (ownedToggleLabel != null)
            ownedToggleLabel.text = LocalizationManager.OwnedMerged + " " + (_ownedOpen ? "▲" : "▼");
    }

    // ── Selekcja ──────────────────────────────────────────────────────────
    void SelectBasic(ClassConfig cfg)
    {
        _selType = SelType.Basic;
        _selCfg  = cfg;

        int owned     = GameData.Instance.CountBasicBalls(cfg.ballClass);
        bool canMerge = owned >= 5;

        ShowDetail(
            canMerge ? new Color(1f, 0.85f, 0.1f) : cfg.color,
            cfg.className,
            LocalizationManager.MergeFlavor,
            LocalizationManager.OwnedCount(owned),
            canMerge ? LocalizationManager.MergeBtn : LocalizationManager.BuyMoreBalls,
            canMerge
        );
    }

    void SelectUpgrade(ClassConfig cfg, int fromLevel)
    {
        _selType      = SelType.Upgrade;
        _selCfg       = cfg;
        _selFromLevel = fromLevel;

        int count     = GameData.Instance.CountMergedOfLevel(cfg.ballClass, fromLevel);
        bool canMerge = count >= 5;
        int nextLevel = fromLevel + 1;

        ShowDetail(
            canMerge ? new Color(1f, 0.85f, 0.1f) : new Color(0.8f, 0.6f, 0.1f),
            cfg.className + "  " + LocalizationManager.LevelPrefix + fromLevel + " → " + LocalizationManager.LevelPrefix + nextLevel,
            LocalizationManager.MergeUpgFlavor,
            LocalizationManager.OwnedCountLevel(fromLevel, count),
            canMerge ? LocalizationManager.MergeBtn : LocalizationManager.BuyMoreBalls,
            canMerge
        );
    }

    void SelectOwned(ClassConfig cfg, MergedBallData merged)
    {
        _selType = SelType.OwnedInfo;
        _selCfg  = cfg;

        int  mult      = (int)merged.statMultiplier;
        int  goldMult  = merged.goldMultiplier;

        ShowDetail(
            new Color(1f, 0.85f, 0.1f),
            LocalizationManager.SuperPrefix + cfg.className + "  (" + LocalizationManager.LevelPrefix + merged.mergeLevel + ")",
            LocalizationManager.SuperFlavor,
            LocalizationManager.MergedStatText(mult, goldMult, merged.mergeLevel),
            LocalizationManager.OwnedSingle,
            false
        );
    }

    void ShowDetail(Color iconCol, string name, string flavor,
                    string stats, string btnLabel, bool btnEnabled)
    {
        if (emptyHint != null) emptyHint.SetActive(false);
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
    }

    // ── Akcja przycisku ───────────────────────────────────────────────────
    public void OnActionClicked()
    {
        switch (_selType)
        {
            case SelType.Basic:   TryMergeBasic();   break;
            case SelType.Upgrade: TryMergeUpgrade(); break;
        }
    }

    void TryMergeBasic()
    {
        if (_selCfg == null) return;
        if (!GameData.Instance.CanMergeBasic(_selCfg.ballClass))
        {
            ShowMsg("Masz: " + GameData.Instance.CountBasicBalls(_selCfg.ballClass) + "/5"); return;
        }
        if (GameData.Instance.TryMergeBasic(_selCfg.ballClass))
        {
            ShowMsg("Scalono " + _selCfg.className + " → " + LocalizationManager.LevelPrefix + "1!");
            RefreshTopBar(); BuildLists();
            SelectBasic(_selCfg);
        }
    }

    void TryMergeUpgrade()
    {
        if (_selCfg == null) return;
        if (!GameData.Instance.CanMergeUp(_selCfg.ballClass, _selFromLevel))
        {
            ShowMsg("Masz: " + GameData.Instance.CountMergedOfLevel(_selCfg.ballClass, _selFromLevel) + "/5"); return;
        }
        if (GameData.Instance.TryMergeUp(_selCfg.ballClass, _selFromLevel))
        {
            ShowMsg(_selCfg.className + " " + LocalizationManager.LevelPrefix + _selFromLevel
                    + " → " + LocalizationManager.LevelPrefix + (_selFromLevel + 1) + "!");
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

    // ── Nawigacja ─────────────────────────────────────────────────────────
    public void GoToShop()     => SceneManager.LoadScene("ShopScene");
    public void GoToGame()     => SceneManager.LoadScene("GameScene");
    public void GoToMainMenu() => SceneManager.LoadScene("MainMenu");
    public void OpenSettings() => SettingsPanel.Open();
}
