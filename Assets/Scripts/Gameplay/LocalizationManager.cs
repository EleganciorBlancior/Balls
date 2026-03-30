// LocalizationManager.cs – PL/EN lokalizacja
using UnityEngine;

public enum GameLanguage { PL, EN }

public static class LocalizationManager
{
    public static GameLanguage Language { get; private set; } = GameLanguage.PL;

    public static void SetLanguage(GameLanguage lang) { Language = lang; }

    // ── Ogólne ────────────────────────────────────────────────────────────────
    public static string Back       => Language == GameLanguage.PL ? "Wróć"        : "Back";
    public static string Settings   => Language == GameLanguage.PL ? "Ustawienia"  : "Settings";
    public static string Quit       => Language == GameLanguage.PL ? "WYJDŹ"       : "QUIT";
    public static string Play       => Language == GameLanguage.PL ? "GRAJ"        : "PLAY";
    public static string GoldPrefix => Language == GameLanguage.PL ? "Złoto: "     : "Gold: ";
    public static string LevelPrefix=> Language == GameLanguage.PL ? "Poz."        : "Lv.";

    // ── Sklep ─────────────────────────────────────────────────────────────────
    public static string ShopTitle       => Language == GameLanguage.PL ? "SKLEP"             : "SHOP";
    public static string BallsSection    => Language == GameLanguage.PL ? "Kulki"             : "Balls";
    public static string UpgradesSection => Language == GameLanguage.PL ? "Ulepszenia"        : "Upgrades";
    public static string NotEnoughGold   => Language == GameLanguage.PL ? "Za mało złota!"    : "Not enough gold!";
    public static string FullArena       => Language == GameLanguage.PL ? "Pełna arena!"      : "Arena is full!";
    public static string UpgradeArena    => Language == GameLanguage.PL ? "Ulepsz arenę!"     : "Upgrade arena!";
    public static string ArenaUpgraded   => Language == GameLanguage.PL ? "Arena ulepszona!"  : "Arena upgraded!";
    public static string ArenaMax        => Language == GameLanguage.PL ? "Arena (maks.)"     : "Arena (max)";
    public static string ArenaMoreSpace  => Language == GameLanguage.PL ? "Więcej miejsca. Więcej kulek." : "More space. More balls.";
    public static string BallLimitLabel  => Language == GameLanguage.PL ? "Limit kulek: "     : "Ball limit: ";
    public static string BallScaleLabel  => Language == GameLanguage.PL ? "Skala kulek: "     : "Ball scale: ";
    public static string MaxUpgrade      => Language == GameLanguage.PL ? "Maks. poziom"      : "Max level";
    public static string PullUpgradeTitle=> Language == GameLanguage.PL ? "Ulepszenie Pulla"  : "Pull Upgrade";
    public static string PullUpgraded    => Language == GameLanguage.PL ? "Pull ulepszony!"   : "Pull upgraded!";

    public static string BoughtBall(string name)
        => Language == GameLanguage.PL ? "Kupiono " + name + "!" : "Bought " + name + "!";
    public static string BuyBtn(int price)
        => Language == GameLanguage.PL ? "KUP  –  " + price + "g" : "BUY  –  " + price + "g";
    public static string BuyArenaBtn(int cost)
        => Language == GameLanguage.PL ? "KUP – " + cost + "g" : "BUY – " + cost + "g";
    public static string BuyPullBtn(int cost)
        => Language == GameLanguage.PL ? "KUP – " + cost + "g" : "BUY – " + cost + "g";
    public static string ShopInfoLine(int ballCount, string tierName)
        => Language == GameLanguage.PL
            ? "Kulki: " + ballCount + "\nArena: " + tierName
            : "Balls: " + ballCount + "\nArena: " + tierName;
    public static string PullStats(int level, float force, float duration)
        => Language == GameLanguage.PL
            ? "Siła: " + force.ToString("F0") + "\nCzas: " + duration.ToString("F1") + "s\nPoziom: " + level
            : "Force: " + force.ToString("F0") + "\nDuration: " + duration.ToString("F1") + "s\nLevel: " + level;

    // ── Scalnia ────────────────────────────────────────────────────────────────
    public static string MergeTitle      => Language == GameLanguage.PL ? "SCALNIA"                         : "MERGE";
    public static string MergeBasic      => Language == GameLanguage.PL ? "Scal bazowe"                     : "Merge basic";
    public static string MergeUpgrade    => Language == GameLanguage.PL ? "Ulepsz scalone"                  : "Upgrade merged";
    public static string OwnedMerged     => Language == GameLanguage.PL ? "Twoje scalone"                   : "Your merged";
    public static string BuyBallsHint    => Language == GameLanguage.PL ? "Kup kulki w sklepie"             : "Buy balls in shop";
    public static string NoMergeAvail    => Language == GameLanguage.PL ? "Brak kulek do ulepszenia"        : "No balls to upgrade";
    public static string NoOwnedMerged   => Language == GameLanguage.PL ? "Brak scalonych kulek"            : "No merged balls";
    public static string MergeBtn        => Language == GameLanguage.PL ? "SCAL"                            : "MERGE";
    public static string BuyMoreBalls    => Language == GameLanguage.PL ? "Dokup kule"                      : "Buy more balls";
    public static string MergeInfoLine   => Language == GameLanguage.PL ? "Scal 5 kulek tej samej klasy i poziomu." : "Merge 5 balls of the same class and level.";
    public static string SuperPrefix     => "SUPER ";

