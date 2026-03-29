// ArenaEvents.cs
// Statyczne zdarzenia areny – broń i menedżer emitują eventy,
// tło i inne systemy dekoracyjne je odbierają bez bezpośrednich zależności.
using UnityEngine;

public static class ArenaEvents
{
    /// Wystrzelono atak AoE: pozycja, kolor klasy, promień obszaru
    public static event System.Action<Vector3, Color, float> OnAoEFired;

    /// Kulka zginęła: pozycja, kolor bazowy
    public static event System.Action<Vector3, Color> OnBallDied;

    /// Mechanika: Blast od ścian
    public static event System.Action OnWallBlast;

    /// Mechanika: Ściąganie do środka
    public static event System.Action OnCenterPull;

    /// Koniec walki (przed zamrożeniem czasu)
    public static event System.Action OnGameEnd;

    public static void FireAoE(Vector3 pos, Color col, float radius)
        => OnAoEFired?.Invoke(pos, col, radius);

    public static void FireBallDied(Vector3 pos, Color col)
        => OnBallDied?.Invoke(pos, col);

    public static void FireWallBlast()
        => OnWallBlast?.Invoke();

    public static void FireCenterPull()
        => OnCenterPull?.Invoke();

    public static void FireGameEnd()
        => OnGameEnd?.Invoke();
}
