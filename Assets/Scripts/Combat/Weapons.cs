// Weapons.cs – 16 klas broni z pasywkami (lv5) i ULT (mistrzostwo)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 1. WOJOWNIK – Szarża + Slam
//    Pasywka: +30 HP, +20 dmg do szarży
//    ULT: Obrotowy Slam AoE + krwawienie
// ─────────────────────────────────────────────────────────────────────────────
public class SwordWeapon : WeaponBase
{
    private bool    charging;
    private float   chargeTimer;
    private Vector2 chargeDir;
    const float CHARGE_DURATION = 0.4f;
    const float CHARGE_MULT     = 2.8f;
    const float SLAM_DMG_BASE   = 40f;
    const float SLAM_DMG_PASS   = 60f;  // z pasywką
    const float ULT_RADIUS      = 3.5f;
    const float ULT_DMG         = 50f;

    private float SlamDmg => HasPassive ? SLAM_DMG_PASS : SLAM_DMG_BASE;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.5f; }

    protected override void OnPassiveUnlocked()
    {
        owner.AddMaxHP(30f, 30f);
    }

    protected override void Update()
    {
        base.Update();
        if (charging)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f) charging = false;
        }
        EnforceSpeed();
    }

    private void FixedUpdate() { EnforceSpeed(); }

    void EnforceSpeed()
    {
        if (owner == null) return;
        if (charging)
        {
            owner.Rb.linearVelocity = chargeDir * owner.Config.moveSpeed * CHARGE_MULT;
            return;
        }
        float sqSpd = owner.Rb.linearVelocity.sqrMagnitude;
        float target = owner.EffectiveSpeed;
        if (sqSpd < 0.01f)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            owner.Rb.linearVelocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * target;
            return;
        }
        if (sqSpd < target * target)
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * target;
    }

    public override void Attack(BallController target)
    {
        if (!IsReady || charging) return;
        chargeDir = (target.transform.position - owner.transform.position).normalized;
        owner.Rb.linearVelocity = chargeDir * owner.Config.moveSpeed * CHARGE_MULT;
        charging = true; chargeTimer = CHARGE_DURATION;
        owner.FlashColor(Color.white, 0.15f);
        AudioController.Instance?.PlayMeleeHit();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        // Obrotowy Slam: AoE wokół właściciela + krwawienie
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > ULT_RADIUS) continue;
            b.TakeDamage(ScaleDmg(ULT_DMG), owner);
            b.ApplyBleed(10f, 5f, owner);
            b.ApplyKnockback((b.transform.position - owner.transform.position).normalized, 5f);
        }
        AttackRingFX.SpawnWave(owner.transform.position, new Color(0.9f, 0.1f, 0.1f), ULT_RADIUS, 0.4f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, ULT_RADIUS);
        owner.PunchScale(1.5f, 0.3f);
    }

    public void OnBallCollision(BallController other)
    {
        float dmg = charging ? SlamDmg : owner.Config.collisionDamage;
        other.TakeDamage(dmg, owner);
        if (charging)
            other.ApplyKnockback((other.transform.position - owner.transform.position).normalized, 6f);
        EnforceSpeed();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 2. MAG – Homing Fireball
//    Pasywka: strzela 2 szybkimi ognistymi kulami
//    ULT: gigantyczny fireball + kałuża ognia
// ─────────────────────────────────────────────────────────────────────────────
public class FireballWeapon : WeaponBase
{
    const float DMG        = 30f;
    const float SPEED      = 9f;
    const float PASS_SPEED = 11f;
    const float PASS_DMG   = 35f;
    const float ULT_DMG    = 90f;
    const float ULT_SPEED  = 7f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.0f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        if (HasPassive)
        {
            Fireball(target, PASS_DMG, PASS_SPEED, 0.22f);
            owner.StartCoroutine(DelayedFireball(target, PASS_DMG, PASS_SPEED, 0.18f));
        }
        else
        {
            Fireball(target, DMG, SPEED, 0.22f);
        }
        AudioController.Instance?.PlayProjectileFire();
        StartCooldown(target);
    }

    void Fireball(BallController target, float dmg, float speed, float size)
    {
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        var go  = BallArenaUtils.CreateBulletGO(owner.transform.position, new Color(1f, 0.4f, 0.1f), ScaleBulletSize(size));
        go.AddComponent<Projectile>().Initialize(owner, target, dir * ScaleBulletSpeed(speed), ScaleDmg(dmg),
            ProjectileType.Fireball, 4f, 6f);
    }

    IEnumerator DelayedFireball(BallController target, float dmg, float speed, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (owner == null || !owner.IsAlive) yield break;
        Fireball(target, dmg, speed, 0.19f);
        AudioController.Instance?.PlayProjectileFire();
    }

    protected override void FireUlt(BallController target)
    {
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        var go   = BallArenaUtils.CreateBulletGO(owner.transform.position, new Color(1f, 0.2f, 0f), ScaleBulletSize(0.45f));
        var proj = go.AddComponent<Projectile>();
        proj.Initialize(owner, target, dir * ScaleBulletSpeed(ULT_SPEED), ScaleDmg(ULT_DMG), ProjectileType.Fireball, 5f, 8f);
        proj.OnHitBallCallback = (hitBall) =>
        {
            FirePuddle.Spawn(hitBall.transform.position, owner, ScaleDmg(12f), 5f);
        };
        AudioController.Instance?.PlayProjectileFire();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 3. ŁUCZNIK – Salwa strzał
//    Pasywka: 7 strzał zamiast 3
//    ULT: duże knockbacki kolejnych strzał (cel zbliżający się = większy odrzut)
// ─────────────────────────────────────────────────────────────────────────────
public class ArrowWeapon : WeaponBase
{
    const float DMG   = 15f;
    const float SPEED = 9f;
    int   burstLeft; float burstTimer; BallController burstTarget;
    bool  _ultBurst = false;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 1.8f; }

    protected override void Update()
    {
        base.Update();
        if (burstLeft <= 0) return;
        burstTimer -= Time.deltaTime;
        if (burstTimer <= 0f)
        {
            FireArrow(burstTarget, _ultBurst);
            burstLeft--;
            burstTimer = 0.18f;
            if (burstLeft == 0) _ultBurst = false;
        }
    }

    public override void Attack(BallController target)
    {
        if (!IsReady || burstLeft > 0) return;
        burstTarget = target;
        burstLeft   = HasPassive ? 7 : 3;
        burstTimer  = 0f;
        _ultBurst   = false;
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        // Odpal serię z dużym knockbackiem
        if (burstLeft > 0) return;
        burstTarget = target;
        burstLeft   = HasPassive ? 7 : 3;
        burstTimer  = 0f;
        _ultBurst   = true;
    }

    void FireArrow(BallController target, bool heavyKnockback)
    {
        if (target == null || !target.IsAlive) return;
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        dir = Quaternion.Euler(0, 0, Random.Range(-8f, 8f)) * dir;
        var go = BallArenaUtils.CreateBulletGO(owner.transform.position + (Vector3)(dir * 0.6f),
                     new Color(0.3f, 1f, 0.3f), ScaleBulletSize(0.14f));
        var proj = go.AddComponent<Projectile>();
        proj.Initialize(owner, target, dir * ScaleBulletSpeed(SPEED), ScaleDmg(DMG), ProjectileType.Arrow, 0f, 3f);
        if (heavyKnockback)
            proj.OnHitBallCallback = (b) => b.ApplyKnockback((b.transform.position - owner.transform.position).normalized, 14f);
        AudioController.Instance?.PlayProjectileFire();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 4. ŁOTRZYK – Teleport + Stab + Trucizna
//    Pasywka: podwójny stab (drugi po 0.25s)
//    ULT: 3× dmg + trucizna + chwilowa nieśmiertelność
// ─────────────────────────────────────────────────────────────────────────────
public class PoisonWeapon : WeaponBase
{
    const float POISON_DPS      = 8f;
    const float POISON_DURATION = 4f;
    const float STAB_DMG        = 20f;
    const float ULT_DMG_MULT    = 3f;
    const float ULT_INVULN_DUR  = 1.5f;

    public bool IsInvincible => IsReady;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 3.5f; owner.ScaledAttackRange = Mathf.Max(owner.ScaledAttackRange * 0.9f, 2.75f); }

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
        DoStab(target, STAB_DMG, false);
        if (HasPassive)
            owner.StartCoroutine(DelayedStab(target, STAB_DMG * 0.8f, 0.25f));
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        if (target == null || !target.IsAlive) return;
        DoStab(target, STAB_DMG * ULT_DMG_MULT, true);
        owner.StartCoroutine(UltInvuln());
    }

    IEnumerator DelayedStab(BallController target, float dmg, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (owner == null || !owner.IsAlive || target == null || !target.IsAlive) yield break;
        DoStab(target, dmg, false);
    }

    IEnumerator UltInvuln()
    {
        owner.invincible = true;
        owner.FlashColor(new Color(0.5f, 1f, 0.2f), ULT_INVULN_DUR);
        yield return new WaitForSeconds(ULT_INVULN_DUR);
        if (owner != null) owner.invincible = false;
    }

    void DoStab(BallController target, float dmg, bool heavyPoison)
    {
        Vector3 fromPos = owner.transform.position;
        Vector3 dir     = (target.transform.position - fromPos).normalized;
        AudioController.Instance?.PlayRogueTeleport();
        SpawnShadowGhost(fromPos);
        owner.transform.position = target.transform.position - dir * (target.Config.radius + owner.Config.radius + 0.1f);
        target.TakeDamage(ScaleDmg(dmg), owner);
        target.ApplyPoison(heavyPoison ? POISON_DPS * 3f : POISON_DPS * 2f,
                           heavyPoison ? POISON_DURATION * 2f : POISON_DURATION, owner);
        owner.FlashColor(new Color(0.5f, 1f, 0.2f), 0.2f);
        AudioController.Instance?.PlayMeleeHit();
    }

    void SpawnShadowGhost(Vector3 pos)
    {
        var go              = new GameObject("RogueShadow");
        go.transform.position   = pos;
        go.transform.localScale = owner.transform.localScale * 1.1f;
        var sr              = go.AddComponent<SpriteRenderer>();
        sr.sprite           = BallArenaUtils.CircleSprite;
        sr.color            = new Color(0.05f, 0.05f, 0.15f, 0.65f);
        sr.sortingOrder     = owner.GetComponent<SpriteRenderer>().sortingOrder - 1;
        go.AddComponent<ShadowFader>().Init(0.18f);
    }

    public void OnBallCollision(BallController other)
        => other.ApplyPoison(POISON_DPS, POISON_DURATION, owner);
}

// ─────────────────────────────────────────────────────────────────────────────
// 5. PALADYN – Shield Smite + self-heal + 30% dmg reduction
//    Pasywka: +30 HP tarczy (absorbuje obrażenia przed HP)
//    ULT: cios zadający tyle obrażeń ile Paladyn zredukował od ostatniego ULT
// ─────────────────────────────────────────────────────────────────────────────
public class ShieldWeapon : WeaponBase
{
    const float SMITE_DMG    = 45f;
    const float KNOCKBACK    = 8f;
    const float HEAL         = 20f;
    const float DMG_REDUCTION = 0.30f;
    const float PASSIVE_SHIELD = 30f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.8f; owner.OnTakePhysicalDamage += ReduceDmg; }

    protected override void OnPassiveUnlocked() => owner.AddShieldHP(PASSIVE_SHIELD);

    private void OnDestroy() { if (owner != null) owner.OnTakePhysicalDamage -= ReduceDmg; }

    float ReduceDmg(float d) => d * (1f - DMG_REDUCTION);

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        target.TakeDamage(ScaleDmg(SMITE_DMG), owner);
        target.ApplyKnockback((target.transform.position - owner.transform.position).normalized, KNOCKBACK);
        owner.Heal(HEAL);
        owner.FlashColor(new Color(0.5f, 0.9f, 1f), 0.25f);
        owner.PunchScale(1.3f, 0.2f);
        AudioController.Instance?.PlayMeleeHit();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        float bonusDmg = owner.GetAndConsumeReducedDmg();
        if (bonusDmg < 1f) bonusDmg = 20f;
        target.TakeHolyDamage(ScaleDmg(bonusDmg), owner);
        target.ApplyKnockback((target.transform.position - owner.transform.position).normalized, 12f);
        AttackRingFX.SpawnWave(owner.transform.position, new Color(0.5f, 0.9f, 1f), 3f, 0.35f);
        owner.FlashColor(new Color(0.5f, 1f, 1f), 0.4f);
    }

    public void OnBallCollision(BallController other)
        => other.TakeDamage(owner.Config.collisionDamage * 0.5f, owner);
}

