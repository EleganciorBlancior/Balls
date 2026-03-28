using System.Collections.Generic;
using UnityEngine;

// ── Tier areny – rozmiar przez skalowanie, nie przez fizyczny rozmiar ────────
[System.Serializable]
public class ArenaTier
{
    public string tierName;
    public float  ballScaleMultiplier;   // 1.0 = normalna kulka, 0.5 = połowa
    public float  backgroundScale;       // skala obrazka tła (efekt "większej" areny)
    public int    upgradeCost;

    [Header("Edytowalne w Inspectorze")]
    public int maxExtraBalls;            // ile dodatkowych kulek można kupić
}

// ── Dane o scalonej kulce ─────────────────────────────────────────────────────
[System.Serializable]
public class MergedBallData
{
    public BallClass ballClass;
    public int       mergeLevel = 1;    // ile razy scalona (1 = 5 kulek, 2 = 25 itd.)
    public float     statMultiplier => Mathf.Pow(5f, mergeLevel);
    public int       goldMultiplier => (int)Mathf.Pow(5f, mergeLevel);
}

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    [HideInInspector] public int  gold           = 0;
    [HideInInspector] public int  arenaTierIndex = 0;

    // Kulki dokupione w sklepie (zwykłe, po 1)
    [HideInInspector] public List<BallClass>      purchasedBalls = new List<BallClass>();

    // Scalone kulki (każda = 5x statystyki)
    [HideInInspector] public List<MergedBallData> mergedBalls    = new List<MergedBallData>();

    // ── Tiery areny ──────────────────────────────────────────────────────────
    private static ArenaTier[] _defaultTiers;

    public static ArenaTier[] ArenaTiers
    {
        get
        {
            if (_defaultTiers == null) InitDefaultTiers();
            return _defaultTiers;
        }
    }

    static void InitDefaultTiers()
    {
        _defaultTiers = new ArenaTier[]
        {
            new ArenaTier { tierName="Mała (S)",    ballScaleMultiplier=1.000f, backgroundScale=18f, upgradeCost=  80, maxExtraBalls= 3 },
            new ArenaTier { tierName="Średnia (M)", ballScaleMultiplier=0.875f, backgroundScale=24f, upgradeCost= 150, maxExtraBalls= 9 },
            new ArenaTier { tierName="Duża (L)",    ballScaleMultiplier=0.750f, backgroundScale=32f, upgradeCost= 250, maxExtraBalls=15 },
            new ArenaTier { tierName="Gigant (XL)", ballScaleMultiplier=0.625f, backgroundScale=42f, upgradeCost=1250, maxExtraBalls=21 },
            new ArenaTier { tierName="Twoja matka (XXL)", ballScaleMultiplier=0.5f, backgroundScale=54f, upgradeCost=2500, maxExtraBalls=27 },
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
            default:                     return  50;
        }
    }

    // ── Pomocnicze ────────────────────────────────────────────────────────────
    /// <summary>Ile kulek danej klasy gracz posiada (purchased + merged)</summary>
    public int CountBallsOfClass(BallClass cls)
    {
        int count = 0;
        if (cls == BallClass.Warrior || cls == BallClass.Mage || cls == BallClass.Archer
            || cls == BallClass.Rogue || cls == BallClass.Paladin)
            count = 1;
        foreach (var b in purchasedBalls)
            if (b == cls) count++;
        return count;
    }

    /// <summary>Czy gracz może scalić 5 kulek danej klasy?</summary>
    public bool CanMerge(BallClass cls) => CountBallsOfClass(cls) >= 5;

    /// <summary>Scal 5 kulek danej klasy w jedną super-kulkę</summary>
    public bool TryMerge(BallClass cls)
    {
        if (!CanMerge(cls)) return false;
        int removed = 0;
        for (int i = purchasedBalls.Count - 1; i >= 0 && removed < 5; i--)
            if (purchasedBalls[i] == cls) { purchasedBalls.RemoveAt(i); removed++; }

        // Sprawdź czy jest już scalona kulka tej klasy – zwiększ level
        var existing = mergedBalls.Find(m => m.ballClass == cls);
        if (existing != null)
            existing.mergeLevel++;
        else
            mergedBalls.Add(new MergedBallData { ballClass = cls, mergeLevel = 1 });

        return true;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
