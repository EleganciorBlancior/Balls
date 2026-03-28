using UnityEngine;

public static class BallArenaUtils
{
    // ── Sprite kółka ─────────────────────────────────────────────────────────
    private static Sprite _circle;
    public static Sprite CircleSprite
    {
        get
        {
            if (_circle != null) return _circle;
            int sz = 64; var tex = new Texture2D(sz, sz); float r = sz * 0.5f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - r, dy = y - r;
                    float a  = Mathf.Clamp01(1f - (Mathf.Sqrt(dx*dx+dy*dy) - (r-2f)));
                    tex.SetPixel(x, y, new Color(1,1,1,a));
                }
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, sz);
            return _circle;
        }
    }

    // ── Sprite pełnego kwadratu (dla ramek areny) ─────────────────────────────
    private static Sprite _square;
    public static Sprite SolidSquareSprite
    {
        get
        {
            if (_square != null) return _square;
            int sz = 4; var tex = new Texture2D(sz, sz);
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _square = Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, sz);
            return _square;
        }
    }

    // ── Sprite z wzorem (pattern) ────────────────────────────────────────────
    public static Sprite CreatePatternSprite(Color c1, Color c2, Color c3,
                                             BallPattern pattern, int stripeCount = 5)
    {
        int sz = 128; float r = sz * 0.5f;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = x - r, dy = y - r;
            float dist = Mathf.Sqrt(dx*dx + dy*dy);
            float edge = Mathf.Clamp01(1f - (dist - (r - 2f)));
            if (edge <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }

            float nx = (float)x / sz; // 0-1
            float ny = (float)y / sz; // 0-1
            Color col;

            switch (pattern)
            {
                case BallPattern.HorizontalStripes:
                    col = (Mathf.FloorToInt(ny * stripeCount) % 2 == 0) ? c1 : c2;
                    break;
                case BallPattern.DiagonalStripes:
                    col = (Mathf.FloorToInt((nx + ny) * stripeCount * 0.5f) % 2 == 0) ? c1 : c2;
                    break;
                case BallPattern.Pepsi:
                    // Krzywa dzieląca kulę: lewa+górna = c1, prawa+dolna = c2
                    float wave = 0.5f + 0.18f * Mathf.Sin((ny - 0.5f) * Mathf.PI * 2.5f);
                    col = (nx < wave) ? c1 : c2;
                    break;
                case BallPattern.Quarters:
                    col = ((nx > 0.5f) == (ny > 0.5f)) ? c1 : c2;
                    break;
                case BallPattern.Wedge:
                    float angle = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 1f) % 1f;
                    int   slice = Mathf.FloorToInt(angle * 6) % 6;
                    col = (slice % 3 == 0) ? c1 : (slice % 3 == 1) ? c2 : c3;
                    break;
                case BallPattern.Dots:
                    float sc   = 1f / stripeCount;
                    float fx   = (nx % sc) / sc - 0.5f;
                    float fy   = (ny % sc) / sc - 0.5f;
                    col = (fx*fx + fy*fy < 0.10f) ? c2 : c1;
                    break;
                case BallPattern.Ring:
                    float nd = dist / r;
                    col = nd > 0.65f ? c1 : nd > 0.35f ? c2 : c3;
                    break;
                default: // Solid
                    col = c1; break;
            }

            col.a *= edge;
            tex.SetPixel(x, y, col);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
    }

    // ── Fabryka pocisków ─────────────────────────────────────────────────────
    public static GameObject CreateBulletGO(Vector3 pos, Color col, float radius)
    {
        var go = new GameObject("Bullet");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite; sr.color = col;
        go.transform.localScale = Vector3.one * radius * 2f;
        var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f;
        var c  = go.AddComponent<CircleCollider2D>(); c.radius = 0.5f; c.isTrigger = true;
        go.layer = LayerMask.NameToLayer("Projectile");
        return go;
    }
}
