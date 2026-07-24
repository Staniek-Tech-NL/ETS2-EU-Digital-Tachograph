# Plan wdrożenia — Planer podróży MVP

**Projekt:** ETS2 EU Digital Tachograph  
**Funkcja:** Planer podróży — strategia „Najwcześniejsza legalna”  
**Wersja dokumentu:** 2.2 — aktualizacja po beta.11.1 i lokalnych zmianach UI
**Status specyfikacji:** **ZATWIERDZONA DLA BETA.12 (M1 GO 2026-07-24)**
**Status implementacji:** **M2 ZAKOŃCZONY — SILNIK ZDARZENIOWY GOTOWY**
**Gate przed implementacją:** M0 zakończony formalnym GO; decyzja właściciela
o rozpoczęciu Planera została wydana 2026-07-24. Kontrakty i czerwone testy M1
są gotowe, a implementacja algorytmu M2 otrzymała GO 2026-07-24.
**Dokument przeglądu:** `JOURNEY_PLANNER_MVP_REVIEW_REPORT.md`  
**Raport terenowy:** `FIELD_TEST_REPORT_2026-07-21.md`

---

## 1. Status wykonawczy

### 1.1 Stan bieżący

Koncepcja Planera podróży została zaakceptowana. MVP będzie obsługiwał jedną strategię:

> **Najwcześniejsza legalna**

Wersja 2.2 została zatwierdzona 2026-07-24 jako specyfikacja implementacyjna
beta.12. Przy materializacji kontraktów doprecyzowano dwie niespójności:

- pole `RegulatoryActivity` używa istniejącego domenowego typu `DriverActivity`
  zamiast nieistniejącego `ActivityType`;
- snapshot zawiera jawne `MultiManningActive`, ponieważ wybór S1/S2 nie może
  sam aktywować okna 30 h;
- snapshot i jego tożsamość zawierają `WeekEpochOffsetDays`, aby granice 56/90 h
  były identyczne z granicami używanymi przez RuleEngine.

Po przeglądzie pierwotnego planu zaktualizowano specyfikację w zakresie:

- limitów jazdy tygodniowej 56 h i dwutygodniowej 90 h,
- oczekiwania na zmianę okresu regulacyjnego,
- terminów ukończenia odpoczynku dobowego,
- semantyki bufora operacyjnego,
- ograniczeń modelu rekompensat tygodniowych,
- atomowego snapshotu wejściowego,
- statusów wyniku,
- ważności planu,
- gwarancji zakończenia algorytmu,
- ograniczonego rozgałęziania wariantów,
- podwójnej obsady,
- testów blokujących P0.

### 1.2 Ważne rozróżnienie

Rozszerzone testy terenowe reguły ciągłości odpoczynku z beta.10 zostały rozpoczęte.

> **Dzień 2 zakończono wynikiem zielonym.**

Potwierdzono dla obu kart:

- ciągłość odpoczynku przez pojedynczą rozliczoną lukę `CardRemoved`,
- przyznanie resetu dobowego na końcu połączonego bloku,
- zachowanie `SourceGapId` jako śladu audytowego,
- stabilność wyniku po restarcie aplikacji,
- poprawne przerwanie odpoczynku kierowcy w slocie 2 podczas jazdy pojazdu.

Gate beta.10 został zastąpiony późniejszą bazą beta.11.1. Scenariusze granicy
tygodnia i rekompensaty są pokryte późniejszym modelem, testami RuleEngine oraz
dokumentacją rekompensat. Obserwacje dotyczące bloków z wieloma rozliczonymi
lukami pozostają osobną decyzją domenową.

Artefakt beta.11.1 przeszedł osobisty smoke z aktywną telemetrią 23 lipca 2026
i otrzymał decyzję **GO**. Poniższy warunek smoke bieżącego drzewa pozostaje
osobnym zabezpieczeniem, ponieważ lokalne zmiany UI powstały już po zamrożeniu
artefaktu beta.11.1.

Planer nie przechodzi do implementacji przed:

1. osobistym smoke bieżącego drzewa z aktywną telemetrią,
2. rozliczeniem ewentualnych problemów,
3. osobną decyzją właściciela projektu o rozpoczęciu Planera,
4. aktualizacją dokumentacji aktywnego zakresu,
5. potwierdzeniem kontrolowanego stanu repozytorium.

Nie oznacza to, że Planer jest anulowany. Jego specyfikacja może zostać
domknięta wcześniej, ale kodowanie pozostaje za osobną decyzją projektową.

---

## 2. Cel funkcji

Planer ma wyznaczać:

- **najwcześniejszy legalny czas przyjazdu do celu**,
- **najwcześniejszy legalny czas zakończenia dostawy**,
- zapas czasu albo minimalne opóźnienie względem terminu,
- chronologiczny harmonogram jazdy, przerw, odpoczynków, innej pracy i oczekiwania kalendarzowego.

Plan musi być wyliczany na podstawie:

- aktualnego stanu kierowcy,
- historii aktywności,
- istniejącego modelu regulacyjnego,
- pozostałego czasu samej jazdy z GPS,
- czasu pozostałego do terminu dostawy,
- bufora operacyjnego po przyjeździe.

Planer nie jest drugim tachografem i nie może posiadać własnej, rozbieżnej interpretacji reguł.

---

## 3. Decyzje produktowe MVP

### 3.1 Strategia

MVP obsługuje tylko:

> **Najwcześniejsza legalna**

Nie dodajemy na tym etapie wariantów:

- konserwatywnego,
- bez skróceń,
- ekonomicznego,
- porównania kilku strategii.

### 3.2 Znaczenie terminu

Termin wprowadzony przez użytkownika oznacza:

> **czas do wymaganego zakończenia dostawy**, a nie wyłącznie dojazdu pod rampę.

UI pokazuje osobno:

- przewidywany przyjazd,
- przewidywane zakończenie dostawy.

### 3.3 Bufor operacyjny

W MVP obowiązuje jedna polityka:

