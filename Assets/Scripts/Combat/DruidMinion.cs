// DruidMinion.cs – minionek Druida: dynamiczna fizyka, odbija się jak kulka
using UnityEngine;

public class DruidMinion : MonoBehaviour
{
    private BallController parent;
    private float          damage;
    private Rigidbody2D    rb;
    private GameObject     _spinPivot;
    private Color          _dotColor = new Color(1f, 1f, 0.3f, 0.95f);
    private const float    SPEED    = 5.5f;
    private const float    STEER    = 6f;
    private BallController _cachedTarget;
    private float          _targetTimer;

    public void Initialize(BallController parentBall, float dmg, Color dotColor = default)
    {
        parent = parentBall;
        damage = dmg;
        if (dotColor != default) _dotColor = dotColor;
        rb     = GetComponent<Rigidbody2D>();

        // Dynamic – odbija się od ścian jak normalna kulka
        rb.bodyType               = RigidbodyType2D.Dynamic;
        rb.gravityScale           = 0f;
        rb.linearDamping          = 0f;
        rb.angularDamping         = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;

        var phyMat = new PhysicsMaterial2D { bounciness = 1f, friction = 0f };
        var col    = GetComponent<CircleCollider2D>();
        col.radius         = 0.5f;
        col.isTrigger      = false; // fizyczne odbicia
        col.sharedMaterial = phyMat;
        rb.sharedMaterial  = phyMat;

        // Projectile layer żeby BallController.OnCollisionEnter2D go ignorował
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        Destroy(gameObject, 9f);

        float ang = Random.Range(0f, Mathf.PI * 2f);
        rb.linearVelocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * SPEED;

        CreateSpinIndicator();
    }

    void CreateSpinIndicator()
    {
        _spinPivot = new GameObject("SpinPivot");
        _spinPivot.transform.SetParent(transform);
        _spinPivot.transform.localPosition = Vector3.zero;
        _spinPivot.transform.localScale    = Vector3.one;

        var dot = new GameObject("SpinDot");
        dot.transform.SetParent(_spinPivot.transform);
        dot.transform.localPosition = new Vector3(0.72f, 0f, 0f);
        dot.transform.localScale    = Vector3.one * 0.3f;

        var s = dot.AddComponent<SpriteRenderer>();
        s.sprite       = BallArenaUtils.CircleSprite;
        s.color        = _dotColor;
        s.sortingOrder = 3;
    }

    private void FixedUpdate()
    {
        if (parent == null) { Destroy(gameObject); return; }

        if (_spinPivot != null)
            _spinPivot.transform.Rotate(0f, 0f, 340f * Time.fixedDeltaTime);

        // Naprowadzanie przez siłę (cache target co 0.3s)
        _targetTimer -= Time.fixedDeltaTime;
        if (_targetTimer <= 0f || _cachedTarget == null || !_cachedTarget.IsAlive)
        {
            _targetTimer = 0.3f;
            _cachedTarget = null;
            var all = ArenaGameManager.AliveBalls;
            float minD = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (b == parent || !b.IsAlive) continue;
                float d = (b.transform.position - transform.position).sqrMagnitude;
                if (d < minD) { minD = d; _cachedTarget = b; }
            }
        }
        if (_cachedTarget == null) return;
        var nearest = _cachedTarget;

        Vector2 desired = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized * SPEED;
        rb.AddForce((desired - rb.linearVelocity) * STEER, ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > SPEED * 1.3f)
            rb.linearVelocity = rb.linearVelocity.normalized * SPEED;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        var ball = col.gameObject.GetComponent<BallController>();
        if (ball == null || ball == parent || !ball.IsAlive) return;
        ball.TakeDamage(damage, parent);
        if (BallController.IsVfxAllowed()) HitParticles.Spawn(transform.position, new Color(0.3f, 0.9f, 0.2f), BallController.VfxHitCount);
        Destroy(gameObject);
    }
}
