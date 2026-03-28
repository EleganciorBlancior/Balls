// ShopUI.cs (v6) – akordeonowa lista (lewo) + panel szczegółów (prawo)
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

    // ── Klasy ─────────────────────────────────────────────────────────────
    [Header("Klasy (15 assetów)")]
    public List<ClassConfig> allClassConfigs;

    // ── Stan ──────────────────────────────────────────────────────────────
    private bool _ballsOpen     = false;
    private bool _upgradesOpen  = false;
    private float _bobTimer;

    private enum SelType { None, Ball, Arena }
    private SelType      _selType  = SelType.None;
    private ClassConfig  _selCfg;
    private int          _selTierIdx;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    private void Start()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData"); go.AddComponent<GameData>();
        }
        if (GameData.Instance.gold == 0)
            GameData.Instance.gold = 10000;

        RefreshTopBar();
        BuildLists();
        ShowEmpty();
        if (ballsToggleLabel    != null) ballsToggleLabel.text    = "Kulki ▼";
        if (upgradesToggleLabel != null) upgradesToggleLabel.text = "Ulepszenia ▼";
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
            row.Setup(cfg.color, cfg.className, price + "g");

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
            if (row == null) return;
            row.Setup(new Color(0.8f, 0.6f, 0.1f),
                      "Arena: " + nextTier.tierName,
                      nextTier.upgradeCost + "g");
            if (row.btn != null)
                row.btn.onClick.AddListener(() => SelectArena(nextIdx));
        }
        else
        {
            var row = SpawnRow(upgradesContent);
            if (row != null)
                row.Setup(new Color(0.4f, 0.4f, 0.4f), "Arena (maks.)", "MAX", false);
        }

        if (upgradesContent != null)
            upgradesContent.gameObject.SetActive(_upgradesOpen);
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
    public void ToggleBalls()
    {
        _ballsOpen = !_ballsOpen;
        if (ballsContent != null) ballsContent.gameObject.SetActive(_ballsOpen);
        if (ballsToggleLabel != null)
            ballsToggleLabel.text = "Kulki " + (_ballsOpen ? "▲" : "▼");
    }

    public void ToggleUpgrades()
    {
        _upgradesOpen = !_upgradesOpen;
        if (upgradesContent != null) upgradesContent.gameObject.SetActive(_upgradesOpen);
        if (upgradesToggleLabel != null)
            upgradesToggleLabel.text = "Ulepszenia " + (_upgradesOpen ? "▲" : "▼");
    }

    // ── Selekcja ──────────────────────────────────────────────────────────
    void SelectBall(ClassConfig cfg)
    {
        _selType = SelType.Ball;
        _selCfg  = cfg;

        int  price  = GameData.GetBallPrice(cfg.ballClass);
        var  tier   = GameData.Instance.CurrentTier;
        int  owned  = GameData.Instance.purchasedBalls.Count;
        bool canBuy = GameData.Instance.gold >= price && owned < tier.maxExtraBalls;

        string btnText = canBuy
            ? "KUP  –  " + price + "g"
            : (GameData.Instance.gold < price ? "Za mało złota" : "Pełna arena");

        ShowDetail(
            cfg.color,
            cfg.className,
            GetFlavor(cfg.ballClass),
            GetStats(cfg),
            btnText,
            canBuy
        );
    }

    void SelectArena(int tierIdx)
    {
        _selType    = SelType.Arena;
        _selTierIdx = tierIdx;
        var tier    = GameData.ArenaTiers[tierIdx];
        bool canBuy = GameData.Instance.gold >= tier.upgradeCost;

        ShowDetail(
            new Color(0.8f, 0.6f, 0.1f),
            "Arena: " + tier.tierName,
            "Więcej miejsca. Więcej kulek.",
            "Limit kulek:  " + tier.maxExtraBalls + "\n" +
            "Skala kulek:  x" + tier.ballScaleMultiplier.ToString("F2"),
            canBuy ? "KUP – " +  tier.upgradeCost : "Za mało złota",
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
    }

    // ── Akcja przycisku ───────────────────────────────────────────────────
    public void OnActionClicked()
    {
        switch (_selType)
        {
            case SelType.Ball:  TryBuyBall();  break;
            case SelType.Arena: TryBuyArena(); break;
        }
    }

    private void TryBuyBall()
    {
        if (_selCfg == null) return;
        var  tier   = GameData.Instance.CurrentTier;
        int  owned  = GameData.Instance.purchasedBalls.Count;
        int  price  = GameData.GetBallPrice(_selCfg.ballClass);

        if (owned >= tier.maxExtraBalls) { ShowMsg("Ulepsz arenę!"); return; }
        if (GameData.Instance.gold < price) { ShowMsg("Za mało złota!"); return; }

        GameData.Instance.gold -= price;
        GameData.Instance.purchasedBalls.Add(_selCfg.ballClass);
        ShowMsg("Kupiono " + _selCfg.className + "!");
        RefreshTopBar(); BuildLists();
        SelectBall(_selCfg); // odśwież panel
    }

    void TryBuyArena()
    {
        var tier = GameData.ArenaTiers[_selTierIdx];
        if (GameData.Instance.gold < tier.upgradeCost) { ShowMsg("Za mało złota!"); return; }
        GameData.Instance.gold -= tier.upgradeCost;
        GameData.Instance.arenaTierIndex++;
        ShowMsg("Arena ulepszona!");
        RefreshTopBar();
        BuildUpgradesList();
        // odśwież panel: pokaż kolejny tier lub info o maksie
        if (!GameData.Instance.IsMaxTier)
            SelectArena(GameData.Instance.arenaTierIndex + 1);
        else
            ShowEmpty();
    }


// ── Helpers ───────────────────────────────────────────────────────────
    void RefreshTopBar()
    {
        if (goldText != null) goldText.text = "Złoto: " + GameData.Instance.gold;
        if (infoText != null)
            infoText.text = "Kulki: " + (5 + GameData.Instance.purchasedBalls.Count)
                          + "\nArena: " + GameData.Instance.CurrentTier.tierName;
    }

    void ShowMsg(string msg) { if (infoText != null) infoText.text = msg; }

    string GetStats(ClassConfig cfg)
    {
        return
            "HP: " + cfg.maxHP + "\n" +
            "Szybkość: " + cfg.moveSpeed.ToString("F1") + "\n" +
            "Zasięg: " + cfg.attackRange.ToString("F1") + "\n" +
            "Cooldown: " + cfg.attackCooldown.ToString("F1") + "s\n" +
            "Kolizja: " + cfg.collisionDamage.ToString("F0");
    }

    string GetFlavor(BallClass cls)
    {
        switch (cls)
        {
            case BallClass.Warrior:      return "Jebnie jak Darek Kaśce";
            case BallClass.Mage:         return "Hustler, mefe sprzedaje kilogramami";
            case BallClass.Archer:       return "Ratatata skurwysynu";
            case BallClass.Rogue:        return "Mały ale wariat";
            case BallClass.Paladin:      return "Pierdolony zawadiaka";
            case BallClass.Berserker:    return "Jak ugryzie będzie ślad";
            case BallClass.Necromancer:  return "Pawulonik 5mg";
            case BallClass.Elementalist: return "Spierdolił z monaru";
            case BallClass.Priest:       return "Żył w celibacie, poddał się po 3 dniu";
            case BallClass.Titan:        return "Twoja stara";
            case BallClass.Nerd:         return "Syn koleżanki twojej starej";
            case BallClass.Glitch:       return "Podobno jego prawdziwe imię to feature";
            case BallClass.Druid:        return "Zjednoczył emiraty arabskie";
            case BallClass.Psychic:      return "Pojebał cukier z solą i dosypał cyjanku do kawy";
            case BallClass.Technician:   return "Zdał zawodowe, chłopak twojej żony";
            case BallClass.Mariachi:     return "Wszystko co robi potrafi skończyć, oprócz przyśpiewki";
            default: return "";
        }
    }

    // ── Nawigacja ─────────────────────────────────────────────────────────
    // ── Kompatybilność z ShopItem (stary prefab) ─────────────────────────
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

    public void OnStartGameClicked() => SceneManager.LoadScene("GameScene");
    public void OnMergeClicked()     => SceneManager.LoadScene("MergeScene");
    public void OnBackClicked()      => SceneManager.LoadScene("MainMenu");
}
