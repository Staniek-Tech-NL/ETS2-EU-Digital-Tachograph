# Pakiet startowy projektu — ETS2 EU Digital Tachograph

**Stan na:** 22 lipca 2026  
**Obowiązująca wersja testowa:** `0.1.0-beta.10.1`  
**Przeznaczenie:** przekazanie kompletnego kontekstu do nowej sesji AI bez ponownego analizowania całej rozmowy  
**Źródło ustaleń:** dotychczasowa rozmowa projektowa i powstała w niej dokumentacja

---

## 1. Streszczenie projektu

ETS2 EU Digital Tachograph jest desktopowym symulatorem europejskiego tachografu cyfrowego dla Euro Truck Simulator 2. Aplikacja działa na Windows x64, odczytuje oficjalną telemetrię SCS SDK 1.14 przez natywny plugin C++, prowadzi historię aktywności w czasie gry (`game_time`) i wylicza liczniki kierowcy na podstawie tej historii. Projekt nie korzysta z czasu systemowego jako podstawy reguł.

Użytkownikiem jest gracz ETS2 oczekujący realistycznej obsługi jednej lub dwóch kart kierowców, ręcznych aktywności, przerw, odpoczynków, podwójnej obsady, raportów i nakładki nad grą. Projekt ma też wartość dla VTC: generuje czytelne PDF-y, surowy CSV do diagnostyki, JSON VTC oraz własny format `.tacho`.

Najważniejszym problemem domenowym jest nietypowy zegar ETS2. `game_time` przyspiesza względem czasu realnego, potrafi przeskakiwać do przodu podczas snu, `g_set_time`, załadunku i rozładunku, a także cofać się po wczytaniu zapisu lub korekcie pozycji. System musi zachować materiał dowodowy, nie liczyć tych samych minut podwójnie i nie wymyślać aktywności dla okresów bez danych.

Projekt przeszedł etapy: model domenowy, telemetria, RuleEngine, integracja Engine, SQLite/EF Core, Application, raporty, realistyczne WPF, dwie karty, nakładki, luki i wpisy manualne, retencja hot/warm oraz wielodniowe beta-testy. Aktualne wydanie to `0.1.0-beta.10.1`. Hotfix usunął blokujący start błąd nakładania rekordów kanonicznych. Zestaw automatyczny ma 239 zielonych testów, a build Release nie zgłasza błędów ani ostrzeżeń.

Projekt nie jest jednak bezwarunkowo gotowy do szerokiej publikacji. Najnowszy potwierdzony problem dotyczy licznika pauzy w UI: może pokazać `45:00 / ZALICZONA`, gdy po zastosowaniu reguły jednej minuty historia regulacyjna zawiera dopiero 44 minuty odpoczynku. RuleEngine działa wtedy poprawnie i nie zeruje jazdy ciągłej; myląca jest prezentacja. Poprawka tego rozjazdu nie została jeszcze wdrożona.

---

## 2. Aktualny zakres projektu

### 2.1. Funkcje podstawowe

- **[GOTOWE]** Odczyt oficjalnej telemetrii ETS2 przez SCS SDK 1.14 i pamięć współdzieloną.
- **[GOTOWE]** Wersjonowany protokół telemetrii v3 z `world_generation` i `cargo_operation_generation`.
- **[GOTOWE]** Prosty model profilu, karty kierowcy, sesji, rekordów minutowych i osi czasu.
- **[GOTOWE]** Automatyczna Jazda dla slotu 1 po przekroczeniu progu prędkości.
- **[GOTOWE]** Ręczny wybór na postoju: Przerwa/Odpoczynek, Inna praca, Dyspozycyjność.
- **[GOTOWE]** RuleEngine: jazda ciągła, przerwa 45 min i 15+30, jazda dzienna, tygodniowa i dwutygodniowa, odpoczynki dobowe i tygodniowe, naruszenia.
- **[GOTOWE]** Dynamiczny limit jazdy dziennej 9/10 h oraz licznik wykorzystanych wydłużeń w tygodniu regulacyjnym.
- **[GOTOWE]** Licznik skróconych odpoczynków dobowych od ostatniego odpoczynku tygodniowego.
- **[W TRAKCIE]** Rekompensaty skróconych odpoczynków tygodniowych; istnieje działająca projekcja i UI, ale model jest uproszczony i zaniża dług.
- **[GOTOWE]** Dwa sloty kart, podwójna obsada i okno dobowe 30 h.
- **[GOTOWE]** Specjalna 45-minutowa przerwa kierowcy w slocie 2 podczas jazdy slotu 1; nie buduje odpoczynku dobowego.
- **[GOTOWE]** Zmiana kierowców przez wyjęcie i ponowne włożenie kart do slotów.
- **[ODRZUCONE]** Osobny przycisk „Zamień kierowców”.
- **[GOTOWE]** Tryby OUT i prom; prom jest włączany ręcznie.
- **[ODRZUCONE]** Modelowanie pociągów.
- **[GOTOWE]** Ręczny wybór kraju rozpoczęcia i zakończenia pracy.
- **[GOTOWE]** Obsługa snu, `g_set_time`, cofnięć zegara, wczytania zapisu, pauzy oraz operacji ładunkowych według zasad z sekcji 6.
- **[GOTOWE]** `TachographSnapshot` jako spójny wynik `TachographEngine.ProcessFrame()`.
- **[GOTOWE]** `CrewTachographEngine` koordynujący dwie karty i wspólną zmianę gałęzi czasu.
- **[DO ZROBIENIA]** Poprawa prezentacyjnego licznika pauzy, aby nie wyprzedzał zatwierdzonej historii o minutę.

### 2.2. Funkcje dodatkowe

- **[GOTOWE]** Nakładki nad grą: `Alt+1` dla S1, `Alt+2` dla S2, dodatkowo `Alt+Q` dla S1.
- **[GOTOWE]** Niezależne przeciąganie i zapamiętywanie pozycji nakładek S1/S2.
- **[GOTOWE]** Raport diagnostyczny ZIP z logami i podsumowaniem stanu.
- **[GOTOWE]** Import/eksport własnego formatu `.tacho` z zachowaniem śladu wpisów manualnych.
- **[GOTOWE]** Eksport PDF, surowy CSV i JSON VTC.
- **[GOTOWE]** Czytelne bloki aktywności w PDF zamiast wiersza na każdą minutę.
- **[GOTOWE]** Ostrzeżenie o nierozliczonych lukach i bilans kompletności raportu.
- **[GOTOWE]** Wydruk z menu tachografu oraz katalog `Printouts`.
- **[DO ZROBIENIA]** Brak potwierdzenia pełnej animacji wysuwania papieru z drukarki termicznej.
- **[ODRZUCONE]** Reguła pierwszej godziny dołączenia drugiego kierowcy w multi-manning; została świadomie wyjęta z zakresu.
- **[ODRZUCONE]** Rozpoznawanie dzielonego odpoczynku dobowego 3 h + 9 h w obecnej wersji.
- **[DO ZROBIENIA]** Planer podróży MVP „Najwcześniejsza legalna”; istnieje plan, implementacja nie została rozpoczęta.

### 2.3. Wymagania techniczne

- **[GOTOWE]** .NET 9, C#, WPF, SQLite i Entity Framework Core.
- **[GOTOWE]** Natywny plugin C++ `Release|x64` dla SCS Telemetry SDK.
- **[GOTOWE]** Self-contained publikacja `win-x64` w ZIP-ie.
- **[GOTOWE]** Named mutex wykrywający drugą instancję aplikacji/monitora.
- **[GOTOWE]** Alarm niezgodnej wersji protokołu pluginu przy starcie.
- **[GOTOWE]** Ignorowanie `running == 0` przez zapis historii i `highWaterMark`.
- **[GOTOWE]** Backup SQLite przed migracją.
- **[GOTOWE]** Transakcyjne zapisy granicy sesji i partii rekordów.
- **[GOTOWE]** Idempotentność po kluczu `ActivitySessionId + StartGameMinute`, a nie po losowym GUID.
- **[GOTOWE]** Kanoniczna projekcja bez nakładek: `SubtractCoveredRanges`, `EnsureNoOverlap`, `InvalidCanonicalHistoryException`.
- **[DO ZROBIENIA]** Logowanie pełnego łańcucha `InnerException` przy `APP_START_FAILED`.
- **[DO ZROBIENIA]** Instalator, podpis kodu i automatyczne aktualizacje.

