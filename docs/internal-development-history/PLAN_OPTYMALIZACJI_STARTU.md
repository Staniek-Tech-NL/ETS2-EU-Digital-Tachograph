# Plan optymalizacji czasu uruchamiania

**Projekt:** ETS2 EU Digital Tachograph
**Data planu:** 27 lipca 2026
**Punkt wyjścia:** checkpoint `M5.2-P` (commit `25f1358`)
**Status:** propozycja do zatwierdzenia

## Problem

Checkpoint `M5.2-P` zbił pracę repozytorium przy starcie z około 49 sekund do
5,36 sekundy na bieżącej bazie około 20 MB. Nie usunął jednak przyczyny
strukturalnej: koszt startu nadal rośnie liniowo z całą zachowaną historią.

Trzy mechanizmy odpowiadają za ten wzrost:

1. `ArchiveWarmAsync` przy każdym starcie ładuje wszystkie surowe rekordy
   karty, kanonizuje całość i odtwarza całą projekcję warm od zera.
2. Przy jakiejkolwiek różnicy zapis warm jest pełny: `RemoveRange` wszystkich
   bloków i `AddRange` wszystkich nowych, zamiast zapisu różnicowego.
3. Rekordy oznaczone jako zarchiwizowane pozostają w SQLite i są ładowane
   ponownie, a pętla ustawiająca `IsArchivedToWarm` przechodzi również po
   rekordach, które tę flagę już mają.

Próg cold istnieje w kontrakcie retencji, ale archiwizator cold nie istnieje,
więc historia nigdy nie przestaje obciążać startu.

## Cel

Czas uruchomienia przestaje zależeć liniowo od całkowitej długości historii
i zaczyna zależeć od jej świeżego wycinka.

**Kryterium liczbowe:** na bazie trzykrotnie większej od dzisiejszej praca
repozytorium przy starcie rośnie nie więcej niż półtora raza względem bazy
dzisiejszej. Bez spełnienia tego kryterium optymalizacja jest nieskuteczna,
niezależnie od bezwzględnych sekund.

## Niezmienniki obowiązujące w każdej fazie

- projekcja kanoniczna historii, lista luk i ocena RuleEngine pozostają
  identyczne co do wartości;
- kontrakty eksportowe JSON, CSV, `.tacho` i SQLite, protokół v3, enumy,
  `ObligationId`, `RestBlockId`, `CandidateId` oraz kody naruszeń i błędów
  pozostają nietknięte;
- migracje wyłącznie addytywne;
- retencja pozostaje idempotentna: dwa przebiegi z rzędu na niezmienionej
  bazie dają identyczny stan bazy;
- każda faza jest osobnym commitem z własną wiadomością opisującą zakres.

## Wymagany dowód dla każdej fazy

Ten sam rygor, bez wyjątków i bez skracania:

1. złoty zrzut **przed** zmianą, na świeżej kopii realnej bazy z
   `output\ODZYSK-BAZY`, obejmujący projekcję kanoniczną, luki nierozliczone
   i rozliczone, bloki warm oraz wynik raportu;
2. zrzut **po** zmianie porównany z złotym co do wartości;
3. test idempotencji retencji;
4. pomiar na trzech rozmiarach bazy, nie na jednym punkcie;
5. pełna regresja zielona i build Release bez ostrzeżeń.

Praca zawsze na kopii. Baza w `%LocalAppData%` nie jest wejściem do żadnego
pomiaru ani testu.

## Faza 0 — domknięcie długu z M5.2-P

Wchodzi przed beta.12. Jest warunkiem wiarygodności wszystkiego dalej.

- [x] Test rozliczający lukę leżącą w strefie warm, porównujący ocenę
      RuleEngine przed podmianą źródła `CanonicalRecords` i po niej.
      Uzasadnienie: `LoadGapContextAsync` przestało budować `CanonicalRecords`
      z surowej projekcji, a zaczęło z projekcji hot/warm, która scala bloki
      i inaczej obcina gałąź sesji. Ten strumień idzie prosto do RuleEngine
      w `ManualEntryService`. Pomiar checkpointu rozliczył zero luk, więc tej
      ścieżki nie dotknął.
- [x] Test drugiej rozliczonej luki z rzędu — obszar otwartego znaleziska
      o ciągłości odpoczynku z beta.10.
- [x] Poprawka dokumentu `M5_2_CHECKPOINT_WYDAJNOSCI_STARTU.md`: niezmiennik
      o identycznej projekcji kanonicznej jest w obecnym brzmieniu sprzeczny
      z opisem zmiany technicznej w tym samym dokumencie. Do przeformułowania
      na stan faktyczny.
- [x] Zapis bazy odniesienia `APP_START` → `APP_READY` z osobistego
      uruchomienia.

## Faza 1 — instrumentacja i stanowisko pomiarowe

Bez tego kolejne fazy są zgadywaniem. Faza tania, wchodzi przed pracą
właściwą.

- [ ] Znaczniki czasu w logu diagnostycznym: `APP_START`, migracja bazy,
      koniec korekty luk załogi, koniec archiwizacji osobno dla każdej karty,
      pokazanie okna, `APP_READY`.
