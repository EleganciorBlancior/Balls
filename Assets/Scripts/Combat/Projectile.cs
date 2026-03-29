using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private BallController owner;
    private BallController homingTarget;
    private float          damage;
    private ProjectileType pType;
    private float          homingStrength;
    private Rigidbody2D    rb;

    /// <summary>Opcjonalny callback wywoływany gdy pocisk trafi w cel (przed Destroy).</summary>
    public System.Action OnHitCallback;

    public void Initialize(BallController owner, BallController target,
                           Vector2 vel, float dmg,
                           ProjectileType type, float homing, float lifetime)
    {
        this.owner      = owner;
        homingTarget    = target;
        damage          = dmg;
        pType           = type;
        homingStrength  = homing;

        rb              = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = vel;

        gameObject.layer = LayerMask.NameToLayer("Projectile");
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (homingStrength > 0f && homingTarget != null && homingTarget.IsAlive)
        {
            Vector2 dir = ((Vector2)homingTarget.transform.position - rb.position).normalized;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity,
                dir * rb.linearVelocity.magnitude,
                homingStrength * Time.fixedDeltaTime);
        }
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            transform.right = rb.linearVelocity.normalized;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<BallController>();
        if (ball == null || ball == owner || !ball.IsAlive) return;

        ball.TakeDamage(damage, owner);
        if (pType == ProjectileType.PoisonBolt)
            ball.ApplyPoison(6f, 3f, owner);

        AudioController.Instance?.PlayProjectileHit();
        OnHitCallback?.Invoke();
        Destroy(gameObject);
    }
}
