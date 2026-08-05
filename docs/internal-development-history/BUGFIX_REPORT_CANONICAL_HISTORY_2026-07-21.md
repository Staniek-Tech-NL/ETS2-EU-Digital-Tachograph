# Raport błędu — nakładki w kanonicznej historii aktywności

**Data:** 21 lipca 2026
**Wersja, w której błąd wystąpił:** 0.1.0-beta.10
**Wersja z poprawką:** 0.1.0-beta.10.1
**Commity:** `49e200d` (poprawka), `906b7d5` (przygotowanie wydania)
**Klasyfikacja:** blokujący — aplikacja nie uruchamiała się

---

## 1. Objaw

Aplikacja przestała startować. Po ekranie inicjalizacji pojawiało się okno
**„Nie można uruchomić tachografu"** z komunikatem:

```
An error occurred while saving the entity changes. See the inner exception for details.
```

Proces kończył się kodem 1. Powtórne uruchomienia dawały identyczny wynik. Baza danych
nie była uszkodzona — błąd występował za każdym razem w tym samym miejscu inicjalizacji.

Wpisy w logu `%LocalAppData%\ETS2Tachograph\logs\tachograph-2026-07-21.log`:

```
21:09:44 | INFO  | APP_START             | Uruchamianie aplikacji.
21:09:44 | INFO  | DATABASE_BACKUP_CREATED
21:09:46 | INFO  | APP_READY             | Inicjalizacja profili, kart i historii.
21:09:49 | ERROR | APP_START_FAILED      | DbUpdateException: An error occurred while saving...
21:09:56 | INFO  | APP_STOP              | Zamykanie aplikacji. Kod: 1.
```

Ścieżka wywołań z logu:

```
ActivityRepository.ArchiveWarmAsync        (ActivityRepository.cs)
CrewTachographService.RegisterCardAsync    (CrewTachographService.cs:29)
MainViewModel.StartAsync                   (MainViewModel.cs:495)
App.OnStartup                              (App.xaml.cs:120)
```

Log zapisywał wyłącznie wyjątek zewnętrzny. Wyjątek wewnętrzny — ten, który niósł
faktyczną informację — nie trafiał do pliku i został odzyskany dopiero przez uruchomienie
`ArchiveWarmAsync` na kopii bazy:

```
SqliteException: SQLite Error 19: 'UNIQUE constraint failed:
  WarmActivityBlocks.DriverCardId, WarmActivityBlocks.StartGameMinute'
```

## 2. Skutek dla użytkownika

- aplikacja całkowicie niedostępna, bez obejścia od strony interfejsu;
- błąd występował przy rejestracji karty, czyli przed pokazaniem głównego okna;
- dane nie były tracone — transakcja wycofywała się w całości — ale nie było do nich dostępu;
- niezależnie od tego kanoniczna projekcja liczyła jedną minutę dwa razy, co naruszało
  zasadę nadrzędną projektu mówiącą, że historia minutowa jest jednoznacznym źródłem prawdy.

## 3. Przebieg diagnozy

Diagnoza wymagała trzech podejść. Dwa pierwsze okazały się błędne i zostały odrzucone
przez eksperyment. Opisane są tutaj, ponieważ oba wyglądały wiarygodnie i mogą wrócić
przy podobnych objawach.

### 3.1 Hipoteza pierwsza — kolizja usuwania ze wstawianiem (odrzucona)

`ArchiveWarmAsync` wykonywał `RemoveRange(existingBlocks)` i `AddRange(desiredBlocks)`
w jednym `SaveChangesAsync`. Klucz główny bloku to świeży `Guid`, więc EF Core nie widzi,
że nowy wiersz koliduje ze starym na indeksie **alternatywnym**. Wniosek nasuwał się sam:
`INSERT` wykonuje się przed `DELETE`.

**Obalone testem.** Test odtwarzający dokładnie ten układ — istniejące bloki, przebudowa
z tym samym `StartGameMinute`, nowe identyfikatory — przeszedł na zielono. EF Core radzi
sobie z tą sytuacją. Proponowane rozdzielenie `SaveChanges` nie naprawiłoby niczego.

### 3.2 Hipoteza druga — rekordy przed kotwicą sesji, do odrzucenia (odrzucona)

Reprodukcja na prawdziwej bazie pokazała, że pada karta **Doboś**, która nie miała ani
jednego istniejącego bloku — więc nie było czego usuwać. Zrzut wierszy oczekujących na
wstawienie ujawnił, że to sam kod produkuje duplikat:

```
bloki do wstawienia: 29
  DUPLIKAT start=179351:
     179351 -> 179352   dur=1   OtherWork  Telemetry
     179351 -> 179354   dur=3   OtherWork  Telemetry
```

