# Znane ograniczenia i problemy

> Protokół v3 wymaga jednoczesnej aktualizacji aplikacji i DLL. Po podmianie
> pluginu trzeba całkowicie uruchomić ETS2 ponownie; ponowne wczytanie zapisu nie
> przeładuje biblioteki natywnej.

Stan na wersję beta po Fazie 1. Ta lista dotyczy potwierdzonych ograniczeń;
nowe błędy z testów należy dopisywać razem z numerem wersji paczki i raportem
diagnostycznym.

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
- Rekompensaty skróconych odpoczynków tygodniowych mają nadal uproszczony model:
  termin jest liczony numerami tygodni, nie ma jeszcze trwałego śladu przypisania
  spłaty do konkretnego odpoczynku ani pełnej obsługi przypadków granicznych.
  Model **zaniża dług**: skrócenie tygodniowe (`2700 − długość`) jest spłacane
  nadwyżką ponad 9 h z każdego kolejnego odpoczynku dobowego, sumowaną po okruchach,
  podczas gdy art. 8(6)/(7) wymaga rekompensaty w jednym bloku (en bloc) dołączonej
  do dedykowanego odpoczynku co najmniej 9 h. W efekcie licznik `REKOMPENSATA` może
  pokazywać dług bliski zeru, mimo że realnie pozostaje do odebrania. Przykład:
  skrócenie o 1253 min bywa raportowane jako 18 min zaległości.
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
- Surowy CSV jest celowo minutowy i może być duży. PDF używa zwiniętych bloków.
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
