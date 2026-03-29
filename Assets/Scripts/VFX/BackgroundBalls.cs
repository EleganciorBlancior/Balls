// BackgroundBalls.cs – animowane kulki w tle
// Tryby:
//   Bouncing   – kulki odbijające się od krawędzi (menu)
//   Diagonal   – kulki lecące ukosem z wraparoundem (menu)
//   Orbital    – kulki krążące wokół środka (menu)
//   PaintShop  – żywe kulki z cykliczną paletą farb, część powoli spływa (malarnia)
//   Arena      – subtelne kulki w marginesach areny, reagują na zdarzenia walki
//
// Podepnij na pustym GameObject w dowolnej scenie.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundBalls : MonoBehaviour
{
    public enum Mode { Bouncing, Diagonal, Orbital, PaintShop, Arena }

    [Header("Tryb")]
    public Mode mode = Mode.Bouncing;

    [Header("Ustawienia ogólne")]
    public int   ballCount  = 12;
    public float minRadius  = 0.18f;
    public float maxRadius  = 0.45f;
    public float minSpeed   = 1.2f;
    public float maxSpeed   = 3.0f;

    [Header("Arena – promień areny (tylko tryb Arena)")]
    public float arenaHalfSize = 9f;

    // ─── Wewnętrzna klasa kulki ────────────────────────────────────────────────
    private class BGBall
    {
        public GameObject     go;
        public SpriteRenderer sr;

        // Ruch
        public Vector2 vel;
        public Vector2 baseVel;      // prędkość po zakończeniu efektu mechaniki
        public float   orbitAngle, orbitRadius, orbitSpeed;

        // Kolor / alfa
        public float alphaPulseOffset;
        public Color baseColor;

        // Kolor HSV cykliczny (PaintShop, Arena)
        public float hue;
        public float hueSpeed;       // zmiana hue na sekundę
        public bool  useHueCycle;

        // Skala
        public float baseScale;      // nominalna skala, do pulsacji

        // Spływanie (PaintShop)
        public bool  isDrifter;      // kulka powoli opada w dół
        public float driftSpeedY;

        // Czas życia (dla burst-kulek w trybie Arena)
        public bool  isTemporary;
    }

    private readonly List<BGBall> _balls = new List<BGBall>();
    private Camera _cam;
    private float  _hw, _hh;

    // ─── Paleta trybów menu ────────────────────────────────────────────────────
    static readonly Color[] PALETTE =
    {
        new Color(0.85f, 0.15f, 0.10f, 0.35f),
        new Color(0.20f, 0.40f, 0.95f, 0.35f),
        new Color(0.15f, 0.80f, 0.30f, 0.35f),
        new Color(0.55f, 0.10f, 0.70f, 0.35f),
        new Color(0.95f, 0.85f, 0.15f, 0.35f),
        new Color(0.90f, 0.30f, 0.05f, 0.35f),
        new Color(0.45f, 0.05f, 0.55f, 0.35f),
        new Color(0.10f, 0.75f, 0.90f, 0.35f),
    };

    // Hue farb dla trybów PaintShop/Arena (równomiernie rozłożone)
    static readonly float[] PAINT_HUES =
    {
        0.00f, 0.08f, 0.15f, 0.22f, 0.33f,
        0.45f, 0.55f, 0.65f, 0.75f, 0.88f,
    };

    // ─── Cykl życia ───────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (mode == Mode.Arena) SubscribeEvents();
    }

    void OnDisable()
    {
        if (mode == Mode.Arena) UnsubscribeEvents();
    }

    void SubscribeEvents()
    {
        ArenaEvents.OnBallDied   += HandleBallDied;
        ArenaEvents.OnAoEFired   += HandleAoEFired;
        ArenaEvents.OnWallBlast  += HandleWallBlast;
        ArenaEvents.OnCenterPull += HandleCenterPull;
        ArenaEvents.OnGameEnd    += HandleGameEnd;
    }

    void UnsubscribeEvents()
    {
        ArenaEvents.OnBallDied   -= HandleBallDied;
        ArenaEvents.OnAoEFired   -= HandleAoEFired;
        ArenaEvents.OnWallBlast  -= HandleWallBlast;
        ArenaEvents.OnCenterPull -= HandleCenterPull;
        ArenaEvents.OnGameEnd    -= HandleGameEnd;
    }

    void Start()
    {
        _cam = Camera.main;
        RefreshBounds();

        int count = (mode == Mode.PaintShop) ? Mathf.Max(ballCount, 22)
                  : (mode == Mode.Arena)     ? Mathf.Max(ballCount, 20)
                  : ballCount;

        for (int i = 0; i < count; i++)
            _balls.Add(CreateBall(i));
    }

    void RefreshBounds()
    {
        if (_cam == null) return;
        _hh = _cam.orthographicSize;
        _hw = _hh * _cam.aspect;
    }

    // ─── Tworzenie kulek ──────────────────────────────────────────────────────

    BGBall CreateBall(int index)
    {
        switch (mode)
        {
            case Mode.PaintShop: return CreatePaintBall(index);
            case Mode.Arena:     return CreateArenaBall(index);
            default:             return CreateMenuBall(index);
        }
    }

    BGBall CreateMenuBall(int index)
    {
        var b  = new BGBall();
        float r = Random.Range(minRadius, maxRadius);
        b.baseColor = PALETTE[index % PALETTE.Length];
        b.alphaPulseOffset = Random.Range(0f, Mathf.PI * 2f);
        b.baseScale = r;

        b.go = new GameObject("BGBall_" + index);
        b.go.transform.SetParent(transform);
        b.sr = b.go.AddComponent<SpriteRenderer>();
        b.sr.sprite       = BallArenaUtils.CircleSprite;
        b.sr.color        = b.baseColor;
        b.sr.sortingOrder = -10;
        b.go.transform.localScale = Vector3.one * r * 2f;

        if (mode == Mode.Bouncing)
        {
            b.go.transform.position = new Vector3(
                Random.Range(-_hw, _hw), Random.Range(-_hh, _hh), 0f);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(minSpeed, maxSpeed);
            b.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            b.baseVel = b.vel;
        }
        else if (mode == Mode.Diagonal)
        {
            b.go.transform.position = new Vector3(
                Random.Range(-_hw, _hw), Random.Range(-_hh, _hh), 0f);
            float ds = Random.Range(minSpeed, maxSpeed);
            b.vel = new Vector2(ds, -ds * Random.Range(0.6f, 1.0f));
            b.baseVel = b.vel;
        }
        else // Orbital
        {
            b.orbitRadius = Random.Range(_hw * 0.15f, _hw * 0.75f);
            b.orbitAngle  = Random.Range(0f, Mathf.PI * 2f);
            b.orbitSpeed  = Random.Range(minSpeed, maxSpeed) * (Random.value > 0.5f ? 1f : -1f) * 0.4f;
        }
        return b;
    }

    BGBall CreatePaintBall(int index)
    {
        var b = new BGBall();
        b.hue          = PAINT_HUES[index % PAINT_HUES.Length];
        b.hueSpeed     = Random.Range(-0.018f, 0.018f);
        b.useHueCycle  = true;
        b.alphaPulseOffset = Random.Range(0f, Mathf.PI * 2f);

        float r = Random.Range(
            Mathf.Max(minRadius, 0.22f),
            Mathf.Min(maxRadius * 1.6f, 1.3f));
        b.baseScale = r;

        b.go = new GameObject("PaintBall_" + index);
        b.go.transform.SetParent(transform);
        b.sr = b.go.AddComponent<SpriteRenderer>();
        b.sr.sprite       = BallArenaUtils.CircleSprite;
        b.sr.sortingOrder = -10 + (index % 4);
        b.go.transform.localScale = Vector3.one * r * 2f;
        b.go.transform.position   = new Vector3(
            Random.Range(-_hw, _hw), Random.Range(-_hh, _hh), 0f);

        // Co trzecia kulka powoli spływa (efekt kapania farby – nadal kulka)
        if (index % 3 == 0)
        {
            b.isDrifter    = true;
            b.driftSpeedY  = Random.Range(-0.22f, -0.55f);
        }
        else
        {
            // Pozostałe leniwie się unoszą / odbijają
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(0.3f, 0.9f);
            b.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            b.baseVel = b.vel;
        }
        return b;
    }

    BGBall CreateArenaBall(int index)
    {
        var b = new BGBall();
        b.hue          = PAINT_HUES[index % PAINT_HUES.Length];
        b.hueSpeed     = Random.Range(-0.008f, 0.008f);
        b.useHueCycle  = true;
        b.alphaPulseOffset = Random.Range(0f, Mathf.PI * 2f);

        float r = Random.Range(0.06f, 0.20f);
        b.baseScale = r;

        b.go = new GameObject("ArenaBG_" + index);
        b.go.transform.SetParent(transform);
        b.sr = b.go.AddComponent<SpriteRenderer>();
        b.sr.sprite       = BallArenaUtils.CircleSprite;
        b.sr.sortingOrder = -18;
        b.go.transform.localScale = Vector3.one * r * 2f;
        b.go.transform.position   = RandomMarginPos();

        float ang = Random.Range(0f, Mathf.PI * 2f);
        float spd = Random.Range(0.20f, 0.65f);
        b.vel     = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
        b.baseVel = b.vel;
        return b;
    }

    Vector3 RandomMarginPos()
    {
        float hw = Mathf.Max(_hw, arenaHalfSize + 0.5f);
        float hh = Mathf.Max(_hh, arenaHalfSize + 0.5f);
        float margin = arenaHalfSize + 0.5f;
        switch (Random.Range(0, 4))
        {
            case 0: return new Vector3(Random.Range(-hw, hw),       Random.Range(margin, hh),   0f);
            case 1: return new Vector3(Random.Range(-hw, hw),       Random.Range(-hh, -margin), 0f);
            case 2: return new Vector3(Random.Range(margin, hw),    Random.Range(-hh, hh),      0f);
            default:return new Vector3(Random.Range(-hw, -margin),  Random.Range(-hh, hh),      0f);
        }
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        RefreshBounds();
        // Arena mode używa unscaledTime żeby działać nawet gdy Time.timeScale = 0
        float t  = mode == Mode.Arena ? Time.unscaledTime  : Time.time;
        float dt = mode == Mode.Arena ? Time.unscaledDeltaTime : Time.deltaTime;

        foreach (var b in _balls)
        {
            if (b.go == null || b.sr == null) continue;

            // Kolor
            if (b.useHueCycle)
            {
                b.hue = (b.hue + b.hueSpeed * dt) % 1f;
                if (b.hue < 0f) b.hue += 1f;
                UpdateHueBall(b, t);
            }
            else
            {
                float alpha = 0.18f + Mathf.Sin(t * 1.2f + b.alphaPulseOffset) * 0.12f;
                b.sr.color = new Color(b.baseColor.r, b.baseColor.g, b.baseColor.b, alpha);
            }

            // Pulsacja skali (PaintShop)
            if (mode == Mode.PaintShop)
            {
                float pulse = 1f + Mathf.Sin(t * 1.05f + b.alphaPulseOffset) * 0.065f;
                b.go.transform.localScale = Vector3.one * b.baseScale * 2f * pulse;
            }

            // Ruch
            switch (mode)
            {
                case Mode.Bouncing:  MoveBouncing(b); break;
                case Mode.Diagonal:  MoveDiagonal(b); break;
                case Mode.Orbital:   MoveOrbital(b);  break;
                case Mode.PaintShop: MovePaint(b);    break;
                case Mode.Arena:     MoveArena(b, dt); break;
            }
        }
    }

    void UpdateHueBall(BGBall b, float t)
    {
        float sat, val, alpha;
        if (mode == Mode.PaintShop)
        {
            sat   = 0.82f + Mathf.Sin(t * 0.55f + b.alphaPulseOffset) * 0.14f;
            val   = 0.90f;
            alpha = 0.18f + Mathf.Sin(t * 0.85f + b.alphaPulseOffset) * 0.07f;
        }
        else // Arena
        {
            sat   = 0.70f;
            val   = 0.80f;
            alpha = 0.09f + Mathf.Sin(t * 1.3f + b.alphaPulseOffset) * 0.05f;
        }
        Color rgb  = Color.HSVToRGB(b.hue, sat, val);
        b.sr.color = new Color(rgb.r, rgb.g, rgb.b, alpha);
    }

    // ─── Metody ruchu ─────────────────────────────────────────────────────────

    void MoveBouncing(BGBall b)
    {
        b.go.transform.position += (Vector3)(b.vel * Time.deltaTime);
        Vector3 p = b.go.transform.position;
        if (p.x >  _hw || p.x < -_hw) { b.vel.x = -b.vel.x; p.x = Mathf.Clamp(p.x, -_hw, _hw); }
        if (p.y >  _hh || p.y < -_hh) { b.vel.y = -b.vel.y; p.y = Mathf.Clamp(p.y, -_hh, _hh); }
        b.go.transform.position = p;
    }

    void MoveDiagonal(BGBall b)
    {
        b.go.transform.position += (Vector3)(b.vel * Time.deltaTime);
        Vector3 p = b.go.transform.position;
        if (p.x >  _hw + 1f) p.x = -_hw - 1f;
        if (p.x < -_hw - 1f) p.x =  _hw + 1f;
        if (p.y >  _hh + 1f) p.y = -_hh - 1f;
        if (p.y < -_hh - 1f) p.y =  _hh + 1f;
        b.go.transform.position = p;
    }

    void MoveOrbital(BGBall b)
    {
        b.orbitAngle += b.orbitSpeed * Time.deltaTime;
        b.go.transform.position = new Vector3(
            Mathf.Cos(b.orbitAngle) * b.orbitRadius,
            Mathf.Sin(b.orbitAngle) * b.orbitRadius * 0.55f, 0f);
    }

    void MovePaint(BGBall b)
    {
        Vector3 p = b.go.transform.position;

        if (b.isDrifter)
        {
            // Powolne spływanie w dół, delikatne kołysanie boczne
            p.y += b.driftSpeedY * Time.deltaTime;
            p.x += Mathf.Sin(Time.time * 1.5f + b.alphaPulseOffset) * 0.01f;
            b.go.transform.position = p;

            // Reset na górę ekranu gdy wyjdzie za dół
            if (p.y < -_hh - 1f)
            {
                p.y = _hh + Random.Range(0.5f, 2f);
                p.x = Random.Range(-_hw, _hw);
                b.driftSpeedY = Random.Range(-0.22f, -0.55f);
                b.go.transform.position = p;
            }
        }
        else
        {
            // Leniwy unos z odbijaniem od krawędzi
            b.go.transform.position += (Vector3)(b.vel * Time.deltaTime);
            p = b.go.transform.position;
            if (p.x >  _hw || p.x < -_hw) { b.vel.x = -b.vel.x; p.x = Mathf.Clamp(p.x, -_hw, _hw); }
            if (p.y >  _hh || p.y < -_hh) { b.vel.y = -b.vel.y; p.y = Mathf.Clamp(p.y, -_hh, _hh); }
            b.go.transform.position = p;
        }
    }

    void MoveArena(BGBall b, float dt)
    {
        // Lagowanie prędkości z powrotem do bazowej po mechanikach
        b.vel = Vector2.Lerp(b.vel, b.baseVel, dt * 1.8f);

        b.go.transform.position += (Vector3)(b.vel * dt);
        Vector3 p = b.go.transform.position;

        float hw = Mathf.Max(_hw, arenaHalfSize + 0.5f);
        float hh = Mathf.Max(_hh, arenaHalfSize + 0.5f);

        // Odbij od krawędzi ekranu
        if (p.x >  hw || p.x < -hw) { b.vel.x = -b.vel.x; b.baseVel.x = -b.baseVel.x; p.x = Mathf.Clamp(p.x, -hw, hw); }
        if (p.y >  hh || p.y < -hh) { b.vel.y = -b.vel.y; b.baseVel.y = -b.baseVel.y; p.y = Mathf.Clamp(p.y, -hh, hh); }

        // Odbij od krawędzi areny (zostań w marginesach)
        float margin = arenaHalfSize + 0.3f;
        bool insideArena = Mathf.Abs(p.x) < margin && Mathf.Abs(p.y) < margin;
        if (insideArena)
        {
            b.go.transform.position = RandomMarginPos();
            b.vel     = b.baseVel;
            return;
        }

        b.go.transform.position = p;
    }

    // ─── Reakcje na zdarzenia Areny ───────────────────────────────────────────

    void HandleBallDied(Vector3 pos, Color col)
    {
        // Pobliskie kulki tła lekko odskakują od miejsca śmierci
        foreach (var b in _balls)
        {
            if (b.go == null) continue;
            float d = Vector2.Distance(b.go.transform.position, pos);
            if (d > 7f) continue;
            Vector2 dir = ((Vector2)b.go.transform.position - (Vector2)pos).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitCircle.normalized;
            b.vel += dir * Mathf.Lerp(5f, 0.8f, d / 7f);
        }
        StartCoroutine(DeathBurst(pos, col));
    }

    void HandleGameEnd()
    {
        // Wszystkie kulki tła silnie odlatują poza kamerę
        foreach (var b in _balls)
        {
            if (b.go == null) continue;
            Vector2 dir = ((Vector2)b.go.transform.position).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitCircle.normalized;
            float spd = Random.Range(20f, 40f);
            b.vel     = dir * spd;
            b.baseVel = b.vel; // nie wracaj do normalnej prędkości
        }
    }

    void HandleAoEFired(Vector3 pos, Color col, float radius)
    {
        // Najbliższe kulki tła lekko rozpraszają się od centrum AoE
        foreach (var b in _balls)
        {
            if (b.go == null) continue;
            float d = Vector2.Distance(b.go.transform.position, pos);
            if (d > radius * 2.5f) continue;
            Vector2 dir = ((Vector2)b.go.transform.position - (Vector2)pos).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitCircle.normalized;
            b.vel += dir * Mathf.Lerp(3f, 0.5f, d / (radius * 2.5f));
        }
    }

    void HandleWallBlast()
    {
        foreach (var b in _balls)
        {
            if (b.go == null) continue;
            Vector2 dir = ((Vector2)b.go.transform.position).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitCircle.normalized;
            b.vel += dir * Random.Range(5f, 10f);
        }
    }

    void HandleCenterPull()
    {
        foreach (var b in _balls)
        {
            if (b.go == null) continue;
            Vector2 dir = -((Vector2)b.go.transform.position).normalized;
            b.vel += dir * Random.Range(3f, 7f);
        }
    }

    // ─── Burst przy śmierci kulki ─────────────────────────────────────────────

    IEnumerator DeathBurst(Vector3 pos, Color col)
    {
        const int   N   = 10;
        const float DUR = 0.60f;

        var burst = new GameObject[N];
        var vels  = new Vector2[N];

        for (int i = 0; i < N; i++)
        {
            burst[i] = new GameObject("DB");
            burst[i].transform.SetParent(transform);
            burst[i].transform.position = pos;

            var sr  = burst[i].AddComponent<SpriteRenderer>();
            sr.sprite       = BallArenaUtils.CircleSprite;
            sr.sortingOrder = -14;
            sr.transform.localScale = Vector3.one * Random.Range(0.06f, 0.18f);
            sr.color        = new Color(col.r, col.g, col.b, 0.55f);

            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(2f, 6f);
            vels[i]   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
        }

        float elapsed = 0f;
        while (elapsed < DUR)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.55f, 0f, elapsed / DUR);
            for (int i = 0; i < N; i++)
            {
                if (burst[i] == null) continue;
                burst[i].transform.position += (Vector3)(vels[i] * Time.deltaTime);
                vels[i] *= (1f - Time.deltaTime * 2.8f);
                var sr = burst[i].GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(col.r, col.g, col.b, alpha);
            }
            yield return null;
        }

        for (int i = 0; i < N; i++)
            if (burst[i] != null) Destroy(burst[i]);
    }
}
