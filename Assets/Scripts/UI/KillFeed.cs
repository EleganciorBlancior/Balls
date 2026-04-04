// KillFeed.cs – lista zabójstw z ikonkami kulek
// Format: [●] ClassName #N  ⚔  [●] ClassName #N
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KillFeed : MonoBehaviour
{
    public static KillFeed Instance { get; private set; }

    public int   maxEntries    = 6;
    public float entryLifetime = 5f;

    private RectTransform        _container;
    private readonly List<Entry> _entries = new List<Entry>();

    private class Entry { public RectTransform rt; public GameObject go; public float timeLeft; public CanvasGroup cg; }

    static readonly Color BG_COLOR     = new Color(0.05f, 0.05f, 0.08f, 0.88f);
    const float ENTRY_W   = 310f;
    const float ENTRY_H   = 30f;
    const float BALL_SIZE = 18f;

    private void Awake()
    {
        Instance = this;
        BuildContainer();
    }

    void BuildContainer()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go     = new GameObject("KillFeedContainer");
        go.transform.SetParent(canvas.transform, false);
        _container = go.AddComponent<RectTransform>();
        _container.anchorMin        = new Vector2(0f, 1f);
        _container.anchorMax        = new Vector2(0f, 1f);
        _container.pivot            = new Vector2(0f, 1f);
        _container.anchoredPosition = new Vector2(12f, -12f);
        _container.sizeDelta        = new Vector2(ENTRY_W, 400f);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.spacing                = 4f;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = false;
        vlg.childControlHeight     = false;
    }

    public void ReportKill(BallController killer, BallController victim)
    {
        if (_container == null) return;
        while (_entries.Count >= maxEntries) RemoveEntry(_entries[0]);

        string killerLabel = LocalizationManager.GetClassName(killer.Config.ballClass) + " #" + killer.BallNumber;
        string victimLabel = LocalizationManager.GetClassName(victim.Config.ballClass)  + " #" + victim.BallNumber;
        Color  killerColor = killer.BaseColor;
        Color  victimColor = victim.BaseColor;

        var entryGO = BuildEntry(killerLabel, killerColor, victimLabel, victimColor);
        var cg      = entryGO.AddComponent<CanvasGroup>();
        var rt      = entryGO.GetComponent<RectTransform>();
        var e       = new Entry { rt = rt, go = entryGO, timeLeft = entryLifetime, cg = cg };
        _entries.Add(e);
        StartCoroutine(FadeOut(e));
    }

    GameObject BuildEntry(string killerLabel, Color killerColor,
                          string victimLabel,  Color victimColor)
    {
        var go = new GameObject("KillEntry");
        go.transform.SetParent(_container, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(ENTRY_W, ENTRY_H);

        // tło
        var bg   = go.AddComponent<Image>();
        bg.color = BG_COLOR;

        // lewa kolorowa kreska (kolor zabójcy)
        var stripe     = new GameObject("Stripe");
        stripe.transform.SetParent(go.transform, false);
        var srrt       = stripe.AddComponent<RectTransform>();
        srrt.anchorMin = new Vector2(0f, 0f);
        srrt.anchorMax = new Vector2(0f, 1f);
        srrt.pivot     = new Vector2(0f, 0.5f);
        srrt.offsetMin = new Vector2(0f,  0f);
        srrt.offsetMax = new Vector2(3f,  0f);
        var si         = stripe.AddComponent<Image>();
        si.color       = new Color(killerColor.r, killerColor.g, killerColor.b, 0.9f);
        si.raycastTarget = false;

        // poziomy layout wewnątrz (padding lewy = 8 żeby nie nachodzić na stripe)
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding        = new RectOffset(8, 6, 4, 4);
        hlg.spacing        = 5f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight     = true;
        hlg.childForceExpandHeight = true;

        // ── Zabójca ──────────────────────────────────────────────────────────
        AddBallIcon(go.transform, killerColor);
        AddLabel(go.transform, killerLabel, Color.white, FontStyles.Bold, 11f);

        // ── Separator ────────────────────────────────────────────────────────
        AddLabel(go.transform, " x ", new Color(0.9f, 0.35f, 0.25f), FontStyles.Bold, 10f);

        // ── Ofiara ───────────────────────────────────────────────────────────
        AddBallIcon(go.transform, victimColor);
        AddLabel(go.transform, victimLabel, new Color(0.65f, 0.65f, 0.7f), FontStyles.Normal, 11f);

        return go;
    }

    // Kółko wyglądające jak kulka: ciemna obwódka + wypełnienie + błysk
    // WAŻNE: dodajemy Image najpierw, bo Image wymaga RectTransform –
    //        Unity auto-dodaje RT przy AddComponent<Image>().
    void AddBallIcon(Transform parent, Color ballColor)
    {
        // ── Kontener ──────────────────────────────────────────────────────────
        var wrap     = new GameObject("BallIcon");
        wrap.transform.SetParent(parent, false);
        var wrapImg  = wrap.AddComponent<Image>();   // ← auto-dodaje RectTransform
        wrapImg.color        = Color.clear;
        wrapImg.raycastTarget = false;
        var wrt      = (RectTransform)wrap.transform;
        wrt.sizeDelta = new Vector2(BALL_SIZE, BALL_SIZE);
        var le       = wrap.AddComponent<LayoutElement>();
        le.preferredWidth  = BALL_SIZE;
        le.preferredHeight = BALL_SIZE;
        le.flexibleHeight  = 0f;

        // ── Ciemna obwódka ────────────────────────────────────────────────────
        var outer    = new GameObject("Outer");
        outer.transform.SetParent(wrap.transform, false);
        var oImg     = outer.AddComponent<Image>();  // ← auto-dodaje RectTransform
        oImg.sprite  = BallArenaUtils.CircleSprite;
        oImg.color   = new Color(ballColor.r * 0.3f, ballColor.g * 0.3f, ballColor.b * 0.3f, 1f);
        oImg.raycastTarget = false;
        var ort      = (RectTransform)outer.transform;
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;

        // ── Wewnętrzny kolor kulki ────────────────────────────────────────────
        float pad    = BALL_SIZE * 0.15f;
        var inner    = new GameObject("Inner");
        inner.transform.SetParent(wrap.transform, false);
        var iImg     = inner.AddComponent<Image>();  // ← auto-dodaje RectTransform
        iImg.sprite  = BallArenaUtils.CircleSprite;
        iImg.color   = new Color(ballColor.r, ballColor.g, ballColor.b, 1f);
        iImg.raycastTarget = false;
        var irt      = (RectTransform)inner.transform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2( pad,  pad);
        irt.offsetMax = new Vector2(-pad, -pad);

        // ── Biały błysk (lewy górny róg) ──────────────────────────────────────
        float sz     = BALL_SIZE * 0.28f;
        var shine    = new GameObject("Shine");
        shine.transform.SetParent(wrap.transform, false);
        var sImg     = shine.AddComponent<Image>();  // ← auto-dodaje RectTransform
        sImg.sprite  = BallArenaUtils.CircleSprite;
        sImg.color   = new Color(1f, 1f, 1f, 0.45f);
        sImg.raycastTarget = false;
        var srt      = (RectTransform)shine.transform;
        srt.anchorMin = new Vector2(0.2f, 0.58f);
        srt.anchorMax = new Vector2(0.2f, 0.58f);
        srt.pivot     = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(sz, sz);
    }

    void AddLabel(Transform parent, string text, Color col, FontStyles style, float size)
    {
        var go  = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = col;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        go.AddComponent<LayoutElement>().flexibleWidth = 0f;
    }

    IEnumerator FadeOut(Entry e)
    {
        float fadeStart = entryLifetime * 0.65f;
        while (e.timeLeft > 0f)
        {
            e.timeLeft -= Time.deltaTime;
            if (e.cg != null && e.timeLeft < fadeStart)
                e.cg.alpha = Mathf.Clamp01(e.timeLeft / fadeStart);
            yield return null;
        }
        RemoveEntry(e);
    }

    void RemoveEntry(Entry e)
    {
        _entries.Remove(e);
        if (e.go != null) Destroy(e.go);
    }
}
