# M1 — Planer: kontrakty i czerwone testy

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Formalny wynik **GO** dla M0.  
**Kryterium wyjścia:** Kontrakty zatwierdzone, czerwone testy kompletne, brak zależności od WPF i zapisu do SQLite.  
**Następny etap:** M2

> Ten dokument jest samodzielnym wydzieleniem etapu M1 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zamknąć kontrakty przed implementacją algorytmu.

### Zadania

- [ ] Zatwierdzić `JOURNEY_PLANNER_MVP_PLAN.md` 2.2 jako specyfikację implementacyjną beta.12.
- [ ] Dodać kontrakty request/result/status/confidence/segments/warnings/usage/limits.
- [ ] Dodać `JourneyPlanningSnapshot` i `JourneyPlanSnapshotIdentity`.
- [ ] Dodać `DailyRestPlanningWindow`.
- [ ] Dodać testy `JP-P0-01–08`.
- [ ] Dodać testy statusów `JP-ST-01–08`.
- [ ] Dodać testy snapshotu i unieważniania wyniku.
- [ ] Dodać testy zakończenia algorytmu i limitów bezpieczeństwa.
- [ ] Nie tworzyć jeszcze UI Planera.

### Gate M1

- kontrakty zaakceptowane;
- testy blokujące istnieją i prawidłowo zawodzą bez implementacji;
- brak zależności Planera od WPF;
- brak operacji zapisu do SQLite w kontrakcie Planera.

---

## Kontrakt zakresowy Planera beta.12

Planer realizuje wyłącznie strategię **„Najwcześniejsza legalna”**. Musi korzystać z atomowego `JourneyPlanningSnapshot`, działać wyłącznie do odczytu, korzystać z tego samego modelu regulacyjnego co Dashboard i zwracać wielostanowy wynik z poziomem wiarygodności.

Wymagane elementy modelu:

- `CalendarWait` dla limitów 56 h i 90 h;
- termin ukończenia odpoczynku w oknie 24/30 h;
- jazda dzienna 9/10 h i limit dwóch wydłużeń;
- odpoczynek dobowy 9/11 h;
- przerwa 45 min oraz wykorzystanie już zaliczonych 15 min;
- odpoczynek tygodniowy 24/45 h tylko w granicach bieżącego modelu rekompensat;
- bufor jako `OtherWorkAfterArrival`;
- osobny czas przyjazdu i zakończenia dostawy;
- ograniczone rozgałęzienie, deduplikacja stanów, gwarancja postępu i limity obliczeń;
- obsługa luk, braku telemetrii i unieważnienia snapshotu.

Planer nie może zapisywać hipotetycznych aktywności do SQLite, modyfikować prawdziwego `RegulationState`, planować przyszłych zmian kierowcy ani aktywować podwójnej obsady samym wyborem S1/S2.

## Minimalne klasy czerwonych testów

- `JP-P0-01–08` — limity 56/90 h, `CalendarWait`, ukończenie odpoczynku 24/30 h, bufor i bezpieczny wariant odpoczynku tygodniowego;
- `JP-ST-01–08` — wszystkie statusy wyniku;
- testy tożsamości i unieważniania snapshotu;
- testy deterministyczności;
- testy limitów segmentów, czasu i odwiedzonych stanów;
- test potwierdzający brak operacji zapisu do SQLite.

## Poza zakresem M1

- implementacja algorytmu;
- `JourneyPlannerService`;
- XAML, ViewModel i zakładka Planera.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M1

- niepełny kontrakt prowadzący do zmian API w trakcie implementacji;
- ukryte uzależnienie od WPF lub persistence;
- pominięcie statusu terminalnego albo mechanizmu unieważniania;
- testy, które nie odtwarzają krytycznych granic 56/90 h oraz 24/30 h.

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
