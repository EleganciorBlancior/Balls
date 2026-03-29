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
        float rage = 1f - (owner.CurrentHP / owner.MaxHP);
        float speed = owner.Config.moveSpeed * owner.SpeedMultiplier * (1f + rage * 1.5f);
        if (owner.Rb.linearVelocity.magnitude > 0.1f)
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * speed;
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float rage   = 1f - (owner.CurrentHP / owner.MaxHP);
        float dmg    = BASE_DMG * (1f + rage * 2f);
        target.TakeDamage(dmg, owner);
        owner.FlashColor(new Color(1f, 0.2f, 0.0f), 0.1f);
        StartCooldown();
    }

    public void OnBallCollision(BallController other)
    {
        float rage = 1f - (owner.CurrentHP / owner.MaxHP);
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
        AttackRingFX.SpawnWave(owner.transform.position, owner.Config.color, PULSE_RADIUS, 0.38f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, PULSE_RADIUS);
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
        AttackRingFX.SpawnWave(owner.transform.position, owner.Config.color, QUAKE_RADIUS, 0.42f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, QUAKE_RADIUS);
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
    const float REPEL_CD     = 0.45f;  // min. czas między odpychaniami

    private float _repelTimer = 0f;

    public override void Initialize(BallController ownerBall)
    {
        base.Initialize(ownerBall);
        Cooldown = 3f;
        owner.OnDamageTaken += OnHit;
    }

    protected override void Update()
    {
        base.Update();
        if (_repelTimer > 0f) _repelTimer -= Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (owner != null) owner.OnDamageTaken -= OnHit;
    }

    void OnHit(float amount, BallController source)
    {
        if (_repelTimer > 0f) return;   // cooldown – zapobiega pętli feedback
        _repelTimer = REPEL_CD;

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
        AttackRingFX.SpawnWave(owner.transform.position, owner.Config.color, REPEL_RADIUS, 0.50f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, REPEL_RADIUS);
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
// 16. MARIACHI – Rewolwer: zadanie obrażeń → po 3s buff (+atkspd, +HP, +dmg); respawn raz
//               Predictive aim z symulacją odbicias od ścian
// ─────────────────────────────────────────────────────────────────────────────
public class MariachWeapon : WeaponBase
{
    const float BASE_DMG        = 20f;
    const float BULLET_SPEED    = 16f;
    const float BUFF_DELAY      = 3f;    // sekund po zadaniu obrażeń do efektu buffa
    const float ATKSPD_PER_BUFF = 0.1f;  // +0.1 do mnożnika attack speed za buff
    const float MAX_ATKSPD_MULT = 3.0f;
    const float HP_PER_BUFF     = 4f;    // +4 maxHP (i currentHP) za buff
    const float RESPAWN_DUR     = 2f;

    private bool  _hasRespawned = false;
    private float _atkSpdMult   = 1f;
    private float _baseCooldown;
    private float _bonusDmg     = 0f;   // każdy buff daje +1 do obrażeń pocisku

    // Cache granic areny (raz na sesję)
    private static Bounds _arenaBounds;
    private static bool   _boundsReady = false;

    public override void Initialize(BallController ownerBall)
    {
        base.Initialize(ownerBall);
        _baseCooldown = 1.4f;
        Cooldown      = _baseCooldown;
        if (!_boundsReady) CacheArenaBounds();
    }

    static void CacheArenaBounds()
    {
        var allColliders = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        bool found = false;
        Bounds b   = new Bounds();
        foreach (var c in allColliders)
        {
            if (!c.gameObject.name.StartsWith("Wall_")) continue;
            if (!found) { b = c.bounds; found = true; }
            else          b.Encapsulate(c.bounds);
        }
        if (found)
        {
            b.Expand(-1.0f);
            _arenaBounds = b;
            _boundsReady = true;
        }
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        Vector2 aimPos = PredictIntercept(target);
        Vector2 dir    = (aimPos - (Vector2)owner.transform.position).normalized;

        var go   = BallArenaUtils.CreateBulletGO(
            owner.transform.position + (Vector3)(dir * 0.6f),
            new Color(1f, 0.55f, 0.05f), 0.16f);
        var proj = go.AddComponent<Projectile>();
        proj.Initialize(owner, target, dir * BULLET_SPEED,
                        ScaleDmg(BASE_DMG + _bonusDmg),
                        ProjectileType.Arrow, 0f, 3.5f);
        proj.OnHitCallback = OnDamageDealt;
        owner.FlashColor(new Color(1f, 0.6f, 0.1f), 0.12f);
        StartCooldown();
    }

    // Wywoływane gdy pocisk faktycznie zadał obrażenia
    void OnDamageDealt() => StartCoroutine(DelayedBuff());

    System.Collections.IEnumerator DelayedBuff()
    {
        yield return new WaitForSeconds(BUFF_DELAY);
        if (owner == null || !owner.IsAlive) yield break;

        _atkSpdMult = Mathf.Min(_atkSpdMult + ATKSPD_PER_BUFF, MAX_ATKSPD_MULT);
        Cooldown    = _baseCooldown / _atkSpdMult;
        _bonusDmg  += 1f;
        owner.AddMaxHP(HP_PER_BUFF, HP_PER_BUFF * 0.5f);

        owner.FlashColor(new Color(1f, 0.85f, 0.1f), 0.35f);
        owner.PunchScale(1.15f, 0.18f);
    }

    public override bool OnPreDeath()
    {
        if (_hasRespawned) return false;
        _hasRespawned = true;
        StartCoroutine(RespawnRoutine());
        return true;
    }

    System.Collections.IEnumerator RespawnRoutine()
    {
        owner.PunchScale(1.6f, 0.3f);

        float elapsed = 0f;
        while (elapsed < RESPAWN_DUR)
        {
            elapsed += Time.deltaTime;
            owner.FlashColor(new Color(1f, 0.85f, 0.3f), 0.08f);
            yield return new WaitForSeconds(0.12f);
        }
    }

    // ── Predictive aim z odbiciami od ścian ──────────────────────────────────

    Vector2 PredictIntercept(BallController target)
    {
        if (!_boundsReady || target == null) return target != null
            ? (Vector2)target.transform.position
            : (Vector2)owner.transform.position;

        Vector2 shootPos  = owner.transform.position;
        Vector2 targetPos = target.transform.position;
        Vector2 targetVel = target.Rb != null ? target.Rb.linearVelocity : Vector2.zero;

        if (targetVel.sqrMagnitude < 0.01f)
            return targetPos;

        // Szukaj czasu t w którym bullet dociera do symulowanej pozycji celu
        const float MAX_T = 3f;
        const int   STEPS = 48;
        float bestT   = 0f;
        float bestErr = float.MaxValue;

        for (int i = 0; i <= STEPS; i++)
        {
            float   t    = MAX_T * i / STEPS;
            Vector2 p    = SimulateWithBounce(targetPos, targetVel, t);
            float   dist = Vector2.Distance(shootPos, p);
            float   err  = Mathf.Abs(dist - BULLET_SPEED * t);
            if (err < bestErr) { bestErr = err; bestT = t; }
        }

        return SimulateWithBounce(targetPos, targetVel, bestT);
    }

    static Vector2 SimulateWithBounce(Vector2 pos, Vector2 vel, float time)
    {
        if (!_boundsReady || vel.sqrMagnitude < 0.001f)
            return pos + vel * time;

        Vector2 mn        = _arenaBounds.min;
        Vector2 mx        = _arenaBounds.max;
        float   remaining = time;
        const int MAX_BOUNCES = 8;

        for (int b = 0; b < MAX_BOUNCES && remaining > 0.0001f; b++)
        {
            float txWall = vel.x > 0f ? (mx.x - pos.x) / vel.x
                         : vel.x < 0f ? (mn.x - pos.x) / vel.x
                         : float.MaxValue;
            float tyWall = vel.y > 0f ? (mx.y - pos.y) / vel.y
                         : vel.y < 0f ? (mn.y - pos.y) / vel.y
                         : float.MaxValue;

            float tHit = Mathf.Min(txWall, tyWall);

            if (tHit <= 0.0001f || tHit >= remaining)
            {
                pos += vel * remaining;
                break;
            }

            pos       += vel * tHit;
            remaining -= tHit;

            if (Mathf.Abs(txWall - tyWall) < 0.001f)
            { vel.x = -vel.x; vel.y = -vel.y; }   // narożnik
            else if (txWall < tyWall)
                vel.x = -vel.x;                    // ściana boczna
            else
                vel.y = -vel.y;                    // ściana górna/dolna
        }

        return pos;
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
