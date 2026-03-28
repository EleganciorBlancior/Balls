// ArenaGameManager.cs (v5) – arena przez skalowanie, nie przez zmianę rozmiaru
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ArenaGameManager : MonoBehaviour
{
    [Header("Class Configs – wszystkie 10")]
    public List<ClassConfig> classConfigs;

    [Header("Arena – stały rozmiar fizyczny")]
    public float arenaHalfSize = 9f; // ściany zawsze w tym miejscu

    [Header("Arena – kolory ramek")]
    public Color arenaBackgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
    public Color arenaFrameColor      = new Color(0.70f, 0.50f, 0.10f, 1f);
    public float frameThickness       = 0.4f;

    [Header("Tło")]
    public Sprite backgroundSprite;

    [Header("Mechaniki ściany")]
    public float wallBlastForce   = 12f;
    public float centerPullForce  = 10f;
    public float mechanicCooldown = 8f;

    [Header("UI (opcjonalne)")]
    public TMP_Text   statusText;
    public TMP_Text   goldText;
    public TMP_Text   winnerText;
    public TMP_Text   tierText;         // "Arena: Średnia (M)"
    public GameObject restartPanel;
    public GameObject shopButton;

    // ── Prywatne ─────────────────────────────────────────────────────────────
    private List<BallController> balls = new List<BallController>();
    private int                  aliveCount;
    private PhysicsMaterial2D    bounceMat;
    private float                ballScaleMult;
    private float                mechanicTimer;

    private SpriteRenderer       bgRenderer;

    private void Start()
    {
        if (GameData.Instance == null)
        {
            var gd = new GameObject("GameData"); gd.AddComponent<GameData>();
        }

        var tier      = GameData.Instance.CurrentTier;
        ballScaleMult = tier.ballScaleMultiplier;

        bounceMat = new PhysicsMaterial2D("Bounce") { bounciness = 1f, friction = 0f };

        SetupBackground(tier);
        SetupFrame();
        SetupWalls();
        SpawnBalls();

        if (restartPanel != null) restartPanel.SetActive(false);
        if (shopButton   != null) shopButton.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false);
        if (tierText     != null) tierText.text = "Arena: " + tier.tierName;
    }

    // ── Tło – skaluje się wraz z tierem (efekt większej areny) ──────────────
    void SetupBackground(ArenaTier tier)
    {
        var go = new GameObject("Background");
        go.transform.position = Vector3.zero;

        bgRenderer              = go.AddComponent<SpriteRenderer>();
        bgRenderer.sortingOrder = -2;
        bgRenderer.sprite       = backgroundSprite != null
                                    ? backgroundSprite
                                    : BallArenaUtils.SolidSquareSprite;
        bgRenderer.color        = backgroundSprite != null ? Color.white : arenaBackgroundColor;

        // Skala tła z tieru – daje wrażenie że arena się "powiększa"
        float s = tier.backgroundScale;
        go.transform.localScale = new Vector3(s, s, 1f);
    }

    // ── Ramki ─────────────────────────────────────────────────────────────────
    void SetupFrame()
    {
        float s = arenaHalfSize, t = frameThickness;
        CreateFrameBar("Frame_Top",    new Vector3(0,  s+t*.5f, 0), new Vector3(s*2+t*2, t, 1f));
        CreateFrameBar("Frame_Bottom", new Vector3(0, -s-t*.5f, 0), new Vector3(s*2+t*2, t, 1f));
        CreateFrameBar("Frame_Left",   new Vector3(-s-t*.5f, 0, 0), new Vector3(t, s*2,   1f));
        CreateFrameBar("Frame_Right",  new Vector3( s+t*.5f, 0, 0), new Vector3(t, s*2,   1f));
    }

    void CreateFrameBar(string name, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(name); go.transform.position = pos; go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BallArenaUtils.SolidSquareSprite; sr.color = arenaFrameColor; sr.sortingOrder = -1;
    }

    // ── Ściany – zawsze w tym samym miejscu ──────────────────────────────────
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

    // ── Spawn ──────────────────────────────────────────────────────────────────
    void SpawnBalls()
    {
        balls.Clear();
        float s = arenaHalfSize * 0.75f;
        int num = 1;

        // 5 bazowych (pierwszych 5 klas)
        for (int i = 0; i < 5 && i < classConfigs.Count; i++)
            SpawnOne(classConfigs[i], num++, s, statMult: 1f, goldMult: 1);

        // Scalone kulki (5x statystyki)
        if (GameData.Instance != null)
        {
            foreach (var merged in GameData.Instance.mergedBalls)
            {
                var cfg = classConfigs.Find(c => c.ballClass == merged.ballClass);
                if (cfg != null)
                    SpawnOne(cfg, num++, s,
                             statMult: merged.statMultiplier,
                             goldMult: merged.goldMultiplier,
                             mergeLevel: merged.mergeLevel);
            }

            // Zwykłe dokupione
            foreach (var cls in GameData.Instance.purchasedBalls)
            {
                var cfg = classConfigs.Find(c => c.ballClass == cls);
                if (cfg != null) SpawnOne(cfg, num++, s, statMult: 1f, goldMult: 1);
            }
        }

        aliveCount = balls.Count;
        UpdateUI();
    }

    void SpawnOne(ClassConfig cfg, int number, float s,
                  float statMult, int goldMult, int mergeLevel = 0)
    {
        Vector2 pos = new Vector2(Random.Range(-s, s), Random.Range(-s, s));
        var go = new GameObject("Ball_" + cfg.className + "_" + number);
        go.transform.position = pos;
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<SpriteRenderer>();

        var ball      = go.AddComponent<BallController>();
        ball.Initialize(cfg, ballScaleMult, statMult, goldMult);
        ball.OnDeath += HandleDeath;
        balls.Add(ball);

        AddLabel(go, number, mergeLevel);
    }

    // ── Numer NA kulce + ikona scalenia ──────────────────────────────────────
    void AddLabel(GameObject ballGO, int number, int mergeLevel)
    {
        var label = new GameObject("Label");
        label.transform.SetParent(ballGO.transform);
        label.transform.localPosition = Vector3.zero;
        label.transform.localScale    = Vector3.one * 0.45f;

        var tmp          = label.AddComponent<TMPro.TextMeshPro>();
        string txt       = number.ToString();
        if (mergeLevel > 0) txt += "\n<size=3>★" + (int)Mathf.Pow(5, mergeLevel) + "x</size>";
        tmp.text         = txt;
        tmp.fontSize     = 5f;
        tmp.alignment    = TMPro.TextAlignmentOptions.Center;
        tmp.color        = mergeLevel > 0 ? new Color(1f, 0.9f, 0.2f) : Color.white;
        tmp.fontStyle    = TMPro.FontStyles.Bold;
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
            winnerText.text = survivor != null ? "Wygrał: " + survivor.Config.className + "!" : "Remis!";
            winnerText.gameObject.SetActive(true);
        }
        if (restartPanel != null) restartPanel.SetActive(true);
        if (shopButton   != null) shopButton.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Update()
    {
        UpdateUI();
        mechanicTimer -= Time.deltaTime;
    }

    public void TriggerWallBlast()
    {
        if (mechanicTimer > 0f) return;
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            Vector2 dir = (b.transform.position - Vector3.zero).normalized;
            if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
            b.Rb.AddForce(dir * wallBlastForce, ForceMode2D.Impulse);
        }
        mechanicTimer = mechanicCooldown;
    }

    public void TriggerCenterPull()
    {
        if (mechanicTimer > 0f) return;
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            Vector2 dir = (Vector3.zero - b.transform.position).normalized;
            b.Rb.AddForce(dir * centerPullForce, ForceMode2D.Impulse);
        }
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