Źródłem okazały się rekordy zaczynające się przed kotwicą własnej sesji. Naturalna
propozycja brzmiała: odrzucać takie rekordy, symetrycznie do istniejącego `TruncateAfter`.

**Obalone sprawdzeniem pokrycia.** Reguła oparta na kotwicy skasowałaby około **1007 minut**
prawdziwej historii — o czym niżej.

### 3.3 Ustalenie przyczyny

Dopiero analiza tego, **kto pokrywa sporne minuty**, rozdzieliła dwa zjawiska, które
wobec kotwicy wyglądają identycznie.

## 4. Przyczyna źródłowa

`Canonicalize` w [ActivityRepository.cs](../../src/ETS2Tachograph.Infrastructure/Persistence/ActivityRepository.cs)
składa historię z kolejnych sesji, traktując je jako gałęzie czasu gry. Dla każdej sesji
wywoływał `TruncateAfter`, przycinając **poprzednią** historię do kotwicy nowej sesji, po czym
dopisywał **wszystkie** rekordy nowej sesji bez żadnej kontroli.

Jeżeli sesja zawierała rekord zaczynający się przed własną kotwicą, a poprzednia gałąź te
minuty już posiadała, ta sama minuta trafiała do projekcji dwa razy.

Konkretny przypadek z bazy:

| element | wartość |
|---|---|
| sesja 1 karty Doboś | zawiera rekord `179351 → 179352` |
| kotwica sesji 2 | `179352` |
| sesja 2 karty Doboś | zawiera **ten sam** rekord `179351 → 179352` |

`TruncateAfter(canonical, 179352)` nie usuwa rekordu sesji 1, bo ten kończy się dokładnie
na kotwicy. Rekord sesji 2 dochodzi obok niego.

Dalej `BuildWarmBlocks` skleja wyłącznie rekordy stykające się co do minuty
(`blocks[^1].End == record.Start`). Nakładka tego warunku nie spełnia, więc zakłada nowy
blok — z tym samym początkiem co poprzedni. Unikalny indeks
`IX_WarmActivityBlocks_DriverCardId_StartGameMinute` odrzuca wstawkę i cała transakcja pada.

### 4.1 Dlaczego ujawnił się dopiero 21 lipca

Rekordy powodujące duplikat są w bazie od dawna. Blokada wymagała jednak, aby
`ArchiveWarmAsync` w ogóle zaczął zapisywać bloki, a robi to tylko wtedy, gdy zestaw bloków
się zmienił. Tego wieczoru o 20:19 rozliczono dwie luki (`acc1278d`, `efdc2d8d`), co po raz
pierwszy od dawna zmieniło kanoniczną historię. Aplikacja pracowała jeszcze normalnie do
21:06, a padła przy pierwszym restarcie po tych wpisach.

## 5. Dwie klasy rekordów przed kotwicą

W obu bazach znaleziono **sześć** rekordów zaczynających się przed kotwicą własnej sesji.
Dzielą się na dwie klasy o przeciwnym znaczeniu.

**Klasa A — rzeczywiste duplikaty (2 rekordy):**

| karta | sesja | rekord | źródło | pokrycie |
|---|---|---|---|---|
| Doboś | 2 | `179351 → 179352` | Telemetry | ten sam rekord w sesji 1 |
| Staniek | 1 | `179351 → 179352` | Telemetry | ten sam rekord w sesji 0 |

**Klasa B — backfill wpisów manualnych (4 rekordy):**

| karta | sesja | rekord | długość | źródło | pokrycie |
|---|---|---|---|---|---|
| Staniek | 14 | `184392 → 184481` | 89 min | ManualEntry | tylko 2 pierwsze minuty |
| Staniek | 14 | `184481 → 184704` | 223 min | ManualEntry | brak |
| Staniek | 14 | `184704 → 185152` | 448 min | ManualEntry | brak |
| Staniek | 16 | `185806 → 186055` | 249 min | ManualEntry | brak |

Klasa B to rozliczenia luk. Wpis manualny trafia do sesji **bieżącej**, a rozlicza czas
**wcześniejszy**, więc z definicji leży przed kotwicą własnej sesji. To poprawne działanie
mechanizmu, nie usterka.

Wszystkie sześć rekordów kończy się dokładnie na kotwicy własnej sesji, więc reguła
„kończy się przed kotwicą lub na niej → odrzuć" usunęłaby **wszystkie**, w tym komplet
rozliczeń luk. Strata wyniosłaby **1007 minut netto** (1009 minut rozpiętości minus
2 minuty pokryte przez sesję 10), czyli blisko 17 godzin historii karty Staniek.