### 2.4. Wymagania dotyczące interfejsu

- **[GOTOWE]** WPF z zachowanym lewym panelem nawigacji.
- **[GOTOWE]** Realistyczna obudowa tachografu, LCD, przyciski 1/2, góra/dół/OK/C i sloty kart.
- **[GOTOWE]** Klikalne urządzenie z menu, wyborem aktywności, trybów, krajów i liczników obu kart.
- **[GOTOWE]** Dashboard, Historia, Raporty, Kierowcy i Ustawienia.
- **[GOTOWE]** Wkładanie i wyjmowanie kart, komunikat powitalny oraz obowiązkowy kreator po `CardRemoved`.
- **[GOTOWE]** Blokada ręcznej zmiany aktywności slotu 1 podczas jazdy.
- **[GOTOWE]** Główny LCD pokazuje czas gry, nie czas Windows.
- **[GOTOWE]** UI pokazuje pracę dobową, wydłużenia, skrócone odpoczynki i rekompensaty.
- **[GOTOWE]** Przekroczenia są pokazywane jako fakt, np. `3 / 2`, bez przycinania do limitu.
- **[GOTOWE]** Osobne etykiety horyzontów: tydzień regulacyjny oraz „od odpoczynku tygodniowego”.
- **[GOTOWE]** Checklista regresji UI po każdej zmianie `MainWindow.xaml`, `OverlayWindow.xaml` lub `App.xaml`.
- **[DO ZROBIENIA]** Licznik pauzy UI musi korzystać z faktycznie zakwalifikowanych minut, nie tylko różnicy od chwili kliknięcia.

### 2.5. Wymagania dotyczące danych

- **[GOTOWE]** Jednostką atomową jest jedna minuta gry.
- **[GOTOWE]** Historia minutowa jest źródłem prawdy; liczniki są z niej wyliczane.
- **[GOTOWE]** Model `truncate-and-append` zachowuje wcześniejsze gałęzie i buduje projekcję kanoniczną.
- **[GOTOWE]** Jawna encja `ActivityGap`, a nie aktywność `Unknown`.
- **[GOTOWE]** `SourceGapId` łączy wpis manualny z pierwotną luką.
- **[GOTOWE]** Hot retention: ostatnie 14 dni gry minutowo.
- **[GOTOWE]** Warm retention: starsze rekordy jako bloki, zmiana samego źródła daje `Mixed`.
- **[DO ZROBIENIA]** Cold retention: podsumowanie dobowe starsze niż 365 dni; istnieje tylko hak.
- **[GOTOWE]** Próg retencji liczony od monotonicznego `highWaterMark`.
- **[GOTOWE]** Brak automatycznego kasowania danych.
- **[DO ZROBIENIA]** Świadoma akcja „Wyczyść historię starszą niż X dni gry” z potwierdzeniem.

### 2.6. Wymagania prawne lub biznesowe

- **[GOTOWE]** Aplikacja jest opisana jako symulator, nie certyfikowany tachograf.
- **[ODRZUCONE]** Deklarowanie obecnych eksportów jako urzędowo zgodnych z Annex 1C.
- **[GOTOWE]** Reguły odwołują się do zaimplementowanego zakresu rozporządzenia 561/2006, ale known issues jawnie opisują uproszczenia.
- **[DO DECYZJI]** Model publikacji repozytorium.
- **[DO DECYZJI]** Licencja i model komercjalizacji/supportu.

---

## 3. Najważniejsze ustalenia i decyzje

| Obszar | Podjęta decyzja | Powód | Konsekwencje |
|---|---|---|---|
| Koncepcja domeny | Bazą jest poprawiona koncepcja 3; prosty model karty z koncepcji 1; katalog naruszeń dopiero w RuleEngine | Oddzielenie danych kierowcy od obliczeń regulacyjnych | Model kierowcy nie przechowuje liczników |
| Pociągi | Pominięte | W ETS2 brak użytecznego scenariusza | Zakres obejmuje drogę i ręczny prom |
| UI desktop | WPF/MVVM zamiast Avalonia | Najszybsza droga do natywnego Windows UI i nakładek | Projekt `ETS2Tachograph.Desktop`, Windows x64 |
| Telemetria | Oficjalne SCS SDK 1.14, plugin C++ i shared memory | Stabilne źródło danych z gry | Wymagana DLL w `bin\win_x64\plugins` |
| Czas | Wyłącznie `game_time` | Skala ETS2 i skoki nie odpowiadają czasowi realnemu | Wszystkie rekordy i raporty używają minut gry |
| Liczniki | Wyliczane z historii | Możliwość rekalkulacji po błędzie lub zmianie reguł | Brak trwałych liczników w modelu kierowcy |
| Cofnięcie czasu | Sesje + `truncate-and-append` | Wczytanie zapisu nie może niszczyć źródła | Porzucona gałąź zostaje, projekcja odcina nakładającą się przyszłość |
| Granica sesji | Jedno wyzwolenie dla obu kart; `StartNewSession()` centralizuje indeks/GUID | `game_time` jest wspólny dla załogi | Obie osie czasu rozgałęziają się spójnie |
| Idempotentność | `ActivitySessionId + StartGameMinute` | Losowy `ActivityRecord.Id` nie wykrywał duplikatu treści | Powtórka jest cicha, konflikt logowany |
| Kanonizacja beta.10.1 | Nowa sesja poniżej kotwicy dopisuje tylko niepokryte fragmenty | Odrzucenie wszystkiego przed kotwicą usunęłoby poprawny backfill | Brak podwójnych minut bez utraty wpisów manualnych |
| Retencja | Hot 14 dni / warm bloki / cold jako hak | Wydajność przy zachowaniu źródła | RuleEngine czyta hot; raporty mogą czytać całość |
| Kasowanie | Nigdy automatycznie | Raporty VTC są materiałem dowodowym | Ewentualne usuwanie tylko świadomą akcją |
| Luki | `ActivityGap` z `GapReason` | Brak danych nie jest aktywnością | Oddzielone od RuleEngine do chwili rozliczenia |
| Priorytet luk | `CardRemoved > ForwardTimeJump` per karta | Wyjęta karta jest silniejszą przyczyną niewiedzy | Skok w trakcie wyjęcia nie tworzy drugiej luki |
| ManualEntry | Dozwolone: odpoczynek, praca, dyspozycyjność; bez Jazdy/OUT/Unknown | Jazdy nie deklaruje się, OUT odłożono | Pełne pokrycie luki i trwały audyt |
| Polityka wymuszenia | `CardRemoved` blokuje logicznie; `ForwardTimeJump` jest opcjonalny | Skok czasu może być niewiarygodny | Raport może pozostać niekompletny |
| Wyjęcie karty a odpoczynek | Najnowsza decyzja: rozliczony odpoczynek może łączyć się z odpoczynkiem przed/po luce | Koniec zapisu nie oznacza końca czynności | `2 h + 7 h = 9 h`; działa też tygodniowo |
| Zmiana kierowców | Wyłącznie przez wyjęcie i włożenie kart | Realizm urządzenia | Brak przycisku „Zamień kierowców” |
| Slot 2 w ruchu | Domyślnie Dyspozycyjność; tylko dedykowana przerwa 45 min | Drugi kierowca nie buduje odpoczynku dobowego w jadącym pojeździe | Osobna logika w `CrewTachographEngine` |
| Cel pauzy w UI | Cel jest prezentacyjny; klasyfikacja wynika z faktycznej długości | Nie można uznać 30 h za 45 h przez wybór menu | UI pokazuje „wybrany cel” i „zakwalifikowano” |
| Skoki czasu | ≤2 min rekonstruowane; duża Jazda = luka; długi odpoczynek tylko przy postoju przed/po; cargo według znacznika | Nie tworzyć naruszeń z powietrza | Potrzebny stan prędkości sprzed skoku |
| Protokół | v2 dodał `world_generation`; v3 dodał `cargo_operation_generation` | Rozpoznawanie wczytania świata i ładunku | Aplikacja i plugin muszą mieć zgodną wersję |
| Raporty | Dysk = minuty, PDF = bloki, CSV = minuty | Czytelność bez utraty diagnostyki | PDF agreguje po aktywności, źródło mieszane nie rozcina |
| Rekompensata | Baza 9 h, nie 11 h; pełny model en bloc odłożony | Naprawiono konkretny błąd bez przepisywania całej warstwy | Obecny model nadal może zaniżać dług |
| Reguła pierwszej godziny multi-manning | Odrzucona | Duża złożoność względem zakresu | Nie implementować bez nowej decyzji |
| Dystrybucja | ZIP self-contained, aktualizacja ręczna | Beta lokalna | Instalator/podpis/auto-update później |

