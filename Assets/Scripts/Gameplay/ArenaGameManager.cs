// ArenaGameManager.cs (v7)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ArenaGameManager : MonoBehaviour
{
    [Header("Class Configs – wszystkie 16")]
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
    public float mechanicCooldown = 8f;

    [Header("Przyspieszenie kulek")]
    public float accelerationStartTime   = 30f;  // po ilu sekundach zaczyna się przyspieszanie
    public float accelerationInterval    = 5f;   // co ile sekund kolejny krok
    [Range(0f, 0.5f)]
    public float accelerationPerStep     = 0.12f; // +12% prędkości każdy krok

    [Header("UI (opcjonalne)")]
    public TMP_Text   statusText;
    public TMP_Text   goldText;
    public TMP_Text   winnerText;
    public TMP_Text   tierText;
    public GameObject restartPanel;
    public GameObject shopButton;

    [Header("Teksty przycisków endpanelu (opcjonalne)")]
    public TMP_Text restartButtonLabel;
    public TMP_Text shopButtonLabel;
    public TMP_Text settingsButtonLabel;
    public TMP_Text quitButtonLabel;

    [Header("Przyciski mechanik (opcjonalne – do odliczania)")]
    public Button wallBlastButton;
    public Button centerPullButton;

    // ── Prywatne ──────────────────────────────────────────────────────────────
    private readonly List<BallController> balls = new List<BallController>();
    private int               aliveCount;

    /// <summary>Współdzielona lista żywych kulek — używana przez BallController.FindNearest().</summary>
    public static List<BallController> AliveBalls { get; private set; } = new List<BallController>();
    private float             ballScaleMult;
    private float             mechanicTimer;

    private float             _gameTimer         = 0f;
    private bool              _accelerating      = false;
    private float             _accelStepTimer    = 0f;

    private bool              _gameEnded         = false;
    private bool              _escPaused         = false;

    // Pull upgrade stats (leniwie ładowane)
    private float _pullForce;
    private float _pullDuration;

    private void Start()
    {
        if (GameData.Instance == null)
        {
            var gd = new GameObject("GameData"); gd.AddComponent<GameData>();
        }

        var tier      = GameData.Instance.CurrentTier;
        ballScaleMult = tier.ballScaleMultiplier; // zostanie przeliczony w SpawnBalls
        int pullLvl   = GameData.Instance.pullUpgradeLevel;
        _pullForce    = GameData.GetPullForce(pullLvl);
        _pullDuration = GameData.GetPullDuration(pullLvl);

        BallController.ArenaHalf = arenaHalfSize;
        SetupBackground(tier);
        SetupFrame();
        SetupWalls();
        SpawnBalls();

        if (restartPanel != null) restartPanel.SetActive(false);
        if (shopButton   != null) shopButton.SetActive(false);
        if (winnerText   != null) winnerText.gameObject.SetActive(false);
        if (tierText     != null) tierText.text = LocalizationManager.ArenaTierLabel(
            LocalizationManager.GetArenaTierName(GameData.Instance.arenaTierIndex));

        // Lokalizacja przycisków endpanelu
        if (restartButtonLabel  != null) restartButtonLabel.text  = LocalizationManager.Play;
        if (shopButtonLabel     != null) shopButtonLabel.text     = LocalizationManager.MainMenuShop;
        if (settingsButtonLabel != null) settingsButtonLabel.text = LocalizationManager.MainMenuSettings;
        if (quitButtonLabel     != null) quitButtonLabel.text     = LocalizationManager.MainMenuQuit;

        if (centerPullButton != null)
        {
            var anim = centerPullButton.GetComponent<ButtonAnimator>();
            if (anim != null) anim.fillDisabled = new Color(0.392f, 0.392f, 0.392f, 0.9f);
        }

        AudioController.Instance?.PlayRoundStart();
    }

    // ── Tło ───────────────────────────────────────────────────────────────────
    void SetupBackground(ArenaTier tier)
    {
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
    // Odbicia obsługuje BallController.FixedUpdate — EdgeCollidery nie są potrzebne
    void SetupWalls() { }

    // ── Spawn ─────────────────────────────────────────────────────────────────
    // Generuje pozycje siatki dla N kulek w obszarze ±s
    Vector2[] BuildGridPositions(int count, float s)
    {
        int cols   = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows   = Mathf.CeilToInt((float)count / cols);
        float stepX = cols > 1 ? s * 2f / (cols - 1) : 0f;
        float stepY = rows > 1 ? s * 2f / (rows - 1) : 0f;
        var positions = new Vector2[count];
        int idx = 0;
        for (int r = 0; r < rows && idx < count; r++)
            for (int c = 0; c < cols && idx < count; c++, idx++)
                positions[idx] = new Vector2(-s + c * stepX, -s + r * stepY)
                               + Random.insideUnitCircle * Mathf.Min(stepX, stepY) * 0.3f;
        return positions;
    }

    void SpawnBalls()
    {
        balls.Clear();
        AliveBalls.Clear();
        float s = arenaHalfSize * 0.82f;
        int num = 1;

        if (GameData.Instance != null)
        {
            // Dynamiczne skalowanie: mało kulek = większe, dużo kulek = minimum tiera
            int   total        = Mathf.Max(GameData.Instance.TotalOwnedBalls(), 1);
            float densityScale = 0.75f * Mathf.Sqrt(123f / total);
            float minScale     = GameData.Instance.CurrentTier.ballScaleMultiplier;
            ballScaleMult      = Mathf.Clamp(densityScale, minScale, 1.0f);

            // Zbierz kolejkę do spawnu
            var spawnQueue = new System.Collections.Generic.List<(ClassConfig cfg, float stat, int gold, int ml)>();
            var baseClasses = new BallClass[]
            {
                BallClass.Warrior, BallClass.Mage, BallClass.Archer,
                BallClass.Rogue,   BallClass.Paladin
            };
            foreach (var cls in baseClasses)
            {
                if (GameData.Instance.consumedBaseBalls.Contains(cls)) continue;
                var cfg = FindCfg(cls);
                if (cfg != null) spawnQueue.Add((cfg, 1f, 1, 0));
            }
            foreach (var merged in GameData.Instance.mergedBalls)
            {
                var cfg = FindCfg(merged.ballClass);
                if (cfg != null) spawnQueue.Add((cfg, merged.statMultiplier, merged.goldMultiplier, merged.mergeLevel));
            }
            foreach (var cls in GameData.Instance.purchasedBalls)
            {
                var cfg = FindCfg(cls);
                if (cfg != null) spawnQueue.Add((cfg, 1f, 1, 0));
            }

            // Generuj siatkę dla całej liczby kulek
            var grid = BuildGridPositions(spawnQueue.Count, s);
            // Przelosuj kolejność żeby klasy się nie skupiały w jednym miejscu
            for (int i = spawnQueue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (grid[i], grid[j]) = (grid[j], grid[i]);
            }

            for (int i = 0; i < spawnQueue.Count; i++)
            {
                var (cfg, stat, gold, ml) = spawnQueue[i];
                SpawnOne(cfg, num++, grid[i], stat, gold, ml);
            }
        }
        else
        {
            var grid = BuildGridPositions(Mathf.Min(5, classConfigs.Count), s);
            for (int i = 0; i < 5 && i < classConfigs.Count; i++)
                SpawnOne(classConfigs[i], num++, grid[i], 1f, 1, 0);
        }

        aliveCount = balls.Count;
        UpdateUI();
    }

    ClassConfig FindCfg(BallClass cls)
        => classConfigs.Find(c => c.ballClass == cls)
        ?? Resources.Load<ClassConfig>("ClassConfigs/" + cls);

    void SpawnOne(ClassConfig cfg, int number, Vector2 pos,
                  float statMult, int goldMult, int mergeLevel)
    {
        var go = new GameObject("Ball_" + cfg.className + "_" + number);
        go.transform.position = pos;
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<SpriteRenderer>();

        var ball = go.AddComponent<BallController>();
        ball.Initialize(cfg, ballScaleMult, statMult, goldMult, null, mergeLevel);
        ball.BallNumber = number;
        ball.OnDeath += HandleDeath;
        balls.Add(ball);
        AliveBalls.Add(ball);

        AddLabel(go, number, mergeLevel);
    }

    void AddLabel(GameObject ballGO, int number, int mergeLevel)
    {
        var label = new GameObject("Label");
        label.transform.SetParent(ballGO.transform);
        label.transform.localPosition = Vector3.zero;
        label.transform.localScale    = Vector3.one * 0.45f;

        var tmp = label.AddComponent<TextMeshPro>();
        tmp.fontSize     = 5f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.outlineWidth = 0.35f;
        tmp.outlineColor = Color.black;
        tmp.sortingOrder = 1;

        if (mergeLevel > 0)
        {
            // Scalone: tylko złota liczba mnożnika
            tmp.text  = ((int)Mathf.Pow(5, mergeLevel)).ToString();
            tmp.color = new Color(1f, 0.9f, 0.2f);
        }
        else
        {
            // Bazowe: biały numer
            tmp.text  = number.ToString();
            tmp.color = Color.white;
        }
    }

    // ── Śmierć / koniec ───────────────────────────────────────────────────────
    void HandleDeath(BallController dead)
    {
        AliveBalls.Remove(dead);
        aliveCount--;
        UpdateUI();
        UpdateDynamicScale();
        if (aliveCount <= 1) EndGame();
    }

    void UpdateDynamicScale()
    {
        // Dynamiczne skalowanie tylko na dwóch największych arenach (tier 4 i 5)
        if (GameData.Instance == null || GameData.Instance.arenaTierIndex < 4) return;

        int alive = AliveBalls.Count;
        if (alive < 50) return;
        float densityMult = 0.75f * Mathf.Sqrt(123f / Mathf.Max(alive, 1));
        float mult        = Mathf.Clamp(densityMult / Mathf.Max(ballScaleMult, 0.001f), 1f, 6f);
        foreach (var b in AliveBalls)
            if (b != null && b.IsAlive) b.SetDynamicScale(mult);
    }

    void EndGame()
    {
        if (_gameEnded) return;
        _gameEnded = true;
        AudioController.Instance?.StopAccelerationWarning();

        BallController survivor = null;
        foreach (var b in balls) if (b.IsAlive) { survivor = b; break; }

        if (winnerText != null)
        {
            winnerText.text = survivor != null
                ? LocalizationManager.Winner(LocalizationManager.GetClassName(survivor.Config.ballClass))
                : LocalizationManager.Draw;
            winnerText.gameObject.SetActive(true);
        }

        if (restartPanel != null) restartPanel.SetActive(true);
        if (shopButton   != null) shopButton.SetActive(true);

        if (survivor != null)
        {
            survivor.Rb.linearVelocity = Vector2.zero;
            survivor.Rb.constraints    = RigidbodyConstraints2D.FreezeAll;
            if (GameData.Instance != null)
                GameData.Instance.RecordWin(survivor.Config.ballClass);
        }

        GameData.Instance?.Save();

        ArenaEvents.FireGameEnd();
        StartCoroutine(FreezeAfterDelay(0.7f));
    }

    void ShowEscPanel()
    {
        _escPaused = true;
        if (winnerText  != null) winnerText.gameObject.SetActive(false);
        if (restartPanel != null) restartPanel.SetActive(true);
        if (shopButton   != null) shopButton.SetActive(true);
    }

    void HideEscPanel()
    {
        _escPaused = false;
        if (restartPanel != null) restartPanel.SetActive(false);
        if (shopButton   != null) shopButton.SetActive(false);
    }

    IEnumerator FreezeAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 0f;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        // ESC – toggle panelu pauzy (tylko gdy gra trwa)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame &&
            !_gameEnded)
        {
            if (_escPaused) HideEscPanel();
            else            ShowEscPanel();
        }

        // Mechanic timer działa zawsze (też gdy ESC panel otwarty)
        mechanicTimer -= Time.deltaTime;
        UpdateMechanicButton(wallBlastButton,  LocalizationManager.BlastBtn);
        UpdateMechanicButton(centerPullButton, LocalizationManager.PullBtn);

        if (_gameEnded) return;

        UpdateUI();
        if (Time.frameCount % 10 == 0) KillOutOfBounds();
        if (_escPaused) return;

        // Przyspieszenie kulek po czasie
        _gameTimer += Time.deltaTime;
        if (!_accelerating && _gameTimer >= accelerationStartTime)
        {
            _accelerating   = true;
            _accelStepTimer = 0f;
            TriggerAccelerationStep();
        }
        else if (_accelerating)
        {
            _accelStepTimer += Time.deltaTime;
            if (_accelStepTimer >= accelerationInterval)
            {
                _accelStepTimer = 0f;
                TriggerAccelerationStep();
            }
        }
    }

    void KillOutOfBounds()
    {
        float limit = arenaHalfSize + 1.5f;
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            var p = b.transform.position;
            if (Mathf.Abs(p.x) > limit || Mathf.Abs(p.y) > limit)
                b.TakeHolyDamage(float.MaxValue, null);
        }
    }

    void TriggerAccelerationStep()
    {
        AudioController.Instance?.PlayAccelerationWarning();
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            b.SpeedMultiplier += accelerationPerStep;
            if (b.Rb.linearVelocity.sqrMagnitude > 0.01f)
                b.Rb.linearVelocity *= (1f + accelerationPerStep);
        }
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

        // Impuls
        foreach (var b in balls)
        {
            if (!b.IsAlive) continue;
            Vector2 dir = -(Vector2)b.transform.position.normalized;
            b.Rb.AddForce(dir * _pullForce, ForceMode2D.Impulse);
        }
        ArenaEvents.FireCenterPull();
        mechanicTimer = mechanicCooldown;

        // Ciągłe przyciąganie jeśli upgradowane
        if (_pullDuration > 0f)
            StartCoroutine(SustainedPull(_pullDuration));
    }

    IEnumerator SustainedPull(float duration)
    {
        float elapsed = 0f;
        float sustainForce = _pullForce * 0.25f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            foreach (var b in balls)
            {
                if (!b.IsAlive) continue;
                Vector2 dir = -(Vector2)b.transform.position.normalized;
                b.Rb.AddForce(dir * sustainForce * Time.deltaTime, ForceMode2D.Force);
            }
        }
    }

    void UpdateUI()
    {
        if (statusText != null) statusText.text = LocalizationManager.AliveCount(aliveCount, balls.Count);
        if (goldText   != null && GameData.Instance != null)
            goldText.text = LocalizationManager.GoldCount(GameData.Instance.gold);
    }

    // ── Ustawienia z areny ────────────────────────────────────────────────────
    public void OpenSettings()
    {
        SettingsPanel.Open(() =>
        {
            if (restartButtonLabel  != null) restartButtonLabel.text  = LocalizationManager.Play;
            if (shopButtonLabel     != null) shopButtonLabel.text     = LocalizationManager.MainMenuShop;
            if (settingsButtonLabel != null) settingsButtonLabel.text = LocalizationManager.MainMenuSettings;
            if (quitButtonLabel     != null) quitButtonLabel.text     = LocalizationManager.MainMenuQuit;
            if (tierText            != null) tierText.text            = LocalizationManager.ArenaTierLabel(
                LocalizationManager.GetArenaTierName(GameData.Instance.arenaTierIndex));
        });
    }

    // ── Nawigacja ─────────────────────────────────────────────────────────────
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
