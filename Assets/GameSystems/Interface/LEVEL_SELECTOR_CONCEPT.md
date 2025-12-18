# Koncepcja Selektora Poziomów Story Mode

## Przegląd
Selektor poziomów pozwala graczowi wybrać misję story mode dla królika lub kreta. Każdy poziom reprezentuje jeden dzień tygodnia (poniedziałek-niedziela = 7 misji).

## Wymagania Funkcjonalne

### 1. Informacje wyświetlane dla każdego poziomu:
- **Nazwa poziomu**: Dzień tygodnia (np. "Poniedziałek", "Wtorek")
- **Opis wprowadzający**: Tekst opisujący misję (z lokalizacji)
- **Status ukończenia**: Wizualna informacja czy poziom został ukończony (✓/✗)
- **Status złotej marchewki**: Wizualna informacja czy złota marchewka została zebrana (🥕/✓)

### 2. Funkcjonalność:
- Kliknięcie w poziom ładuje odpowiednią scenę gameplay
- Poziom powinien być klikalny (może być zablokowany jeśli poprzedni nie ukończony?)
- Wizualne wyróżnienie ukończonych poziomów i poziomów ze złotą marchewką

## Architektura Rozwiązania

### 1. Nowy Element GUI: `GUILevelSelectorItem`
- Dziedziczy po `LocalizedElementBase` (lub `InterfaceElement`)
- Zawiera:
  - Główny tekst: Nazwa dnia tygodnia
  - Opis: Tekst wprowadzający misji
  - Ikona ukończenia: ✓/✗
  - Ikona złotej marchewki: 🥕/✓
  - Przycisk: Klikalny obszar do uruchomienia poziomu

### 2. Metody w `MainMenuManager`:
```csharp
internal InterfaceElement CreateLevelSelectorItem(
    PlayerType playerType, 
    DayOfWeek dayOfWeek, 
    LocalizedString levelName, 
    LocalizedString levelDescription, 
    Action onClick)
```

### 3. Metody w `PanelBuilder`:
```csharp
public PanelBuilder AddLevelSelector(
    PlayerType playerType, 
    DayOfWeek dayOfWeek, 
    LocalizedString levelName, 
    LocalizedString levelDescription)
```

### 4. Integracja z GameManager:
- `GameManager.GetRabbitStoryProgress(DayOfWeek)` - sprawdza ukończenie
- `GameManager.IsGoldenCarrotCollected(DayOfWeek)` - sprawdza złotą marchewkę
- `GameManager.SetCurrentDayOfWeek(DayOfWeek)` - ustawia dzień przed załadowaniem sceny (może wymagać dodania)

### 5. Integracja z GameSceneManager:
- `GameSceneManager.ChangeScene(SceneType.Gameplay_RabbitSolo)` - dla królika
- `GameSceneManager.ChangeScene(SceneType.GamePlay_MoleSolo)` - dla kreta

## Struktura UI Elementu

```
┌─────────────────────────────────────┐
│ [Nazwa Dnia]        [✓] [🥕]        │
│ Opis wprowadzający misji...         │
└─────────────────────────────────────┘
```

Lub bardziej szczegółowo:
```
┌─────────────────────────────────────┐
│ Poniedziałek          [✓] [🥕]     │
│ ─────────────────────────────────   │
│ Rozpocznij swoją przygodę jako...   │
│ [Kliknij aby rozpocząć]              │
└─────────────────────────────────────┘
```

## Klucze Lokalizacji (przykładowe)

- `level_monday_name` - "Poniedziałek"
- `level_tuesday_name` - "Wtorek"
- `level_monday_description_rabbit` - Opis misji dla królika
- `level_monday_description_mole` - Opis misji dla kreta
- `level_completed` - "Ukończone"
- `level_golden_carrot_collected` - "Złota marchewka zebrana"

## Implementacja w RabbitVsMoleMenuSetup

```csharp
_playPanelStoryRabbit = _menuManager.CreatePanel(GetLocalizedString("menu_play_story_rabbit"))
    .AddLevelSelector(PlayerType.Rabbit, DayOfWeek.Monday, 
        GetLocalizedString("level_monday_name"), 
        GetLocalizedString("level_monday_description_rabbit"))
    .AddLevelSelector(PlayerType.Rabbit, DayOfWeek.Tuesday, ...)
    // ... dla wszystkich 7 dni
    .AddBackButton()
    .Build();
```

## Wizualne Stany

1. **Nieodblokowany**: Szary, nieaktywny (jeśli poprzedni nie ukończony)
2. **Aktywny**: Normalny kolor, klikalny
3. **Ukończony**: Zielony checkmark ✓
4. **Ze złotą marchewką**: Ikona 🥕 obok checkmarka

## Uwagi Implementacyjne

1. **Prefab**: Należy stworzyć prefab `LevelSelectorItem.prefab` w `Assets/Interface/elements/`
2. **Ikony**: Można użyć TextMeshPro z emoji lub Image z sprite'ami
3. **Layout**: Użyć Vertical Layout Group w panelu dla automatycznego układania poziomów
4. **Scroll**: Jeśli poziomów jest dużo, panel powinien mieć ScrollRect

