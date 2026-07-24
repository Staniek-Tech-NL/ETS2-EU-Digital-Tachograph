# M3A — Game Calendar & Deadline Presentation

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status specyfikacji:** **ZATWIERDZONA — GO**  
**Status implementacji:** **AUTOMATYCZNIE DOMKNIĘTA — HOLD DO RĘCZNEGO GATE’U UI**  
**Kryterium wejścia:** M3A-0 zakończony wynikiem `PASS Z UWAGĄ FORMALIZACYJNĄ`.  
**Kryterium wyjścia:** jeden kanoniczny kalendarz gry w Core, jawna semantyka
deadline’ów i zgodna prezentacja pierwszej tury M3A.  
**Następny etap:** M3 po spełnieniu `M2-CREW GO AND M3A GO`.

## 1. Cel

M3A formalizuje istniejącą semantykę kalendarza gry i zapewnia wspólną,
jednoznaczną prezentację terminów regulacyjnych.

M3A:

- nie zmienia algorytmu `GameWeek.From`;
- nie zmienia znaczenia `WeekEpochOffsetDays`;
- nie dodaje nowej kalibracji ani drugiego anchoru;
- nie przenosi kwalifikacji regulacyjnej do UI;
- nie zmienia historycznych snapshotów ani identyfikatorów zobowiązań.

## 2. Decyzja M3A-0

Obowiązuje normatywnie:

> Początek przedziału zwracanego przez `GameWeek.From` odpowiada poniedziałkowi
> 00:00 w kalendarzu gry przesuniętym przez surową wartość
> `WeekEpochOffsetDays`.

Wynik audytu:

```text
M3A-0 — PASS Z UWAGĄ FORMALIZACYJNĄ
```

Istniejąca implementacja i test `Game_week_uses_monday_epoch_and_offset`
potwierdzają intencję epoki poniedziałkowej. Brakujący element to publiczny,
kanoniczny kontrakt granic tygodnia dostępny poza RuleEngine.

## 3. Niezmienniki kalendarza

### 3.1 Kanoniczne granice `GameWeek`

Dla tygodnia o indeksie `k` i surowego offsetu `o`:

```text
StartGameMinute =
    k * GameWeek.MinutesPerWeek
    - o * GameWeek.MinutesPerDay

EndGameMinuteExclusive =
    StartGameMinute + GameWeek.MinutesPerWeek
```

Obowiązuje przedział półotwarty:

```text
[StartGameMinute, EndGameMinuteExclusive)
```

Początek jest poniedziałkiem 00:00, a koniec wyłączny jest następnym
poniedziałkiem 00:00 w skonfigurowanym kalendarzu gry.

Core ma wystawić publiczną operację zwracającą te granice. RuleEngine, Planer,
Application i prezentacja nie mogą utrzymywać własnych kopii tego algorytmu.

### 3.2 Dzień tygodnia

Indeks dnia jest wyprowadzany wyłącznie z początku kanonicznego tygodnia:

```text
dayIndexWithinWeek =
    (gameTime.TotalMinutes - week.StartGameMinute)
    / GameWeek.MinutesPerDay

0 = Monday
1 = Tuesday
2 = Wednesday
3 = Thursday
4 = Friday
5 = Saturday
6 = Sunday
```

Dla czasu należącego do danego tygodnia wynik musi mieścić się w zakresie
`0…6`.

### 3.3 Wyświetlany numer dnia gry

`DisplayedGameDay` jest niezależny od tygodnia regulacyjnego:

```text
DisplayedGameDay =
    floor(gameTime.TotalMinutes / GameWeek.MinutesPerDay) + 1
```

Zmiana `WeekEpochOffsetDays`:

- może zmienić nazwę dnia tygodnia;
- nie może zmienić napisu `Dzień N`;
- nie może zmienić godziny wynikającej z `game_time`.

## 4. Surowy `WeekEpochOffsetDays`

`GameCalendarContext` przechowuje dokładną wartość `WeekEpochOffsetDays`
z atomowego snapshotu.

