# M3 — Planer: Application Service i UI

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status bieżący:** **IMPLEMENTACJA ZAKOŃCZONA — HOLD DO TESTU RĘCZNEGO UŻYTKOWNIKA**
**Kryterium wejścia:** Silnik M2 i jego testy są zielone.  
**Kryterium wyjścia:** Kompletny przepływ użytkownika Planera, zgodność z RuleEngine i poprawne unieważnianie wyniku.  
**Następny etap:** M4

> Ten dokument jest samodzielnym wydzieleniem etapu M3 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** udostępnić Planer jako kompletny przepływ użytkownika.

### Zadania

- [x] Zaimplementować `JourneyPlannerService`.
- [x] Zapewnić atomowe pobieranie snapshotu.
- [x] Obsłużyć `CardRemoved`, `ForwardTimeJump`, brak telemetrii i stale snapshot.
- [x] Dodać `JourneyPlannerViewModel` i widok Planera.
- [x] Dodać zakładkę `PLANER` do nawigacji.
- [x] Dodać formularz: czas jazdy GPS, czas do zakończenia dostawy, bufor, karta.
- [x] Dodać walidację `HH:MM`, w tym godziny powyżej 23.
- [x] Dodać wynik: status, wiarygodność, przyjazd, zakończenie, margines, segmenty i ostrzeżenia.
- [x] Dodać prezentację `CalendarWait` i powodów segmentów.
- [x] Dodać unieważnienie starego wyniku po zmianie stanu.
- [x] Sprawdzić S1 i S2 bez sztucznego aktywowania multi-manning.

### Gate M3

- pełny przepływ od formularza do harmonogramu działa;
- wynik jest zgodny z RuleEngine;
- zmiana stanu kierowcy unieważnia wynik;
- brak zapisu planu do historii;
- UI Planera przechodzi testy funkcjonalne i wizualne.

Stan gate'u:

- [x] pełny przepływ od formularza do harmonogramu jest pokryty automatycznie;
- [x] wynik pochodzi z `JourneyPlanningEngine` i tego samego stanu regulacyjnego;
- [x] zmiana snapshotu unieważnia widoczny wynik;
- [x] Planer nie zapisuje hipotetycznych aktywności;
- [x] automatyczne testy ViewModelu i kontrolny smoke wizualny zakładki są zielone;
- [ ] końcowy test ręczny użytkownika.

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

## Wymagany przepływ użytkownika

1. Użytkownik wybiera kartę i podaje pozostały czas jazdy GPS, czas do zakończenia dostawy oraz opcjonalny bufor.
2. `JourneyPlannerService` pobiera jeden atomowy snapshot.
3. Formularz waliduje czas trwania `HH:MM`, także dla godzin powyżej 23.
4. Silnik zwraca status, confidence, przyjazd, zakończenie, margines, segmenty i ostrzeżenia.
5. UI pokazuje powody segmentów, wykorzystane wyjątki oraz `CalendarWait`.
6. Zmiana sesji, świata, high-water mark, karty lub historii unieważnia wynik.

## Poza zakresem M3

- pełna lokalizacja zasobów;
- zapisywanie planów;
- dynamiczne przeliczanie planu w ruchu;
- przyszłe zmiany kierowców.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M3

- snapshot złożony z danych z różnych momentów;
- reguły domenowe przeniesione do ViewModelu;
- nieaktualny wynik pozostający widoczny jako ważny;
- wybór karty błędnie aktywujący tryb 30 h;
- różnica między wynikiem Planera a stanem Dashboardu.

## Szablon aktualizacji statusu

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia implementacji:** 2026-07-24
- **Wynik:** **HOLD** — oczekiwanie wyłącznie na końcowy test ręczny użytkownika
- **Commit / punkt przywracania:** `e8efc61` — implementacja Application Service i UI M3
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 402/402
- **Testy manualne / dowody:** kontrolny smoke wizualny aplikacji przy 1280×800 zielony; końcowy test ręczny wykonuje użytkownik
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M4 pozostaje zamknięty do czasu ręcznego potwierdzenia M3.

## Końcowa checklista ręczna użytkownika

- [ ] Zakładka `PLANER` otwiera się poprawnie i nie ma obciętych elementów.
- [ ] `01:60` jest odrzucane, a czas powyżej 23 godzin (np. `28:00`) jest akceptowany.
- [ ] Dla aktywnej karty S1 plan pokazuje status, wiarygodność, przyjazd, zakończenie, margines i segmenty.
- [ ] Bufor wpływa na czas zakończenia dostawy, ale nie na czas przyjazdu.
- [ ] Przełączenie S1/S2 nie aktywuje samoistnie podwójnej obsady i unieważnia poprzedni wynik.
- [ ] Nowa telemetria lub zmiana stanu karty unieważnia poprzedni wynik.
- [ ] Ostrzeżenia oraz powody segmentów, w tym `CalendarWait`, są czytelne, gdy występują.

Po potwierdzeniu tej checklisty wynik M3 można zmienić z **HOLD** na **GO** i otworzyć M4.

## Poprawki po pierwszym smoke użytkownika

- Planer odrzucał obliczenie przy aktywnej telemetrii, ponieważ porównywał
  referencje kolejnych snapshotów zamiast ich istotnego stanu. Porównanie jest
  teraz semantyczne; nowa klatka w tej samej minucie i bez zmiany stanu prawnego
  nie unieważnia obliczenia.
- Karta Staniek miała zapisaną decyzję rozliczenia, której kandydat przestał
  pasować do aktualnej projekcji historii. Taki przypadek z jednym bieżącym
  kandydatem jest teraz ponownie oznaczany jako wymagający wyboru i pojawia się
  w panelu rozliczenia zamiast znikać z widoku.
- Obie poprawki mają testy regresyjne. Pełny gate po poprawkach: 402/402,
  build Release 0 błędów / 0 ostrzeżeń.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