Wniosek: kotwica mówi, **kiedy rekord zapisano**, a nie **czyje są minuty**. Prawidłowym
kryterium jest pokrycie.

## 6. Co zostało naprawione

### 6.1 Odejmowanie pokrytych zakresów w `Canonicalize`

Dodano `SubtractCoveredRanges(incoming, canonicalRecords)`, które zwraca **0…N fragmentów**
rekordu — te części, których nie pokrywa jeszcze żaden rekord kanoniczny. Przedziały są
półotwarte `[Start, End)`, więc fragment kończący się tam, gdzie zaczyna się następny, jest
przyległy, a nie nakładający się.

Rekord może zostać rozcięty na dowolną liczbę części:

```
rekord wejściowy:  100 → 200
pokrycie:          120 → 130,  150 → 170
wynik:             100 → 120,  130 → 150,  170 → 200
```

`Canonicalize` dopisuje teraz wyłącznie te fragmenty:

```csharp
foreach (var record in session.Records.OrderBy(x => x.Start))
    canonical.AddRange(SubtractCoveredRanges(record, canonical).ToList());
```

Obowiązująca zasada domenowa:

> Nowa sesja przejmuje oś czasu **od swojej kotwicy w górę**, bo tam czyści ją
> `TruncateAfter`. **Poniżej kotwicy** może wyłącznie uzupełniać minuty, których nikt nie
> pokrywa.

Pierwszeństwo ma historia już kanoniczna. Wpis manualny rozlicza luki i nie koryguje
zapisanej telemetrii — świadoma korekta historii, gdyby kiedyś była potrzebna, wymaga
osobnego mechanizmu z własnym powodem, audytem i jawną regułą pierwszeństwa.

Rozwiązanie działa też na nakładki wewnątrz jednej sesji, bo rekordy porównywane są
z tym, co już zostało dopisane, niezależnie od pochodzenia.

### 6.2 Twarda walidacja niezmiennika

Dodano `EnsureNoOverlap`, sprawdzające `records[i].End <= records[i + 1].Start` — czyli
pełny niezmiennik, nie tylko unikalność początków. Dwa rekordy mogą się nakładać, mając
różne początki, a indeks w bazie tego nie wykryje.

Walidacja działa w **dwóch** miejscach:

- na końcu `Canonicalize`, więc obejmuje wszystkich trzech konsumentów projekcji
  (`LoadGapContextAsync`, `LoadRawDriverHistoryAsync`, `ArchiveWarmAsync`);
- na wejściu `BuildWarmBlocks`, jako druga linia obrony przed innymi producentami danych.

### 6.3 Wyjątek domenowy zamiast błędu bazy

Dodano `InvalidCanonicalHistoryException` z identyfikatorem karty oraz obiema kolidującymi
minutami wraz z ich aktywnością i źródłem:

```
Canonical records overlap for card Doboś: 179351-179352 (OtherWork, Telemetry)
and 179351-179354 (OtherWork, Telemetry).
```

Unikalny indeks w SQLite pozostaje ostatnim strażnikiem, ale problem jest teraz wykrywany
wcześniej, w warstwie rozumiejącej historię kierowcy, zamiast kończyć jako `SQLite Error 19`.

Świadomie **nie** zastosowano cichego pomijania duplikatów — zamiotłoby to pod dywan każdą
przyszłą usterkę produkującą nakładki.

## 7. Zachowanie po zmianie

| sytuacja | wynik |
|---|---|
| rekord w całości pokryty | znika z projekcji |
| pokryty początek | zostaje część prawa |
| pokryty koniec | zostaje część lewa |
| pokryty środek | powstają dwa fragmenty |
| kilka rozłącznych pokryć | powstaje kilka fragmentów |
| brak pokrycia | rekord bez zmian |
| identyczny duplikat | znika |
| backfill manualny w niepokrytej luce | zostaje w całości |
| konflikt wpisu manualnego z telemetrią | wygrywa istniejąca historia |

## 8. Weryfikacja

### 8.1 Testy automatyczne

Nowy plik `tests/ETS2Tachograph.Infrastructure.Tests/CanonicalProjectionTests.cs` — 14
testów, w tym fixture'y odtwarzające rzeczywiste kształty danych obu kart (kotwice sesji
i minuty gry wprost z baz terenowych).

- **bez poprawki pada 12 z 14** — są to testy regresyjne, nie dokumentacja;
- pełny pakiet: **239 testów, zero niepowodzeń** (przed zmianą 225);
- kompilacja Release bez ostrzeżeń.

Kopii bazy nie dołączono do repozytorium — fixture'y odtwarzają kształt danych, nie
wciągając prywatnej historii jazdy do gita.

