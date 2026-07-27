# M5.2-P — checkpoint wydajności uruchamiania

**Projekt:** ETS2 EU Digital Tachograph
**Wydanie docelowe:** `0.1.0-beta.12`
**Data:** 27 lipca 2026
**Status:** **GO WARUNKOWE — KOSZT LINIOWY HISTORII POZOSTAJE**

## Powód

Podczas ręcznej weryfikacji fundamentu M5.2 aplikacja uruchamiała się na
bieżącej bazie użytkownika przez około 52 sekundy. Profil wykazał, że zasoby
lokalizacji i katalog krajów zajmują około 0,03 sekundy. Koszt pochodził z
projekcji historii wykonywanej przy automatycznej korekcie luk załogi oraz
archiwizacji ciepłej historii kart.

Checkpoint jest osobną bramką wydajnościową wewnątrz M5. Nie zmienia zakresu
lokalizacji, reguł domenowych ani kolejności prac M5.3–M5.8.

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

## Chronione niezmienniki

- wynik projekcji kanonicznej historii pozostaje identyczny;
- kolejność i semantyka gałęzi sesji pozostają identyczne;
- automat nie rozlicza dodatkowych luk;
- retencja ciepła pozostaje idempotentna;
- praca startowa nie jest przenoszona w tło i nie jest pomijana;
- schemat SQLite oraz formaty zewnętrzne pozostają bez zmian.

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
pomiarze nadal istniały te same 4 nierozliczone luki. Wynik domenowy nie uległ
zmianie.

## Automatyka

- 24/24 testy projekcji kanonicznej i retencji: PASS;
- 14/14 testów automatycznej korekty załogi i wpisu manualnego: PASS;
- 55/55 testów infrastruktury: PASS;
- pełna regresja rozwiązania: **558/558 PASS**;
- test regresyjny historii 12 000 rekordów minutowych wymaga projekcji
  poniżej 1 sekundy.

## Gate

### Automatyka — GO warunkowe

- praca repozytorium na kopii bieżącej bazy: mniej niż 10 sekund;
- pełna regresja zielona;
- brak zmiany wyniku domenowego;
- brak zmiany schematu i kontraktów zewnętrznych.

Warunek obowiązywania GO:

- przy kontroli przed M6 praca repozytorium na kopii bazy wydaniowej pozostaje
  poniżej 10 sekund;
- osobisty pomiar `APP_START` → `APP_READY` zostaje zapisany jako nowa baza
  odniesienia;
- checkpoint zostaje ponownie otwarty, jeżeli praca repozytorium przekroczy
  10 sekund albo czas `APP_START` → `APP_READY` wzrośnie o więcej niż 50%
  względem zatwierdzonej bazy odniesienia.

Stałe ograniczenie kosztu wymaga osobnego zadania po beta.12: inkrementalnej
przebudowy warm albo wdrożenia cold retention. Nie należy dokładać tej zmiany
do M5 bez ponownego otwarcia zakresu i testów retencji.

### Osobiste potwierdzenie — oczekuje

Na uruchomionej aplikacji z tą samą bazą należy potwierdzić:

- wyraźne skrócenie czasu od startu procesu do gotowego okna;
- poprawne odtworzenie obu kart i bieżącego stanu;
- brak nowo rozliczonych albo utraconych luk;
- brak błędu startu w logu diagnostycznym.

Osobiste potwierdzenie zamyka część bieżącą checkpointu, lecz status pozostaje
**GO warunkowe** do kontroli przed M6.