```csharp
public enum JourneyOperationalBufferPolicy
{
    OtherWorkAfterArrival
}
```

Bufor:

- występuje po ostatnim segmencie jazdy,
- reprezentuje załadunek, rozładunek, tankowanie lub inne czynności operacyjne po przyjeździe,
- jest klasyfikowany jako `OtherWork`,
- zwiększa czas zakończenia dostawy,
- wpływa na okno odpoczynku dobowego,
- nie zeruje jazdy ciągłej,
- nie zeruje jazdy dziennej,
- nie jest traktowany jako odpoczynek ani dyspozycyjność.

### 3.4 Skrócony odpoczynek tygodniowy

Odpoczynek tygodniowy 24 h może być wybrany tylko wtedy, gdy aktualny `RuleEngine` potrafi jednoznacznie potwierdzić:

- jego dopuszczalność,
- powstające zobowiązanie,
- zgodność z obsługiwanym modelem rekompensat.

Jeżeli model nie daje takiej pewności:

- planer wybiera odpoczynek 45 h,
- albo zwraca wynik z ograniczoną wiarygodnością, jeżeli pełny plan nie może zostać potwierdzony.

### 3.5 Podwójna obsada

W MVP:

> Wszystkie przyszłe segmenty `Drive` wykonuje wybrana karta.

Planer:

- nie symuluje przyszłych zmian kierowców,
- nie przenosi prowadzenia na drugą kartę,
- nie planuje odpoczynku drugiego kierowcy podczas jazdy pierwszego,
- używa okna 30 h tylko wtedy, gdy bieżący snapshot potwierdza aktywny stan podwójnej obsady dla wybranej karty.

Sam wybór S1 lub S2 w UI nie aktywuje trybu załogi.

---

## 4. Zasady nadrzędne

1. Wszystkie obliczenia używają `game_time`.
2. Zegar systemowy nie uczestniczy w planowaniu.
3. Historia aktywności pozostaje jedynym źródłem prawdy.
4. Dane wejściowe pochodzą z jednego atomowego snapshotu.
5. Planowanie jest symulacją tylko do odczytu.
6. Planer nie zapisuje hipotetycznych aktywności do SQLite.
7. Planer nie modyfikuje prawdziwego `RegulationState`.
8. Ten sam snapshot i te same dane wejściowe muszą zwrócić identyczny wynik.
9. Plan musi być oceniany przez ten sam model reguł co Dashboard.
10. UI i ViewModel nie implementują reguł tachografu.
11. Każdy naprawiony błąd otrzymuje dokładny test regresyjny.
12. Algorytm musi gwarantować postęp albo kontrolowane zakończenie.
13. Ograniczenia obecnego modelu muszą być widoczne w wyniku.
14. Prototyp HTML jest wyłącznie inspiracją wizualną.

---

## 5. Zakres MVP

### 5.1 Dane wprowadzane przez użytkownika

| Pole | Format | Wymagane | Znaczenie |
|---|---|---:|---|
| Pozostały czas jazdy z GPS | czas trwania `HH:MM` | Tak | Wyłącznie czas prowadzenia pojazdu |
| Czas do zakończenia dostawy | czas trwania `HH:MM` | Tak | Dostępne minuty od snapshotu do terminu |
| Bufor operacyjny | czas trwania `HH:MM` | Nie | `OtherWorkAfterArrival` |
| Karta | S1 / S2 | Tak | Kierowca planowany przez MVP |

### 5.2 Reguły formatu czasu

- godziny mogą być większe niż 23,
- minuty muszą należeć do zakresu `00–59`,
- wartości ujemne są zabronione,
- `00:00` pozostałej jazdy jest poprawne,
- `00:00` bufora nie dodaje segmentu,
- wszystkie obliczenia odbywają się w pełnych minutach,
- maksymalny horyzont określają `JourneyPlanningLimits`.

Proponowany typ wejściowy:

```csharp
public readonly record struct DurationInput(int TotalMinutes);
```

### 5.3 Dane pobierane automatycznie

Atomowy snapshot dostarcza:

- czas jazdy ciągłej,
- zaliczony pierwszy segment przerwy dzielonej,
- wykorzystaną jazdę dzienną,
- dostępność wydłużenia do 10 h,
- liczbę skróconych odpoczynków dobowych,
- jazdę tygodniową,
- jazdę dwutygodniową,
- moment rozpoczęcia bieżącego okna dobowego,
- termin ukończenia odpoczynku w oknie 24/30 h,
- stan odpoczynku tygodniowego,
- obsługiwane zobowiązania rekompensacyjne,
- tryb pojedynczej lub podwójnej obsady,
- historię i jej high-water mark,
- bieżącą sesję aktywności,
- `world_generation`,
- nierozliczone luki,
- dostępność telemetrii.

---

## 6. Wynik planowania

### 6.1 Status

`bool IsFeasible` nie jest używany.

```csharp
public enum JourneyPlanStatus
{
    MeetsDeadline,
    MissesDeadline,
    BlockedByGap,
    InsufficientData,
    StaleSnapshot,
    UnsupportedScenario,
    NoLegalContinuation,
    CalculationLimitReached
}
```

Interpretacja:

| Status | Znaczenie |
|---|---|
| `MeetsDeadline` | Istnieje plan mieszczący się w terminie |
| `MissesDeadline` | Istnieje legalny plan, ale kończy się po terminie |
| `BlockedByGap` | Nierozliczona luka blokuje wiarygodne planowanie |
| `InsufficientData` | Brakuje danych do zbudowania stanu |
| `StaleSnapshot` | Snapshot przestał odpowiadać bieżącej historii |
| `UnsupportedScenario` | Przypadek wymaga funkcji spoza MVP |
| `NoLegalContinuation` | W horyzoncie nie istnieje legalna kontynuacja |
| `CalculationLimitReached` | Zadziałał bezpiecznik obliczeń |

### 6.2 Poziom wiarygodności

