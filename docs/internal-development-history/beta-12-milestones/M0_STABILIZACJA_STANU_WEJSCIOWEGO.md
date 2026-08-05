# M0 — Stabilizacja stanu wejściowego

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status końcowy:** **ZAMKNIĘTY — GO (24 lipca 2026)**

**Kryterium wejścia:** Baza `0.1.0-beta.11.1`, lokalny gate 338/338 oraz zielona regresja granicy pauzy 44/45 min.

**Kryterium wyjścia:** Pełna regresja lokalnego drzewa zielona; brak P0/P1; kontrolowany punkt przywracania.  
**Następny etap:** M1

> Ten dokument jest samodzielnym wydzieleniem etapu M0 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** potwierdzić, że obecne drzewo jest stabilną bazą dla Planera.

### Zadania

- [x] Zaktualizować `KNOWN_ISSUES.md`: problem 44/45 oznaczyć jako naprawiony lokalnie 24 lipca 2026.
- [x] Zaktualizować `BETA_TEST_PLAN.md`: Test 1A oznaczyć jako zaliczony i zapisać scenariusz `41+3=44`.
- [x] Wykonać pełną checklistę UI bieżącego drzewa — 10/10 zielone.
- [x] Sprawdzić wariant B wpisu manualnego.
- [x] Sprawdzić katalog krajów i kod tachografowy.
- [x] Sprawdzić `ODP. TYG.` na progach 89:39, 96:00 i 144:00+ — WRF-01…16 oraz ręczny smoke LCD S1/S2 zielone.
- [x] Sprawdzić oba sloty, nakładki, OUT, Prom, restart i logi.
- [x] Sprawdzić aktywną telemetrię, automatyczną Jazdę i blokady zależne od ruchu.
- [x] Sprawdzić eksporty i zachowanie danych po restarcie.
- [x] Utworzyć listę wszystkich pozostałych elementów UI i nadać im status `beta.12` albo `poza zakresem` — inwentaryzacja UI zielona.

### Gate M0

- pełna regresja lokalnego drzewa zielona;
- brak otwartych P0 i P1;
- wszystkie znane rozbieżności opisane;
- build 0/0;
- pełny pakiet testów zielony;
- kontrolowany punkt przywracania w repozytorium.

**Decyzja gate:** **GO** — wszystkie warunki zostały spełnione 24 lipca 2026.

---

## Kontekst wejściowy

Zamrożona baza `0.1.0-beta.11.1` przeszła końcowy smoke z aktywną telemetrią. Bieżące drzewo zawiera wariant B wpisu manualnego, katalog krajów oraz korektę `ODP. TYG.`. Końcowy gate M0 wynosi `338/338`, a build Release `0/0`. Regresja `41 min reconstructed + 3 min telemetry = 44 min`, pełna checklista UI, smoke LCD S1/S2 oraz inwentaryzacja UI przeszły 24 lipca 2026 na zielono.

## Wymagane dowody zamknięcia

- pełna checklista UI: **10/10 zielone**;
- aktywna telemetria i automatyczna Jazda: **zielone**;
- oba sloty, nakładki, OUT i Prom: **zielone**;
- restart, eksporty i kontrola logów: **zielone**;
- smoke LCD S1/S2 po hotfixie `ODP. TYG.`: **zielony**;
- inwentaryzacja UI z decyzją `beta.12` albo `poza zakresem`: **zamknięta, zielona**;
- punkt przywracania: commit `50ee50a`.

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

## Status zamknięcia

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `50ee50a` — hotfix ODP.TYG.
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 338/338
- **Testy manualne / dowody:** checklista UI 10/10 zielona; smoke LCD S1/S2 zielony; inwentaryzacja UI zielona
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M1 może wystartować; inwentaryzacja UI jest gotowa jako wejście do M4.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
