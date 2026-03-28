using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class BallController : MonoBehaviour
{
    public ClassConfig Config    { get; private set; }
    public bool        IsAlive   => currentHP > 0f;
    public float       CurrentHP => currentHP;
    public Rigidbody2D Rb        { get; private set; }
    public HealthBar   healthBar;

    public string DisplayName { get; private set; }

    public event Action<BallController>        OnDeath;
    public event Func<float, float>            OnTakePhysicalDamage;
    public event Action<float, BallController> OnDamageTaken;

    // ── Stan ──────────────────────────────────────────────────────────────────
    private float          currentHP;
    private float          maxHP;
    private float          poisonDPS, poisonLeft;
    private BallController poisonSource;
    private float          weakenMult  = 1f;
    private float          weakenLeft  = 0f;
    private bool           invincible  = false;
    private int            goldReward  = 20;

    private SpriteRenderer sr;
    private WeaponBase     weapon;
    private Color          baseColor;
    private float          flashTimer;
    private Color          flashColor;
    private Vector3        baseScale;
    private SpriteRenderer glowSR;
    private float          glowPulse;

    public float ScaledAttackRange { get; set; }

    private float          _psychicRepelTimer;
    private BallController _psychicRepelSource;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// scaleMult  – mnożnik rozmiaru kulki (z tieru areny, np. 0.75)
    /// statMult   – mnożnik statystyk (ze scalenia, np. 5.0)
    /// goldMult   – mnożnik złota za śmierć (ze scalenia)
    /// </summary>
    public void Initialize(ClassConfig cfg, float scaleMult = 1f,
                           float statMult = 1f, int goldMult = 1,
                           string displayName = null)
    {
        Config      = cfg;
        maxHP       = cfg.maxHP * statMult;
        currentHP   = maxHP;
        baseColor   = cfg.color;
        goldReward  = 20 * goldMult;
        DisplayName = displayName ?? cfg.className;

        // Rozmiar: radius z configa * scaleMult areny * ewentualne skalowanie scalenia
        float mergeBonus     = statMult > 1f ? Mathf.Pow(statMult, 0.15f) : 1f;
        baseScale            = Vector3.one * cfg.radius * 2f * scaleMult * mergeBonus;
        transform.localScale = baseScale;
        ScaledAttackRange    = cfg.attackRange * scaleMult * mergeBonus;

        Rb.gravityScale           = 0f;
        Rb.linearDamping          = 0f;
        Rb.angularDamping         = 0f;
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Rb.constraints            = RigidbodyConstraints2D.FreezeRotation;

        var phyMat         = new PhysicsMaterial2D { bounciness = 1f, friction = 0f };
        var col            = GetComponent<CircleCollider2D>();
        col.radius         = 0.5f;
        col.sharedMaterial = phyMat;
        Rb.sharedMaterial  = phyMat;

        sr.sprite = cfg.ballSprite != null ? cfg.ballSprite : BallArenaUtils.CircleSprite;
        sr.color  = cfg.useMultiColor ? Color.white : baseColor;

        float speed = cfg.moveSpeed / scaleMult * 0.6f;

        CreateGlowObject();
        if (cfg.useMultiColor) CreateColorLayers(cfg);
        AttachWeapon(cfg.ballClass, statMult, speed);

        float ang         = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Rb.linearVelocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;

        if (healthBar != null) healthBar.Setup(maxHP, cfg.color, cfg.className);
    }

    void CreateGlowObject()
    {
        var glow = new GameObject("Glow");
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale    = Vector3.one * 1.4f;
        glowSR         = glow.AddComponent<SpriteRenderer>();
        glowSR.sprite  = BallArenaUtils.CircleSprite;
        glowSR.color   = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        glowSR.sortingOrder = -1;
    }

    void CreateColorLayers(ClassConfig cfg)
    {
        CreateColorLayer("Mid",  0.62f, cfg.color2, 0);
        CreateColorLayer("Core", 0.35f, cfg.color3, 1);
    }

    void CreateColorLayer(string name, float scale, Color col, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one * scale;
        var s = go.AddComponent<SpriteRenderer>();
        s.sprite       = BallArenaUtils.CircleSprite;
        s.color        = col;
        s.sortingOrder = order;
    }

    void AttachWeapon(BallClass cls, float statMult, float speed)
    {
        switch (cls)
        {
            case BallClass.Warrior:      weapon = gameObject.AddComponent<SwordWeapon>();    break;
            case BallClass.Mage:         weapon = gameObject.AddComponent<FireballWeapon>(); break;
            case BallClass.Archer:       weapon = gameObject.AddComponent<ArrowWeapon>();    break;
            case BallClass.Rogue:        weapon = gameObject.AddComponent<PoisonWeapon>();   break;
            case BallClass.Paladin:      weapon = gameObject.AddComponent<ShieldWeapon>();   break;
            case BallClass.Berserker:    weapon = gameObject.AddComponent<BerserkWeapon>();  break;
            case BallClass.Necromancer:  weapon = gameObject.AddComponent<NecroWeapon>();    break;
            case BallClass.Elementalist: weapon = gameObject.AddComponent<ElementWeapon>();  break;
            case BallClass.Priest:       weapon = gameObject.AddComponent<PriestWeapon>();   break;
            case BallClass.Titan:        weapon = gameObject.AddComponent<TitanWeapon>();    break;
            case BallClass.Druid:        weapon = gameObject.AddComponent<DruidWeapon>();    break;
            case BallClass.Technician:   weapon = gameObject.AddComponent<TechWeapon>();     break;
            case BallClass.Glitch:       weapon = gameObject.AddComponent<GlitchWeapon>();   break;
            case BallClass.Psychic:      weapon = gameObject.AddComponent<PsychicWeapon>();  break;
            case BallClass.Nerd:         weapon = gameObject.AddComponent<NerdWeapon>();     break;
        }
        weapon.Initialize(this);
        weapon.StatMultiplier = statMult;
    }

    public void SetInvincibleGlow(bool on) => invincible = on;

    private void Update()
    {
        if (!IsAlive) return;
        HandlePoison(); HandleWeaken(); HandleFlash(); HandleGlow();
        HandleAI(); ClampSpeed();
        if (_psychicRepelTimer > 0f) _psychicRepelTimer -= Time.deltaTime;
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
    }

    void HandleAI()
    {
        var target = FindNearest();
        if (target == null) return;
        if (Vector2.Distance(transform.position, target.transform.position) <= ScaledAttackRange
            && weapon.IsReady)
            weapon.Attack(target);
    }

    BallController FindNearest()
    {
        var all = FindObjectsByType<BallController>(FindObjectsSortMode.None);
        BallController nearest = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == this || !b.IsAlive) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < minD) { minD = d; nearest = b; }
        }
        return nearest;
    }

    void ClampSpeed()
    {
        float speed = Config.moveSpeed;
        if (Rb.linearVelocity.magnitude < speed * 0.3f)
            Rb.linearVelocity = Rb.linearVelocity.normalized * speed;
    }

    public void TakeDamage(float amount, BallController source)
    {
        if (!IsAlive || invincible) return;
        currentHP = Mathf.Max(currentHP - amount, 0f);
        FlashColor(Color.red, 0.12f);
        HitParticles.Spawn(transform.position, baseColor);
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
        OnDamageTaken?.Invoke(amount, source);
        if (currentHP <= 0f) Die(source);
    }

    public void StartPsychicRepel(BallController source)
    {
        _psychicRepelTimer  = 0.7f;
        _psychicRepelSource = source;
    }

    public void TakePhysicalDamage(float amount, BallController source)
    {
        if (invincible) return;
        float modified = OnTakePhysicalDamage != null ? OnTakePhysicalDamage(amount) : amount;
        TakeDamage(modified, source);
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        FlashColor(new Color(0.2f, 1f, 0.4f), 0.15f);
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
    }

    public void ApplyPoison(float dps, float duration, BallController source)
    {
        if (invincible) return;
        poisonDPS    = Mathf.Max(poisonDPS, dps);
        poisonLeft   = Mathf.Max(poisonLeft, duration);
        poisonSource = source;
    }

    public void ApplyKnockback(Vector3 dir, float force)
        => Rb.AddForce(dir.normalized * force, ForceMode2D.Impulse);

    public void ApplyWeaken(float pct, float duration)
    { weakenMult = 1f - pct; weakenLeft = duration; }

    public float ApplyOutgoingWeaken(float dmg) => dmg * weakenMult;

    void HandlePoison()
    {
        if (poisonLeft <= 0f) return;
        if (invincible) { poisonLeft -= Time.deltaTime; return; }
        poisonLeft -= Time.deltaTime;
        TakeDamage(poisonDPS * Time.deltaTime, poisonSource);
        sr.color = Color.Lerp(sr.color, new Color(0.4f, 1f, 0.2f), 0.15f);
    }

    void HandleWeaken()
    {
        if (weakenLeft <= 0f) { weakenMult = 1f; return; }
        weakenLeft -= Time.deltaTime;
        sr.color = Color.Lerp(sr.color, new Color(0.8f, 0.5f, 1f), 0.1f);
    }

    void HandleGlow()
    {
        if (glowSR == null) return;
        if (invincible)
        {
            glowPulse += Time.deltaTime * 4f;
            float a = (Mathf.Sin(glowPulse) + 1f) * 0.5f * 0.6f;
            glowSR.color = new Color(1f, 1f, 0.3f, a);
            glowSR.transform.localScale = Vector3.one * (1.3f + Mathf.Sin(glowPulse) * 0.1f);
        }
        else glowSR.color = Color.Lerp(glowSR.color, new Color(0,0,0,0), Time.deltaTime * 8f);
    }

    void Die(BallController killer)
    {
        BallDeathParticles.Spawn(transform.position, baseColor);
        if (GameData.Instance != null) GameData.Instance.gold += goldReward;
        if (KillFeed.Instance != null && killer != null)
            KillFeed.Instance.ReportKill(killer.DisplayName, killer.Config.color,
                                         DisplayName, baseColor);
        OnDeath?.Invoke(killer);
        Rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        if (healthBar != null) healthBar.gameObject.SetActive(false);
    }

    public void FlashColor(Color col, float duration) { flashColor = col; flashTimer = duration; }

    void HandleFlash()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            sr.color = flashTimer > 0f ? Color.Lerp(baseColor, flashColor, flashTimer * 6f) : baseColor;
        }
        else if (poisonLeft <= 0f && weakenLeft <= 0f)
            sr.color = Color.Lerp(sr.color, baseColor, Time.deltaTime * 5f);
    }

    public void PunchScale(float scale, float duration) => StartCoroutine(PunchRoutine(scale, duration));

    IEnumerator PunchRoutine(float scale, float duration)
    {
        transform.localScale = baseScale * scale;
        yield return new WaitForSeconds(duration);
        transform.localScale = baseScale;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // Psychic: uderzenie w ścianę po odrzucie
        if (_psychicRepelTimer > 0f && col.gameObject.name.StartsWith("Wall_"))
        {
            TakeDamage(28f, _psychicRepelSource);
            Rb.linearVelocity  *= 0.15f;
            _psychicRepelTimer  = 0f;
            return;
        }

        var other = col.gameObject.GetComponent<BallController>();
        if (other == null || !other.IsAlive) return;
        FlashColor(Color.white, 0.05f);
        float dmg = Config.collisionDamage * weakenMult * (weapon != null ? weapon.StatMultiplier : 1f);
        other.TakePhysicalDamage(dmg, this);
        switch (Config.ballClass)
        {
            case BallClass.Warrior:   GetComponent<SwordWeapon>()?.OnBallCollision(other);   break;
            case BallClass.Rogue:     GetComponent<PoisonWeapon>()?.OnBallCollision(other);  break;
            case BallClass.Paladin:   GetComponent<ShieldWeapon>()?.OnBallCollision(other);  break;
            case BallClass.Berserker: GetComponent<BerserkWeapon>()?.OnBallCollision(other); break;
            case BallClass.Titan:     GetComponent<TitanWeapon>()?.OnBallCollision(other);   break;
            case BallClass.Glitch:    GetComponent<GlitchWeapon>()?.OnBallCollision(other);  break;
        }
    }
}
