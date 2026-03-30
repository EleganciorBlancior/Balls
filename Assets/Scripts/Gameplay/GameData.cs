using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BallCustomization
{
    public BallClass   ballClass;
    public Color       color1      = Color.white;
    public Color       color2      = Color.white;
    public Color       color3      = Color.white;
    public BallPattern pattern     = BallPattern.Solid;
    public int         stripeCount = 5;
}

[System.Serializable]
public class ArenaTier
{
    public string tierName;
    public float  ballScaleMultiplier;
    public float  backgroundScale;
    public int    upgradeCost;
    [Header("Edytowalne w Inspectorze")]
    public int maxExtraBalls;
}

[System.Serializable]
public class MergedBallData
{
    public BallClass ballClass;
    public int       mergeLevel = 1;
    public float     statMultiplier => Mathf.Pow(5f, mergeLevel);
    public int       goldMultiplier => (int)Mathf.Pow(5f, mergeLevel);
}

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    [HideInInspector] public int  gold           = 100;
    [HideInInspector] public int  arenaTierIndex = 0;

    [HideInInspector] public List<BallClass>         purchasedBalls      = new List<BallClass>();
    [HideInInspector] public List<MergedBallData>    mergedBalls         = new List<MergedBallData>();
    [HideInInspector] public List<BallClass>         consumedBaseBalls   = new List<BallClass>();
    [HideInInspector] public List<BallCustomization> ballCustomizations  = new List<BallCustomization>();
    [HideInInspector] public bool                    paintShopUnlocked   = false;

    // ── Ustawienia ────────────────────────────────────────────────────────────
    [HideInInspector] public float        sfxVolume    = 1f;
    [HideInInspector] public float        musicVolume  = 0.4f;
    [HideInInspector] public int          qualityLevel = 1;
    [HideInInspector] public GameLanguage language     = GameLanguage.PL;

    // ── Ulepszenie Pulla ──────────────────────────────────────────────────────
    [HideInInspector] public int pullUpgradeLevel = 0;
    public const int PULL_MAX_LEVEL = 4;

    public static int GetPullUpgradeCost(int currentLevel)
    {
        switch (currentLevel)
        {
            case 0: return 100;
            case 1: return 200;
            case 2: return 400;
            case 3: return 800;
            default: return 0;
        }
    }

    // Siła impulsu przy TriggerCenterPull
    public static float GetPullForce(int level)    => 18f + level * 8f;
    // Czas trwania ciągłego przyciągania po impulsie (sekundy)
    public static float GetPullDuration(int level) => level * 0.6f;

    public const int PAINT_SHOP_COST = 300;

    static readonly BallClass[] BASE_CLASSES =
    {
        BallClass.Warrior, BallClass.Mage, BallClass.Archer,
        BallClass.Rogue,   BallClass.Paladin
    };

    public static bool IsBaseClass(BallClass cls)
    {
        foreach (var b in BASE_CLASSES) if (b == cls) return true;
        return false;
    }

    // ── Tiery areny ──────────────────────────────────────────────────────────
    private static ArenaTier[] _defaultTiers;
    public static ArenaTier[] ArenaTiers
    {
        get { if (_defaultTiers == null) InitDefaultTiers(); return _defaultTiers; }
    }

    static void InitDefaultTiers()
    {
        _defaultTiers = new ArenaTier[]
        {
            new ArenaTier { tierName="Mała (S)",          ballScaleMultiplier=1.000f, backgroundScale=18f, upgradeCost=  80, maxExtraBalls=3  },
            new ArenaTier { tierName="Średnia (M)",       ballScaleMultiplier=0.875f, backgroundScale=24f, upgradeCost= 150, maxExtraBalls=9  },
            new ArenaTier { tierName="Duża (L)",          ballScaleMultiplier=0.750f, backgroundScale=32f, upgradeCost= 250, maxExtraBalls=24  },
            new ArenaTier { tierName="Gigant (XL)",       ballScaleMultiplier=0.625f, backgroundScale=42f, upgradeCost=1250, maxExtraBalls=49  },
            new ArenaTier { tierName="Twoja matka (XXL)", ballScaleMultiplier=0.500f, backgroundScale=54f, upgradeCost=2500, maxExtraBalls=69  },
        };
    }

    public ArenaTier CurrentTier => ArenaTiers[arenaTierIndex];
    public bool      IsMaxTier   => arenaTierIndex >= ArenaTiers.Length - 1;

    // ── Ceny klas ─────────────────────────────────────────────────────────────
    public static int GetBallPrice(BallClass cls)
    {
        switch (cls)
        {
            case BallClass.Warrior:      return  50;
            case BallClass.Mage:         return  70;
            case BallClass.Archer:       return  60;
            case BallClass.Rogue:        return  90;
            case BallClass.Paladin:      return  80;
            case BallClass.Berserker:    return  65;
            case BallClass.Necromancer:  return 100;
            case BallClass.Elementalist: return  85;
            case BallClass.Priest:       return  75;
            case BallClass.Titan:        return 120;
            case BallClass.Druid:        return  95;
            case BallClass.Technician:   return 110;
            case BallClass.Glitch:       return  80;
            case BallClass.Psychic:      return 105;
            case BallClass.Nerd:         return  70;
            case BallClass.Mariachi:     return 130;
            default:                     return  50;
        }
    }

    // ── Liczniki ──────────────────────────────────────────────────────────────
    /// <summary>Ile bazowych (niezeskalowanych) kulek klasy gracz posiada.</summary>
    public int CountBasicBalls(BallClass cls)
    {
        int count = (IsBaseClass(cls) && !consumedBaseBalls.Contains(cls)) ? 1 : 0;
        foreach (var b in purchasedBalls) if (b == cls) count++;
        return count;
    }

    /// <summary>Ile scalonych kulek danej klasy na danym poziomie gracz posiada.</summary>
    public int CountMergedOfLevel(BallClass cls, int level)
    {
        int count = 0;
        foreach (var m in mergedBalls) if (m.ballClass == cls && m.mergeLevel == level) count++;
        return count;
    }

    // Aliasy dla starych wywołań
    public int  CountBallsOfClass(BallClass cls) => CountBasicBalls(cls);
    public bool CanMerge(BallClass cls)           => CanMergeBasic(cls);
    public bool TryMerge(BallClass cls)           => TryMergeBasic(cls);

    // ── Scalanie ──────────────────────────────────────────────────────────────
    public bool CanMergeBasic(BallClass cls) => CountBasicBalls(cls) >= 5;
    public bool CanMergeUp(BallClass cls, int fromLevel) => CountMergedOfLevel(cls, fromLevel) >= 5;

    /// <summary>Scal 5 bazowych kulek klasy → 1 kulka Poz.1</summary>
    public bool TryMergeBasic(BallClass cls)
    {
        if (!CanMergeBasic(cls)) return false;
        int toRemove = 5;
        // Zużyj bazową kulkę jeśli dostępna
        if (IsBaseClass(cls) && !consumedBaseBalls.Contains(cls))
        {
            consumedBaseBalls.Add(cls);
            toRemove--;
        }
        for (int i = purchasedBalls.Count - 1; i >= 0 && toRemove > 0; i--)
            if (purchasedBalls[i] == cls) { purchasedBalls.RemoveAt(i); toRemove--; }
        mergedBalls.Add(new MergedBallData { ballClass = cls, mergeLevel = 1 });
        return true;
    }

    /// <summary>Scal 5 kulek klasy na poziomie fromLevel → 1 kulka Poz.(fromLevel+1)</summary>
    public bool TryMergeUp(BallClass cls, int fromLevel)
    {
        if (!CanMergeUp(cls, fromLevel)) return false;
        int removed = 0;
        for (int i = mergedBalls.Count - 1; i >= 0 && removed < 5; i--)
            if (mergedBalls[i].ballClass == cls && mergedBalls[i].mergeLevel == fromLevel)
            { mergedBalls.RemoveAt(i); removed++; }
        mergedBalls.Add(new MergedBallData { ballClass = cls, mergeLevel = fromLevel + 1 });
        return true;
    }

    // ── Malarnia ──────────────────────────────────────────────────────────────
    public BallCustomization GetCustomization(BallClass cls)
    {
        return ballCustomizations.Find(c => c.ballClass == cls);
    }

    public void SaveCustomization(BallClass cls, Color c1, Color c2, Color c3,
                                   BallPattern pattern, int stripeCount)
    {
        var existing = ballCustomizations.Find(c => c.ballClass == cls);
        if (existing != null)
        {
            existing.color1 = c1; existing.color2 = c2; existing.color3 = c3;
            existing.pattern = pattern; existing.stripeCount = stripeCount;
        }
        else
        {
            ballCustomizations.Add(new BallCustomization
            {
                ballClass   = cls,
                color1      = c1, color2 = c2, color3 = c3,
                pattern     = pattern,
                stripeCount = stripeCount
            });
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
