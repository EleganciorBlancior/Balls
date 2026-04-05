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
    public  int            MergeLevel  { get; private set; }
    private float          currentHP;
    private float          maxHP;
    private float          poisonDPS, poisonLeft;
    private BallController poisonSource;
    private float          bleedDPS,   bleedLeft;
    private BallController bleedSource;
    private float          shieldHP    = 0f;
    private float          _totalDmgReduced = 0f;
    private float          weakenMult  = 1f;
    private float          weakenLeft  = 0f;
    public  bool           invincible      = false; // od broni (ShieldWeapon, respawn buff)
    public  bool           ChampionShield  = false; // od ArenaGameManager — bronie tego nie dotykają
    private bool           IsInvincible    => invincible || ChampionShield;
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
    private GameObject     _weaponSpriteGO;
    private BallController _weaponTarget;
    private BallController _nearestCache;
    private float          _nearestCacheTimer;

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
                           string displayName = null, int mergeLevel = 0)
    {
        Config      = cfg;
        MergeLevel  = mergeLevel;
        maxHP       = cfg.maxHP * statMult;
        currentHP   = maxHP;
        baseColor   = cfg.color;
        goldReward  = 20 * goldMult;
        DisplayName = displayName ?? cfg.className;

        // Rozmiar: radius z configa * scaleMult areny — identyczny dla real i ghost
        _baseScaleSize       = cfg.radius * 2f * scaleMult;
        baseScale            = Vector3.one * _baseScaleSize;
        transform.localScale = baseScale;
        // Minimalny range: proporcjonalny do rozmiaru areny
        float ballR    = cfg.radius * scaleMult;
        float minRange = Mathf.Max(ballR * 2f + 0.6f, ArenaHalf * 0.07f);
        ScaledAttackRange = Mathf.Max(cfg.attackRange * scaleMult, minRange);

        Rb.gravityScale           = 0f;
        Rb.linearDamping          = 0f;
        Rb.angularDamping         = 0f;
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
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

        if (!HighLoadMode) CreateGlowObject();
        if (!HighLoadMode) CreateWeaponSprite(cfg);
        AttachWeapon(cfg.ballClass, statMult, speed, scaleMult);

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

    void CreateWeaponSprite(ClassConfig cfg)
    {
        if (cfg.weaponSprite == null) return;
        _weaponSpriteGO = new GameObject("WeaponSprite");
        _weaponSpriteGO.transform.SetParent(transform);
        _weaponSpriteGO.transform.localPosition = Vector3.zero;
        _weaponSpriteGO.transform.localRotation = Quaternion.identity;
        _weaponSpriteGO.transform.localScale    = Vector3.one;
        var wsr         = _weaponSpriteGO.AddComponent<SpriteRenderer>();
        wsr.sprite      = cfg.weaponSprite;
        wsr.sortingOrder = 2;
    }

    void AttachWeapon(BallClass cls, float statMult, float speed, float arenaScale)
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
        weapon.ArenaScale     = arenaScale;
        bool hasPassive = MergeLevel >= 5;
        bool hasMastery = GameData.Instance?.HasMastery(cls) ?? false;
        weapon.SetMasteryFlags(hasPassive, hasMastery);
    }

    public void SetInvincibleGlow(bool on) => invincible = on;

    // Granice areny — ustawiane przez ArenaGameManager
    public static float ArenaHalf   = 9f;
    /// <summary>Tryb wysokiego obciążenia — wyłącza kosztowne efekty wizualne.</summary>
    public static bool  HighLoadMode = false;
    /// <summary>Co ile klatek kulka wykonuje AI (staggering). Ustawiane przez ArenaGameManager.</summary>
    public static int   AiSkip       = 1;

    /// <summary>Prawdopodobieństwo spawnu VFX (0=brak, 1=każda kulka). Ustawiane przez ArenaGameManager.</summary>
    public static float VfxChance       = 1f;
    /// <summary>Prawdopodobieństwo spawnu pierścieni ataków (oddzielne od particli).</summary>
    public static float VfxAttackChance = 1f;
    /// <summary>Ilość cząsteczek przy trafieniu.</summary>
    public static int   VfxHitCount   = 12;
    /// <summary>Ilość cząsteczek przy śmierci.</summary>
    public static int   VfxDeathCount = 40;
    /// <summary>Skala cząsteczek — dopasowana do aktualnego rozmiaru kulek (1=normalny).</summary>
    public static float VfxScale      = 1f;

    /// <summary>Losuje czy ten VFX ma się pojawić.</summary>
    public static bool IsVfxAllowed()
    {
        if (VfxChance <= 0f) return false;
        if (VfxChance >= 1f) return true;
        return UnityEngine.Random.value < VfxChance;
    }

    private float _dynamicScaleMult = 1f;
    private float _baseScaleSize;   // bazowy rozmiar bez dynamic mult

    /// <summary>Płynnie zmienia skalę kulki (wywoływane przez ArenaGameManager gdy giną inne kulki).</summary>
    public void SetDynamicScale(float newMult)
    {
        _dynamicScaleMult = newMult;
        float s = _baseScaleSize * newMult;
        transform.localScale = Vector3.one * s;
        float ballR    = s * 0.5f;
        float minRange = Mathf.Max(ballR * 2f + 0.6f, ArenaHalf * 0.07f);
        ScaledAttackRange = Mathf.Max(Config.attackRange * s / (Config.radius * 2f), minRange);
        if (weapon != null) weapon.ArenaScale = s / (Config.radius * 2f);
    }

    private void FixedUpdate()
    {
        if (!IsAlive || Rb == null) return;
        float r    = transform.localScale.x * 0.5f;
        float lim  = ArenaHalf - r - 0.05f;  // mały margines żeby nie wchodzić w ramkę
        Vector2 pos = Rb.position;
        Vector2 vel = Rb.linearVelocity;
        bool clamped = false;
        if (pos.x < -lim) { pos.x = -lim; vel.x =  Mathf.Abs(vel.x); clamped = true; }
        if (pos.x >  lim) { pos.x =  lim; vel.x = -Mathf.Abs(vel.x); clamped = true; }
        if (pos.y < -lim) { pos.y = -lim; vel.y =  Mathf.Abs(vel.y); clamped = true; }
        if (pos.y >  lim) { pos.y =  lim; vel.y = -Mathf.Abs(vel.y); clamped = true; }
        if (clamped) { Rb.position = pos; Rb.linearVelocity = vel; }
    }

    private void Update()
    {
        if (!IsAlive) return;
        HandlePoison(); HandleBleed(); HandleWeaken(); HandleSlowness(); HandleFlash(); HandleGlow();
        if (tauntLeft > 0f) tauntLeft -= Time.deltaTime;  // zawsze, niezależnie od AiSkip
        HandleAI(); ClampSpeed(); RotateWeaponSprite();
        if (_psychicRepelTimer > 0f) _psychicRepelTimer -= Time.deltaTime;
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
    }

    void RotateWeaponSprite()
    {
        if (_weaponSpriteGO == null) return;
        if (_weaponTarget == null || !_weaponTarget.IsAlive) return;
        Vector2 dir   = (Vector2)(_weaponTarget.transform.position - transform.position);
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _weaponSpriteGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void HandleAI()
    {
        // AI staggering: przy dużej liczbie kulek nie każda kulka liczy AI co klatkę
        if (AiSkip > 1 && (Time.frameCount + BallNumber) % AiSkip != 0) return;

        BallController target = tauntLeft > 0f && tauntTarget != null && tauntTarget.IsAlive
            ? tauntTarget : FindNearest();
        _weaponTarget = target;
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
        // Cache przez 0.2s żeby nie szukać co klatkę przy 10k kulek
        _nearestCacheTimer -= Time.deltaTime;
        if (_nearestCacheTimer > 0f && _nearestCache != null && _nearestCache.IsAlive)
            return _nearestCache;

        var list = ArenaGameManager.AliveBalls;
        BallController nearest = null; float minD = float.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            var b = list[i];
            if (b == this || !b.IsAlive) continue;
            float d = (b.transform.position - transform.position).sqrMagnitude;
            if (d < minD) { minD = d; nearest = b; }
        }

        _nearestCache      = nearest;
        _nearestCacheTimer = HighLoadMode ? 0.5f : 0.2f;
        return nearest;
    }

    void ClampSpeed()
    {
        float speed = EffectiveSpeed;
        float mag   = Rb.linearVelocity.magnitude;
        if (mag < speed * 0.3f)
            Rb.linearVelocity = Rb.linearVelocity.normalized * speed;
        // Ogranicz max prędkość żeby kulki nie wylatywały z areny
        else if (mag > speed * 3f)
            Rb.linearVelocity = Rb.linearVelocity.normalized * speed * 3f;
    }

    public void TakeDamage(float amount, BallController source)
    {
        if (!IsAlive || IsInvincible) return;
        currentHP = Mathf.Max(currentHP - amount, 0f);
        FlashColor(Color.red, 0.12f);
        if (IsVfxAllowed()) HitParticles.Spawn(transform.position, baseColor, VfxHitCount);
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
        if (GameData.Instance != null && amount > 0f)
        {
            if (source != null) GameData.Instance.RecordDamageDealt(source.Config.ballClass, amount);
            GameData.Instance.RecordDamageTaken(Config.ballClass, amount);
        }
        OnDamageTaken?.Invoke(amount, source);
        if (currentHP <= 0f) Die(source);
    }

    public void TakeHolyDamage(float amount, BallController source)
    {
        if (!IsAlive || IsInvincible) return;
        currentHP = Mathf.Max(currentHP - amount, 0f);
        FlashColor(new Color(1f, 1f, 0.3f), 0.15f);
        if (IsVfxAllowed()) HitParticles.Spawn(transform.position, baseColor, VfxHitCount);
        if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
        if (GameData.Instance != null && amount > 0f)
        {
            if (source != null) GameData.Instance.RecordDamageDealt(source.Config.ballClass, amount);
            GameData.Instance.RecordDamageTaken(Config.ballClass, amount);
        }
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
        if (IsInvincible) return;
        float modified = OnTakePhysicalDamage != null ? OnTakePhysicalDamage(amount) : amount;
        _totalDmgReduced += (amount - modified);
        if (shieldHP > 0f)
        {
            float blocked = Mathf.Min(shieldHP, modified);
            shieldHP  -= blocked;
            modified  -= blocked;
        }
        if (modified > 0f) TakeDamage(modified, source);
    }

    public void Heal(float amount)
    {
        if (bleedLeft > 0f) return;  // bleed blocks healing
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
        if (IsInvincible) return;
        poisonDPS    = Mathf.Max(poisonDPS, dps);
        poisonLeft   = Mathf.Max(poisonLeft, duration);
        poisonSource = source;
    }

    public void ApplyBleed(float dps, float duration, BallController source)
    {
        if (IsInvincible) return;
        bleedDPS    = Mathf.Max(bleedDPS, dps);
        bleedLeft   = Mathf.Max(bleedLeft, duration);
        bleedSource = source;
    }

    public void AddShieldHP(float amount) => shieldHP += amount;

    public float GetAndConsumeReducedDmg()
    {
        float v = _totalDmgReduced;
        _totalDmgReduced = 0f;
        return v;
    }

    public void ApplyKnockback(Vector3 dir, float force)
        => Rb.AddForce(dir.normalized * force, ForceMode2D.Impulse);

    public void ApplyWeaken(float pct, float duration)
    { weakenMult = 1f - pct; weakenLeft = duration; }

    public float ApplyOutgoingWeaken(float dmg) => dmg * weakenMult;

    public void ApplySlowness(float speedMult, float rangeReduction, float duration, BallController taunter)
    {
        if (IsInvincible) return;
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
        if (IsInvincible) { poisonLeft -= Time.deltaTime; return; }
        poisonLeft -= Time.deltaTime;
        TakeDamage(poisonDPS * Time.deltaTime, poisonSource);
        sr.color = Color.Lerp(sr.color, new Color(0.4f, 1f, 0.2f), 0.15f);
    }

    void HandleBleed()
    {
        if (bleedLeft <= 0f) return;
        if (IsInvincible) { bleedLeft -= Time.deltaTime; return; }
        bleedLeft -= Time.deltaTime;
        TakeDamage(bleedDPS * Time.deltaTime, bleedSource);
        sr.color = Color.Lerp(sr.color, new Color(1f, 0.15f, 0.1f), 0.18f);
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
        // Pierścień rogua (i innych) widoczny dopiero od ≤500 kulek total
        bool glowAllowed = !HighLoadMode || IsVfxAllowed();
        if (invincible && glowAllowed)
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
            currentHP = maxHP * weapon.RespawnHPFraction;
            bleedLeft = 0f;
            if (healthBar != null) healthBar.UpdateBar(currentHP, transform.position);
            return;
        }

        if (IsVfxAllowed()) BallDeathParticles.Spawn(transform.position, baseColor, VfxDeathCount);
        ArenaEvents.FireBallDied(transform.position, baseColor);
        if (GameData.Instance != null) GameData.Instance.gold += goldReward;
        if (KillFeed.Instance != null && killer != null)
            KillFeed.Instance.ReportKill(killer, this);
        OnDeath?.Invoke(this);  // przekazujemy siebie (martwą kulkę), nie killera
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
        else if (poisonLeft <= 0f && bleedLeft <= 0f && weakenLeft <= 0f)
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
