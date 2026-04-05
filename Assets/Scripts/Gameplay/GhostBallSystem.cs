// GhostBallSystem.cs
// Zarządza "ghost balls" — czysto wizualnymi kulkami bez fizyki ani AI.
// Ghosty same "giną" w kontrolowanym tempie, generując fake kille w KillFeed.
// Ghosty mogą być "promowane" do prawdziwych kulek gdy real balle giną.
using System.Collections.Generic;
using UnityEngine;

/// <summary>Dane potrzebne do promocji ghosta na prawdziwą kulkę.</summary>
public struct GhostPromoteData
{
    public Vector2     pos;
    public BallClass   ballClass;
    public ClassConfig cfg;
    public float       statMult;
    public int         goldMult;
    public int         mergeLevel;
}

public class GhostBallSystem : MonoBehaviour
{
    struct GhostData
    {
        public Vector2        pos;
        public Vector2        vel;
        public Transform      tf;
        public SpriteRenderer sr;
        public float          baseDiameter;
        public float          radius;
        public Color          baseColor;
        public BallClass      ballClass;
        public int            number;
        // Dane do promocji
        public ClassConfig    cfg;
        public float          statMult;
        public int            goldMult;
        public int            mergeLevel;
    }

    private readonly List<GhostData> _ghosts = new List<GhostData>();
    private List<ClassConfig> _ownedConfigs;
    private Dictionary<BallClass, int> _ownedCounts;
    private float _dynamicMult = 1f;

    // Eliminacja
    private float _killTimer;
    private float _killInterval;
    private bool  _frozen = true;

    // Slow start: prędkość ghostów rośnie z 10% do 100% przez pierwsze sekundy
    private float _speedRamp      = 0.1f;
    private const float RAMP_DURATION = 4f;
    private float _timeSinceStart = 0f;

    // Płynna eliminacja
    private float _targetKillInterval;
    private bool  _fastElimActive = false;

    public int Count => _ghosts.Count;

    /// <summary>Inicjalizuje system — ownedConfigs to tylko klasy posiadane przez gracza.</summary>
    public void Init(List<ClassConfig> ownedConfigs, Dictionary<BallClass, int> ownedCounts,
                     int totalGhosts, int totalReal)
    {
        _ownedConfigs = ownedConfigs;
        _ownedCounts  = ownedCounts;
        _frozen = true;

        // Tempo eliminacji: ~45s na pozbycie się wszystkich ghostów
        float targetDuration = 45f;
        _killInterval = totalGhosts > 0 ? targetDuration / totalGhosts : 1f;
        _killInterval = Mathf.Max(_killInterval, 0.005f);
        _killTimer = 0f;
    }

    public void AddGhost(Vector2 pos, Color color, float diameter, float speed,
                         BallClass cls, int number,
                         ClassConfig cfg, float statMult, int goldMult, int mergeLevel)
    {
        var go = new GameObject("Ghost");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * diameter;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = BallArenaUtils.CircleSprite;
        sr.color        = color;
        sr.sortingOrder = -2;

        float ang = Random.Range(0f, Mathf.PI * 2f);
        _ghosts.Add(new GhostData
        {
            pos          = pos,
            vel          = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed,
            tf           = go.transform,
            sr           = sr,
            baseDiameter = diameter,
            radius       = diameter * 0.5f,
            baseColor    = color,
            ballClass    = cls,
            number       = number,
            cfg          = cfg,
            statMult     = statMult,
            goldMult     = goldMult,
            mergeLevel   = mergeLevel
        });
    }

    /// <summary>Odmraża ghosty — zaczynają się ruszać i ginąć.</summary>
    public void Unfreeze()
    {
        _frozen = false;
        _killTimer = 0f;
        _timeSinceStart = 0f;
        _speedRamp = 0.1f;
    }

    /// <summary>Przyspiesza eliminację — usuwa wszystkie ghosty w ciągu ~4s.</summary>
    public void SetFastElimination()
    {
        int left = _ghosts.Count;
        if (left <= 0) return;
        _fastElimActive = true;
        _targetKillInterval = 4f / Mathf.Max(left, 1);
    }

    /// <summary>Promuje losowego ghosta — usuwa go i zwraca dane do stworzenia prawdziwej kulki.</summary>
    public GhostPromoteData? PromoteOne()
    {
        if (_ghosts.Count == 0) return null;

        int idx = Random.Range(0, _ghosts.Count);
        var g   = _ghosts[idx];

        var data = new GhostPromoteData
        {
            pos        = g.pos,
            ballClass  = g.ballClass,
            cfg        = g.cfg,
            statMult   = g.statMult,
            goldMult   = g.goldMult,
            mergeLevel = g.mergeLevel
        };

        // Usuń ghost GO i element z listy (swap-remove)
        if (g.tf != null) Destroy(g.tf.gameObject);
        _ghosts[idx] = _ghosts[_ghosts.Count - 1];
        _ghosts.RemoveAt(_ghosts.Count - 1);

        return data;
    }

