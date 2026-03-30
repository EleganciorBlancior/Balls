# IMPLEMENTATION.md – Balls v2 changelog + instrukcja implementacji

## Co się zmieniło (sprint 2026-03-30)

### 1. Lokalizacja PL/EN (`LocalizationManager.cs`)
Nowy plik `Assets/Scripts/Gameplay/LocalizationManager.cs`.
Statyczna klasa bez komponentu Unity – wszystkie teksty UI pobierane przez nią.
- `GameLanguage` enum: `PL` / `EN`
- `LocalizationManager.SetLanguage(lang)` – zmiana języka
- Flavor texty kulek przetłumaczone na angielski
- Wszystkie UI stringi zduplikowane dla obu języków

### 2. Panel ustawień (`SettingsPanel.cs`)
Nowy plik `Assets/Scripts/UI/SettingsPanel.cs`.
Otwierany przez `SettingsPanel.Open(optionalCallback)` z dowolnej sceny.
Prefab musi być w `Assets/Resources/SettingsPanel.prefab`.

**Zawartość ustawień:**
- Suwak głośności SFX (0–1)
- Suwak głośności muzyki (0–1)
- Dropdown języka (Polski / English)
- Dropdown jakości grafiki (Niska/Średnia/Wysoka → Unity QualitySettings)
- Dropdown rozdzielczości (lista z `Screen.resolutions`, stosuje `Screen.SetResolution`)
- Przycisk Wróć

**Dostęp:**
- Main Menu → przycisk "Ustawienia"
- Arena → przycisk "Ustawienia" (lub można otworzyć z End Panelu)
- Każda scena sklepu/scalnia → metoda `OpenSettings()`

### 3. Normalizacja dźwięków (`AudioController.cs`)
- Dodane pole `accelerationWarning` (klip wskaźnika przyspieszenia)
- Dodane pola `normXxx` (Range 0–2) dla każdego klipu – mnożniki normalizacji ustawiane w Inspectorze
- `SetSFXVolume(float v)` i `SetMusicVolume(float v)` – wywoływane przez `SettingsPanel`
- Głośności zapisywane i wczytywane z `GameData`

### 4. Ustawienia w `GameData.cs`
Nowe pola:
```csharp
float sfxVolume = 1f
float musicVolume = 0.4f
int qualityLevel = 1
GameLanguage language = GameLanguage.PL
int pullUpgradeLevel = 0
```
Nowe metody statyczne:
```csharp
GetPullUpgradeCost(int currentLevel) → int  // 100, 200, 400, 800
GetPullForce(int level) → float             // 18 + level*8
GetPullDuration(int level) → float          // level*0.6s (ciągłe przyciąganie)
```
Stała `PULL_MAX_LEVEL = 4`.

### 5. Przyspieszenie kulek na arenie (`ArenaGameManager.cs`)
Po `accelerationStartTime` (domyślnie 60s) kulki zaczynają przyspieszać:
- Co `accelerationInterval` (domyślnie 5s) każda kulka dostaje `+accelerationPerStep` (domyślnie +12%) do `SpeedMultiplier`
- Przy każdym kroku odgrywany jest dźwięk `AudioController.accelerationWarning`
- Pola konfiguracyjne widoczne w Inspectorze

### 6. ESC → End Panel (bez zwycięzcy)
Na arenie naciśnięcie **ESC** podczas trwającej gry pokazuje `restartPanel` bez wyświetlania `winnerText`. Gracze widzą panel z opcjami (restart/sklep/menu) ale gra technicznie dalej trwa.

### 7. Etykieta scalonej kulki – tylko żółta liczba
Scalone kulki teraz pokazują tylko mnożnik statystyk (np. "25" dla Poz.2) w żółtym kolorze. Usunięto numer kulki i gwiazdkę z prefiksu.