---

## 4. Aktualna architektura

### 4.1. Komponenty

- `ETS2Tachograph.Core` — encje domenowe, `GameTime`, `GameClockFormatter`, `ActivityTimeline`, reguła jednej minuty.
- `ETS2Tachograph.Telemetry.Scs` — odczyt shared memory i walidacja protokołu v3.
- `ETS2Tachograph.Engine` — `TelemetryProcessor`, `ActivityHistoryProcessor`, `TachographEngine`, `CrewTachographEngine`, snapshoty, sesje i luki.
- `ETS2Tachograph.RuleEngine` — `RegulationEngine`, stan liczników, naruszenia, kwalifikowane odpoczynki i rekompensaty.
- `ETS2Tachograph.Infrastructure` — `TachographDbContext`, repozytoria, migracje, transakcje, backup, kanonizacja i retencja.
- `ETS2Tachograph.Application` — serwisy przypadków użycia, DTO, import/eksport, raporty, luki i wpisy manualne.
- `ETS2Tachograph.Reports` — budowa prezentacji oraz eksport PDF.
- `ETS2Tachograph.Desktop` — WPF/MVVM, urządzenie, dashboard, historia, raporty i nakładki.
- `native/ETS2Tachograph.ScsPlugin` — plugin C++ SCS x64.

### 4.2. Przepływ danych

```text
ETS2
  → plugin.cpp / SCS SDK 1.14
    → Local\ETS2Tachograph.Telemetry.v3
      → ScsMemoryMappedTelemetryReader
        → TelemetryProcessor / ActivityHistoryProcessor
          → ActivityRecord + ActivityGap + sesje
            → SQLite / EF Core
              → projekcja kanoniczna + retencja
                → RegulationEngine
                  → TachographSnapshot / DTO
                    → WPF, nakładki, PDF, CSV, JSON, .tacho
```

### 4.3. Odpowiedzialności i zależności

Plugin publikuje wyłącznie dane gry i generacje zdarzeń. Nie zna kart ani reguł. Warstwa Telemetry mapuje pamięć współdzieloną na ramkę. Engine wybiera aktywność, tworzy minuty, sesje i luki. Infrastructure zapisuje źródło i buduje kanoniczny widok. RuleEngine nie modyfikuje historii; wylicza stan z przekazanych rekordów. Application spina przypadki użycia. Desktop i Reports tylko prezentują wynik.

Przy cofnięciu czasu zamykane rekordy pozostają przypisane do starej sesji, a nowa sesja dostaje wspólną granicę dla obu kart. W beta.10.1 rekord przychodzący poniżej kotwicy sesji jest dzielony na niepokryte fragmenty. Istniejąca historia kanoniczna ma pierwszeństwo; manualny backfill uzupełnia tylko brakujące minuty.

SQLite przechowuje profile, karty, sesje, rekordy, luki, snapshoty regulacyjne, prom, ustawienia, stan retencji i bloki warm. Backup jest wykonywany przed migracją. Zapis granicy sesji i partii rekordów ma być atomowy.

Integracja zewnętrzna ogranicza się do ETS2. JSON VTC jest eksportem pliku, nie aktywnym połączeniem z TruckersMP/VTC. SCS telemetry jest tylko do odczytu, więc blokada jazdy przy nierozliczonej karcie nie może fizycznie zatrzymać ciężarówki.

---

## 5. Struktura projektu i pliki

```text
ETS2 EU Digital Tachograph/
├── src/
│   ├── ETS2Tachograph.Core/
│   │   ├── Entities/ActivityGap.cs
│   │   ├── Entities/ActivityRecord.cs
│   │   ├── OneMinuteRule/
│   │   └── Time/GameClockFormatter.cs
│   ├── ETS2Tachograph.Telemetry.Scs/
│   ├── ETS2Tachograph.Engine/
│   │   ├── ActivityHistoryProcessor.cs
│   │   ├── CrewTachographEngine.cs
│   │   ├── TachographEngine.cs
│   │   ├── ITachographEngine.cs
│   │   └── TachographSnapshot.cs
│   ├── ETS2Tachograph.RuleEngine/
│   │   └── RegulationEngine.cs
│   ├── ETS2Tachograph.Infrastructure/
│   │   └── Persistence/
│   │       ├── TachographDbContext.cs
│   │       ├── Repositories.cs
│   │       ├── RegulationReportAnalyzer.cs
│   │       └── Migrations/
│   ├── ETS2Tachograph.Application/
│   │   ├── Dtos/ActivityGapDtos.cs
│   │   ├── Dtos/ManualEntryDtos.cs
│   │   ├── Dtos/ReportDto.cs
│   │   └── Services/
│   │       ├── ActivityGapService.cs
│   │       ├── ManualEntryService.cs
│   │       ├── ManualEntryWizardDraft.cs
│   │       └── ReportService.cs
│   ├── ETS2Tachograph.Reports/
│   │   ├── PdfReportExporter.cs
│   │   └── ReportPresentationBuilder.cs
│   └── ETS2Tachograph.Desktop/
│       ├── App.xaml
│       ├── MainWindow.xaml
│       ├── OverlayWindow.xaml
│       └── ViewModels/
│           ├── MainViewModel.cs
│           └── OverlayViewModel.cs
├── native/
│   └── ETS2Tachograph.ScsPlugin/
│       ├── plugin.cpp
│       ├── telemetry_protocol.h
│       └── ETS2Tachograph.ScsPlugin.vcxproj
├── tests/
│   ├── ETS2Tachograph.Core.Tests/
│   ├── ETS2Tachograph.Telemetry.Scs.Tests/
│   ├── ETS2Tachograph.Engine.Tests/
│   ├── ETS2Tachograph.RuleEngine.Tests/
│   ├── ETS2Tachograph.Application.Tests/
│   ├── ETS2Tachograph.Infrastructure.Tests/
│   │   └── CanonicalProjectionTests.cs
│   └── ETS2Tachograph.Reports.Tests/
├── docs/
│   ├── PROJECT_STARTER_PACK_2026-07-22.md
│   ├── PROJECT_HANDOFF.md
│   ├── DAILY_WORK_REPORT_2026-07-18.md
│   ├── FIELD_TEST_REPORT_2026-07-21.md
│   ├── BUGFIX_REPORT_CANONICAL_HISTORY_2026-07-21.md
│   ├── JOURNEY_PLANNER_MVP_PLAN.md
│   ├── PRODUCTION_STATUS_REPORT_BETA4.md
│   └── UI_VISIBLE_DATA_REPORT_BETA4.md
├── output/releases/
│   └── ETS2Tachograph-0.1.0-beta.10.1-win-x64.zip
├── BETA_TEST_PLAN.md
├── KNOWN_ISSUES.md
├── RELEASE_NOTES.md
└── README.md
```

### 5.1. Najważniejsze pliki

