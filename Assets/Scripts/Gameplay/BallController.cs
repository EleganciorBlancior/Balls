using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class BallController : MonoBehaviour
{
    public ClassConfig Config    { get; private set; }
    public bool        IsAlive   => currentHP > 0f;
    public float       CurrentHP => currentHP;
    public float       MaxHP     => maxHP;
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
    public  bool           invincible  = false;
    private int            goldReward  = 20;

    private SpriteRenderer sr;
    private WeaponBase     weapon;
    private Color          baseColor;
    private bool           _hasPatternSprite;
    private float          flashTimer;
    private Color          flashColor;
    private Vector3        baseScale;
    private SpriteRenderer glowSR;
    private float          glowPulse;

    public float ScaledAttackRange { get; set; }
    public float SpeedMultiplier   { get; set; } = 1f;
    public int   BallNumber        { get; set; }
    public Color BaseColor         => baseColor;
    public float EffectiveSpeed    => Config.moveSpeed * SpeedMultiplier * (slownessLeft > 0f ? slownessSpeedMult : 1f);

    private float          _psychicRepelTimer;
    private BallController _psychicRepelSource;

    // Spowolnienie (Druid)
    private float          slownessSpeedMult = 1f;
    private float          slownessLeft      = 0f;
    private float          rangeReductMult   = 1f;
    private float          rangeReductLeft   = 0f;
    private BallController tauntTarget;
    private float          tauntLeft         = 0f;

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
        // Minimalny range = średnica tej kulki + bufor, żeby scalone kulki zawsze mogły atakować
        float ballR = cfg.radius * scaleMult * mergeBonus;
        ScaledAttackRange = Mathf.Max(cfg.attackRange * scaleMult * mergeBonus, ballR * 2f + 0.6f);

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

        // Nadpisz kolory/wzór jeśli gracz spersonalizował tę kulkę w malarni
        Color       vc1     = cfg.color;
        Color       vc2     = cfg.color2;
        Color       vc3     = cfg.color3;
        BallPattern vpat    = cfg.pattern;
        int         vstrcnt = cfg.stripeCount;
        if (GameData.Instance != null)
        {
            var custom = GameData.Instance.GetCustomization(cfg.ballClass);
            if (custom != null)
            {
                vc1 = custom.color1; vc2 = custom.color2; vc3 = custom.color3;
                vpat = custom.pattern; vstrcnt = custom.stripeCount;
                baseColor = vc1;
            }
        }

        bool hasCustom = GameData.Instance != null && GameData.Instance.GetCustomization(cfg.ballClass) != null;
        if (!hasCustom && cfg.ballSprite != null)
        {
            sr.sprite = cfg.ballSprite;
            sr.color  = Color.white;
            _hasPatternSprite = true;
        }
        else if (vpat != BallPattern.Solid)
        {
            sr.sprite = BallArenaUtils.CreatePatternSprite(vc1, vc2, vc3, vpat, vstrcnt);
            sr.color  = Color.white;
            _hasPatternSprite = true;
        }
        else
        {
            sr.sprite = BallArenaUtils.CircleSprite;
            sr.color  = baseColor;
            _hasPatternSprite = false;
        }

        float speed = cfg.moveSpeed / scaleMult * 0.6f;

        CreateGlowObject();
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
            case BallClass.Nerd:         weapon = gameObject.AddComponent<NerdWeapon>();      break;
            case BallClass.Mariachi:     weapon = gameObject.AddComponent<MariachWeapon>();   break;
        }
        weapon.Initialize(this);
        weapon.StatMultiplier = statMult;
    }

    public void SetInvincibleGlow(bool on) => invincible = on;

    private void Update()
    {
        if (!IsAlive) return;
        HandlePoison(); HandleWeaken(); HandleSlowness(); HandleFlash(); HandleGlow();
        HandleAI(); ClampSpeed();
        if (_psychicRepelTimer > 0f) _psychicRepelTimer -= Time.deltaTime;
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
    }

    void HandleAI()
    {
        if (tauntLeft > 0f) tauntLeft -= Time.deltaTime;
        BallController target = tauntLeft > 0f && tauntTarget != null && tauntTarget.IsAlive
            ? tauntTarget : FindNearest();
        if (target == null) return;
        float effectiveRange = ScaledAttackRange;
        if (rangeReductLeft > 0f && IsRangedClass())
            effectiveRange *= rangeReductMult;
        if (Vector2.Distance(transform.position, target.transform.position) <= effectiveRange && weapon.IsReady)
            weapon.Attack(target);
    }

    bool IsRangedClass()
    {
        switch (Config.ballClass)
        {
            case BallClass.Mage:
            case BallClass.Archer:
            case BallClass.Elementalist:
            case BallClass.Priest:
            case BallClass.Technician:
            case BallClass.Nerd:
            case BallClass.Mariachi:
                return true;
            default: return false;
        }
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
        float speed = EffectiveSpeed;
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

    public void AddMaxHP(float maxAmount, float currentAmount = -1f)
    {
        maxHP     += maxAmount;
        currentHP += currentAmount < 0f ? maxAmount : currentAmount;
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

    public void ApplySlowness(float speedMult, float rangeReduction, float duration, BallController taunter)
    {
        if (invincible) return;
        slownessSpeedMult = Mathf.Min(slownessSpeedMult, speedMult);
        slownessLeft      = Mathf.Max(slownessLeft, duration);
        rangeReductMult   = Mathf.Min(rangeReductMult, 1f - rangeReduction);
        rangeReductLeft   = Mathf.Max(rangeReductLeft, duration);
        tauntTarget       = taunter;
        tauntLeft         = Mathf.Max(tauntLeft, duration);
    }

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

    void HandleSlowness()
    {
        if (slownessLeft <= 0f) { slownessSpeedMult = 1f; rangeReductMult = 1f; return; }
        slownessLeft    -= Time.deltaTime;
        rangeReductLeft  = Mathf.Max(rangeReductLeft - Time.deltaTime, 0f);
        sr.color = Color.Lerp(sr.color, new Color(0.2f, 0.8f, 0.6f), 0.12f);
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
        // Daj broni szansę na zablokowanie śmierci (np. respawn Mariachi)
        if (weapon != null && weapon.OnPreDeath())
        {
            currentHP = maxHP * 0.5f;
            if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
            return;
        }

        BallDeathParticles.Spawn(transform.position, baseColor);
        ArenaEvents.FireBallDied(transform.position, baseColor);
        if (GameData.Instance != null) GameData.Instance.gold += goldReward;
        if (KillFeed.Instance != null && killer != null)
            KillFeed.Instance.ReportKill(killer, this);
        OnDeath?.Invoke(killer);
        Rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        if (healthBar != null) healthBar.gameObject.SetActive(false);
    }

    /// <summary>Wywołaj z broni żeby aplikować krótkotrwałe buffy po respawnie.</summary>
    public void TriggerRespawnBuff(float duration, float speedMult, float cooldownDiv)
        => StartCoroutine(RespawnBuffRoutine(duration, speedMult, cooldownDiv));

    IEnumerator RespawnBuffRoutine(float duration, float speedMult, float cooldownDiv)
    {
        SpeedMultiplier = speedMult;
        SetInvincibleGlow(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            // szybki blink żeby było widać że jest znieczulony
            FlashColor(new Color(1f, 0.85f, 0.3f), 0.08f);
            yield return new WaitForSeconds(0.12f);
        }
        SpeedMultiplier = 1f;
        SetInvincibleGlow(false);
        invincible = false;
    }

    public void FlashColor(Color col, float duration) { flashColor = col; flashTimer = duration; }

    void HandleFlash()
    {
        // Kulki z wzorem (pattern sprite) mają sr.color = white – nie tintować baseColor'em
        Color resetColor = _hasPatternSprite ? Color.white : baseColor;
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            sr.color = flashTimer > 0f ? Color.Lerp(resetColor, flashColor, flashTimer * 6f) : resetColor;
        }
        else if (poisonLeft <= 0f && weakenLeft <= 0f)
            sr.color = Color.Lerp(sr.color, resetColor, Time.deltaTime * 5f);
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
        AudioController.Instance?.PlayBallCollision();
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
