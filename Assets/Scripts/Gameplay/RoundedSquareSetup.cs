// RoundedSquareSetup.cs
// Podepnij na każdy Image który używa shadera Custom/roundedSquare.
// Automatycznie:
//   - tworzy osobną instancję materiału (żeby _scale każdego przycisku był niezależny)
//   - ustawia _scale co klatkę na podstawie aktualnego rozmiaru RectTransform
//
// [ExecuteAlways] = działa też w edytorze, więc od razu widać zaokrąglenia.
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class RoundedSquareSetup : MonoBehaviour
{
    private Image         _image;
    private RectTransform _rt;
    private Material      _mat;
    private Vector2       _lastSize;

    private void Awake()    => Init();
    private void OnEnable() => Init();

    void Init()
    {
        _image = GetComponent<Image>();
        _rt    = GetComponent<RectTransform>();
        if (_image == null || _image.material == null) return;

        // Każdy Image dostaje własną kopię materiału
        // (Instantiate żeby nie modyfikować oryginału w Assets)
        if (_mat == null || _mat.name.Contains("(Instance)") == false)
        {
            _mat         = new Material(_image.material);
            _mat.name    = _image.material.name + " (Instance)";
            _image.material = _mat;
        }
    }

    private void LateUpdate()
    {
        if (_mat == null || _rt == null) return;

        Vector2 size = _rt.rect.size;

        // Aktualizuj tylko gdy rozmiar się zmienił (optymalizacja)
        if (size == _lastSize) return;
        _lastSize = size;

        _mat.SetVector("_scale", new Vector4(size.x, size.y, 0f, 0f));
    }

    private void OnDestroy()
    {
        // Wyczyść instancję materiału żeby nie zostawiać śmieci w pamięci
        if (_mat != null && Application.isPlaying)
            Destroy(_mat);
    }

#if UNITY_EDITOR
    private void OnValidate() => LateUpdate();
#endif
}
