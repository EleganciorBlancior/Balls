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
