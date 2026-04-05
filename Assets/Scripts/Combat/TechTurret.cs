// TechTurret.cs – statyczna wieżyczka Technika
using UnityEngine;

public class TechTurret : MonoBehaviour
{
    private BallController owner;
    private float          damage;
    private float          arenaScale   = 1f;
    private float          fireTimer    = 0.5f;
    private const float    FIRE_CD      = 1.4f;
    private const float    BULLET_SPEED = 9f;
    private const float    LIFETIME     = 14f;
    private const float    RANGE        = 10f;

    // Tryb mega (ULT Technika)
    private bool  _isMega    = false;
    private const float MEGA_FIRE_CD  = 2.2f;
    private const float ROCKET_SPEED  = 7f;
    private const float MEGA_LIFETIME = 18f;
    private const float MEGA_RANGE    = 14f;

    private float rotSpeed;

    public void Initialize(BallController ownerBall, float dmg, bool isMega = false, float scale = 1f)
    {
        owner      = ownerBall;
        damage     = dmg;
        _isMega    = isMega;
        arenaScale = scale;
        rotSpeed = Random.Range(60f, 150f) * (Random.value > 0.5f ? 1f : -1f);

        if (_isMega)
        {
            transform.localScale *= 1.8f;
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 0.1f, 1f);
            fireTimer = 0.8f;
            Destroy(gameObject, MEGA_LIFETIME);
        }
        else
        {
            Destroy(gameObject, LIFETIME);
        }
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        float range = _isMega ? MEGA_RANGE : RANGE;
        BallController target = FindNearest(range);
        if (target == null) return;

        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;

        if (_isMega)
        {
            // Wielka rakieta: duży pocisk, duże obrażenia, AoE przy trafieniu
            float rocketDmg = damage * 2.5f;
            var go = BallArenaUtils.CreateBulletGO(transform.position, new Color(1f, 0.5f, 0.1f), 0.35f * arenaScale);
            var proj = go.AddComponent<Projectile>();
            proj.Initialize(owner, target, dir * ROCKET_SPEED, rocketDmg, ProjectileType.Arrow, 1.5f, 3f);
            proj.OnHitBallCallback = (hitBall) =>
            {
                // Eksplozja AoE przy trafieniu
                var all = ArenaGameManager.AliveBalls;
                foreach (var b in all)
                {
                    if (b == owner || !b.IsAlive) continue;
                    float d = Vector2.Distance(hitBall.transform.position, b.transform.position);
                    if (d < 3f)
                        b.TakeDamage(rocketDmg * 0.5f * (1f - d / 3f), owner);
                }
                AttackRingFX.SpawnWave(hitBall.transform.position, new Color(1f, 0.5f, 0.1f), 3f, 0.35f);
                ArenaEvents.FireAoE(hitBall.transform.position, new Color(1f, 0.4f, 0.1f), 3f);
            };
            AttackRingFX.Spawn(transform.position, new Color(1f, 0.5f, 0.1f), 0.6f, 0.25f);
            fireTimer = MEGA_FIRE_CD * (BallController.AiSkip > 1 ? 1f + BallController.AiSkip * 0.5f : 1f);
        }
        else
        {
            var go = BallArenaUtils.CreateBulletGO(transform.position, new Color(0.3f, 0.9f, 1f), 0.16f * arenaScale);
            go.AddComponent<Projectile>().Initialize(owner, target, dir * BULLET_SPEED,
                damage, ProjectileType.Arrow, 0f, 2.5f);
            AttackRingFX.Spawn(transform.position, new Color(0.3f, 0.85f, 1f), 0.4f, 0.2f);
            fireTimer = FIRE_CD * (BallController.AiSkip > 1 ? 1f + BallController.AiSkip * 0.5f : 1f);
        }
    }

    BallController FindNearest(float range)
    {
        var all = ArenaGameManager.AliveBalls;
        BallController nearest = null; float minD = float.MaxValue;
        foreach (var b in all)
        {
            if (b == owner || !b.IsAlive) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < minD && d <= range) { minD = d; nearest = b; }
        }
        return nearest;
    }
}
