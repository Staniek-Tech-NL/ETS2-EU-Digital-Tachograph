# Raport stanu produkcji — ETS2 EU Digital Tachograph

**Wersja:** 0.1.0-beta.4  
**Data raportu:** 18 lipca 2026  
**Platforma:** Windows x64  
**Technologie:** .NET 9, C#, WPF, SQLite, Entity Framework Core, natywny plugin C++  
**Telemetria:** oficjalne SCS SDK 1.14, protokół shared memory v2

## 1. Podsumowanie wykonawcze

Projekt osiągnął etap kompletnej, instalowalnej wersji beta przeznaczonej do testów
w prawdziwej rozgrywce ETS2. Aplikacja odbiera oficjalną telemetrię SCS, prowadzi
historię dwóch kart kierowców w czasie gry, wylicza liczniki tachografu, obsługuje
cofnięcia i skoki czasu, zapisuje dane w SQLite oraz udostępnia realistyczny panel,
nakładki i raporty.

Beta.4 jest zbudowana jako samodzielna aplikacja `win-x64`. Pełny przebieg testów
w konfiguracji Release zakończył się wynikiem **193/193**. Wszystkie migracje bazy
zostały również próbnie zastosowane na czystej bazie SQLite.

Projekt nie jest jeszcze certyfikowanym tachografem ani implementacją urzędowego
Annex 1C. Obecny status wydania to **kontrolowana beta**, gotowa do dalszych testów
regresyjnych i długich sesji w grze.

## 2. Stan głównych obszarów

| Obszar | Stan | Rezultat |
|---|---|---|
| Model domenowy | Gotowy | Karty, aktywności, sesje, luki, warunki specjalne i czas gry |
| Telemetria SCS | Gotowa | Plugin x64, shared memory v2, monitor i kontrola wersji |
| Silnik tachografu | Gotowy do bety | Centralne przetwarzanie ramek, timeline, snapshoty i dwie karty |
| RuleEngine | Operacyjny | Liczniki jazdy i odpoczynku, naruszenia, podwójna obsada i prom |
| Cofanie czasu | Gotowe | Model `truncate-and-append`, wspólna granica świata dla obu kart |
| Wpisy manualne | Gotowe do bety | Jawne luki, rozliczanie, ślad audytowy i blokada po wyjęciu karty |
| Baza danych | Gotowa | SQLite, EF Core, migracje, repozytoria, transakcje i backup |
| Retencja | Hot/warm gotowe | Minuty przez 14 dni gry, starsze dane w blokach, bez kasowania |
| UI WPF | Operacyjne | Panel tachografu, karty, menu, historia, raporty i ustawienia |
| Nakładki | Gotowe | S1/S2, skróty, zapamiętywanie osobnych pozycji |
| Raporty i eksport | Gotowe do bety | PDF, CSV, VTC JSON, `.tacho` i diagnostyka ZIP |
| Testy | Zielone | 193 testy Release oraz plan testów manualnych beta.4 |
| Dystrybucja | Gotowa lokalnie | Self-contained ZIP, plugin i suma SHA-256 |

## 3. Zrealizowane etapy projektu

### Fundament domenowy

- prosty model profilu kierowcy i karty kierowcy;
- `ActivityRecord` jako minutowe źródło prawdy;
- `ActivityTimeline` oraz sesje historii;
- aktywności: Jazda, Inna praca, Dyspozycyjność, Przerwa/Odpoczynek i OUT;
- warunki promowe oraz model dwóch slotów;
- wszystkie obliczenia oparte na `game_time`, nigdy na zegarze Windows;
- liczniki regulacyjne są wyliczane z historii, a nie przechowywane w modelu kierowcy.

### Oficjalna telemetria ETS2

- natywny plugin C++ wykorzystujący SCS SDK 1.14;
- pamięć współdzielona `Local\ETS2Tachograph.Telemetry.v2`;
- 28-bajtowy, wersjonowany protokół;
- pola stanu gry, czasu gry, prędkości i generacji świata;
- `world_generation` zwiększane po `frame_start.timer_restart`;
- wykrywanie starego pluginu v1 i czytelny alarm niezgodności;
- ignorowanie ramek `running == 0`, aby menu lub pauza nie zapisywały fałszywego
  `game_time = 0`;
- osobny monitor diagnostyczny telemetrii.

### Silnik aktywności i reguł

