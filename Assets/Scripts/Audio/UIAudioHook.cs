// UIAudioHook.cs
// Wrzuć jeden egzemplarz na dowolny GameObject w każdej scenie z UI.
// Na Start() automatycznie dodaje ButtonSound do każdego Button w scenie.
// Wywołaj UIAudioHook.HookButton(btn) dla przycisków tworzonych dynamicznie.
using UnityEngine;
using UnityEngine.UI;

public class UIAudioHook : MonoBehaviour
{
    private void Start() => HookAll();

    public static void HookButton(Button btn)
    {
        if (btn != null && btn.GetComponent<ButtonSound>() == null)
            btn.gameObject.AddComponent<ButtonSound>();
    }

    void HookAll()
    {
        foreach (var btn in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            HookButton(btn);
    }
}
