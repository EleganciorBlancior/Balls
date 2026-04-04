// LocalizationManager.cs – PL/EN lokalizacja
using UnityEngine;

public enum GameLanguage { PL, EN }

public static class LocalizationManager
{
    public static GameLanguage Language { get; private set; } = GameLanguage.EN;

    public static void SetLanguage(GameLanguage lang) { Language = lang; }

    // ── Ogólne ────────────────────────────────────────────────────────────────
    public static string Back       => Language == GameLanguage.PL ? "Wróć"        : "Back";
    public static string Settings   => Language == GameLanguage.PL ? "OPCJE"  : "SETTINGS";
    public static string Quit       => Language == GameLanguage.PL ? "WYJDŹ"       : "QUIT";
    public static string Play       => Language == GameLanguage.PL ? "GRAJ"        : "PLAY";
    public static string GoldPrefix => Language == GameLanguage.PL ? "Złoto: "     : "Gold: ";
    public static string LevelPrefix=> Language == GameLanguage.PL ? "Poz."        : "Lv.";

    // ── Menu główne ───────────────────────────────────────────────────────────
    public static string MainMenuPlay     => Language == GameLanguage.PL ? "GRAJ"       : "PLAY";
    public static string MainMenuQuit     => Language == GameLanguage.PL ? "WYJDŹ"      : "QUIT";
    public static string MainMenuPaint    => Language == GameLanguage.PL ? "Malarnia"   : "Paint Shop";
    public static string MainMenuPaintBuy => Language == GameLanguage.PL
        ? "KUP (" + GameData.PAINT_SHOP_COST + "G)" : "BUY (" + GameData.PAINT_SHOP_COST + "G)";
    public static string MainMenuShop     => Language == GameLanguage.PL ? "SKLEP"      : "SHOP";
    public static string MainMenuLab      => Language == GameLanguage.PL ? "Ekwipunek"         : "Equipment";
    public static string MainMenuSettings => Language == GameLanguage.PL ? "OPCJE" : "SETTINGS";

    // ── Sklep ─────────────────────────────────────────────────────────────────
    public static string ShopTitle       => Language == GameLanguage.PL ? "SKLEP"             : "SHOP";
    public static string BallsSection    => Language == GameLanguage.PL ? "Kulki"             : "Balls";
    public static string UpgradesSection => Language == GameLanguage.PL ? "Ulepszenia"        : "Upgrades";
    public static string NotEnoughGold   => Language == GameLanguage.PL ? "Za mało złota!"    : "Too poor!";
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
    public static string PullFlavorText  => Language == GameLanguage.PL ? "Silniejszy i dłuższy pull" : "Stronger and longer pull";

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

    // ── Statystyki kulek (ShopUI) ─────────────────────────────────────────────
    public static string StatsHP        => Language == GameLanguage.PL ? "HP: "        : "HP: ";
    public static string StatsSpeed     => Language == GameLanguage.PL ? "Szybkość: "  : "Speed: ";
    public static string StatsRange     => Language == GameLanguage.PL ? "Zasięg: "    : "Range: ";
    public static string StatsCooldown  => Language == GameLanguage.PL ? "Cooldown: "  : "Cooldown: ";
    public static string StatsCollision => Language == GameLanguage.PL ? "Kolizja: "   : "Collision: ";

    // ── Lab ────────────────────────────────────────────────────────────────
    public static string MergeTitle      => Language == GameLanguage.PL ? "SCALNIA"                         : "MERGE";
    public static string MergeBasic      => Language == GameLanguage.PL ? "Scal bazowe"                     : "Merge basic";
    public static string MergeUpgrade    => Language == GameLanguage.PL ? "Ulepsz scalone"                  : "Upgrade merged";
    public static string OwnedMerged     => Language == GameLanguage.PL ? "Twoje scalone"                   : "Your merged";
    public static string BuyBallsHint    => Language == GameLanguage.PL ? "Kup kulki w sklepie"             : "Buy balls in shop";
    public static string NoMergeAvail    => Language == GameLanguage.PL ? "Brak kulek do ulepszenia"        : "No balls to upgrade";
    public static string NoOwnedMerged   => Language == GameLanguage.PL ? "Brak scalonych kulek"            : "No merged balls";
    public static string MergeBtn        => Language == GameLanguage.PL ? "SCAL"                            : "MERGE";
    public static string BuyMoreBalls    => Language == GameLanguage.PL ? "Dokup kule"                      : "Buy more";
    public static string MergeInfoLine   => Language == GameLanguage.PL ? "Scal 5 kulek tej samej klasy i poziomu." : "Merge 5 balls of the same class and level.";
    public static string SuperPrefix     => "SUPER ";
    public static string MasteryPrefix   => Language == GameLanguage.PL ? "MISTRZ " : "MASTER ";
    public static string Sell         => Language == GameLanguage.PL ? "Sprzedaj"  : "Sell";
    public static string SellBtn      => Language == GameLanguage.PL ? "SPRZEDAJ"  : "SELL";
    public static string ValueLine(int gold)
        => Language == GameLanguage.PL ? "Wartość: " + gold + "g" : "Value: " + gold + "g";
    public static string SoldBall(string name)
        => Language == GameLanguage.PL ? "Sprzedano " + name + "!" : "Sold " + name + "!";
    public static string MergeNeedArenaUpgrade => Language == GameLanguage.PL ? "Ulepsz arenę!" : "Buy arena!";

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