- centralny `TachographEngine` i spójny `TachographSnapshot`;
- automatyczna Jazda po przekroczeniu progu prędkości;
- ręczny wybór Innej pracy, Dyspozycyjności i Odpoczynku na postoju;
- blokada ręcznej zmiany aktywności podczas jazdy;
- liczniki jazdy ciągłej, dziennej, tygodniowej i dwutygodniowej;
- czas pozostały do przerwy oraz odpoczynku;
- przerwa 45 minut i obsługa podziału przerwy;
- odpoczynek dobowy skrócony i regularny, klasyfikowany z faktycznej długości;
- podwójna obsada i 30-godzinne okno;
- specjalna 45-minutowa przerwa kierowcy ze slotu 2 podczas jazdy drugiego kierowcy;
- ręczne tryby OUT i prom;
- katalog naruszeń oraz czytelne prezentowanie ich w monitorze i raportach.

### Dwie karty i obsada

- dwa niezależne sloty kart;
- brak sztucznego przycisku „Zamień kierowców”;
- zmiana kierowcy odbywa się przez fizyczną logikę wyjęcia i włożenia kart do slotów;
- osobna historia i liczniki dla każdej karty;
- menu podglądu liczników obu kart;
- wspólna granica sesji dla obu kart po wczytaniu świata lub cofnięciu czasu;
- brak nieprawidłowego zerowania dziennego licznika drugiej karty.

## 4. Czas gry, skoki i cofnięcia

Historia została zaprojektowana pod nietypowe zachowanie `game_time` w ETS2.

- cofnięcie czasu tworzy nową sesję historii;
- wcześniejsza historia przed punktem cofnięcia zostaje zachowana;
- porzucona, nakładająca się przyszłość jest odcinana tylko w projekcji kanonicznej;
- nowa sesja jest następnie dołączana do historii logicznej;
- granica nowej sesji jest zapisywana atomowo dla obu kart;
- rekordy są idempotentne po znaczącym kluczu `ActivitySessionId + StartGameMinute`;
- konflikt tej samej minuty jest logowany zamiast powodować crash aplikacji.

Kluczowa regresja `03:53 + 01:34 = 05:27` jest zabezpieczona testem. Historia nie
traci pierwszej części jazdy i nie liczy nakładania dwa razy.

Skoki do przodu są obsługiwane według bezpiecznej polityki:

- skok do 2 minut może zostać zrekonstruowany ostatnią aktywnością;
- duży skok po Jeździe nigdy nie tworzy sztucznej wielogodzinnej Jazdy;
- długi odpoczynek jest rekonstruowany tylko wtedy, gdy pojazd stał przed i po skoku;
- pozostałe przypadki tworzą jawną lukę `ForwardTimeJump`.

## 5. Jawne luki i wpisy manualne

Luka jest osobną encją audytową, a nie aktywnością `Unknown`.

- przyczyny: `ForwardTimeJump`, `CardRemoved` oraz rezerwa na brak telemetrii;
- stan: `Unresolved` lub `Resolved`;
- luka jest przypisana do karty, slotu i konkretnej sesji czasu;
- `CardRemoved` ma pierwszeństwo przed `ForwardTimeJump` dla tej samej karty;
- maksymalnie jedna otwarta luka danej karty występuje w projekcji kanonicznej;
- luki są przycinane razem z historią po cofnięciu czasu;
- nierozliczone luki nie wchodzą do RuleEngine i przerywają ciągłość odpoczynku;
- po rozliczeniu luki sąsiadujące minutowo segmenty odpoczynku mogą połączyć się
  z odpoczynkiem zmierzonym przed wyjęciem lub po ponownym włożeniu karty.

`ManualEntryService.ResolveGap`:

- dopuszcza Przerwę/Odpoczynek, Inną pracę i Dyspozycyjność;
- wymaga pełnego pokrycia luki bez dziur, nakładania i wyjścia poza zakres;
- zapisuje segmenty jako `ManualEntry`;
- zachowuje `SourceGapId`, dzięki czemu raport pokazuje pochodzenie wpisu;
- jest idempotentny dla identycznego ponowienia i głośno zgłasza konflikt treści;
- po zapisie przelicza RuleEngine z całej historii.

Włożenie karty z nierozliczoną luką `CardRemoved` uruchamia obowiązkowy kreator.
`ForwardTimeJump` jest opcjonalny i nie blokuje obsługi tachografu. Ciągły blok
odpoczynku co najmniej 9 godzin w rozliczonej luce może wykonać retroaktywny reset
dobowy na końcu tego bloku. Jeżeli wpis manualny jest odpoczynkiem, zachowuje
ciągłość z bezpośrednio sąsiadującym odpoczynkiem zmierzonym. Inna praca,
Dyspozycyjność, nierozliczona luka lub rzeczywista dziura w minutach przerywają
ciągłość.