| Plik | Przeznaczenie i obecna zawartość | Co pozostaje | Powiązania |
|---|---|---|---|
| `ActivityHistoryProcessor.cs` | Ramki → minuty, skoki czasu, sesje, luki, cargo, stan sprzed pauzy | Chronić regresje przy każdej zmianie rekonstrukcji | `TelemetryFrame`, `ActivityTimeline`, `ActivityHistoryProcessorTests` |
| `CrewTachographEngine.cs` | Dwa sloty, multi-manning, slot 2 w ruchu, wspólna granica czasu | Bez reguły pierwszej godziny | `TachographEngine`, `CrewTachographEngineTests` |
| `TachographSnapshot.cs` | Spójny stan zwracany do Application/UI | Ewentualne nowe projekcje muszą przechodzić przez snapshot | `TachographEngine`, `MainViewModel` |
| `RegulationEngine.cs` | Liczniki, odpoczynki, tygodnie, rekompensaty | Pełny model rekompensaty en bloc | `RegulationState`, `ManualGapDailyResetTests`, raporty |
| `Repositories.cs` | Repozytoria, projekcja kanoniczna, warm retention | Logowanie dodatkowego kontekstu sesji opcjonalne | `TachographDbContext`, `CanonicalProjectionTests` |
| `TachographDbContext.cs` | Mapowanie SQLite/EF Core | Cold retention wymaga przyszłego modelu | migracje i repozytoria |
| `ManualEntryService.cs` | `ResolveGap`, walidacja typów i pokrycia, idempotentność | Wiele luk to osobna decyzja domenowa | DTO, repozytorium, Engine |
| `ActivityGapService.cs` | Kanoniczne zapytania o luki do UI i raportów | Brak | `ActivityGapDtos`, Historia, ReportService |
| `ReportService.cs` | Zakres raportu, kompletność, eksporty | Pełna zgodność Annex 1C nie jest celem obecnej wersji | Reports, repozytoria |
| `PdfReportExporter.cs` | Czytelny PDF blokowy | Utrzymywać bilans luk i aktywności | `ReportPresentationBuilder` |
| `MainViewModel.cs` | Główne komendy, bindingi, urządzenie, liczniki pauz | **Naprawić off-by-one licznika pauzy**; obecnie start od chwili kliknięcia | Engine snapshot, OverlayViewModel |
| `OverlayViewModel.cs` | Projekcja danych S1/S2 do nakładek | Po poprawce pauzy ma czytać ten sam stan co Dashboard | `MainViewModel`, `OverlayWindow.xaml` |
| `plugin.cpp` | Rejestracja SCS API, ramki, restart świata i cargo | Plugin v3 stabilny; nie zmieniać przy poprawce UI | `telemetry_protocol.h`, Telemetry.Scs |
| `CanonicalProjectionTests.cs` | 14 regresji nakładek, backfillu i cięcia rekordów | Rozszerzać przy nowych klasach konfliktów | `Repositories.cs` |
| `BETA_TEST_PLAN.md` | Testy funkcjonalne i checklista XAML | Tytuł nadal odnosi się do beta.10; zaktualizować po następnym wydaniu | UI, release process |
| `KNOWN_ISSUES.md` | Ograniczenia telemetrii, prawa, retencji i dystrybucji | Dopisać licznik pauzy UI, jeżeli poprawka nie wejdzie od razu | README i release notes |
| `JOURNEY_PLANNER_MVP_PLAN.md` | Plan następnego dużego modułu | Implementacja dopiero po zamknięciu gate’u tachografu | przyszłe projekty Application/RuleEngine/UI |

---

## 6. Reguły działania systemu

| Reguła | Dokładny opis | Warunek uruchomienia | Oczekiwany rezultat | Wyjątki i przypadki brzegowe | Status |
|---|---|---|---|---|---|
| Reguła jednej minuty | Minuta otrzymuje najdłuższą ciągłą aktywność; przy remisie wygrywa późniejsza | Zmiana aktywności wewnątrz minuty | Jeden rekord na minutę | Minuta kliknięcia pauzy może pozostać poprzednią aktywnością | GOTOWE; UI nie jest jeszcze w pełni zsynchronizowane |
| Jazda automatyczna | Slot 1 przechodzi na Jazdę powyżej progu prędkości | Pojazd rusza | Ręczna zmiana zablokowana, zapis Jazdy | Pauza/menu nie generuje jazdy | GOTOWE |
| Przerwa 45 min | Ciągłe 45 min zeruje jazdę ciągłą | Blok BreakOrRest ≥45 | `ContinuousDrivingMinutes = 0` | 44 min nie zeruje; po przerwaniu nie można dodać jednej minuty | GOTOWE |
| Przerwa dzielona | Najpierw ≥15 min, potem ≥30 min | Dwa bloki w tej kolejności | Drugi blok zeruje jazdę ciągłą | 44 min przerwane pracą liczy się jako pierwsza część; potrzeba kolejnych 30 min | GOTOWE |
| Odpoczynek dobowy | 9–<11 h skrócony, ≥11 h regularny | Nieprzerwany blok odpoczynku | Reset dobowy na końcu bloku | Jazda, praca i dyspozycyjność przerywają | GOTOWE |
| Odpoczynek tygodniowy | 24–<45 h skrócony, ≥45 h regularny | Faktyczna długość bloku | Klasyfikacja tygodniowa | Cel UI 24/45 h nie zmienia klasyfikacji | GOTOWE |
| Skrócone dobowe | Maksymalnie 3 od ostatniego tygodniowego | Kolejne bloki 9–<11 h | Licznik i naruszenie po przekroczeniu | Reset nie jest związany z granicą tygodnia kalendarzowego | GOTOWE |
| Wydłużenia dzienne | Przekroczenie 9:00 zużywa jedno z 2 wydłużeń | Jazda dzienna >540 min | Limit dynamiczny 10 h | Dokładnie 9:00 nie zużywa wydłużenia; reset na granicy tygodnia regulacyjnego | GOTOWE |
| Praca dobowa | Jazda + Inna praca od ostatniego resetu | Aktywności bieżącego okresu dobowego | Prezentacja względem 13 h | Dyspozycyjność nie obciąża, ale przerywa odpoczynek | GOTOWE |
| Multi-manning | Dwie karty dają okno 30 h | Oba sloty zajęte | Dłuższy deadline odpoczynku | Reguła pierwszej godziny odrzucona | GOTOWE w ustalonym zakresie |
| Slot 2 w ruchu | Normalnie Dyspozycyjność; dedykowana przerwa max 45 min | Slot 1 jedzie | Krótka przerwa może wyzerować jazdę ciągłą karty 2 | Nie buduje odpoczynku dobowego/tygodniowego | GOTOWE |
| Mały skok czasu | Rekonstrukcja ostatnią aktywnością | Skok ≤2 min | Brak luki | Może obejmować Jazdę, bo traktowany jako opóźnienie telemetrii | GOTOWE |
| Duży skok po Jeździe | Nie rekonstruować Jazdy | Skok >2 min, poprzednia Jazda | `ForwardTimeJump` | Potwierdzone cargo jest osobną gałęzią | GOTOWE |
| Duży skok przy odpoczynku | Rekonstrukcja odpoczynku tylko przy postoju przed i po | Skok >2 min, BreakOrRest | Odpoczynek zamiast luki | Slot 2 w jadącym pojeździe nie spełnia warunku | GOTOWE |
| Cargo | Zachować aktywność wybraną przed operacją | Zmiana `cargo_operation_generation` | Rekonstrukcja bez luki | Stan sprzed `GamePaused` musi być zachowany | GOTOWE od beta.9 |
| Cofnięcie czasu | Nowa gałąź dla obu kart | `world_generation`, timer restart lub zegar wstecz | Truncate-and-append | Źródło starej gałęzi nie jest usuwane | GOTOWE |
| Kanonizacja | Każda minuta najwyżej raz | Składanie sesji/backfill | Niepokryte fragmenty zostają | Półotwarte zakresy `[Start,End)`; konflikt wykrywany przed SQLite | GOTOWE beta.10.1 |
| CardRemoved | Otwarta luka od minuty wyjęcia | `CARD_EJECTED` | Unresolved do ponownego włożenia | Włożenie w tej samej minucie odrzuca lukę zerową | GOTOWE |
| Priorytet luki | CardRemoved pochłania skok per karta | Skok przy wyjętej karcie | Jedna luka CardRemoved | Druga karta w slocie może dostać ForwardTimeJump | GOTOWE |
| Cofnięcie przed lukę | Luka starej gałęzi znika z projekcji | Nowa kotwica przed początkiem | Nowa luka tylko gdy karta nadal wyjęta | Surowy rekord starej gałęzi zostaje | GOTOWE |
| ResolveGap | Segmenty dokładnie pokrywają domkniętą lukę | Użytkownik zatwierdza wpis | ManualEntry + Resolved + ślad audytowy | Jazda, OUT i Unknown odrzucane; powtórka idempotentna | GOTOWE |
| Ciągłość przez lukę | Manualny odpoczynek łączy stykające się bloki | Rozliczona luka i brak dziury minutowej | Jeden blok dobowy/tygodniowy | Wiele różnych luk w jednym bloku jest nierozstrzygnięte | GOTOWE dla pojedynczej luki |
| Raport luk | Jawna informacja o kompletności | Zakres raportu zawiera Unresolved | Ostrzeżenie w UI/PDF/JSON | Nie blokuje eksportu | GOTOWE |
| Retencja | Hot→warm przy starcie przed RuleEngine | `highWaterMark - 20160` | Starsze minuty agregowane bez usuwania | Cofnięcie nie odmładza danych; cold brak | GOTOWE hot/warm |
| Pauza/menu | Nie zapisuj czasu realnego | `running == 0` / `GamePaused` | Brak fałszywych minut | Generacja zdarzenia jest obsługiwana na pierwszej aktywnej ramce | GOTOWE |

