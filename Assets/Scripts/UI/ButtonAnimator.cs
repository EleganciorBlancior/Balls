// ButtonAnimator.cs
// Podepnij na każdy przycisk (Button component).
//
// STRUKTURA OBWÓDKI (border za przyciskiem):
//   Button GO  ← tu jest ten skrypt + Button component + Image (= BORDER, pełny rozmiar)
//   └── Fill GO  ← child Image (= wypełnienie przycisku, mniejsze o padding)
//       └── Label (TMP)
//
// W Unity UI dzieci renderują się NA WIERZCHU rodzica → Fill przykrywa Border
// wszędzie oprócz krawędzi = naturalna obwódka bez żadnych tricków.
// Pole "fillImage" przypisz do Image z Fill GO.
// Button.targetGraphic też ustaw na Fill GO Image.
//
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonAnimator : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,  IPointerUpHandler
{
    [Header("Skala (relatywna – 1.0 = oryginalna skala przycisku)")]
    public float hoverScale  = 1.08f;
    public float pressScale  = 0.93f;
    public float normalScale = 1.00f;
    public float scaleSpeed  = 14f;

    [Header("Kolor wypełnienia (Fill)")]
    public Image fillImage;                                      // child Fill GO
    public bool  tintFill        = true;
    public Color fillNormal      = new Color(0.09f, 0.11f, 0.15f, 1f);
    public Color fillHover       = new Color(0.14f, 0.17f, 0.22f, 1f);
    public Color fillPress       = new Color(0.06f, 0.08f, 0.11f, 1f);
    public Color fillDisabled    = new Color(0.06f, 0.07f, 0.09f, 1f);
    public float tintSpeed       = 12f;

    [Header("Kolor obwódki (Image na tym samym GO)")]
    public bool  animateBorder   = true;
    // borderImage to Image na TYM GO (root) – jeśli null, użyje GetComponent<Image>()
    public Image borderImage;
    public Color borderNormal    = new Color(0.22f, 0.25f, 0.30f, 1f);
    public Color borderHover     = new Color(0.85f, 0.50f, 0.05f, 1f);
    public Color borderPress     = new Color(1.00f, 0.68f, 0.10f, 1f);
    public Color borderDisabled  = new Color(0.15f, 0.17f, 0.20f, 1f);
    public float borderSpeed     = 10f;

    [Header("Idle pulse (opcjonalny – np. na przycisku GRAJ)")]
    public bool  idlePulse   = false;
    public float pulseAmount = 0.022f;
    public float pulseSpeed  = 1.8f;

    // ── Prywatne ──────────────────────────────────────────────────────────────
    private Button        _btn;
    private RectTransform _rt;
    private Image         _border;
    private Vector3       _baseScale;   // oryginalna skala z Inspectora

    private float  _targetScale;
    private Color  _targetFill;
    private Color  _targetBorder;
    private bool   _hovered;
    private float  _pulseTime;

    private void Awake()
    {
        _btn    = GetComponent<Button>();
        _rt     = GetComponent<RectTransform>();
        _border = borderImage != null ? borderImage : GetComponent<Image>();

        // Zapamiętaj oryginalną skalę – animacje będą relatywne
        _baseScale    = _rt.localScale;

        _targetScale  = normalScale;
        _targetFill   = fillNormal;
        _targetBorder = borderNormal;

        // Wyłącz wbudowane przejścia Unity żeby nie walczyły z naszymi
        _btn.transition = Selectable.Transition.None;

        // Ustaw kolory startowe
        if (fillImage  != null) fillImage.color  = fillNormal;
        if (_border    != null) _border.color     = borderNormal;
    }

    private void Update()
    {
        bool disabled = !_btn.interactable;

        if (disabled)
        {
            _targetScale  = normalScale;
            _targetFill   = fillDisabled;
            _targetBorder = borderDisabled;
        }

        // Idle pulse (tylko gdy aktywny i bez hovera)
        float pulse = 0f;
        if (idlePulse && !_hovered && !disabled)
        {
            _pulseTime += Time.unscaledDeltaTime * pulseSpeed;
            pulse = Mathf.Sin(_pulseTime) * pulseAmount;
        }

        // ── Skala (relatywna do _baseScale) ──────────────────────────────────
        float curNorm = _rt.localScale.x / (_baseScale.x > 0f ? _baseScale.x : 1f);
        float newNorm = Mathf.Lerp(curNorm, _targetScale + pulse, Time.unscaledDeltaTime * scaleSpeed);
        _rt.localScale = _baseScale * newNorm;

        // ── Kolory ───────────────────────────────────────────────────────────
        if (tintFill && fillImage != null)
            fillImage.color = Color.Lerp(fillImage.color, _targetFill, Time.unscaledDeltaTime * tintSpeed);

        if (animateBorder && _border != null)
            _border.color = Color.Lerp(_border.color, _targetBorder, Time.unscaledDeltaTime * borderSpeed);
    }

    // ── Eventy ────────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _hovered      = true;
        _targetScale  = hoverScale;
        _targetFill   = fillHover;
        _targetBorder = borderHover;
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovered      = false;
        _targetScale  = normalScale;
        _targetFill   = fillNormal;
        _targetBorder = borderNormal;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _targetScale  = pressScale;
        _targetFill   = fillPress;
        _targetBorder = borderPress;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _targetScale  = _hovered ? hoverScale  : normalScale;
        _targetFill   = _hovered ? fillHover   : fillNormal;
        _targetBorder = _hovered ? borderHover : borderNormal;
    }

    // ── Shake – wywołaj gdy gracz kliknie nieaktywny przycisk ─────────────────
    public void Shake() => StartCoroutine(ShakeRoutine());

    private System.Collections.IEnumerator ShakeRoutine()
    {
        Vector3 origin  = _rt.anchoredPosition3D;
        float[] offsets = { -9f, 9f, -6f, 6f, -3f, 3f, 0f };
        foreach (float ox in offsets)
        {
            _rt.anchoredPosition3D = origin + new Vector3(ox, 0, 0);
            yield return new WaitForSecondsRealtime(0.028f);
        }
        _rt.anchoredPosition3D = origin;
    }
}
