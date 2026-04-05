// AttackRingFX.cs – fala uderzeniowa AoE w stylu "Kassadin ult" (LoL)
// Pierścień wylatuje szybko od centrum, gwałtownie zwalnia przy granicy obszaru.
// Środek blednie jako pierwszy, krawędź zostaje jasna najdłużej.
// Efekt renderuje się ZA kulkami (sortingOrder ujemny) – nie przykrywa ich.
using System.Collections;
using UnityEngine;

public class AttackRingFX : MonoBehaviour
{
    // ─── Statyczne zasoby (tworzone raz i cache'owane) ────────────────────────
    static Sprite   _ringSprite;
    static Material _mat;

    static Sprite RingSprite
    {
        get
        {
            if (_ringSprite == null) _ringSprite = BuildRingSprite();
            return _ringSprite;
        }
    }

    static Material Mat
    {
        get
        {
            if (_mat == null) _mat = new Material(Shader.Find("Sprites/Default"));
            return _mat;
        }
    }

    // Pierścień: cienki, ostry annulus z przezroczystym środkiem
    static Sprite BuildRingSprite()
    {
        const int   SIZE        = 128;
        const float INNER_RATIO = 0.76f; // 76% promienia = cienki, wyostrzony pierścień
        const float SMOOTH      = 2f;

        var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c  = SIZE * 0.5f;
        float OR = c - 1.5f;
        float IR = OR * INNER_RATIO;

        for (int y = 0; y < SIZE; y++)
        for (int x = 0; x < SIZE; x++)
        {
            float d  = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            float ao = Mathf.Clamp01((OR - d) / SMOOTH);
            float ai = Mathf.Clamp01((d  - IR) / SMOOTH);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Min(ao, ai)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE), Vector2.one * 0.5f, SIZE * 0.5f);
    }

    // ─── Publiczne API ─────────────────────────────────────────────────────────

    /// Pulsujący zarys zapowiedzi ataku przy granicy AoE.
    /// Zwróć zwrócony GameObject i zniszcz go gdy atak odpala.
    public static GameObject SpawnTelegraph(Vector3 pos, Color col, float radius)
    {
        var go = new GameObject("AoETelegraph");
        go.transform.position = pos;
        var fx = go.AddComponent<AttackRingFX>();
        fx._col       = BoostColor(col);
        fx._maxRadius = radius;
        fx.StartCoroutine(fx.TelegraphRoutine());
        return go;
    }

    /// Główna fala ataku AoE: pierścień rozszerza się ease-out, środek blednie pierwszy.
    public static void SpawnWave(Vector3 pos, Color col, float maxRadius, float duration = 0.38f)
    {
        if (BallController.VfxAttackChance <= 0f) return;
        if (BallController.VfxAttackChance < 1f && UnityEngine.Random.value > BallController.VfxAttackChance) return;
        var go = new GameObject("AoEWaveFX");
        go.transform.position = pos;
        var fx = go.AddComponent<AttackRingFX>();
        fx._col       = BoostColor(col);
        fx._maxRadius = maxRadius;
        fx._duration  = duration;
        fx.StartCoroutine(fx.WaveRoutine());
        Destroy(go, duration + 0.5f);
    }

    /// Kompatybilność wsteczna z TechTurret i innymi systemami.
    public static void Spawn(Vector3 pos, Color col, float maxRadius, float duration = 0.4f)
        => SpawnWave(pos, col, maxRadius, duration);

    // ─── Prywatne ──────────────────────────────────────────────────────────────

    Color _col;
    float _maxRadius, _duration;

    // Gwarantuje że kolor jest dostatecznie jasny i nasycony (dla dark klas jak Titan)
    static Color BoostColor(Color c)
    {
        float maxC = Mathf.Max(c.r, c.g, c.b, 0.001f);
        float scale = Mathf.Max(1f, 0.80f / maxC);
        return new Color(
            Mathf.Clamp01(c.r * scale),
            Mathf.Clamp01(c.g * scale),
            Mathf.Clamp01(c.b * scale), 1f);
    }

    SpriteRenderer MakeSR(Sprite spr, int order)
    {
        var go = new GameObject("_sr");
        go.transform.SetParent(transform, false);
        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = spr;
        sr.material  = Mat;
        sr.sortingOrder = order;
        return sr;
    }

    // ─── Telegraph ─────────────────────────────────────────────────────────────
    IEnumerator TelegraphRoutine()
    {
        var sr = MakeSR(RingSprite, -4);
        sr.transform.localScale = Vector3.one * _maxRadius * 2f;
        float t = 0f;
        while (gameObject != null)
        {
            t += Time.deltaTime * 6f;
            float a = 0.22f + Mathf.Sin(t) * 0.13f;
            sr.color = new Color(_col.r, _col.g, _col.b, a);
            yield return null;
        }
    }

    // ─── Wave ──────────────────────────────────────────────────────────────────
    IEnumerator WaveRoutine()
    {
        // Jedno wypełnione kółko: rozszerza się ease-out, potem rozpływa się na zewnątrz
        var disc = MakeSR(BallArenaUtils.CircleSprite, -3);

        const float EXPAND_END = 0.55f; // proporcja czasu przeznaczona na rozszerzanie
        const float ALPHA      = 0.46f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / _duration;
            float tc = Mathf.Clamp01(t);

            if (tc < EXPAND_END)
            {
                // Faza 1: kółko rośnie 0 → maxRadius (ease-out-cubic: szybko ze środka, zwalnia na końcu)
                float p     = tc / EXPAND_END;
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                disc.transform.localScale = Vector3.one * (_maxRadius * eased * 2f);
                disc.color = new Color(_col.r, _col.g, _col.b, ALPHA);
            }
            else
            {
                // Faza 2: kółko lekko dalej rośnie + zanika (efekt "rozpływania się")
                float p     = (tc - EXPAND_END) / (1f - EXPAND_END); // 0 → 1
                float scale = Mathf.Lerp(1f, 1.16f, p);
                disc.transform.localScale = Vector3.one * (_maxRadius * scale * 2f);
                float alpha = ALPHA * (1f - p * p); // ease-in: wolne zanikanie potem gwałtowne
                disc.color = new Color(_col.r, _col.g, _col.b, alpha);
            }

            yield return null;
        }
    }
}