```csharp
public enum JourneyPlanConfidence
{
    VerifiedByCurrentRuleModel,
    LimitedByCompensationModel,
    BasedOnIncompleteHistory,
    BasedOnLastSavedState
}
```

Status i poziom wiarygodności są niezależne. Przykład:

- `MissesDeadline`
- `VerifiedByCurrentRuleModel`

oznacza legalny, potwierdzony harmonogram, który po prostu nie mieści się w terminie.

### 6.3 Wymagane pola wyniku

```csharp
public sealed record JourneyPlanResult(
    JourneyPlanStatus Status,
    JourneyPlanConfidence Confidence,
    long StartGameMinute,
    long? EarliestArrivalGameMinute,
    long? EarliestCompletionGameMinute,
    int RequiredElapsedMinutes,
    int MarginMinutes,
    IReadOnlyList<JourneyPlanSegment> Segments,
    IReadOnlyList<JourneyPlanWarning> Warnings,
    JourneyPlanUsageSummary Usage,
    JourneyPlanSnapshotIdentity SnapshotIdentity);
```

`MarginMinutes`:

- dodatnie — zapas,
- zero — dokładnie na termin,
- ujemne — minimalne opóźnienie.

---

## 7. Atomowy snapshot

### 7.1 Kontrakt

```csharp
public sealed record JourneyPlanningSnapshot(
    int DriverSlot,
    long StartGameMinute,
    Guid ActivitySessionId,
    long WorldGeneration,
    long HistoryHighWaterMark,
    RegulationEvaluation Evaluation,
    IReadOnlyList<ActivityRecord> History,
    IReadOnlyList<ActivityGap> Gaps,
    int WeekEpochOffsetDays,
    bool MultiManningActive,
    bool TelemetryAvailable);
```

### 7.2 Zasada spójności

Wszystkie pola muszą pochodzić z jednego logicznego momentu aplikacji.

Nie wolno pobierać osobno:

- historii,
- `RegulationEvaluation`,
- luk,
- czasu startowego,

jeżeli pomiędzy odczytami telemetria może zmienić stan.

### 7.3 Unieważnienie planu

Wynik jest nieaktualny, gdy:

- zmienił się `ActivitySessionId`,
- zmienił się `world_generation`,
- nastąpiło cofnięcie `game_time`,
- zmienił się `HistoryHighWaterMark`,
- historia została przycięta lub przebudowana,
- użytkownik wybrał inną kartę,
- rozpoczęła się nowa jazda po utworzeniu planu.

UI nie aktualizuje automatycznie starego wyniku. Pokazuje komunikat:

```text
Stan kierowcy zmienił się. Oblicz plan ponownie.
```

---

## 8. Segmenty planu

### 8.1 Typy segmentów

```csharp
public enum JourneyPlanSegmentType
{
    Drive,
    Break,
    DailyRest,
    WeeklyRest,
    CalendarWait,
    OtherWork,
    Availability
}
```

Bufor nie jest osobnym regulacyjnym typem aktywności. W wyniku jest segmentem:

```text
Type = OtherWork
Reason = OperationalBufferAfterArrival
```

### 8.2 Powody segmentów

```csharp
public enum JourneyPlanSegmentReason
{
    RemainingRouteDrive,
    ContinuousDrivingBreak,
    SplitBreakCompletion,
    DailyRestDeadline,
    DailyDrivingLimit,
    WeeklyRestRequirement,
    WeeklyDrivingLimitReached,
    BiweeklyDrivingLimitReached,
    WaitForNewRegulatoryWeek,
    WaitForBiweeklyCapacity,
    OperationalBufferAfterArrival
}
```

### 8.3 Kontrakt segmentu

```csharp
public sealed record JourneyPlanSegment(
    JourneyPlanSegmentType Type,
    int DriverSlot,
    long StartGameMinute,
    long EndGameMinute,
    int DurationMinutes,
    JourneyPlanSegmentReason Reason,
    DriverActivity RegulatoryActivity,
    bool UsesRegulatoryException,
    string? WarningCode);
```

### 8.4 `CalendarWait`

`CalendarWait` oznacza, że jazda jest niemożliwa z powodu limitu okresowego.

Segment musi również zawierać regulacyjną klasyfikację aktywności, najczęściej `BreakRest`.

Dzięki temu:

- czas oczekiwania może równocześnie spełniać warunki odpoczynku,
- odpoczynek i oczekiwanie nie są naliczane podwójnie,
- harmonogram pokazuje zarówno czynność, jak i przyczynę oczekiwania.

Przykład:

```text
CalendarWait
RegulatoryActivity: BreakRest
Reason: WaitForNewRegulatoryWeek
Duration: 30:00
```

---

## 9. Termin odpoczynku dobowego

### 9.1 Definicja

Koniec okna 24/30 h jest:

> terminem ukończenia kwalifikującego odpoczynku dobowego.

Nie jest to wyłącznie termin rozpoczęcia odpoczynku.

### 9.2 Model

```csharp
public sealed record DailyRestPlanningWindow(
    long CompletionDeadlineGameMinute,
    long LatestRegularRestStartGameMinute,
    long? LatestReducedRestStartGameMinute);
```

Obliczenia:

```text
LatestRegularRestStartGameMinute
= CompletionDeadlineGameMinute - 11 h

LatestReducedRestStartGameMinute
= CompletionDeadlineGameMinute - 9 h
```

Wariant 9 h jest dostępny tylko wtedy, gdy stan kierowcy na to pozwala.

### 9.3 Wpływ na planowanie

Maksymalny blok jazdy lub innej pracy musi zostać skrócony tak, aby wybrany odpoczynek:

- rozpoczął się najpóźniej w dopuszczalnym momencie,
- zakończył się przed granicą 24/30 h.

Decyzja 9 h lub 11 h musi być podjęta przed wyliczeniem poprzedzającego segmentu.

---

## 10. Limity 56 h i 90 h

### 10.1 Zasada

Odpoczynek tygodniowy nie resetuje automatycznie:

- limitu 56 h w bieżącym tygodniu,
- limitu 90 h w dwóch kolejnych tygodniach.