// ─────────────────────────────────────────────────────────────────────────────
// 6. BERSERKER – Im mniej HP tym więcej dmg i szybkości
//    Pasywka: ataki leczą (25% lifesteal)
//    ULT: szarża kolejno do 3 celów
// ─────────────────────────────────────────────────────────────────────────────
public class BerserkWeapon : WeaponBase
{
    const float BASE_DMG      = 20f;
    const float LIFESTEAL_PCT = 0.25f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 1.2f; }

    protected override void Update()
    {
        base.Update();
        float rage  = 1f - (owner.CurrentHP / owner.MaxHP);
        float speed = owner.Config.moveSpeed * owner.SpeedMultiplier * (1f + rage * 1.5f);
        if (owner.Rb.linearVelocity.magnitude > 0.1f)
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * speed;
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float rage = 1f - (owner.CurrentHP / owner.MaxHP);
        float dmg  = ScaleDmg(BASE_DMG * (1f + rage * 2f));
        target.TakeDamage(dmg, owner);
        if (HasPassive) owner.Heal(dmg * LIFESTEAL_PCT);
        owner.FlashColor(new Color(1f, 0.2f, 0.0f), 0.1f);
        AudioController.Instance?.PlayMeleeHit();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        owner.StartCoroutine(TripleCharge(target));
    }

    IEnumerator TripleCharge(BallController firstTarget)
    {
        for (int i = 0; i < 3; i++)
        {
            BallController t = i == 0 ? firstTarget : FindNearestOther();
            if (t == null || !t.IsAlive) break;
            float rage = 1f - (owner.CurrentHP / owner.MaxHP);
            float dmg  = ScaleDmg(BASE_DMG * 1.5f * (1f + rage * 2f));
            Vector3 dir = (t.transform.position - owner.transform.position).normalized;
            owner.Rb.linearVelocity = dir * owner.Config.moveSpeed * 3f;
            owner.FlashColor(new Color(1f, 0.4f, 0f), 0.15f);
            t.TakeDamage(dmg, owner);
            if (HasPassive) owner.Heal(dmg * LIFESTEAL_PCT);
            t.ApplyKnockback(dir, 8f);
            AudioController.Instance?.PlayMeleeHit();
            yield return new WaitForSeconds(0.25f);
        }
    }

    BallController FindNearestOther()
    {
        var all = ArenaGameManager.AliveBalls;
        BallController best = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d < minD) { minD = d; best = b; }
        }
        return best;
    }

    public void OnBallCollision(BallController other)
    {
        float rage = 1f - (owner.CurrentHP / owner.MaxHP);
        other.TakeDamage(owner.Config.collisionDamage * (1f + rage), owner);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 7. NEKROMANTA – Puls AoE + Lifesteal + Minionki
//    Pasywka: przy śmierci eksplozja + spawns 10 minionków
//    ULT: osłabienie + egzekucja słabego celu → spawns 3 dzieci
// ─────────────────────────────────────────────────────────────────────────────
public class NecroWeapon : WeaponBase
{
    const float PULSE_DMG    = 15f;
    const float PULSE_RADIUS = 4f;
    const float LIFESTEAL    = 0.4f;
    const float MINION_DMG   = 20f;
    const int   MAX_MINIONS  = 3;

    private static readonly Color NECRO_BODY = new Color(0.25f, 0f, 0.35f, 1f);
    private static readonly Color NECRO_DOT  = new Color(0.8f,  0f, 1f,    0.9f);

    private readonly List<GameObject> _minions = new List<GameObject>();

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 3.0f; }

    public override bool OnPreDeath()
    {
        if (!HasPassive) return false;
        // Eksplozja śmierci
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d < PULSE_RADIUS * 1.5f)
            {
                b.TakeDamage(ScaleDmg(PULSE_DMG * 2f), owner);
                b.ApplySlowness(0.01f, 0.5f, 2f, owner);  // chwilowe zatrzymanie
            }
        }
        AttackRingFX.SpawnWave(owner.transform.position, NECRO_DOT, PULSE_RADIUS * 1.5f, 0.45f);
        ArenaEvents.FireAoE(owner.transform.position, NECRO_BODY, PULSE_RADIUS * 1.5f);
        // 10 minionków
        for (int i = 0; i < 10; i++) SpawnMinion(owner.transform.position);
        return false;  // pozwól na śmierć po eksplozji
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        var all = ArenaGameManager.AliveBalls;
        float totalDmg = 0f;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > PULSE_RADIUS) continue;
            float dmg = ScaleDmg(PULSE_DMG * (1f - d / PULSE_RADIUS));
            b.TakeDamage(dmg, owner);
            b.ApplyKnockback((b.transform.position - owner.transform.position).normalized, 5f);
            totalDmg += dmg;
        }
        owner.Heal(totalDmg * LIFESTEAL);
        owner.FlashColor(new Color(0.5f, 0f, 0.8f), 0.3f);
        owner.PunchScale(1.5f, 0.3f);
        AttackRingFX.SpawnWave(owner.transform.position, new Color(0.4f, 0f, 0.6f), PULSE_RADIUS, 0.38f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, PULSE_RADIUS);
        AudioController.Instance?.PlayMeleeHit();
        SpawnMinion(owner.transform.position);
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        if (target == null || !target.IsAlive) return;
        target.ApplyWeaken(0.5f, 5f);
        bool execute = (target.CurrentHP / target.MaxHP) < 0.3f;
        if (execute)
        {
            Vector3 pos = target.transform.position;
            target.TakeHolyDamage(target.CurrentHP + 1f, owner);  // egzekucja
            for (int i = 0; i < 3; i++) SpawnMinion(pos);
            AttackRingFX.SpawnWave(pos, NECRO_DOT, 2.5f, 0.35f);
        }
        else
        {
            target.TakeDamage(ScaleDmg(PULSE_DMG * 1.5f), owner);
        }
        owner.FlashColor(NECRO_DOT, 0.4f);
    }

    void SpawnMinion(Vector3 pos)
    {
        _minions.RemoveAll(m => m == null);
        if (_minions.Count >= MAX_MINIONS + (HasPassive ? 7 : 0)) return;
        var go = new GameObject("NecroMinion");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.38f * ArenaScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BallArenaUtils.CircleSprite; sr.color = NECRO_BODY; sr.sortingOrder = 2;
        var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>(); col.radius = 0.5f; col.isTrigger = true;
        var minion = go.AddComponent<DruidMinion>();
        minion.Initialize(owner, ScaleDmg(MINION_DMG), NECRO_DOT);
        _minions.Add(go);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 8. ELEMENTALISTA – Rotacja żywiołów
//    Pasywka: szybsze (+30%) i silniejsze (+25%) pociski
//    ULT: 8-kierunkowa salwa 3-strzałowa
// ─────────────────────────────────────────────────────────────────────────────
public class ElementWeapon : WeaponBase
{
    const float SPEED_BASE = 6f;
    const float PASS_SPEED_MULT = 1.3f;
    const float PASS_DMG_MULT   = 1.25f;
    private int element = 0;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.2f; element = Random.Range(0, 3); }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        element = (element + 1) % 3;
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        FireElement(dir, target, element);
        AudioController.Instance?.PlayProjectileFire();
        StartCooldown(target);
    }

    void FireElement(Vector2 dir, BallController target, int el)
    {
        float spd = HasPassive ? SPEED_BASE * PASS_SPEED_MULT : SPEED_BASE;
        float dmgMult = HasPassive ? PASS_DMG_MULT : 1f;
        Color col; float dmg; ProjectileType type;
        switch (el)
        {
            case 0: col = new Color(1f, 0.3f, 0f);   dmg = 25f; type = ProjectileType.Fireball;  break;
            case 1: col = new Color(0.4f, 0.8f, 1f); dmg = 18f; type = ProjectileType.Arrow;     break;
            default:col = new Color(1f, 1f, 0.2f);   dmg = 35f; type = ProjectileType.PoisonBolt;break;
        }
        var go = BallArenaUtils.CreateBulletGO(owner.transform.position, col, ScaleBulletSize(0.2f));
        go.AddComponent<Projectile>().Initialize(owner, target, dir * ScaleBulletSpeed(spd), ScaleDmg(dmg * dmgMult), type,
            el == 0 ? 3f : 0f, 4f);
        owner.FlashColor(col, 0.15f);
    }

    protected override void FireUlt(BallController target)
    {
        // 8 kierunków × 3 strzały (opóźnione 0.1s)
        owner.StartCoroutine(OctaSalvo(target));
    }

    IEnumerator OctaSalvo(BallController target)
    {
        for (int shot = 0; shot < 3; shot++)
        {
            int el = (element + shot) % 3;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                FireElement(dir, target, el);
            }
            AudioController.Instance?.PlayProjectileFire();
            yield return new WaitForSeconds(0.12f);
        }
        owner.FlashColor(new Color(1f, 0.8f, 0.1f), 0.5f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 9. KAPŁAN – self-heal + osłabienie wroga
//    Pasywka: silniejsze debuffy + leczenie = 2×
//    ULT: święty pocisk (omija odporności, szybki, namierzający)
// ─────────────────────────────────────────────────────────────────────────────
public class PriestWeapon : WeaponBase
{
    const float HEAL_BASE    = 15f;
    const float DEBUFF_BASE  = 3f;
    const float HEAL_PASS    = 30f;
    const float DEBUFF_PASS  = 5f;
    const float ULT_DMG      = 50f;
    const float ULT_SPEED    = 9f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2.5f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float heal   = HasPassive ? HEAL_PASS    : HEAL_BASE;
        float debDur = HasPassive ? DEBUFF_PASS  : DEBUFF_BASE;
        float wkPct  = HasPassive ? 0.5f         : 0.3f;
        owner.Heal(heal);
        target.ApplyWeaken(wkPct, debDur);
        owner.FlashColor(new Color(1f, 1f, 0.6f), 0.2f);
        var go = BallArenaUtils.CreateBulletGO(owner.transform.position, new Color(1f, 1f, 0.4f), ScaleBulletSize(0.15f));
        go.AddComponent<Projectile>().Initialize(owner, target,
            (target.transform.position - owner.transform.position).normalized * ScaleBulletSpeed(4f),
            0f, ProjectileType.Arrow, 0f, 2f);
        AudioController.Instance?.PlayProjectileFire();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        Vector2 dir = (target.transform.position - owner.transform.position).normalized;
        var go  = BallArenaUtils.CreateBulletGO(owner.transform.position, new Color(1f, 1f, 0.5f), ScaleBulletSize(0.2f));
        var proj = go.AddComponent<Projectile>();
        proj.Initialize(owner, target, dir * ScaleBulletSpeed(ULT_SPEED), ScaleDmg(ULT_DMG), ProjectileType.Arrow,
            6f, 5f, isHoly: true);
        owner.FlashColor(Color.white, 0.3f);
        AudioController.Instance?.PlayProjectileFire();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 10. TYTAN – Quake AoE + wielkie zderzenia
//    Pasywka: Quake przyspiesza Tytana
//    ULT: cios = aktualna prędkość × 3 + boczny knockback
// ─────────────────────────────────────────────────────────────────────────────
public class TitanWeapon : WeaponBase
{
    const float QUAKE_DMG    = 20f;
    const float QUAKE_RADIUS = 3.5f;
    const float SPEED_BONUS  = 0.08f;   // pasywka: quake dodaje % prędkości
    const float MAX_SPEED_MULT = 2.5f;
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
        target.TakeDamage(ScaleDmg(60f), owner);
        target.ApplyKnockback((target.transform.position - owner.transform.position).normalized, 15f);
        owner.PunchScale(1.6f, 0.4f);
        owner.FlashColor(Color.white, 0.3f);
        AudioController.Instance?.PlayMeleeHit();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        if (target == null || !target.IsAlive) return;
        float spd = owner.Rb.linearVelocity.magnitude;
        float dmg = ScaleDmg(spd * 3f + 20f);
        target.TakeDamage(dmg, owner);
        // Boczny knockback (prostopadle do kierunku ataku)
        Vector2 fwd      = (target.transform.position - owner.transform.position).normalized;
        Vector2 sideways = new Vector2(-fwd.y, fwd.x);
        target.ApplyKnockback(sideways, 18f);
        target.ApplyKnockback(fwd, 8f);
        owner.PunchScale(1.8f, 0.4f);
        owner.FlashColor(Color.white, 0.4f);
        AudioController.Instance?.PlayTitanQuake();
    }

    void Quake()
    {
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > QUAKE_RADIUS) continue;
            b.TakeDamage(ScaleDmg(QUAKE_DMG * (1f - d / QUAKE_RADIUS)), owner);
            b.ApplyKnockback((b.transform.position - owner.transform.position).normalized, 3f);
        }
        AttackRingFX.SpawnWave(owner.transform.position, owner.Config.color, QUAKE_RADIUS, 0.42f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, QUAKE_RADIUS);
        AudioController.Instance?.PlayTitanQuake();
        if (HasPassive)
            owner.SpeedMultiplier = Mathf.Min(owner.SpeedMultiplier + SPEED_BONUS, MAX_SPEED_MULT);
    }

    public void OnBallCollision(BallController other)
    {
        other.TakeDamage(owner.Config.collisionDamage * 2f, owner);
        other.ApplyKnockback((other.transform.position - owner.transform.position).normalized, 8f);
        // Z pasywką zderzenie NIE zwalnia Tytana
        if (HasPassive && owner.Rb.linearVelocity.sqrMagnitude > 0.01f)
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * owner.EffectiveSpeed;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 11. DRUID – Minionki + Spowolnienie
//    Pasywka: przy śmierci AoE + zatrzymanie + dłuższe debuffy
//    ULT: atak dodaje DoT (trucizna)
// ─────────────────────────────────────────────────────────────────────────────
public class DruidWeapon : WeaponBase
{
    const float MINION_DMG       = 22f;
    const int   MAX_MINIONS      = 3;
    const float SLOW_SPEED_MULT  = 0.5f;
    const float SLOW_RANGE_REDUC = 0.45f;
    const float SLOW_DURATION    = 3.5f;
    const float SLOW_DURATION_PASS = 6f;
    const float ULT_POISON_DPS   = 10f;
    const float ULT_POISON_DUR   = 5f;

    private readonly List<GameObject> _minions = new List<GameObject>();

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 3.5f; }

    public override bool OnPreDeath()
    {
        if (!HasPassive) return false;
        // Eksplozja śmierci: AoE + zatrzymanie pobliskich
        float r = 4f;
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > r) continue;
            b.TakeDamage(ScaleDmg(25f * (1f - d / r)), owner);
            b.ApplySlowness(0.02f, SLOW_RANGE_REDUC, SLOW_DURATION_PASS, owner);
        }
        AttackRingFX.SpawnWave(owner.transform.position, new Color(0.3f, 1f, 0.3f), r, 0.42f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, r);
        return false;
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float dur = HasPassive ? SLOW_DURATION_PASS : SLOW_DURATION;
        target.ApplySlowness(SLOW_SPEED_MULT, SLOW_RANGE_REDUC, dur, owner);
        owner.FlashColor(new Color(0.3f, 1f, 0.3f), 0.2f);
        AudioController.Instance?.PlayMeleeHit();
        _minions.RemoveAll(m => m == null);
        if (_minions.Count < MAX_MINIONS)
        {
            var go = new GameObject("DruidMinion");
            go.transform.position   = owner.transform.position;
            go.transform.localScale = Vector3.one * 0.38f * ArenaScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BallArenaUtils.CircleSprite; sr.color = new Color(0.25f, 0.85f, 0.3f, 1f); sr.sortingOrder = 2;
            var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>(); col.radius = 0.5f; col.isTrigger = true;
            var minion = go.AddComponent<DruidMinion>();
            minion.Initialize(owner, ScaleDmg(MINION_DMG));
            _minions.Add(go);
        }
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        if (target == null || !target.IsAlive) return;
        target.ApplyPoison(ULT_POISON_DPS, ULT_POISON_DUR, owner);
        target.FlashColor(new Color(0.4f, 1f, 0.2f), 0.4f);
        owner.FlashColor(new Color(0.2f, 1f, 0.4f), 0.25f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 12. TECHNIK – co 5s stawia wieżyczkę
//    Pasywka: max 5 wieżyczek (zamiast 3)
//    ULT: stawia gigantyczną wieżyczkę rakietową
// ─────────────────────────────────────────────────────────────────────────────
public class TechWeapon : WeaponBase
{
    const float TURRET_DMG      = 18f;
    const float MEGA_TURRET_DMG = 35f;
    private readonly List<GameObject> _turrets = new List<GameObject>();

    private int MaxTurrets => HasPassive ? 5 : 3;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 5f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        _turrets.RemoveAll(t => t == null);
        if (_turrets.Count >= MaxTurrets)
        {
            Destroy(_turrets[0]);
            _turrets.RemoveAt(0);
        }
        SpawnTurret(TURRET_DMG, false);
        owner.FlashColor(new Color(0.3f, 0.8f, 1f), 0.2f);
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        SpawnTurret(ScaleDmg(MEGA_TURRET_DMG), true);
        AttackRingFX.SpawnWave(owner.transform.position, new Color(1f, 0.5f, 0.1f), 2.5f, 0.35f);
        owner.FlashColor(new Color(1f, 0.6f, 0.1f), 0.4f);
    }

    void SpawnTurret(float dmg, bool isMega)
    {
        float s = ArenaScale;
        var go = new GameObject("TechTurret");
        go.transform.position   = owner.transform.position;
        go.transform.localScale = Vector3.one * (isMega ? 0.85f : 0.55f) * s;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BallArenaUtils.CircleSprite;
        sr.color  = isMega ? new Color(1f, 0.5f, 0.1f, 1f) : new Color(0.3f, 0.75f, 1f, 1f);
        sr.sortingOrder = 2;
        var turret = go.AddComponent<TechTurret>();
        turret.Initialize(owner, dmg, isMega, s);
        if (!isMega) _turrets.Add(go);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 13. GLITCH – Chaotyczny
//    Pasywka: kolizja → AoE obrażenia wokół
//    ULT: boost prędkości + następny atak = dmg × prędkość
// ─────────────────────────────────────────────────────────────────────────────
public class GlitchWeapon : WeaponBase
{
    const float BASE_DMG = 20f;
    const float AoE_RADIUS = 3f;
    private static readonly Color[] CHAOS_COLORS =
    {
        new Color(1f, 0f, 0.8f), new Color(0f, 1f, 0.9f),
        new Color(1f, 0.9f, 0f), new Color(0.5f, 0f, 1f),
        new Color(0f, 1f, 0.3f), new Color(1f, 0.3f, 0f),
    };
    private int   _colorIdx;
    private float _speedBurstDmg = -1f;  // -1 = brak oczekującego ult

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 2f; }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float dmg;
        if (_speedBurstDmg >= 0f)
        {
            dmg = ScaleDmg(_speedBurstDmg);
            _speedBurstDmg = -1f;
        }
        else
        {
            dmg = ScaleDmg(BASE_DMG);
        }
        target.TakeDamage(dmg, owner);
        owner.FlashColor(CHAOS_COLORS[_colorIdx % CHAOS_COLORS.Length], 0.15f);
        AudioController.Instance?.PlayMeleeHit();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        float spd = owner.Rb.linearVelocity.magnitude;
        owner.SpeedMultiplier = Mathf.Min(owner.SpeedMultiplier + 0.5f, 3f);
        if (owner.Rb.linearVelocity.sqrMagnitude > 0.01f)
            owner.Rb.linearVelocity = owner.Rb.linearVelocity.normalized * owner.EffectiveSpeed;
        _speedBurstDmg = spd * 2.5f + BASE_DMG;
        _colorIdx++;
        owner.FlashColor(CHAOS_COLORS[_colorIdx % CHAOS_COLORS.Length], 0.4f);
        owner.StartCoroutine(ResetSpeedMult());
    }

    IEnumerator ResetSpeedMult()
    {
        yield return new WaitForSeconds(3f);
        if (owner != null) owner.SpeedMultiplier = Mathf.Max(owner.SpeedMultiplier - 0.5f, 1f);
    }

    public void OnBallCollision(BallController other)
    {
        owner.ScaledAttackRange = Random.Range(1.5f, 7f);
        Cooldown  = Random.Range(0.6f, 4f);
        _colorIdx++;
        Color c = CHAOS_COLORS[_colorIdx % CHAOS_COLORS.Length];
        owner.FlashColor(c, 0.3f);
        other.TakeDamage(ScaleDmg(Random.Range(8f, 35f)), owner);

        if (HasPassive)
        {
            // AoE wokół miejsca zderzenia
            var all = ArenaGameManager.AliveBalls;
            foreach (var b in all)
            {
                if (b == owner || b == other || !b.IsAlive) continue;
                float d = Vector2.Distance(other.transform.position, b.transform.position);
                if (d < AoE_RADIUS)
                    b.TakeDamage(ScaleDmg(BASE_DMG * 0.6f * (1f - d / AoE_RADIUS)), owner);
            }
            AttackRingFX.SpawnWave(other.transform.position, c, AoE_RADIUS, 0.3f);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 14. PSYCHIC – Odpychanie + Bezpośredni atak
//    Pasywka: atak uderza 2 cele (w tym minionki)
//    ULT: bańka czasu (freeze pobliskich 2s) → wielki repel + nieśmiertelność
// ─────────────────────────────────────────────────────────────────────────────
public class PsychicWeapon : WeaponBase
{
    const float REPEL_RADIUS = 7f;
    const float REPEL_FORCE  = 14f;
    const float DIRECT_DMG   = 18f;
    const float REPEL_CD     = 0.45f;
    const float ULT_FREEZE   = 2f;
    const float ULT_REPEL    = 30f;
    const float ULT_INVULN   = 2.5f;

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

    private void OnDestroy() { if (owner != null) owner.OnDamageTaken -= OnHit; }

    void OnHit(float amount, BallController source)
    {
        if (_repelTimer > 0f) return;
        _repelTimer = REPEL_CD;
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > REPEL_RADIUS) continue;
            b.Rb.AddForce((b.transform.position - owner.transform.position).normalized * REPEL_FORCE, ForceMode2D.Impulse);
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
        if (HasPassive)
        {
            // Drugi cel (najbliższy inny)
            BallController second = FindSecondTarget(target);
            if (second != null)
            {
                second.TakeDamage(ScaleDmg(DIRECT_DMG * 0.7f), owner);
                second.ApplyKnockback((second.transform.position - owner.transform.position).normalized, 6f);
            }
        }
        owner.FlashColor(new Color(0.6f, 0.1f, 1f), 0.2f);
        AudioController.Instance?.PlayMeleeHit();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        owner.StartCoroutine(TimeFreezeUlt());
    }

    IEnumerator TimeFreezeUlt()
    {
        // Zamroź pobliskich wrogów
        float r = REPEL_RADIUS * 1.2f;
        var frozen = new List<(BallController, Vector2)>();
        var allBalls = ArenaGameManager.AliveBalls;
        foreach (var b in allBalls)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d > r) continue;
            frozen.Add((b, b.Rb.linearVelocity));
            b.Rb.linearVelocity = Vector2.zero;
            b.Rb.constraints    = RigidbodyConstraints2D.FreezeAll;
            b.FlashColor(new Color(0.8f, 0.4f, 1f), ULT_FREEZE);
        }
        AttackRingFX.SpawnWave(owner.transform.position, new Color(0.7f, 0.3f, 1f), r, 0.5f);

        owner.invincible = true;
        yield return new WaitForSeconds(ULT_FREEZE);

        // Odmroź + wielki repel
        foreach (var (b, vel) in frozen)
        {
            if (b == null || !b.IsAlive) continue;
            b.Rb.constraints    = RigidbodyConstraints2D.FreezeRotation;
            b.Rb.linearVelocity = vel.sqrMagnitude > 0.01f ? vel : Random.insideUnitCircle.normalized * 3f;
            b.Rb.AddForce((b.transform.position - owner.transform.position).normalized * ULT_REPEL, ForceMode2D.Impulse);
            b.StartPsychicRepel(owner);
            b.TakeDamage(ScaleDmg(35f), owner);
        }
        AttackRingFX.SpawnWave(owner.transform.position, new Color(0.9f, 0.5f, 1f), r * 1.2f, 0.45f);
        ArenaEvents.FireAoE(owner.transform.position, owner.Config.color, r);

        yield return new WaitForSeconds(ULT_INVULN - ULT_FREEZE);
        if (owner != null) owner.invincible = false;
    }

    BallController FindSecondTarget(BallController exclude)
    {
        var all = ArenaGameManager.AliveBalls;
        BallController best = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == owner || b == exclude || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d < minD) { minD = d; best = b; }
        }
        // Sprawdź też minionki (DruidMinion/NecroMinion) wśród wszystkich koliderów
        return best;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 16. MARIACHI – Rewolwer z predictive aim; respawn raz