Obowiązują następujące zakazy:

- brak normalizacji przez `% 7`;
- brak zamiany wartości ujemnej na dodatni odpowiednik;
- brak wyprowadzania nowego offsetu w UI;
- brak przepisywania historycznych snapshotów;
- brak zmiany indeksów `GameWeek` i tożsamości zobowiązań.

W szczególności:

```text
-1 != +6
```

Wartości mogą wskazywać równoważne fizyczne granice modulo siedem, lecz mogą
nadawać im inne indeksy `GameWeek`. Ta różnica jest domenowo istotna.

Znaczenie przykładowych wartości:

| Offset | Dzień tygodnia dla `gameMinute = 0` | Najbliższy poniedziałek od minuty 0 |
|---:|---|---:|
| `-1` | niedziela | `1440` |
| `0` | poniedziałek | `0` |
| `+1` | wtorek | `8640` |

## 5. Podział odpowiedzialności

```text
Core
└── kanoniczne granice GameWeek Start/End

GameCalendarContext
└── niemutowalny kontekst z atomowego snapshotu

GameCalendarResolver
└── GameTime → GameCalendarMoment

GameCalendarFormatter
└── wyłącznie prezentacja gotowego momentu

DeadlinePresentation
└── komunikat dobrany do semantyki terminu
```

### 5.1 `GameCalendarContext`

Kontekst:

- zawiera surowy `WeekEpochOffsetDays`;
- powstaje z tego samego atomowego snapshotu co prezentowany wynik;
- jest niemutowalny;
- nie odczytuje ustawień globalnych podczas formatowania;
- nie może zostać podmieniony w trakcie renderowania jednego wyniku.

Wynik Planera musi być formatowany przy użyciu offsetu z jego snapshotu, nie
przy użyciu później odczytanej wartości ustawienia.

### 5.2 `GameCalendarMoment`

Rozwiązany moment powinien udostępniać co najmniej:

- źródłowy `GameTime`;
- `GameWeek`;
- kanoniczne granice tygodnia;
- `GameWeekday`;
- `DisplayedGameDay`;
- godzinę i minutę dnia.

### 5.3 Zakazy dla formattera

Formatter nie może:

- ponownie obliczać początku tygodnia;
- pobierać offsetu z globalnego stanu;
- tworzyć deadline’u przez interpretację reguł;
- kwalifikować odpoczynku lub rekompensaty;
- odejmować minuty od terminu wyłącznego bez jawnego kontraktu prezentacji.

Nazwy i skróty dni tygodnia muszą być mapowane z `GameWeekday` w jednym miejscu
warstwy prezentacji, przygotowanym do przeniesienia wartości do zasobów `.resx`
w M5. Niedopuszczalne są rozproszone literały `Pon`, `Sob`, `Ndz` albo ich
angielskie odpowiedniki w ViewModelach, XAML i formatterach szczegółowych.

## 6. Semantyka deadline’ów

Obowiązuje jawny typ:

```csharp
public enum GameDeadlineSemantic
{
    CompleteBy,
    StartNoLaterThan,
    CompleteBefore,
    AvailableFrom
}
```

| Obszar | Semantyka | Znaczenie dokładnej granicy |
|---|---|---|
| Odpoczynek dzienny | `CompleteBy` | ukończenie na granicy jest dopuszczalne |
| Odpoczynek tygodniowy | `StartNoLaterThan` | rozpoczęcie na granicy jest dopuszczalne |
| Rekompensata | `CompleteBefore` | ukończenie na granicy jest spóźnione |
| `CalendarWait` | `AvailableFrom` | dostępność rozpoczyna się na granicy |

Wymagane komunikaty:

| Semantyka | Pełny prefiks |
|---|---|
| `CompleteBy` | `Ukończ do:` |
| `StartNoLaterThan` | `Rozpocznij najpóźniej:` |
| `CompleteBefore` | `Ukończ przed:` |
| `AvailableFrom` | `Jazda dostępna od:` |

