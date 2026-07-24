# M2 — Planer: silnik zdarzeniowy

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Kontrakty M1 zaakceptowane; testy blokujące istnieją i prawidłowo zawodzą bez implementacji.  
**Kryterium wyjścia:** Testy P0 i algorytmu zielone; deterministyczny wynik; kontrolowane zakończenie każdej kalkulacji.  
**Następny etap:** M3

> Ten dokument jest samodzielnym wydzieleniem etapu M2 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zaimplementować deterministyczny silnik strategii „Najwcześniejsza legalna”.

### Zadania

- [ ] Zaimplementować stan roboczy i zdarzenia regulacyjne.
- [ ] Zaimplementować jazdę ciągłą i przerwy.
- [ ] Zaimplementować limity dzienne 9/10 h.
- [ ] Zaimplementować odpoczynek dobowy 9/11 h z terminem ukończenia.
- [ ] Zaimplementować limity 56 h i 90 h.
- [ ] Zaimplementować `CalendarWait` bez podwójnego naliczania czasu.
- [ ] Zaimplementować odpoczynki tygodniowe zgodnie z obecnym modelem.
- [ ] Zaimplementować bufor jako `OtherWorkAfterArrival`.
- [ ] Zaimplementować ograniczone rozgałęzienie i ranking wariantów.
- [ ] Zaimplementować deduplikację stanów.
- [ ] Zaimplementować gwarancję postępu i kontrolowane zakończenie.
- [ ] Potwierdzić brak zapisu do bazy i brak modyfikacji prawdziwego stanu kierowcy.

### Gate M2

- wszystkie testy P0 zielone;
- wszystkie testy algorytmu zielone;
- ten sam snapshot daje identyczny wynik;
- każda kalkulacja kończy się kontrolowanym wynikiem;
- brak nieskończonych pętli;
- brak hipotetycznych rekordów w SQLite.

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

## Niezmienniki silnika

- po każdej iteracji maleje pozostały czas jazdy albo rośnie `currentGameMinute`;
- ten sam snapshot i request dają identyczny wynik;
- `CalendarWait` i odpoczynek nie dublują czasu;
- wariant o ograniczonej wiarygodności nie wygrywa bezwarunkowo z wariantem w pełni zweryfikowanym;
- każda kalkulacja kończy się wynikiem, `NoLegalContinuation` albo `CalculationLimitReached`;
- żadna hipotetyczna aktywność nie trafia do historii ani SQLite.

## Poza zakresem M2

- warstwa Application;
- prezentacja WPF;
- zapisywanie lub automatyczna korekta planu w ruchu.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M2

- powstanie drugiego, rozbieżnego silnika reguł;
- nieskończona pętla lub eksplozja liczby stanów;
- fałszywy reset limitów 56/90 h;
- rozpoczęcie odpoczynku zbyt późno, aby zakończył się w oknie 24/30 h;
- podwójne naliczanie czasu odpoczynku i `CalendarWait`;
- przypadkowy zapis hipotetycznych danych.

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
