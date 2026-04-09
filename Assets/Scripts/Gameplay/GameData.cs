using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

    [HideInInspector] public ObfuscatedInt gold   = 100;
    [HideInInspector] public int  arenaTierIndex = 0;

    [HideInInspector] public List<BallClass>         purchasedBalls      = new List<BallClass>();
    [HideInInspector] public List<MergedBallData>    mergedBalls         = new List<MergedBallData>();
    [HideInInspector] public List<BallClass>         consumedBaseBalls   = new List<BallClass>();
    [HideInInspector] public List<BallCustomization> ballCustomizations  = new List<BallCustomization>();
    [HideInInspector] public bool                    paintShopUnlocked   = false;

    // ── Ustawienia ────────────────────────────────────────────────────────────
    [HideInInspector] public float        sfxVolume       = 1f;
    [HideInInspector] public float        musicVolume     = 0.4f;
    [HideInInspector] public int          qualityLevel    = 1;
    [HideInInspector] public GameLanguage language        = GameLanguage.EN;
    [HideInInspector] public int          resolutionIndex = -1;

    // ── Ulepszenie Pulla ──────────────────────────────────────────────────────
    [HideInInspector] public int pullUpgradeLevel = 0;
    public const int PULL_MAX_LEVEL = 4;

    public static int GetPullUpgradeCost(int currentLevel)
    {
        switch (currentLevel)
        {
            case 0: return 100;
            case 1: return 2000;
            case 2: return 40000;
            case 3: return 120000;
            default: return 0;
        }
    }

    public static float GetPullForce(int level)    => 18f + level * 8f;
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
            new ArenaTier { tierName="Mała (S)",           ballScaleMultiplier=1.000f, backgroundScale=18f, upgradeCost=   80, maxExtraBalls=3    },
            new ArenaTier { tierName="Średnia (M)",        ballScaleMultiplier=0.875f, backgroundScale=24f, upgradeCost=  150, maxExtraBalls=23   },
            new ArenaTier { tierName="Duża (L)",           ballScaleMultiplier=0.750f, backgroundScale=32f, upgradeCost= 1250, maxExtraBalls=123  },
            new ArenaTier { tierName="Gigant (XL)",        ballScaleMultiplier=0.333f, backgroundScale=42f, upgradeCost= 7500, maxExtraBalls=623  },
            new ArenaTier { tierName="Olbrzym (XXL)",      ballScaleMultiplier=0.149f, backgroundScale=54f, upgradeCost=50000, maxExtraBalls=3123 },
            new ArenaTier { tierName="Twoja stara (XXXL)", ballScaleMultiplier=0.083f, backgroundScale=68f, upgradeCost=250000, maxExtraBalls=9999},
        };
    }

    public ArenaTier CurrentTier => ArenaTiers[arenaTierIndex];
    public bool      IsMaxTier   => arenaTierIndex >= ArenaTiers.Length - 1;

    // ── Ceny klas ─────────────────────────────────────────────────────────────
    public static int GetMergedBallRefund(BallClass cls, int mergeLevel)
    {
        int total = (int)Mathf.Pow(5, mergeLevel);
        if (IsBaseClass(cls)) total -= 1;  // jedna kulka bazowa jest darmowa
        return total * GetBallPrice(cls);
    }

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

    /// <summary>Łączna liczba kulek gracza: bazowe (niezużyte) + kupione + scalone.</summary>
    public int TotalOwnedBalls()
    {
        int count = BASE_CLASSES.Length - consumedBaseBalls.Count;
        count += purchasedBalls.Count;
        count += mergedBalls.Count;
        return count;
    }

    // Aliasy dla starych wywołań
    public int  CountBallsOfClass(BallClass cls) => CountBasicBalls(cls);
    public bool CanMerge(BallClass cls)           => CanMergeBasic(cls);
    public bool TryMerge(BallClass cls)           => TryMergeBasic(cls);

    // ── Scalanie ──────────────────────────────────────────────────────────────
    /// <summary>Czy tier areny pozwala na scalenie na dany poziom (1 = tier 1, itd.).</summary>
    public bool HasMergeTier(int mergeLevel = 1) => arenaTierIndex >= mergeLevel;

    public bool CanMergeBasic(BallClass cls) => HasMergeTier(1) && CountBasicBalls(cls) >= 5;
    public bool CanMergeUp(BallClass cls, int fromLevel)
    {
        if (!HasMergeTier(fromLevel + 1)) return false;
        if (CountMergedOfLevel(cls, fromLevel) < 5) return false;
        if (fromLevel == 4 && CountMergedOfLevel(cls, 5) >= 1) return false;  // max 1x Lv5
        return true;
    }

    /// <summary>Scal 5 bazowych kulek klasy → 1 kulka Poz.1</summary>
    public bool TryMergeBasic(BallClass cls)
    {
        if (!CanMergeBasic(cls)) return false;
        int toRemove = 5;
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

    // ── Stan scen ─────────────────────────────────────────────────────────────

    // ShopScene
    [HideInInspector] public bool      shopBallsOpen    = false;
    [HideInInspector] public bool      shopUpgradesOpen = false;
    [HideInInspector] public int       shopSelType      = 0;   // 0=none,1=ball,2=arena,3=pull
    [HideInInspector] public BallClass shopSelClass     = BallClass.Warrior;
    [HideInInspector] public int       shopSelTierIdx   = 0;

    // MergeScene
    [HideInInspector] public bool      mergeBasicOpen   = false;
    [HideInInspector] public bool      mergeUpgradeOpen = false;
    [HideInInspector] public bool      mergeOwnedOpen   = false;
    [HideInInspector] public int       mergeSelType     = 0;   // 0=none,1=basic,2=upgrade,3=owned
    [HideInInspector] public BallClass mergeSelClass    = BallClass.Warrior;
    [HideInInspector] public int       mergeSelFromLevel= 1;

    // PaintScene
    [HideInInspector] public bool      paintHasSelection  = false;
    [HideInInspector] public BallClass paintSelClass      = BallClass.Warrior;

    // Nawigacja Merge → Shop (gdy brakuje kulek do scalenia)
    [HideInInspector] public bool      shopPendingBall      = false;
    [HideInInspector] public BallClass shopPendingBallClass = BallClass.Warrior;

    // ── Korony (Crown tracking) ───────────────────────────────────────────────
    public const float CROWN_DMG_DEALT_THRESHOLD = 3000000f;
    public const float CROWN_DMG_TAKEN_THRESHOLD = 1500000f;
    public const int   CROWN_WINS_THRESHOLD      = 50;

    [System.Serializable]
    public class ClassCrowns
    {
        public BallClass ballClass;
        public float     totalDamageDealt;
        public float     totalDamageTaken;
        public int       totalWins;
        public bool      crownDamageDealt;
        public bool      crownDamageTaken;
        public bool      crownWins;
        public bool      HasMastery => crownDamageDealt && crownDamageTaken && crownWins;
    }

    [HideInInspector] public List<ClassCrowns> classCrowns = new List<ClassCrowns>();

    public ClassCrowns GetOrCreateCrowns(BallClass cls)
    {
        var c = classCrowns.Find(x => x.ballClass == cls);
        if (c == null) { c = new ClassCrowns { ballClass = cls }; classCrowns.Add(c); }
        return c;
    }

    public bool HasMastery(BallClass cls) => GetOrCreateCrowns(cls).HasMastery;

    public void RecordDamageDealt(BallClass cls, float dmg)
    {
        var c = GetOrCreateCrowns(cls);
        if (c.crownDamageDealt) return;
        c.totalDamageDealt += dmg;
        if (c.totalDamageDealt >= CROWN_DMG_DEALT_THRESHOLD)
        { c.totalDamageDealt = CROWN_DMG_DEALT_THRESHOLD; c.crownDamageDealt = true; }
    }

    public void RecordDamageTaken(BallClass cls, float dmg)
    {
        var c = GetOrCreateCrowns(cls);
        if (c.crownDamageTaken) return;
        c.totalDamageTaken += dmg;
        if (c.totalDamageTaken >= CROWN_DMG_TAKEN_THRESHOLD)
        { c.totalDamageTaken = CROWN_DMG_TAKEN_THRESHOLD; c.crownDamageTaken = true; }
    }

    public void RecordWin(BallClass cls)
    {
        var c = GetOrCreateCrowns(cls);
        if (c.crownWins) return;
        c.totalWins++;
        if (c.totalWins >= CROWN_WINS_THRESHOLD)
        { c.totalWins = CROWN_WINS_THRESHOLD; c.crownWins = true; }
    }

    // ── Zapis / Odczyt ────────────────────────────────────────────────────────
    static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    public void Save()
    {
        try
        {
            var sd = new SaveData(this);
            File.WriteAllText(SavePath, JsonUtility.ToJson(sd, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GameData] Save failed: " + e.Message);
        }
    }

    public bool Load()
    {
        if (!File.Exists(SavePath)) return false;
        try
        {
            var sd = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (!sd.VerifyChecksum())
            {
                Debug.LogWarning("[GameData] Save file tampered — resetting.");
                ResetSave();
                return false;
            }
            sd.ApplyTo(this);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GameData] Load failed: " + e.Message);
            return false;
        }
    }

    public static bool SaveExists() => File.Exists(SavePath);

    public void ResetSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);

        gold               = 100;
        arenaTierIndex     = 0;
        paintShopUnlocked  = false;
        sfxVolume          = 1f;
        musicVolume        = 0.4f;
        qualityLevel       = 1;
        language           = GameLanguage.EN;
        pullUpgradeLevel   = 0;
        purchasedBalls     = new List<BallClass>();
        mergedBalls        = new List<MergedBallData>();
        consumedBaseBalls  = new List<BallClass>();
        ballCustomizations = new List<BallCustomization>();
        classCrowns        = new List<ClassCrowns>();

        shopBallsOpen    = false;
        shopUpgradesOpen = false;
        shopSelType      = 0;
        mergeBasicOpen   = false;
        mergeUpgradeOpen = false;
        mergeOwnedOpen   = false;
        mergeSelType     = 0;
        paintHasSelection= false;
        shopPendingBall  = false;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnApplicationQuit()   => Save();
    private void OnApplicationPause(bool paused) { if (paused) Save(); }
}