---

## 7. Co zostało już wykonane

### 7.1. Moduły i funkcje

- Model domenowy z prostą kartą, `ActivityRecord`, `ActivityTimeline`, sesjami i `ActivityGap`.
- Telemetria SCS: plugin v1→v2→v3, seqlock, `world_generation`, `cargo_operation_generation`, czytnik shared memory i monitor.
- `TelemetryProcessor`, `ActivityHistoryProcessor`, `TachographEngine`, `CrewTachographEngine`, `TachographSnapshot`.
- RuleEngine oraz projekcje do UI: jazda ciągła/dzienna/tygodniowa/dwutygodniowa, praca dobowa, odpoczynki, multi-manning, prom, wydłużenia, skrócone dobowe, rekompensaty.
- SQLite/EF Core, migracje, repozytoria, Unit of Work/transakcje, backup przed migracją, automatyczny zapis zamkniętych minut i odtwarzanie po restarcie.
- Warstwowa retencja hot/warm z `highWaterMark`.
- Application: serwisy tachografu, kierowców, importu/eksportu, raportu, luk i wpisów manualnych, DTO.
- WPF: realistyczne urządzenie, dashboard, historia, raporty, profile, ustawienia, dwie karty, kraje, tryby, nakładki.
- Eksporty PDF/CSV/JSON/`.tacho`, agregacja PDF i raport diagnostyczny.
- Ekran luk, filtr resolved, kreator manualny, blokada `CardRemoved`, opcjonalny `ForwardTimeJump` i kompletność raportów.

### 7.2. Najważniejsze naprawione błędy

- Utrata wcześniejszej jazdy po cofnięciu czasu: regresja `03:53 + 01:34 = 05:27`.
- Błędne przypisanie zamkniętej partii do nowej sesji i `DbUpdateException`.
- Pozorna idempotentność oparta na losowym `ActivityRecord.Id`.
- Niespójna granica sesji obu kart przy wczytaniu świata.
- Fałszywa wielogodzinna Jazda po `g_set_time`.
- Niezerowanie przerwy slotu 2 podczas jazdy oraz przełączanie na Dyspozycyjność.
- Błędny reset dzienny karty po manewrowaniu/cofnięciu czasu.
- Nieczytelny PDF minuta po minucie.
- Przycięta luka, której nie można było rozliczyć po cofnięciu czasu.
- Błędne luki podczas załadunku/rozładunku; ostateczna przyczyna: utrata `_lastActivity` w `GamePaused`.
- Zbyt rygorystyczne przerwanie odpoczynku przez wyjęcie karty.
- Blokujący start `SQLite Error 19` przez nakładające się rekordy kanoniczne; beta.10.1.

### 7.3. Testy i dokumentacja

- Aktualnie 239/239 testów: Core 33, Telemetry.Scs 8, Engine 64, RuleEngine 42, Application 38, Reports 9, Infrastructure 45.
- 14 testów `CanonicalProjectionTests.cs` odtwarzających realne kształty danych obu kart.
- Testy E2E shared memory, cofnięć czasu, multi-manning, promu, luk, restartu, retencji, PDF i import/eksport.
- Build Release: 0 błędów, 0 ostrzeżeń.
- Dokumentacja: README, known issues, release notes, test plan, raporty produkcyjne, terenowe i hotfixu.
- Field test pojedynczej luki `CardRemoved` na obu kartach, stabilność po restarcie, granica tygodnia regulacyjnego i slot 2 w ruchu — zaliczone.

### 7.4. Wydania

- Beta.1–beta.5: fundamenty, UI, persistence, luki i raporty.
- Beta.6–beta.9: protokół v3 i kolejne poprawki rzeczywistej sekwencji cargo.
- Beta.10: ciągłość odpoczynku przez rozliczoną lukę.
- Beta.10.1: hotfix kanonicznej historii, 239 testów, poprawny `ProductVersion` z hashem commita.
- Aktualna paczka: `output/releases/ETS2Tachograph-0.1.0-beta.10.1-win-x64.zip`.
- SHA-256: `5f4f7d85e33fb3e2ad4111bc7372067477ce611de9f70dc835be29182cb26195`.
- Plugin pozostaje bitowo zgodny z beta.10, protokół v3; hash `4F73CBFE0893A9D734E22173F7CDDC46B3C78F562B6CCF58288FDB0A73D97D02`.

---

## 8. Aktualny stan prac

Ostatnią zakończoną zmianą kodu był hotfix beta.10.1. `Canonicalize` wcześniej przycinał starą historię do kotwicy nowej sesji, po czym dopisywał wszystkie rekordy nowej sesji. Jeżeli nowa sesja zawierała minutę sprzed kotwicy już obecną w poprzedniej sesji, projekcja zawierała nakładkę. `BuildWarmBlocks` produkował dwa bloki z tym samym początkiem, a SQLite odrzucał zapis. Aplikacja nie startowała.

Naprawa odejmuje od rekordu wejściowego wszystkie zakresy już pokryte, zachowując poprawny manualny backfill. Na kopii rzeczywistej bazy zniknęła dokładnie jedna podwójnie liczona minuta na kartę, a około 1007 minut wpisów manualnych zostało zachowane. Pierwszy i drugi start aplikacji, archiwizacja warm i zamknięcie kodem 0 zostały potwierdzone.

Testy terenowe beta.10/10.1 potwierdziły ciągłość odpoczynku przez pojedynczą lukę `CardRemoved`, również przez restart i granicę tygodnia. Slot 2 w ruchu został domknięty. Wiele rozliczonych luk w jednym odpoczynku zachowuje się inaczej i wymaga decyzji domenowej, ale nie należy do hotfixu kanonizacji.

**Aktualny błąd pozostający bez poprawki:** licznik celu pauzy w `MainViewModel` zaczyna od minuty kliknięcia. RuleEngine liczy zatwierdzone minuty po regule jednej minuty. W zdarzeniu z 19.07 zapisano 41 minut odpoczynku rekonstruowanego i 3 minuty telemetrii, razem 44. UI mogło pokazać 45, lecz historia od `17:49` do `18:33` miała 44 minuty, więc jazda ciągła `04:07` nie została wyzerowana. Po przełączeniu na Dyspozycyjność te 44 minuty stały się pierwszą częścią dzielonej przerwy; potrzeba następnych ciągłych 30 minut, a nie jednej minuty.

