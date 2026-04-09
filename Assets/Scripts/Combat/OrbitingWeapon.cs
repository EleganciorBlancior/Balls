using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Broń orbitująca wokół melee kulki z mastery.
/// Kręci się, zadaje osobne obrażenia, odbija kulkę przy trafieniu
/// i zmienia kierunek kręcenia.
/// </summary>
public class OrbitingWeapon : MonoBehaviour
{
    const float BASE_ORBIT_SPEED  = 220f;  // stopni/s
    const float BASE_DAMAGE       = 15f;
    const float HIT_COOLDOWN      = 0.5f;
    const float KNOCKBACK_FORCE   = 6f;

    private BallController _owner;
    private GameObject     _weaponGO;
    private float          _orbitAngle;
    private float          _orbitDirection = 1f; // 1 = CW, -1 = CCW
    private float          _orbitRadius;
    private float          _damage;
    private float          _scaleMult;

    private Dictionary<int, float> _hitTimers = new Dictionary<int, float>();

    public void Setup(BallController owner, Sprite weaponSprite, float scaleMult, float statMult)
    {
        _owner     = owner;
        _scaleMult = scaleMult;
        _damage    = BASE_DAMAGE * statMult;
        _orbitRadius = owner.Config.radius * scaleMult * 1.3f;
        _orbitAngle  = Random.Range(0f, 360f);

        _weaponGO = new GameObject("OrbitWeapon");
        _weaponGO.transform.SetParent(transform);
        _weaponGO.transform.localScale = Vector3.one * 0.8f;

        var sr = _weaponGO.AddComponent<SpriteRenderer>();
        sr.sprite       = weaponSprite;
        sr.sortingOrder = 4;

        var col = _weaponGO.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.4f;

        _weaponGO.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        _weaponGO.AddComponent<OrbitHitbox>().Init(this);
    }

    private void Update()
    {
        if (_owner == null || !_owner.IsAlive || _weaponGO == null) return;

        _orbitAngle += BASE_ORBIT_SPEED * _orbitDirection * Time.deltaTime;
        float rad = _orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _orbitRadius;
        _weaponGO.transform.position = transform.position + offset;

        float spriteAngle = _orbitAngle - 45f;
        _weaponGO.transform.rotation = Quaternion.Euler(0f, 0f, spriteAngle);

        // Cleanup hit cooldown timers
        var expired = new List<int>();
        foreach (var kv in _hitTimers)
        {
            _hitTimers[kv.Key] = kv.Value - Time.deltaTime;
            if (kv.Value - Time.deltaTime <= 0f) expired.Add(kv.Key);
        }
        foreach (var id in expired) _hitTimers.Remove(id);
    }

    public void OnOrbitHit(Collider2D other)
    {
        var ball = other.GetComponent<BallController>();
        if (ball == null || ball == _owner || !ball.IsAlive) return;

        int id = ball.GetInstanceID();
        if (_hitTimers.ContainsKey(id) && _hitTimers[id] > 0f) return;
        _hitTimers[id] = HIT_COOLDOWN;

        ball.TakePhysicalDamage(_damage, _owner);

        Vector2 knockDir = ((Vector2)(ball.transform.position - transform.position)).normalized;
        ball.ApplyKnockback(knockDir, KNOCKBACK_FORCE);

        _orbitDirection *= -1f;

        AudioController.Instance?.PlayMeleeHit();
    }

    private void OnDestroy()
    {
        if (_weaponGO != null) Destroy(_weaponGO);
    }
}

public class OrbitHitbox : MonoBehaviour
{
    private OrbitingWeapon _weapon;

    public void Init(OrbitingWeapon weapon) => _weapon = weapon;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_weapon != null) _weapon.OnOrbitHit(other);
    }
}