- [ ] Powtarzalny scenariusz pomiarowy na kopii bazy, z wynikiem w postaci
      tabeli odcinków, a nie jednej liczby łącznej.
- [ ] Trzy bazy testowe: dzisiejsza, trzykrotność, dziesięciokrotność.
      Krzywa jest wynikiem, punkt nie jest.

## Faza 2 — inkrementalna archiwizacja warm

Główna pozycja planu. To tutaj znika liniowy koszt.

- [ ] Wprowadzić trwały znacznik uzgodnienia warm: minutę gry, do której
      projekcja warm jest już zgodna z rekordami surowymi.
- [ ] Przetwarzać wyłącznie przedział między znacznikiem a bieżącym progiem
      warm, zamiast całej historii.
- [ ] Zapis różnicowy bloków warm: usuwać i wstawiać wyłącznie bloki
      przecinające zmieniony przedział.
- [ ] Ograniczyć pętlę oznaczania `IsArchivedToWarm` do rekordów, które tej
      flagi jeszcze nie mają.
- [ ] Rozdzielić odczyt bez śledzenia od wąskiego zestawu encji do zapisu.

**Główne ryzyko poprawnościowe.** Operacja obcinająca historię poniżej
znacznika unieważnia już zarchiwizowany przedział. Znacznik musi się wtedy
cofać, inaczej w warm zostaną nieaktualne bloki, a błąd będzie cichy i trwały.
To wymaga własnego zestawu testów gałęzi sesji, napisanych przed zmianą, a nie
po niej. Jeżeli ta część nie da się przetestować przekonująco, faza 2 nie
wchodzi.

## Faza 3 — retencja cold

Wymaga decyzji zakresowej przed implementacją, nie w jej trakcie.

- [ ] Rozstrzygnąć, jak głęboko wstecz raporty i widoki muszą sięgać
      w pełnej rozdzielczości minutowej.
- [ ] Poniżej progu cold przechowywać agregat o grubszej ziarnistości
      w osobnej tabeli i nie ładować go przy starcie.
- [ ] Ścieżka odczytu sięga po dane cold wyłącznie na żądanie raportu.

Bez odpowiedzi na pierwszy punkt implementacja nie ma sensu, bo nie wiadomo,
co wolno zagregować.

## Faza 4 — zapytania

Indeksy zostały sprawdzone i są w większości na miejscu: rekordy aktywności po
sesji i minucie oraz po luce źródłowej, luki po karcie i minucie, bloki warm po
karcie i minucie. Dodawanie indeksów **nie jest** pozycją tego planu.

- [ ] Jedyny kandydat do sprawdzenia: odczyt historii filtruje rekordy po
      karcie przez złączenie z sesją oraz po fladze archiwizacji, która
      indeksu nie ma. Do potwierdzenia planem zapytania na dużej bazie —
      i tylko jeżeli pomiar to uzasadni.

## Faza 5 — okno przed zakończeniem pracy startowej

Pozycja świadomie ostatnia, bo dotyka decyzji produktowej, nie technicznej.

Checkpoint `M5.2-P` deklaruje niezmiennik: praca startowa nie jest przenoszona
w tło ani pomijana. Dopóki ten niezmiennik obowiązuje, okno nie pojawi się
wcześniej niż praca się skończy, a jedyną drogą jest przyspieszanie.

Jeżeli celem jest okno widoczne w mniej niż sekundę niezależnie od historii,
niezmiennik trzeba zmienić świadomie: okno pokazuje się od razu, w jawnym
stanie przygotowywania historii, z zablokowanymi akcjami domenowymi do czasu
gotowości. To jest do rozstrzygnięcia osobno i nie należy tego robić przy
okazji fazy 2.

## Kolejność i przypisanie do wydań

| Faza | Kiedy | Uzasadnienie |
|---|---|---|
| 0 | przed beta.12 | zdejmuje otwarty HOLD, warunek wiarygodności |
| 1 | przed beta.12 | tania, addytywna, bez ryzyka domenowego |
| 2 | po beta.12 | zmienia zachowanie retencji, wymaga pełnych testów |
| 3 | po beta.12 | wymaga wcześniejszej decyzji zakresowej |
| 4 | po beta.12 | tylko jeżeli pomiar z fazy 1 to uzasadni |
| 5 | osobna decyzja | zmiana niezmiennika, nie optymalizacja |

Podział jest zgodny z zapisem w `M5_2_CHECKPOINT_WYDAJNOSCI_STARTU.md`, że
stałe ograniczenie kosztu jest osobnym zadaniem po beta.12 i nie należy go
dokładać do M5.

## Kryteria wyjścia całości

- praca repozytorium przy starcie na bazie trzykrotnej nie przekracza
  półtorakrotności wyniku z bazy dzisiejszej;
- złote zrzuty zgodne co do wartości na wszystkich trzech bazach testowych;
- retencja idempotentna, potwierdzona testem;
- pełna regresja zielona, build Release bez ostrzeżeń;
- osobiste uruchomienie potwierdza poprawne odtworzenie kart, stanu i luk.
