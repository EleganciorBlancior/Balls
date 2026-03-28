# IMPLEMENTATION.md – Ball Arena Setup Guide

## Sceny (Scenes)
Potrzebujesz 4 scen w Build Settings (kolejność ma znaczenie):
```
0. MainMenu
1. ShopScene
2. MergeScene
3. GameScene
```

---

## Kroki ustawienia

### 1. Generowanie ClassConfig (assetów klas)
1. Otwórz Unity Editor
2. Menu: **BallArena → Create All ClassConfigs (15)**
3. Pliki zostaną wygenerowane w `Assets/Resources/ClassConfigs/`

> Wszystkie 15 klas: Warrior, Mage, Archer, Rogue, Paladin, Berserker, Necromancer,
> Elementalist, Priest, Titan, Druid, Technician, Glitch, Psychic, Nerd.
> Klasy Druid/Technician/Glitch/Psychic/Nerd mają `useMultiColor = true` (orbitujące kropki).

---

### 2. Scena MainMenu
1. Utwórz pustą scenę `MainMenu`
2. Dodaj pusty GameObject → `MainMenuUI` (skrypt `MainMenuUI.cs`)
3. Dodaj pusty GameObject → `BackgroundBalls` (skrypt `BackgroundBalls.cs`)
   - `mode = Bouncing`
4. Ustaw Camera Background Color: `#0D1117`

---

### 3. Scena ShopScene
1. Utwórz pustą scenę `ShopScene`
2. Dodaj pusty GameObject → `ShopUI` (skrypt `ShopUI.cs`)
   - `allClassConfigs`: przeciągnij 15 assetów z `Resources/ClassConfigs/`
3. Dodaj pusty GameObject → `BackgroundBalls` (skrypt `BackgroundBalls.cs`)
   - `mode = Diagonal`
4. Dodaj pusty GameObject → `GameData` (skrypt `GameData.cs`)

---

### 4. Scena MergeScene
1. Utwórz pustą scenę `MergeScene`
2. Dodaj pusty GameObject → `MergeUI` (skrypt `MergeUI.cs`)
   - `allClassConfigs`: przeciągnij 15 assetów z `Resources/ClassConfigs/`
3. Dodaj pusty GameObject → `BackgroundBalls` (skrypt `BackgroundBalls.cs`)
   - `mode = Orbital`

---

### 5. Scena GameScene
1. Utwórz pustą scenę `GameScene`
2. Dodaj pusty GameObject → `ArenaGameManager` (skrypt `ArenaGameManager.cs`)
   - `classConfigs`: przeciągnij 15 assetów z `Resources/ClassConfigs/`
   - `arenaSize`: np. `7`
3. Dodaj pusty GameObject → `KillFeed` (skrypt `KillFeed.cs`)
   - `maxEntries = 6`, `entryLifetime = 5`
4. Skrypt tworzy arenę (ściany) i UI proceduralnie – nie musisz nic konfigurować w Canvas

#### Warstwy fizyki (Physics 2D Layer Matrix)
Upewnij się, że warstwy `Ball` i `Projectile` są skonfigurowane:
- Layer `Ball` (np. layer 8)
- Layer `Projectile` (np. layer 9)
- W Physics 2D Matrix: Ball-Ball = ON, Ball-Projectile = ON, Projectile-Projectile = OFF

---

## Nowe klasy – opis zachowania

| Klasa | Broń | Specjalność |
|---|---|---|
| Druid | DruidWeapon | Co 5s spawnuje minionka (DruidMinion) z żółtą orbitującą kropką, który leci do wroga (kinematyczny – nie bounces) |
| Technician | TechWeapon | Co 5s spawnuje statyczną wieżyczkę (TechTurret), która strzela do najbliższego wroga |
| Glitch | GlitchWeapon | Przy każdej kolizji losuje nowy damage/range/cooldown – chaos stats |
| Psychic | PsychicWeapon | Gdy otrzyma obrażenia, odpycha wszystkich w promieniu 7 – odrzucone kulki dostają dmg przy zetknięciu ze ścianą |
| Nerd | NerdWeapon | Damage rośnie ciągiem Fibonacciego: 1→1→2→3→5→8→13→21→34→55→89→144→233→377→610 |

---

## Multicolor kulki
Klasy z `useMultiColor = true` mają:
- Główny sprite: gradient radialny `color3 → color`
- Dwie orbitujące kropki: `color2` (orbit zewnętrzny, 130°/s) i `color3` (orbit wewnętrzny, -85°/s)

---

## DruidMinion – dlaczego kinematyczny?
Minionek ma `RigidbodyType2D.Kinematic` i porusza się przez `MovePosition()`.
Dzięki temu:
- Nie odbija się od ścian
- Nie wchodzi w interakcje fizyczne z kulkami (tylko trigger)
- Wizualnie odróżnia się żółtą orbitującą kropką od zwykłych kulek

---

## UI – styl dark navy

Skrypty generują UI proceduralnie (brak potrzeby setupu Canvas w edytorze):
- `UIStyle.cs` – paleta kolorów i factory methods
- `MainMenuUI.cs` – bobbing tytuł + przyciski
- `ShopUI.cs` + `ShopItem.cs` – karty kulek z ceną, arena upgrade
- `MergeUI.cs` – progress dots (●●○○○), scalone kulki z badge "S"
- `KillFeed.cs` – pill-karty (lewy górny róg): [dot KillerName zabił dot VictimName]
- `ArenaGameManager.cs` – gold pill (prawy górny róg), przyciski PULL/BLAST, mini-legenda (prawy dolny), ekran end-game

---

## Pliki skryptów

```
Assets/Scripts/
├── Editor/
│   └── ClassConfigSetup.cs      <- generator assetów (tylko Editor)
├── BallArenaUtils.cs             <- sprite factory + bullet factory
├── BallClass.cs                  <- enum + ClassConfig ScriptableObject
├── BallController.cs             <- główna logika kulki + orbiting dots
├── BackgroundBalls.cs            <- animowane tło (3 tryby)
├── DruidMinion.cs                <- kinematyczny minionek Druida
├── TechTurret.cs                 <- statyczna wieżyczka Technika
├── AttackRingFX.cs               <- rozszerzający się pierścień efektu
├── ArenaGameManager.cs           <- zarządzanie rundą + UI areny
├── GameData.cs                   <- stan gry (złoto, kulki, merge)
├── KillFeed.cs                   <- kill feed (lewy górny róg)
├── MainMenuUI.cs                 <- UI główne menu
├── MergeUI.cs                    <- UI scalania
├── ShopUI.cs                     <- UI sklepu
├── ShopItem.cs                   <- statyczne helpery kart UI
├── UIStyle.cs                    <- paleta + UI factory
├── Weapons.cs                    <- wszystkie 15 broni
├── HitParticles.cs               <- iskry przy trafieniu
├── BallDeathParticles.cs         <- eksplozja przy śmierci
└── HealthBar.cs                  <- pasek HP nad kulką
```
