// ShopUI.cs (v5)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text   goldText;
    public TMP_Text   infoText;
    public TMP_Text   arenaInfoText;
    public Transform  shopItemContainer;
    public GameObject shopItemPrefab;
    public Button     startGameButton;
    public RectTransform titleRect;

    private float titleBobTimer = 0f;

    [Header("Klasy (10 assetów)")]
    public List<ClassConfig> allClassConfigs;

    private void Start()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData"); go.AddComponent<GameData>();
        }
        if (GameData.Instance.gold == 0)
            GameData.Instance.gold = 100;

        RefreshUI();
        BuildShopItems();
    }

    private void Update()
    {
        // Delikatne bujanie tytułu
        if (titleRect != null)
        {
            titleBobTimer += Time.deltaTime;
            titleRect.anchoredPosition = new Vector2(
                titleRect.anchoredPosition.x,
                (Mathf.Sin(titleBobTimer * 1.5f) * 16f) - 55);
        }
    }

    void RefreshUI()
    {
        if (goldText != null) goldText.text = "Złoto: " + GameData.Instance.gold;

        var tier  = GameData.Instance.CurrentTier;
        int owned = GameData.Instance.purchasedBalls.Count;

        if (infoText != null)
            infoText.text = "Kulki: " + (5 + owned);

        if (arenaInfoText != null)
            arenaInfoText.text = "Arena: " + tier.tierName;
    }

    void BuildShopItems()
    {
        if (shopItemContainer == null || shopItemPrefab == null) return;
        foreach (Transform child in shopItemContainer) Destroy(child.gameObject);

        AddHeader("Kulki");
        foreach (var cfg in allClassConfigs)
        {
            var itemGO = Instantiate(shopItemPrefab, shopItemContainer);
            var item   = itemGO.GetComponent<ShopItem>();
            if (item != null) item.SetupBall(cfg, this);
        }

        AddHeader("Ulepszenie areny");
        if (!GameData.Instance.IsMaxTier)
        {
            int nextIdx  = GameData.Instance.arenaTierIndex + 1;
            var nextTier = GameData.ArenaTiers[nextIdx];
            var itemGO   = Instantiate(shopItemPrefab, shopItemContainer);
            var item     = itemGO.GetComponent<ShopItem>();
            if (item != null) item.SetupArena(nextTier, this);
        }
        else AddHeader("Maks. rozmiar areny!");
    }

    void AddHeader(string title)
    {
        var itemGO = Instantiate(shopItemPrefab, shopItemContainer);
        var item   = itemGO.GetComponent<ShopItem>();
        if (item != null) item.SetupHeader(title);
    }

    // ── Akcje ─────────────────────────────────────────────────────────────────
    public void TryBuyBall(ClassConfig cfg)
    {
        var tier  = GameData.Instance.CurrentTier;
        int owned = GameData.Instance.purchasedBalls.Count;
        int price = GameData.GetBallPrice(cfg.ballClass);

        if (owned >= tier.maxExtraBalls)
        { ShowMessage("Ulepsz arenę!"); return; }

        if (GameData.Instance.gold < price)
        { ShowMessage("Za mało złota!"); return; }

        GameData.Instance.gold -= price;
        GameData.Instance.purchasedBalls.Add(cfg.ballClass);
        ShowMessage("Kupiono!");
        RefreshUI(); BuildShopItems();
    }

    public void TryUpgradeArena(ArenaTier nextTier, int cost)
    {
        if (GameData.Instance.gold < cost)
        { ShowMessage("Za mało złota!"); return; }

        GameData.Instance.gold -= cost;
        GameData.Instance.arenaTierIndex++;
        ShowMessage("Arena ulepszona!");
        RefreshUI(); BuildShopItems();
    }

    void ShowMessage(string msg) { if (infoText != null) infoText.text = msg; }

    public void OnStartGameClicked() => SceneManager.LoadScene("GameScene");
    public void OnMergeClicked()     => SceneManager.LoadScene("MergeScene");
    public void OnBackClicked()      => SceneManager.LoadScene("MainMenu");
}
