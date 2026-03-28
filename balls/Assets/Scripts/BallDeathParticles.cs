// BallDeathParticles.cs
// Tworzy efekt eksplozji cząsteczkowej w miejscu śmierci kulki
using UnityEngine;

public class BallDeathParticles : MonoBehaviour
{
    // Wywołaj: BallDeathParticles.Spawn(position, color)
    public static void Spawn(Vector3 position, Color color)
    {
        var go = new GameObject("DeathFX");
        go.transform.position = position;
        var fx = go.AddComponent<BallDeathParticles>();
        fx.color = color;
        fx.Play();
        Destroy(go, 2f);
    }

    private Color color;
    private ParticleSystem ps;

    void Play()
    {
        ps = gameObject.AddComponent<ParticleSystem>();

        // Zatrzymaj auto-play żeby najpierw skonfigurować
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ── Main ────────────────────────────────────────────────────────────
        var main              = ps.main;
        main.duration         = 0.3f;
        main.loop             = false;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(2f, 8f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startColor       = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.gravityModifier  = 0.3f;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;

        // ── Emission ────────────────────────────────────────────────────────
        var emission          = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        // ── Shape: kula ─────────────────────────────────────────────────────
        var shape             = ps.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Circle;
        shape.radius          = 0.3f;

        // ── Color over lifetime: zanika ──────────────────────────────────────
        var col               = ps.colorOverLifetime;
        col.enabled           = true;
        var grad              = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),    new GradientAlphaKey(0f, 1f) });
        col.color             = new ParticleSystem.MinMaxGradient(grad);

        // ── Size over lifetime: maleje ───────────────────────────────────────
        var size              = ps.sizeOverLifetime;
        size.enabled          = true;
        var sizeCurve         = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        size.size             = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Renderer: sprite kółka ───────────────────────────────────────────
        var renderer          = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material     = CreateParticleMaterial();
        renderer.sortingOrder = 10;

        ps.Play();
    }

    Material CreateParticleMaterial()
    {
        // Używa wbudowanego shadera Sprites/Default
        var mat   = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        return mat;
    }
}