### 8. Ulepszenie Pulla w sklepie (`ShopUI.cs`)
W sekcji "Ulepszenia" dodano pozycję **Ulepszenie Pulla** (4 poziomy):

| Poziom | Koszt | Siła | Czas trwania |
|--------|-------|------|--------------|
| 1 | 100g | 26 | 0.6s |
| 2 | 200g | 34 | 1.2s |
| 3 | 400g | 42 | 1.8s |
| 4 | 800g | 50 | 2.4s |

Po impulsie Pull uruchamia się ciągłe przyciąganie przez `duration` sekund (coroutine).

### 9. Kolejność w Scalnia = kolejność w Sklepie (`MergeUI.cs`)
Listy "Scal bazowe" i "Ulepsz scalone" iterują teraz przez `allClassConfigs` w tej samej kolejności co `ShopUI.allClassConfigs`. Dla każdej klasy poziomy posortowane rosnąco.

### 10. Lokalizacja we wszystkich UI
`MainMenuUI`, `ShopUI`, `MergeUI`, `ArenaGameManager` – wszystkie stringi przechodzą przez `LocalizationManager`.

---

## Instrukcja implementacji w Unity Editor

### KROK 1 – AudioController prefab
1. Otwórz prefab `AudioController` (DontDestroyOnLoad)
2. Przypisz nowy klip `Acceleration Warning` do pola `accelerationWarning`
3. Dostosuj pola `normXxx` aby wyrównać głośności klipów (np. ballCollision = 0.7)

### KROK 2 – GameData
Brak zmian w Editorze – nowe pola są `[HideInInspector]` i mają wartości domyślne.

### KROK 3 – Tworzenie prefabu SettingsPanel

Utwórz: `Assets/Resources/SettingsPanel.prefab`

```
GameObject: SettingsPanel
  Component: Canvas (Screen Space – Overlay, Sort Order: 100)
  Component: CanvasScaler
  Component: GraphicRaycaster
  Component: SettingsPanel (skrypt)

  Child: Backdrop (Image, kolor #000000 alpha 0.7, RectTransform full screen)
    [Użyj Stretch + 0/0/0/0 anchors]

  Child: Panel (Image, kolor ciemny, RectTransform ~700x580, wycentrowany)
    Child: Title (TextMeshProUGUI, "USTAWIENIA", fontSize 36, bold, centered)
    Child: SFXLabel (TextMeshProUGUI, "Głośność SFX")
    Child: SFXSlider (Slider, Min=0, Max=1, Value=1)
    Child: MusicLabel (TextMeshProUGUI, "Głośność Muzyki")
    Child: MusicSlider (Slider, Min=0, Max=1, Value=0.4)
    Child: LangLabel (TextMeshProUGUI, "Język")
    Child: LangRow (HorizontalLayoutGroup)
      Child: BtnPL (Button, TMP "PL")
      Child: BtnEN (Button, TMP "EN")
    Child: GfxLabel (TextMeshProUGUI, "Grafika")
    Child: GfxRow (HorizontalLayoutGroup)
      Child: BtnLow  (Button, TMP "Niska")
      Child: BtnMed  (Button, TMP "Średnia")
      Child: BtnHigh (Button, TMP "Wysoka")
    Child: BackButton (Button)
      Child: BackLabel (TextMeshProUGUI, "Wróć")
```

Podepnij referencje w komponencie `SettingsPanel`:
- `titleLabel` → Title
- `sfxLabel` → SFXLabel, `sfxSlider` → SFXSlider
- `musicLabel` → MusicLabel, `musicSlider` → MusicSlider
- `langLabel` → LangLabel
- `btnPL` → BtnPL, `btnEN` → BtnEN
- `gfxLabel` → GfxLabel
- `btnQualityLow` → BtnLow, `btnQualityMed` → BtnMed, `btnQualityHigh` → BtnHigh
- `backButton` → BackButton, `backLabel` → BackLabel