    /// <summary>Ustawia dynamic mult — identyczny co BallController.SetDynamicScale(mult).</summary>
    public void SetDynamicMult(float mult)
    {
        _dynamicMult = mult;
        for (int i = 0; i < _ghosts.Count; i++)
        {
            var g    = _ghosts[i];
            float d  = g.baseDiameter * mult;
            g.radius = d * 0.5f;
            g.tf.localScale = Vector3.one * d;
            _ghosts[i] = g;
        }
    }

    /// <summary>Przyciąga ghost balle do centrum — efekt center pull.</summary>
    public void ApplyCenterPull(float force)
    {
        for (int i = 0; i < _ghosts.Count; i++)
        {
            var g   = _ghosts[i];
            var dir = -g.pos.normalized;
            g.vel  += dir * force;
            _ghosts[i] = g;
        }
    }

    /// <summary>Niszczy wszystkie ghost balle natychmiast.</summary>
    public void Clear()
    {
        foreach (var g in _ghosts)
            if (g.tf != null) Destroy(g.tf.gameObject);
        _ghosts.Clear();
    }

    private int _frameCount = 0;

    private void Update()
    {
        if (_frozen) return;

        float dt  = Time.deltaTime;
        float lim = BallController.ArenaHalf;
        _frameCount++;

        // Slow start: ramp prędkości 10% → 100% przez RAMP_DURATION sekund
        _timeSinceStart += dt;
        if (_speedRamp < 1f)
            _speedRamp = Mathf.Clamp01(0.1f + 0.9f * (_timeSinceStart / RAMP_DURATION));

        // Płynne przejście kill interval
        if (_fastElimActive && _killInterval > _targetKillInterval)
        {
            _killInterval = Mathf.Lerp(_killInterval, _targetKillInterval, dt * 3f);
            if (_killInterval - _targetKillInterval < 0.0001f)
                _killInterval = _targetKillInterval;
        }

        // Ruch: pozycja liczona co klatkę, transform sync co klatkę

        for (int i = 0; i < _ghosts.Count; i++)
        {
            var g = _ghosts[i];
            g.pos += g.vel * (_speedRamp * dt);

            float l = lim - g.radius;
            if (g.pos.x < -l) { g.pos.x = -l; g.vel.x =  Mathf.Abs(g.vel.x); }
            if (g.pos.x >  l) { g.pos.x =  l; g.vel.x = -Mathf.Abs(g.vel.x); }
            if (g.pos.y < -l) { g.pos.y = -l; g.vel.y =  Mathf.Abs(g.vel.y); }
            if (g.pos.y >  l) { g.pos.y =  l; g.vel.y = -Mathf.Abs(g.vel.y); }


            g.tf.position = new Vector3(g.pos.x, g.pos.y, 0f);

            _ghosts[i] = g;
        }

        // Eliminacja ghost ballów
        if (_ghosts.Count > 0)
        {
            _killTimer += dt * _speedRamp;
            // Limit killów per klatkę żeby nie robić setek Destroy() naraz
            int killBudget = _ghosts.Count > 1000 ? 5 : 3;
            int killed = 0;
            while (_killTimer >= _killInterval && _ghosts.Count > 0 && killed < killBudget)
            {
                _killTimer -= _killInterval;
                KillRandomGhost();
                killed++;
            }
        }
    }

    void KillRandomGhost()
    {
        int idx = Random.Range(0, _ghosts.Count);
        var g   = _ghosts[idx];

        // Efekty śmierci — tylko w sektorze VFX
        if (BallController.IsVfxAllowed())
            BallDeathParticles.Spawn(g.tf.position, g.baseColor, BallController.VfxDeathCount);

        // Fake kill w KillFeed — tylko klasy posiadane przez gracza
        if (KillFeed.Instance != null && _ownedConfigs != null && _ownedConfigs.Count > 1)
        {
            ClassConfig killerCfg = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var candidate = _ownedConfigs[Random.Range(0, _ownedConfigs.Count)];
                if (candidate.ballClass != g.ballClass) { killerCfg = candidate; break; }
            }
            if (killerCfg == null) killerCfg = _ownedConfigs[0];

            Color killerColor = killerCfg.color;
            var custom = GameData.Instance?.GetCustomization(killerCfg.ballClass);
            if (custom != null) killerColor = custom.color1;

            int killerMax = 1;
            if (_ownedCounts != null && _ownedCounts.TryGetValue(killerCfg.ballClass, out int kc)) killerMax = kc;
            int victimMax = 1;
            if (_ownedCounts != null && _ownedCounts.TryGetValue(g.ballClass, out int vc)) victimMax = vc;

            string killerLabel = LocalizationManager.GetClassName(killerCfg.ballClass) + " #" + Random.Range(1, killerMax + 1);
            string victimLabel = LocalizationManager.GetClassName(g.ballClass) + " #" + Random.Range(1, victimMax + 1);
            KillFeed.Instance.ReportGhostKill(killerLabel, killerColor, victimLabel, g.baseColor);
        }

        Destroy(g.tf.gameObject);
        _ghosts[idx] = _ghosts[_ghosts.Count - 1];
        _ghosts.RemoveAt(_ghosts.Count - 1);
    }
}