//    Pasywka: respawn na 100% HP
//    ULT: ultra-szybki pocisk (3× dmg) do dowolnego wroga w arenie
// ─────────────────────────────────────────────────────────────────────────────
public class MariachWeapon : WeaponBase
{
    const float BASE_DMG        = 20f;
    const float BULLET_SPEED    = 16f;
    const float ULT_SPEED_MULT  = 2f;
    const float ULT_DMG_MULT    = 3f;
    const float BUFF_DELAY      = 3f;
    const float ATKSPD_PER_BUFF = 0.1f;
    const float MAX_ATKSPD_MULT = 3.0f;
    const float HP_PER_BUFF     = 4f;

    private bool  _hasRespawned = false;
    private float _atkSpdMult   = 1f;
    private float _baseCooldown;
    private float _bonusDmg     = 0f;

    public override float RespawnHPFraction => HasPassive ? 1.0f : 0.5f;

    private static Bounds _arenaBounds;
    private static bool   _boundsReady = false;

    /// <summary>Precache — wywołaj z ArenaGameManager.Start żeby nie liczyć przy pierwszym ataku.</summary>
    public static void PrecacheArenaBounds()
    {
        float h = BallController.ArenaHalf - 0.5f;
        _arenaBounds = new Bounds(Vector3.zero, new Vector3(h * 2f, h * 2f, 1f));
        _boundsReady = true;
    }

