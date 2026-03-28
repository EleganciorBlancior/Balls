// ColorWheelPicker.cs
//
// Umieść ten komponent na obiekcie z RawImage (kole HSV).
//
// Wymagana hierarchia w Unity:
//   WheelImage (RawImage + ten komponent + EventTrigger/GraphicRaycaster)
//   WheelCursor  (Image, mały kółko wskaźnik, dziecko WheelImage)
//   BrightnessSlider (Slider 0–1, gdziekolwiek)
//   ColorPreview     (Image, bieżący kolor, gdziekolwiek)

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ColorWheelPicker : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    [Header("Refs")]
    public RectTransform wheelCursor;
    public Slider        brightnessSlider;
    public Image         colorPreview;

    public event Action<Color> OnColorChanged;

    private RawImage      _img;
    private RectTransform _rect;
    private Texture2D     _tex;
    private float         _h, _s, _v = 1f;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        _img  = GetComponent<RawImage>();
        _rect = GetComponent<RectTransform>();
        BuildTexture();
        if (brightnessSlider != null) HookBrightnessSlider(brightnessSlider);
    }

    public void HookBrightnessSlider(Slider s)
    {
        if (s == null) return;
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveAllListeners();
        brightnessSlider          = s;
        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.SetValueWithoutNotify(_v);
        brightnessSlider.onValueChanged.AddListener(v => { _v = v; NotifyChanged(); });
    }

    void BuildTexture()
    {
        const int SIZE = 256;
        _tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        float half = SIZE * 0.5f;
        for (int y = 0; y < SIZE; y++)
        for (int x = 0; x < SIZE; x++)
        {
            float dx   = (x - half) / half;
            float dy   = (y - half) / half;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > 1f) { _tex.SetPixel(x, y, Color.clear); continue; }
            float hue = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f);
            if (hue < 0f) hue += 1f;
            _tex.SetPixel(x, y, Color.HSVToRGB(hue, dist, 1f));
        }
        _tex.Apply();
        _img.texture = _tex;
    }

    // ── Zdarzenia wskaźnika ───────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e) => PickFromEvent(e);
    public void OnDrag(PointerEventData e)        => PickFromEvent(e);

    void PickFromEvent(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, e.position, e.pressEventCamera, out Vector2 local)) return;

        Vector2 half = _rect.sizeDelta * 0.5f;
        float dx   = local.x / half.x;
        float dy   = local.y / half.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        float angle = Mathf.Atan2(dy, dx);
        _h = Mathf.Repeat(angle / (Mathf.PI * 2f), 1f);
        _s = Mathf.Min(dist, 1f);

        MoveCursor(angle, _s, half);
        NotifyChanged();
    }

    void MoveCursor(float angleRad, float saturation, Vector2 half)
    {
        if (wheelCursor == null) return;
        wheelCursor.anchoredPosition = new Vector2(
            Mathf.Cos(angleRad) * saturation * half.x,
            Mathf.Sin(angleRad) * saturation * half.y);
    }

    void NotifyChanged()
    {
        Color col = Color.HSVToRGB(_h, _s, _v);
        if (colorPreview != null) colorPreview.color = col;
        OnColorChanged?.Invoke(col);
    }

    // ── API ───────────────────────────────────────────────────────────────

    public Color GetColor() => Color.HSVToRGB(_h, _s, _v);

    public void SetColor(Color col, bool notify = false)
    {
        Color.RGBToHSV(col, out _h, out _s, out _v);

        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(_v);

        if (_rect != null)
        {
            Vector2 half  = _rect.sizeDelta * 0.5f;
            float   angle = _h * Mathf.PI * 2f;
            MoveCursor(angle, _s, half);
        }

        if (colorPreview != null) colorPreview.color = col;
        if (notify) NotifyChanged();
    }
}