Nie wykonano poprawki UI, nowego testu ani wydania zawierającego tę poprawkę. To jest punkt, od którego należy wznowić pracę. Po incydencie bazodanowym należy też przed kolejnym testem potwierdzić, który katalog `%LocalAppData%\ETS2Tachograph` jest aktywny; kopia odzyskowa była odkładana do `output\ODZYSK-BAZY`.

---

## 9. Otwarte problemy i ryzyka

| Problem lub ryzyko | Wpływ | Prawdopodobna przyczyna | Proponowane rozwiązanie | Priorytet |
|---|---|---|---|---|
| UI zalicza pauzę o minutę za wcześnie | Gracz widzi `ZALICZONA`, ale jazda ciągła nie resetuje się | `_restStartedAtGameMinute` pochodzi z chwili kliknięcia, nie z zakwalifikowanego bloku po regule jednej minuty | Wyliczać `RestElapsed/Remaining/Status` z ciągłego bloku zatwierdzonych rekordów lub projekcji RuleEngine; test 41+3=44 | **P1** |
| Wiele rozliczonych luk w jednym odpoczynku | Część długiego odpoczynku może pozostać rozbita | Łączenie wymaga, by jedna strona miała `SourceGapId == null`; dwa różne wpisy manualne nie łączą się | Najpierw decyzja domenowa, potem osobna specyfikacja i testy | P2 |
| Rekompensata tygodniowa zaniża dług | UI może pokazywać niemal spłacony dług mimo braku legalnego bloku en bloc | Nadwyżki z wielu odpoczynków dobowych są sumowane po okruchach | Osobny etap: zobowiązania i dedykowana spłata en bloc do odpoczynku ≥9 h | P2, wysoki wpływ prawny |
| `APP_START_FAILED` bez `InnerException` | Dłuższa diagnoza awarii EF/SQLite | Logowany jest wyjątek zewnętrzny | Logować pełny bezpieczny łańcuch wyjątków | P2 |
| Cold retention brak | W bardzo długiej perspektywie warm może rosnąć | Etap odłożony | Dobowe podsumowania >365 dni po osobnej specyfikacji | P3 |
| Brak świadomego kasowania | Użytkownik nie może łatwo ograniczyć bazy | Celowo brak auto-delete | Dodać akcję z potwierdzeniem, nie automatyzować | P3 |
| Eksport nie jest Annex 1C | Ryzyko błędnej interpretacji przez użytkownika | Projekt jest symulatorem | Zachować wyraźne ostrzeżenie, nie reklamować zgodności urzędowej | Stałe |
| SCS jest tylko do odczytu | Logiczna blokada nie zatrzyma ciężarówki | Ograniczenie SDK | Pokazywać ostrzeżenie i rejestrować Jazdę; nie obiecywać fizycznej blokady | Stałe |
| Ręczna aktualizacja pluginu | Tester może użyć starej DLL | Brak instalatora/auto-update | Kontrola protokołu, hash, instrukcja restartu ETS2 | P3 |
| Konflikty skrótów overlay | `Alt+1/2/Q` mogą kolidować | Globalne hotkeye innych aplikacji | Konfiguracja skrótów w przyszłości lub instrukcja wyłączenia konfliktu | P3 |
| Numeracja dnia +1 | Analizy surowych minut mogą wyglądać na przesunięte | UI: `floor(minute/1440)+1` | Utrwalić w narzędziach i dokumentacji | Procesowy |
| Rozrost zakresu | Kolejne poprawki mogą opóźnić stabilne wydanie | Duża liczba funkcji sąsiednich | Gate’y, osobne zadania, brak zmian „przy okazji” | Procesowy |

---

## 10. Nierozstrzygnięte decyzje

### 10.1. Ciągłość przez wiele wpisów manualnych

- **Opcja A — nie łączyć dwóch różnych `SourceGapId`:** bezpieczniej audytowo, lecz długi rzeczywisty odpoczynek z kilkoma wyjęciami karty może być rozbity.
- **Opcja B — łączyć przy pełnym pokryciu i identycznej aktywności:** wierniejsze rzeczywistej deklaracji, ale wymaga reguł pierwszeństwa, testów nadużyć i prezentacji wielu źródeł.
- **Rekomendacja:** osobna specyfikacja po naprawie licznika pauzy; nie zmieniać warunku ad hoc.
- **Wpływ:** RuleEngine `HistoryAnalysis`, `QualifiedRestPeriod`, raporty i audyt `SourceGapId`.

### 10.2. Zakres pełnych rekompensat tygodniowych

- **Opcja A — zaakceptować uproszczenie w becie:** szybciej przejść do Planera, ale licznik długu nie może być traktowany jako prawnie wiarygodny.
- **Opcja B — wdrożyć pełny model en bloc przed Planerem:** poprawniejsza podstawa, większy osobny projekt domenowy.
- **Rekomendacja:** jawnie oznaczyć known issue; pełny model realizować jako odrębny etap, nie hotfix.
- **Wpływ:** `RegulationEngine`, persistence zobowiązań, snapshot, UI i raporty.

### 10.3. Decyzja GO/FIX/HOLD dla beta.10.1

- **GO:** możliwe dopiero po świadomym zaakceptowaniu błędu licznika pauzy UI albo po jego naprawie.
- **FIX:** naprawić UI, dodać test i wydać kolejną betę — rekomendowane.
- **HOLD:** wstrzymać testy i wrócić do szerszej przebudowy — brak przesłanek.
- **Rekomendacja:** **FIX**, następnie krótki smoke i GO.

### 10.4. Publikacja i komercjalizacja

- Repo publiczne vs prywatne vs brak publikacji.
- Darmowe vs dobrowolne wsparcie vs płatne.
- **Rekomendacja dotychczasowa:** publikacja dopiero po uporządkowaniu licencji/README; darmowe lub dobrowolne wsparcie ogranicza zobowiązania supportowe.

### 10.5. Planer podróży

- Specyfikacja MVP istnieje, ale start zależy od zamknięcia gate’u tachografu.
- **Rekomendacja:** nie rozpoczynać kodu Planera przed poprawką licznika pauzy i decyzją GO.

---

## 11. Lista zadań

### Priorytet 1 — najbliższy krok

#### P1.1. Naprawić licznik pauzy UI

- **Opis:** zastąpić licznik od chwili kliknięcia licznikiem wynikającym z faktycznie zakwalifikowanego, ciągłego bloku `BreakOrRest` po regule jednej minuty.
- **Oczekiwany rezultat:** przy historii 41 min rekonstruowanych + 3 min telemetrii UI pokazuje `44:00`, `pozostało 00:01`, bez `ZALICZONA`; po kolejnej pełnej minucie RuleEngine zeruje jazdę i UI pokazuje zaliczenie.
- **Zależności:** kanoniczna historia, OneMinuteRule, `RegulationEngine`, snapshot.
- **Pliki:** `MainViewModel.cs`, ewentualnie `TachographSnapshot.cs`/`RegulationState`, `OverlayViewModel.cs`, testy Desktop/Engine/RuleEngine.
- **Kryterium ukończenia:** czerwony test dokładnego scenariusza, poprawka, wszystkie 239+ testów zielone, dashboard i overlay zgodne, ręczny test w ETS2.

#### P1.2. Wydać następną betę po poprawce

- **Opis:** zbudować WPF Release, spakować aplikację z niezmienionym pluginem v3, zaktualizować dokumentację.
- **Oczekiwany rezultat:** paczka z jednoznacznym `ProductVersion`, SHA-256 i planem testu 45 min.
- **Zależności:** P1.1.
- **Pliki:** `RELEASE_NOTES.md`, `BETA_TEST_PLAN.md`, `KNOWN_ISSUES.md`, `output/releases`.
- **Kryterium ukończenia:** build 0/0, pełny test suite, zweryfikowany ZIP i hash.

### Priorytet 2 — po ukończeniu podstaw

#### P2.1. Logować `InnerException`

