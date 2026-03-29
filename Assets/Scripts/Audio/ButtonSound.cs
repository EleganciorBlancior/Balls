// ButtonSound.cs
// Dodaj ten komponent do każdego przycisku (lub do prefabu wiersza listy).
// Automatycznie podpina się pod Button.onClick i gra dźwięk kliknięcia.
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private void Awake()
        => GetComponent<Button>().onClick.AddListener(() => AudioController.Instance?.PlayButtonClick());
}