    public override void Initialize(BallController ownerBall)
    {
        base.Initialize(ownerBall);
        _baseCooldown = 1.4f;
        Cooldown      = _baseCooldown;
        if (!_boundsReady) PrecacheArenaBounds();
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        FireBullet(target, ScaleDmg(BASE_DMG + _bonusDmg), BULLET_SPEED, true);
        AudioController.Instance?.PlayMariachiBullet();
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        BallController t = target != null && target.IsAlive ? target : FindAnyEnemy();
        if (t == null) return;
        FireBullet(t, ScaleDmg((BASE_DMG + _bonusDmg) * ULT_DMG_MULT), BULLET_SPEED * ULT_SPEED_MULT, false);
        owner.FlashColor(new Color(1f, 0.9f, 0.2f), 0.4f);
        AudioController.Instance?.PlayMariachiBullet();
    }

    void FireBullet(BallController target, float dmg, float speed, bool withBuff)
    {
        Vector2 aimPos = PredictIntercept(target, speed);
        Vector2 dir    = (aimPos - (Vector2)owner.transform.position).normalized;
        var go   = BallArenaUtils.CreateBulletGO(owner.transform.position + (Vector3)(dir * 0.6f),
            new Color(1f, 0.55f, 0.05f), ScaleBulletSize(0.16f));
        var proj = go.AddComponent<Projectile>();
        proj.Initialize(owner, target, dir * ScaleBulletSpeed(speed), dmg, ProjectileType.Arrow, 0f, 3.5f);
        if (withBuff) proj.OnHitCallback = OnDamageDealt;
        owner.FlashColor(new Color(1f, 0.6f, 0.1f), 0.12f);
    }

