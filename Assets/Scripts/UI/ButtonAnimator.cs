// ButtonAnimator.cs
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

    [Header("Kolor (Image na tym samym GO)")]
    public bool  animateFill   = true;
    public Image fillImage;
    public Color fillNormal    = new Color(0.22f, 0.25f, 0.30f, 1f);
    public Color fillHover     = new Color(0.85f, 0.50f, 0.05f, 1f);
    public Color fillPress     = new Color(1.00f, 0.68f, 0.10f, 1f);
    public Color fillDisabled  = new Color(0.15f, 0.17f, 0.20f, 1f);
    public float fillSpeed     = 10f;

    [Header("Idle pulse (opcjonalny – np. na przycisku GRAJ)")]
    public bool  idlePulse   = false;
    public float pulseAmount = 0.022f;
    public float pulseSpeed  = 1.8f;

    // ── Prywatne ──────────────────────────────────────────────────────────────
    private Button        _btn;
    private RectTransform _rt;
    private Image         _fill;
    private Vector3       _baseScale;

    private float  _targetScale;
    private Color  _targetFill;
    private bool   _hovered;
    private float  _pulseTime;

    private void Awake()
    {
        _btn  = GetComponent<Button>();
        _rt   = GetComponent<RectTransform>();
        _fill = fillImage != null ? fillImage : GetComponent<Image>();

        _baseScale   = _rt.localScale;
        _targetScale = normalScale;
        _targetFill  = fillNormal;

        _btn.transition = Selectable.Transition.None;

        if (_fill != null) _fill.color = fillNormal;
    }

    private void Update()
    {
        bool disabled = !_btn.interactable;

        if (disabled)
        {
            _targetScale = normalScale;
            _targetFill  = fillDisabled;
        }

        float pulse = 0f;
        if (idlePulse && !_hovered && !disabled)
        {
            _pulseTime += Time.unscaledDeltaTime * pulseSpeed;
            pulse = Mathf.Sin(_pulseTime) * pulseAmount;
        }

        float curNorm = _rt.localScale.x / (_baseScale.x > 0f ? _baseScale.x : 1f);
        float newNorm = Mathf.Lerp(curNorm, _targetScale + pulse, Time.unscaledDeltaTime * scaleSpeed);
        _rt.localScale = _baseScale * newNorm;

        if (animateFill && _fill != null)
            _fill.color = Color.Lerp(_fill.color, _targetFill, Time.unscaledDeltaTime * fillSpeed);
    }

    // ── Eventy ────────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _hovered     = true;
        _targetScale = hoverScale;
        _targetFill  = fillHover;
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovered     = false;
        _targetScale = normalScale;
        _targetFill  = fillNormal;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _targetScale = pressScale;
        _targetFill  = fillPress;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _targetScale = _hovered ? hoverScale : normalScale;
        _targetFill  = _hovered ? fillHover  : fillNormal;
    }

    // ── Shake ─────────────────────────────────────────────────────────────────
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
