# M5.2-P — checkpoint wydajności uruchamiania

**Projekt:** ETS2 EU Digital Tachograph
**Wydanie docelowe:** `0.1.0-beta.12`
**Data:** 27 lipca 2026
**Status:** **WYDAJNOŚĆ GO — POPRAWNOŚĆ HOLD**

Checkpoint ma dwie niezależne bramki. Bramka wydajnościowa jest spełniona
warunkowo. Bramka zgodności projekcji jest otwarta i wstrzymuje M6.

## Powód

Podczas ręcznej weryfikacji fundamentu M5.2 aplikacja uruchamiała się na
bieżącej bazie użytkownika przez około 52 sekundy. Profil wykazał, że zasoby
lokalizacji i katalog krajów zajmują około 0,03 sekundy. Koszt pochodził z
projekcji historii wykonywanej przy automatycznej korekcie luk załogi oraz
archiwizacji ciepłej historii kart.

Checkpoint jest osobną bramką wewnątrz M5. Nie zmienia zakresu lokalizacji
ani kolejności prac M5.3–M5.8.

## Odpowiedź na ryzyko wzrostu

Problem nie powinien już narastać w sposób patologiczny jak przed poprawką,
ale nie został ograniczony stałym kosztem:

- automatyczna korekta luk nie materializuje już całej historii dla każdej
  luki; jej koszt zależy przede wszystkim od liczby luk kandydackich;
- `ArchiveWarmAsync` nadal przy każdym starcie ładuje wszystkie surowe rekordy
  karty i odtwarza projekcję warm;
- rekordy oznaczone jako zarchiwizowane pozostają w SQLite;
- próg cold istnieje w kontrakcie, ale cold-tier archiver nie jest
  zaimplementowany.

W konsekwencji koszt archiwizacji pozostaje w przybliżeniu liniowy względem
całej zachowanej historii. Przy wielokrotnym wzroście liczby rekordów czas
startu ponownie wzrośnie. Dzisiejszy wynik jest wystarczający dla bieżącej
bazy beta.12, ale nie stanowi gwarancji stałego czasu dla nieograniczonej
historii.

## Zmiana techniczna

1. Projekcja kanoniczna utrzymuje uporządkowaną listę rekordów i wyszukuje
   pierwszy możliwy konflikt binarnie, zamiast skanować i sortować całą
   dotychczasową historię dla każdego rekordu.
2. Zapytania dotyczące samych luk nie materializują rekordów aktywności.
3. Kontekst rozliczenia luki pobiera rekordy rozliczenia bezpośrednim
   zapytaniem i korzysta z istniejącej projekcji historii hot/warm.

## Wynik pomiaru

Pomiar wykonano na świeżej kopii bazy użytkownika o rozmiarze około 20 MB.
Kopia zachowała dane wejściowe; próbny przebieg nie zmienił bazy użytkownika.

| Odcinek | Przed | Po |
|---|---:|---:|
| Automatyczna korekta luk załogi | 30,84 s | 1,31 s |
| Archiwizacja trzech kart | ok. 16,35 s | 4,01 s |
| Łączna praca repozytorium objęta checkpointem | ok. 49 s | 5,36 s |

Przed pomiarem były 4 nierozliczone luki. Automat rozliczył 0 pozycji, a po
pomiarze nadal istniały te same 4 nierozliczone luki.

**Zasięg dowodowy tego pomiaru.** Skoro automat rozliczył zero pozycji, przebieg
nie wykonał ścieżki rozliczania luki ani przeliczenia po wpisie manualnym.
Pomiar potwierdza wynik domenowy dla ścieżki odczytu i **nie** potwierdza go dla
ścieżki zmienionej w punkcie 3 powyżej. Wcześniejsza wersja tego dokumentu
wyciągała z niego wniosek szerszy, niż na to pozwalał.

## Znalezisko poprawnościowe

Punkt 3 zmiany technicznej przełączył `CanonicalRecords` w kontekście luki
z projekcji surowej na projekcję hot/warm. Strumień ten trafia bezpośrednio do
RuleEngine w `ManualEntryService`.

Projekcja hot/warm obcina gałąź sesji na `Math.Max(kotwica, próg warm)`, a nie
na samej kotwicy. Gdy nowa sesja zakotwiczy się poniżej progu warm — po
wczytaniu zapisu gry starszego o ponad 14 dni gry — blok ciepły nie zostaje
przycięty i nachodzi na rekordy nowej sesji. Projekcja hot/warm nie ma kontroli
nachodzenia, którą projekcja surowa wykonuje przy każdym złożeniu.

Pomiar odtwarzający, 27 lipca 2026:

```
projekcja surowa : [0-600), [660-700), [700-800)    poprawna
projekcja warm   : [0-600), [660-1300), [700-800)   nachodzenie 700..800
```

Rozjazd w `RegulationState`:

