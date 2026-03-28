// MainMenuUI.cs
// Podepnij na Canvas w scenie MainMenu
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Przyciski")]
    public GameObject playButton;
    public GameObject quitButton;

    [Header("Animacja tytułu (opcjonalne)")]
    public RectTransform titleRect;

    private float titleBobTimer = 0f;

    private void Start()
    {
        // Upewnij się że GameData istnieje
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }
    }

    private void Update()
    {
        // Delikatne bujanie tytułu
        if (titleRect != null)
        {
            titleBobTimer += Time.deltaTime;
            titleRect.anchoredPosition = new Vector2(
                titleRect.anchoredPosition.x,
                (Mathf.Sin(titleBobTimer * 1.5f) * 16f) - 250);
        }
    }

    public void OnPlayClicked()
    {
        SceneManager.LoadScene("ShopScene");
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
