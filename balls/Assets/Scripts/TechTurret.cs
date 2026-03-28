// TechTurret.cs – statyczna wieżyczka Technika
using UnityEngine;

public class TechTurret : MonoBehaviour
{
    private BallController owner;
    private float          damage;
    private float          fireTimer    = 0.5f; // krótkie opóźnienie przed pierwszym strzałem
    private const float    FIRE_CD      = 1.4f;
    private const float    BULLET_SPEED = 9f;
    private const float    LIFETIME     = 14f;
    private const float    RANGE        = 10f;

    // Wizualny obrót wieżyczki
    private float rotSpeed;

    public void Initialize(BallController ownerBall, float dmg)
    {
        owner    = ownerBall;
        damage   = dmg;
        rotSpeed = Random.Range(60f, 150f) * (Random.value > 0.5f ? 1f : -1f);
        Destroy(gameObject, LIFETIME);
    }

    private void Update()
    {
        // Powolny obrót dla estetyki
        transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        BallController target = FindNearest();
        if (target == null) return;

        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        var go = BallArenaUtils.CreateBulletGO(transform.position, new Color(0.3f, 0.9f, 1f), 0.16f);
        go.AddComponent<Projectile>().Initialize(owner, target, dir * BULLET_SPEED,
            damage, ProjectileType.Arrow, 0f, 2.5f);

        AttackRingFX.Spawn(transform.position, new Color(0.3f, 0.85f, 1f), 0.4f, 0.2f);
        fireTimer = FIRE_CD;
    }

    BallController FindNearest()
    {
        var all = FindObjectsByType<BallController>(FindObjectsSortMode.None);
        BallController nearest = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < minD && d <= RANGE) { minD = d; nearest = b; }
        }
        return nearest;
    }
}
