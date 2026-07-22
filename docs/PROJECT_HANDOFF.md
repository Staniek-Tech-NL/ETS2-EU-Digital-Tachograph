# PROJECT HANDOFF — ETS2 EU Digital Tachograph

**Wersja projektu:** 0.1.0-beta.11
**Data przygotowania dokumentu:** 20 lipca 2026
**Ostatnia aktualizacja:** 22 lipca 2026 — pełny model rekompensat wdrożony; kandydat beta.11 przygotowywany do końcowego smoke testu terenowego
**Przeznaczenie:** pakiet startowy dla nowej sesji AI / nowego okna kontekstowego

---

## 1. Streszczenie projektu

ETS2 EU Digital Tachograph to samodzielna aplikacja desktopowa (.NET 9, C#, WPF) symulująca cyfrowy tachograf zgodny z logiką rozporządzenia UE 561/2006, przeznaczona dla graczy Euro Truck Simulator 2 grających w trybie singleplayer. Aplikacja odczytuje oficjalną telemetrię gry przez natywny plugin C++ oparty na SCS SDK, prowadzi historię aktywności dwóch kart kierowców (podwójna obsada) w czasie gry (`game_time`), wylicza liczniki regulacyjne (jazda ciągła, dzienna, tygodniowa, dwutygodniowa, odpoczynki, rekompensaty), obsługuje nietypowe zachowania czasu gry (cofnięcia, skoki, pauzy, operacje ładunkowe) i generuje raporty (PDF, CSV, JSON dla VTC, własny format `.tacho`).

Projekt powstał jako reakcja na niedociągnięcia istniejących na rynku narzędzi tego typu (m.in. aplikacja „Emre54", tacho wbudowane w Ox-Ram VTC) — użytkownik napotkał w nich błędy (niezerujący się licznik dzienny, brak wsparcia dla podwójnej obsady, brak logowania) i zbudował własne rozwiązanie od zera, jednocześnie naprawiając analogiczne klasy błędów, które by odziedziczył.

Użytkownikiem docelowym jest gracz ETS2 ceniący realizm (w tym społeczność VTC), a autor projektu jest jednocześnie jego głównym testerem i deweloperem (samouk, korzysta z Claude Code / Codex jako wsparcia).

**Aktualny etap:** wersja **0.1.0-beta.11**, 262/262 testy automatyczne zielone, kompilacja Release bez błędów i ostrzeżeń. Uproszczony model rekompensat został zastąpiony spłatą en bloc z terminem wyłącznym, stabilnymi identyfikatorami i pełnym śladem. Referencje terenowe wynoszą Staniek `1253 min / 20:53` i Doboś `1192 min / 19:52`. RuleEngine, DTO, UI, PDF, CSV, JSON oraz integracyjny restart SQLite są zamknięte; pozostał osobisty końcowy smoke test testera z aktywną telemetrią.

---

## 2. Aktualny zakres projektu

### Funkcje podstawowe
- Odczyt telemetrii ETS2 przez natywny plugin C++ (SCS SDK 1.14) — **[GOTOWE]**
- Model domenowy: karta kierowcy, sesje historii, minutowe rekordy aktywności (`ActivityRecord`) — **[GOTOWE]**
- Silnik reguł: jazda ciągła (4h30), przerwa (45 min, w tym podział 15+30), odpoczynek dobowy (9h/11h), tygodniowy (24h/45h), limity 56h/90h — **[GOTOWE]**
- Podwójna obsada: dwa niezależne sloty kart, okno 30h zamiast 24h, specjalna przerwa 45 min dla slotu 2 w ruchu — **[GOTOWE]**
- Obsługa cofnięć i skoków czasu gry (`truncate-and-append`) — **[GOTOWE]**
- Kanoniczna projekcja bez nakładających się minut (`SubtractCoveredRanges` + `EnsureNoOverlap`) — **[GOTOWE]** w beta.10.1
- Jawne luki aktywności (`ActivityGap`) i wpisy manualne (`ManualEntryService.ResolveGap`) — **[GOTOWE]**
- Ciągłość odpoczynku przez rozliczoną lukę `CardRemoved` (beta.10) — **[GOTOWE]**; bieżący zakres testów terenowych zaliczony, wiele luk pozostaje osobnym zadaniem domenowym
- Rozpoznawanie operacji załadunku/rozładunku (protokół v3) — **[GOTOWE]**
- Raporty PDF/CSV/JSON VTC/`.tacho` z informacją o kompletności dowodu — **[GOTOWE]**
- Retencja danych (hot/warm) — **[GOTOWE]**; cold — **[DO ZROBIENIA]** (hak przygotowany)
- Statystyki regulacyjne w UI (praca dobowa, wydłużenia, skrócone odpoczynki, rekompensaty) — **[GOTOWE]**
- Pełne rekompensaty tygodniowe: en bloc, FIFO, ścisły termin, stabilne identyfikatory i ślad spłaty — **[GOTOWE]** w beta.11
- Warstwowa prezentacja rekompensat oraz pełne eksporty PDF/CSV/JSON — **[GOTOWE]** w beta.11
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
- Kreator wpisu manualnego (blokujący dla `CardRemoved`, opcjonalny dla `ForwardTimeJump`) — **[GOTOWE]**
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
| Rekonstrukcja skoków czasu | Mały skok (≤2 min) → rekonstrukcja ostatnią aktywnością (również Jazdą); duży skok po Jeździe → zawsze luka; duży skok przy odpoczynku → rekonstrukcja tylko gdy pojazd stał przed i po; inne aktywności → opcjonalna luka; potwierdzony załadunek/rozładunek → wybrana aktywność bez luki | Naprawa realnego bugu (fałszywa wielogodzinna Jazda z powietrza) | Osobna klasyfikacja per typ aktywności i per slot |
| Wykrywanie operacji ładunkowych | Protokół v3, znacznik generacji operacji z oficjalnych zdarzeń SCS | Bez sygnału z gry nie dało się odróżnić skoku czasu od przesunięcia przy załadunku | Wymagało 4 iteracji (beta.6–beta.9); rzeczywista przyczyna: stan aktywności gubiony w gałęzi `GamePaused` |
| Reguła pierwszej godziny (multi-manning) | **Odrzucona z zakresu** | Zbyt złożona (retroaktywna maszyna stanów), niska widoczność względem nakładu | Zaprojektowana koncepcyjnie, niezakodowana |
| Testy regresyjne | Każdy znaleziony bug dostaje dedykowany test odtwarzający dokładny scenariusz | Zapobieganie powrotowi tej samej klasy błędu | Test `03:53 + 01:34 = 05:27` jako stały punkt odniesienia |
| Repozytorium git | Lokalne repo ma powstać **przed** publikacją, niezależnie od niej | Bezpieczne cofanie zmian wprowadzanych przez agentów AI | Repo odtworzone i zwersjonowane — commit bazowy `e510ed9` na `main`; `output/` (1,26 GB paczek) wyłączone przez `.gitignore` |

---

## 4. Aktualna architektura

**Główne komponenty (projekty w rozwiązaniu):**

- `ETS2Tachograph.Core` — model domenowy, czas gry, `ActivityTimeline`, reguła jednej minuty, `GameClockFormatter`
- `ETS2Tachograph.Telemetry.Scs` — odczyt wersjonowanej pamięci współdzielonej (protokół v3)
- `ETS2Tachograph.Engine` — klasyfikacja ramek telemetrii, sesje, luki (`ActivityHistoryProcessor`, `CrewTachographEngine`), snapshoty
- `ETS2Tachograph.RuleEngine` — liczniki regulacyjne, naruszenia, rekompensaty (`RegulationEngine`, `RegulationEvaluation`, `RegulationState`, `WeeklyRestCompensation`, `CompensationSummary`)
- `ETS2Tachograph.Infrastructure` — SQLite, EF Core, repozytoria, migracje, retencja i kanoniczna projekcja historii (`Canonicalize`, `SubtractCoveredRanges`, `EnsureNoOverlap`)
- `ETS2Tachograph.Application` — przypadki użycia, DTO, import/eksport, wpisy manualne (`ManualEntryService`, `ActivityGapService`, `ManualEntryWizardDraft`)
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
│   ├── ETS2Tachograph.Engine.Tests/          (64 testy)
│   │   ├── ActivityHistoryProcessorTests.cs
│   │   ├── CrewTachographEngineTests.cs
│   │   └── ManualEntryLockTests.cs
│   ├── ETS2Tachograph.RuleEngine.Tests/      (55 testów)
│   │   └── RegulationEngineTests.cs
│   ├── ETS2Tachograph.Application.Tests/     (45 testów)
│   │   └── ManualEntryWizardDraftTests.cs
│   ├── ETS2Tachograph.Reports.Tests/         (9 testów)
│   └── ETS2Tachograph.Infrastructure.Tests/  (48 testów)
│       ├── CanonicalProjectionTests.cs         (14 testów regresyjnych beta.10.1)
│       └── WeeklyRestCompensationSqliteRestartTests.cs
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
│   └── JOURNEY_PLANNER_MVP_PLAN.md
├── output/releases/                    [ignorowane przez git — 1,26 GB paczek]
│   └── ETS2Tachograph-0.1.0-beta.11-win-x64.zip
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
| Rekompensata tygodniowa | Dług = 45h − rzeczywisty zamknięty odpoczynek | Odpoczynek 24–<45h | Zobowiązanie przypisane do tygodnia, ścisły `DueAtExclusive`, atomowa spłata en bloc przez jeden odpoczynek ≥9 h, deterministyczne FIFO i pełny ślad | GOTOWE (beta.11) |
| Rekompensata — baza dołączenia | 9h jako próg nadwyżki dołączalnej | Odpoczynek dłuższy niż wymagany | Poprawka bugu B/5 | GOTOWE |
| Blokada jazdy przy `CardRemoved` | Włożenie karty z nierozliczoną luką wymusza kreator | Luka `CardRemoved` nierozliczona | Blokada logiczna UI (nie fizyczna — telemetria SCS tylko do odczytu) | GOTOWE |
| `ForwardTimeJump` opcjonalny | Nie blokuje jazdy | Luka typu `ForwardTimeJump` | Ostrzeżenie + opcjonalne rozliczenie | GOTOWE |
| Gate `running == 0` | Pauza/menu nie zasila historii ani retencji | Telemetria zgłasza brak aktywnej gry | Brak dopisywania czasu rzeczywistego, brak fałszywego `game_time = 0` | GOTOWE |
| Klasyfikacja z faktycznej długości | Cel wybrany w UI nie ma mocy regulacyjnej | Zakończenie odpoczynku | RuleEngine klasyfikuje z rzeczywistej długości bloku; UI pokazuje osobno „Wybrany cel" i „Zakwalifikowano" | GOTOWE |
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
- **262/262 testy automatyczne** (Core 33, Telemetry.Scs 8, Engine 64, RuleEngine 55, Application 45, Reports 9, Infrastructure 48)
- Kompilacja Release: 0 błędów, 0 ostrzeżeń
- Dwa długie scenariusze terenowe potwierdzone w rzeczywistej grze (2h+7h=9h odpoczynku; wariant tygodniowy 45h)
- Wydania beta.4 → beta.11 z artefaktami i sumami SHA-256; beta.11 ma poprawny `ProductVersion` i numer widoczny w UI
- Usunięcie martwego, nigdy niewidocznego bloku XAML (alternatywna wersja Dashboardu) wraz z powiązanymi zasobami — `MainWindow.xaml` skrócony z 356 do 285 linii, usunięto 8 osieroconych plików z `Assets/` (zachowano `lcd-background.png` i `tachograph-panel.png`)
- **Wizualna weryfikacja UI po tej zmianie — wykonana, test ręczny zaliczony** (Dashboard, przyciski urządzenia, sloty kart, aktywności, tryby, pauza, wydruk, `OperationStatus`, obie nakładki, zakładki, restart)
- Odtworzenie repozytorium git po wykryciu pustego katalogu `.git` (brak historii lokalnie, brak remote) — commit bazowy `e510ed9` na `main`, 198 plików, `output/` wyłączone przez `.gitignore`
- Stała checklista regresji UI po każdej zmianie XAML dodana do `BETA_TEST_PLAN.md` (`51cad1f`); kryteria restartu nakładek doprecyzowane (`0d9e226`)
- Dzień 2 rozszerzonych testów terenowych: pojedyncza luka `CardRemoved` zielona na obu kartach, stabilna po restarcie; slot 2 podczas jazdy domknięty
- Hotfix beta.10.1: `SubtractCoveredRanges`, `EnsureNoOverlap`, `InvalidCanonicalHistoryException`; commity `49e200d` i `906b7d5`
- Pomiar na kopii rzeczywistej bazy potwierdził usunięcie dokładnie jednej zdublowanej minuty na kartę przy zachowaniu 1007 minut backfillu manualnego; brak migracji i zmian danych źródłowych

---

## 8. Aktualny stan prac

**Obowiązująca wersja testowa:** `0.1.0-beta.11`. Wersje beta.10 i beta.10.1 nie powinny być używane do oceny rekompensat.

**Ostatnia praca nad kodem:** naprawa kanonicznej projekcji historii po błędzie blokującym start aplikacji. `Canonicalize` dopisywał rekordy nowej sesji bez odejmowania minut już obecnych w projekcji. Duplikaty ujawniły się podczas przebudowy `WarmActivityBlocks` po rozliczeniu luk i kończyły się `SQLite Error 19`.

**Wdrożona poprawka:**
- `SubtractCoveredRanges` pozostawia wyłącznie niepokryte fragmenty rekordów;
- `EnsureNoOverlap` weryfikuje pełny niezmiennik `End <= next.Start` po kanonizacji i przed budową bloków warm;
- `InvalidCanonicalHistoryException` zgłasza konflikt w warstwie domenowej przed zapisem do SQLite;
- istniejąca historia kanoniczna ma pierwszeństwo, a backfill manualny zachowuje niepokryte minuty.

**Model rekompensat beta.11:**
- dług powstaje wyłącznie z zakończonego kanonicznego skróconego odpoczynku tygodniowego;
- spłata wymaga pełnej kwoty w jednym kwalifikującym bloku odpoczynku ≥9 h;
- zobowiązania mają stabilne `ObligationId`, `SourceRestBlockId` i `PaymentRestBlockId`, termin wyłączny, status oraz zakres i moment spłaty;
- bieżący stan jest zawsze przeliczany z historii kanonicznej;
- Dashboard i nakładki pokazują podsumowanie, zakładka `Rekompensaty` pełny ślad, a PDF/CSV/JSON pełne dane eksportowe;
- referencje regresyjne: Staniek `1253 min / 20:53`, Doboś `1192 min / 19:52`.

**Weryfikacja beta.11:**
- 262/262 testy zielone;
- build Release: 0 błędów i 0 ostrzeżeń;
- pełny kontrakt otwartego i spłaconego zobowiązania identyczny po zamknięciu i ponownym otwarciu plikowej bazy SQLite;
- archiwizacja warm idempotentna;
- zero nakładek i zdublowanych początków;
- brak nowej migracji EF Core;
- paczka: `ETS2Tachograph-0.1.0-beta.11-win-x64`;
- końcowy smoke test terenowy z aktywną telemetrią pozostaje do wykonania osobiście przez testera.

**Testy terenowe reguły beta.10/10.1:**
- pojedyncza rozliczona luka `CardRemoved` — zaliczona na obu kartach (Dzień 2);
- wynik zgodny między RuleEngine, UI i stanem po restarcie;
- slot 2 podczas jazdy — domknięty i wycofany z dalszego planu;
- **smoke test świeżej telemetrii — zaliczony**: normalna sesja, nowe rekordy, czyste zamknięcie i restart bez `SQLite Error 19` ani `InvalidCanonicalHistoryException`;
- **Dzień 3, luka na granicy tygodnia regulacyjnego — zaliczony**;
- interakcja z rekompensatą tygodniową została naprawiona w beta.11 i ma zatwierdzone dane referencyjne oraz regresje;
- wiele rozliczonych luk w jednym bloku pozostaje osobnym zadaniem domenowym i nie jest częścią hotfiksa kanonizacji.

**Najbliższy punkt kontynuacji:** osobisty końcowy smoke test beta.11: zgodność Stanka i Dobosia w UI i eksportach, restart aplikacji oraz automatyczna Jazda i blokady przy aktywnej telemetrii.

---

## 9. Otwarte problemy i ryzyka

| Problem / ryzyko | Wpływ | Stan / przyczyna | Działanie | Priorytet |
|---|---|---|---|---|
| ~~Końcowy smoke test beta.10.1 na świeżej telemetrii~~ | — | — | **ZAMKNIĘTE** — smoke test zaliczony: nowa sesja, czyste zamknięcie, restart bez `SQLite Error 19` i `InvalidCanonicalHistoryException` | — |
| ~~Luka na granicy tygodnia regulacyjnego~~ | — | — | **ZAMKNIĘTE (Dzień 3)** — scenariusz zaliczony terenowo | — |
| Ciągłość przez wiele rozliczonych luk | Historia terenowa pokazała inną ścieżkę scalania i niepełne łączenie bloków | Zakres beta.10 obejmował pojedynczą lukę; intencja blokady przed łączeniem wpisów manualnych nie jest rozstrzygnięta | Osobna decyzja domenowa i specyfikacja; nie poprawiać przy okazji | Średni, poza bieżącym gate’em |
| Log `APP_START_FAILED` pomija `InnerException` | Wydłuża diagnozę awarii bazy lub EF | Logowany jest tylko wyjątek zewnętrzny | Dodać bezpieczne logowanie łańcucha wyjątków w osobnym zadaniu diagnostycznym | Średni |
| ~~Uproszczony model rekompensat tygodniowych~~ | — | — | **NAPRAWIONE W BETA.11** — atomowa spłata en bloc, ścisły termin, FIFO, stabilne identyfikatory i pełny ślad | — |
| Numeracja dni w analizach surowych | Ryzyko fałszywego zgłoszenia błędu o jeden dzień | UI stosuje `floor(GameMinute / 1440) + 1` | Traktować `+1` jako niezmiennik przy porównaniu minut z UI/CSV/PDF | Średni procesowy |
| Ryzyko rozrostu zakresu | Opóźnienie zamknięcia testów i Planera | Sąsiednie obserwacje kuszą do zmian „przy okazji” | Zachować gate’y: hotfix → smoke → dwa testy → decyzja GO/FIX/HOLD | Średni |

---

## 10. Nierozstrzygnięte decyzje

**1. Ciągłość odpoczynku przez wiele wpisów manualnych**
- Pytanie: czy wymaganie, aby jedna strona sklejenia pochodziła z telemetrii, jest celową ochroną przed łączeniem dwóch wpisów manualnych?
- Opcja A: zachować ograniczenie — mniejsze ryzyko fałszywej ciągłości, ale bloki z wieloma lukami mogą pozostać rozbite.
- Opcja B: dopuścić łączenie po ścisłym pokryciu i audycie — pełniejsza rekonstrukcja, ale wymaga osobnej specyfikacji i testów nadużyć.
- Rekomendacja: decyzję odłożyć poza gate beta.10.1; nie mieszać z hotfiksem kanonizacji.

**2. Publikacja repozytorium i forma wydania**
- Opcje: publiczne repo po zamknięciu bety, repo prywatne albo brak publikacji.
- Rekomendacja: publiczne repo dopiero po testach i uporządkowaniu README, licencji oraz warunków SCS SDK.

**3. Model komercjalizacji**
- Opcje: bezpłatne, bezpłatne z dobrowolnym wsparciem albo płatne.
- Rekomendacja: bezpłatne lub dobrowolne wsparcie, aby ograniczyć zobowiązania supportowe.

---

## 11. Lista zadań

### Priorytet 1 — zakończone

- ✅ Ręczna weryfikacja UI po usunięciu martwego XAML.
- ✅ Odtworzenie repozytorium i commit bazowy `e510ed9`.
- ✅ Stała checklista regresji XAML w `BETA_TEST_PLAN.md` (`51cad1f`).
- ✅ Usunięcie nieaktualnej sekcji instrukcji AI z handoffu (`caec0af`).
- ✅ Doprecyzowanie trwałości nakładek: pozycja trwała, widoczność nietrwała (`0d9e226`).
- ✅ Dzień 2 testów terenowych: pojedyncza luka `CardRemoved` zielona na obu kartach; slot 2 domknięty.
- ✅ Naprawa nakładek kanonicznej historii i wydanie beta.10.1 (`49e200d`, `906b7d5`).
- ✅ Smoke test beta.10.1 na świeżej telemetrii: nowa sesja, czyste zamknięcie, restart bez `SQLite Error 19` ani `InvalidCanonicalHistoryException`.
- ✅ Dzień 3: luka przecinająca granicę tygodnia regulacyjnego — zaliczona terenowo.
- ✅ Pełny model rekompensat beta.11, dane referencyjne Stanka i Dobosia, UI, eksporty oraz restart SQLite.

### Priorytet 2 — najbliższe kroki

**2.1 Końcowy smoke test terenowy beta.11**
- Staniek `1253 min / 20:53`, Doboś `1192 min / 19:52`;
- zgodność Dashboardu, szczegółów, PDF, CSV i JSON;
- identyczne wyniki po restarcie aplikacji;
- automatyczna Jazda i blokady zależne od ruchu przy aktywnej telemetrii.

**2.2 Dokumentacja po gate’cie**
- `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md` i handoff zaktualizowane dla beta.11;
- po smoke teście dopisać wynik terenowy i decyzję GO/FIX/HOLD;
- potwierdzić czyste repozytorium.

### Priorytet 3 — po decyzji GO

- Planer podróży MVP „Najwcześniejsza legalna”: najpierw końcowa akceptacja specyfikacji, następnie gałąź funkcjonalna, kontrakty i czerwone testy P0 — bez UI na Etapie 1.
- Logowanie pełnego łańcucha `InnerException`.
- Osobna specyfikacja ciągłości przez wiele rozliczonych luk.
- Warstwa zimnej retencji, instalator, podpis kodu i auto-update.

---

## 12. Rekomendowany następny krok

**Wykonać osobisty końcowy smoke test terenowy beta.11.**

Gate terenowy jest funkcjonalnie zamknięty:
- ✅ smoke test świeżej telemetrii — aplikacja dwukrotnie doszła do `APP_READY`, zamknięcie kodem 0, brak `SQLite Error 19` i `InvalidCanonicalHistoryException`;
- ✅ Dzień 2 — pojedyncza rozliczona luka `CardRemoved` na obu kartach;
- ✅ Dzień 3 — luka na granicy tygodnia regulacyjnego;
- ✅ pełny model rekompensat, dane referencyjne, eksporty i restart SQLite — zamknięte automatycznie;
- ⏳ końcowa aktywna telemetria i blokady ruchu — do wykonania osobiście przez testera.

**Po decyzji GO:** otworzyć implementację Planera podróży — najpierw końcowa akceptacja specyfikacji, potem gałąź funkcjonalna, kontrakty i czerwone testy P0.

**Uwaga o danych:** po incydencie z 21.07 aktywna baza mogła zostać podmieniona; stan katalogu danych w `%LocalAppData%` potwierdzić przed dalszą pracą. Kopia z incydentu leży w `output\ODZYSK-BAZY`.

---

## 13. Skrócony kontekst startowy

*(wersja do wklejenia jako pierwsza wiadomość w nowym oknie kontekstowym)*

**Cel projektu:** ETS2 EU Digital Tachograph to aplikacja desktopowa .NET 9/WPF/SQLite z natywnym pluginem C++ SCS Telemetry SDK, symulująca tachograf dla ETS2. Prowadzi historię dwóch kart w `game_time`, liczy limity UE 561/2006, obsługuje cofnięcia/skoki czasu i luki aktywności oraz generuje raporty.

**Obowiązująca wersja:** `0.1.0-beta.11`. Build Release: 0 błędów i 0 ostrzeżeń; 262/262 testy zielone.

**Zasada nadrzędna:** historia minutowa jest jedynym źródłem prawdy. Liczniki, raporty, bloki warm i projekcje są zawsze wyliczane. Wszystko używa `game_time`, nigdy zegara systemowego.

**Kanonizacja beta.10.1:** kolejne sesje są gałęziami czasu. Nowa sesja przejmuje oś od swojej kotwicy w górę przez `TruncateAfter`; poniżej kotwicy może tylko uzupełniać niepokryte minuty. O przynależności minuty decyduje pokrycie, nie położenie wobec kotwicy. Istniejąca historia kanoniczna ma pierwszeństwo; niepokryty backfill manualny zostaje. `SubtractCoveredRanges` odejmuje pokryte zakresy, `EnsureNoOverlap` wymusza brak nakładek, a `InvalidCanonicalHistoryException` wykrywa konflikt przed SQLite.

**Hotfix beta.10.1:** beta.10 nie startowała po przebudowie warm, ponieważ jedna minuta na każdej karcie występowała w dwóch sesjach i tworzyła nakładające się bloki. Poprawka usunęła tylko dwie zdublowane minuty z projekcji, zachowując około 1007 minut prawidłowego backfillu manualnego i wszystkie rekordy źródłowe. Commity: `49e200d`, `906b7d5`; 14 nowych testów `CanonicalProjectionTests.cs`.

**Reguła odpoczynku i rekompensat:** odpoczynek zmierzony i rozliczona luka `CardRemoved` jako `Przerwa/Odpoczynek` mogą tworzyć jeden ciągły blok z `SourceGapId`. Beta.11 rozlicza rekompensatę wyłącznie en bloc przez jeden kwalifikujący odpoczynek ≥9 h, zachowuje FIFO, ścisły termin i pełny ślad. Staniek ma `1253 min / 20:53`, Doboś `1192 min / 19:52`. Wiele rozliczonych luk jest osobnym, nierozstrzygniętym tematem domenowym.

**Ważne decyzje:** brak OUT we wpisie manualnym; `CardRemoved > ForwardTimeJump`; najwyżej jedna otwarta luka na kartę; najdłuższy nieprzerwany odpoczynek zamiast sumy; klucz idempotentności `ActivitySessionId + StartGameMinute`; reguła pierwszej godziny podwójnej obsady odrzucona; dzielony odpoczynek 3+9 nierozpoznawany; rekompensaty wymagają jednego pełnego bloku en bloc i nie sumują okruchów.

**Numeracja dni:** `displayedDay = floor(GameMinute / 1440) + 1`. Przy porównaniu surowych minut z UI, CSV i PDF zawsze stosuj `+1`.

**Planer podróży MVP:** strategia „Najwcześniejsza legalna”, specyfikacja po przeglądzie P0/P1/P2. Implementacja pozostaje za końcowym smoke testem beta.11. Po GO: końcowa akceptacja specyfikacji, osobna gałąź, kontrakty i czerwone testy P0, potem silnik zdarzeniowy, Application Service i UI.

**Najbliższe zadanie:** osobisty końcowy smoke test beta.11 na paczce wydaniowej, z aktywną telemetrią. Po zaliczeniu — decyzja GO i dalsza praca nad Planerem podróży.