    public static string MergeNeedMore(int have)
        => Language == GameLanguage.PL ? "Masz: " + have + "/5" : "Have: " + have + "/5";
    public static string MergeSuccessBasic(string name)
        => Language == GameLanguage.PL
            ? "Scalono " + name + " → " + LevelPrefix + "1!"
            : "Merged " + name + " → " + LevelPrefix + "1!";
    public static string MergeSuccessUp(string name, int fromLevel)
        => Language == GameLanguage.PL
            ? name + " " + LevelPrefix + fromLevel + " → " + LevelPrefix + (fromLevel + 1) + "!"
            : name + " " + LevelPrefix + fromLevel + " → " + LevelPrefix + (fromLevel + 1) + "!";

    // ── Malarnia ──────────────────────────────────────────────────────────────
    public static string PaintSave      => Language == GameLanguage.PL ? "Zapisz"  : "Save";
    public static string PaintReset     => Language == GameLanguage.PL ? "Resetuj" : "Reset";
    public static string PaintTab1      => Language == GameLanguage.PL ? "Kolor 1" : "Color 1";
    public static string PaintTab2      => Language == GameLanguage.PL ? "Kolor 2" : "Color 2";
    public static string PaintTab3Color => Language == GameLanguage.PL ? "Kolor 3" : "Color 3";
    public static string PaintStripes   => Language == GameLanguage.PL ? "Paski"   : "Stripes";
    public static string PaintDots      => Language == GameLanguage.PL ? "Kropki"  : "Dots";

    public static string GetPatternName(BallPattern p)
    {
        if (Language == GameLanguage.EN)
        {
            switch (p)
            {
                case BallPattern.Solid:             return "Solid";
                case BallPattern.HorizontalStripes: return "Horizontal Stripes";
                case BallPattern.DiagonalStripes:   return "Diagonal Stripes";
                case BallPattern.Pepsi:             return "Pepsi";
                case BallPattern.Quarters:          return "Quarters";
                case BallPattern.Wedge:             return "Wedges";
                case BallPattern.Dots:              return "Dots";
                case BallPattern.Ring:              return "Ring";
                default:                            return p.ToString();
            }
        }
        switch (p)
        {
            case BallPattern.Solid:             return "Jednolity";
            case BallPattern.HorizontalStripes: return "Poziome prążki";
            case BallPattern.DiagonalStripes:   return "Skośne prążki";
            case BallPattern.Pepsi:             return "Pepsi";
            case BallPattern.Quarters:          return "Ćwiartki";
            case BallPattern.Wedge:             return "Plasterki";
            case BallPattern.Dots:              return "Kropki";
            case BallPattern.Ring:              return "Pierścień";
            default:                            return p.ToString();
        }
    }

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

    public static string GetArenaTierName(int tierIdx)
    {
        if (Language == GameLanguage.EN)
        {
            switch (tierIdx)
            {
                case 0: return "Small (S)";
                case 1: return "Medium (M)";
                case 2: return "Large (L)";
                case 3: return "Giant (XL)";
                case 4: return "Tremendous (XXL)";
                case 5: return "Your mom (XXXL)";
                default: return "???";
            }
        }
        switch (tierIdx)
        {
            case 0: return "Mała (S)";
            case 1: return "Średnia (M)";
            case 2: return "Duża (L)";
            case 3: return "Gigant (XL)";
            case 4: return "Olbrzym (XXL)";
            case 5: return "Twoja stara (XXXL)";
            default: return "???";
        }
    }

    // ── Korony / Mistrzostwo ──────────────────────────────────────────────────
    public static string MasteryTitle
        => Language == GameLanguage.PL ? "— MISTRZOSTWO —" : "— MASTERY —";
    public static string MasteryUnlocked
        => Language == GameLanguage.PL ? "[ULT odblokowany!]" : "[ULT unlocked!]";
    public static string MasteryLocked
        => Language == GameLanguage.PL ? "Zdobadz 3 korony aby odblokowac ULT" : "Get 3 crowns to unlock ULT";
    public static string PassiveUnlocked
        => Language == GameLanguage.PL ? ">> Pasywka aktywna" : ">> Passive active";

