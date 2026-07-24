# M6 — Release Candidate `0.1.0-beta.12`

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Pełna regresja funkcjonalna i lokalizacyjna PL/EN jest zielona.  
**Kryterium wyjścia:** Niezmienny, identyfikowalny artefakt gotowy do końcowego smoke testu.  
**Następny etap:** M7

> Ten dokument jest samodzielnym wydzieleniem etapu M6 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zbudować jeden niezmienny artefakt do końcowego smoke testu.

### Zadania

- [ ] Ustawić numer `0.1.0-beta.12` we wszystkich wymaganych metadanych.
- [ ] Wykonać pełny build Release.
- [ ] Uruchomić pełny pakiet testów automatycznych.
- [ ] Sprawdzić start na istniejącej bazie użytkownika.
- [ ] Sprawdzić start na czystej bazie.
- [ ] Sprawdzić migracje, backup i dwa restarty.
- [ ] Wykonać pełną regresję funkcjonalną PL.
- [ ] Wykonać pełną regresję funkcjonalną EN.
- [ ] Porównać dane raportów PL/EN.
- [ ] Zaktualizować README, release notes, known issues i plan testów.
- [ ] Przygotować self-contained `win-x64`.
- [ ] Dołączyć właściwą DLL pluginu v3.
- [ ] Utworzyć ZIP.
- [ ] Obliczyć SHA-256.
- [ ] Zapisać commit/tag źródłowy artefaktu.
- [ ] Potwierdzić czyste repozytorium.

### Gate M6

- wszystkie testy zielone;
- build `0 błędów / 0 nowych ostrzeżeń`;
- brak otwartych P0/P1;
- pełna regresja PL i EN zielona;
- artefakt identyfikowalny przez numer, commit i SHA-256;
- ZIP nie jest już modyfikowany.

---

## Macierz walidacji RC

| Obszar | Automatyczne | Manualne PL | Manualne EN | Wymagane przed M7 |
|---|---:|---:|---:|---:|
| Core / historia / reguła jednej minuty | Tak | Kontrolne | Kontrolne | Tak |
| Telemetria v3 | Tak + E2E | Tak | Tak | Tak |
| Engine / skoki / cargo / dwie karty | Tak | Tak | Tak | Tak |
| RuleEngine i rekompensaty | Tak | Tak | Tak | Tak |
| SQLite / migracje / restart | Tak | Tak | Tak | Tak |
| Wpis manualny wariant B | Tak | Tak | Tak | Tak |
| Planer | Tak | Tak | Tak | Tak |
| Desktop / XAML i nakładki | Gdzie możliwe | Pełne | Pełne | Tak |
| PDF / JSON / CSV / `.tacho` | Tak | Kontrolne | Kontrolne | Tak |
| Lokalizacja / zasoby | Tak | Pełne | Pełne | Tak |
| Czysta i istniejąca baza | Integracyjne | Tak | Tak | Tak |

Liczba testów nie jest zamrożona na 310. Warunkiem jest 100% wymaganych testów zielonych.

## Kryteria gotowości do smoke

- Planer i UI są funkcjonalnie kompletne;
- PL i EN są kompletne;
- nie ma P0/P1;
- start i restart działają na istniejącej oraz czystej bazie;
- artefakt ma numer, commit i SHA-256;
- zawartość ZIP-a, w tym plugin v3 i dokumentacja, jest zamrożona.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M6

- zbudowanie RC z nieczystego lub nieidentyfikowalnego drzewa;
- różna DLL pluginu w paczce i podczas testu;
- zmiana pliku po obliczeniu SHA-256;
- pominięcie startu na istniejącej albo czystej bazie;
- pozostawienie P0/P1 przed smoke.

## Szablon aktualizacji statusu

- **Data rozpoczęcia:**
- **Data zakończenia:**
- **Wynik:** `GO` / `FIX` / `HOLD` / `NIE DOTYCZY`
- **Commit / punkt przywracania:**
- **Build Release:**
- **Testy automatyczne:**
- **Testy manualne / dowody:**
- **Otwarte błędy P0:**
- **Otwarte błędy P1:**
- **Uwagi do następnego etapu:**

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
