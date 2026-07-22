# Znane ograniczenia i problemy

> Protokół v3 wymaga jednoczesnej aktualizacji aplikacji i DLL. Po podmianie
> pluginu trzeba całkowicie uruchomić ETS2 ponownie; ponowne wczytanie zapisu nie
> przeładuje biblioteki natywnej.

Stan na wersję beta po Fazie 1. Ta lista dotyczy potwierdzonych ograniczeń;
nowe błędy z testów należy dopisywać razem z numerem wersji paczki i raportem
diagnostycznym.

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
- Kraj rozpoczęcia i zakończenia jest wybierany ręcznie; nie jest automatycznie
  ustalany z pozycji ciężarówki.

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
