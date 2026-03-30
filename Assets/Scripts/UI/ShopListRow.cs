// ShopListRow.cs
// Prosty prefab wiersza w akordeonowej liście po lewej stronie (sklep / merge).
// Prefab potrzebuje:
//   root: Button, HorizontalLayoutGroup, LayoutElement (preferredHeight = 48)
//   ├── Dot:   Image (36×36), LayoutElement flexible=0
//   ├── Label: TMP_Text, LayoutElement flexible=1
//   └── Badge: TMP_Text (right-aligned, gray), LayoutElement flexible=0
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopListRow : MonoBehaviour
{
    public Image      dot;
    public TMP_Text   label;
    public TMP_Text   badge;
    public Image      background;
    [HideInInspector] public Button     btn;
    [HideInInspector] public GameObject detailView;

    private static readonly Color BG_READY  = new Color(1f, 0.85f, 0.1f, 0.18f);
    private static readonly Color BG_NORMAL = new Color(0f, 0f, 0f, 0f);

    // Wywoływane jawnie przez SpawnRow – nie polega na Awake (który nie odpala
    // gdy parent jest nieaktywny w momencie Instantiate).
    public void Init(GameObject dv)
    {
        detailView = dv;

        btn = GetComponent<Button>();
        if (btn == null) btn = GetComponentInChildren<Button>(true);
        if (btn == null)
        {
            var img = GetComponent<Image>();
            if (img == null) { img = gameObject.AddComponent<Image>(); img.color = Color.clear; }
            img.raycastTarget = true;
            btn = gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
        }

        btn.onClick.AddListener(() => { if (detailView != null) detailView.SetActive(true); });
    }

    public void Setup(Color dotColor, string labelText, string badgeText,
                      bool interactable = true, bool readyToMerge = false)
    {
        if (dot        != null) dot.color        = dotColor;
        if (label      != null) label.text       = labelText;
        if (badge      != null) badge.text       = badgeText;
        if (btn        != null) btn.interactable = interactable;
        if (background != null) background.color = readyToMerge ? BG_READY : BG_NORMAL;
    }
}
