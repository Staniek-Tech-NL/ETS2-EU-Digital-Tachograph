# M3 — Planer: Application Service i UI

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status bieżący:** **REDESIGN — M3-R1 CZERWONY GATE GOTOWY, M3-R2 ODBLOKOWANE**
**Kryterium wejścia:** `M2-CREW GO AND M3A GO` — spełnione.
**Kryterium wyjścia:** Kompletny przepływ użytkownika Planera, zgodność z RuleEngine i poprawne unieważnianie wyniku.  
**Następny etap:** M4

> Ten dokument jest samodzielnym wydzieleniem etapu M3 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

## Decyzja produktowa M3-R0

**M3-R0 zatwierdzone przez użytkownika 2026-07-24.**

Stary prototyp M3 zostaje odrzucony produktowo. Nie wolno go dalej rozwijać
przez dokładanie kolejnych pól lub łatek do modelu opartego na jednym
`RemainingDriveMinutes`. Poniższa historyczna implementacja może być użyta
wyłącznie jako źródło elementów technicznych nadających się do ponownego użycia.

Nowe M3 obsługuje dwa jawne przypadki użycia:

```text
MarketOffer
ActiveDelivery
```

`MarketOffer` przyjmuje dane rynku ETS2:

- czas dojazdu po ładunek;
- osobny termin wygaśnięcia oferty;
- czas trasy z ładunkiem;
- początek i koniec okna dostawy;
- czas odbioru;
- czas rozładunku;
- aktywną obsadę.

`ActiveDelivery` zachowuje uproszczony scenariusz przyjętego zlecenia, bez
udawania danych oferty rynkowej.

Wynik odpowiada na pytanie, czy ofertę można legalnie odebrać i dostarczyć,
kiedy nastąpi odbiór i dostawa oraz jaki pozostaje zapas. Klasyfikacja
produktowa wyniku:

```text
BIERZ
NA STYK
NIE BIERZ
```

### Wzorzec UI nowego M3

Normatywną referencją wizualną jest:

[`ChatGPT Image 24 lip 2026, 17_15_10.png`](../images/ChatGPT%20Image%2024%20lip%202026%2C%2017_15_10.png)

Z makiety przyjmujemy:

- układ strony `PLANER PODRÓŻY` z formularzem u góry;
- pas podsumowania z czasem gry, wygaśnięciem oferty, oknem dostawy,
  najwcześniejszym przyjazdem, końcem dostawy i marginesem;
- tabelę segmentów planu z czasami, powodem i aktywnością;
- prawy panel ostrzeżeń, ograniczeń i podsumowania;
- czytelne rozróżnienie jazdy, innej pracy, oczekiwania i odpoczynku;
- prezentację terminów przez kalendarz M3A;
- wspólny harmonogram pojazdu oraz aktywności S1/S2 z M2-CREW.

Makieta jest wzorcem hierarchii informacji i wyglądu, nie kontraktem starego
formularza. Pola wejściowe muszą wynikać z `MarketOffer` albo `ActiveDelivery`.
UI powstaje po kontraktach, czerwonych testach M3-R1 i nowym Application Service.

## M3-R1 — blokujące testy przed implementacją

```text
M3-P0-01  brak ręcznego liczenia czasu do dostawy
M3-P0-02  osobny deadline wygaśnięcia oferty
M3-P0-03  osobne okno dostawy od/do
M3-P0-04  dojazd po ładunek uwzględniony przed odbiorem
M3-P0-05  harmonogram S1/S2 wykorzystuje M2-CREW
M3-P0-06  przerwa 45 min w ruchu nie zatrzymuje pojazdu
M3-P0-07  wynik rozróżnia: BIERZ / NA STYK / NIE BIERZ
M3-P0-08  kalendarz korzysta z M3A i offsetu snapshotu
```

Stan M3-R1:

- [x] kontrakty `MarketOffer` i `ActiveDelivery`;
- [x] jawne fazy planu i klasyfikacja `BIERZ / NA STYK / NIE BIERZ`;
- [x] testy `M3-P0-01…08` istnieją i kompilują się;
- [x] 9/9 wykonań testów jest kontrolowanie czerwonych
  (`M3-P0-08` obejmuje offsety `-1` i `+1`);
- [x] dotychczasowy pakiet RuleEngine pozostaje zielony 157/157;
- [x] pełna dotychczasowa regresja, z wyłączeniem celowo czerwonego pakietu,
  pozostaje zielona 478/478.

**M3-R2 jest odblokowane. Następny krok: zazielenienie M3-P0-01…08, następnie
nowy Application Service.**

---

## Historyczny, odrzucony wariant M3

**Cel historyczny:** udostępnić Planer jako kompletny przepływ użytkownika.

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
- [ ] atomowy snapshot zawiera obie karty aktywnej załogi;
- [ ] UI prezentuje wspólną oś pojazdu i równoległe aktywności S1/S2;
- [ ] Planer wykorzystuje przyszłe zmiany prowadzącego i przerwy w ruchu.

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
prawdziwego `RegulationState`. Dla aktywnej załogi musi planować wspólną trasę
obu kart, przyszłe zmiany prowadzącego i kwalifikowane przerwy w ruchu.

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

Przyszłe zmiany kierowców nie są już poza zakresem — są warunkiem ponownego
otwarcia gate’u M3.

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
- Pierwsza diagnoza karty Staniek była błędna: rekompensata `20:53` została
  wykonana, a zapisana decyzja wskazuje prawidłowego kandydata pełnej spłaty.
  Problem powodował zoptymalizowany odczyt historii, który ponownie stosował
  granicę pustej starszej sesji i usuwał źródłowy skrócony odpoczynek tygodniowy
  z projekcji warm.
- Odczyt historii stosuje teraz historyczne granice sesji wyłącznie do gorącego
  ogona od granicy retencji. Wykonana rekompensata pozostaje rozliczona i nie
  wymaga ponownego wyboru.
- Poprawki Planera i retencji historii mają testy regresyjne. Pełny gate po
  poprawkach: 402/402, build Release 0 błędów / 0 ostrzeżeń.
- **Commit poprawki Planera po smoke:** `95f9777`.
- **Commit naprawy wykonanej rekompensaty:** `af97b39`.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
