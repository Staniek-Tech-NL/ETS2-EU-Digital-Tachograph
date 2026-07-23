# PLAN NAPRAWCZY — ETS2 EU Digital Tachograph 0.1.0-beta.11.1

## [Status]

**Aktualizacja bieżąca:** beta.11.1 pozostaje bazą wydaniową i nie została
zastąpiona nową paczką. Po wykonaniu tego planu wdrożono lokalnie osobny zakres
UI: wariant B wpisu manualnego, katalog ISO oraz `ODP. TYG.`. Bieżący gate
Release wynosi 310/310, build 0/0. Zakres planu naprawczego poniżej pozostaje
zamknięty; nie należy ponownie traktować go jako listy niewykonanych zadań.

**Wynik wykonania — 23 lipca 2026:** plan został wdrożony. Wydanie
`0.1.0-beta.11.1` ma 282/282 zielone testy, build Release 0/0, migrację i
restart sprawdzone na kopii właściwej bazy oraz gotowy artefakt self-contained.
Pozostał osobisty smoke test z aktywną telemetrią i decyzja GO/FIX/HOLD.

Kandydat `0.1.0-beta.11` został wycofany przed smoke testem z powodu dwóch potwierdzonych błędów:

1. nieprawidłowej automatycznej klasyfikacji bloku 24 h+ używanego do spłaty rekompensaty;
2. niezależnego przetwarzania wspólnego skoku czasu przez obie karty podwójnej obsady, powodującego fałszywe luki `ForwardTimeJump`.

Obie poprawki weszły do jednego wydania `0.1.0-beta.11.1`, pozostając osobnymi zmianami technicznymi, testami i commitami.

Obie poprawki zostały zrealizowane jako osobne commity:

- `fedc10b` — wspólna klasyfikacja skoku czasu załogi;
- `2d2b703` — audytowana decyzja alokacji odpoczynku;
- `345ac16`, `1cda06e`, `55e5ef5` — Application/SQLite, UI/raporty i testy.

Wartości `262/262`, `55/55` i `48/48` występujące niżej są celowo zachowanym
baseline’em sprzed implementacji, nie bieżącym stanem projektu.

---

## [Analiza]

### FIX A — ręczna alokacja bloku odpoczynku i rekompensaty

Obecny RuleEngine wybiera `HostMinimumMinutes` na podstawie automatycznej klasyfikacji całego bloku:

- `540 min` dla odpoczynku dobowego;
- `1440 min` dla skróconego tygodniowego;
- `2700 min` dla regularnego tygodniowego.

Specyfikacja uzależnia jednak podstawę od roli, w której blok jest używany. Spłata nadal musi być pełna, en bloc i pochodzić z jednego ciągłego bloku.

Dla zakończonego bloku 24 h+ system ma generować dopuszczalne warianty, a użytkownik wybiera sposób alokacji minut.

#### Staniek — blok 29:53

| Wariant | Wynik |
|---|---|
| `DailyRestWithCompensation` | `09:00 + 20:53`; stary dług spłacony; brak nowego długu; odpoczynek tygodniowy niezaliczony |
| `ReducedWeeklyRestOnly` | cały blok `29:53`; stary dług `20:53` pozostaje; powstaje nowy dług `15:07` |

Nie istnieje wariant `DailyRestOnly` dla zakończonego bloku 24 h+.

#### Doboś — blok 28:52

| Wariant | Wynik |
|---|---|
| `DailyRestWithCompensation` | `09:00 + 19:52`; stary dług spłacony; brak nowego długu |
| `ReducedWeeklyRestOnly` | cały blok `28:52`; stary dług `19:52` pozostaje; powstaje nowy dług `16:08` |

Użytkownik nie może wpisać własnego podziału. Wybiera wyłącznie kandydaturę wygenerowaną i zweryfikowaną przez RuleEngine.

### FIX B — koordynacja skoku czasu między kartami

Potwierdzone dane referencyjne:

| Odpoczywająca karta | Fałszywa luka | Zakres | Długość |
|---|---|---|---:|
| Staniek, slot 1 | Doboś, slot 2 | Dzień 141, 15:30–15:45 | 15 min |
| Doboś, slot 2 | Staniek, slot 1 | Dzień 141, 18:56–19:15 | 19 min |

Skok czasu jest wspólnym zdarzeniem pojazdu, ale obecnie każda karta ocenia go osobno. Karta odpoczywająca dostaje rekonstrukcję, natomiast druga karta z `OtherWork` albo `Availability` otrzymuje lukę.

Docelowo decyzja o charakterze skoku musi powstać raz w `CrewTachographEngine`, zanim oba procesory historii przetworzą ramkę.

---

## [Kolejne kroki]

### Etap 0 — zamrożenie i zabezpieczenie dowodów

1. Oznaczyć beta.11 jako:

   > Wycofany kandydat — błędna alokacja rekompensaty oraz fałszywe luki załogi przy skokach czasu.

2. Nie wykonywać końcowego smoke testu beta.11.
3. Nie rozliczać ani nie usuwać dwóch luk z Dnia 141.
4. Zachować kopię i hash bazy zawierającej te luki.
5. Potwierdzić bazowy stan:
   - `262/262` testy;
   - RuleEngine `55/55`;
   - Infrastructure `48/48`;
   - build 0 błędów i 0 ostrzeżeń.

