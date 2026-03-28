// Weapons.cs – 10 klas broni
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 1. WOJOWNIK – Szarża + Slam
// ─────────────────────────────────────────────────────────────────────────────
public class SwordWeapon : WeaponBase
{
    private bool  charging;
    private float chargeTimer;
    const float CHARGE_DURATION = 0.4f;
    const float CHARGE_MULT     = 2.8f;
    const float SLAM_DMG        = 40f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.5f; }

    protected override void Update()
    {
        base.Update();
        if (!charging) return;
        chargeTimer -= Time.deltaTime;
        if (chargeTimer <= 0f)
        {
            charging = false;
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * owner.Config.moveSpeed;
        }
    }

    public override void Attack(BallController target)
    {
        if (!IsReady || charging) return;
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        owner.Rb.linearVelocity = dir * owner.Config.moveSpeed * CHARGE_MULT;
        charging = true; chargeTimer = CHARGE_DURATION;
        owner.FlashColor(Color.white, 0.15f);
        StartCooldown();
    }

    public void OnBallCollision(BallController other)
    {
        float dmg = charging ? SLAM_DMG : owner.Config.collisionDamage;
        other.TakeDamage(dmg, owner);
        if (charging)
            other.ApplyKnockback((other.transform.position - owner.transform.position).normalized, 6f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 2. MAG – Homing Fireball
// ─────────────────────────────────────────────────────────────────────────────
public class FireballWeapon : WeaponBase
{
    const float DMG = 30f, SPEED = 5f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.0f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        var go   = BallArenaUtils.CreateBulletGO(owner.transform.position, new Color(1f, 0.4f, 0.1f), 0.22f);
        var proj = go.AddComponent<Projectile>();
        proj.Initialize(owner, target, dir * SPEED, DMG, ProjectileType.Fireball, 4f, 6f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 3. ŁUCZNIK – Salwa 3 strzał
// ─────────────────────────────────────────────────────────────────────────────
public class ArrowWeapon : WeaponBase
{
    const float DMG = 15f, SPEED = 9f;
    int   burstLeft; float burstTimer; BallController burstTarget;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 1.8f; }

    protected override void Update()
    {
        base.Update();
        if (burstLeft <= 0) return;
        burstTimer -= Time.deltaTime;
        if (burstTimer <= 0f) { FireArrow(burstTarget); burstLeft--; burstTimer = 0.18f; }
    }

    public override void Attack(BallController target)
    {
        if (!IsReady || burstLeft > 0) return;
        burstTarget = target; burstLeft = 3; burstTimer = 0f;
        StartCooldown();
    }

    void FireArrow(BallController target)
    {
        if (target == null || !target.IsAlive) return;
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        dir = Quaternion.Euler(0, 0, Random.Range(-8f, 8f)) * dir;
        var go = BallArenaUtils.CreateBulletGO(owner.transform.position + (Vector3)(dir * 0.6f),
                     new Color(0.3f, 1f, 0.3f), 0.14f);
        go.AddComponent<Projectile>().Initialize(owner, target, dir * SPEED, DMG, ProjectileType.Arrow, 0f, 3f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 4. ŁOTRZYK – Nieśmiertelność gdy IsReady + Stab + Trucizna
// ─────────────────────────────────────────────────────────────────────────────
public class PoisonWeapon : WeaponBase
{
    const float POISON_DPS      = 8f;
    const float POISON_DURATION = 4f;
    const float STAB_DMG        = 20f;

    public bool IsInvincible => IsReady;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 3.5f; }

    protected override void Update()
    {
        base.Update();
        if (IsReady && owner != null)
            owner.SetInvincibleGlow(true);
        else if (owner != null)
            owner.SetInvincibleGlow(false);
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        Vector3 dir = (target.transform.position - owner.transform.position).normalized;
        owner.transform.position = target.transform.position
            - dir * (target.Config.radius + owner.Config.radius + 0.1f);

        target.TakeDamage(STAB_DMG, owner);
        target.ApplyPoison(POISON_DPS * 2f, POISON_DURATION, owner);
        owner.FlashColor(new Color(0.5f, 1f, 0.2f), 0.2f);
        StartCooldown();
    }

    public void OnBallCollision(BallController other)
        => other.ApplyPoison(POISON_DPS, POISON_DURATION, owner);
}

// ─────────────────────────────────────────────────────────────────────────────
// 5. PALADYN – Shield Smite + self-heal + dmg reduction 30%
// ─────────────────────────────────────────────────────────────────────────────
public class ShieldWeapon : WeaponBase
{
    const float SMITE_DMG = 45f, KNOCKBACK = 8f, HEAL = 20f, DMG_REDUCTION = 0.30f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.8f; owner.OnTakePhysicalDamage += ReduceDmg; }

    private void OnDestroy() { if (owner != null) owner.OnTakePhysicalDamage -= ReduceDmg; }

    float ReduceDmg(float d) => d * (1f - DMG_REDUCTION);

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        target.TakeDamage(SMITE_DMG, owner);
        target.ApplyKnockback((target.transform.position - owner.transform.position).normalized, KNOCKBACK);
        owner.Heal(HEAL);
        owner.FlashColor(new Color(0.5f, 0.9f, 1f), 0.25f);
        owner.PunchScale(1.3f, 0.2f);
        StartCooldown();
    }

    public void OnBallCollision(BallController other)
        => other.TakeDamage(owner.Config.collisionDamage * 0.5f, owner);
}

// ─────────────────────────────────────────────────────────────────────────────
// 6. BERSERKER – Im mniej HP tym więcej dmg i szybkości
// ─────────────────────────────────────────────────────────────────────────────
public class BerserkWeapon : WeaponBase
{
    const float BASE_DMG = 20f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 1.2f; }

    protected override void Update()
    {
        base.Update();
        float rage = 1f - (owner.CurrentHP / owner.Config.maxHP);
        float speed = owner.Config.moveSpeed * (1f + rage * 1.5f);
        if (owner.Rb.linearVelocity.magnitude > 0.1f)
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * speed;
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float rage   = 1f - (owner.CurrentHP / owner.Config.maxHP);
        float dmg    = BASE_DMG * (1f + rage * 2f);
        target.TakeDamage(dmg, owner);
        owner.FlashColor(new Color(1f, 0.2f, 0.0f), 0.1f);
        StartCooldown();
    }

    public void OnBallCollision(BallController other)
    {
        float rage = 1f - (owner.CurrentHP / owner.Config.maxHP);
        other.TakeDamage(owner.Config.collisionDamage * (1f + rage), owner);
    }

    public void NotifyCollision() { }
}

// ─────────────────────────────────────────────────────────────────────────────
// 7. NEKROMANTA – Odpycha wszystkich i leczy się za zadane obrażenia
// ─────────────────────────────────────────────────────────────────────────────
public class NecroWeapon : WeaponBase
{
    const float PULSE_DMG    = 15f;
    const float PULSE_RADIUS = 4f;
    const float LIFESTEAL    = 0.4f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 3.0f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        var all = UnityEngine.Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
        float totalDmg = 0f;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > PULSE_RADIUS) continue;
            float dmg = PULSE_DMG * (1f - d / PULSE_RADIUS);
            b.TakeDamage(dmg, owner);
            b.ApplyKnockback((b.transform.position - owner.transform.position).normalized, 5f);
            totalDmg += dmg;
        }
        owner.Heal(totalDmg * LIFESTEAL);
        owner.FlashColor(new Color(0.5f, 0f, 0.8f), 0.3f);
        owner.PunchScale(1.5f, 0.3f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 8. ELEMENTALISTA – Losuje żywioł: ogień/lód/piorun
// ─────────────────────────────────────────────────────────────────────────────
public class ElementWeapon : WeaponBase
{
    const float SPEED = 6f;
    private int element = 0;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.2f; element = Random.Range(0, 3); }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        element = (element + 1) % 3;
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;

        Color col; float dmg; ProjectileType type;
        switch (element)
        {
            case 0: col = new Color(1f, 0.3f, 0f);   dmg = 25f; type = ProjectileType.Fireball;  break;
            case 1: col = new Color(0.4f, 0.8f, 1f); dmg = 18f; type = ProjectileType.Arrow;     break;
            default:col = new Color(1f, 1f, 0.2f);   dmg = 35f; type = ProjectileType.PoisonBolt;break;
        }

        var go = BallArenaUtils.CreateBulletGO(owner.transform.position, col, 0.2f);
        go.AddComponent<Projectile>().Initialize(owner, target, dir * SPEED, dmg, type,
            element == 0 ? 3f : 0f, 4f);
        owner.FlashColor(col, 0.15f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 9. KAPŁAN – self-heal + osłabienie wroga
// ─────────────────────────────────────────────────────────────────────────────
public class PriestWeapon : WeaponBase
{
    const float HEAL_AMT   = 15f;
    const float DEBUFF_DUR = 3f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.5f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        owner.Heal(HEAL_AMT);
        target.ApplyWeaken(0.3f, DEBUFF_DUR);
        owner.FlashColor(new Color(1f, 1f, 0.6f), 0.2f);

        var go = BallArenaUtils.CreateBulletGO(owner.transform.position, new Color(1f, 1f, 0.4f), 0.15f);
        go.AddComponent<Projectile>().Initialize(owner, target,
            (target.transform.position - owner.transform.position).normalized * 4f,
            0f, ProjectileType.Arrow, 0f, 2f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 10. TYTAN – Wolny, ogromny, odpycha przy zderzeniu + ciągłe tąpnięcie
// ─────────────────────────────────────────────────────────────────────────────
public class TitanWeapon : WeaponBase
{
    const float QUAKE_DMG    = 20f;
    const float QUAKE_RADIUS = 3.5f;
    private float quakeTimer = 0f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 4.0f; }

    protected override void Update()
    {
        base.Update();
        quakeTimer -= Time.deltaTime;
        if (quakeTimer <= 0f) { Quake(); quakeTimer = 1.5f; }
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        target.TakeDamage(60f, owner);
        target.ApplyKnockback((target.transform.position - owner.transform.position).normalized, 15f);
        owner.PunchScale(1.6f, 0.4f);
        owner.FlashColor(Color.white, 0.3f);
        StartCooldown();
    }

    void Quake()
    {
        var all = UnityEngine.Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > QUAKE_RADIUS) continue;
            b.TakeDamage(QUAKE_DMG * (1f - d / QUAKE_RADIUS), owner);
            b.ApplyKnockback((b.transform.position - owner.transform.position).normalized, 3f);
        }
    }

    public void OnBallCollision(BallController other)
    {
        other.TakeDamage(owner.Config.collisionDamage * 2f, owner);
        other.ApplyKnockback((other.transform.position - owner.transform.position).normalized, 8f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 11. DRUID – co 5s przywołuje minionka (max 3), minionki ścigają wrogów
// ─────────────────────────────────────────────────────────────────────────────
public class DruidWeapon : WeaponBase
{
    const float MINION_DMG = 22f;
    const int   MAX_MINIONS = 3;
    private readonly System.Collections.Generic.List<GameObject> _minions
        = new System.Collections.Generic.List<GameObject>();

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 5f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        _minions.RemoveAll(m => m == null);
        if (_minions.Count >= MAX_MINIONS) return;

        var go = new GameObject("DruidMinion");
        go.transform.position = owner.transform.position;
        go.transform.localScale = Vector3.one * 0.38f;

        var sr     = go.AddComponent<SpriteRenderer>();
        sr.sprite  = BallArenaUtils.CircleSprite;
        sr.color   = new Color(0.25f, 0.85f, 0.3f, 1f);
        sr.sortingOrder = 2;

        var rb          = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col         = go.AddComponent<CircleCollider2D>();
        col.radius      = 0.5f;
        col.isTrigger   = true;

        var minion = go.AddComponent<DruidMinion>();
        minion.Initialize(owner, ScaleDmg(MINION_DMG));

        _minions.Add(go);
        owner.FlashColor(new Color(0.3f, 1f, 0.3f), 0.2f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 12. TECHNIK – co 5s stawia wieżyczkę (max 3, najstarsza usuwana)
// ─────────────────────────────────────────────────────────────────────────────
public class TechWeapon : WeaponBase
{
    const float TURRET_DMG  = 18f;
    const int   MAX_TURRETS = 3;
    private readonly System.Collections.Generic.List<GameObject> _turrets
        = new System.Collections.Generic.List<GameObject>();

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 5f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        _turrets.RemoveAll(t => t == null);
        if (_turrets.Count >= MAX_TURRETS)
        {
            Destroy(_turrets[0]);
            _turrets.RemoveAt(0);
        }

        var go = new GameObject("TechTurret");
        go.transform.position   = owner.transform.position;
        go.transform.localScale = Vector3.one * 0.55f;

        var sr     = go.AddComponent<SpriteRenderer>();
        sr.sprite  = BallArenaUtils.CircleSprite;
        sr.color   = new Color(0.3f, 0.75f, 1f, 1f);
        sr.sortingOrder = 2;

        var turret = go.AddComponent<TechTurret>();
        turret.Initialize(owner, ScaleDmg(TURRET_DMG));

        _turrets.Add(go);
        owner.FlashColor(new Color(0.3f, 0.8f, 1f), 0.2f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 13. GLITCH – przy każdej kolizji losuje nowe dmg/range/cooldown + zmienia kolor
// ─────────────────────────────────────────────────────────────────────────────
public class GlitchWeapon : WeaponBase
{
    const float BASE_DMG = 20f;
    private static readonly Color[] CHAOS_COLORS =
    {
        new Color(1f, 0f, 0.8f), new Color(0f, 1f, 0.9f),
        new Color(1f, 0.9f, 0f), new Color(0.5f, 0f, 1f),
        new Color(0f, 1f, 0.3f), new Color(1f, 0.3f, 0f),
    };
    private int _colorIdx;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        target.TakeDamage(ScaleDmg(BASE_DMG), owner);
        owner.FlashColor(CHAOS_COLORS[_colorIdx % CHAOS_COLORS.Length], 0.15f);
        StartCooldown();
    }

    public void OnBallCollision(BallController other)
    {
        // Losuj nowe statystyki
        owner.ScaledAttackRange = Random.Range(1.5f, 7f);
        Cooldown                = Random.Range(0.6f, 4f);
        _colorIdx++;
        Color c = CHAOS_COLORS[_colorIdx % CHAOS_COLORS.Length];
        owner.FlashColor(c, 0.3f);
        other.TakeDamage(ScaleDmg(Random.Range(8f, 35f)), owner);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 14. PSYCHIC – przy obrażeniach odpycha wszystkich w pobliżu
// ─────────────────────────────────────────────────────────────────────────────
public class PsychicWeapon : WeaponBase
{
    const float REPEL_RADIUS = 7f;
    const float REPEL_FORCE  = 14f;
    const float DIRECT_DMG   = 18f;

    public override void Initialize(BallController ownerBall)
    {
        base.Initialize(ownerBall);
        Cooldown = 3f;
        owner.OnDamageTaken += OnHit;
    }

    private void OnDestroy()
    {
        if (owner != null) owner.OnDamageTaken -= OnHit;
    }

    void OnHit(float amount, BallController source)
    {
        // Odepchij wszystkich w zasięgu
        var all = FindObjectsByType<BallController>(FindObjectsSortMode.None);
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > REPEL_RADIUS) continue;
            Vector2 dir = (b.transform.position - owner.transform.position).normalized;
            b.Rb.AddForce(dir * REPEL_FORCE, ForceMode2D.Impulse);
            b.StartPsychicRepel(owner);
        }
        owner.FlashColor(new Color(0.7f, 0.3f, 1f), 0.2f);
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        target.TakeDamage(ScaleDmg(DIRECT_DMG), owner);
        target.ApplyKnockback((target.transform.position - owner.transform.position).normalized, 8f);
        owner.FlashColor(new Color(0.6f, 0.1f, 1f), 0.2f);
        StartCooldown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 15. NERD – obrażenia rosnące wg ciągu Fibonacciego (1,1,2,3,5...610)
// ─────────────────────────────────────────────────────────────────────────────
public class NerdWeapon : WeaponBase
{
    private static readonly int[] Fibs = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610 };
    private int _fibIdx = 0;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 1.8f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float dmg = ScaleDmg(Fibs[_fibIdx]);
        target.TakeDamage(dmg, owner);

        // Kolor wg mocy: słaby = niebieski, mocny = czerwony
        float t = (float)_fibIdx / (Fibs.Length - 1);
        owner.FlashColor(Color.Lerp(new Color(0.4f, 0.6f, 1f), new Color(1f, 0.1f, 0.1f), t), 0.2f);

        _fibIdx = Mathf.Min(_fibIdx + 1, Fibs.Length - 1);
        StartCooldown();
    }
}