- **Rezultat:** raport diagnostyczny pokazuje rzeczywisty błąd SQLite/EF.
- **Zależności:** brak.
- **Pliki:** obsługa startu aplikacji i `DiagnosticLogService`.
- **Kryterium:** test z opakowanym wyjątkiem zawiera cały łańcuch bez ujawniania wrażliwych danych.

#### P2.2. Rozstrzygnąć wiele luk w jednym odpoczynku

- **Rezultat:** zaakceptowana specyfikacja A/B i testy rzeczywistych dni 129–130.
- **Zależności:** decyzja użytkownika z sekcji 10.1.
- **Pliki:** `HistoryAnalysis`, `RegulationEngine`, raporty i testy.
- **Kryterium:** brak podwójnego liczenia, zachowany audyt wielu luk, jednoznaczna klasyfikacja.

#### P2.3. Pełny model rekompensat

- **Rezultat:** dług spłacany en bloc, przypisany do zobowiązania i terminu.
- **Zależności:** osobna szczegółowa specyfikacja prawna/domenowa.
- **Pliki:** `RegulationEngine`, `RegulationState`, persistence, snapshot, UI, PDF/JSON.
- **Kryterium:** testy 24 h→21 h długu, termin 3 tygodni, dedykowany blok ≥9 h, brak spłaty okruchami.

#### P2.4. Zamknąć dokumentacyjny gate

- **Rezultat:** aktualne wersje README, known issues, release notes, test plan i handoff.
- **Zależności:** nowe wydanie.
- **Kryterium:** dokumenty wskazują tę samą wersję i te same znane ograniczenia.

### Priorytet 3 — rozwój późniejszy

#### P3.1. Planer podróży MVP

- **Rezultat:** strategia „Najwcześniejsza legalna” według `JOURNEY_PLANNER_MVP_PLAN.md`.
- **Zależności:** decyzja GO i stabilny RuleEngine.
- **Pliki:** nowe kontrakty Application/RuleEngine, testy P0, później UI.
- **Kryterium:** najpierw czerwone testy kontraktów, potem silnik bez UI, na końcu ekran.

#### P3.2. Cold retention

- **Rezultat:** podsumowania dobowe danych >365 dni bez utraty raportów.
- **Zależności:** stabilny hot/warm i migracja.
- **Kryterium:** idempotentność, `highWaterMark`, bilans minut, brak auto-delete.

#### P3.3. Dystrybucja

- Instalator, podpis kodu, opcjonalny auto-update i konfiguracja hotkeyów.
- Zależność: decyzja o publikacji i licencji.

---

## 12. Rekomendowany następny krok

**Naprawić rozjazd licznika pauzy UI z zatwierdzoną historią regulacyjną.**

1. Dodać test odtwarzający potwierdzony przypadek: aktywność zmieniona na odpoczynek wewnątrz minuty, operacja cargo rekonstruuje 41 minut, telemetria dopisuje 3 minuty, a kolejna aktywność zaczyna się w 45. minucie liczonej przez UI. Historia ma 44 minuty i RuleEngine nie resetuje jazdy.
2. Przenieść źródło `RestElapsed`, `RestRemaining` i `RestStatus` z `_restStartedAtGameMinute` na faktyczny ciągły blok odpoczynku po OneMinuteRule. Dashboard, urządzenie i overlay muszą czytać tę samą projekcję.
3. Nie zmieniać progu RuleEngine i nie dopisywać brakującej minuty sztucznie. To UI jest błędne, nie reguła 45 minut.
4. Zmienić `MainViewModel.cs`, w razie potrzeby wystawić minimalną projekcję przez `TachographSnapshot`/`RegulationState`, zaktualizować `OverlayViewModel.cs` i testy.
5. Sprawdzić: 44 min → pierwsza część 15+30, brak resetu i 1 min do celu; 45 min → reset; po przerwaniu przy 44 min kolejna 1 min nie wystarcza, wymagane jest 30 min.

To zadanie jest pierwsze, ponieważ usuwa jedyny potwierdzony rozjazd między tym, co widzi tester, a tym, co liczy silnik. Bez niego decyzja GO dla bieżącej bety byłaby myląca.

---

## 13. Instrukcja dla kolejnej sesji AI

```text
Jesteś starszym inżynierem C#/.NET współpracującym nad projektem ETS2 EU Digital Tachograph.
Projekt to symulator tachografu dla ETS2: .NET 9, WPF, SQLite/EF Core, RuleEngine oraz
natywny plugin C++ SCS SDK. Obowiązująca wersja to beta.10.1, 239 testów zielonych.

Historia minutowa w game_time jest jedynym źródłem prawdy. Nie przechowuj liczników w
modelu kierowcy. Zachowuj sesje, truncate-and-append, jawne ActivityGap i audyt SourceGapId.
Nie usuwaj danych automatycznie. Nie zmieniaj ustalonych decyzji ani reguł prawnych bez
wyraźnego uzasadnienia i osobnej specyfikacji. Nie łącz napraw z sąsiednimi refaktorami.

Przed propozycją lub zmianą zawsze przeczytaj istniejący kod, testy, PROJECT_STARTER_PACK,
KNOWN_ISSUES i raporty związane z problemem. Każdy potwierdzony bug najpierw odtwórz
czerwonym testem, potem popraw minimalnie i uruchom pełny zestaw regresji. Po większej
zmianie zaktualizuj README/RELEASE_NOTES/BETA_TEST_PLAN/KNOWN_ISSUES oraz handoff.

Najbliższe zadanie: naprawić licznik pauzy UI. Obecnie może pokazać 45 min od chwili
kliknięcia, gdy OneMinuteRule zatwierdził tylko 44 minuty. RuleEngine jest poprawny; nie
zmieniaj progu 45 min. UI ma liczyć faktyczny ciągły blok BreakOrRest i być zgodne na
Dashboardzie, urządzeniu oraz overlay. Zacznij od testu 41 min reconstructed + 3 min
telemetry = 44 min, brak resetu i 1 min pozostała.
```

---

## 14. Skrócony kontekst startowy

ETS2 EU Digital Tachograph to aplikacja desktopowa dla Euro Truck Simulator 2 symulująca europejski tachograf cyfrowy. Stos technologiczny: .NET 9, C#, WPF/MVVM, SQLite z Entity Framework Core oraz natywny plugin C++ oparty na oficjalnym SCS Telemetry SDK 1.14. Aplikacja działa na Windows x64 i jest publikowana jako self-contained ZIP. Użytkownikiem jest gracz ETS2, również w środowisku VTC, oczekujący realistycznej obsługi jednej lub dwóch kart, aktywności, przerw, odpoczynków, raportów i nakładek nad grą. Projekt jest symulatorem, nie certyfikowanym tachografem ani urzędową implementacją Annex 1C.

Zasada nadrzędna: całość działa na `game_time` ETS2, nigdy na czasie Windows. Jedna minuta gry jest rekordem atomowym i źródłem prawdy. Liczniki nie są zapisywane w profilu kierowcy; RuleEngine za każdym razem wylicza je z historii. Historia musi obsługiwać przyspieszenie czasu, sen, `g_set_time`, załadunek, rozładunek, cofnięcie po wczytaniu zapisu oraz korekty pozycji. Cofnięcie tworzy nową sesję i projekcję `truncate-and-append`: źródłowe gałęzie pozostają, a nowa gałąź zastępuje tylko nakładającą się przyszłość.

Architektura ma projekty: Core (encje, czas, ActivityTimeline, OneMinuteRule), Telemetry.Scs (shared memory v3), Engine (`ActivityHistoryProcessor`, `TachographEngine`, `CrewTachographEngine`, snapshoty), RuleEngine, Infrastructure (SQLite, repozytoria, migracje, retencja i kanonizacja), Application (DTO i serwisy), Reports oraz Desktop WPF. Plugin C++ znajduje się w `native/ETS2Tachograph.ScsPlugin`. Przepływ: ETS2 → plugin → shared memory → Telemetry → Engine → SQLite/projekcja kanoniczna → RuleEngine → snapshot/DTO → UI, overlay i eksporty.

