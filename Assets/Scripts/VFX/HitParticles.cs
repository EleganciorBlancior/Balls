// HitParticles.cs
// Małe iskry przy trafieniu kulki
using UnityEngine;

public class HitParticles : MonoBehaviour
{
    public static void Spawn(Vector3 position, Color color, int count = 12)
    {
        if (count <= 0) return;
        var go = new GameObject("HitFX");
        go.transform.position = position;
        var fx = go.AddComponent<HitParticles>();
        fx.color = color;
        fx._count = count;
        fx.Play();
        Destroy(go, 0.5f);
    }

    private Color color;
    private int   _count = 12;

    void Play()
    {
        float s  = Mathf.Clamp(BallController.VfxScale, 0.15f, 2f);
        var ps   = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.duration        = 0.1f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f * s, 8f * s);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.06f * s, 0.2f * s);
        main.startColor      = new ParticleSystem.MinMaxGradient(color, Color.yellow);
        main.gravityModifier = 0.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission         = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, _count) });

        var shape            = ps.shape;
        shape.shapeType      = ParticleSystemShapeType.Circle;
        shape.radius         = 0.2f * s;

        var col              = ps.colorOverLifetime;
        col.enabled          = true;
        var grad             = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),    new GradientAlphaKey(0f, 1f) });
        col.color            = new ParticleSystem.MinMaxGradient(grad);

        var renderer         = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material    = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 10;

        ps.Play();
    }
}
