# M6 — Release Candidate `0.1.0-beta.12`

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status:** **W TOKU**
**Data rozpoczęcia:** 28 lipca 2026
**Kryterium wejścia:** Pełna regresja funkcjonalna i lokalizacyjna PL/EN jest zielona.  
**Kryterium wyjścia:** Niezmienny, identyfikowalny artefakt gotowy do końcowego smoke testu.  
**Następny etap:** M7

> Ten dokument jest samodzielnym wydzieleniem etapu M6 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zbudować jeden niezmienny artefakt do końcowego smoke testu.

### Zadania

- [x] Ustawić numer `0.1.0-beta.12` we wszystkich wymaganych metadanych.
- [x] Wykonać pełny build Release.
- [x] Uruchomić pełny pakiet testów automatycznych.
- [ ] Sprawdzić start na istniejącej bazie użytkownika.
- [ ] Sprawdzić start na czystej bazie.
- [ ] Sprawdzić migracje, backup i dwa restarty.
- [ ] Wykonać pełną regresję funkcjonalną PL.
- [ ] Wykonać pełną regresję funkcjonalną EN.
- [ ] Porównać dane raportów PL/EN.
- [x] Zaktualizować README, release notes, known issues i plan testów.
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

## Unieważniony pierwszy kandydat

Pierwszy self-contained z commita `47cc9e5` został uruchomiony na kopii
istniejącej bazy. Start PL, przejście przez siedem głównych ekranów, przełączenie
na `en-GB` i ponowny start były poprawne, ale Historia oraz Rekompensaty nadal
wyświetlały polskie `Dzień` w widocznych wartościach czasu gry.

Kandydat `47cc9e5` jest **unieważniony**: nie utworzono z niego ZIP-a i nie może
być wejściem do M7. Poprawka przenosi widoczne wartości czasu gry na lokalny
presenter Desktopu używający `GameCalendar_DayFormat`. Test regresyjny
`Visible_game_clock_values_use_selected_ui_culture` przeszedł najpierw na
czerwono (`Dzień 2, 00:00` zamiast `Day 2, 00:00`), a po poprawce na zielono.
Pełna regresja po poprawce: 570/570 PASS; Release: 0 błędów / 0 ostrzeżeń.
Nowy self-contained musi zostać zbudowany i zweryfikowany z nowego commita
źródłowego.

## Szablon aktualizacji statusu

- **Data rozpoczęcia:** 2026-07-28
- **Data zakończenia:**
- **Wynik:** `W TOKU`
- **Commit / punkt przywracania:** `9f61da5` — wejście do M6;
  `47cc9e5` — pierwszy kandydat unieważniony po walidacji EN;
  gałąź `codex/m6-release-candidate-beta-12`
- **Build Release:** przygotowawczy gate 0 błędów / 0 ostrzeżeń;
  `FileVersion 0.1.12.0`,
  `ProductVersion 0.1.0-beta.12+47cc9e5a8aecda4bcfde8c35801d84f2496b43f9`.
  Do powtórzenia z finalnego commita źródłowego.
- **Testy automatyczne:** 570/570 PASS po poprawce lokalizacji czasu gry.
  Do powtórzenia z finalnego commita
  źródłowego przed publikacją self-contained.
- **Testy manualne / dowody:** checkpoint M5.2-P GO; pierwszy smoke
  self-contained wykrył i zatrzymał pozostałość `Dzień` w EN; kandydat
  `47cc9e5` unieważniony
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M7 nie rozpoczyna się przed zamrożeniem ZIP-a
  i zapisaniem commita oraz SHA-256. Paczka musi zawierać
  `docs/THIRD_PARTY_NOTICES.md` z notami Unicode CLDR i SCS SDK.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