### KROK 4 – Main Menu scene
1. Dodaj przycisk "Ustawienia" do Canvas MainMenu
2. Podepnij go w `MainMenuUI.settingsButton`
3. W On Click przycisku wywołaj `MainMenuUI.OnSettingsClicked()`

### KROK 5 – Arena (GameScene)
1. W `ArenaGameManager` w Inspectorze ustaw:
   - `Acceleration Start Time` = 60 (sekundy, możesz zmniejszyć do testów np. 20)
   - `Acceleration Interval` = 5
   - `Acceleration Per Step` = 0.12
2. Opcjonalnie dodaj przycisk "Ustawienia" (wywoła `ArenaGameManager.OpenSettings()`)
3. **End Panel (ESC)** – `restartPanel` musi być już przypisany – ESC go pokaże bez zmian w editorze

### KROK 6 – ShopScene
1. Dodaj przycisk "Ustawienia" (wywołuje `ShopUI.OpenSettings()`)
2. Lista `allClassConfigs` powinna zawierać wszystkie 16 konfiguracji **w tej samej kolejności** co `MergeUI.allClassConfigs`

### KROK 7 – MergeScene
1. Upewnij się że `allClassConfigs` ma tę samą kolejność co w ShopUI
2. Opcjonalnie dodaj przycisk "Ustawienia" (wywołuje `MergeUI.OpenSettings()`)

### KROK 8 – Sprite koła 1024×1024
1. Utwórz nowy sprite koła w wybranym narzędziu graficznym (1024×1024px, antyaliasing)
2. Importuj do Unity jako `Sprite (2D and UI)`, typ kompresji wg potrzeb
3. W `ClassConfig` każdej kulki przypisz nowy sprite w polu `ballSprite`
4. W `BallArenaUtils.cs` upewnij się że sprite jest używany zamiast generowanego (lub zaktualizuj generator)

### KROK 9 – Unity Quality Settings
Projekt powinien mieć przynajmniej 3 poziomy jakości (Edit → Project Settings → Quality):
- Index 0 = Niska / Low
- Index 1 = Średnia / Medium
- Index 2 = Wysoka / High

### KROK 10 – Weryfikacja Build Settings
Sprawdź że scena `SettingsScene` NIE jest potrzebna – panel działa jako overlay prefab.
Sceny w Build Settings (powinny być):
1. LogoScreen
2. MainMenu
3. ShopScene
4. MergeScene
5. GameScene
6. PaintScene

---

## Notatki techniczne

### AudioMixer (opcjonalne ulepszenie)
Skrypty używają bezpośrednio `AudioSource.volume` do skalowania głośności.
Jeśli chcesz użyć Unity AudioMixer (np. dla efektów kompresji):
1. Utwórz `Assets/Audio/GameMixer.mixer` w Editorze
2. Stwórz grupy `SFX` i `Music`
3. W AudioController zamień podejście `PlayOneShot(clip, volume)` na `AudioMixerGroup` z expose parametrów
4. W SettingsPanel używaj `mixer.SetFloat("SFXVolume", Mathf.Log10(v) * 20)` (logarytmiczna skala dB)

### Persystencja ustawień
Aktualnie ustawienia trzymane są w `GameData` (DontDestroyOnLoad, brak PlayerPrefs).
Przy restarcie gry ustawienia się resetują. Jeśli potrzeba trwałości:
```csharp
// W GameData.Awake() dodaj:
sfxVolume   = PlayerPrefs.GetFloat("sfxVol", 1f);
musicVolume = PlayerPrefs.GetFloat("musicVol", 0.4f);
language    = (GameLanguage)PlayerPrefs.GetInt("lang", 0);
qualityLevel= PlayerPrefs.GetInt("quality", 1);

// W SetSFXVolume/SetMusicVolume dodaj:
PlayerPrefs.SetFloat("sfxVol", sfxVolume);
PlayerPrefs.Save();
```