### 10.2 Wymagane zdarzenia

Silnik musi znać:

- najbliższy początek nowego tygodnia regulacyjnego,
- dostępny limit jazdy po zmianie tygodnia,
- moment przesunięcia okna dwutygodniowego,
- dostępny limit po przesunięciu okna,
- możliwość pokrycia oczekiwania kwalifikującym odpoczynkiem.

### 10.3 Oczekiwanie kalendarzowe

Jeżeli limit jest wyczerpany, silnik dodaje `CalendarWait` do najbliższego momentu, w którym ponowna jazda staje się legalna.

Silnik nie może:

- dodawać kolejnych odpoczynków bez zmiany blokującego limitu,
- ruszyć bez sprawdzenia nowego dostępnego limitu,
- liczyć tego samego czasu jednocześnie jako osobny odpoczynek i osobne oczekiwanie.

---

## 11. Przerwa dzielona 15 + 30 min

W MVP:

- jeżeli snapshot potwierdza zaliczone pierwsze 15 min, planer może dodać segment 30 min,
- w przeciwnym razie planer dodaje pełne 45 min,
- planer nie rozpoczyna samodzielnie nowej strategii 15 + 30,
- optymalizacja nowego podziału przechodzi do Fazy 2.

---

## 12. Algorytm

### 12.1 Model wykonania

Planer używa:

> symulacji zdarzeniowej z ograniczonym rozgałęzieniem.

Nie używa:

- symulacji każdej minuty,
- pojedynczej zachłannej ścieżki bez porównania wariantów,
- ręcznie zakodowanych uproszczeń z prototypu HTML.

### 12.2 Punkty zdarzeń

Minimalny zestaw zdarzeń:

- koniec dostępnego czasu jazdy ciągłej,
- osiągnięcie 9 h jazdy dziennej,
- osiągnięcie 10 h jazdy dziennej,
- najpóźniejszy start odpoczynku 9 h,
- najpóźniejszy start odpoczynku 11 h,
- osiągnięcie 56 h,
- osiągnięcie 90 h,
- początek nowego tygodnia,
- zwolnienie limitu dwutygodniowego,
- ukończenie przerwy,
- ukończenie odpoczynku dobowego,
- ukończenie odpoczynku tygodniowego,
- przyjazd do celu,
- zakończenie bufora operacyjnego.

### 12.3 Punkty rozgałęzienia

Rozgałęzienie jest dozwolone wyłącznie w kontrolowanych miejscach:

- jazda dzienna 9 h / 10 h,
- odpoczynek dobowy 9 h / 11 h,
- odpoczynek tygodniowy 24 h / 45 h, o ile 24 h jest w pełni obsługiwane,
- odpoczynek teraz / dalsza legalna aktywność,
- odpoczynek pokrywający oczekiwanie kalendarzowe.

### 12.4 Ocena wariantów

Warianty są porównywane kolejno według:

1. najwcześniejszego zakończenia dostawy,
2. pełnej zgodności z bieżącym modelem,
3. wyższego poziomu wiarygodności,
4. mniejszej liczby użytych wyjątków regulacyjnych,
5. mniejszej liczby segmentów przy identycznym czasie.

Plan o niższej wiarygodności nie może pokonać planu w pełni zweryfikowanego wyłącznie minimalną różnicą czasu, jeśli jego legalność zależy od nieobsługiwanego modelu.

### 12.5 Deduplikacja stanów

Stan równoważny można deduplikować po kluczu obejmującym co najmniej:

- bieżący `game_time`,
- pozostały czas jazdy,
- jazdę ciągłą,
- jazdę dzienną,
- jazdę tygodniową,
- jazdę dwutygodniową,
- użyte wydłużenia,
- użyte skrócone odpoczynki,
- bieżące okno 24/30 h,
- stan odpoczynku tygodniowego,
- zobowiązania rekompensacyjne obsługiwane przez model,
- stan przerwy dzielonej.

---

## 13. Gwarancja postępu

Po każdej iteracji musi nastąpić co najmniej jedno:

```text
remainingDriveMinutes maleje
lub
currentGameMinute rośnie
```

Dodatkowo obowiązują:

```csharp
public sealed record JourneyPlanningLimits(
    int MaximumSegments,
    int MaximumElapsedMinutes,
    int MaximumVisitedStates);
```

Silnik musi wykrywać:

- powrót do równoważnego stanu,
- brak zdarzenia przesuwającego czas,
- zerowy legalny segment,
- przekroczenie limitu segmentów,
- przekroczenie horyzontu,
- przekroczenie liczby odwiedzonych stanów.

Wynik terminalny:

- `NoLegalContinuation`,
- albo `CalculationLimitReached`.

Każde żądanie kończy się wynikiem kontrolowanym. Pętla nieskończona jest niedopuszczalna.

---

## 14. Przepływ obliczenia

### Krok 1 — zbudowanie snapshotu

`JourneyPlannerService` pobiera atomowy snapshot dla wybranej karty.

### Krok 2 — walidacja

Odrzuć lub oznacz wynik, gdy:

- czas jazdy jest mniejszy od zera,
- czas do terminu jest mniejszy od zera,
- bufor jest mniejszy od zera,
- format czasu jest niepoprawny,
- karta nie istnieje,
- snapshot jest niespójny,
- nie można zbudować stanu regulacyjnego,
- występuje nierozliczona luka `CardRemoved`.

`00:00` czasu jazdy jest poprawne.

### Krok 3 — kompletność danych

- `CardRemoved` nierozliczona → `BlockedByGap`,
- `ForwardTimeJump` nierozliczona → plan może być policzony z `BasedOnIncompleteHistory`,
- brak telemetrii → plan może być policzony z ostatniego zapisu jako `BasedOnLastSavedState`.

### Krok 4 — utworzenie stanu roboczego

Silnik tworzy niemutowalną lub izolowaną kopię stanu.

### Krok 5 — rozwijanie zdarzeń

