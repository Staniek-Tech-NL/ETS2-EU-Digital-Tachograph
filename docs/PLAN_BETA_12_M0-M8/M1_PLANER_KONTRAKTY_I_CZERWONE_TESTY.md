# M1 — Planer: kontrakty i czerwone testy

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status:** **ROZSZERZONY — KONTRAKTY PLANOWANIA ZAŁOGI (2026-07-24)**
**Kryterium wejścia:** Formalny wynik **GO** dla M0.  
**Kryterium wyjścia:** Kontrakty zatwierdzone, czerwone testy kompletne, brak zależności od WPF i zapisu do SQLite.  
**Następny etap:** M2

> Ten dokument jest samodzielnym wydzieleniem etapu M1 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zamknąć kontrakty przed implementacją algorytmu.

### Zadania

- [x] Zatwierdzić `JOURNEY_PLANNER_MVP_PLAN.md` 2.2 jako specyfikację implementacyjną beta.12.
- [x] Dodać kontrakty request/result/status/confidence/segments/warnings/usage/limits.
- [x] Dodać `JourneyPlanningSnapshot` i `JourneyPlanSnapshotIdentity`.
- [x] Dodać `DailyRestPlanningWindow`.
- [x] Dodać testy `JP-P0-01–08`.
- [x] Dodać testy statusów `JP-ST-01–08`.
- [x] Dodać testy snapshotu i unieważniania wyniku.
- [x] Dodać testy zakończenia algorytmu i limitów bezpieczeństwa.
- [x] Dodać tryb `MultiManningCrew`, snapshot obu kart i wspólną oś czasu pojazdu.
- [x] Dodać segment równoległych aktywności S1/S2 z jednym prowadzącym.
- [x] Dodać testy `JP-CREW-P0-01–06`.
- [x] Nie tworzyć jeszcze UI Planera.

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

Planer nie może zapisywać hipotetycznych aktywności do SQLite ani modyfikować
prawdziwego `RegulationState`. Tryb jednej karty nie aktywuje podwójnej obsady
samym wyborem S1/S2. Tryb `MultiManningCrew` wymaga potwierdzonej aktywnej
załogi, snapshotu obu różnych kart i planuje przyszłe zmiany prowadzącego.

## Minimalne klasy czerwonych testów

- `JP-P0-01–08` — limity 56/90 h, `CalendarWait`, ukończenie odpoczynku 24/30 h, bufor i bezpieczny wariant odpoczynku tygodniowego;
- `JP-ST-01–08` — wszystkie statusy wyniku;
- testy tożsamości i unieważniania snapshotu;
- testy deterministyczności;
- testy limitów segmentów, czasu i odwiedzonych stanów;
- test potwierdzający brak operacji zapisu do SQLite.
- `JP-CREW-P0-01–06` — przejęcie jazdy bez postoju, niepełna przerwa,
  rozdzielenie przerwy od odpoczynku dobowego, krótszy przyjazd, warunek
  aktywnej załogi i zakaz jednoczesnej jazdy obu kart.

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

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `879eda5` — kontrakty i czerwone testy M1
- **Rozszerzenie kontraktów załogi:** `db34de2`
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** regresja + kontrakty M1 355/355; pakiet `Stage=M2Red`
  13/13 prawidłowo czerwony na celowej granicy `NotImplementedException`
- **Testy manualne / dowody:** nie dotyczy — brak zmian UI
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M2 ma zastąpić celową granicę
  `JourneyPlanningEngine.Plan` deterministycznym silnikiem zdarzeniowym i
  sukcesywnie zazielenić pakiet `Stage=M2Red`.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
