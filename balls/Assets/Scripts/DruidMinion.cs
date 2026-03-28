// DruidMinion.cs – mały minionek Druida ścigający wrogów (kinematic, bez kolizji fizycznych)
using UnityEngine;

public class DruidMinion : MonoBehaviour
{
    private BallController parent;
    private float          damage;
    private Rigidbody2D    rb;
    private GameObject     _spinPivot;
    private float          lifetime  = 9f;
    private const float    SPEED     = 4.8f;

    public void Initialize(BallController parentBall, float dmg)
    {
        parent = parentBall;
        damage = dmg;
        rb     = GetComponent<Rigidbody2D>();

        // Kinematyczny – nie reaguje na siły/kolizje fizyczne, sterujemy przez MovePosition
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        gameObject.layer = LayerMask.NameToLayer("Projectile");
        Destroy(gameObject, lifetime);

        CreateSpinIndicator();
    }

    // Żółta mała kropka obracająca się wokół minionka – wizualne odróżnienie od kulek
    void CreateSpinIndicator()
    {
        _spinPivot = new GameObject("SpinPivot");
        _spinPivot.transform.SetParent(transform);
        _spinPivot.transform.localPosition = Vector3.zero;
        _spinPivot.transform.localScale    = Vector3.one;

        var dot = new GameObject("SpinDot");
        dot.transform.SetParent(_spinPivot.transform);
        dot.transform.localPosition = new Vector3(0.72f, 0f, 0f);
        dot.transform.localScale    = Vector3.one * 0.32f;

        var s = dot.AddComponent<SpriteRenderer>();
        s.sprite       = BallArenaUtils.CircleSprite;
        s.color        = new Color(1f, 1f, 0.3f, 0.95f);
        s.sortingOrder = 3;
    }

    private void Update()
    {
        if (parent == null) { Destroy(gameObject); return; }

        // Obracamy wskaźnik
        if (_spinPivot != null)
            _spinPivot.transform.Rotate(0f, 0f, 320f * Time.deltaTime);

        // Znajdź najbliższy cel (nie rodzic)
        var all = FindObjectsByType<BallController>(FindObjectsSortMode.None);
        BallController nearest = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == parent || !b.IsAlive) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < minD) { minD = d; nearest = b; }
        }

        if (nearest == null) return;

        Vector2 dir = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
        rb.MovePosition((Vector2)transform.position + dir * SPEED * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<BallController>();
        if (ball == null || ball == parent || !ball.IsAlive) return;
        ball.TakeDamage(damage, parent);
        HitParticles.Spawn(transform.position, new Color(0.3f, 0.9f, 0.2f));
        Destroy(gameObject);
    }
}