Silnik generuje legalne kolejne segmenty i ograniczone warianty.

### Krok 6 — zakończenie jazdy

Po wyzerowaniu `RemainingDriveMinutes` zapisywany jest:

```text
EarliestArrivalGameMinute
```

### Krok 7 — bufor

Jeżeli bufor jest większy od zera, dodawany jest:

```text
OtherWork / OperationalBufferAfterArrival
```

Bufor również musi być legalnie wpasowany w okno odpoczynku. Jeżeli jego wykonanie naruszyłoby termin odpoczynku, silnik musi rozważyć odpoczynek przed buforem lub odpowiedni wariant harmonogramu.

### Krok 8 — zakończenie dostawy

Po buforze zapisywany jest:

```text
EarliestCompletionGameMinute
```

### Krok 9 — termin

```text
MarginMinutes
= DeliveryWindowMinutes - RequiredElapsedMinutes
```

- `MarginMinutes >= 0` → `MeetsDeadline`,
- `MarginMinutes < 0` → `MissesDeadline`.

---

## 15. Kontrakty

### 15.1 `JourneyPlanRequest`

```csharp
public sealed record JourneyPlanRequest(
    JourneyPlanningSnapshot Snapshot,
    int RemainingDriveMinutes,
    int DeliveryWindowMinutes,
    int OperationalBufferMinutes,
    JourneyOperationalBufferPolicy BufferPolicy,
    JourneyPlanningLimits Limits);
```

### 15.2 `JourneyPlanSnapshotIdentity`

```csharp
public sealed record JourneyPlanSnapshotIdentity(
    int DriverSlot,
    long StartGameMinute,
    Guid ActivitySessionId,
    long WorldGeneration,
    long HistoryHighWaterMark,
    int WeekEpochOffsetDays);
```

### 15.3 `JourneyPlanUsageSummary`

Powinien zawierać co najmniej:

- użycie wydłużenia jazdy dziennej,
- użycie skróconego odpoczynku dobowego,
- użycie odpoczynku tygodniowego 24 h lub 45 h,
- powstałe zobowiązanie rozpoznawane przez model,
- wykorzystanie istniejącego pierwszego segmentu przerwy 15 min,
- zastosowanie okna 30 h,
- wystąpienie `CalendarWait`,
- ograniczenie przez 56 h,
- ograniczenie przez 90 h.

---

## 16. Architektura

```text
ETS2Tachograph.RuleEngine
└── JourneyPlanning/
    ├── JourneyPlanningEngine.cs
    ├── JourneyPlanningSnapshot.cs
    ├── JourneyPlanningState.cs
    ├── JourneyPlanningLimits.cs
    ├── JourneyPlanRequest.cs
    ├── JourneyPlanResult.cs
    ├── JourneyPlanSegment.cs
    ├── JourneyPlanStatus.cs
    ├── JourneyPlanConfidence.cs
    ├── JourneyPlanWarning.cs
    ├── JourneyPlanUsageSummary.cs
    └── DailyRestPlanningWindow.cs

ETS2Tachograph.Application
└── Services/
    └── JourneyPlannerService.cs

ETS2Tachograph.Desktop
├── ViewModels/
│   └── JourneyPlannerViewModel.cs
└── Views/
    └── JourneyPlannerView.xaml
```

### 16.1 `JourneyPlanningEngine`

Odpowiada za:

- symulację zdarzeniową,
- ograniczone rozgałęzianie,
- używanie aktualnego modelu reguł,
- budowę segmentów,
- wybór najwcześniejszego wariantu,
- gwarancję postępu,
- brak operacji zapisu.

### 16.2 `JourneyPlannerService`

Odpowiada za:

- atomowe pobranie snapshotu,
- ocenę ważności,
- obsługę luk,
- obsługę braku telemetrii,
- zbudowanie requestu,
- uruchomienie silnika,
- mapowanie DTO,
- sprawdzenie, czy wynik nie zdążył się zdezaktualizować.

### 16.3 Desktop

Odpowiada wyłącznie za:

- parsowanie czasu trwania,
- walidację formularza,
- prezentację snapshotu,
- uruchomienie obliczenia,
- prezentację statusu, wiarygodności i segmentów,
- unieważnienie widoku po zmianie stanu.

---

## 17. Obsługa danych niepełnych

### 17.1 `CardRemoved`

```text
Nie można obliczyć wiarygodnego planu.
Karta ma nierozliczoną lukę aktywności.
Najpierw uzupełnij wpis manualny.
```

Status:

```text
BlockedByGap
```

### 17.2 `ForwardTimeJump`

Plan może zostać zwrócony z ostrzeżeniem:

```text
Plan obliczono na podstawie niepełnej historii.
Wynik może się zmienić po rozliczeniu luki.
```

Confidence:

```text
BasedOnIncompleteHistory
```

### 17.3 Brak telemetrii

Plan może użyć ostatniego zapisanego stanu.

UI pokazuje:

- ostatni znany `game_time`,
- informację o braku telemetrii,
- identyfikator snapshotu,
- konieczność ponownego obliczenia po zmianie stanu.

Confidence:

```text
BasedOnLastSavedState
```

---

## 18. Koncepcja UI

Planer jest osobną zakładką:

> **PLANER**

### 18.1 Formularz

```text
┌──────────────────── PLANER PODRÓŻY ────────────────────┐
│ STAN KIEROWCY                                           │
│ Karta: S1      Snapshot: 21.07, 18:45 czasu gry         │
│ Do przerwy: 02:18   Jazda dzienna: 05:40 / 09:00       │
│ Odpoczynek musi zakończyć się za: 08:25                 │
│                                                        │
│ DANE DOSTAWY                                            │
│ Pozostały czas jazdy GPS:       [ 12:35 ]               │
│ Czas do zakończenia dostawy:    [ 28:00 ]               │
│ Praca po przyjeździe:           [ 00:30 ]               │
│ Karta:                          [ S1 ▼ ]                 │
│                                                        │
│                    [ OBLICZ PLAN ]                      │
└────────────────────────────────────────────────────────┘
```