    public static string OwnedCount(int owned)
        => Language == GameLanguage.PL ? "Posiadasz: " + owned + "/5\n" : "Owned: " + owned + "/5\n";
    public static string OwnedCountLevel(int level, int owned)
        => Language == GameLanguage.PL ? "Posiadasz " + LevelPrefix + level + ": " + owned + "/5\n" : "Owned " + LevelPrefix + level + ": " + owned + "/5\n";
    public static string MergedStatText(int mult, int goldMult, int mergeLevel)
        => Language == GameLanguage.PL
            ? "Statystyki: x" + mult + "\nZłoto/kill: " + 20 * goldMult + "\nPoziom: " + mergeLevel
            : "Stats: x" + mult + "\nGold/kill: " + 20 * goldMult + "\nLevel: " + mergeLevel;
    public static string CanMergeMore    => Language == GameLanguage.PL ? "Można scalić" : "Can merge more";
    public static string OwnedSingle     => Language == GameLanguage.PL ? "Posiadasz"    : "Owned";
    public static string MergeFlavor     => Language == GameLanguage.PL ? "Ta kula odpiero da fulla"  : "This ball will go hard";
    public static string MergeUpgFlavor  => Language == GameLanguage.PL ? "Scal swoje kulki"          : "Merge your balls";
    public static string SuperFlavor     => Language == GameLanguage.PL ? "Super kula"                : "Super ball";

    // ── Arena ─────────────────────────────────────────────────────────────────
    public static string AliveCount(int alive, int total)
        => Language == GameLanguage.PL ? "Żyje: " + alive + " / " + total : "Alive: " + alive + " / " + total;
    public static string GoldCount(int gold)
        => Language == GameLanguage.PL ? "Złoto: " + gold : "Gold: " + gold;
    public static string ArenaTierLabel(string name)
        => "Arena: " + name;
    public static string Winner(string name)
        => Language == GameLanguage.PL ? "Wygrał: " + name + "!" : "Winner: " + name + "!";
    public static string Draw
        => Language == GameLanguage.PL ? "Remis!" : "Draw!";
    public static string BlastBtn        => Language == GameLanguage.PL ? "BLAST"   : "BLAST";
    public static string PullBtn         => Language == GameLanguage.PL ? "PULL"    : "PULL";

    // ── Ustawienia ────────────────────────────────────────────────────────────
    public static string SettingsTitle   => Language == GameLanguage.PL ? "USTAWIENIA"      : "SETTINGS";
    public static string SFXVolumeLabel  => Language == GameLanguage.PL ? "Głośność SFX"    : "SFX Volume";
    public static string MusicVolumeLabel=> Language == GameLanguage.PL ? "Głośność Muzyki" : "Music Volume";
    public static string LanguageLabel   => Language == GameLanguage.PL ? "Język"           : "Language";
    public static string GraphicsLabel   => Language == GameLanguage.PL ? "Grafika"         : "Graphics";
    public static string QualityLow       => Language == GameLanguage.PL ? "Niska"           : "Low";
    public static string QualityMed       => Language == GameLanguage.PL ? "Średnia"         : "Medium";
    public static string QualityHigh      => Language == GameLanguage.PL ? "Wysoka"          : "High";
    public static string ResolutionLabel  => Language == GameLanguage.PL ? "Rozdzielczość"   : "Resolution";

    // ── Flavor texty ─────────────────────────────────────────────────────────
    public static string GetFlavor(BallClass cls)
    {
        if (Language == GameLanguage.EN)
        {
            switch (cls)
            {
                case BallClass.Warrior:      return "Hits like a freight train";
                case BallClass.Mage:         return "Moves product by the kilogram";
                case BallClass.Archer:       return "Ratatata, you know the rest";
                case BallClass.Rogue:        return "Small but absolutely feral";
                case BallClass.Paladin:      return "Holy pain in the ass";
                case BallClass.Berserker:    return "Bite marks included";
                case BallClass.Necromancer:  return "5mg of pure mayhem";
                case BallClass.Elementalist: return "Escaped from rehab";
                case BallClass.Priest:       return "Kept celibacy for 3 days";
                case BallClass.Titan:        return "Your mom";
                case BallClass.Nerd:         return "Your mom's friend's son";
                case BallClass.Glitch:       return "His real name is reportedly Feature";
                case BallClass.Druid:        return "United the Arab Emirates";
                case BallClass.Psychic:      return "Mixed sugar with salt and cyanide";
                case BallClass.Technician:   return "Passed vocational school, dating your wife";
                case BallClass.Mariachi:     return "Finishes everything except the song";
                default:                     return "";
            }
        }
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
            case BallClass.Glitch:       return "Podobno jego prawdziwe imię to feature";
            case BallClass.Druid:        return "Zjednoczył emiraty arabskie";
            case BallClass.Psychic:      return "Pojebał cukier z solą i dosypał cyjanku do kawy";
            case BallClass.Technician:   return "Zdał zawodowe, chłopak twojej żony";
            case BallClass.Mariachi:     return "Wszystko co robi potrafi skończyć, oprócz przyśpiewki";
            default:                     return "";
        }
    }
}