Beta.4 naprawia przypadek luki przyciętej przez późniejszą gałąź czasu. Rozliczony
fragment jest materializowany w aktualnej sesji, natomiast źródłowa luka porzuconej
gałęzi pozostaje nietknięta jako ślad audytowy. Rozwiązanie przeżywa restart i jest
zachowywane w eksporcie `.tacho` w schemacie 3.

## 6. Trwałość danych i retencja

Baza SQLite zawiera między innymi:

- profile kierowców i karty;
- sesje aktywności;
- minutowe rekordy aktywności;
- jawne luki aktywności i powiązane wpisy manualne;
- snapshoty ocen regulacyjnych;
- rekordy odpoczynku promowego;
- stan retencji i `highWaterMark`;
- ciepłe bloki historii;
- ustawienia tachografu.

Zaimplementowano repozytoria, Unit of Work oraz transakcje. Granica nowej gałęzi
czasu i partie dotyczące obu kart są zapisywane atomowo. Przed każdą migracją
aplikacja tworzy kopię `tachograph.db.bak.<data>`.

Retencja ma obecnie dwie działające warstwy:

1. **Gorąca:** ostatnie 14 dni gry, minuta po minucie, używana przez RuleEngine.
2. **Ciepła:** starsze dane zwinięte w ciągłe bloki tej samej aktywności. Zmiana
   samego źródła nie dzieli bloku; wtedy źródło ma wartość `Mixed`.

Próg jest zakotwiczony na monotonicznym `highWaterMark`, więc cofnięcie czasu nie
„odmładza” starych danych. Retencja archiwizuje, ale automatycznie niczego nie usuwa.
Warstwa zimna po 365 dniach ma przygotowany hak, lecz nie została jeszcze wdrożona.

## 7. Warstwa aplikacyjna i interfejs

Zrealizowane serwisy aplikacyjne:

- `TachographService`;
- `CrewTachographService`;
- `DriverService`;
- `ExportService` i `ImportService`;
- `ReportService`;
- `DiagnosticLogService`;
- `ManualEntryService`.

Interfejs WPF obejmuje:

- dashboard z realistycznym panelem tachografu;
- ekran LCD z czasem gry, prędkością, kilometrami i aktywnościami;
- interaktywne przyciski 1/2, strzałki, OK i C;
- wkładanie i wyjmowanie kart;
- menu kraju startu i końca;
- historię aktywności i filtrowanie;
- profile kierowców;
- raporty, eksport i ustawienia;
- drukarkę oraz wydruk 24 godzin;
- tryb ręczny i ustawienia progu prędkości.

Nakładki działają niezależnie:

- `Alt+1` — slot S1;
- `Alt+2` — slot S2;
- `Alt+Q` — dodatkowy skrót S1;
- każda nakładka pamięta osobne położenie;
- pozycję można zmieniać przeciągnięciem górnego paska;
- nakładka wyświetla liczniki wybranej karty, bez zbędnego pełnego panelu.

## 8. Raporty, eksport i diagnostyka

- PDF agreguje minuty w czytelne bloki aktywności;
- raport pokazuje czas gry jako dzień i godzinę, a nie surowy numer minuty;
- raport zawiera podsumowania jazdy, pracy i odpoczynku oraz naruszenia;
- surowy CSV pozostaje minuta po minucie do diagnostyki;
- dostępny jest JSON dla VTC/TruckersMP;
- własny format `.tacho` ma sumę kontrolną i obsługuje import oraz eksport;
- schemat `.tacho` w beta.4 ma wersję 3 i zachowuje relację wpisu z luką źródłową;
- raport diagnostyczny ZIP zbiera logi i dane potrzebne do beta-testów;
- lokalne logi są rotowane i przechowywane przez 14 dni.

Eksporty nie są urzędowymi plikami Annex 1C. Stanowią format symulatora i materiał
do raportowania w środowisku ETS2/VTC.

## 9. Zabezpieczenia eksploatacyjne

- named mutex blokuje drugą instancję aplikacji i monitora;
- protokół pluginu jest kontrolowany przy starcie;
- baza jest kopiowana przed migracją;
- dane nie są automatycznie kasowane;
- konflikt zapisu minuty jest raportowany w logu;
- `running == 0` nie zasila historii ani retencji;
- ustawienia i dane kierowców pozostają po ponownym uruchomieniu;
- migracje beta.4 zostały próbnie wykonane od pustej bazy do najnowszego schematu.