### 18.2 Wynik

```text
✅ ZAKOŃCZYSZ W TERMINIE — zapas 02:10

Przyjazd:                 wtorek 18:15 czasu gry
Zakończenie dostawy:      wtorek 18:45 czasu gry
Wiarygodność:             potwierdzone przez bieżący model

PLAN
1. Jazda                  02:18
2. Przerwa                00:45
3. Jazda                  04:30
4. Przerwa                00:45
5. Jazda                  02:12
6. Odpoczynek dobowy      09:00
7. Jazda                  03:35
8. Inna praca             00:30

Wykorzystano:
- skrócony odpoczynek dobowy 2/3
- wydłużenie jazdy dziennej 1/2
```

### 18.3 `CalendarWait` w UI

```text
Oczekiwanie do nowego tygodnia      06:30
Aktywność: odpoczynek
Powód: wykorzystany limit 56 h
```

### 18.4 Kolory

- zielony — `MeetsDeadline` + pełna wiarygodność,
- bursztynowy — zapas poniżej 30 min lub ograniczona wiarygodność,
- czerwony — `MissesDeadline`,
- pomarańczowy — niepełna historia / ograniczony model,
- szary — brak danych, stale snapshot lub scenariusz nieobsługiwany.

UI nie używa zielonego komunikatu „legalny”, gdy `Confidence` nie jest `VerifiedByCurrentRuleModel`.

---

## 19. Testy blokujące P0

| ID | Scenariusz | Oczekiwany wynik |
|---|---|---|
| JP-P0-01 | 56 h osiągnięte przed końcem tygodnia | `CalendarWait` do nowego tygodnia |
| JP-P0-02 | 56 h + odpoczynek 24 h kończy się przed nowym tygodniem | brak jazdy po samym odpoczynku |
| JP-P0-03 | 90 h osiągnięte w oknie dwutygodniowym | oczekiwanie do faktycznego zwolnienia limitu |
| JP-P0-04 | Odpoczynek pokrywa część oczekiwania | brak podwójnego naliczenia czasu |
| JP-P0-05 | Do końca okna 24 h pozostaje mniej niż długość odpoczynku | wcześniejszy start odpoczynku |
| JP-P0-06 | Analogiczny przypadek dla okna 30 h | odpoczynek kończy się przed granicą |
| JP-P0-07 | Bufor `OtherWork` koliduje z terminem odpoczynku | odpoczynek przed buforem lub inny legalny wariant |
| JP-P0-08 | 24 h wymaga nieobsługiwanej rekompensaty | wariant 45 h lub ograniczona wiarygodność |

---

## 20. Testy statusów

| ID | Scenariusz | Status |
|---|---|---|
| JP-ST-01 | Plan mieści się w terminie | `MeetsDeadline` |
| JP-ST-02 | Legalny plan kończy się po terminie | `MissesDeadline` |
| JP-ST-03 | Nierozliczona luka `CardRemoved` | `BlockedByGap` |
| JP-ST-04 | Brak stanu regulacyjnego | `InsufficientData` |
| JP-ST-05 | Cofnięcie czasu po snapshotcie | `StaleSnapshot` |
| JP-ST-06 | Przypadek spoza MVP | `UnsupportedScenario` |
| JP-ST-07 | Brak legalnej kontynuacji | `NoLegalContinuation` |
| JP-ST-08 | Przekroczenie limitu obliczeń | `CalculationLimitReached` |

---

## 21. Testy algorytmu

### 21.1 Jazda i przerwy

- trasa kończy się przed wymaganą przerwą,
- pełna przerwa 45 min,
- wykorzystanie istniejących 15 min i dodanie 30 min,
- brak samodzielnego planowania nowego podziału 15 + 30,
- brak zbędnej przerwy po przyjeździe.

### 21.2 Jazda dzienna

- limit 9 h bez dostępnego wydłużenia,
- użycie 10 h przy dostępnym wydłużeniu,
- brak trzeciego wydłużenia,
- wybór wcześniejszego odpoczynku, gdy daje szybszy wynik globalny.

### 21.3 Odpoczynek dobowy

- 9 h przy dostępnym skróceniu,
- 11 h po wykorzystaniu limitu skróceń,
- termin ukończenia w oknie 24 h,
- termin ukończenia w oknie 30 h,
- `OtherWork` przed odpoczynkiem,
- odpoczynek pokrywający `CalendarWait`.

### 21.4 Limity okresowe

- 56 h przed początkiem nowego tygodnia,
- 56 h z odpoczynkiem kończącym się za wcześnie,
- 90 h z przesunięciem okna,
- częściowo zwolniony limit,
- brak nieskończonej pętli przy zerowym limicie.

### 21.5 Odpoczynek tygodniowy

- regularny 45 h,
- skrócony 24 h przy pełnym potwierdzeniu modelu,
- fallback do 45 h,
- ograniczony model rekompensat,
- odpoczynek równocześnie pokrywający oczekiwanie kalendarzowe.

---

## 22. Testy snapshotu i integracji

- historia i `Evaluation` mają ten sam high-water mark,
- zmiana `ActivitySessionId` unieważnia wynik,
- zmiana `world_generation` unieważnia wynik,
- cofnięcie czasu unieważnia wynik,
- zmiana karty unieważnia wynik,
- nowy rekord jazdy unieważnia wynik,
- ten sam snapshot daje identyczny rezultat,
- obliczenie nie zapisuje danych do SQLite,
- wynik przed i po restarcie jest identyczny dla tego samego stanu,
- dane hot i warm retencji dają zgodny wynik.

---

## 23. Testy bufora

- `00:00` nie dodaje segmentu,
- bufor jest `OtherWork`,
- bufor zmienia zakończenie, nie przyjazd,
- bufor nie zwiększa czasu jazdy,
- bufor wpływa na okno odpoczynku,
- bufor może wymusić odpoczynek przed jego wykonaniem,
- ujemny bufor jest odrzucany,
- `28:00` jest akceptowane jako czas trwania.