### 8.2 Pomiar na kopii prawdziwej bazy

| | przed | po |
|---|---|---|
| Staniek — rekordy kanoniczne | 13 660 | 13 659 |
| Staniek — minuty | 20 982 | 20 981 |
| Doboś — rekordy kanoniczne | 15 595 | 15 594 |
| Doboś — minuty | 21 199 | 21 198 |
| nakładki / zdublowane początki | 1 + 1 | **0** |
| `ArchiveWarmAsync` dla Doboś | `SQLite Error 19` | **28 bloków, 1049 minut** |
| powtórne wykonanie | — | wynik identyczny |

Ubyła **dokładnie jedna minuta na kartę** — ta liczona podwójnie. Backfill manualny
1007 minut pozostał nienaruszony.

### 8.3 Uruchomienie aplikacji

Test wykonano na kopii katalogu danych; oryginał odłożono jako
`%LocalAppData%\ETS2Tachograph.original-backup`.

```
Pierwszy start:                     OK (główne okno, APP_READY)
Drugi start:                        OK (przebieg identyczny)
Zamknięcie:                         APP_STOP kod 0  (wcześniej kod 1)
SQLite Error 19:                    nie
InvalidCanonicalHistoryException:   nie
Doboś — WarmActivityBlocks:         28 bloków / 1049 minut
Staniek — WarmActivityBlocks:       31 bloków / 1047 minut
Rekordy źródłowe:                   nietknięte, sześć spornych rekordów na miejscu
```

## 9. Wpływ na dane użytkownika

- **żadne dane nie zostały usunięte ani zmienione**; sześć rekordów, które ujawniły błąd,
  pozostaje w bazie i napędza fixture'y testowe;
- brak migracji i brak potrzeby czyszczenia bazy;
- zmiana dotyczy wyłącznie projekcji wyliczanej na bieżąco, nie zapisu;
- jedyna różnica w liczbach to zniknięcie jednej podwójnie liczonej minuty na kartę.

## 10. Wydanie

Paczka `output/releases/ETS2Tachograph-0.1.0-beta.10.1-win-x64`, zbudowana od zera po
commicie wydania.

- `FileVersion = 0.1.10.1`, `ProductVersion = 0.1.0-beta.10.1+906b7d54…` — artefakt niesie
  hash commita, z którego powstał; wcześniejsze paczki deklarowały `0.1.0-beta.5`
  niezależnie od faktycznej zawartości;
- `AssemblyVersion` pozostaje `0.1.0.0`, żeby referencje między zestawami nie wymagały
  przewiązywania;
- plugin SCS **bit w bit identyczny** z beta.10 (`4F73CBFE…D97D02`), protokół nadal v3;
- SHA-256 archiwum: `5f4f7d85e33fb3e2ad4111bc7372067477ce611de9f70dc835be29182cb26195`.

## 11. Sprawy otwarte

1. **Ciągłość odpoczynku przez wiele rozliczonych luk** — osobne zadanie domenowe,
   niezwiązane z tą poprawką mimo wspólnych danych. Dni 129–130 zachowują się tak samo
   przed i po zmianie.
2. **`SessionId` w komunikacie wyjątku** — świadomie pominięty, wymagałby przeciągnięcia
   indeksu sesji przez projekcję.
3. **Wyjątek wewnętrzny w logu** — `APP_START_FAILED` zapisuje wyłącznie wyjątek zewnętrzny.
   Gdyby log od razu zawierał `SQLite Error 19`, diagnoza byłaby krótsza o dwie błędne
   hipotezy. Warto rozważyć logowanie łańcucha `InnerException`.
4. **Numeracja dni** — `GameClockFormatter` etykietuje dni od jedynki, więc minuta gry `N`
   to `Dzień N/1440 + 1`. Surowe wyliczenia z minut wymagają przesunięcia o jeden, żeby
   zgadzały się z UI, CSV i PDF.

## 12. Wnioski

Objaw był błędem bazy danych, a przyczyna leżała trzy warstwy wyżej, w regule domenowej
mówiącej, czyje są minuty przy zmianie gałęzi czasu gry. Dwie pierwsze hipotezy celowały
w warstwę zapisu i obie dały się obalić eksperymentem — pierwszą testem jednostkowym,
drugą sprawdzeniem pokrycia na prawdziwych danych.

Decydujące okazało się pytanie nie „skąd pochodzi rekord", tylko „czy te minuty ktoś już
ma". Gdyby przyjąć kryterium kotwicy, aplikacja wystartowałaby i wyglądałaby na naprawioną,
po cichu tracąc blisko 17 godzin rozliczonej historii.