`DueAtExclusive` rekompensaty nie może być prezentowane jako poprzednia minuta
ani jako termin włącznie. Obowiązuje warunek:

```text
SettledAt < DueAtExclusive
```

## 7. Źródła absolutnych terminów

UI nie tworzy terminu przez swobodne `teraz + pozostało`.

| Powierzchnia | Kanoniczne źródło |
|---|---|
| `ODP. DZIENNY` | absolutny termin ukończenia z tego samego snapshotu |
| `ODP. TYG.` | `WeeklyRestStartDeadlineGameMinute` z RuleEngine |
| rekompensata | istniejący `DueAtExclusive` |
| `CalendarWait` | absolutna granica zwolnienia limitu z wyniku Planera |

Jeżeli DTO udostępnia jedynie liczbę minut pozostałych, warstwa Application może
zbudować termin absolutny tylko z `Now` i wartości pobranej w tym samym atomowym
snapshotcie. XAML i `MainViewModel` nie mogą samodzielnie interpretować reguły.

## 8. Format prezentacji

### 8.1 Format pełny

```text
Pon · Dzień 29 · 00:00
```

Format zawiera:

- nazwę dnia tygodnia dla szybkiej orientacji;
- stabilny i audytowalny numer dnia gry;
- dokładną godzinę granicy.

Przykłady:

```text
Ukończ do: Pon · Dzień 29 · 00:00
Rozpocznij najpóźniej: Sob · Dzień 27 · 07:29
Ukończ przed: Pon · Dzień 29 · 00:00
Jazda dostępna od: Pon · Dzień 29 · 00:00
```

### 8.2 Format kompaktowy

```text
PON · D29 · 00:00
```

Mała powierzchnia może użyć formatu kompaktowego pod warunkiem, że tooltip albo
widok szczegółowy pokazuje pełny komunikat semantyczny.

Format samego dnia tygodnia i godziny, na przykład `Pon 00:00`, jest
niedopuszczalny dla terminów, ponieważ nie identyfikuje konkretnego tygodnia.

## 9. Zakres pierwszej implementacji

Pierwsza tura M3A obejmuje wyłącznie:

```text
ODP. DZIENNY
ODP. TYG.
terminy rekompensat
```

Kontrakt `AvailableFrom` zostaje zdefiniowany teraz, lecz prezentacja
`CalendarWait` nie wchodzi do pierwszej tury.

Poza pierwszą turą pozostają:

- `DO PRZERWY`;
- zmiany UI Planera rynku;
- problem i regresja 44/45;
- nowe reguły regulacyjne;
- normalizacja lub migracja offsetu;
- dynamiczne stosowanie offsetu bez restartu.

## 10. Spójność ustawień i snapshotu

Zmiana `WeekEpochOffsetDays` zaczyna obowiązywać spójnie po ponownym uruchomieniu
aplikacji. Wszystkie serwisy domenowe, RuleEngine, Planer i prezentacja muszą w
danym uruchomieniu używać tej samej wartości.

Niedopuszczalny jest stan:

```text
UI używa nowego offsetu
RuleEngineEvaluation pochodzi ze starego offsetu
```

Snapshot i jego tożsamość zachowują surowy offset. Zmiana offsetu unieważnia
wynik zależny od poprzedniej definicji tygodnia.

## 11. Wymagane testy

### 11.1 Core

- dokładny początek tygodnia;
- minuta przed początkiem;
- początek następnego tygodnia;
- offsety `0`, `+1` i `-1`;
- zgodność `GameWeek.From` z publicznymi granicami;
- poniedziałek `0` i niedziela `6`;
- niezmienność `DisplayedGameDay` przy zmianie offsetu;
- zachowanie surowej różnicy `-1` i `+6`.
- test równości publicznych granic Core z dotychczasową formułą
  `HistoryAnalysis.WeekBounds` dla offsetów `{-1, 0, +1, +6}`;
- pełny pakiet 157 testów RuleEngine pozostaje zielony po zastąpieniu lokalnego
  obliczenia delegacją do Core.

