# M0 — Stabilizacja stanu wejściowego

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **DO WYKONANIA**  
**Kryterium wejścia:** Baza `0.1.0-beta.11.1`, lokalny gate 310/310 oraz zielona regresja granicy pauzy 44/45 min.  
**Kryterium wyjścia:** Pełna regresja lokalnego drzewa zielona; brak P0/P1; kontrolowany punkt przywracania.  
**Następny etap:** M1

> Ten dokument jest samodzielnym wydzieleniem etapu M0 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** potwierdzić, że obecne drzewo jest stabilną bazą dla Planera.

### Zadania

- [ ] Zaktualizować `KNOWN_ISSUES.md`: problem 44/45 oznaczyć jako naprawiony lokalnie 24 lipca 2026.
- [ ] Zaktualizować `BETA_TEST_PLAN.md`: Test 1A oznaczyć jako zaliczony i zapisać scenariusz `41+3=44`.
- [ ] Wykonać pełną checklistę UI bieżącego drzewa.
- [ ] Sprawdzić wariant B wpisu manualnego.
- [ ] Sprawdzić katalog krajów i kod tachografowy.
- [ ] Sprawdzić `ODP. TYG.` na progach 89:39, 96:00 i 144:00+.
- [ ] Sprawdzić oba sloty, nakładki, OUT, Prom, restart i logi.
- [ ] Sprawdzić aktywną telemetrię, automatyczną Jazdę i blokady zależne od ruchu.
- [ ] Sprawdzić eksporty i zachowanie danych po restarcie.
- [ ] Utworzyć listę wszystkich pozostałych elementów UI i nadać im status `beta.12` albo `poza zakresem`.

### Gate M0

- pełna regresja lokalnego drzewa zielona;
- brak otwartych P0 i P1;
- wszystkie znane rozbieżności opisane;
- build 0/0;
- pełny pakiet testów zielony;
- kontrolowany punkt przywracania w repozytorium.

---

## Kontekst wejściowy

Zamrożona baza `0.1.0-beta.11.1` przeszła końcowy smoke z aktywną telemetrią. Bieżące drzewo zawiera lokalnie wariant B wpisu manualnego, katalog krajów oraz korektę `ODP. TYG.` i ma bazowy gate `310/310`, build Release `0/0`. Regresja `41 min reconstructed + 3 min telemetry = 44 min` przeszła 24 lipca 2026 na zielono, ale dokumentacja statusowa nadal wymaga aktualizacji.

## Wymagane dowody zamknięcia

- wynik pełnej checklisty UI;
- wynik testu aktywnej telemetrii i automatycznej Jazdy;
- wynik testów obu slotów, nakładek, OUT i Prom;
- wynik restartu, eksportów i kontroli logów;
- zamknięta inwentaryzacja UI z decyzją `beta.12` albo `poza zakresem`;
- commit lub równoważny punkt przywracania.

## Poza zakresem M0

- implementacja Planera;
- pełna lokalizacja PL/EN;
- budowanie artefaktu beta.12;
- poprawki P2 niezwiązane z potwierdzonym zakresem regresji.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M0

- ukryta regresja po lokalnych zmianach XAML;
- rozjazd Dashboardu, urządzenia i nakładek;
- błędne zachowanie wariantu B przy aktywnej telemetrii;
- pominięcie elementu UI, który później utrudni UI freeze;
- rozpoczęcie Planera na niestabilnej bazie.

Każdy problem P0/P1 zatrzymuje przejście do M1 i wymaga ścieżki **FIX** z testem regresyjnym.

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
