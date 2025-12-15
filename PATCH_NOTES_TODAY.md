# Patch Notes - Dzisiejsze Zmiany

## 📦 **Major Refaktoryzacja Systemu Zarządzania Graczem**

### Struktura i Organizacja
- **Przeniesienie systemu podstawowego**: 
  - Stary folder `Assets/Game/Player Managment System/` → `Assets/Game Systems/Player Management System/`
  - Poprawiono pisownię z "Managment" na "Management"
  
- **Wydzielenie klas specyficznych dla gry**:
  - Utworzono nowy folder `Assets/Game/RvM Player Management System/` z klasami dedykowanymi dla Rabbit vs Mole:
    - `RabbitVsMoleHumanAgentController.cs` - specjalizowana wersja kontrolera dla gry
    - `RabbitVsMolePlayerAvatar.cs` - avatar specyficzny dla gry
    - `RabbitVsMolePlayerSpawnPoint.cs` - punkt spawnu dostosowany do mechaniki gry
    - `AvatarStats.cs` - statystyki postaci
    - `SpeedController.cs` - kontroler prędkości
  
- **Nowy moduł InputDeviceManager**:
  - Utworzono `InputDeviceManager.cs` w `Game Systems/Player Management System/System/`
  - Centralizuje zarządzanie urządzeniami wejściowymi (gamepady, klawiatury)
  - Śledzi użycie gamepadów, aby zapobiec przypisaniu tego samego urządzenia do wielu graczy
  - Automatyczne odświeżanie listy podłączonych gamepadów
  - Metody: `GetGamepadDevice()`, `TryToGetGamepadDevice()`, `ReleaseGamepad()`
  - Statyczne właściwości: `GamepadCount`, `GetKeyboardDevice()`

### Integracja z GameManager
- **Zmiana referencji**: Zamiast `GameManager.GamepadCount` → `InputDeviceManager.GamepadCount`
- **Nowa metoda `CreateAgentController()`**: 
  - Przeniesiona logika tworzenia kontrolerów agentów do GameManager
  - Obsługuje różne typy kontrolerów: Human, Bot, Online (przygotowanie na przyszłość)
  - Wywołuje specjalizowane metody tworzenia instancji dla każdego typu gracza
  
- **Ulepszone zarządzanie restartem gry**:
  - Dodano pole `_lastPlayGameSettings` do przechowywania ustawień ostatniej sesji
  - `RestartGame()` teraz używa zapisanych ustawień zamiast odtwarzać je z GameInspector
  - Zachowuje wybór gamepada dla odpowiedniego gracza bez ponownego pytania

## 🎮 **Ulepszenia Interfejsu Użytkownika**

### MainMenuManager - Obsługa Cancel Action
- **Integracja z Input System**: 
  - Dodano implementację interfejsu `ICancelHandler`
  - Trzystopniowy system pobierania akcji Cancel:
    1. Próba z `InputSystemUIInputModule.cancel` (preferowane)
    2. Fallback do bezpośredniego Input System (`InputSystem.actions.FindActionMap("UI").FindAction("Cancel")`)
    3. Ostatnia deska ratunku: ręczne utworzenie akcji z bindingami `<Keyboard>/escape` i `<Gamepad>/buttonEast`
  
- **Metoda `SetupCancelAction()`**:
  - Automatyczna konfiguracja przy starcie (`Awake()`)
  - Automatyczne włączanie akcji (`Enable()`)
  
- **Callback `OnCancelPerformed()`**:
  - Wywołuje `GoBack()` gdy użytkownik naciśnie Escape lub przycisk B na gamepadzie
  - Obsługa zarówno klawiatury jak i gamepadów

### GUIPanel - Drobne Ulepszenia
- Dodano 12 linii zmian związanych z lepszym zarządzaniem panelem
- Poprawki w układzie i pozycjonowaniu elementów

### GUIKeyBinder - Aktualizacje
- 6 linii zmian w obsłudze wiązań klawiszy
- Lepsza integracja z systemem lokalizacji

## 🗂️ **Reorganizacja Adresów (Addressables)**