---

### Etap 1 — aktualizacja kontraktów domenowych

#### 1A. Decyzja o alokacji odpoczynku

Dodać kontrakty zbliżone do:

```csharp
public enum RestAllocationPurpose
{
    DailyRestWithCompensation,
    ReducedWeeklyRestOnly,
    ReducedWeeklyRestWithCompensation,
    RegularWeeklyRestOnly,
    RegularWeeklyRestWithCompensation
}

public sealed record RestAllocationCandidate(
    string CandidateId,
    string RestBlockId,
    RestAllocationPurpose Purpose,
    int HostMinimumMinutes,
    IReadOnlyList<string> ObligationIds,
    int NewDebtMinutes,
    bool SatisfiesWeeklyRestRequirement);

public sealed record RestAllocationDecision(
    Guid DriverCardId,
    string RestBlockId,
    string CandidateId,
    long EffectiveAtGameMinute,
    DateTimeOffset DecidedAtUtc,
    int DecisionSchemeVersion);
```

Zasady:

- kandydatury wylicza RuleEngine;
- użytkownik wybiera kandydaturę, nie minuty;
- te same minuty nie mogą być jednocześnie częścią odpoczynku bazowego i rekompensatą;
- blok 24 h+ bez przypisania rekompensaty zachowuje konsekwencje odpoczynku tygodniowego;
- `DailyRestOnly` nie jest dostępne dla zakończonego bloku 24 h+;
- zmiana `RestBlockId` unieważnia wcześniejszą decyzję;
- zmieniona decyzja pozostawia poprzednią wersję jako `Superseded`.

Historia minutowa pozostaje źródłem faktycznego przebiegu czasu. `RestAllocationDecision` jest audytowaną deklaracją sposobu prawnego wykorzystania tego czasu — nie zmienia ani nie przepisuje `ActivityRecord`.

#### 1B. Wspólny kontekst skoku załogi

Dodać kontrakt zbliżony do:

```csharp
public sealed record CrewTimeJumpResolution(
    long StartGameMinute,
    long EndGameMinuteExclusive,
    bool VehicleStationaryBeforeAndAfter,
    bool ExplainedByCrewRest,
    IReadOnlyDictionary<int, ActivityType?> ReconstructedActivities);
```

`CrewTachographEngine`:

1. wykrywa wspólny skok;
2. ocenia kontekst pojazdu;
3. wyznacza aktywność każdej karty;
4. przekazuje tę samą decyzję obu procesorom historii.

Rekonstrukcja drugiej karty jest dozwolona tylko wtedy, gdy jej własna aktywność przed i po skoku jest identyczna i należy do:

- `BreakOrRest`;
- `OtherWork`;
- `Availability`.

Nie rekonstruować:

- `Driving`;
- karty wyjętej;
- aktywności zmienionej przez skok;
- pustego slotu;
- skoku po cofnięciu czasu lub zmianie świata.

---

### Etap 2 — czerwone testy

#### 2A. Rekompensaty

Minimalny zestaw:

1. Staniek `29:53`:
   - kandydatura `09:00 + 20:53`;
   - wybór spłaca stary dług i nie tworzy nowego;
   - wybór tygodniowy pozostawia stary dług i tworzy `15:07`.

2. Doboś `28:52`:
   - analogicznie `19:52`;
   - wariant tygodniowy tworzy `16:08`.

3. O minutę za mało:
   - brak kandydatury spłacającej;
   - brak częściowego zmniejszenia długu.

4. `44:53`:
   - poprawne rozdzielenie wariantów;
   - zakaz podwójnego użycia minut.

5. `65:53` Stanka:
   - `45:00 + 20:53`;
   - regularny tygodniowy i pełna spłata;
   - brak nowego długu.

6. Brak decyzji:
   - stan `PendingRestAllocation`;
   - widoczna prognoza konsekwencji wariantu tygodniowego;
   - Planer i pełna ocena raportowa oznaczone jako niewiarygodne do czasu decyzji.

7. Restart SQLite:
   - identyczna decyzja, kandydatura, zobowiązania i zakresy spłaty.

8. Zmiana historii:
   - nowy `RestBlockId`;
   - stara decyzja nie uczestniczy w obliczeniu.

Dotychczasowe testy en bloc, FIFO, terminu i restartu muszą pozostać zielone.

#### 2B. Skoki czasu załogi

1. `CREW-JUMP-01`  
   Dokładny zakres Dnia 141, 15:30–15:45:
   - Staniek S1: odpoczynek;
   - Doboś S2: stabilna praca albo dyspozycyjność;
   - brak `ForwardTimeJump` dla obu kart.

2. `CREW-JUMP-02`  
   Dokładny zakres Dnia 141, 18:56–19:15:
   - Doboś S2: odpoczynek;
   - Staniek S1: stabilna aktywność;
   - brak `ForwardTimeJump`.

3. Żadna karta nie odpoczywa:
   - zachować dotychczasową bezpieczną politykę luk.

