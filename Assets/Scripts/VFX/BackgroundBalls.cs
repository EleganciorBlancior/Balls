// BackgroundBalls.cs – animowane kulki w tle menu
// Podepnij na pustym GameObject w scenie. Tryby: Bouncing, Diagonal, Orbital.
using System.Collections.Generic;
using UnityEngine;

public class BackgroundBalls : MonoBehaviour
{
    public enum Mode { Bouncing, Diagonal, Orbital }

    [Header("Tryb")]
    public Mode mode = Mode.Bouncing;

    [Header("Ustawienia")]
    public int   ballCount  = 12;
    public float minRadius  = 0.18f;
    public float maxRadius  = 0.45f;
    public float minSpeed   = 1.2f;
    public float maxSpeed   = 3.0f;

    private class BGBall
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float orbitAngle, orbitRadius, orbitSpeed;
        public float alphaPulseOffset;
        public Color baseColor;
    }

    private readonly List<BGBall> _balls = new List<BGBall>();
    private Camera _cam;
    private float  _hw, _hh; // half-width / half-height w jednostkach świata

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

    void Start()
    {
        _cam = Camera.main;
        UpdateBounds();

        for (int i = 0; i < ballCount; i++)
            _balls.Add(CreateBall(i));
    }

    void UpdateBounds()
    {
        if (_cam == null) return;
        _hh = _cam.orthographicSize;
        _hw = _hh * _cam.aspect;
    }

    BGBall CreateBall(int index)
    {
        var b  = new BGBall();
        float r = Random.Range(minRadius, maxRadius);
        Color col = PALETTE[index % PALETTE.Length];
        b.baseColor = col;
        b.alphaPulseOffset = Random.Range(0f, Mathf.PI * 2f);

        b.go = new GameObject("BGBall_" + index);
        b.go.transform.SetParent(transform);
        b.sr = b.go.AddComponent<SpriteRenderer>();
        b.sr.sprite       = BallArenaUtils.CircleSprite;
        b.sr.color        = col;
        b.sr.sortingOrder = -10;
        b.go.transform.localScale = Vector3.one * r * 2f;

        switch (mode)
        {
            case Mode.Bouncing:
                b.go.transform.position = new Vector3(
                    Random.Range(-_hw, _hw), Random.Range(-_hh, _hh), 0f);
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float spd = Random.Range(minSpeed, maxSpeed);
                b.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
                break;

            case Mode.Diagonal:
                b.go.transform.position = new Vector3(
                    Random.Range(-_hw, _hw), Random.Range(-_hh, _hh), 0f);
                float ds = Random.Range(minSpeed, maxSpeed);
                b.vel = new Vector2(ds, -ds * Random.Range(0.6f, 1.0f));
                break;

            case Mode.Orbital:
                b.orbitRadius = Random.Range(_hw * 0.15f, _hw * 0.75f);
                b.orbitAngle  = Random.Range(0f, Mathf.PI * 2f);
                b.orbitSpeed  = Random.Range(minSpeed, maxSpeed) * (Random.value > 0.5f ? 1f : -1f) * 0.4f;
                break;
        }

        return b;
    }

    void Update()
    {
        UpdateBounds();
        float t = Time.time;

        foreach (var b in _balls)
        {
            // Pulsujące alpha
            float alpha = 0.18f + Mathf.Sin(t * 1.2f + b.alphaPulseOffset) * 0.12f;
            b.sr.color = new Color(b.baseColor.r, b.baseColor.g, b.baseColor.b, alpha);

            switch (mode)
            {
                case Mode.Bouncing:
                    MoveBouncing(b);
                    break;
                case Mode.Diagonal:
                    MoveDiagonal(b);
                    break;
                case Mode.Orbital:
                    MoveOrbital(b);
                    break;
            }
        }
    }

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
        // Wrap around
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
            Mathf.Sin(b.orbitAngle) * b.orbitRadius * 0.55f,
            0f);
    }
}