---

## 24. Testy podwójnej obsady

- okno 30 h tylko dla potwierdzonego stanu załogi,
- wybór S2 nie aktywuje automatycznie 30 h,
- wszystkie segmenty jazdy należą do wybranej karty,
- brak hipotetycznych zmian kierowców,
- brak planowania przerwy pasażera w ruchu,
- wynik zawiera ostrzeżenie o ograniczeniu MVP.

---

## 25. Testy zakończenia algorytmu

- każdy segment zmniejsza jazdę lub przesuwa czas,
- powtarzający się stan jest wykrywany,
- limit segmentów kończy obliczenie,
- limit czasu kończy obliczenie,
- limit stanów kończy obliczenie,
- zerowy możliwy segment nie tworzy pętli,
- `NoLegalContinuation` i `CalculationLimitReached` są rozróżnione.

---

## 26. Etapy realizacji

### Etap 0 — testy terenowe beta.10

**Status:** **W TRAKCIE — DZIEŃ 2 ZIELONY**

Zakres zakończony:

- [x] pojedyncza luka `CardRemoved` w bloku odpoczynku — zweryfikowana na obu kartach,
- [x] reset dobowy przyznawany na końcu połączonego bloku,
- [x] zachowanie `SourceGapId` w śladzie audytowym,
- [x] stabilność wyniku po restarcie aplikacji,
- [x] slot 2 podczas jazdy — scenariusz domknięty i wycofany z dalszych testów.

Pozostały zakres gate’u:

- [ ] luka przecinająca granicę tygodnia regulacyjnego,
- [ ] interakcja rozliczonej luki z rekompensatą tygodniową,
- [ ] porównanie UI, raportu PDF i wyniku po restarcie dla obu pozostałych scenariuszy.

Poza bieżącym gate’em:

- bloki z wieloma rozliczonymi lukami,
- warianty łączące wiele wpisów manualnych, w tym `OtherWork` i `Availability`.

Powyższe obserwacje wymagają osobnej decyzji zakresowej i nie blokują obecnego gate’u beta.10.

Gate zakończenia:

- dwa pozostałe scenariusze zakończone bez niewyjaśnionych rozbieżności,
- każdy znaleziony błąd ma test regresyjny,
- dokumentacja beta jest zaktualizowana,
- repozytorium jest czyste.

### Etap 0.5 — końcowy przegląd specyfikacji Planera

**Status:** **DOKUMENT ZAKTUALIZOWANY / DO AKCEPTACJI**

- [x] semantyka bufora,
- [x] `CalendarWait`,
- [x] atomowy snapshot,
- [x] wielostanowy wynik,
- [x] termin ukończenia odpoczynku,
- [x] ograniczenie 24 h,
- [x] ograniczenie podwójnej obsady,
- [x] gwarancja postępu,
- [x] testy P0,
- [ ] końcowa akceptacja planu po przeglądzie.

Gate:

> Plan zatwierdzony jako specyfikacja implementacyjna.

### Etap 1 — kontrakty i testy

**Może rozpocząć się dopiero po Etapie 0 i 0.5.**

- [ ] utworzyć gałąź funkcjonalną,
- [ ] dodać kontrakty,
- [ ] napisać testy JP-P0-01–08,
- [ ] napisać testy statusów,
- [ ] napisać testy snapshotu,
- [ ] napisać testy zakończenia algorytmu,
- [ ] nie tworzyć UI.

Gate:

> Testy prawidłowo zawodzą bez implementacji.

### Etap 2 — silnik zdarzeniowy

- [ ] kopia stanu roboczego,
- [ ] generowanie zdarzeń,
- [ ] maksymalny legalny blok jazdy,
- [ ] przerwy i odpoczynki,
- [ ] termin ukończenia odpoczynku,
- [ ] `CalendarWait`,
- [ ] rozgałęzienia 9/10 h i 9/11 h,
- [ ] bezpieczna obsługa 24/45 h,
- [ ] deduplikacja,
- [ ] gwarancja postępu,
- [ ] brak zapisu do bazy.

Gate:

> Testy jednostkowe, P0 i zakończenia algorytmu są zielone.

### Etap 3 — Application Service

- [ ] budowa atomowego snapshotu,
- [ ] ważność snapshotu,
- [ ] luki,
- [ ] brak telemetrii,
- [ ] DTO,
- [ ] integracja z historią i `RuleEngine`.

Gate:

> Testy integracyjne są zielone.

### Etap 4 — UI

- [ ] zakładka `PLANER`,
- [ ] pola czasu trwania,
- [ ] wybór karty,
- [ ] stan snapshotu,
- [ ] przyjazd i zakończenie,
- [ ] status i wiarygodność,
- [ ] lista segmentów,
- [ ] `CalendarWait`,
- [ ] komunikaty o unieważnieniu,
- [ ] brak reguł domenowych w ViewModelu.

Gate:

> Kontrola wizualna i test z telemetrią zaliczone.

### Etap 5 — walidacja terenowa Planera

- [ ] minimum trzy standardowe trasy,
- [ ] przypadek 56 h,
- [ ] przypadek 90 h,
- [ ] przypadek okna 24 h,
- [ ] przypadek okna 30 h,
- [ ] bufor kolidujący z odpoczynkiem,
- [ ] porównanie z Dashboardem,
- [ ] porównanie z raportem,
- [ ] ręczna symulacja,
- [ ] restart aplikacji.

---

## 27. Kryteria akceptacji MVP

Planer jest ukończony, gdy:

