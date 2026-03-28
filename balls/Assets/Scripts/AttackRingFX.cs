// AttackRingFX.cs
// Rozszerzający się impuls (shockwave) jako oznaczenie ataku/zasięgu AoE
using System.Collections;
using UnityEngine;

public class AttackRingFX : MonoBehaviour
{
    /// <summary>Spawnuje rozszerzający się impuls pierścienia.</summary>
    /// <param name="pos">Pozycja środka</param>
    /// <param name="col">Kolor</param>
    /// <param name="maxRadius">Maksymalny promień</param>
    /// <param name="duration">Czas trwania (s)</param>
    public static void Spawn(Vector3 pos, Color col, float maxRadius, float duration = 0.4f)
    {
        var go = new GameObject("RingFX");
        go.transform.position = pos;
        var fx = go.AddComponent<AttackRingFX>();
        fx.col       = col;
        fx.maxRadius = maxRadius;
        fx.duration  = duration;
        fx.Play();
        Destroy(go, duration + 0.1f);
    }

    private Color col;
    private float maxRadius;
    private float duration;

    void Play() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        // Cząsteczki rozchodzące się po pierścieniu
        var ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        int   count     = Mathf.Clamp(Mathf.RoundToInt(maxRadius * 18f), 12, 60);
        float startSize = Mathf.Clamp(maxRadius * 0.12f, 0.08f, 0.25f);

        var main             = ps.main;
        main.duration        = duration * 0.15f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(duration * 0.85f, duration);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(maxRadius / duration * 0.9f,
                                                               maxRadius / duration * 1.1f);
        main.startSize       = new ParticleSystem.MinMaxCurve(startSize * 0.7f, startSize);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(col.r, col.g, col.b, 0.55f),
            new Color(col.r, col.g, col.b, 0.35f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission             = ps.emission;
        emission.rateOverTime    = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape            = ps.shape;
        shape.enabled        = true;
        shape.shapeType      = ParticleSystemShapeType.Circle;
        shape.radius         = 0.05f;   // mały – cząsteczki lecą od środka na zewnątrz
        shape.radiusThickness = 1f;

        var colLife          = ps.colorOverLifetime;
        colLife.enabled      = true;
        var grad             = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(col, 0f), new GradientColorKey(col, 1f) },
            new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
        colLife.color        = new ParticleSystem.MinMaxGradient(grad);

        var sizeLife         = ps.sizeOverLifetime;
        sizeLife.enabled     = true;
        var curve            = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f));
        sizeLife.size        = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer         = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material    = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 5;

        ps.Play();
        yield return null;
    }
}