4. Druga karta ma `Driving`:
   - nie rekonstruować Jazdy.

5. Aktywność drugiej karty różni się przed i po skoku:
   - utworzyć `ForwardTimeJump`.

6. Druga karta jest wyjęta:
   - zachować `CardRemoved`;
   - nie tworzyć konkurencyjnego `ForwardTimeJump`.

7. Zamiana kolejności przetwarzania slotów:
   - wynik identyczny;
   - brak zależności S1 → S2 lub S2 → S1.

---

### Etap 3 — implementacja

#### Ścieżka A — RuleEngine i decyzje rekompensaty

1. Generowanie `RestAllocationCandidate`.
2. Walidacja wyboru.
3. Wyliczenie zobowiązań z uwzględnieniem decyzji.
4. Trwałe zapisanie decyzji w SQLite.
5. Unieważnienie decyzji po zmianie kanonicznego bloku.
6. Pełny audyt zmiany i `Superseded`.

#### Ścieżka B — Engine i wspólne skoki

1. Przenieść klasyfikację wspólnego skoku do `CrewTachographEngine`.
2. Przekazać gotowy kontekst do obu `ActivityHistoryProcessor`.
3. Zachować własną stabilną aktywność każdej karty.
4. Nie zmieniać zachowania:
   - cargo;
   - cofnięć czasu;
   - `world_generation`;
   - małych skoków;
   - `CardRemoved`;
   - pojedynczej obsady.

---

### Etap 4 — Application, UI i raporty

#### UI decyzji odpoczynku

Po zakończeniu niejednoznacznego bloku UI pokazuje:

```text
WYBIERZ SPOSÓB ROZLICZENIA ODPOCZYNKU

DOBOWY + REKOMPENSATA
09:00 + 20:53
Stary dług: spłacony
Nowy dług: brak
Odpoczynek tygodniowy: niezaliczony

SKRÓCONY TYGODNIOWY
29:53
Stary dług: pozostaje 20:53
Nowy dług: 15:07
```

UI nie zawiera własnych obliczeń — prezentuje kandydatury RuleEngine.

#### Raporty

PDF, CSV i JSON powinny zawierać:

- `RestBlockId`;
- wybraną rolę bloku;
- `CandidateId`;
- czas decyzji;
- przypisane zobowiązania;
- podstawę `540/1440/2700`;
- zakresy rekompensaty;
- nowy dług, jeżeli powstał;
- informację o zmianie lub unieważnieniu decyzji.

---

### Etap 5 — korekta dwóch luk referencyjnych

Dopiero po przejściu czerwonych testów:

1. uruchomić nowy algorytm na kopii bazy;
2. potwierdzić, że oba zakresy są rekonstruowane bez luk;
3. nie usuwać rekordów SQL ręcznie;
4. wykonać audytowaną korektę danych:
   - uzupełnić właściwą aktywność;
   - oznaczyć pierwotną lukę jako rozliczoną;
   - zachować przyczynę korekty, np. `AutomaticCrewReconstruction`;
5. ponownie uruchomić aplikację i porównać historię obu kart.

---

### Etap 6 — proponowane commity

1. `docs(domain): opisz ręczną alokację odpoczynku i wspólne skoki załogi`
2. `fix(engine): koordynuj długie skoki czasu między kartami`
3. `feat(compensation): dodaj audytowaną decyzję alokacji bloku odpoczynku`
4. `feat(application): zapisz i mapuj decyzje alokacji`
5. `feat(ui-reports): pokaż warianty i pełny ślad decyzji`
6. `test(infrastructure): zweryfikuj restart i unieważnianie decyzji`
7. `chore(release): przygotuj 0.1.0-beta.11.1`

Po każdym commicie: build czysty i pełny pakiet testów zielony.

---

### Etap 7 — gate beta.11.1

Warunki wejścia do smoke testu:

- wszystkie dotychczasowe `262` testy nadal zielone;
- wszystkie nowe regresje zielone;
- build Release 0 błędów i 0 ostrzeżeń;
- restart SQLite zachowuje decyzje i pełny ślad;
- brak fałszywych luk w obu scenariuszach Dnia 141;
- brak podwójnego użycia minut rekompensaty;
- PDF, CSV, JSON i UI pokazują identyczne konsekwencje;
- migracja sprawdzona na kopii właściwej bazy;
- czyste drzewo;
- nowy ZIP i nowy SHA-256.

Końcowy smoke wykonuje się wyłącznie na artefakcie `0.1.0-beta.11.1`.

---

## [Blokery / Pytania]

Brak blokera decyzyjnego.

Przyjęte zasady:

- użytkownik wybiera jedną z kandydatur RuleEngine;
- nie istnieje `DailyRestOnly` dla zamkniętego bloku 24 h+;
- skok czasu jest najpierw klasyfikowany wspólnie dla pojazdu;
- aktywność drugiej karty pochodzi wyłącznie z jej własnego stabilnego stanu;
- obie poprawki muszą przejść wspólny gate — nie wydajemy beta.11.1 z tylko jednym FIX-em;
- Planer podróży pozostaje wstrzymany do formalnego GO.