1. używa atomowego snapshotu,
2. korzysta z tego samego modelu co Dashboard,
3. nie zapisuje hipotetycznej historii,
4. rozróżnia opóźnienie od braku legalnej kontynuacji,
5. obsługuje 4:30,
6. obsługuje jazdę 9/10 h,
7. obsługuje odpoczynek dobowy 9/11 h,
8. obsługuje 56 h,
9. obsługuje 90 h,
10. potrafi czekać na zmianę okresu,
11. nie resetuje 56/90 h odpoczynkiem tygodniowym,
12. kończy odpoczynek przed granicą 24/30 h,
13. stosuje bufor jako `OtherWorkAfterArrival`,
14. rozdziela przyjazd i zakończenie dostawy,
15. bezpiecznie ogranicza odpoczynek tygodniowy 24 h,
16. nie symuluje przyszłych zmian kierowców,
17. gwarantuje zakończenie obliczenia,
18. pokazuje powody segmentów,
19. pokazuje poziom wiarygodności,
20. unieważnia nieaktualny plan,
21. wszystkie testy są zielone,
22. build Release ma 0 błędów i 0 ostrzeżeń,
23. test z telemetrią jest zaliczony,
24. kontrola wizualna UI jest zaliczona,
25. wynik zgadza się z ręczną symulacją i `RuleEngine`.

---

## 28. Poza zakresem MVP

- integracja z mapą ETS2,
- automatyczny odczyt czasu z Route Advisor,
- wiele dostaw,
- wspólny harmonogram dwóch kierowców,
- przyszłe zmiany kierowców,
- samodzielne planowanie nowego podziału 15 + 30,
- dzielony odpoczynek dobowy 3 + 9,
- pełna optymalizacja rekompensat,
- zapisywanie planów,
- automatyczne korygowanie planu w ruchu,
- eksport PDF,
- koszty paliwa i opłat,
- porównanie strategii.

---

## 29. Ryzyka

| Ryzyko | Priorytet | Ograniczenie |
|---|---:|---|
| Drugi silnik reguł | Krytyczny | Wspólny model i testy integracyjne |
| Nieaktualny snapshot | Wysoki | Tożsamość snapshotu i unieważnienie |
| Fałszywy reset 56/90 h | Krytyczny | `CalendarWait` i testy P0 |
| Odpoczynek rozpoczęty za późno | Krytyczny | Termin ukończenia 24/30 h |
| Niejasny bufor | Zamknięte | `OtherWorkAfterArrival` |
| Niepełne rekompensaty | Wysoki | Fallback 45 h i confidence |
| Pętla algorytmu | Krytyczny | Gwarancja postępu i limity |
| Lokalnie szybki, globalnie wolny wariant | Wysoki | Ograniczone rozgałęzienie |
| Nadmierny zakres załogi | Zamknięte | Jedna karta, bez zmian kierowców |
| Rozrost MVP | Wysoki | Jedna strategia i jawne „poza zakresem” |

---

## 30. Kolejność względem backlogu

1. Utrzymać bieżący lokalny zakres UI bez publikowania nowej bety.
2. Wykonać osobisty smoke z aktywną telemetrią dla aktualnego drzewa.
3. Podjąć osobną decyzję `GO / FIX / HOLD` dla rozpoczęcia Planera.
4. Zatwierdzić wersję 2.2 tego planu jako specyfikację implementacyjną.
5. Utworzyć gałąź Planera.
6. Napisać kontrakty i testy P0.
7. Zaimplementować silnik zdarzeniowy.
8. Dodać Application Service.
9. Dodać UI.
10. Wykonać walidację terenową Planera.
11. Dopiero potem rozważyć Fazę 2.

---

## 31. Definicja ukończenia

> Planer podróży MVP jest ukończony, gdy na podstawie jednego atomowego snapshotu historii kierowcy potrafi bez zapisu do bazy wyznaczyć najwcześniejszy wariant zgodny z aktualnie obsługiwanym modelem reguł, prawidłowo obsłużyć przerwy, odpoczynki, limity 56/90 h i oczekiwanie kalendarzowe, rozdzielić przyjazd od zakończenia dostawy, jawnie określić poziom wiarygodności oraz zwrócić wynik zgodny z Dashboardem, `RuleEngine` i ręczną symulacją w ETS2.

---

## 32. Historia zmian dokumentu

### Wersja 2.2 — bieżący stan lokalny

- zastąpiono nieaktualny gate beta.10 bieżącym stanem beta.11.1;
- odnotowano 310/310 testów Release bez tworzenia nowej paczki;
- potwierdzono, że wariant B wpisu manualnego pozostaje wejściowym warunkiem
  blokującym Planer przy nierozliczonej luce;
- utrzymano implementację Planera jako nierozpoczętą.

### Wersja 2.1 — 21.07.2026

- zaktualizowano gate beta.10 ze stanu „nie rozpoczęte” na „w trakcie”,
- odnotowano zielony wynik Dnia 2 dla obu kart,
- potwierdzono ciągłość odpoczynku przez pojedynczą lukę `CardRemoved`,
- odnotowano stabilność wyniku po restarcie i zachowanie `SourceGapId`,
- zamknięto scenariusz slotu 2 podczas jazdy,
- ograniczono pozostały gate do granicy tygodnia i rekompensaty tygodniowej,
- przeniesiono bloki z wieloma lukami poza bieżący gate,
- zaktualizowano kolejność backlogu i utrzymano blokadę implementacji Planera.

### Wersja 2.0 — 21.07.2026

- zaznaczono, że testy terenowe beta.10 nie zostały rozpoczęte,
- dodano `CalendarWait`,
- poprawiono obsługę limitów 56/90 h,
- zdefiniowano termin ukończenia odpoczynku,
- przyjęto `OtherWorkAfterArrival`,
- rozdzielono przyjazd od zakończenia dostawy,
- dodano `JourneyPlanStatus`,
- dodano `JourneyPlanConfidence`,
- dodano atomowy snapshot,
- dodano unieważnianie planu,
- przyjęto symulację zdarzeniową,
- dodano ograniczone rozgałęzienie,
- dodano gwarancję postępu,
- ograniczono użycie odpoczynku 24 h,
- ograniczono podwójną obsadę do jednej wybranej karty,
- dodano testy blokujące P0,
- zaktualizowano kolejność etapów.

### Wersja 1.0 — 21.07.2026

Pierwotna koncepcja Planera podróży MVP.
