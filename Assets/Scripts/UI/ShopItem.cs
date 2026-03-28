// ShopItem.cs – obsługuje: kulkę, arenę, nagłówek, merge, merged-info
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    [Header("UI w prefabie")]
    public Image    iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public Button   actionButton;
    public TMP_Text buttonLabel;
    public TMP_Text priceLabel;

    private ShopUI  shop;
    private MergeUI mergeUI;
    public MergedBallData ballData;

    // ── Kulka (sklep) ─────────────────────────────────────────────────────────
    public void SetupBall(ClassConfig cfg, ShopUI shopUI)
    {
        shop = shopUI;
        int price = GameData.GetBallPrice(cfg.ballClass);
        if (priceLabel != null) priceLabel.text = "" + price;
        SetIcon(cfg.color);
        SetTexts(cfg.className, GetClassDesc(cfg.ballClass));
        SetButton(price + "", () => shop.TryBuyBall(cfg));
    }

    // ── Arena (sklep) ─────────────────────────────────────────────────────────
    public void SetupArena(ArenaTier tier, ShopUI shopUI)
    {
        shop = shopUI;
        SetIcon(new Color(0.8f, 0.6f, 0.1f));
        SetTexts("Arena " + tier.tierName, "Limit kulek: " + tier.maxExtraBalls);
        int cost = tier.upgradeCost;
        SetButton(cost + "", () => shop.TryUpgradeArena(tier, cost));
        if (priceLabel != null) priceLabel.text = "" + cost;
    }

    // ── Merge: dostępne do scalenia ──────────────────────────────────────────
    public void SetupMerge(ClassConfig cfg, int owned, MergeUI mergeUIRef)
    {
        mergeUI = mergeUIRef;
        SetIcon(cfg.color);

        bool canMerge = owned >= 5;
        string progress = owned + "/5";
        SetTexts(cfg.className, GetClassDesc(cfg.ballClass));
        if (canMerge)
        {
            SetButton("SCAL (5x)", () => mergeUI.TryMerge(cfg.ballClass, cfg));
            if (priceLabel != null) priceLabel.text = "SCAL";
        }
        else
        {
            if (actionButton != null) actionButton.interactable = false;
            if (priceLabel   != null) priceLabel.text = progress;
        }

        // Pasek postępu kolorem
        if (iconImage != null)
            iconImage.color = canMerge ? new Color(1f, 0.85f, 0.1f) : cfg.color;
    }

    // ── Merge: już scalona kulka (info) ──────────────────────────────────────
    public void SetupMergedInfo(ClassConfig cfg, MergedBallData merged, MergeUI mergeUIRef)
    {
        mergeUI = mergeUIRef;
        SetIcon(new Color(1f, 0.85f, 0.1f)); // złoty
        int mult = (int)merged.statMultiplier;
        SetTexts("SUPER " + cfg.className,
                 mult + "x statystyki | " + (20 * merged.goldMultiplier) + " złota za zabicie");

        // Opcja dalszego scalania jeśli mamy kolejne 5
        bool canMergeAgain = GameData.Instance.CountBallsOfClass(cfg.ballClass) >= 5;
        if (canMergeAgain)
            SetButton("SCAL DALEJ", () => mergeUI.TryMerge(cfg.ballClass, cfg));
        else
        {
            if (actionButton != null) actionButton.interactable = false;
            if (buttonLabel  != null) buttonLabel.text = "Poziom " + merged.mergeLevel;
        }
    }

    // ── Merge: upgrade scalonych kulek ──────────────────────────────────────
    public void SetupMergeUp(ClassConfig cfg, int fromLevel, int owned, MergeUI mergeUIRef)
    {
        mergeUI = mergeUIRef;
        bool canMerge = owned >= 5;
        Color iconCol = canMerge ? new Color(1f, 0.85f, 0.1f) : new Color(0.8f, 0.6f, 0.1f);
        SetIcon(iconCol);
        SetTexts(cfg.className + "  Poz." + fromLevel + " → Poz." + (fromLevel + 1),
                 owned + "/5 kulek Poz." + fromLevel);
        if (canMerge)
            SetButton("ULEPSZ", () => mergeUI.TryMergeUp(cfg.ballClass, cfg, fromLevel));
        else
        {
            if (actionButton != null) actionButton.interactable = false;
            if (priceLabel   != null) priceLabel.text           = owned + "/5";
        }
        if (iconImage != null) iconImage.color = iconCol;
    }

    // ── Nagłówek sekcji ───────────────────────────────────────────────────────
    public void SetupHeader(string title)
    {
        if (iconImage    != null) iconImage.enabled             = false;
        if (nameText     != null) nameText.text                  = "";
        if (descText     != null) descText.text                  = title;
        if (actionButton != null) actionButton.gameObject.SetActive(false);
        if (priceLabel   != null) priceLabel.text               = "";
        var bg = GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void SetIcon(Color col)
    {
        if (iconImage != null) { iconImage.color = col; iconImage.enabled = true; }
    }

    void SetTexts(string name, string desc)
    {
        if (nameText != null) nameText.text = name;
        if (descText != null) descText.text = desc;
    }

    void SetButton(string label, UnityEngine.Events.UnityAction action)
    {
        if (buttonLabel  != null) buttonLabel.text = label;
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(action);
            actionButton.interactable = true;
        }
    }

    string GetClassDesc(BallClass cls)
    {
        switch (cls)
        {
            case BallClass.Warrior:      return "Jebnie jak Darek Kaśce";
            case BallClass.Mage:         return "Hustler, mefe sprzedaje kilogramami";
            case BallClass.Archer:       return "Ratatata skurwysynu";
            case BallClass.Rogue:        return "Mały ale wariat";
            case BallClass.Paladin:      return "Pierdolony zawadiaka";
            case BallClass.Berserker:    return "Jak ugryzie będzie ślad";
            case BallClass.Necromancer:  return "Pawulonik 5mg";
            case BallClass.Elementalist: return "Spierdolił z monaru";
            case BallClass.Priest:       return "Żył w celibacie, poddał się po 3 dniu";
            case BallClass.Titan:        return "Twoja stara";
            case BallClass.Nerd:         return "Syn koleżanki twojej starej";
            case BallClass.Glitch:       return "Podobno jegoprawdziwe imie to feature";
            case BallClass.Druid:        return "Zjednoczył emiraty arabskie";
            case BallClass.Psychic:      return "Pojebał cukier z solą i dosypał cyjanku do kawy";
            case BallClass.Technician:   return "Zdał zawodowe, chłopak twojej żony";
            default: return "";
        }
    }
}
