using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected BallController owner;
    private   float          _timer;

    public float Cooldown       { get; protected set; } = 2f;
    public bool  IsReady        => _timer <= 0f;

    /// <summary>Mnożnik obrażeń ze scalenia (domyślnie 1.0)</summary>
    public float StatMultiplier { get; set; } = 1f;

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

    protected void StartCooldown() => _timer = Cooldown;

    /// <summary>Skaluj obrażenia przez StatMultiplier</summary>
    protected float ScaleDmg(float baseDmg) => baseDmg * StatMultiplier;
}
