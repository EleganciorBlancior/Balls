// MergeUI.cs – scena MergeScene
// Gracz może scalić 5 kulek tej samej klasy w 1 super-kulkę (5× statystyki)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MergeUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text   goldText;
    public TMP_Text   infoText;          // komunikaty
    public Transform  mergeContainer;    // Vertical Layout Group + Content Size Fitter
    public GameObject mergeItemPrefab;   // ten sam ShopItem prefab
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
        RefreshUI();
        BuildMergeList();
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

        if (infoText != null)
            infoText.text = "Scal 5 kulek tej samej klasy.\nOtrzymaj 1 super kulkę!";
    }

    void BuildMergeList()
    {
        if (mergeContainer == null || mergeItemPrefab == null) return;
        foreach (Transform child in mergeContainer) Destroy(child.gameObject);

        // Policz ile kulek każdej klasy gracz ma w purchased
        var counts = new Dictionary<BallClass, int>();
        foreach (var cls in GameData.Instance.purchasedBalls)
        {
            if (!counts.ContainsKey(cls)) counts[cls] = 0;
            counts[cls]++;
        }

        bool anyAvailable = false;

        foreach (var cfg in allClassConfigs)
        {
            int owned = counts.ContainsKey(cfg.ballClass) ? counts[cfg.ballClass] : 0;
            if (owned == 0) continue; // nie pokazuj klas których gracz nie ma

            anyAvailable = true;
            var itemGO = Instantiate(mergeItemPrefab, mergeContainer);
            var item   = itemGO.GetComponent<ShopItem>();
            if (item != null)
                item.SetupMerge(cfg, owned, this);
        }

        // Pokaż scalone kulki które gracz już posiada
        if (GameData.Instance.mergedBalls.Count > 0)
        {
            AddHeader("Scalone kulki");
            foreach (var merged in GameData.Instance.mergedBalls)
            {
                var cfg = allClassConfigs.Find(c => c.ballClass == merged.ballClass);
                if (cfg == null) continue;
                var itemGO = Instantiate(mergeItemPrefab, mergeContainer);
                var item   = itemGO.GetComponent<ShopItem>();
                if (item != null)
                    item.SetupMergedInfo(cfg, merged, this);
            }
        }

        if (!anyAvailable && GameData.Instance.mergedBalls.Count == 0)
        {
            if (infoText != null)
                infoText.text = "Masz za mało kulek!";
        }
    }

    void AddHeader(string title)
    {
        var itemGO = Instantiate(mergeItemPrefab, mergeContainer);
        var item   = itemGO.GetComponent<ShopItem>();
        if (item != null) item.SetupHeader(title);
    }

    public void TryMerge(BallClass cls, ClassConfig cfg)
    {
        if (!GameData.Instance.CanMerge(cls))
        {
            int have = GameData.Instance.CountBallsOfClass(cls);
            ShowMessage("Masz: " + have + "/5");
            return;
        }

        bool ok = GameData.Instance.TryMerge(cls);
        if (ok)
        {
            ShowMessage("Scalono " + cfg.className + " → SUPER " + cfg.className + "!\n5x statystyki, 5x złoto za zabicie!");
            RefreshUI();
            BuildMergeList();
        }
    }

    void ShowMessage(string msg) { if (infoText != null) infoText.text = msg; }

    public void GoToShop()    => SceneManager.LoadScene("ShopScene");
    public void GoToGame()    => SceneManager.LoadScene("GameScene");
    public void GoToMainMenu()=> SceneManager.LoadScene("MainMenu");
}
