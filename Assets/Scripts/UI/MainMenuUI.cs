// MainMenuUI.cs
// Podepnij na Canvas w scenie MainMenu
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Przyciski")]
    public GameObject playButton;
    public GameObject quitButton;
    public Button     paintButton;
    public TMP_Text   paintButtonLabel;
    public Button     settingsButton;

    // Opcjonalne referencje do tekstów przycisków (jeśli TMP_Text nie jest bezpośrednim dzieckiem)
    [Header("Teksty przycisków (opcjonalne)")]
    public TMP_Text playButtonLabel;
    public TMP_Text quitButtonLabel;
    public TMP_Text settingsButtonLabel;

    [Header("Animacja tytułu (opcjonalne)")]
    public RectTransform titleRect;

    private float titleBobTimer = 0f;

    private void Start()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }

        LocalizationManager.SetLanguage(GameData.Instance.language);
        AudioController.Instance?.SetSFXVolume(GameData.Instance.sfxVolume);
        AudioController.Instance?.SetMusicVolume(GameData.Instance.musicVolume);
        QualitySettings.SetQualityLevel(GameData.Instance.qualityLevel, true);

        RefreshLabels();
    }

    void RefreshLabels()
    {
        // Przyciski z opcjonalnymi referencjami
        if (playButtonLabel     != null) playButtonLabel.text     = LocalizationManager.MainMenuPlay;
        if (quitButtonLabel     != null) quitButtonLabel.text     = LocalizationManager.MainMenuQuit;
        if (settingsButtonLabel != null) settingsButtonLabel.text = LocalizationManager.MainMenuSettings;

        RefreshPaintButton();
    }

    void RefreshPaintButton()
    {
        if (paintButton == null) return;
        paintButton.interactable = true;
        if (paintButtonLabel != null)
            paintButtonLabel.text = GameData.Instance.paintShopUnlocked
                ? LocalizationManager.MainMenuPaint
                : LocalizationManager.MainMenuPaintBuy;
    }

    private void Update()
    {
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
        SceneTransition.ExitTo("ShopScene");
    }

    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnPaintClicked()
    {
        if (GameData.Instance == null) return;

        if (GameData.Instance.paintShopUnlocked)
        {
            SceneTransition.ExitTo("PaintScene");
            return;
        }

        if (GameData.Instance.gold < GameData.PAINT_SHOP_COST) return;

        GameData.Instance.gold -= GameData.PAINT_SHOP_COST;
        GameData.Instance.paintShopUnlocked = true;
        GameData.Instance.Save();
        RefreshPaintButton();
        SceneTransition.ExitTo("PaintScene");
    }

    public void OnSettingsClicked()
    {
        SettingsPanel.Open(() =>
        {
            RefreshLabels();
        });
    }
}
