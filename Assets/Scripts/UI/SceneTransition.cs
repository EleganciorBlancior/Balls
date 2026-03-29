// SceneTransition.cs
// Dodaj komponent na dowolny GameObject w scenie.
// Na starcie sceny: czarny panel pod kątem ześlizguje się z ekranu.
// SceneTransition.ExitTo("NazwaSceny") robi odwrotność — panel wjeżdża i ładuje scenę.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [Header("Animacja wejścia")]
    [Range(5f, 30f)]  public float angle       = 12f;   // kąt nachylenia panelu (stopnie)
    [Range(0.2f, 1.5f)] public float revealTime = 0.55f; // czas odsłonięcia
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Statyczna referencja do aktywnej instancji – używana przez ExitTo()
    private static SceneTransition _current;

    private RectTransform _panel;
    private float         _screenW;

    private void Awake()
    {
        _current = this;
        _panel   = BuildOverlay();
    }

    private void Start() => StartCoroutine(RevealRoutine());

    // ── API publiczne ─────────────────────────────────────────────────────────

    /// Wywołaj zamiast SceneManager.LoadScene aby zagrać exit transition.
    public static void ExitTo(string sceneName)
    {
        if (_current != null)
            _current.StartCoroutine(_current.ExitRoutine(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    // ── Animacje ──────────────────────────────────────────────────────────────

    // Panel wjeżdża z lewej → ześlizguje się w prawo poza ekran
    IEnumerator RevealRoutine()
    {
        Vector2 startPos = new Vector2(-_screenW * 0.05f, 0f); // lekko w lewo – zakrywa cały ekran
        Vector2 endPos   = new Vector2( _screenW * 1.15f, 0f); // wychodzi w prawo

        _panel.anchoredPosition = startPos;
        _panel.gameObject.SetActive(true);

        float t = 0f;
        while (t < revealTime)
        {
            t += Time.deltaTime;
            float p = curve.Evaluate(Mathf.Clamp01(t / revealTime));
            _panel.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, p);
            yield return null;
        }

        _panel.gameObject.SetActive(false);
    }

    // Panel wjeżdża z prawej → zakrywa ekran → ładuje scenę
    IEnumerator ExitRoutine(string sceneName)
    {
        Vector2 startPos = new Vector2( _screenW * 1.15f, 0f);
        Vector2 endPos   = new Vector2(-_screenW * 0.05f, 0f);

        _panel.anchoredPosition = startPos;
        _panel.gameObject.SetActive(true);

        float t = 0f;
        while (t < revealTime)
        {
            t += Time.deltaTime;
            float p = curve.Evaluate(Mathf.Clamp01(t / revealTime));
            _panel.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, p);
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

    // ── Budowanie overlaya ────────────────────────────────────────────────────

    RectTransform BuildOverlay()
    {
        // Canvas na wierzchu wszystkiego
        var canvasGO = new GameObject("TransitionCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas           = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 9999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);

        // Panel – szerszy niż ekran żeby kąt nie odsłaniał narożników
        _screenW = Screen.width * 1.6f;
        float screenH = Screen.height * 1.6f;

        var panelGO    = new GameObject("TransitionPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var img        = panelGO.AddComponent<Image>();
        img.color      = Color.black;
        img.raycastTarget = false;

        var rt             = panelGO.GetComponent<RectTransform>();
        rt.sizeDelta       = new Vector2(_screenW, screenH);
        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation   = Quaternion.Euler(0f, 0f, angle);

        panelGO.SetActive(false);
        return rt;
    }
}