Obowiązująca wersja to `0.1.0-beta.10.1`. Ma 239/239 zielonych testów i build Release bez błędów/ostrzeżeń. Paczka: `output/releases/ETS2Tachograph-0.1.0-beta.10.1-win-x64.zip`, SHA-256 `5f4f7d85e33fb3e2ad4111bc7372067477ce611de9f70dc835be29182cb26195`. Plugin v3 jest identyczny z beta.10; jego hash to `4F73CBFE0893A9D734E22173F7CDDC46B3C78F562B6CCF58288FDB0A73D97D02`.

Beta.10.1 naprawiła blokujący start błąd kanonicznej historii. W starszym kodzie nowa sesja mogła ponownie dopisać minutę sprzed własnej kotwicy, która była już w historii. Podczas budowania `WarmActivityBlocks` powstawały dwa bloki z tym samym początkiem i SQLite zgłaszał `UNIQUE constraint failed`. Nie wolno było po prostu odrzucić wszystkich rekordów sprzed kotwicy, bo poprawne wpisy manualne uzupełniają starsze luki i leżą przed kotwicą sesji zapisu. Rozwiązanie: `SubtractCoveredRanges` odejmuje od rekordu zakresy już zajęte przez historię kanoniczną, a `EnsureNoOverlap` twardo sprawdza `End <= next.Start`. Istniejąca historia ma pierwszeństwo, a niepokryty manualny backfill zostaje. `InvalidCanonicalHistoryException` wykrywa konflikt przed SQLite. Na realnych danych usunięto z projekcji po jednej zdublowanej minucie na kartę, zachowując około 1007 minut backfillu i wszystkie rekordy źródłowe.

Funkcje gotowe: jazda automatyczna, ręczne odpoczynek/praca/dyspozycyjność, jazda ciągła 4 h 30, przerwa ciągła 45 i dzielona 15+30, jazda dzienna 9/10 h, tygodniowa 56 h, dwutygodniowa 90 h, odpoczynek dobowy 9/11 h, tygodniowy 24/45 h, wydłużenia dzienne, skrócone odpoczynki, podwójna obsada 30 h, OUT, ręczny prom i kraje start/koniec. Slot 2 w ruchu normalnie ma Dyspozycyjność; może użyć dedykowanej przerwy 45 min, która zeruje jazdę ciągłą tej karty, lecz nie tworzy odpoczynku dobowego. Nie ma przycisku zamiany kierowców — karty trzeba wyjąć i włożyć do odpowiednich slotów. Reguła pierwszej godziny multi-manning i dzielony odpoczynek dobowy 3+9 są poza zakresem.

Luki są osobnymi encjami `ActivityGap`, nie aktywnością Unknown. Przyczyny: `ForwardTimeJump`, `CardRemoved`, rezerwa `TelemetryUnavailable`. `CardRemoved` ma priorytet nad skokiem per karta. W projekcji może istnieć najwyżej jedna otwarta luka na kartę. Wyjęcie otwiera lukę, włożenie zamyka. `CardRemoved` wymusza kreator i logicznie blokuje tachograf; `ForwardTimeJump` jest opcjonalny. ManualEntry dopuszcza tylko odpoczynek, Inną pracę i Dyspozycyjność; segmenty muszą dokładnie pokrywać lukę. `SourceGapId` zachowuje audyt. Po zmianie beta.10 odpoczynek manualny może łączyć się minutowo z odpoczynkiem przed i po wyjęciu karty: 2 h + 7 h daje 9 h, a 5 h + 40 h może dać regularny tygodniowy 45 h. Wiele różnych rozliczonych luk w jednym bloku jest nadal decyzją otwartą.

Skoki czasu: do 2 minut są rekonstruowane ostatnią aktywnością. Duży skok po Jeździe nigdy nie tworzy sztucznej Jazdy — powstaje luka. Długi odpoczynek może być rekonstruowany tylko, gdy przed i po skoku pojazd stoi i nadal wybrano odpoczynek. Załadunek/rozładunek jest rozpoznawany przez `cargo_operation_generation`; zachowuje aktywność wybraną na każdej karcie. Beta.9 naprawiła utratę aktywności w gałęzi `GamePaused`. Ramki `running == 0` nie zasilają historii ani `highWaterMark`.

Persistence ma backup przed migracją, znaczący klucz idempotentności `ActivitySessionId + StartGameMinute`, atomową granicę sesji obu kart i retencję hot/warm. Hot to ostatnie 14 dni gry minutowo, warm to starsze ciągłe bloki; zmiana samego źródła nie rozcina bloku, tylko daje `Mixed`. Próg używa monotonicznego `highWaterMark`. Cold >365 dni jest tylko hakiem. System niczego automatycznie nie usuwa.

UI WPF ma realistyczny tachograf, Dashboard, Historię, Raporty, Kierowców i Ustawienia. Nakładki: Alt+1 S1, Alt+2 S2, Alt+Q dodatkowo S1; pozycje są zapisywane osobno, widoczność nie. PDF agreguje minuty w bloki, CSV pozostaje surowy. PDF i JSON pokazują nierozliczone luki oraz bilans kompletności. Eksport `.tacho` jest własnym formatem, nie urzędowym Annex 1C.

Tryb pracy nad projektem jest regresyjny i oparty na dowodach. Każdy potwierdzony błąd należy najpierw odtworzyć testem na dokładnych minutach gry oraz właściwej karcie, slocie i sesji, a dopiero potem wprowadzić najmniejszą poprawkę. Po zmianach w historii albo regułach trzeba uruchomić cały zestaw testów, ponieważ pozornie lokalna korekta może zmienić reset dobowy, tydzień regulacyjny, raport lub drugi slot. Nie wolno maskować konfliktów przez ignorowanie wyjątków, obcinanie liczników do limitu ani sztuczne dopisywanie brakujących minut. Naruszenia pokazują rzeczywisty wynik, przykładowo `3 / 2`, a nie zatrzymane `2 / 2`. Diagnostyka, surowy CSV i źródłowe sesje muszą pozostać dostępne, by dało się odtworzyć każdy wynik. Wersję uznaje się za gotową do bety dopiero po zielonym buildzie i testach, sprawdzeniu paczki oraz hashy, krótkim smoke teście z prawdziwą telemetrią i aktualizacji dokumentów wydania. Obecny terenowy gate potwierdził stabilność beta.10.1 po restarcie, ciągłość obu kart oraz zachowanie na granicy tygodnia, ale nie zamknął jeszcze rozjazdu licznika pauzy w UI.

Znane ograniczenia: rekompensata tygodniowa jest uproszczona i może zaniżać dług, ponieważ spłaca go nadwyżkami z wielu odpoczynków zamiast dedykowanym blokiem en bloc. Wiele manualnych luk w jednym odpoczynku wymaga decyzji. `APP_START_FAILED` nie pokazuje całego `InnerException`. Cold retention, instalator, podpis i auto-update nie istnieją. SCS jest tylko do odczytu, więc blokada nie może fizycznie zatrzymać ciężarówki.

Najważniejszy aktualny problem i następne zadanie: licznik pauzy UI może pokazać zaliczenie o minutę za wcześnie. `MainViewModel` liczy od minuty kliknięcia, a OneMinuteRule może zakwalifikować tę minutę jako poprzednią aktywność. W realnym przypadku historia miała 41 min odpoczynku rekonstruowanego + 3 min telemetrii = 44, ale UI mogło wskazać 45. RuleEngine poprawnie nie wyzerował jazdy. Po przerwaniu 44 min liczy się jako pierwsza część 15+30 i potrzeba kolejnych 30 min, nie jednej. Należy najpierw dodać czerwony test 41+3=44, następnie wyliczać `RestElapsed`, `RestRemaining` i `RestStatus` z faktycznego ciągłego bloku zatwierdzonych rekordów. Nie zmieniać progu RuleEngine ani nie dopisywać minuty. Dashboard, urządzenie i overlay muszą czytać tę samą projekcję. Po poprawce wykonać pełne testy, smoke ETS2, wydać następną betę i dopiero wtedy podjąć GO oraz rozpocząć Planer podróży MVP.