    public static string CrownProgress(GameData.ClassCrowns c)
    {
        string crown1 = c.crownDamageDealt ? "[+]" : "[ ]";
        string crown2 = c.crownDamageTaken ? "[+]" : "[ ]";
        string crown3 = c.crownWins        ? "[+]" : "[ ]";
        string dmgDealt = crown1 + " " +
            (Language == GameLanguage.PL ? "Obrazenia zadane: " : "Damage dealt: ") +
            c.totalDamageDealt.ToString("F0") + "/" + GameData.CROWN_DMG_DEALT_THRESHOLD;
        string dmgTaken = crown2 + " " +
            (Language == GameLanguage.PL ? "Obrazenia otrzymane: " : "Damage taken: ") +
            c.totalDamageTaken.ToString("F0") + "/" + GameData.CROWN_DMG_TAKEN_THRESHOLD;
        string wins = crown3 + " " +
            (Language == GameLanguage.PL ? "Wygrane rundy: " : "Rounds won: ") +
            c.totalWins + "/" + GameData.CROWN_WINS_THRESHOLD;
        string masteryLine = c.HasMastery ? MasteryUnlocked : MasteryLocked;
        return MasteryTitle + "\n" + dmgDealt + "\n" + dmgTaken + "\n" + wins + "\n" + masteryLine;
    }

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

    // ── Reset danych ──────────────────────────────────────────────────────────
    public static string ResetDataBtn     => Language == GameLanguage.PL ? "Resetuj dane"    : "Reset data";
    public static string ResetConfirmTitle=> Language == GameLanguage.PL ? "NA PEWNO?"       : "ARE YOU SURE?";
    public static string ResetConfirmBody => Language == GameLanguage.PL
        ? "Usuwa: zloto, kulki, scalone, mistrzostwa, malarnie.\nUstawienia zostaja."
        : "Deletes: gold, balls, merged, mastery, paint.\nSettings are kept.";
    public static string ResetConfirmYes  => Language == GameLanguage.PL ? "TAK, RESETUJ"   : "YES, RESET";
    public static string ResetConfirmNo   => Language == GameLanguage.PL ? "Anuluj"          : "Cancel";
    public static string ResetDone        => Language == GameLanguage.PL ? "Dane zresetowane!" : "Data reset!";

    // ── LogoScreen disclaimer ─────────────────────────────────────────────────
    public static string DisclaimerText
        => Language == GameLanguage.PL
            ? "Postęp jest zapisywany automatycznie."
            : "Progress is saved automatically.";

    // ── Nazwy klas ────────────────────────────────────────────────────────────
    public static string GetClassName(BallClass cls)
    {
        if (Language == GameLanguage.EN)
        {
            switch (cls)
            {
                case BallClass.Warrior:      return "Warrior";
                case BallClass.Mage:         return "Mage";
                case BallClass.Archer:       return "Archer";
                case BallClass.Rogue:        return "Rogue";
                case BallClass.Paladin:      return "Paladin";
                case BallClass.Berserker:    return "Berserker";
                case BallClass.Necromancer:  return "Necromancer";
                case BallClass.Elementalist: return "Elementalist";
                case BallClass.Priest:       return "Priest";
                case BallClass.Titan:        return "Titan";
                case BallClass.Druid:        return "Druid";
                case BallClass.Technician:   return "Technician";
                case BallClass.Glitch:       return "Glitch";
                case BallClass.Psychic:      return "Psychic";
                case BallClass.Nerd:         return "Nerd";
                case BallClass.Mariachi:     return "Mariachi";
                default:                     return cls.ToString();
            }
        }
        switch (cls)
        {
            case BallClass.Warrior:      return "Wojownik";
            case BallClass.Mage:         return "Mag";
            case BallClass.Archer:       return "Łucznik";
            case BallClass.Rogue:        return "Łotrzyk";
            case BallClass.Paladin:      return "Paladyn";
            case BallClass.Berserker:    return "Berserker";
            case BallClass.Necromancer:  return "Nekromanta";
            case BallClass.Elementalist: return "Elementalista";
            case BallClass.Priest:       return "Kapłan";
            case BallClass.Titan:        return "Tytan";
            case BallClass.Druid:        return "Druid";
            case BallClass.Technician:   return "Technik";
            case BallClass.Glitch:       return "Glitch";
            case BallClass.Psychic:      return "Psychic";
            case BallClass.Nerd:         return "Nerd";
            case BallClass.Mariachi:     return "Mariachi";
            default:                     return cls.ToString();
        }
    }

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
