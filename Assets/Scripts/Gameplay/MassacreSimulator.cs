// MassacreSimulator.cs
// Symuluje "wielką rzeź" czysto matematycznie, bez Unity Physics.
// Używane gdy liczba kulek przekracza próg wydajnościowy.
using System.Collections.Generic;
using UnityEngine;

public static class MassacreSimulator
{
    public class SimBall
    {
        public BallClass ballClass;
        public int       mergeLevel;
        public float     statMult;
        public int       goldMult;
        public float     hp;
        public float     power;   // hp * dps — określa szansę przeżycia
    }

    /// <summary>
    /// Symuluje bitwę aż zostanie targetCount kulek.
    /// Każda "runda": losujemy 2 kulki, silniejsza wygrywa z prawdopodobieństwem
    /// proporcjonalnym do swojej "mocy". Szybkie i niedeterministyczne.
    /// </summary>
    public static List<SimBall> Simulate(List<SimBall> balls, int targetCount)
    {
        // Kopia listy — swap-with-last dla O(1) usuwania
        var alive = new List<SimBall>(balls);
        targetCount = Mathf.Max(targetCount, 1);

        while (alive.Count > targetCount)
        {
            int ia = Random.Range(0, alive.Count);
            int ib = Random.Range(0, alive.Count - 1);
            if (ib >= ia) ib++;          // gwarantuje ia != ib

            var a = alive[ia];
            var b = alive[ib];

            // Która kulka przeżywa? Losowanie ważone mocą
            float totalPower = a.power + b.power;
            bool aWins = totalPower < 0.001f
                ? Random.value < 0.5f
                : Random.value < a.power / totalPower;

            int loserIdx = aWins ? ib : ia;
            // Swap z ostatnią i usuń — O(1)
            alive[loserIdx] = alive[alive.Count - 1];
            alive.RemoveAt(alive.Count - 1);
        }

        return alive;
    }

    /// <summary>Szacuje "moc" kulki na podstawie configu i poziomu scalenia.</summary>
    public static float EstimatePower(ClassConfig cfg, float statMult)
    {
        if (cfg == null) return 1f;
        float hp  = cfg.maxHP * statMult;
        float dps = cfg.collisionDamage * statMult + cfg.maxHP * 0.1f * statMult;
        return hp * dps;
    }
}
