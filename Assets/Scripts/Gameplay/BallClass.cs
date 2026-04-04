using UnityEngine;

public enum BallClass
{
    Warrior,      // 1. Wojownik   – Szarża
    Mage,         // 2. Mag        – Homing Fireball
    Archer,       // 3. Łucznik   – Salwa strzał
    Rogue,        // 4. Łotrzyk   – Nieśmiertelność + Stab
    Paladin,      // 5. Paladyn    – Smite + Heal
    Berserker,    // 6. Berserker  – Rage: im mniej HP tym mocniejszy
    Necromancer,  // 7. Nekromanta – AoE pulse + lifesteal
    Elementalist, // 8. Elementalista – rotuje żywioły
    Priest,       // 9. Kapłan    – self-heal + osłabienie wroga
    Titan,        // 10. Tytan       – wolny, ogromny, tąpnięcia
    Druid,        // 11. Druid       – przywołuje minionki
    Technician,   // 12. Technik     – stawia wieżyczki
    Glitch,       // 13. Glitch      – chaos przy kolizji
    Psychic,      // 14. Psychic     – odpycha przy obrażeniach
    Nerd,         // 15. Nerd        – Fibonacci dmg
    Mariachi      // 16. Mariachi    – Rewolwer; po trafieniu boost statystyk; respawn przy pierwszej śmierci
}

public enum ProjectileType { Fireball, Arrow, PoisonBolt }

public enum BallPattern
{
    Solid,             // jeden kolor
    HorizontalStripes, // poziome prążki c1/c2
    DiagonalStripes,   // ukośne prążki c1/c2
    Pepsi,             // lewa-górna c1, prawa-dolna c2 z krzywą
    Quarters,          // 4 ćwiartki naprzemiennie c1/c2
    Wedge,             // plasterki pizzy c1/c2/c3
    Dots,              // kropki c2 na tle c1
    Ring,              // pierścień c2 + rdzeń c3 na tle c1
}

[CreateAssetMenu(menuName = "BallArena/ClassConfig", fileName = "ClassConfig")]
public class ClassConfig : ScriptableObject
{
    public BallClass  ballClass;
    public string     className       = "Kulka";
    public Color      color           = Color.white;
    public Color      color2          = Color.white;
    public Color      color3          = Color.white;
    public BallPattern pattern        = BallPattern.Solid;
    [Range(2, 12)]
    public int        stripeCount     = 5;
    public float      maxHP           = 100f;
    public float      radius          = 0.5f;
    public float      moveSpeed       = 3f;
    public float      attackRange     = 2f;
    public float      attackCooldown  = 2f;
    public float      collisionDamage = 10f;
    public Sprite     ballSprite;
    public Sprite     weaponSprite;
}
