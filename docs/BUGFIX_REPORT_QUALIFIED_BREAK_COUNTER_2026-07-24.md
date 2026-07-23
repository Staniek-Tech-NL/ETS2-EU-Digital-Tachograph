# Raport naprawy licznika pauzy 44/45

**Data:** 24 lipca 2026

**Status:** wdrożone lokalnie, automatyczny gate zaliczony

**Baza wydaniowa:** `0.1.0-beta.11.1` — zamrożony artefakt bez zmian

**Gałąź:** `codex/hotfix-qualified-break-counter`

## 1. Cel naprawy

Usunąć rozjazd między licznikiem celu pauzy w UI a RuleEngine. Dashboard,
urządzenie i overlay mogły liczyć czas od chwili kliknięcia aktywności
`BreakOrRest`, podczas gdy RuleEngine kwalifikował dopiero minuty zatwierdzone
przez regułę jednej minuty. Na granicy 44/45 UI mogło przez to pokazać
`ZALICZONA` o minutę za wcześnie.

Przypadek referencyjny:

```text
41 min odpoczynku zrekonstruowanego
+ 3 min odpoczynku z telemetrii
= 44 min zakwalifikowanego, ciągłego bloku
```

Oczekiwany stan przy celu 45 min to `00:44`, `00:01` do końca i
`W TRAKCIE`. Dopiero 45. zakwalifikowana minuta zeruje jazdę ciągłą i daje
status `ZALICZONA`.

## 2. Przyczyna

`MainViewModel` wyznaczał czas pauzy jako różnicę między bieżącym `game_time`
a `_restStartedAtGameMinute`. Pole startowe było stanem prezentacyjnym i nie
uwzględniało:

- minut odtworzonych lub wpisanych manualnie przed bieżącą telemetrią;
- wyniku reguły jednej minuty;
- faktycznego scalenia sąsiadujących rekordów w jeden blok aktywności;
- trailing gap, gdy ostatni odpoczynek nie sięga bieżącego `Now`.

Silnik reguł i UI korzystały więc z dwóch różnych miar.

## 3. Wdrożona zmiana

### RuleEngine

Do `RegulationState` dodano addytywne pole:

```csharp
public long CurrentContinuousBreakMinutes { get; init; }
```

`RegulationEngine.Evaluate` ustawia je na długość ostatniego biegu tylko wtedy,
gdy:

- bieg ma aktywność `BreakOrRest`;
- jego `EndExclusive` jest równy `context.Now`.

W przeciwnym przypadku wartość wynosi `0`. Miara pochodzi z tych samych
`ActivityRun`, których RuleEngine używa do progu 45 minut.

### Desktop

Postojowe liczniki slotów 1 i 2 odczytują
`snapshot.Regulation.State.CurrentContinuousBreakMinutes`. Z tej wartości
wyliczane są:

- czas zaliczony;
- czas pozostały;
- procent postępu;
- status `W TRAKCIE` albo `ZALICZONA`.

Gałąź dedykowanej 45-minutowej przerwy slotu 2 podczas jazdy nie została
zmieniona i nadal korzysta z `CrewTachographEngine`.

Overlay oraz LCD urządzenia korzystają z właściwości `MainViewModel`, dlatego
nie wymagały osobnych zmian. XAML pozostał bez zmian.

Pola `_restStartedAtGameMinute`, `_restStartedAtGameMinute2` oraz
`RestCardPersistentState` pozostawiono dla zgodności persystencji karty. Nie są
już źródłem wartości `elapsed`.

## 4. Testy test-first

Dodano regresje RuleEngine:

1. `41 reconstructed + 3 telemetry = 44`, bez resetu jazdy ciągłej;
2. 45. minuta daje `CurrentContinuousBreakMinutes == 45` i reset jazdy;
3. gdy ostatni bieg jest Jazdą, bieżąca pauza wynosi `0`.

Dodano cienki test projekcji Desktop:

| Minuty | Zaliczono | Pozostało | Status |
|---:|---:|---:|---|
| 44 | `00:44` | `00:01` | `W TRAKCIE` |
| 45 | `00:45` | `00:00` | `ZALICZONA` |

Format pozostaje zgodny z istniejącą aplikacją: `HH:mm`.

## 5. Gate automatyczny

Po pełnej implementacji uruchomiono:

```text
dotnet test ETS2Tachograph.sln --no-restore
dotnet build ETS2Tachograph.sln -c Release --no-restore
```

Wynik:

| Projekt testowy | Testy |
|---|---:|
| Core | 33/33 |
| Telemetry.Scs | 8/8 |
| Engine | 69/69 |
| RuleEngine | 65/65 |
| Application | 50/50 |
| Reports | 9/9 |
| Infrastructure | 51/51 |
| Desktop | 30/30 |
| **Łącznie** | **315/315** |

Build Release: **0 błędów, 0 ostrzeżeń**.

## 6. Commity

- `a75f25b fix(ui-counters): licz pauze z zakwalifikowanego bloku`
- `d93245b test(ui-counters): pokryj granice 44-45 minut`

## 7. Zakres świadomie wyłączony

- progi i reguły RuleEngine;
- przerwa dzielona 15+30;
- logika moving break slotu 2;
- persystencja stanu karty;
- XAML;
- zamrożony artefakt `0.1.0-beta.11.1`.

## 8. Pozostała weryfikacja

Automatyczny gate jest zamknięty. Przed publikacją kolejnego artefaktu należy
wykonać ręczny scenariusz w działającym ETS2:

1. doprowadzić licznik zakwalifikowanego odpoczynku do 44 minut;
2. potwierdzić `00:44`, `00:01`, `W TRAKCIE` na Dashboardzie, LCD i overlay;
3. domknąć 45. minutę;
4. potwierdzić `00:45`, `00:00`, `ZALICZONA` dla S1 i postojowej gałęzi S2;
5. sprawdzić osobno, że moving break S2 podczas jazdy działa bez regresji.

Do czasu tej kontroli nie należy deklarować ręcznego gate'u wizualnego jako
zaliczonego.
