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

    [Header("Optymalizacja (High Load)")]
    [Tooltip("Maks. liczba kulek z pełną fizyką i AI. Nadmiar staje się ghost ballami.")]
    public int maxRealBalls = 500;

    // ── Prywatne ──────────────────────────────────────────────────────────────
    private readonly List<BallController> balls = new List<BallController>();
    private int               aliveCount;
    private int               _totalSpawned;       // real + ghost przy starcie
    private GhostBallSystem   _ghostSystem;

    /// <summary>Współdzielona lista żywych kulek — używana przez BallController.FindNearest().</summary>
    public static List<BallController> AliveBalls { get; private set; } = new List<BallController>();
    private float             ballScaleMult;
    private float             mechanicTimer;

    private float             _gameTimer         = 0f;
    private bool              _accelerating      = false;
    private float             _accelStepTimer    = 0f;
    private bool              _accelSoundPlayed  = false;
    private float             _battleSoundTimer  = 0f;

    private bool              _gameEnded         = false;
    private bool              _escPaused         = false;
    private bool              _battleStarted     = false;  // false = faza freeze
    private bool              _fastElimTriggered = false;

    // Stopniowe zdejmowanie nieśmiertelności: 800→100 kulek
    private const int CHAMPION_RELEASE_START = 800;
    private const int CHAMPION_RELEASE_END   = 100;
    private bool  _championsReleased  = false;
    private float _releaseTimer       = 0f;

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

        BallController.ArenaHalf    = arenaHalfSize;
        BallController.HighLoadMode = false;
        BallController.AiSkip       = 1;
        MariachWeapon.PrecacheArenaBounds();

        var ghostGO = new GameObject("GhostBallSystem");
        _ghostSystem = ghostGO.AddComponent<GhostBallSystem>();

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

        // Start natychmiast
        _battleStarted = true;
        _ghostSystem?.Unfreeze();
        AudioController.Instance?.PlayRoundStart();
        if (winnerText != null)
        {
            winnerText.text = _totalSpawned.ToString("N0");
            winnerText.gameObject.SetActive(true);
            StartCoroutine(HideTextAfterDelay(1.5f));
        }
    }

    IEnumerator HideTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (winnerText != null) winnerText.gameObject.SetActive(false);
    }

    IEnumerator ShowEndPanelDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (restartPanel != null) restartPanel.SetActive(true);
        if (shopButton   != null) shopButton.SetActive(true);
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

            // ── Zbierz kolejkę do spawnu ──────────────────────────────────────
            var spawnQueue = new List<(ClassConfig cfg, float stat, int gold, int ml)>();
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

            // ── Sortuj po mergeLevel malejąco: najsilniejsze = real, najsłabsze = ghost ──
            spawnQueue.Sort((a, b) => b.ml.CompareTo(a.ml));

            // ── Ghost at 750+ balls ──────────────────────────────────────────
            int cap = maxRealBalls > 0 ? maxRealBalls : 500;
            int realCount, ghostCount;
            if (spawnQueue.Count < 750)
            {
                // Poniżej 750: wszystkie kulki real, zero ghostów
                realCount  = spawnQueue.Count;
                ghostCount = 0;
            }
            else
            {
                realCount  = Mathf.Min(spawnQueue.Count, cap);
                ghostCount = spawnQueue.Count - realCount;
            }

            // HighLoadMode
            bool highLoad = realCount > 200;
            BallController.HighLoadMode = highLoad;
            BallController.AiSkip = realCount <= 100 ? 1
                                  : realCount <= 200 ? 2
                                  : realCount <= 400 ? 3 : 4;
            if (highLoad)
            {
                Physics2D.velocityIterations = 3;
                Physics2D.positionIterations = 2;
                Time.fixedDeltaTime          = 0.033f;
            }

            // ── Tasuj w obrębie real i ghost osobno (żeby klasy się mieszały na arenie) ──
            ShuffleRange(spawnQueue, 0, realCount);
            if (ghostCount > 0) ShuffleRange(spawnQueue, realCount, ghostCount);

            // ── Siatki ────────────────────────────────────────────────────────
            var realGrid  = BuildGridPositions(realCount,  s);
            var ghostGrid = ghostCount > 0 ? BuildGridPositions(ghostCount, s) : null;

            _championsReleased = realCount < 200; // małe rundy nie mają tarczy → od razu "released"
            for (int i = 0; i < realCount; i++)
            {
                var (cfg, stat, gold, ml) = spawnQueue[i];
                SpawnOne(cfg, num++, realGrid[i], stat, gold, ml);
            }
            // Nieśmiertelność dla wszystkich kulek przy dużych rundach (≥200 real)
            if (realCount >= 200)
            {
                foreach (var b in balls) b.ChampionShield = true;
            }

            // ── Spawnuj ghost balle ──────────────────────────────────────────
            if (ghostCount > 0)
            {
                // Zbierz klasy posiadane przez gracza + ile kulek per klasa
                var ownedCounts = new Dictionary<BallClass, int>();
                foreach (var (cfg, _, _, _) in spawnQueue)
                {
                    if (ownedCounts.ContainsKey(cfg.ballClass)) ownedCounts[cfg.ballClass]++;
                    else ownedCounts[cfg.ballClass] = 1;
                }
                var ownedConfigs = new List<ClassConfig>();
                foreach (var cls in ownedCounts.Keys)
                {
                    var cfg = FindCfg(cls);
                    if (cfg != null) ownedConfigs.Add(cfg);
                }
                _ghostSystem.Init(ownedConfigs, ownedCounts, ghostCount, realCount);
                for (int i = 0; i < ghostCount; i++)
                {
                    var (cfg, stat, gold, ml) = spawnQueue[realCount + i];
                    Color col = cfg.color;
                    var custom = GameData.Instance?.GetCustomization(cfg.ballClass);
                    if (custom != null) col = custom.color1;
                    float diam  = Mathf.Max(cfg.radius * 2f * ballScaleMult, 0.05f);
                    // Stała absolutna prędkość — nie dzielimy przez ballScaleMult bo przy małych kulkach
                    // daje ogromne wartości i efekt "TV static / mrówki"
                    float speed = Mathf.Clamp(cfg.moveSpeed * 2f, 6f, 12f);
                    _ghostSystem.AddGhost(ghostGrid[i], col, diam, speed, cfg.ballClass,
                                          realCount + i + 1, cfg, stat, gold, ml);
                }
            }
        }
        else
        {
            var grid = BuildGridPositions(Mathf.Min(5, classConfigs.Count), s);
            for (int i = 0; i < 5 && i < classConfigs.Count; i++)
                SpawnOne(classConfigs[i], num++, grid[i], 1f, 1, 0);
        }

        aliveCount       = balls.Count;
        _totalSpawned    = balls.Count + _ghostSystem.Count;
        _nextBallNumber  = _totalSpawned;
        BallController.VfxChance       = 1f;
        BallController.VfxAttackChance = 1f;
        BallController.VfxHitCount     = 15;
        BallController.VfxDeathCount   = 50;
        BallController.VfxScale        = ballScaleMult;
        UpdateUI();
    }

    static void ShuffleRange<T>(List<T> list, int start, int count)
    {
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[start + i]; list[start + i] = list[start + j]; list[start + j] = tmp;
        }
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

        if (!BallController.HighLoadMode) AddLabel(go, number, mergeLevel);
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
    private int _nextBallNumber = 1;
    private int _pendingPromotions = 0; // ile ghostów do zamiany w real (deferred)

    void HandleDeath(BallController dead)
    {
        // NIE usuwaj z AliveBalls tutaj — bronie mogą iterować listę w tym momencie.
        // Cleanup w Update(). Bronie i tak sprawdzają IsAlive.
        aliveCount--;

        // Nie promuj od razu — zrobimy to w Update() żeby nie modyfikować AliveBalls
        // podczas foreach w broni (AoE itp.)
        _pendingPromotions++;

        // Gdy zostaje ≤200 real ballów i ghosty, przyspiesz eliminację
        if (!_fastElimTriggered && aliveCount <= 200 && _ghostSystem != null && _ghostSystem.Count > 0)
        {
            _fastElimTriggered = true;
            _ghostSystem.SetFastElimination();
        }

        UpdateUI();
        if (aliveCount <= 1 && (_ghostSystem == null || _ghostSystem.Count == 0)) EndGame();
    }

    /// <summary>Przetwarzaj odłożone promocje (max 2/klatkę żeby nie ścinać).</summary>
    void ProcessPendingPromotions()
    {
        if (_pendingPromotions <= 0 || _ghostSystem == null || _ghostSystem.Count == 0) return;

        // Promuj gdy łączna liczba kulek (real+ghost) ≤ 1000
        int visualAlive = aliveCount + _ghostSystem.Count;
        if (visualAlive > 1000) { _pendingPromotions = 0; return; }

        int toProcess = Mathf.Min(_pendingPromotions, 2);
        _pendingPromotions -= toProcess;

        for (int i = 0; i < toProcess; i++)
        {
            if (_ghostSystem.Count == 0) break;
            var promo = _ghostSystem.PromoteOne();
            if (promo.HasValue) PromoteGhostToReal(promo.Value);
        }
    }

    void PromoteGhostToReal(GhostPromoteData d)
    {
        _nextBallNumber++;
        var go = new GameObject("Ball_" + d.cfg.className + "_P" + _nextBallNumber);
        go.transform.position = d.pos;
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<SpriteRenderer>();

        var ball = go.AddComponent<BallController>();
        ball.Initialize(d.cfg, ballScaleMult, d.statMult, d.goldMult, null, d.mergeLevel);
        ball.BallNumber = _nextBallNumber;
        ball.OnDeath += HandleDeath;
        balls.Add(ball);
        AliveBalls.Add(ball);
        aliveCount++;
    }

    // ── Płynne skalowanie kulek ────────────────────────────────────────────
    // Target mult zależy od visualAlive, _currentDynMult Lerpuje w stronę targetu.
    private float _currentDynMult = 1f;
    private float _targetDynMult  = 1f;

    void UpdateScaleAndVfx()
    {
        int visualAlive = aliveCount + (_ghostSystem != null ? _ghostSystem.Count : 0);

        // ── VFX throttle: 500→200 kulki throttle, poza tym pełne ──
        if (visualAlive > 500)
        {
            BallController.VfxChance = 1f; BallController.VfxAttackChance = 1f;
            BallController.VfxHitCount = 15; BallController.VfxDeathCount = 50;
        }
        else if (visualAlive > 200)
        {
            float th = Mathf.InverseLerp(200f, 500f, visualAlive);
            BallController.VfxChance       = Mathf.Lerp(0.35f, 1f, th);
            BallController.VfxAttackChance = Mathf.Lerp(0.4f,  1f, th);
            BallController.VfxHitCount     = (int)Mathf.Lerp(5f, 15f, th);
            BallController.VfxDeathCount   = (int)Mathf.Lerp(15f, 50f, th);
        }
        else
        {
            BallController.VfxChance = 1f; BallController.VfxAttackChance = 1f;
            BallController.VfxHitCount = 15; BallController.VfxDeathCount = 50;
        }

        // ── Stopniowe zdejmowanie nieśmiertelności (800→100 kulek) ──────────
        if (!_championsReleased && visualAlive <= CHAMPION_RELEASE_START)
        {
            _releaseTimer += Time.deltaTime;
            if (_releaseTimer >= 0.2f)
            {
                _releaseTimer = 0f;
                int toRelease = 40;
                int released = 0;
                for (int attempt = 0; attempt < aliveCount * 2 && released < toRelease; attempt++)
                {
                    var b = balls[Random.Range(0, balls.Count)];
                    if (b != null && b.IsAlive && b.ChampionShield)
                    {
                        b.ChampionShield = false;
                        released++;
                    }
                }
            }

            if (visualAlive <= CHAMPION_RELEASE_END)
            {
                _championsReleased = true;
                foreach (var b in balls)
                    if (b != null) b.ChampionShield = false;
            }
        }

        // ── Dynamiczna adaptacja AI/fizyki ───────────────────────────────────
        BallController.AiSkip = aliveCount <= 100 ? 1
                              : aliveCount <= 200 ? 2
                              : aliveCount <= 400 ? 3 : 4;
        // HighLoadMode wyłącza się dopiero gdy łączna ilość kulek (real+ghost) ≤ 300
        if (visualAlive <= 300) BallController.HighLoadMode = false;

        // ── Płynne skalowanie kulek ─────────────────────────────────────────
        // fullMult = rozmiar w którym kulki mają normalną wielkość
        float fullMult = 1f / Mathf.Max(ballScaleMult, 0.001f);
        // t: 0 przy dużej ilości kulek, 1 przy małej — ciągła interpolacja
        float t = Mathf.InverseLerp(500f, 50f, visualAlive);
        _targetDynMult = Mathf.Lerp(1f, fullMult, t);

        // Płynny Lerp aktualnego mnożnika w stronę targetu
        _currentDynMult = Mathf.Lerp(_currentDynMult, _targetDynMult, Time.deltaTime * 2f);

        // VfxScale: rzeczywisty rozmiar kulki = ballScaleMult * _currentDynMult
        // Clamp żeby particly były zawsze widoczne (min 20% normalnego rozmiaru)
        BallController.VfxScale = Mathf.Clamp(ballScaleMult * _currentDynMult, 0.2f, 1.5f);

        // Aplikuj do kulek (co 5 klatek żeby nie mielić co klatkę)
        if (Time.frameCount % 5 == 0)
        {
            foreach (var b in AliveBalls)
                if (b != null && b.IsAlive) b.SetDynamicScale(_currentDynMult);
            _ghostSystem?.SetDynamicMult(_currentDynMult);
        }
    }

    void EndGame()
    {
        if (_gameEnded) return;
        _gameEnded = true;
        AudioController.Instance?.StopAccelerationWarning();

        // Usuń wszystkie ghost balle — game over, liczy się tylko winner
        _ghostSystem?.Clear();

        BallController survivor = null;
        foreach (var b in balls) if (b.IsAlive) { survivor = b; break; }

        if (winnerText != null)
        {
            winnerText.text = survivor != null
                ? LocalizationManager.Winner(LocalizationManager.GetClassName(survivor.Config.ballClass))
                : LocalizationManager.Draw;
            winnerText.gameObject.SetActive(true);
        }

        if (survivor != null)
        {
            survivor.Rb.linearVelocity = Vector2.zero;
            survivor.Rb.constraints    = RigidbodyConstraints2D.FreezeAll;
            if (GameData.Instance != null)
                GameData.Instance.RecordWin(survivor.Config.ballClass);
        }

        GameData.Instance?.Save();

        ArenaEvents.FireGameEnd();
        // Panel z przyciskami z opóźnieniem — żeby gracz nie kliknął "Shop" trzymając przycisk mechaniki
        StartCoroutine(ShowEndPanelDelayed(0.5f));
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
        UpdateScaleAndVfx();

        // Ghosty eliminują się same — sprawdzaj czy gra nie powinna się zakończyć
        if (!_gameEnded && aliveCount <= 1 && (_ghostSystem == null || _ghostSystem.Count == 0))
            EndGame();

        if (!_battleStarted) return;  // faza freeze — nie ruszaj nic

        // Cleanup martwych kulek z AliveBalls (odłożone z HandleDeath żeby nie modyfikować
        // listy podczas foreach w broniach)
        AliveBalls.RemoveAll(b => b == null || !b.IsAlive);

        ProcessPendingPromotions();

        if (Time.frameCount % 10 == 0) KillOutOfBounds();
        SimulateBattleSounds();
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

    /// <summary>Symuluje dźwięki walki proporcjonalnie do liczby kulek na arenie.
    /// Działa do 500 kulek — robi gracza że bitwa trwa nawet bez widocznych efektów.</summary>
    void SimulateBattleSounds()
    {
        int visualAlive = aliveCount + (_ghostSystem != null ? _ghostSystem.Count : 0);
        if (visualAlive <= 0) return;

        // Im więcej kulek, tym częściej gramy dźwięk (max ~8/s, min ~1/s przy małej ilości)
        float interval;
        if      (visualAlive > 2000) interval = 0.08f;
        else if (visualAlive > 1000) interval = 0.10f;
        else if (visualAlive > 500)  interval = 0.13f;
        else if (visualAlive > 200)  interval = 0.18f;
        else                         return; // przy <200 dźwięki grają naturalnie z realnych kulek

        _battleSoundTimer += Time.deltaTime;
        if (_battleSoundTimer >= interval)
        {
            _battleSoundTimer = 0f;
            AudioController.Instance?.PlayBattleSound();
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
        if (!_accelSoundPlayed)
        {
            _accelSoundPlayed = true;
            AudioController.Instance?.PlayAccelerationWarning();
        }
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
        _ghostSystem?.ApplyCenterPull(_pullForce * 0.5f);
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
        // Pokaż total wizualnych kulek (real + ghost) żeby gracz widział właściwy licznik
        int visualAlive = aliveCount + (_ghostSystem != null ? _ghostSystem.Count : 0);
        if (statusText != null) statusText.text = LocalizationManager.AliveCount(visualAlive, _totalSpawned > 0 ? _totalSpawned : balls.Count);
        if (goldText != null)
        {
            int ghostCount = _ghostSystem != null ? _ghostSystem.Count : 0;
            goldText.text = $"Gold: {GameData.Instance.gold}";
        }
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
    void ResetPhysicsSettings()
    {
        Physics2D.velocityIterations = 8;
        Physics2D.positionIterations = 3;
        Time.fixedDeltaTime          = 0.02f;
    }

    public void RestartGame() { CleanupBeforeExit(); SceneManager.LoadScene("GameScene"); }
    public void GoToShop()    { CleanupBeforeExit(); SceneManager.LoadScene("ShopScene"); }
    public void GoToMerge()   { CleanupBeforeExit(); SceneManager.LoadScene("MergeScene"); }
    public void GoToMainMenu(){ CleanupBeforeExit(); SceneManager.LoadScene("MainMenu"); }

    void CleanupBeforeExit()
    {
        ResetPhysicsSettings();
        Time.timeScale = 1f;
        AudioController.Instance?.StopAccelerationWarning();
        BallController.VfxChance       = 1f;
        BallController.VfxAttackChance = 1f;
        BallController.VfxHitCount     = 12;
        BallController.VfxDeathCount   = 40;
        BallController.VfxScale        = 1f;
        BallController.HighLoadMode    = false;
        BallController.AiSkip          = 1;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.4f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * arenaHalfSize * 2f);
    }
}
