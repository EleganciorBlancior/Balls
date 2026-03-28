// KillFeed.cs – prosta lista zabójstw w lewym górnym rogu
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

    private class Entry { public GameObject go; public float timeLeft; public CanvasGroup cg; }

    private void Awake()
    {
        Instance = this;
        BuildContainer();
    }

    void BuildContainer()
    {
        Canvas canvas = GetComponentInParent<Canvas>()
                     ?? FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go          = new GameObject("KillFeedContainer");
        go.transform.SetParent(canvas.transform, false);
        _container      = go.AddComponent<RectTransform>();
        _container.anchorMin        = new Vector2(0f, 1f);
        _container.anchorMax        = new Vector2(0f, 1f);
        _container.pivot            = new Vector2(0f, 1f);
        _container.anchoredPosition = new Vector2(10f, -10f);
        _container.sizeDelta        = new Vector2(320f, 400f);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.spacing                = 3f;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = false;
        vlg.childControlHeight     = false;
    }

    public void ReportKill(string killerName, Color killerColor,
                           string victimName,  Color victimColor)
    {
        if (_container == null) return;
        while (_entries.Count >= maxEntries) RemoveEntry(_entries[0]);

        var entryGO = BuildEntry(killerName, killerColor, victimName, victimColor);
        var cg      = entryGO.AddComponent<CanvasGroup>();
        var e       = new Entry { go = entryGO, timeLeft = entryLifetime, cg = cg };
        _entries.Add(e);
        StartCoroutine(FadeOut(e));
    }

    GameObject BuildEntry(string killerName, Color killerColor,
                          string victimName,  Color victimColor)
    {
        var go = new GameObject("KillEntry");
        go.transform.SetParent(_container, false);

        // Tło
        var rt       = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 24f);
        var bg       = go.AddComponent<Image>();
        bg.color     = new Color(0.06f, 0.08f, 0.10f, 0.82f);

        // Outline
        var ol            = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0.25f, 0.28f, 0.32f, 1f);
        ol.effectDistance = new Vector2(1f, -1f);

        // Poziomy layout
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding        = new RectOffset(8, 8, 2, 2);
        hlg.spacing        = 4f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight     = true;
        hlg.childForceExpandHeight = true;

        AddDot(go.transform, killerColor);
        AddText(go.transform, killerName, killerColor, FontStyles.Bold,   13f);
        AddText(go.transform, " > ",      Color.gray,  FontStyles.Normal, 11f);
        AddDot(go.transform, victimColor);
        AddText(go.transform, victimName, victimColor, FontStyles.Normal, 12f);

        return go;
    }

    void AddDot(Transform parent, Color col)
    {
        var go          = new GameObject("Dot");
        go.transform.SetParent(parent, false);
        var img         = go.AddComponent<Image>();
        img.sprite      = BallArenaUtils.CircleSprite;
        img.color       = new Color(col.r, col.g, col.b, 0.9f);
        img.raycastTarget = false;
        var le          = go.AddComponent<LayoutElement>();
        le.preferredWidth = le.preferredHeight = 10f;
        le.flexibleHeight = 0f;
    }

    void AddText(Transform parent, string text, Color col, FontStyles style, float size)
    {
        var go  = new GameObject("Txt");
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
        float fadeStart = entryLifetime * 0.6f;
        while (e.timeLeft > 0f)
        {
            e.timeLeft -= Time.deltaTime;
            if (e.cg != null && e.timeLeft < fadeStart)
                e.cg.alpha = e.timeLeft / fadeStart;
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
