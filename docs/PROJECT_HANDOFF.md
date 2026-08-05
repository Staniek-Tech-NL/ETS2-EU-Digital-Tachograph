# PROJECT HANDOFF — ETS2 EU Digital Tachograph

**Baza wydaniowa:** 0.1.0-beta.12 — GO M8, publiczne pre-release
**Data przygotowania dokumentu:** 20 lipca 2026
**Ostatnia aktualizacja:** 5 sierpnia 2026 — publikacja `0.1.0-beta.12`, GO M8
**Bieżący stan:** publiczne pre-release; 570/570 testów, build Release 0/0,
końcowy smoke M7 zielony, brak P0/P1
**Przeznaczenie:** pakiet startowy dla nowej sesji AI / nowego okna kontekstowego

---

## 1. Streszczenie projektu

ETS2 EU Digital Tachograph to samodzielna aplikacja desktopowa (.NET 9, C#, WPF) symulująca cyfrowy tachograf zgodny z logiką rozporządzenia UE 561/2006, przeznaczona dla graczy Euro Truck Simulator 2 grających w trybie singleplayer. Aplikacja odczytuje oficjalną telemetrię gry przez natywny plugin C++ oparty na SCS SDK, prowadzi historię aktywności dwóch kart kierowców (podwójna obsada) w czasie gry (`game_time`), wylicza liczniki regulacyjne (jazda ciągła, dzienna, tygodniowa, dwutygodniowa, odpoczynki, rekompensaty), obsługuje nietypowe zachowania czasu gry (cofnięcia, skoki, pauzy, operacje ładunkowe) i generuje raporty (PDF, CSV, JSON dla VTC, własny format `.tacho`).

Projekt powstał jako reakcja na niedociągnięcia istniejących na rynku narzędzi tego typu (m.in. aplikacja „Emre54", tacho wbudowane w Ox-Ram VTC) — użytkownik napotkał w nich błędy (niezerujący się licznik dzienny, brak wsparcia dla podwójnej obsady, brak logowania) i zbudował własne rozwiązanie od zera, jednocześnie naprawiając analogiczne klasy błędów, które by odziedziczył.

Użytkownikiem docelowym jest gracz ETS2 ceniący realizm (w tym społeczność VTC), a autor projektu jest jednocześnie jego głównym testerem i deweloperem (samouk, korzysta z Claude Code / Codex jako wsparcia).

**Aktualny etap:** `0.1.0-beta.12` zostało opublikowane 5 sierpnia 2026 jako
publiczne GitHub pre-release z decyzją **GO M8**. Tag `v0.1.0-beta.12` wskazuje
commit źródłowy `ffe6f7fad2c4fccfad8fc12f1a93675cc5d13c78`, a opublikowany ZIP
zachował SHA-256 `A2B8F949E100F8683225B7A0D5A76E5C7E3434AD95AEC9596006C4A5E41F5E78`.
Gate wynosi `570/570`, build Release 0 błędów i 0 ostrzeżeń, a końcowy smoke M7
jest zielony bez P0/P1. Łańcuch M0–M8 jest zamknięty; odblokowany został backlog
popublikacyjny. `0.1.0-beta.11.1` pozostaje zachowaną wersją historyczną.

---

## 2. Aktualny zakres projektu

### Funkcje podstawowe
- Odczyt telemetrii ETS2 przez natywny plugin C++ (SCS SDK 1.14) — **[GOTOWE]**
- Model domenowy: karta kierowcy, sesje historii, minutowe rekordy aktywności (`ActivityRecord`) — **[GOTOWE]**
- Silnik reguł: jazda ciągła (4h30), przerwa (45 min, w tym podział 15+30), odpoczynek dobowy (9h/11h), tygodniowy (24h/45h), limity 56h/90h — **[GOTOWE]**
- Podwójna obsada: dwa niezależne sloty kart, okno 30h zamiast 24h, specjalna przerwa 45 min dla slotu 2 w ruchu — **[GOTOWE]**
- Obsługa cofnięć i skoków czasu gry (`truncate-and-append`) — **[GOTOWE]**; wspólny długi skok dwóch kart jest koordynowany w `CrewTachographEngine`
- Kanoniczna projekcja bez nakładających się minut (`SubtractCoveredRanges` + `EnsureNoOverlap`) — **[GOTOWE]** w beta.10.1
- Jawne luki aktywności (`ActivityGap`) i wpisy manualne (`ManualEntryService.ResolveGap`) — **[GOTOWE]**
- Ciągłość odpoczynku przez rozliczoną lukę `CardRemoved` (beta.10) — **[GOTOWE]**; bieżący zakres testów terenowych zaliczony, wiele luk pozostaje osobnym zadaniem domenowym
- Rozpoznawanie operacji załadunku/rozładunku (protokół v3) — **[GOTOWE]**
- Raporty PDF/CSV/JSON VTC/`.tacho` z informacją o kompletności dowodu — **[GOTOWE]**
- Retencja danych (hot/warm) — **[GOTOWE]**; cold — **[DO ZROBIENIA]** (hak przygotowany)
- Statystyki regulacyjne w UI (praca dobowa, wydłużenia, skrócone odpoczynki, rekompensaty) — **[GOTOWE]**
- Pełne rekompensaty tygodniowe: en bloc, FIFO, ścisły termin, stabilne identyfikatory, ślad spłaty i audytowana alokacja bloków 24 h+ — **[GOTOWE]**
- Warstwowa prezentacja rekompensat oraz pełne eksporty PDF/CSV/JSON — **[GOTOWE]**; zawierają decyzję, kandydaturę, podstawę hosta i ślad zmiany
- Lista nierozliczonych luk w UI + ostrzeżenie w raporcie — **[GOTOWE]**

### Funkcje dodatkowe
- Nakładki w grze (overlay) S1/S2 z niezależną pozycją — **[GOTOWE]**
- Reguła pierwszej godziny przy podwójnej obsadzie (maszyna stanów PENDING/QUALIFIED/FAILED) — **[ODRZUCONE]** (świadomie wyrzucone z zakresu)
- Dzielony odpoczynek dobowy 3h+9h — **[ODRZUCONE]** / nierozpoznawany, known issue
- Kolory ostrzegawcze licznika „DO PRZERWY" (Faza 1 UX) — **[DO ZROBIENIA]**, niski priorytet

### Wymagania techniczne
- Platforma: Windows x64 wyłącznie — **[GOTOWE]**
- .NET 9, C#, WPF, SQLite + Entity Framework Core — **[GOTOWE]**
- Natywny plugin C++ (SCS Telemetry SDK, pamięć współdzielona, protokół v3) — **[GOTOWE]**
- Aplikacja self-contained (paczka ZIP), bez instalatora, bez podpisu kodu, bez auto-update — **[DO ZROBIENIA]** jako możliwy przyszły etap
- Named mutex blokujący drugą instancję — **[GOTOWE]**

### Wymagania dotyczące interfejsu
- Realistyczny panel tachografu (ekran LCD, menu, przyciski fizyczne) — **[GOTOWE]**
- Ekran Historia z filtrowaniem i listą luk — **[GOTOWE]**
- Ekran Raporty z wyborem karty/zakresu i ostrzeżeniem o kompletności — **[GOTOWE]**
- Kreator wpisu manualnego wariant B (blokujący dla `CardRemoved`, opcjonalny
  dla `ForwardTimeJump`): pełny plan, szybkie akcje, edycja trzech aktywności,
  dzielenie/scalanie i walidacja pokrycia — **[GOTOWE LOKALNIE]**
- Przeszukiwalny katalog 249 krajów ISO z osobnym kodem tachografowym oraz
  pamięcią ostatniego kraju per karta — **[GOTOWE LOKALNIE]**
- Prezentacja `ODP. TYG.` jako bieżący okres 24 h `1/6–6/6+` ze stałym
  terminem rozpoczęcia odpoczynku w `game_time`, np. `4/6 (D141 22:55)`
  — **[GOTOWE LOKALNIE]**
- Kontrola wizualna działającego UI po zmianach w XAML — **[GOTOWE]**; stała checklista regresji została dodana do `BETA_TEST_PLAN.md` 21.07

### Wymagania dotyczące danych
- Wszystkie obliczenia oparte na `game_time`, nigdy na zegarze systemowym — **[GOTOWE]**
- Liczniki wyliczane z historii, nie przechowywane jako osobny stan — **[GOTOWE]**
- Backup bazy przed każdą migracją (`tachograph.db.bak.<data>`) — **[GOTOWE]**
- Brak automatycznego kasowania danych — **[GOTOWE]** (świadoma decyzja)

### Wymagania prawne / biznesowe
- Aplikacja jawnie nie jest certyfikowanym tachografem ani implementacją Annex 1C — **[GOTOWE]**
- Model dystrybucji / komercjalizacji — **[DO DECYZJI]**

---

## 3. Najważniejsze ustalenia i decyzje

| Obszar | Podjęta decyzja | Powód | Konsekwencje |
|---|---|---|---|
| Klucz idempotentności zapisu | `ActivitySessionId + StartGameMinute`, nie losowe `ActivityRecord.Id` | Losowy Id nigdy nie wykrywał duplikatów treści | Idempotentność realnie działa; konflikt treści logowany jako `ACTIVITY_RECORD_CONFLICT` |
| Przypisanie sesji przy cofnięciu czasu | Zamknięte rekordy zachowują GUID **starej** sesji, `StartNewSession()` jako jedyny punkt inkrementacji indeksu | Źródło crashu `UNIQUE constraint` — rekordy starej gałęzi dostawały GUID nowej sesji | Bug zniknął u źródła; centralizacja zapobiega przyszłym rozjazdom |
| Kanonizacja rekordów między sesjami (beta.10.1) | Nowa sesja od swojej kotwicy w górę przejmuje oś przez `TruncateAfter`; poniżej kotwicy może tylko uzupełniać **niepokryte** minuty. Istniejąca historia kanoniczna ma pierwszeństwo | Kotwica mówi, kiedy rekord zapisano, a nie czyje są minuty; odrzucanie rekordów przed kotwicą usunęłoby poprawny backfill manualny | `SubtractCoveredRanges` usuwa wyłącznie pokryte fragmenty, `EnsureNoOverlap` twardo pilnuje braku nakładek, a konflikt jest wykrywany jako `InvalidCanonicalHistoryException` przed SQLite |
| Model retencji | Warstwowa: gorąca (14 dni gry, minutowo) / ciepła (bloki) / zimna (hak, niewdrożona) | Wydajność bez utraty danych | Próg kotwiczony na monotonicznym `highWaterMark` |
| Model luki aktywności | Jeden typ `ActivityGap` z enumem `GapReason` (`ForwardTimeJump`, `CardRemoved`, rezerwa `TelemetryUnavailable`) | Flaga `IsCardRemoved` nie skalowałaby się na kolejne przyczyny | Jeden mechanizm `ResolveGap` dla obu przyczyn, różna tylko polityka wymuszenia |
| Priorytet przyczyn luki | `CardRemoved > ForwardTimeJump`, liczony per karta | Karta wyjęta = silniejszy stan niewiedzy, pochłania skok czasu | Brak nakładających się luk dla tej samej karty |
| Cofnięcie przed otwarcie luki | Stara luka znika z projekcji kanonicznej (nie przycinana do zera), źródło zostaje w porzuconej gałęzi, nowa luka otwierana tylko jeśli karta nadal wyjęta | Przesunięcie początku luki byłoby zmyślaniem faktu | Niezmiennik: najwyżej jedna otwarta luka na kartę w projekcji kanonicznej |
| Piktogramy wpisu manualnego | `Przerwa/Odpoczynek`, `Inna praca`, `Dyspozycyjność` — **bez OUT** | OUT niszowy i podatny na nadużycia przy wyjętej karcie | Prostsza macierz: tylko odpoczynek buduje ciągły blok, dwa pozostałe przerywają |
| Ciągłość bloku odpoczynku | Najdłuższy **nieprzerwany** blok, nigdy suma segmentów | Test „6h+2h praca+4h" nie może dać fałszywego resetu | Reset dobowy tylko przy realnym nieprzerwanym ≥9h |
| Reguła wyjęcia karty a odpoczynek (**zmiana w beta.10**) | **Poprzednia decyzja** (wyjęcie karty kończy wszystkie czynności) **odwrócona**: jeśli luka rozliczona jako `Przerwa/Odpoczynek`, sąsiadujące odcinki tworzą jeden ciągły blok regulacyjny | Koniec automatycznego zapisu ≠ koniec rzeczywistego odpoczynku kierowcy | Odpoczynek może być ciągły „przez" lukę po obu stronach; dotyczy też kwalifikacji tygodniowej |
| Baza rekompensaty | 9h (nie 11h) jako próg odejmowania | Realny bug: rekompensata liczona od złej bazy | Odpoczynek 20h daje 11h rekompensaty (nie 9h) |
| Alokacja niejednoznacznego bloku 24 h+ (beta.11.1) | Po zakończeniu bloku użytkownik wybiera jedną z kandydatur wyliczonych przez RuleEngine: np. `DailyRestWithCompensation` albo tygodniową klasyfikację całego bloku | Sama długość fizycznego bloku nie rozstrzyga, czy minuty ponad 9 h są rekompensatą; automatyczna klasyfikacja beta.11 tworzyła błędne nowe długi | Użytkownik wybiera kandydaturę, nie minuty; `DailyRestOnly` nie istnieje dla zamkniętego bloku 24 h+; decyzja jest trwała, audytowana i unieważniana po zmianie `RestBlockId` |
| Rekonstrukcja skoków czasu | Mały skok (≤2 min) → rekonstrukcja ostatnią aktywnością (również Jazdą); duży skok po Jeździe → zawsze luka; duży skok przy odpoczynku → rekonstrukcja tylko gdy pojazd stał przed i po; inne aktywności → opcjonalna luka; potwierdzony załadunek/rozładunek → wybrana aktywność bez luki | Naprawa realnego bugu (fałszywa wielogodzinna Jazda z powietrza) | Osobna klasyfikacja per typ aktywności i per slot |
| Wspólny długi skok przy podwójnej obsadzie (beta.11.1) | `CrewTachographEngine` klasyfikuje wspólny skok raz i przekazuje obu kartom ten sam kontekst; druga karta może zachować wyłącznie własną stabilną aktywność przed i po skoku | Niezależna klasyfikacja per karta tworzyła symetryczne fałszywe `ForwardTimeJump`, gdy tylko jedna karta odpoczywała | Nie rekonstruować Jazdy, pustego slotu, karty wyjętej ani aktywności zmienionej przez skok; wynik nie może zależeć od kolejności S1/S2 |
| Wykrywanie operacji ładunkowych | Protokół v3, znacznik generacji operacji z oficjalnych zdarzeń SCS | Bez sygnału z gry nie dało się odróżnić skoku czasu od przesunięcia przy załadunku | Wymagało 4 iteracji (beta.6–beta.9); rzeczywista przyczyna: stan aktywności gubiony w gałęzi `GamePaused` |
| Reguła pierwszej godziny (multi-manning) | **Odrzucona z zakresu** | Zbyt złożona (retroaktywna maszyna stanów), niska widoczność względem nakładu | Zaprojektowana koncepcyjnie, niezakodowana |
| Testy regresyjne | Każdy znaleziony bug dostaje dedykowany test odtwarzający dokładny scenariusz | Zapobieganie powrotowi tej samej klasy błędu | Test `03:53 + 01:34 = 05:27` jako stały punkt odniesienia |
| Repozytorium git | Lokalne repo ma powstać **przed** publikacją, niezależnie od niej | Bezpieczne cofanie zmian wprowadzanych przez agentów AI | Repo odtworzone i zwersjonowane — commit bazowy `e510ed9` na `main`; `output/` (1,26 GB paczek) wyłączone przez `.gitignore` |

---

## 4. Aktualna architektura

**Główne komponenty (projekty w rozwiązaniu):**

- `ETS2Tachograph.Core` — model domenowy, czas gry, `ActivityTimeline`, reguła jednej minuty, `GameClockFormatter`
- `ETS2Tachograph.Telemetry.Scs` — odczyt wersjonowanej pamięci współdzielonej (protokół v3)
- `ETS2Tachograph.Engine` — klasyfikacja ramek telemetrii, sesje, luki (`ActivityHistoryProcessor`, `CrewTachographEngine`), snapshoty oraz wspólny `CrewTimeJumpResolution`
- `ETS2Tachograph.RuleEngine` — liczniki regulacyjne, naruszenia, rekompensaty
  (`RegulationEngine`, `RegulationEvaluation`, `RegulationState`,
  `WeeklyRestCompensation`, `CompensationSummary`,
  `RestAllocationCandidate`, `RestAllocationDecision`)
- `ETS2Tachograph.Infrastructure` — SQLite, EF Core, repozytoria, migracje, retencja i kanoniczna projekcja historii (`Canonicalize`, `SubtractCoveredRanges`, `EnsureNoOverlap`)
- `ETS2Tachograph.Application` — przypadki użycia, DTO, import/eksport i trwały
  zapis wpisów manualnych (`ManualEntryService`, `ActivityGapService`,
  `ManualEntryValidator`)
- `ETS2Tachograph.Reports` — generowanie PDF, prezentacja bloków (`PdfReportExporter`, `ReportPresentationBuilder`)
- `ETS2Tachograph.Desktop` — WPF (`MainWindow.xaml`, `MainViewModel.cs`, `OverlayViewModel.cs`)
- `ETS2Tachograph.ScsPlugin` — natywny plugin C++ dla ETS2 x64

**Przepływ danych:**

```
Plugin C++ (SCS SDK)
  → pamięć współdzielona (protokół wersjonowany, seqlock)
    → Telemetry.Scs (odczyt ramki)
      → Engine (klasyfikacja aktywności, wykrywanie skoków i luk)
        → Infrastructure (zapis do SQLite)
          → RuleEngine (przeliczenie liczników z historii)
            → Application (DTO / snapshoty)
              → Desktop (UI, nakładki) / Reports (eksporty)
```

**Zasada nadrzędna:** historia minutowa (`ActivityRecord`) jest jedynym źródłem prawdy. Liczniki, projekcje (bloki „ciepłe"), raporty i statystyki UI są **zawsze wyliczane z historii**, nigdy nie są osobno przechowywanym stanem. Ta sama zasada obowiązuje dla luk (`ActivityGap`) i dla warstwy retencji.

**Integracje zewnętrzne:** wyłącznie z grą ETS2 przez oficjalne SCS Telemetry SDK. Brak integracji z zewnętrznymi platformami VTC/dispatcherami w kodzie aplikacji.

---

## 5. Struktura projektu i pliki

```
ETS2 EU Digital Tachograph/
├── src/
│   ├── ETS2Tachograph.Core/
│   │   ├── Time/GameClockFormatter.cs
│   │   └── Entities/ActivityGap.cs
│   ├── ETS2Tachograph.Telemetry.Scs/
│   ├── ETS2Tachograph.Engine/
│   │   ├── ActivityHistoryProcessor.cs
│   │   └── CrewTachographEngine.cs
│   ├── ETS2Tachograph.RuleEngine/
│   │   ├── RegulationEngine.cs
│   │   ├── RegulationEvaluation.cs
│   │   ├── RegulationState.cs
│   │   ├── WeeklyRestCompensation.cs
│   │   └── CompensationSummary.cs
│   ├── ETS2Tachograph.Infrastructure/
│   │   └── Persistence/
│   │       ├── Repositories.cs
│   │       ├── TachographDbContext.cs
│   │       └── Migrations/20260717122624_AddActivityGaps.cs
│   ├── ETS2Tachograph.Application/
│   │   ├── Persistence/IActivityRepository.cs
│   │   ├── Dtos/ActivityGapDtos.cs
│   │   ├── Dtos/ReportDto.cs
│   │   └── Services/
│   │       ├── ActivityGapService.cs
│   │       ├── ManualEntryService.cs
│   │       ├── ManualEntryWizardDraft.cs
│   │       └── ReportService.cs
│   ├── ETS2Tachograph.Reports/
│   │   ├── PdfReportExporter.cs
│   │   └── ReportPresentationBuilder.cs
│   ├── ETS2Tachograph.Desktop/
│   │   ├── Views/MainWindow.xaml
│   │   └── ViewModels/
│   │       ├── MainViewModel.cs
│   │       └── OverlayViewModel.cs
│   └── ETS2Tachograph.ScsPlugin/
│       ├── plugin.cpp
│       └── telemetry_protocol.h
├── tests/
│   ├── ETS2Tachograph.Core.Tests/            (33 testy)
│   ├── ETS2Tachograph.Telemetry.Scs.Tests/   (8 testów)
│   ├── ETS2Tachograph.Engine.Tests/          (69 testów)
│   │   ├── ActivityHistoryProcessorTests.cs
│   │   ├── CrewTachographEngineTests.cs
│   │   └── ManualEntryLockTests.cs
│   ├── ETS2Tachograph.RuleEngine.Tests/      (62 testy)
│   │   └── RegulationEngineTests.cs
│   ├── ETS2Tachograph.Application.Tests/     (50 testów)
│   │   └── ManualEntryWizardDraftTests.cs
│   ├── ETS2Tachograph.Reports.Tests/         (9 testów)
│   ├── ETS2Tachograph.Infrastructure.Tests/  (51 testów)
│   │   ├── CanonicalProjectionTests.cs         (14 testów regresyjnych beta.10.1)
│   │   └── WeeklyRestCompensationSqliteRestartTests.cs
│   └── ETS2Tachograph.Desktop.Tests/         (28 testów)
│       ├── CountryCatalogTests.cs
│       └── ManualEntryPlanEditorTests.cs
├── docs/
│   ├── PROJECT_HANDOFF.md              ← ten dokument
│   ├── Agent raporty/
│   │   └── RAPORT_AGENTA_2026-07-20.md
│   ├── stage-3-rule-engine.md
│   ├── stage-3.5-integration.md
│   ├── PRODUCTION_STATUS_REPORT_BETA4.md
│   ├── UI_VISIBLE_DATA_REPORT_BETA4.md
│   ├── FIELD_TEST_REPORT_2026-07-21.md
│   ├── BUGFIX_REPORT_CANONICAL_HISTORY_2026-07-21.md
│   ├── PLAN_NAPRAWCZY_BETA_11_1.md
│   └── JOURNEY_PLANNER_MVP_PLAN.md
├── output/releases/                    [ignorowane przez git — 1,26 GB paczek]
│   ├── ETS2Tachograph-0.1.0-beta.11-win-x64.zip  [WYCOFANY KANDYDAT]
│   └── ETS2Tachograph-0.1.0-beta.11.1-win-x64.zip [SMOKE ZALICZONY — GO]
├── BETA_TEST_PLAN.md
├── KNOWN_ISSUES.md
├── RELEASE_NOTES.md
└── README.md
```

**Uwaga:** dokładna zawartość poszczególnych plików (poza fragmentami omówionymi w historii projektu) nie jest w pełni udokumentowana w tym pakiecie. Odniesienia do numerów linii pochodzą z raportów wdrożeniowych i mogły się zdezaktualizować.

---

## 6. Reguły działania systemu

| Reguła | Opis | Warunek | Rezultat | Status |
|---|---|---|---|---|
| Jazda ciągła | Max 4h30 (270 min) bez przerwy | Aktywność Jazda | Naruszenie po przekroczeniu | GOTOWE |
| Przerwa 45 min | Reset licznika ciągłej | 45 min ciągłe lub 15+30 (w tej kolejności) | Zeruje jazdę ciągłą, nie dzienną | GOTOWE |
| Odpoczynek dobowy | 9h skrócony / 11h regularny | Ciągły nieprzerwany blok `Przerwa/Odpoczynek` | Reset licznika dziennego, retroaktywny stempel na końcu bloku | GOTOWE |
| Skrócone odpoczynki | Max 3 między odpoczynkami tygodniowymi | Odpoczynek 9–<11h | Licznik `ReducedDailyRestsSinceWeeklyRest` | GOTOWE (liczenie i prezentacja w UI) |
| Wydłużona jazda dzienna | 2× w tygodniu limit 9h→10h | Przekroczenie 9:00 (`> 540 min`) | Dynamiczny limit 10h, licznik `DailyExtensionsUsedThisWeek`, `TooManyDailyExtensions` przy trzecim | GOTOWE |
| Podwójna obsada | Okno 30h zamiast 24h na odbiór odpoczynku | Dwie karty, tryb załogi | Dłuższe okno regulacyjne (`ODP. DZIENNY` przełącza bazę) | GOTOWE |
| Przerwa slotu 2 w ruchu | Drugi kierowca może odebrać 45 min przerwy podczas jazdy pierwszego | Pojazd w ruchu, slot 2 aktywny | Zeruje jazdę ciągłą drugiej karty, nigdy nie tworzy odpoczynku dobowego | GOTOWE |
| Priorytet przyczyn luki | `CardRemoved > ForwardTimeJump` per karta | Skok czasu przy wyjętej karcie | Brak dodatkowej luki dla tej karty | GOTOWE |
| Brak nakładek w historii kanonicznej | Rekord przychodzący oddaje fragmenty już pokryte przez historię kanoniczną; przedziały są półotwarte `[Start, End)` | Kolejne sesje lub backfill manualny obejmują wcześniejsze minuty | Każda minuta występuje najwyżej raz; niepokryty backfill zostaje zachowany | GOTOWE (beta.10.1) |
| Blok odpoczynku ciągły | Najdłuższy nieprzerwany odcinek; `Inna praca` i `Dyspozycyjność` przerywają | Rozliczanie luki / historia | Brak sumowania rozdzielonych bloków | GOTOWE |
| Ciągłość przez rozliczoną lukę (beta.10) | Odpoczynek zmierzony + rozliczona luka jako `Przerwa/Odpoczynek` = jeden ciągły blok (przed, po lub po obu stronach) | Rozliczenie `CardRemoved` jako odpoczynek | Reset dobowy/tygodniowy na końcu połączonego bloku; blok niesie `SourceGapId` | GOTOWE — bieżący zakres testów terenowych zaliczony; wiele luk pozostaje osobnym zadaniem domenowym |
| Rekompensata tygodniowa | Dług = 45h − zakończony skrócony odpoczynek tygodniowy; spłata en bloc przez jeden blok | Odpoczynek 24–<45h albo wybrana kandydatura hostująca rekompensatę | Zobowiązanie, ścisły `DueAtExclusive`, FIFO i pełny ślad; dla niejednoznacznego bloku 24 h+ wymagany wybór kandydatury użytkownika | GOTOWE (beta.11.1) |
| Rekompensata — podstawa hosta | `540`, `1440` albo `2700` minut zależnie od wybranej roli bloku | RuleEngine generuje dopuszczalne kandydatury | Tylko minuty ponad wybraną podstawę mogą spłacać dług; zakaz podwójnego użycia minut | GOTOWE (beta.11.1) |
| Blokada jazdy przy `CardRemoved` | Włożenie karty z nierozliczoną luką wymusza kreator | Luka `CardRemoved` nierozliczona | Blokada logiczna UI (nie fizyczna — telemetria SCS tylko do odczytu) | GOTOWE |
| `ForwardTimeJump` opcjonalny | Nie blokuje jazdy | Luka typu `ForwardTimeJump` | Ostrzeżenie + opcjonalne rozliczenie | GOTOWE |
| Wspólny skok czasu załogi | Jeden skok pojazdu jest klasyfikowany wspólnie przed przetworzeniem obu kart | Jedna karta odpoczywa, pojazd stoi przed i po, druga karta ma tę samą niejazdową aktywność przed i po | Obie karty otrzymują kontrolowaną rekonstrukcję bez fałszywej luki; bez wymyślania Jazdy lub aktywności niepotwierdzonej | GOTOWE (beta.11.1) |
| Gate `running == 0` | Pauza/menu nie zasila historii ani retencji | Telemetria zgłasza brak aktywnej gry | Brak dopisywania czasu rzeczywistego, brak fałszywego `game_time = 0` | GOTOWE |
| Klasyfikacja i alokacja odpoczynku | Faktyczna długość wyznacza możliwe kandydatury; użytkownik nie wpisuje własnych minut | Zakończenie niejednoznacznego bloku 24 h+ | RuleEngine pokazuje tylko legalne warianty, a użytkownik wybiera jeden z nich; brak decyzji daje `PendingRestAllocation` | GOTOWE (beta.11.1) |
| Reguła pierwszej godziny (multi-manning) | Drugi kierowca musi dołączyć w ciągu 60 min od startu pierwszego | — | — | ODRZUCONE — nie w zakresie |
| Dzielony odpoczynek dobowy 3h+9h | Legalny wariant podziału | — | — | NIEROZPOZNAWANY (known issue) |

---

## 7. Co zostało już wykonane

- Kompletny model domenowy: `ActivityRecord`, `ActivityTimeline`, sesje historii, `ActivityGap`
- Natywny plugin C++ (protokoły v1→v2→v3), wersjonowanie z wykrywaniem niezgodności, seqlock, `world_generation`, znacznik operacji ładunkowej
- Silnik reguł: wszystkie liczniki wymienione w sekcji 6 ze statusem GOTOWE
- Mechanizm `truncate-and-append` obejmujący zarówno rekordy aktywności, jak i luki (otwarte i zamknięte)
- Encja `ActivityGap` z pełną obsługą (Etapy 0–4): wykrycie, priorytet przyczyn, `ResolveGap`, blokada UI, lista w Historii, ostrzeżenie w raportach
- Cztery liczniki statystyk regulacyjnych zbindowane w UI (dashboard, nakładki, menu) wraz z prezentacją przekroczeń bez maskowania (np. `3 / 2` na czerwono)
- Główny LCD przełączony z zegara Windows na `game_time` (`GameClockFormatter`, `--:--` przy braku telemetrii)
- Retencja hot/warm z migracją `AddActivityGaps`
- Migracja SQLite z automatycznym backupem przed `MigrateAsync`
- **Naprawione i pokryte testami regresyjnymi błędy:**
  - przypisanie sesji przy cofnięciu czasu (`UNIQUE constraint` crash)
  - idempotentność zapisu po znaczącym kluczu
  - fałszywa rekonstrukcja wielogodzinnej Jazdy przy skoku czasu (C/8)
  - baza rekompensaty 11h→9h (B/5)
  - utrata aktywności w gałęzi `GamePaused` (beta.9)
  - luka przycięta przez późniejszą gałąź czasu (beta.3→beta.4)
  - nakładające się minuty między sesjami blokujące start aplikacji (`SQLite Error 19`) — beta.10.1
  - sumowanie okruchów rekompensaty z wielu odpoczynków — beta.11
  - licznik pauzy UI wyprzedzający RuleEngine na granicy 44/45 — lokalny hotfix
- **338/338 testów automatycznych** (Core 33, Telemetry.Scs 8, Engine 69,
  RuleEngine 70, Application 50, Reports 9, Infrastructure 51, Desktop 48)
- Kompilacja Release: 0 błędów, 0 ostrzeżeń
- Dwa długie scenariusze terenowe potwierdzone w rzeczywistej grze (2h+7h=9h odpoczynku; wariant tygodniowy 45h)
- Wydania beta.4 → beta.11 z artefaktami i sumami SHA-256; beta.11 ma poprawny `ProductVersion`, ale została wycofana przed smoke testem i nie jest obowiązującą wersją testową
- Usunięcie martwego, nigdy niewidocznego bloku XAML (alternatywna wersja Dashboardu) wraz z powiązanymi zasobami — `MainWindow.xaml` skrócony z 356 do 285 linii, usunięto 8 osieroconych plików z `Assets/` (zachowano `lcd-background.png` i `tachograph-panel.png`)
- **Wizualna weryfikacja UI po tej zmianie — wykonana, test ręczny zaliczony** (Dashboard, przyciski urządzenia, sloty kart, aktywności, tryby, pauza, wydruk, `OperationStatus`, obie nakładki, zakładki, restart)
- Odtworzenie repozytorium git po wykryciu pustego katalogu `.git` (brak historii lokalnie, brak remote) — commit bazowy `e510ed9` na `main`, 198 plików, `output/` wyłączone przez `.gitignore`
- Stała checklista regresji UI po każdej zmianie XAML dodana do `BETA_TEST_PLAN.md` (`51cad1f`); kryteria restartu nakładek doprecyzowane (`0d9e226`)
- Dzień 2 rozszerzonych testów terenowych: pojedyncza luka `CardRemoved` zielona na obu kartach, stabilna po restarcie; slot 2 podczas jazdy domknięty
- Hotfix beta.10.1: `SubtractCoveredRanges`, `EnsureNoOverlap`, `InvalidCanonicalHistoryException`; commity `49e200d` i `906b7d5`
- Pomiar na kopii rzeczywistej bazy potwierdził usunięcie dokładnie jednej zdublowanej minuty na kartę przy zachowaniu 1007 minut backfillu manualnego; brak migracji i zmian danych źródłowych

---

## 8. Aktualny stan prac

**Status wydania:** `0.1.0-beta.11` jest **wycofanym kandydatem**, a nie zatwierdzoną wersją testową. Nie wolno wykonywać na niej końcowego smoke testu ani traktować jej wyniku jako gate'u GO.

**Końcowy stan techniczny beta.11.1:**
- commit restartu SQLite: `87e2fdf`;
- 282/282 testy zielone;
- RuleEngine 62/62;
- Engine 69/69;
- Application 50/50;
- Reports 9/9;
- Infrastructure 51/51;
- build Release: 0 błędów i 0 ostrzeżeń;
- migracja `AddRestAllocationDecisions` i dwa restarty przechodzą na kopii właściwej bazy;
- dwa zakresy Dnia 141 mają audytowane rekordy `AutomaticCrewReconstruction`, a nierozliczone luki referencyjne: 0;
- wycofany ZIP beta.11 ma SHA-256 `73217638efb6271588427a5e3ee889bc40116923c0cf66726e1596cdbc998bb1`.

Zielony pakiet obejmuje oba nowe przypadki graniczne, progi rekompensaty, trwałość decyzji i zabezpieczenia wspólnego skoku załogi.

### 8.1 FIX A — ręczna alokacja bloku odpoczynku i rekompensaty

Beta.11 automatycznie klasyfikuje cały zakończony blok 24 h+ jako odpoczynek tygodniowy, zanim zdecyduje, jaka część jest odpoczynkiem bazowym, a jaka dołączoną rekompensatą. Skutek:

- Staniek, blok `29:53`: stary dług `20:53` nie zostaje spłacony, a powstaje nowy dług `15:07`;
- Doboś, blok `28:52`: stary dług `19:52` nie zostaje spłacony, a powstaje nowy dług `16:08`.

Przyjęta decyzja domenowa:

- po zamknięciu niejednoznacznego bloku 24 h+ RuleEngine generuje dopuszczalne kandydatury;
- użytkownik wybiera kandydaturę, nie wpisuje własnych minut;
- przykładowy wariant Stanka `DailyRestWithCompensation` oznacza `09:00 + 20:53`, spłatę starego długu i brak nowego długu;
- wariant `ReducedWeeklyRestOnly` traktuje cały blok `29:53` jako skrócony tygodniowy, pozostawia stary dług i tworzy nowy `15:07`;
- nie istnieje `DailyRestOnly` dla zamkniętego bloku 24 h+;
- te same minuty nie mogą jednocześnie należeć do podstawy odpoczynku i do rekompensaty;
- decyzja jest trwała, audytowana, wersjonowana i unieważniana po zmianie `RestBlockId`;
- brak decyzji daje `PendingRestAllocation`, ogranicza wiarygodność raportów i blokuje Planer.

### 8.2 FIX B — koordynacja wspólnego skoku czasu załogi

Testy terenowe pozostawiły dwie luki referencyjne. Oryginalna baza dowodowa pozostaje niezmieniona, a na kopii oba zakresy zostały rozliczone audytowaną rekonstrukcją:

| Odpoczywająca karta | Fałszywa luka drugiej karty | Czas gry | Długość |
|---|---|---|---:|
| Staniek, slot 1 | Doboś, slot 2 | Dzień 141, 15:30–15:45 | 15 min |
| Doboś, slot 2 | Staniek, slot 1 | Dzień 141, 18:56–19:15 | 19 min |

Przyczyna:

- obie karty przetwarzają ten sam skok niezależnie;
- karta z `BreakOrRest` przed i po skoku dostaje rekonstrukcję;
- druga karta z `OtherWork` albo `Availability` dostaje `ForwardTimeJump`;
- wynik jest symetryczny i zależy tylko od tego, która karta w danej chwili odpoczywa.

Przyjęta reguła:

- `CrewTachographEngine` klasyfikuje wspólny skok raz przed przetworzeniem kart;
- odpoczynek jednej karty może wyjaśnić wspólny postój pojazdu;
- druga karta zachowuje wyłącznie własną stabilną aktywność przed i po skoku;
- dopuszczalne rekonstrukcje: `BreakOrRest`, `OtherWork`, `Availability`;
- niedopuszczalne: `Driving`, pusty slot, karta wyjęta, aktywność zmieniona przez skok;
- wynik musi być niezależny od kolejności przetwarzania S1/S2.

### 8.3 Wspólny plan beta.11.1

Obowiązujący dokument wykonawczy:

> `PLAN_NAPRAWCZY_BETA_11_1.md`

Oba FIX-y mają osobne testy i commity oraz przeszły wspólny gate beta.11.1.

---

## 9. Status problemów i ryzyk

| Problem / ryzyko | Wpływ | Stan / przyczyna | Działanie | Priorytet |
|---|---|---|---|---|
| Automatyczna klasyfikacja bloku 24 h+ hostującego rekompensatę | Błędna spłata starego długu i powstanie nowego zobowiązania | Naprawione: kandydatury RuleEngine i audytowana decyzja użytkownika | Utrzymywać regresje ALC-01–08 | **Zamknięte w beta.11.1** |
| Fałszywe luki drugiej karty podczas wspólnego skoku | Niekompletna historia, błędne raporty i konieczność zbędnego wpisu manualnego | Naprawione: wspólny `CrewTimeJumpResolution` i symetryczne regresje Dnia 141 | Utrzymywać zabezpieczenia Jazdy, pustego slotu, karty wyjętej i zmiany aktywności | **Zamknięte w beta.11.1** |
| Nierozstrzygnięta alokacja odpoczynku | Stan regulacyjny i Planer mogą zależeć od interpretacji użytkownika | Brak zapisanej `RestAllocationDecision` | Status `PendingRestAllocation`, ostrzeżenie, brak automatycznej spłaty i blokada wiarygodnego planowania | P1 |
| Licznik pauzy UI 44/45 min | Dashboard lub overlay pokazywał zaliczenie minutę przed RuleEngine | Naprawione lokalnie: `CurrentContinuousBreakMinutes` z bieżącego bloku po regule jednej minuty | Utrzymywać testy graniczne 41+3=44 i 45=reset; moving break slotu 2 pozostawić osobno | **Zamknięte lokalnie 2026-07-24** |
| Migracja i trwałość decyzji alokacji | Ryzyko utraty wyboru lub związania go ze zmienionym blokiem | Sprawdzone na kopii bazy i po restarcie SQLite | Utrzymywać wersję schematu, `Superseded` i `Invalidated` | Kontrola regresyjna |
| Korekta dwóch luk referencyjnych | Ręczny DELETE zniszczyłby dowód i audyt | Wykonana na kopii jako `AutomaticCrewReconstruction`; źródłowe luki zachowane jako rozliczone | Nie modyfikować oryginalnej bazy dowodowej | **Zamknięte na kopii** |
| Ciągłość przez wiele rozliczonych luk | Długi rzeczywisty odpoczynek może pozostać rozbity | Osobny nierozstrzygnięty przypadek domenowy | Nie mieszać z beta.11.1 | P2 |
| Log `APP_START_FAILED` pomija `InnerException` | Wydłuża diagnozę awarii EF/SQLite | Logowany jest tylko wyjątek zewnętrzny | Osobne zadanie diagnostyczne po gate'cie | P2 |
| Numeracja dni w analizach surowych | Ryzyko fałszywego zgłoszenia błędu o jeden dzień | UI stosuje `floor(GameMinute / 1440) + 1` | Zawsze stosować `+1` przy porównaniu minut z UI/CSV/PDF | Procesowy |
| Ryzyko rozrostu zakresu | Opóźnienie beta.11.1 i Planera | Dwa FIX-y dotykają kilku warstw | Trzymać się planu, osobnych commitów i wspólnego gate'u | Procesowy |

---

## 10. Decyzje projektowe

### 10.1 Rozstrzygnięte dla beta.11.1

**Ręczna alokacja odpoczynku**
- decyzja następuje po zamknięciu bloku;
- RuleEngine generuje legalne kandydatury;
- użytkownik wybiera `CandidateId`, nie minuty;
- `DailyRestOnly` nie istnieje dla zamkniętego bloku 24 h+;
- zmiana decyzji zachowuje poprzednią wersję jako `Superseded`;
- zmiana kanonicznego bloku unieważnia decyzję.

**Wspólny skok czasu załogi**
- skok jest zdarzeniem pojazdu, nie dwóch niezależnych kart;
- klasyfikacja powstaje raz w `CrewTachographEngine`;
- aktywność drugiej karty pochodzi wyłącznie z jej własnego stabilnego stanu;
- nie rekonstruuje się Jazdy ani stanu karty wyjętej;
- wynik nie zależy od kolejności slotów.

**Wydanie**
- beta.11 pozostaje wycofana;
- obowiązujący numer to `0.1.0-beta.11.1`;
- artefakt ma osobny ZIP i SHA-256;
- oba FIX-y przeszły wspólny gate automatyczny.

### 10.2 Nadal nierozstrzygnięte poza bieżącym gate'em

1. Ciągłość odpoczynku przez wiele różnych `SourceGapId`.
2. Publikacja repozytorium i licencja.
3. Model komercjalizacji i wsparcia.
4. Cold retention, instalator, podpis kodu i auto-update.

---

## 11. Lista zadań

### Priorytet 1 — wspólny gate beta.11.1 — zakończony

#### 1. Zamrożenie dowodów
- [x] oznaczyć beta.11 jako wycofaną w dokumentacji;
- [x] zachować dwie nierozliczone luki Dnia 141;
- [x] zachować kopię i hash bazy referencyjnej;
- [x] potwierdzić baseline 262/262 oraz build 0/0.

#### 2. Kontrakty i specyfikacja
- [x] dodać `RestAllocationCandidate`, `RestAllocationDecision` i wersję schematu;
- [x] dodać `CrewTimeJumpResolution`;
- [x] opisać `PendingRestAllocation`, `Superseded` i `ResolutionSource`;
- [x] zaktualizować specyfikację i macierze testowe przed implementacją.

#### 3. Czerwone testy FIX A
- [x] Staniek `29:53`;
- [x] Doboś `28:52`;
- [x] próg o minutę za mało;
- [x] `44:53` i zakaz podwójnego użycia minut;
- [x] `65:53` = `45:00 + 20:53`;
- [x] brak decyzji;
- [x] restart SQLite;
- [x] zmiana historii i unieważnienie decyzji.

#### 4. Czerwone testy FIX B
- [x] `CREW-JUMP-01`: Dzień 141, 15:30–15:45;
- [x] `CREW-JUMP-02`: Dzień 141, 18:56–19:15;
- [x] żadna karta nie odpoczywa;
- [x] druga karta ma `Driving`;
- [x] aktywność drugiej karty zmienia się;
- [x] karta wyjęta;
- [x] pusty slot;
- [x] odwrócona kolejność przetwarzania slotów.

#### 5. Implementacja w osobnych commitach
- [x] `fix(engine): koordynuj długie skoki czasu między kartami`;
- [x] `feat(compensation): dodaj audytowaną decyzję alokacji bloku odpoczynku`;
- [x] Application i persistence decyzji;
- [x] UI wariantów;
- [x] PDF/CSV/JSON pełnego śladu;
- [x] restart i unieważnianie decyzji.

#### 6. Korekta danych referencyjnych
- [x] najpierw uruchomić nowy algorytm na kopii bazy;
- [x] potwierdzić brak obu fałszywych luk;
- [x] nie wykonywać ręcznego DELETE;
- [x] wykonać audytowaną rekonstrukcję i zachować pierwotny ślad.

#### 7. Release gate
- [x] pełny pakiet testów zielony;
- [x] build Release 0/0;
- [x] migracja na kopii właściwej bazy;
- [x] zgodność UI, PDF, CSV, JSON i restartu;
- [x] czyste drzewo poza lokalnym katalogiem `.claude/`;
- [x] self-contained `win-x64`;
- [x] ZIP beta.11.1 i plik SHA-256 wygenerowany obok paczki;
- [x] smoke test wyłącznie na artefakcie beta.11.1;
- [x] decyzja GO/FIX/HOLD: **GO**, 23 lipca 2026.

### Priorytet 2 — po GO beta.11.1

- końcowa akceptacja specyfikacji Planera podróży;
- osobna gałąź Planera;
- kontrakty i czerwone testy P0 bez UI;
- logowanie pełnego łańcucha `InnerException`;
- osobna specyfikacja wielu luk w jednym odpoczynku.

---

## 12. Rekomendowany następny krok

**Gate beta.11.1 jest zamknięty wynikiem GO. Przed rozpoczęciem kolejnego
większego zakresu podjąć osobną decyzję: test-first hotfix licznika pauzy
44/45 min, lokalizacja PL/EN albo Planer podróży.**

Zakończona kolejność techniczna:

```text
zamrożenie dowodów ✓
→ kontrakty domenowe ✓
→ czerwone testy Engine i RuleEngine ✓
→ dwa osobne FIX-y ✓
→ Application / persistence / UI / raporty ✓
→ korekta danych na kopii ✓
→ pełny gate automatyczny ✓
→ artefakt beta.11.1 ✓
→ smoke terenowy ✓
→ GO ✓
```

Nie wykonywać obecnie:
- publikacji nowej wersji beta;
- ręcznego rozliczania lub usuwania dwóch luk w oryginalnej bazie dowodowej;
- implementacji Planera podróży bez osobnej decyzji właściciela projektu.

---

## 13. Skrócony kontekst startowy

*(wersja do wklejenia jako pierwsza wiadomość w nowym oknie kontekstowym)*

**Cel projektu:** ETS2 EU Digital Tachograph to aplikacja .NET 9/WPF/SQLite z natywnym pluginem C++ SCS Telemetry SDK. Prowadzi historię dwóch kart w `game_time`, wylicza reguły UE 561/2006, obsługuje cofnięcia i skoki czasu, luki, raporty oraz nakładki.

**Status:** bazą wydaniową pozostaje `0.1.0-beta.11.1`. Bieżący katalog
roboczy zawiera nieopublikowany wariant B wpisu manualnego, katalog ISO oraz
korektę `ODP. TYG.` i hotfix licznika pauzy 44/45. Ma 338/338 zielonych testów
i build Release 0/0. Nie tworzyć ani nie publikować nowej bety bez osobnej
decyzji.

**FIX A — rekompensaty:** wdrożono ręczną, audytowaną decyzję po zamknięciu bloku 24 h+. RuleEngine generuje legalne `RestAllocationCandidate`, a użytkownik wybiera `CandidateId`. Staniek `29:53`: `DailyRestWithCompensation = 09:00 + 20:53`, stary dług spłacony, brak nowego; `ReducedWeeklyRestOnly` pozostawia stary dług i tworzy `15:07`. Doboś `28:52` analogicznie tworzy `16:08` w wariancie tygodniowym. `DailyRestOnly` nie istnieje dla bloku 24 h+. Zakaz podwójnego użycia minut.

**FIX B — skoki załogi:** `CrewTachographEngine` klasyfikuje wspólny skok raz i przekazuje obu kartom wspólny kontekst. Druga karta zachowuje tylko własną stabilną aktywność `BreakOrRest`, `OtherWork` albo `Availability`; nigdy Jazdę, pusty slot ani kartę wyjętą. Dwie luki referencyjne Dnia 141 zostały audytowalnie rozliczone na kopii bazy jako `AutomaticCrewReconstruction`, bez usuwania źródłowego śladu.

**Zasada nadrzędna:** historia minutowa pozostaje źródłem faktów. `RestAllocationDecision` jest audytowaną interpretacją sposobu wykorzystania zakończonego bloku i nie zmienia `ActivityRecord`.

**Gate beta.11.1:** 282/282; Core 33, Telemetry.Scs 8, Engine 69, RuleEngine 62, Application 50, Reports 9, Infrastructure 51; build 0/0. Właściwy SHA-256 ZIP-a beta.11.1 znajduje się w pliku `.zip.sha256` obok paczki.

**Gate terenowy:** smoke artefaktu beta.11.1 z aktywną telemetrią zaliczony
23 lipca 2026; wszystkie testy zielone, decyzja **GO**.

**Hotfix licznika pauzy:** `RegulationState.CurrentContinuousBreakMinutes`
wyznacza długość bieżącego bloku `BreakOrRest` sięgającego `Now`. Liczniki UI
slotów 1 i 2 korzystają z tej wartości; moving break slotu 2 pozostał bez zmian.
Gate po hotfixie: 315/315, build Release 0/0. Raport:
`BUGFIX_REPORT_QUALIFIED_BREAK_COUNTER_2026-07-24.md`.

**Najbliższe zadanie:** wykonać manualną weryfikację wizualną 44→45 w ETS2 albo
wybrać osobny kolejny zakres prac. Planer podróży pozostaje wstrzymany do
decyzji właściciela projektu.