// ── Dane zapisu ───────────────────────────────────────────────────────────────
[System.Serializable]
public class SaveData
{
    public int   gold;
    public int   arenaTierIndex;
    public bool  paintShopUnlocked;
    public float sfxVolume;
    public float musicVolume;
    public int   qualityLevel;
    public GameLanguage language;
    public int   pullUpgradeLevel;
    public int   resolutionIndex;

    public List<BallClass>         purchasedBalls;
    public List<MergedBallData>    mergedBalls;
    public List<BallClass>         consumedBaseBalls;
    public List<BallCustomization> ballCustomizations;
    public List<GameData.ClassCrowns> classCrowns;

    public string checksum;

    public SaveData() { }

    public SaveData(GameData d)
    {
        gold               = d.gold;
        arenaTierIndex     = d.arenaTierIndex;
        paintShopUnlocked  = d.paintShopUnlocked;
        sfxVolume          = d.sfxVolume;
        musicVolume        = d.musicVolume;
        qualityLevel       = d.qualityLevel;
        language           = d.language;
        pullUpgradeLevel   = d.pullUpgradeLevel;
        resolutionIndex    = d.resolutionIndex;
        purchasedBalls     = new List<BallClass>(d.purchasedBalls);
        mergedBalls        = new List<MergedBallData>(d.mergedBalls);
        consumedBaseBalls  = new List<BallClass>(d.consumedBaseBalls);
        ballCustomizations = new List<BallCustomization>(d.ballCustomizations);
        classCrowns        = new List<GameData.ClassCrowns>(d.classCrowns);
        checksum           = ComputeChecksum();
    }

