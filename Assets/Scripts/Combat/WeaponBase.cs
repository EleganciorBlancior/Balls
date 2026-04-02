using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected BallController owner;
    private   float          _timer;
    private   int            _attackCount;

    public float Cooldown       { get; protected set; } = 2f;
    public bool  IsReady        => _timer <= 0f;
    public float StatMultiplier { get; set; } = 1f;

    // ── Mastery system ────────────────────────────────────────────────────────
    public bool HasPassive { get; private set; }
    public bool HasUlt     { get; private set; }

    /// <summary>Call after Initialize() to enable passive / ULT.</summary>
    public void SetMasteryFlags(bool hasPassive, bool hasUlt)
    {
        HasPassive = hasPassive;
        HasUlt     = hasUlt;
        if (hasPassive) OnPassiveUnlocked();
    }

    /// <summary>Called once when passive is first unlocked. Override for one-time effects (e.g. add HP).</summary>
    protected virtual void OnPassiveUnlocked() { }

    /// <summary>Fraction of maxHP restored on respawn. Override for Mariachi passive (100%).</summary>
    public virtual float RespawnHPFraction => 0.5f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public virtual void Initialize(BallController ownerBall)
    {
        owner  = ownerBall;
        _timer = Random.Range(0f, Cooldown);
    }

    protected virtual void Update()
    {
        if (_timer > 0f) _timer -= Time.deltaTime;
    }

    public abstract void Attack(BallController target);

    /// <summary>Called just before the owner dies. Return true to block death (respawn).</summary>
    public virtual bool OnPreDeath() => false;

    /// <summary>Call at the end of every Attack() instead of setting _timer manually.
    /// Counts attacks and fires ULT every 5th attack (if HasUlt).</summary>
    protected void StartCooldown(BallController target = null)
    {
        _timer = Cooldown;
        _attackCount++;
        if (HasUlt && _attackCount % 5 == 0 && target != null)
            FireUlt(target);
    }

    /// <summary>Override to implement ULT behaviour. Called automatically every 5th attack when HasUlt is true.</summary>
    protected virtual void FireUlt(BallController target) { }

    protected float ScaleDmg(float baseDmg) => baseDmg * StatMultiplier;
}
