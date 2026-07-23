# Znane ograniczenia i problemy

> Protokół v3 wymaga jednoczesnej aktualizacji aplikacji i DLL. Po podmianie
> pluginu trzeba całkowicie uruchomić ETS2 ponownie; ponowne wczytanie zapisu nie
> przeładuje biblioteki natywnej.

Opublikowaną bazą pozostaje beta.11.1. Bieżący kod zawiera również
nieopublikowane zmiany lokalne UI: wariant B wpisu manualnego, katalog krajów
ISO, korektę prezentacji `ODP. TYG.` oraz hotfix licznika pauzy 44/45.
Lokalny gate wynosi 315/315, a build Release ma 0 błędów i 0 ostrzeżeń.
Artefakt beta.11.1 przeszedł końcowy smoke z aktywną telemetrią 23 lipca 2026;
wszystkie testy były zielone, a decyzja wydaniowa brzmi **GO**.
Nowe błędy z testów należy dopisywać razem ze statusem `lokalne` albo numerem
wydanej paczki i raportem diagnostycznym.

## Naprawione lokalnie po beta.11.1

- Licznik celu pauzy nie korzysta już z czasu od kliknięcia w UI.
  `RegulationState.CurrentContinuousBreakMinutes` wskazuje długość bieżącego,
  ciągłego bloku `BreakOrRest` po regule jednej minuty.
- Dashboard, urządzenie i overlay dla slotów 1 i 2 pokazują ten sam stan co
  RuleEngine. Referencyjne `41 min reconstructed + 3 min telemetry` daje
  `00:44`, `00:01` do celu i status `W TRAKCIE`; 45. minuta daje `ZALICZONA`.
- Dedykowana przerwa slotu 2 podczas jazdy nadal korzysta z osobnej logiki
  `CrewTachographEngine` i nie została zmieniona.
- Raport naprawy: `docs/BUGFIX_REPORT_QUALIFIED_BREAK_COUNTER_2026-07-24.md`.

## Naprawione w beta.11.1

- Naprawiono automatyczną klasyfikację bloku 24 h+ hostującego rekompensatę.
  RuleEngine generuje legalne kandydatury, a użytkownik wybiera rolę bloku.
- Naprawiono fałszywe `ForwardTimeJump` drugiej karty podczas wspólnego skoku
  czasu załogi. Rekonstrukcja zachowuje wyłącznie stabilną aktywność własną.
- Dwie luki referencyjne Dnia 141 są korygowane audytowalnie jako
  `AutomaticCrewReconstruction`; nie są usuwane ręcznym SQL.
- Decyzje alokacji i pełny ślad odtwarzają się po restarcie SQLite oraz są
  dostępne w UI, PDF, CSV i JSON.

## Naprawione w beta.11

- Usunięto uproszczony model rekompensat skróconych odpoczynków tygodniowych,
  który sumował okruchy nadwyżek z wielu późniejszych odpoczynków i przez to
  zaniżał dług. Spłata jest teraz atomowa (en bloc), przypisana do jednego
  kwalifikującego odpoczynku co najmniej 9 h, z pełnym terminem i śladem.
- Dane referencyjne po poprawce: Staniek `1253 min / 20:53`, Doboś
  `1192 min / 19:52`. Wartości `18 min` i `353 min` są historycznym wynikiem
  starego algorytmu.
- Pełny kontrakt zobowiązań jest dostępny w DTO, szczegółach UI, PDF, CSV i JSON
  oraz odtwarza się identycznie po restarcie plikowej bazy SQLite.

> Beta.11 została wycofana przed smoke testem. Obowiązującym następcą jest
> beta.11.1.

## Telemetria i czas gry

- Skok czasu do przodu, w tym `g_set_time`, nie dostarcza telemetrii dla pominiętego
  okresu. Skoki do 2 minut są rekonstruowane, dłuższy potwierdzony odpoczynek na
  postoju może zostać odtworzony, a pozostałe przypadki tworzą jawną lukę danych.
- Cofnięcie `game_time` tworzy nową sesję i zastępuje tylko nakładającą się
  przyszłość. Jest to zamierzone zachowanie, a nie usuwanie całej wcześniejszej
  historii.
- Ramki `running == 0` są ignorowane przez historię i high-water mark. Podczas menu
  lub pauzy licznik nie powinien rosnąć.
- Plugin i aplikacja działają tylko na Windows x64. Wymagany jest ETS2 uruchomiony
  w wariancie `win_x64`.

## Reguły tachografu

- Jest to symulator, nie certyfikowana implementacja prawna Annex 1C. PDF, CSV,
  JSON i `.tacho` nie są urzędowymi plikami z rzeczywistego tachografu.
- Tryb promu jest włączany ręcznie. Telemetria ETS2 nie daje wiarygodnego zdarzenia,
  które pozwalałoby automatycznie rozpoznać cały przebieg odpoczynku promowego.
- Pociągi nie są modelowane, ponieważ ETS2 praktycznie nie udostępnia użytecznego
  scenariusza kolejowego dla tego projektu.
- Kraj rozpoczęcia i zakończenia jest wybierany przez użytkownika z
  przeszukiwalnego katalogu ISO 3166-1 alpha-2; nie jest automatycznie ustalany
  z pozycji ciężarówki. LCD może prezentować odpowiadający kod tachografowy.

## Historia, retencja i raporty

- Zimna warstwa retencji, czyli dobowe podsumowania danych starszych niż 365 dni
  gry, ma tylko hak architektoniczny i nie jest jeszcze implementowana.
- Nie ma jeszcze przycisku świadomego usuwania historii starszej niż wybrana liczba
  dni gry. Aplikacja automatycznie archiwizuje, ale nie usuwa danych.
- Surowy diagnostyczny CSV jest celowo minutowy i może być duży. Eksport CSV
  zobowiązań rekompensaty ma osobny kontrakt: jeden rekord na zobowiązanie.
  PDF używa zwiniętych bloków aktywności i osobnej tabeli rekompensat.
- Rekonstruowane odcinki są oznaczane jako `Reconstructed`, a bloki zawierające
  różne źródła jako `Mixed`.

## Aplikacja i dystrybucja

- Blokada wymaganego wpisu manualnego dotyczy stanu tachografu. Oficjalna
  telemetria SCS jest tylko do odczytu, więc aplikacja nie może fizycznie zatrzymać
  ciężarówki w ETS2; próba ruchu jest nadal zapisywana jako jazda.

- Skróty `Alt+1`, `Alt+2` lub `Alt+Q` mogą kolidować z innymi nakładkami. W takim
  przypadku trzeba wyłączyć konfliktujący skrót w drugiej aplikacji.
- Aktualizacja jest ręczna: trzeba podmienić aplikację i właściwą DLL pluginu.
- Przy zgłoszeniu problemu należy używać przycisku **Raport diagnostyczny**. Logi
  lokalne są przechowywane przez 14 dni.