## 10. Architektura rozwiązania

| Projekt | Odpowiedzialność |
|---|---|
| `ETS2Tachograph.Core` | Model domenowy, czas gry, timeline i reguła jednej minuty |
| `ETS2Tachograph.Telemetry.Scs` | Odczyt wersjonowanej pamięci współdzielonej |
| `ETS2Tachograph.Engine` | Klasyfikacja ramek, sesje, luki, karty i snapshoty |
| `ETS2Tachograph.RuleEngine` | Liczniki, odpoczynki i naruszenia |
| `ETS2Tachograph.Infrastructure` | SQLite, EF Core, repozytoria, migracje i retencja |
| `ETS2Tachograph.Application` | Przypadki użycia, DTO, import/eksport i wpisy manualne |
| `ETS2Tachograph.Reports` | PDF, prezentacja bloków i eksporty raportowe |
| `ETS2Tachograph.Desktop` | WPF, realistyczny tachograf i nakładki |
| `ETS2Tachograph.ScsPlugin` | Natywny plugin telemetryczny C++ dla ETS2 x64 |

## 11. Testy i jakość

Końcowy przebieg Release beta.4:

| Zestaw | Liczba testów |
|---|---:|
| Core | 29 |
| RuleEngine | 33 |
| Engine | 54 |
| Application | 32 |
| Infrastructure | 29 |
| Reports | 8 |
| Telemetry SCS | 8 |
| **Łącznie** | **193** |

Zakres obejmuje testy jednostkowe, integracyjne i regresyjne, między innymi:

- mock pamięci współdzielonej telemetrii;
- cofnięcia czasu i `world_generation`;
- atomową granicę obu kart;
- idempotentny zapis rekordów;
- regułę jednej minuty;
- przerwy, odpoczynki i podwójną obsadę;
- lukę `CardRemoved` i priorytet nad skokiem czasu;
- wpisy manualne i retroaktywny reset dobowy;
- przyciętą lukę kanoniczną beta.4;
- restart silnika po jej rozliczeniu;
- SQLite, retencję, import/eksport i raport PDF.

Aplikacja WPF kompiluje się w Release bez ostrzeżeń. Dodatkowo przygotowano manualny
plan beta.4 obejmujący dokładnie scenariusz błędu z beta.3.

## 12. Znane ograniczenia

- aplikacja i plugin działają tylko na Windows x64;
- po podmianie DLL należy ponownie uruchomić ETS2;
- aplikacja nie może fizycznie zatrzymać ciężarówki, ponieważ telemetria SCS jest
  tylko do odczytu;
- rekompensaty skróconych odpoczynków tygodniowych mają uproszczony model;
- prom, kraj startu i kraj końca są wybierane ręcznie;
- scenariusze kolejowe zostały świadomie pominięte;
- zimna retencja po 365 dniach nie jest wdrożona;
- nie ma jeszcze kontrolki świadomego, trwałego usuwania starej historii;
- surowy CSV może być bardzo duży;
- skróty nakładek mogą kolidować z innymi aplikacjami;
- aktualizacja aplikacji i pluginu jest ręczna;
- brak podpisu kodu, instalatora i automatycznej aktualizacji;
- formaty raportowe nie są certyfikowanym Annex 1C.

## 13. Ocena gotowości i następny krok

**Gotowe:** zamknięta technicznie beta.4, pełny pakiet dystrybucyjny, zielone testy,
migracje, backup, logowanie i plan testów terenowych.

**Przed publicznym wydaniem:** należy wykonać dłuższe testy beta.4 na istniejącej
bazie, szczególnie wpis po przyciętej luce, restart po rozliczeniu, cofnięcie czasu
po wpisie oraz sesję dwóch kart. Każdy błąd powinien być zgłaszany z raportem
diagnostycznym ZIP. Po zamknięciu błędów blokujących można przygotować tag wydania,
instalator i publikację.

## 14. Artefakty beta.4

- paczka: `output/releases/ETS2Tachograph-0.1.0-beta.4-win-x64.zip`;
- suma SHA-256:
  `460319B245B55DAA73AD25EFF9354C9C998A65A8AE32D4EF0E8D9DE704EF5395`;
- plan testów: `BETA_TEST_PLAN.md`;
- dokumentacja użytkownika: `README.md`;
- ograniczenia: `KNOWN_ISSUES.md`;
- historia wydania: `RELEASE_NOTES.md`.