    public bool VerifyChecksum() => checksum == ComputeChecksum();

    public void ApplyTo(GameData d)
    {
        d.gold               = gold;
        d.arenaTierIndex     = arenaTierIndex;
        d.paintShopUnlocked  = paintShopUnlocked;
        d.sfxVolume          = sfxVolume;
        d.musicVolume        = musicVolume;
        d.qualityLevel       = qualityLevel;
        d.language           = language;
        d.pullUpgradeLevel   = pullUpgradeLevel;
        d.resolutionIndex    = resolutionIndex;
        d.purchasedBalls     = purchasedBalls     ?? new List<BallClass>();
        d.mergedBalls        = mergedBalls        ?? new List<MergedBallData>();
        d.consumedBaseBalls  = consumedBaseBalls  ?? new List<BallClass>();
        d.ballCustomizations = ballCustomizations ?? new List<BallCustomization>();
        d.classCrowns        = classCrowns        ?? new List<GameData.ClassCrowns>();
    }

    private static readonly byte[] _hmacKey = Encoding.UTF8.GetBytes("b4LLs_s3cR3t_K3y!@#2026");

    private string ComputeChecksum()
    {
        var sb = new StringBuilder(256);
        sb.Append(gold).Append('|');
        sb.Append(arenaTierIndex).Append('|');
        sb.Append(paintShopUnlocked).Append('|');
        sb.Append(pullUpgradeLevel).Append('|');

        if (purchasedBalls != null)
            foreach (var b in purchasedBalls) sb.Append((int)b).Append(',');
        sb.Append('|');

        if (mergedBalls != null)
            foreach (var m in mergedBalls) sb.Append((int)m.ballClass).Append(':').Append(m.mergeLevel).Append(',');
        sb.Append('|');

        if (consumedBaseBalls != null)
            foreach (var b in consumedBaseBalls) sb.Append((int)b).Append(',');

        using (var hmac = new HMACSHA256(_hmacKey))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return System.Convert.ToBase64String(hash);
        }
    }
}