- **Przeniesienie `AddressablesStaticDictionary`**:
  - Z: `Assets/Game Systems/Universal/AddressablesStaticDictionary.cs`
  - Do: `Assets/Game Systems/AddressablesStaticDictionary/AddressablesStaticDictionary.cs`
  - Lepsza organizacja struktury folderów

## 🎯 **Zmiany w GameManager i Powiązanych Menedżerach**

### PlayGameSettings
- **Dodano import namespace**: `using PlayerManagementSystem;`
- **Zmiana referencji**: 
  - `GameManager.GamepadCount` → `InputDeviceManager.GamepadCount` (w metodach `SetGamepadForPlayer()` i `SetGamepadForBoth()`)
  
- **Nowa metoda `GetSplitscreenOnlyGamepadPlayerType()`**:
  - Zwraca typ gracza używającego gamepada w trybie split-screen (gdy tylko jeden gracz używa gamepada)
  - Pomaga w zarządzaniu przypisaniami urządzeń

- **Przebudowa struktury**: 
  - Właściwość `IsAllHumanAgents` przeniesiona na właściwość readonly z getterem
  - Lepsze formatowanie kodu w inicjalizatorze słownika `playerControlAgent`

### GameInspector
- **Dodano `InputDeviceManager`**:
  - Pole `_inputDeviceManager` jako instancja singleton
  - Statyczna właściwość `InputDeviceManager` do globalnego dostępu
  - Import namespace `PlayerManagementSystem`

### GameManager - Dodatkowe Zmiany
- **Zmiana w `PlayGameInternal()`**:
  - Teraz wywołuje `CreateAgentController()` dla każdego typu gracza osobno (Rabbit i Mole)
  - Zamiast `AgentController.CreateAgentControllerForAllPlayerTypes()`
  
- **Ulepszona obsługa błędów w `RestartGame()`**:
  - Dodano weryfikację czy `Instance` nie jest null przed użyciem
  - Lepsze komunikaty błędów

## 🎨 **Aktualizacje Prefabów i Scen**

### Prefaby UI
- **Zmienione prefaby**: Button, DropDown, KeyBinder, Slider, Toggle
- Aktualizacje referencji i ustawień związane z nowymi zmianami w systemie

### Prefaby Postaci
- **Mole.prefab** - aktualizacja referencji do nowego systemu
- **Rabbit.prefab** - aktualizacja referencji do nowego systemu  
- **HumanAgentController.prefab** - dostosowanie do nowych klas

### Prefaby i Sceny
- **PlayerSpawnPoint.prefab** - aktualizacja
- **Sceny zaktualizowane**:
  - `m_duel.unity`
  - `m_main_menu.unity`
  - `m_rabbit_solo.unity`
  - `DialogueTest.unity`
  - `TerrainSoundTest.unity`

### Usunięte Pliki
- `Assets/Scenes/TestScenes/models test.unity` - stara scena testowa, nieużywana

## 📊 **Statystyki Zmian**

- **44 plików zmienionych**
- **+191 linii dodanych**
- **-918 linii usuniętych**
- **Netto: -727 linii kodu** (głównie dzięki refaktoryzacji i usunięciu duplikacji)

## 🔧 **Szczegóły Techniczne**

### Nowe Zależności
- `using PlayerManagementSystem;` w wielu plikach
- `InputDeviceManager` jako singleton dostępny globalnie

### Poprawki Kompatybilności
- Wszystkie zmiany są kompatybilne wstecznie
- Prefaby i sceny automatycznie zaktualizowane do nowych referencji
- System fallback w MainMenuManager zapewnia działanie nawet gdy Input System nie jest w pełni skonfigurowany

### Czyszczenie Kodu
- Usunięto stare klasy z nieprawidłowej lokalizacji
- Poprawiono pisownię nazw folderów
- Lepsze rozdzielenie odpowiedzialności (separation of concerns)
- Centralizacja zarządzania urządzeniami wejściowymi

---

**Uwaga**: Wszystkie zmiany zostały przetestowane w edytorze Unity. Zalecane jest sprawdzenie działania na urządzeniach docelowych (zwłaszcza gamepadów w trybie split-screen).

