// ArenaGameManager.cs (v6)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ArenaGameManager : MonoBehaviour
{
    [Header("Class Configs – wszystkie 15")]
    public List<ClassConfig> classConfigs;

    [Header("Arena – stały rozmiar fizyczny")]
    public float arenaHalfSize        = 9f;

    [Header("Arena – kolory")]
    public Color arenaBackgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
    public Color arenaFrameColor      = new Color(0.70f, 0.50f, 0.10f, 1f);
    public float frameThickness       = 0.4f;

    [Header("Tło (głębokie, za areną)")]
    public Sprite backgroundSprite;

    [Header("Mechaniki ściany")]
    public float wallBlastForce   = 22f;
    public float centerPullForce  = 18f;
    public float mechanicCooldown = 8f;

    [Header("UI (opcjonalne)")]
    public TMP_Text   statusText;
    public TMP_Text   goldText;
    public TMP_Text   winnerText;
    public TMP_Text   tierText;
    public GameObject restartPanel;
    public GameObject shopButton;

    [Header("Przyciski mechanik (opcjonalne – do odliczania)")]
    public Button wallBlastButton;
    public Button centerPullButton;

    // ── Prywatne ──────────────────────────────────────────────────────────────
    private readonly List<BallController> balls = new List<BallController>();
    private int               aliveCount;
    private PhysicsMaterial2D bounceMat;
    private float             ballScaleMult;
    private float             mechanicTimer;

    private void Start()
    {
        if (GameData.Instance == null)
        {
            var gd = new GameObject("GameData"); gd.AddComponent<GameData>();
        }

        var tier      = GameData.Instance.CurrentTier;
        ballScaleMult = tier.ballScaleMultiplier;
        bounceMat     = new PhysicsMaterial2D("Bounce") { bounciness = 1f, friction = 0f };

        SetupBackground(tier);
        SetupFrame();
        SetupWalls();
        SpawnBalls();

        if (restartPanel != null) restartPanel.SetActive(false);
        if (shopButton   != null) shopButton.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false);
        if (tierText     != null) tierText.text = "Arena: " + tier.tierName;

        // Center Pull: kolor po użyciu #646464, alfa 90%
        if (centerPullButton != null)
        {
            var anim = centerPullButton.GetComponent<ButtonAnimator>();
            if (anim != null) anim.fillDisabled = new Color(0.392f, 0.392f, 0.392f, 0.9f);
        }
    }

    // ── Tło ───────────────────────────────────────────────────────────────────
    void SetupBackground(ArenaTier tier)
    {
        // Kolorowe tło areny (tylko obszar areny)
        var fill = new GameObject("ArenaFill");
        fill.transform.position   = Vector3.zero;
        fill.transform.localScale = new Vector3(arenaHalfSize * 2f, arenaHalfSize * 2f, 1f);
        var fillSR = fill.AddComponent<SpriteRenderer>();
        fillSR.sprite       = BallArenaUtils.SolidSquareSprite;
        fillSR.color        = arenaBackgroundColor;
        fillSR.sortingOrder = -15;
    }

    // ── Ramki ─────────────────────────────────────────────────────────────────
    void SetupFrame()
    {
        float s = arenaHalfSize, t = frameThickness;
        CreateFrameBar("Frame_Top",    new Vector3(0,  s+t*.5f, 0), new Vector3(s*2+t*2, t,   1f));
        CreateFrameBar("Frame_Bottom", new Vector3(0, -s-t*.5f, 0), new Vector3(s*2+t*2, t,   1f));
        CreateFrameBar("Frame_Left",   new Vector3(-s-t*.5f, 0, 0), new Vector3(t,       s*2, 1f));
        CreateFrameBar("Frame_Right",  new Vector3( s+t*.5f, 0, 0), new Vector3(t,       s*2, 1f));
    }

    void CreateFrameBar(string name, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(name); go.transform.position = pos; go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BallArenaUtils.SolidSquareSprite; sr.color = arenaFrameColor; sr.sortingOrder = -1;
    }

    // ── Ściany ────────────────────────────────────────────────────────────────
    void SetupWalls()
    {
        float s = arenaHalfSize;
        CreateWall("Top",    new Vector2(-s, s),  new Vector2(s, s));
        CreateWall("Bottom", new Vector2(-s, -s), new Vector2(s, -s));
        CreateWall("Left",   new Vector2(-s, -s), new Vector2(-s, s));
        CreateWall("Right",  new Vector2(s, -s),  new Vector2(s, s));
    }

    void CreateWall(string id, Vector2 a, Vector2 b)
    {
        var go = new GameObject("Wall_" + id); go.transform.parent = transform;
        var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        var ec = go.AddComponent<EdgeCollider2D>();
        ec.SetPoints(new List<Vector2> { a, b });
        ec.sharedMaterial = bounceMat; rb.sharedMaterial = bounceMat;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────
    void SpawnBalls()
    {
        balls.Clear();
        float s = arenaHalfSize * 0.75f;
        int num = 1;

        if (GameData.Instance != null)
        {
            // Bazowe kulki klas startowych (tylko jeśli nie zużyte do scalenia)
            var baseClasses = new BallClass[]
            {
                BallClass.Warrior, BallClass.Mage, BallClass.Archer,
                BallClass.Rogue,   BallClass.Paladin
            };
            foreach (var cls in baseClasses)
            {
                if (GameData.Instance.consumedBaseBalls.Contains(cls)) continue;
                var cfg = FindCfg(cls);
                if (cfg != null) SpawnOne(cfg, num++, s, 1f, 1, 0);
            }

            // Scalone kulki (może być wiele tej samej klasy na różnych poziomach)
            foreach (var merged in GameData.Instance.mergedBalls)
            {
                var cfg = FindCfg(merged.ballClass);
                if (cfg != null)
                    SpawnOne(cfg, num++, s, merged.statMultiplier, merged.goldMultiplier, merged.mergeLevel);
            }

            // Dokupione zwykłe
            foreach (var cls in GameData.Instance.purchasedBalls)
            {
                var cfg = FindCfg(cls);
                if (cfg != null) SpawnOne(cfg, num++, s, 1f, 1, 0);
            }
        }
        else
        {
            for (int i = 0; i < 5 && i < classConfigs.Count; i++)
                SpawnOne(classConfigs[i], num++, s, 1f, 1, 0);
        }

        aliveCount = balls.Count;
        UpdateUI();
    }

    ClassConfig FindCfg(BallClass cls)
        => classConfigs.Find(c => c.ballClass == cls)
        ?? Resources.Load<ClassConfig>("ClassConfigs/" + cls);

    void SpawnOne(ClassConfig cfg, int number, float s,
                  float statMult, int goldMult, int mergeLevel)
    {
        Vector2 pos = new Vector2(Random.Range(-s, s), Random.Range(-s, s));
        var go = new GameObject("Ball_" + cfg.className + "_" + number);
        go.transform.position = pos;
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<SpriteRenderer>();

        var ball = go.AddComponent<BallController>();
        ball.Initialize(cfg, ballScaleMult, statMult, goldMult);
        ball.BallNumber = number;
        ball.OnDeath += HandleDeath;
        balls.Add(ball);

        AddLabel(go, number, mergeLevel);
    }

    void AddLabel(GameObject ballGO, int number, int mergeLevel)
    {
        var label = new GameObject("Label");
        label.transform.SetParent(ballGO.transform);
        label.transform.localPosition = Vector3.zero;
        label.transform.localScale    = Vector3.one * 0.45f;

        var tmp      = label.AddComponent<TextMeshPro>();
        string txt   = number.ToString();
        if (mergeLevel > 0) txt += "\n<size=3>★" + (int)Mathf.Pow(5, mergeLevel) + "x</size>";
        tmp.text         = txt;
        tmp.fontSize     = 5f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.color        = mergeLevel > 0 ? new Color(1f, 0.9f, 0.2f) : Color.white;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.outlineWidth = 0.35f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 1;
    }

    // ── Śmierć / koniec ───────────────────────────────────────────────────────
    void HandleDeath(BallController killer) { aliveCount--; UpdateUI(); if (aliveCount <= 1) EndGame(); }

    void EndGame()
    {
        BallController survivor = null;
        foreach (var b in balls) if (b.IsAlive) { survivor = b; break; }
        if (winnerText != null)
        {
            winnerText.text = survivor != null
                ? "Wygrał: " + survivor.Config.className + "!"
                : "Remis!";
            winnerText.gameObject.SetActive(true);
        }
        if (restartPanel != null) restartPanel.SetActive(true);
        if (shopButton   != null) shopButton.SetActive(true);
        // Natychmiastowe zamrożenie pozycji survivora
        if (survivor != null)
        {
            survivor.Rb.linearVelocity = Vector2.zero;
            survivor.Rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }
        ArenaEvents.FireGameEnd();
        StartCoroutine(FreezeAfterDelay(0.7f));
    }

    System.Collections.IEnumerator FreezeAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 0f;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        UpdateUI();
        mechanicTimer -= Time.deltaTime;

        // Odliczanie na przyciskach
        UpdateMechanicButton(wallBlastButton,   "BLAST");
        UpdateMechanicButton(centerPullButton, "PULL");
    }

    void UpdateMechanicButton(Button btn, string label)
    {
        if (btn == null) return;
        btn.interactable = mechanicTimer <= 0f;
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt == null) return;
        txt.text = mechanicTimer > 0f ? label + "\n" + Mathf.CeilToInt(mechanicTimer) + "s" : label;
    }

    // ── Mechaniki ─────────────────────────────────────────────────────────────
    public void TriggerWallBlast()
    {
        if (mechanicTimer > 0f) return;
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            Vector2 dir = ((Vector2)b.transform.position).normalized;
            if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
            b.Rb.linearVelocity = Vector2.zero;
            b.Rb.AddForce(dir * wallBlastForce, ForceMode2D.Impulse);
        }
        ArenaEvents.FireWallBlast();
        mechanicTimer = mechanicCooldown;
    }

    public void TriggerCenterPull()
    {
        if (mechanicTimer > 0f) return;
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            Vector2 dir = -(Vector2)b.transform.position.normalized;
            b.Rb.AddForce(dir * centerPullForce, ForceMode2D.Impulse);
        }
        ArenaEvents.FireCenterPull();
        mechanicTimer = mechanicCooldown;
    }

    void UpdateUI()
    {
        if (statusText != null) statusText.text = "Żyje: " + aliveCount + " / " + balls.Count;
        if (goldText   != null && GameData.Instance != null) goldText.text = "Złoto: " + GameData.Instance.gold;
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene("GameScene"); }
    public void GoToShop()    { Time.timeScale = 1f; SceneManager.LoadScene("ShopScene"); }
    public void GoToMerge()   { Time.timeScale = 1f; SceneManager.LoadScene("MergeScene"); }
    public void GoToMainMenu(){ Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * arenaHalfSize * 2f);
    }
}