| Pole | Projekcja surowa | Kontekst luki |
|---|---:|---:|
| `ReducedDailyRestsSinceWeeklyRest` | 1 | 2 |
| `MinutesUntilDailyRestDeadline` | 740 | 1440 |
| `DailyRestCompletionDeadlineGameMinute` | 2040 | 2740 |
| `LastDailyRestResetAt` | 600 | 1300 |

Aplikacja przyjmuje reset dobowy w minucie 1300 zamiast 600 i daje kierowcy
blisko 12 godzin więcej do terminu odpoczynku dobowego, niż mu przysługuje.
Błąd działa na jego korzyść, więc nie zgłasza się sam.

Samo nachodzenie w projekcji hot/warm istniało przed checkpointem i dotyczy
również Dashboardu. Checkpoint nie stworzył tego defektu, natomiast zdjął
ochronę z jedynego miejsca, które ją miało — ścieżki rozliczania luk.
Pozycja aktywna w `KNOWN_ISSUES.md`.

## Niezmienniki

Potwierdzone pomiarem i testami:

- automat nie rozlicza dodatkowych luk;
- retencja ciepła pozostaje idempotentna;
- praca startowa nie jest przenoszona w tło i nie jest pomijana;
- schemat SQLite oraz formaty zewnętrzne pozostają bez zmian;
- w zwykłej strefie warm, bez gałęzi poniżej progu, kontekst luki pokrywa te
  same minuty co projekcja surowa i daje identyczny `RegulationState`.

Obalone:

- niezmiennik „wynik projekcji kanonicznej historii pozostaje identyczny”
  nie obowiązuje przy gałęzi zakotwiczonej poniżej progu warm;
- niezmiennik „kolejność i semantyka gałęzi sesji pozostają identyczne”
  nie obowiązuje z tego samego powodu — obie projekcje obcinają gałąź
  w innym miejscu.

## Automatyka

- 24/24 testy projekcji kanonicznej i retencji: PASS;
- 14/14 testów automatycznej korekty załogi i wpisu manualnego: PASS;
- 55/55 testów infrastruktury: PASS;
- pełna regresja rozwiązania w chwili zamknięcia M5: 564/564 PASS;
- test regresyjny historii 12 000 rekordów minutowych wymaga projekcji
  poniżej 1 sekundy;
- `WarmZoneGapResolutionTests` przypina zgodność kontekstu luki z projekcją
  surową w zwykłej strefie warm oraz ciągłość odpoczynku przy drugiej
  rozliczonej luce z rzędu;
- `BackwardBranchProjectionTests` odtwarza znalezisko i pozostaje czerwony
  do czasu naprawy.

## Bramka wydajnościowa — GO warunkowe

- praca repozytorium na kopii bieżącej bazy: mniej niż 10 sekund;
- pełna regresja zielona;
- brak zmiany schematu i kontraktów zewnętrznych.

Warunek obowiązywania:

- przy kontroli przed M6 praca repozytorium na kopii bazy wydaniowej pozostaje
  poniżej 10 sekund;
- osobisty pomiar `APP_START` → `APP_READY` zostaje zapisany jako nowa baza
  odniesienia;
- bramka zostaje ponownie otwarta, jeżeli praca repozytorium przekroczy
  10 sekund albo czas `APP_START` → `APP_READY` wzrośnie o więcej niż 50%
  względem zatwierdzonej bazy odniesienia.

Stałe ograniczenie kosztu wymaga osobnego zadania po beta.12: inkrementalnej
przebudowy warm albo wdrożenia cold retention. Plan: `docs/PLAN_OPTYMALIZACJI_STARTU.md`.

## Bramka zgodności projekcji — HOLD

Bramka jest otwarta i wstrzymuje zamrożenie RC w M6. Zamknięcie wymaga:

- mapa minuta → aktywność z projekcji hot/warm jest identyczna z mapą
  z projekcji surowej w każdym stanie bazy, w tym przy sesji zakotwiczonej
  poniżej progu warm;
- projekcja hot/warm ma własną kontrolę nachodzenia z zapisem diagnostycznym
  i bezpiecznym zejściem do projekcji surowej;
- unieważnianie projekcji warm przy kotwicy poniżej progu jest zaimplementowane
  i pokryte testami gałęzi sesji;
- `BackwardBranchProjectionTests` jest zielony;
- złoty zrzut na kopii realnej bazy zgodny przed i po zmianie, test
  idempotencji retencji zielony, pomiar na trzech rozmiarach bazy wykonany;
- pełna regresja zielona i build Release bez ostrzeżeń.

Dopiero po zamknięciu obu bramek M6 może zamrozić RC.

## Osobiste potwierdzenie

Na uruchomionej aplikacji z tą samą bazą należy potwierdzić:

- wyraźne skrócenie czasu od startu procesu do gotowego okna;
- poprawne odtworzenie obu kart i bieżącego stanu;
- brak nowo rozliczonych albo utraconych luk;
- brak błędu startu w logu diagnostycznym.