    BallController FindAnyEnemy()
    {
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all) if (b != owner && b.IsAlive) return b;
        return null;
    }

    void OnDamageDealt() => owner.StartCoroutine(DelayedBuff());

    IEnumerator DelayedBuff()
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
        owner.StartCoroutine(RespawnRoutine());
        return true;
    }

    IEnumerator RespawnRoutine()
    {
        owner.PunchScale(1.6f, 0.3f);
        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            owner.FlashColor(new Color(1f, 0.85f, 0.3f), 0.08f);
            yield return new WaitForSeconds(0.12f);
        }
    }

    Vector2 PredictIntercept(BallController target, float bulletSpeed)
    {
        if (!_boundsReady || target == null) return target != null
            ? (Vector2)target.transform.position : (Vector2)owner.transform.position;
        Vector2 shootPos  = owner.transform.position;
        Vector2 targetPos = target.transform.position;
        Vector2 targetVel = target.Rb != null ? target.Rb.linearVelocity : Vector2.zero;
        if (targetVel.sqrMagnitude < 0.01f) return targetPos;
        const float MAX_T = 3f; const int STEPS = 48;
        float bestT = 0f, bestErr = float.MaxValue;
        for (int i = 0; i <= STEPS; i++)
        {
            float t   = MAX_T * i / STEPS;
            Vector2 p = SimulateWithBounce(targetPos, targetVel, t);
            float err = Mathf.Abs(Vector2.Distance(shootPos, p) - bulletSpeed * t);
            if (err < bestErr) { bestErr = err; bestT = t; }
        }
        return SimulateWithBounce(targetPos, targetVel, bestT);
    }

    static Vector2 SimulateWithBounce(Vector2 pos, Vector2 vel, float time)
    {
        if (!_boundsReady || vel.sqrMagnitude < 0.001f) return pos + vel * time;
        Vector2 mn = _arenaBounds.min, mx = _arenaBounds.max;
        float remaining = time;
        for (int b = 0; b < 8 && remaining > 0.0001f; b++)
        {
            float txWall = vel.x > 0f ? (mx.x - pos.x) / vel.x : vel.x < 0f ? (mn.x - pos.x) / vel.x : float.MaxValue;
            float tyWall = vel.y > 0f ? (mx.y - pos.y) / vel.y : vel.y < 0f ? (mn.y - pos.y) / vel.y : float.MaxValue;
            float tHit   = Mathf.Min(txWall, tyWall);
            if (tHit <= 0.0001f || tHit >= remaining) { pos += vel * remaining; break; }
            pos += vel * tHit; remaining -= tHit;
            if (Mathf.Abs(txWall - tyWall) < 0.001f) { vel.x = -vel.x; vel.y = -vel.y; }
            else if (txWall < tyWall) vel.x = -vel.x;
            else vel.y = -vel.y;
        }
        return pos;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 15. NERD – obrażenia Fibonacciego
//    Pasywka: pasywny laser (ciągłe obrażenia do najbliższego wroga)
//    ULT: 2× grubszy laser + self-knockback
// ─────────────────────────────────────────────────────────────────────────────
public class NerdWeapon : WeaponBase
{
    private static readonly int[] Fibs = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610 };
    private int   _fibIdx = 0;
    private float _laserTimer = 0f;
    const  float  LASER_TICK = 0.1f;
    const  float  LASER_DPS  = 8f;

    public override void Initialize(BallController ownerBall)
    { base.Initialize(ownerBall); Cooldown = 1.8f; }

    protected override void Update()
    {
        base.Update();
        if (!HasPassive) return;
        _laserTimer -= Time.deltaTime;
        if (_laserTimer <= 0f)
        {
            _laserTimer = LASER_TICK;
            BallController nearest = FindNearest();
            if (nearest != null)
            {
                nearest.TakeDamage(ScaleDmg(LASER_DPS * LASER_TICK), owner);
                nearest.FlashColor(new Color(0.4f, 0.6f, 1f), 0.05f);
            }
        }
    }

    public override void Attack(BallController target)
    {
        if (!IsReady) return;
        float dmg = ScaleDmg(Fibs[_fibIdx]);
        target.TakeDamage(dmg, owner);
        float t = (float)_fibIdx / (Fibs.Length - 1);
        owner.FlashColor(Color.Lerp(new Color(0.4f, 0.6f, 1f), new Color(1f, 0.1f, 0.1f), t), 0.2f);
        AudioController.Instance?.PlayMeleeHit();
        _fibIdx = Mathf.Min(_fibIdx + 1, Fibs.Length - 1);
        StartCooldown(target);
    }

    protected override void FireUlt(BallController target)
    {
        if (target == null || !target.IsAlive) return;
        // 2× grubszy laser: instant hit, duże obrażenia
        float dmg = ScaleDmg(Fibs[_fibIdx] * 2f + 80f);
        target.TakeDamage(dmg, owner);
        // Self-knockback w przeciwnym kierunku
        Vector2 dir = ((Vector2)target.transform.position - (Vector2)owner.transform.position).normalized;
        owner.ApplyKnockback(-dir, 20f);
        owner.FlashColor(new Color(0.2f, 0.5f, 1f), 0.5f);
        owner.PunchScale(1.3f, 0.25f);
        _fibIdx = Mathf.Min(_fibIdx + 1, Fibs.Length - 1);
        AudioController.Instance?.PlayMeleeHit();
    }

    BallController FindNearest()
    {
        var all = ArenaGameManager.AliveBalls;
        BallController best = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(owner.transform.position, b.transform.position);
            if (d < minD) { minD = d; best = b; }
        }
        return best;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// FirePuddle – kałuża ognia zostawiana przez ULT Maga
// ─────────────────────────────────────────────────────────────────────────────
public class FirePuddle : MonoBehaviour
{
    private BallController owner;
    private float          dps;
    private float          lifetime;
    private float          elapsed;
    private float          tickTimer;
    const  float           TICK      = 0.25f;
    const  float           RADIUS    = 2.5f;
    private SpriteRenderer sr;

    public static void Spawn(Vector3 pos, BallController owner, float dps, float lifetime)
    {
        var go = new GameObject("FirePuddle");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * RADIUS * 2f;
        var puddle = go.AddComponent<FirePuddle>();
        puddle.owner    = owner;
        puddle.dps      = dps;
        puddle.lifetime = lifetime;
        var sr_comp     = go.AddComponent<SpriteRenderer>();
        sr_comp.sprite  = BallArenaUtils.CircleSprite;
        sr_comp.color   = new Color(1f, 0.3f, 0f, 0.55f);
        sr_comp.sortingOrder = -2;
        puddle.sr       = sr_comp;
    }

    private void Update()
    {
        elapsed   += Time.deltaTime;
        tickTimer += Time.deltaTime;
        float alpha = Mathf.Lerp(0.55f, 0f, elapsed / lifetime);
        if (sr != null) sr.color = new Color(1f, 0.3f, 0f, alpha);
        if (elapsed >= lifetime) { Destroy(gameObject); return; }

        if (tickTimer < TICK) return;
        tickTimer = 0f;
        var all = ArenaGameManager.AliveBalls;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < RADIUS)
                b.TakeDamage(dps * TICK, owner);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Helper – samodzielny fader cienia (działa niezależnie od życia kulki)
// ─────────────────────────────────────────────────────────────────────────────
public class ShadowFader : MonoBehaviour
{
    private float          _duration;
    private float          _elapsed;
    private SpriteRenderer _sr;
    private Color          _startColor;

    public void Init(float duration)
    {
        _duration   = duration;
        _sr         = GetComponent<SpriteRenderer>();
        _startColor = _sr.color;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t   = _elapsed / _duration;
        _sr.color = new Color(_startColor.r, _startColor.g, _startColor.b,
                              Mathf.Lerp(_startColor.a, 0f, t));
        transform.localScale *= (1f + Time.deltaTime * 1.2f);
        if (_elapsed >= _duration) Destroy(gameObject);
    }
}
