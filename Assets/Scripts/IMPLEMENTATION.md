# IMPLEMENTATION — Ball Arena Game

> Ostatnia aktualizacja: 2026-03-25

---

## Spis treści
1. [Architektura projektu](#1-architektura-projektu)
2. [Klasy i statystyki](#2-klasy-i-statystyki)
3. [Systemy rozgrywki](#3-systemy-rozgrywki)
4. [UI – ShopScene](#4-ui--shopscene)
5. [UI – MergeScene](#5-ui--mergescene)
6. [UI – PaintScene (Malarnia)](#6-ui--paintscene-malarnia)
7. [UI – GameScene (arena)](#7-ui--gamescene-arena)
8. [System scalania (merge)](#8-system-scalania-merge)
9. [Wzory kulek (BallPattern)](#9-wzory-kulek-ballpattern)
10. [Przyciski z zaokrąglonymi rogami](#10-przyciski-z-zaokrąglonymi-rogami)
11. [Prefaby do zbudowania](#11-prefaby-do-zbudowania)
12. [Sceny do ustawienia](#12-sceny-do-ustawienia)
13. [ClassConfig – lista assetów](#13-classconfig--lista-assetow)
14. [TODO – co jeszcze zrobić](#14-todo--co-jeszcze-zrobic)

---

## 1. Architektura projektu

```
Assets/
  Scripts/
    BallClass.cs          – enum BallClass (15 klas), BallPattern, ClassConfig
    GameData.cs           – singleton z danymi gracza (zloto, kulki, merge, customizacja)
    BallController.cs     – logika kulki (HP, ruch, AI, bron, kolizje)
    Weapons.cs            – 15 broni (kazda klasa ma swoja)
    DruidMinion.cs        – minionka druida (dynamic Rigidbody2D)
    BallArenaUtils.cs     – narzedziowe: CircleSprite, CreatePatternSprite
    ArenaGameManager.cs   – spawning, WallBlast, CenterPull, KillFeed
    KillFeed.cs           – pasek zaboistw (lewy dol ekranu)
    HealthBar.cs          – pasek HP nad kulka
    BackgroundBalls.cs    – dekoracyjne kulki w tle menu
    ShopUI.cs             – sklep (akordeon + panel szczegolow)
    ShopListRow.cs        – wiersz w akordeonowej liscie
    MergeUI.cs            – scalanie (akordeon + panel szczegolow)
    PaintShopUI.cs        – malarnia (personalizacja wygladu kulek)
    ButtonAnimator.cs     – animacje hover/press/disabled na przyciskach
    RoundedSquareSetup.cs – per-instance material dla shadera zaokraglonych rogow
  RoundedSquare/
    Shaders/roundedSqaure.shader – shader zaokraglonych rogow UI
```

---

## 2. Klasy i statystyki

| Klasa        | HP  | Spd | Rng | CD  | Specjalnosc |
|--------------|-----|-----|-----|-----|-------------|
| Warrior      | 120 | 3.5 | 2.0 | 1.8 | Szarza (charge + slam) |
| Mage         | 90  | 3.0 | 5.0 | 2.5 | Homing Fireball |
| Archer       | 85  | 4.0 | 6.0 | 1.5 | Salwa 3 strzal |
| Rogue        | 75  | 5.5 | 2.5 | 1.2 | Niesmiertelnosc + Trucizna |
| Paladin      | 130 | 2.5 | 2.0 | 3.0 | Smite + Heal sojusznika |
| Berserker    | 100 | 4.5 | 1.8 | 1.0 | Rage: im mniej HP tym mocniejszy |
| Necromancer  | 95  | 3.0 | 4.0 | 3.5 | AoE puls + lifesteal |
| Elementalist | 80  | 3.5 | 5.5 | 3.0 | Rotuje zywioly (ogien/lod/blyszk) |
| Priest       | 110 | 2.8 | 3.0 | 4.0 | Self-heal +oslabienie wroga |
| Titan        | 200 | 1.5 | 2.5 | 5.0 | Tapniecia AoE, ogromny |
| Druid        | 100 | 3.0 | 3.0 | 4.0 | Przywolywacz (max 3 minionki) |
| Technician   | 90  | 3.5 | 5.0 | 4.5 | Wiezyczki (max 3, usuwa najstarsza) |
| Glitch       | 80  | 4.0 | 3.0 | 2.0 | Chaos przy kolizji (randomizuje statsy) |
| Psychic      | 95  | 3.5 | 7.0 | 3.0 | Odpycha wszystkich gdy dostanie cios |
| Nerd         | 70  | 4.5 | 4.0 | 1.5 | Fibonacci damage (1,1,2,3,5,8...) |

---

## 3. Systemy rozgrywki

### GameData (singleton)
- Przezywa miedzy scenami (DontDestroyOnLoad)
- Trzyma: gold, arenaTierIndex, purchasedBalls, mergedBalls,
  consumedBaseBalls, ballCustomizations, paintShopUnlocked

### Arena tiers
Mala S -> Srednia M -> Duza L -> Gigant XL -> Twoja matka XXL
Kazdy tier zmienia: limit extra kulek, skale kulek, rozmiar tla.

### WallBlast / CenterPull
- WallBlast: eksplozja od scian (sila 22), cooldown 8s
- CenterPull: grawitacja do srodka (sila 18), cooldown 10s
- Przyciski pokazuja countdown: "BLAST\n5s"

### KillFeed
- Max 5 wpisow, fade po 4s
- Kolorowe kropki = kolor kulki zabojcy i ofiary
- Lewy-dolny rog ekranu

---

## 4. UI – ShopScene

### Skrypty
- ShopUI.cs – glowny skrypt sceny
- ShopListRow.cs – wiersze w akordeonach

### Uklad Canvas (ustawic w edytorze)

```
Canvas
+-- TopBar (HorizontalLayoutGroup)
|   +-- TitleRect (TMP_Text, [ExecuteAlways] bob animacja)
|   +-- GoldText (TMP_Text)
|   +-- InfoText (TMP_Text)
+-- MainArea (HorizontalLayoutGroup, fill)
|   +-- LeftPanel (1/3 width)
|   |   +-- ScrollRect -> Viewport -> Content (VerticalLayoutGroup)
|   |       +-- BallsHeaderBtn (Button "Kulki")
|   |       |   onClick -> ShopUI.ToggleBalls()
|   |       +-- BallsContent (VerticalLayoutGroup + ContentSizeFitter)
|   |       |   (wiersze SpawnRow() wypelniaja dynamicznie)
|   |       +-- UpgradesHeaderBtn (Button "Ulepszenia")
|   |       |   onClick -> ShopUI.ToggleUpgrades()
|   |       +-- UpgradesContent (VerticalLayoutGroup + ContentSizeFitter)
|   |           (wiersze SpawnRow() wypelniaja dynamicznie)
|   +-- RightPanel (2/3 width)
|       +-- EmptyHint (TMP_Text "Wybierz cos z listy")
|       +-- DetailView (domyslnie SetActive=false)
|           +-- DetailIcon (Image, 80x80, okrag)
|           +-- DetailName (TMP_Text, bold)
|           +-- DetailFlavor (TMP_Text, italic, szary)
|           +-- DetailStats (TMP_Text, monospace)
|           +-- ActionButton (Button)
|               onClick -> ShopUI.OnActionClicked()
|               +-- ActionLabel (TMP_Text)
+-- BottomBar
    +-- BackButton  -> ShopUI.OnBackClicked()
    +-- MergeButton -> ShopUI.OnMergeClicked()
    +-- PlayButton  -> ShopUI.OnStartGameClicked()
```

### Podpiecia w Inspektorze ShopUI
| Pole               | GO                                    |
|--------------------|---------------------------------------|
| goldText           | TopBar/GoldText                       |
| infoText           | TopBar/InfoText                       |
| titleRect          | TopBar/TitleRect (RectTransform)      |
| ballsContent       | .../BallsContent (Transform)          |
| ballsToggleLabel   | BallsHeaderBtn/Text (TMP_Text)        |
| upgradesContent    | .../UpgradesContent (Transform)       |
| upgradesToggleLabel| UpgradesHeaderBtn/Text (TMP_Text)     |
| listRowPrefab      | Prefab ShopListRow                    |
| emptyHint          | RightPanel/EmptyHint (GameObject)     |
| detailView         | RightPanel/DetailView (GameObject)    |
| detailIcon         | DetailView/DetailIcon (Image)         |
| detailName         | DetailView/DetailName (TMP_Text)      |
| detailFlavor       | DetailView/DetailFlavor (TMP_Text)    |
| detailStats        | DetailView/DetailStats (TMP_Text)     |
| actionButton       | DetailView/ActionButton (Button)      |
| actionLabel        | ActionButton/ActionLabel (TMP_Text)   |
| allClassConfigs    | [15 ClassConfig assetow]              |

---

## 5. UI – MergeScene

### Skrypt
- MergeUI.cs

### Uklad Canvas

```
Canvas
+-- TopBar (GoldText, InfoText, TitleRect jak w ShopScene)
+-- MainArea (HorizontalLayoutGroup)
|   +-- LeftPanel (1/3 width, ScrollRect)
|   |   +-- BasicHeaderBtn   onClick -> MergeUI.ToggleBasic()
|   |   +-- BasicContent     (VLG, dynamicznie wypelniany)
|   |   +-- UpgradeHeaderBtn onClick -> MergeUI.ToggleUpgrade()
|   |   +-- UpgradeContent   (VLG, dynamicznie wypelniany)
|   |   +-- OwnedHeaderBtn   onClick -> MergeUI.ToggleOwned()
|   |   +-- OwnedContent     (VLG, dynamicznie wypelniany)
|   +-- RightPanel (2/3 width)
|       +-- EmptyHint
|       +-- DetailView
|           +-- DetailIcon, DetailName, DetailFlavor, DetailStats
|           +-- ActionButton onClick -> MergeUI.OnActionClicked()
+-- BottomBar
    +-- BackButton  -> MergeUI.GoToMainMenu()
    +-- ShopButton  -> MergeUI.GoToShop()
    +-- PlayButton  -> MergeUI.GoToGame()
```

### Podpiecia MergeUI
Analogiczne do ShopUI, plus:
- basicContent / basicToggleLabel
- upgradeContent / upgradeToggleLabel
- ownedContent / ownedToggleLabel

---

## 6. UI – PaintScene (Malarnia)

Scena odblokowana po zakupie Malarni w ShopScene (koszt 300g).
Dodaj "PaintScene" do Build Settings.

### Skrypt
- PaintShopUI.cs

### Uklad Canvas

```
Canvas
+-- TopBar: TitleText (TMP "Malarnia")
+-- MainArea (HorizontalLayoutGroup)
|   +-- LeftPanel (1/3 width, ScrollRect)
|   |   +-- BallListContent (VLG)
|   |       (ShopListRow per posiadana kulka)
|   +-- RightPanel (2/3 width)
|       +-- EmptyHint
|       +-- EditorView (domyslnie SetActive=false)
|           +-- PreviewImage (Image, 120x120)
|           +-- BallNameLabel (TMP)
|           +-- PatternDropdown (TMP_Dropdown)
|           +-- StripeRow (widoczny tylko dla Stripes)
|           |   +-- StripeLabel (TMP "Praski: 5")
|           |   +-- StripeSlider (Slider, min=2, max=12)
|           +-- Color1Section
|           |   +-- C1Label (TMP "Kolor 1"), C1Preview (Image 30x30)
|           |   +-- C1R, C1G, C1B (Slider 0-1)
|           +-- Color2Section (analogicznie)
|           +-- Color3Section (analogicznie)
|           +-- ResetButton  -> PaintShopUI.OnResetClicked()
|           +-- SaveButton   -> PaintShopUI.OnSaveClicked()
+-- BottomBar
    +-- BackButton -> PaintShopUI.GoToShop()
    +-- MenuButton -> PaintShopUI.GoToMainMenu()
```

### Podpiecia PaintShopUI
| Pole              | GO                              |
|-------------------|---------------------------------|
| ballListContent   | LeftPanel/.../BallListContent   |
| listRowPrefab     | Prefab ShopListRow              |
| emptyHint         | RightPanel/EmptyHint            |
| editorView        | RightPanel/EditorView           |
| previewImage      | EditorView/PreviewImage         |
| ballNameLabel     | EditorView/BallNameLabel        |
| patternDropdown   | EditorView/PatternDropdown      |
| stripeRow         | EditorView/StripeRow (GO)       |
| stripeSlider      | StripeRow/StripeSlider          |
| stripeLabel       | StripeRow/StripeLabel           |
| c1Preview/c1R/G/B | EditorView/Color1Section/...    |
| c2Preview/c2R/G/B | EditorView/Color2Section/...    |
| c3Preview/c3R/G/B | EditorView/Color3Section/...    |
| saveButton        | EditorView/SaveButton           |
| resetButton       | EditorView/ResetButton          |
| allClassConfigs   | [15 ClassConfig assetow]        |

---

## 7. UI – GameScene (arena)

### Skrypt: ArenaGameManager.cs

### Struktura sceny

```
GameScene
+-- GameData GO (jesli nie DontDestroyOnLoad)
+-- ArenaGameManager GO
+-- Canvas (UI)
|   +-- WallBlastButton  -> ArenaGameManager.TriggerWallBlast()
|   +-- CenterPullButton -> ArenaGameManager.TriggerCenterPull()
|   +-- GoldText (TMP)
|   +-- KillFeed GO (KillFeed.cs buduje sie samodzielnie)
+-- HealthBar Canvas (prefab nad kazda kulka)
```

### Podpiecia ArenaGameManager
- classConfigs – lista 15 ClassConfig assetow
- ballPrefab – prefab z BallController
- healthBarPrefab – prefab z HealthBar
- wallBlastButton, centerPullButton
- goldText

---

## 8. System scalania (merge)

### Zasady
1. Scal bazowe – potrzebujesz 5 kulek tej samej klasy:
   5 bazowych LUB (1 bazowa startowa + 4 kupione) -> 1 kulka Poz.1 (x5 statystyk)
   Bazowa kulka jest "zjedzona" i znika z areny.
2. Ulepsz scalone – 5 kulek Poz.N -> 1 kulka Poz.(N+1)  (staty: 5^N)
3. Mozna miec wiele scalonych tej samej klasy (np. 3x Warrior Poz.1)

### API (GameData)
```csharp
CountBasicBalls(BallClass cls)
CountMergedOfLevel(BallClass cls, int n)
CanMergeBasic(BallClass cls)
CanMergeUp(BallClass cls, int fromLevel)
TryMergeBasic(BallClass cls)
TryMergeUp(BallClass cls, int fromLevel)
GetCustomization(BallClass cls)
SaveCustomization(BallClass cls, Color c1, c2, c3, BallPattern, int stripes)
```

---

## 9. Wzory kulek (BallPattern)

Generowane proceduralnie w BallArenaUtils.CreatePatternSprite(c1, c2, c3, pattern, stripes).

| Enum              | Opis                          | Uzywa     |
|-------------------|-------------------------------|-----------|
| Solid             | Jednolity kolor               | c1        |
| HorizontalStripes | Poziome praski                | c1, c2    |
| DiagonalStripes   | Skosne praski (45 stopni)     | c1, c2    |
| Pepsi             | Gorna-lewa c1, dolna-prawa c2 | c1, c2    |
| Quarters          | 4 cwiartki naprzemiennie      | c1, c2    |
| Wedge             | Plasterki pizzy (6 kawalkow)  | c1, c2, c3|
| Dots              | Kropki c2 na tle c1           | c1, c2    |
| Ring              | Pierscien c2, rdzen c3, tlo c1| c1, c2, c3|

Customizacja gracza przechowywana w GameData.ballCustomizations,
aplikowana w BallController.Initialize() przed wyrenderowaniem sprite'a.

---

## 10. Przyciski z zaokraglonymi rogami

### Shader: Custom/roundedSqaure
Parametry: _Radius (0-0.5), _scale (Vector2 w px), toggle per-rog
WAZNE: _scale MUSI byc ustawiony na rozmiar RectTransform w pikselach.

### Skrypt: RoundedSquareSetup.cs
Podepnij na kazdy Image uzywajacy shadera.
- Tworzy osobna instancje materialu
- Aktualizuje _scale co klatke
- [ExecuteAlways] – dziala w edytorze

### Skrypt: ButtonAnimator.cs
Podepnij na root GO przycisku.
- fillImage    – child Image (wypelnienie)
- borderImage  – Image na ROOT GO (null = GetComponent)
- Kolory: fillNormal/Hover/Press/Disabled, borderNormal/Hover/Press/Disabled
- Skala: hoverScale=1.08, pressScale=0.93, normalScale=1.0
- idlePulse – opcjonalny pulse (np. przycisk GRAJ)

### Struktura GO przycisku
```
ButtonRoot  <- Button + ButtonAnimator + Image(border) + RoundedSquareSetup
+-- Fill    <- Image(fill) + RoundedSquareSetup
    +-- Label (TMP_Text)
```
- Button.targetGraphic       -> Fill Image
- ButtonAnimator.fillImage   -> Fill Image
- ButtonAnimator.borderImage -> null (wezmie root Image przez GetComponent)

---

## 11. Prefaby do zbudowania

### ShopListRow
```
Root: Button + HorizontalLayoutGroup(spacing=8, padding=4x8)
    + LayoutElement(preferredHeight=48) + ShopListRow skrypt
+-- Dot:   Image(36x36, sprite=okrag)
+-- Label: TMP_Text(flexible, left-align, font-size=16)
+-- Badge: TMP_Text(fixed-width=60, right-align, gray, font-size=14)
```

### BallPrefab
```
Root: Rigidbody2D + CircleCollider2D + SpriteRenderer + BallController
+-- HealthBarCanvas (WorldSpace Canvas)
    +-- HealthBar GO: HealthBar skrypt
```

### HealthBar
```
Root: HealthBar skrypt + VerticalLayoutGroup
+-- BallName: TMP_Text
+-- BarBG: Image(gray)
    +-- BarFill: Image(green, pivot=left)
```

---

## 12. Sceny do ustawienia

Dodaj do File -> Build Settings (kolejnosc ma znaczenie):
1. MainMenu
2. ShopScene
3. MergeScene
4. GameScene
5. PaintScene  <- NOWA

### MainMenu
- Przyciski: GRAJ -> ShopScene, MERGE -> MergeScene, MALARNIA -> PaintScene
- BackgroundBalls GO z BackgroundBalls skryptem (dekoracja)

### ShopScene
- ShopUI GO z ShopUI skryptem
- Ustaw wszystkie pola wg sekcji 4

### MergeScene
- MergeUI GO z MergeUI skryptem
- Ustaw wszystkie pola wg sekcji 5

### PaintScene
- PaintShopUI GO z PaintShopUI skryptem
- Ustaw wszystkie pola wg sekcji 6

### GameScene
- ArenaGameManager GO
- KillFeed GO (KillFeed buduje UI sam)
- UI Canvas z przyciskami Blast/Pull

---

## 13. ClassConfig – lista assetow

Stworz w Assets/Resources/ClassConfigs/ (lub przypisz recznie w Inspektorze).
Nazwa pliku musi pasowac do nazwy klasy (np. Warrior.asset) jezeli uzywasz Resources.Load.

| Plik              | ballClass    | Sugerowany kolor | Sugerowany wzor |
|-------------------|--------------|------------------|-----------------|
| Warrior.asset     | Warrior      | ciemny czerwony  | Solid           |
| Mage.asset        | Mage         | fioletowy        | Ring            |
| Archer.asset      | Archer       | zielony          | DiagonalStripes |
| Rogue.asset       | Rogue        | czarno-zielony   | Pepsi           |
| Paladin.asset     | Paladin      | zloty            | Quarters        |
| Berserker.asset   | Berserker    | krwisty czerwony | Solid           |
| Necromancer.asset | Necromancer  | ciemny fiolet    | Dots            |
| Elementalist.asset| Elementalist | pomaranczowy     | Wedge           |
| Priest.asset      | Priest       | bialy/zloty      | Ring            |
| Titan.asset       | Titan        | szary            | Solid           |
| Druid.asset       | Druid        | lesna zielen     | HorizontalStripes|
| Technician.asset  | Technician   | stalowy niebieski| DiagonalStripes |
| Glitch.asset      | Glitch       | ciemny + magenta | Pepsi           |
| Psychic.asset     | Psychic      | jasny fiolet     | Dots            |
| Nerd.asset        | Nerd         | turkusowy        | Quarters        |

Wymagane pola kazdego assetu:
- ballClass, className, color, color2, color3, pattern, stripeCount
- maxHP, radius, moveSpeed, attackRange, attackCooldown, collisionDamage

---

## 14. TODO – co jeszcze zrobic

### Obowiazkowe (bez tego gra nie dziala)
- [x] Zbudowac scene ShopScene wg sekcji 4 (Canvas + podpiecia w Inspektorze)
- [x] Zbudowac scene MergeScene wg sekcji 5
- [x] Zbudowac scene PaintScene wg sekcji 6
- [x] Stworzyc prefab ShopListRow wg sekcji 11
- [x] Stworzyc 15x ClassConfig assets wg sekcji 13
- [x] Dodac wszystkie sceny do Build Settings (+ PaintScene)

### Wazne dla wrazenia z gry
- [x] Ustawic shader Custom/roundedSqaure na przyciskach
- [x] Podpiac RoundedSquareSetup na przyciskach
- [x] Ustawic ButtonAnimator na przyciskach sklepu/merge/gry
- [x] Dobrac kolory i wzory w ClassConfig

### Opcjonalne (nice to have)
- [ ] Przycisk "Malarnia" w MainMenu aktywny gdy paintShopUnlocked
- [ ] Efekty dzwiekowe (AudioSource na ArenaGameManager)
- [ ] Zapis stanu gry (PlayerPrefs / JSON) – GameData nie persistuje po zamknieciu Unity
- [ ] AttackRingFX – wizualne pierscienie atakow
