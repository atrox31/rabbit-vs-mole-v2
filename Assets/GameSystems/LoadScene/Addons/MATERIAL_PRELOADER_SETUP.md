# MaterialPreloader - Instrukcja Konfiguracji

## Jak działa MaterialPreloader

`MaterialPreloader` **automatycznie przeszukuje scenę** i znajduje tylko te materiały, które są faktycznie używane w scenie (przez MeshRenderer). Następnie ładuje je z Addressables używając **optymalnej metody batch loading**.

### Proces działania:

1. **Przeszukuje scenę** - znajduje wszystkie unikalne materiały używane w MeshRendererach
2. **Zbiera adresy** - pobiera nazwy materiałów (używane jako adresy w Addressables)
3. **Ładuje batch'em** - ładuje wszystkie materiały jednocześnie (optymalne)
4. **Fallback** - jeśli batch loading nie działa, ładuje pojedynczo

## Konfiguracja materiałów w Addressables

### 1. Oznacz materiały jako Addressables (WYMAGANE)

- W Unity, wybierz materiały które chcesz preloadować
- W Inspectorze, kliknij **"Address"** i zaznacz checkbox
- **WAŻNE:** Upewnij się, że materiały mają **unikalne nazwy** - nazwa materiału będzie używana jako adres w Addressables

### 2. Organizacja w grupach (OPCJONALNE, ale zalecane)

**Dlaczego warto utworzyć grupę:**
- Lepsze zarządzanie materiałami
- Optymalizacja bundli (Addressables może lepiej pakować materiały razem)
- Łatwiejsze wyszukiwanie i organizacja

**Jak utworzyć grupę:**
- Otwórz **Window > Asset Management > Addressables > Groups**
- Kliknij prawym przyciskiem i wybierz **Create > Group > Packed Assets**
- Nazwij ją np. "Materials" lub "Scene Materials"
- Przenieś tam wszystkie materiały które chcesz preloadować

### 3. Label (OPCJONALNE)

Label **NIE jest wymagany** do działania MaterialPreloader. Możesz go użyć do:
- Organizacji materiałów (np. `Scene1_Materials`, `Common_Materials`)
- Innych celów w projekcie
- **MaterialPreloader nie używa labeli** - ładuje tylko materiały znalezione w scenie

## Przykład użycia

```csharp
// MaterialPreloader automatycznie:
// 1. Przeszukuje scenę i znajduje używane materiały
// 2. Ładuje je batch'em z Addressables (optymalne)
// 3. Fallback na pojedyncze ładowanie jeśli potrzeba

yield return MaterialPreloader.PreloadMaterialAssets(
    scene, 
    loadingProgress, 
    progressFrom, 
    progressTo);
```

## Najlepsze praktyki

1. **Unikalne nazwy materiałów:**
   - Każdy materiał powinien mieć unikalną nazwę
   - Nazwa materiału = adres w Addressables

2. **Grupuj materiały:**
   - Utwórz dedykowaną grupę Addressables dla materiałów
   - To ułatwia zarządzanie i optymalizację bundli

3. **Optymalizacja bundli:**
   - W ustawieniach grupy, użyj **Packed Assets** (domyślnie)
   - Addressables automatycznie zoptymalizuje rozmiar bundli

4. **Weryfikacja:**
   - Sprawdź czy wszystkie materiały używane w scenie są oznaczone jako Addressables
   - Upewnij się, że mają unikalne nazwy
   - Uruchom build Addressables i sprawdź czy wszystko działa

## Troubleshooting

**Problem:** Materiały nie ładują się
- Sprawdź czy materiały są oznaczone jako Addressables (checkbox "Address")
- Sprawdź czy materiały mają unikalne nazwy
- Sprawdź logi - system pokaże które materiały się nie załadowały

**Problem:** Materiały ładują się wolno
- Sprawdź logi - powinno być "batch load", nie "individual loading"
- Jeśli używa "individual loading", sprawdź czy wszystkie materiały są w Addressables
- Rozważ optymalizację bundli w ustawieniach grupy

**Problem:** Niektóre materiały się nie ładują
- Sprawdź czy materiały są faktycznie używane w scenie (MeshRenderer)
- Sprawdź czy materiały mają unikalne nazwy w Addressables
- Sprawdź logi - system pokaże które materiały się nie załadowały