### 11.2 Application i prezentacja

- jeden snapshot dostarcza `Now`, deadline i offset;
- zmiana tożsamości snapshotu unieważnia prezentację;
- format pełny i kompaktowy;
- wszystkie cztery prefiksy semantyczne;
- jedno mapowanie `GameWeekday` na pełne i skrócone nazwy dni, gotowe do
  zasilenia z `.resx`;
- dokładna granica rekompensaty pozostaje wyłączna;
- formatter nie odczytuje ustawień globalnych.

### 11.3 UI i regresja

```text
M3A-UI-01 — ODP. DZIENNY: pełny termin i semantyka CompleteBy
M3A-UI-02 — ODP. TYG.: pełny termin i semantyka StartNoLaterThan
M3A-UI-03 — rekompensata: DueAtExclusive jako CompleteBefore
M3A-UI-04 — zgodność S1/S2 i zachowanie po restarcie
REG-44-45 — osobny test pauzy niezaliczonej przy 44 min
```

`M3A-UI-01…03` muszą asertować dokładny prefiks właściwy dla powierzchni, a nie
jedynie obecność dowolnego tekstu terminu:

```text
M3A-UI-01 → Ukończ do:
M3A-UI-02 → Rozpocznij najpóźniej:
M3A-UI-03 → Ukończ przed:
```

`REG-44-45` nie jest testem M3A i nie może być uznany za naprawiony przez zmianę
prezentacji. Każda zmiana wspólnych bindingów lub XAML wymaga pełnej checklisty
regresji UI.

## 12. Gate M3A

### 12.1 Wejście do implementacji

- [x] M3A-0 zakończony wynikiem `PASS Z UWAGĄ FORMALIZACYJNĄ`;
- [x] brak potrzeby nowego anchoru;
- [x] brak zmiany znaczenia `WeekEpochOffsetDays`;
- [x] pełny dokument M3A zatwierdzony 2026-07-24.

### 12.2 Wyjście z M3A

- [x] publiczne granice `GameWeek` są jednym źródłem prawdy;
- [x] publiczne granice Core są równe dotychczasowej formule dla offsetów
  `{-1, 0, +1, +6}`, a pakiet 157 testów RuleEngine pozostaje zielony;
- [x] RuleEngine, Planer i prezentacja używają zgodnego offsetu;
- [x] pierwsza tura UI otrzymuje właściwe absolutne terminy;
- [x] semantyka dokładnej granicy jest poprawna dla każdego rodzaju terminu;
- [x] testy automatyczne M3A są zielone;
- [ ] testy ręczne M3A i pełna checklista UI są zielone;
- [x] brak nowych błędów P0/P1 w gate’ach automatycznych.

### 12.3 Wynik implementacji automatycznej

- **Data implementacji:** 2026-07-24
- **Publiczne granice Core:** zgodne z dotychczasową formułą dla
  `{-1, 0, +1, +6}`
- **RuleEngine:** 157/157
- **Pełna regresja Release:** 478/478
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Regresja restartu rekompensaty:** zapisany wybór
  `DailyRestWithCompensation` jest ponownie stosowany po odtworzeniu historii
  warm + hot; odpoczynek spłacający nie tworzy nowego długu.
- **Gate ręczny:** oczekuje na `M3A-UI-01…04` i pełną checklistę UI
- **Wynik bieżący:** **HOLD DO RĘCZNEGO GATE’U UI**

Automatyczna część implementacji M3A została zakończona. M3 pozostaje HOLD do
zielonego ręcznego gate’u UI i zamknięcia M3A wynikiem GO. Wejście do M3 wymaga
łącznie:

```text
M2-CREW GO
AND
M3A GO
```

---

**Dokumenty powiązane:** `M2_PLANER_SILNIK_ZDARZENIOWY.md`,
`M3_PLANER_APPLICATION_SERVICE_I_UI.md`, `JOURNEY_PLANNER_MVP_PLAN.md`,
`WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`,
`WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
