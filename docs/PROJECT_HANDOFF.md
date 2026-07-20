# PROJECT HANDOFF — ETS2 EU Digital Tachograph

**Wersja projektu:** 0.1.0-beta.10
**Data przygotowania dokumentu:** 20 lipca 2026
**Przeznaczenie:** pakiet startowy dla nowej sesji AI / nowego okna kontekstowego

---

## 1. Streszczenie projektu

ETS2 EU Digital Tachograph to samodzielna aplikacja desktopowa (.NET 9, C#, WPF) symulująca cyfrowy tachograf zgodny z logiką rozporządzenia UE 561/2006, przeznaczona dla graczy Euro Truck Simulator 2 grających w trybie singleplayer. Aplikacja odczytuje oficjalną telemetrię gry przez natywny plugin C++ oparty na SCS SDK, prowadzi historię aktywności dwóch kart kierowców (podwójna obsada) w czasie gry (`game_time`), wylicza liczniki regulacyjne (jazda ciągła, dzienna, tygodniowa, dwutygodniowa, odpoczynki, rekompensaty), obsługuje nietypowe zachowania czasu gry (cofnięcia, skoki, pauzy, operacje ładunkowe) i generuje raporty (PDF, CSV, JSON dla VTC, własny format `.tacho`).

Projekt powstał jako reakcja na niedociągnięcia istniejących na rynku narzędzi tego typu (m.in. aplikacja „Emre54", tacho wbudowane w Ox-Ram VTC) — użytkownik napotkał w nich błędy (niezerujący się licznik dzienny, brak wsparcia dla podwójnej obsady, brak logowania) i zbudował własne rozwiązanie od zera, jednocześnie naprawiając analogiczne klasy błędów, które by odziedziczył.

Użytkownikiem docelowym jest gracz ETS2 ceniący realizm (w tym społeczność VTC), a autor projektu jest jednocześnie jego głównym testerem i deweloperem (samouk, korzysta z Claude Code / Codex jako wsparcia).

**Aktualny etap:** wersja **0.1.0-beta.10**, 225/225 testów automatycznych zielonych, kompilacja Release bez błędów i ostrzeżeń. Projekt jest w fazie kontrolowanej bety — technicznie domknięty, ale świadomie jeszcze nieopublikowany (publiczne wydanie odłożone do czasu zakończenia testów).

---

## 2. Aktualny zakres projektu

### Funkcje podstawowe
- Odczyt telemetrii ETS2 przez natywny plugin C++ (SCS SDK 1.14) — **[GOTOWE]**
- Model domenowy: karta kierowcy, sesje historii, minutowe rekordy aktywności (`ActivityRecord`) — **[GOTOWE]**
- Silnik reguł: jazda ciągła (4h30), przerwa (45 min, w tym podział 15+30), odpoczynek dobowy (9h/11h), tygodniowy (24h/45h), limity 56h/90h — **[GOTOWE]**
- Podwójna obsada: dwa niezależne sloty kart, okno 30h zamiast 24h, specjalna przerwa 45 min dla slotu 2 w ruchu — **[GOTOWE]**
- Obsługa cofnięć i skoków czasu gry (`truncate-and-append`) — **[GOTOWE]**
- Jawne luki aktywności (`ActivityGap`) i wpisy manualne (`ManualEntryService.ResolveGap`) — **[GOTOWE]**
- Ciągłość odpoczynku przez rozliczoną lukę `CardRemoved` (beta.10) — **[GOTOWE]**, wymaga dodatkowych testów terenowych
- Rozpoznawanie operacji załadunku/rozładunku (protokół v3) — **[GOTOWE]**
- Raporty PDF/CSV/JSON VTC/`.tacho` z informacją o kompletności dowodu — **[GOTOWE]**
- Retencja danych (hot/warm) — **[GOTOWE]**; cold — **[DO ZROBIENIA]** (hak przygotowany)
- Statystyki regulacyjne w UI (praca dobowa, wydłużenia, skrócone odpoczynki, rekompensaty) — **[GOTOWE]**
- Lista nierozliczonych luk w UI + ostrzeżenie w raporcie — **[GOTOWE]**

### Funkcje dodatkowe
- Nakładki w grze (overlay) S1/S2 z niezależną pozycją — **[GOTOWE]**
- Reguła pierwszej godziny przy podwójnej obsadzie (maszyna stanów PENDING/QUALIFIED/FAILED) — **[ODRZUCONE]** (świadomie wyrzucone z zakresu)
- Dzielony odpoczynek dobowy 3h+9h — **[ODRZUCONE]** / nierozpoznawany, known issue
- Pełne rekompensaty tygodniowe (pełny ślad spłat, precyzyjny termin) — **[DO ZROBIENIA]**, obecnie uproszczone
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
- Kontrola wizualna działającego UI po zmianach w XAML — **[W TRAKCIE]** / regularnie pomijana (ryzyko, patrz sekcja 9)

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
| Repozytorium git | Lokalne repo ma powstać **przed** publikacją, niezależnie od niej | Bezpieczne cofanie zmian wprowadzanych przez agentów AI | Repo naprawione (`git init` wykonany) |

---

## 4. Aktualna architektura

**Główne komponenty (projekty w rozwiązaniu):**

- `ETS2Tachograph.Core` — model domenowy, czas gry, `ActivityTimeline`, reguła jednej minuty, `GameClockFormatter`
- `ETS2Tachograph.Telemetry.Scs` — odczyt wersjonowanej pamięci współdzielonej (protokół v3)
- `ETS2Tachograph.Engine` — klasyfikacja ramek telemetrii, sesje, luki (`ActivityHistoryProcessor`, `CrewTachographEngine`), snapshoty
- `ETS2Tachograph.RuleEngine` — liczniki regulacyjne, naruszenia, rekompensaty (`RegulationEngine`, `RegulationEvaluation`, `RegulationState`, `WeeklyRestCompensation`, `CompensationSummary`)
- `ETS2Tachograph.Infrastructure` — SQLite, EF Core, repozytoria, migracje, retencja
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
│   ├── ETS2Tachograph.RuleEngine.Tests/      (42 testy)
│   │   └── RegulationEngineTests.cs
│   ├── ETS2Tachograph.Application.Tests/     (38 testów)
│   │   └── ManualEntryWizardDraftTests.cs
│   ├── ETS2Tachograph.Reports.Tests/         (9 testów)
│   └── ETS2Tachograph.Infrastructure.Tests/  (31 testów)
├── output/releases/
│   └── ETS2Tachograph-0.1.0-beta.10-win-x64.zip
├── BETA_TEST_PLAN.md
├── KNOWN_ISSUES.md
├── RELEASE_NOTES.md
├── PROJECT_HANDOFF.md   ← ten dokument
└── README.md            [PROPOZYCJA — do napisania przed publikacją]
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
| Blok odpoczynku ciągły | Najdłuższy nieprzerwany odcinek; `Inna praca` i `Dyspozycyjność` przerywają | Rozliczanie luki / historia | Brak sumowania rozdzielonych bloków | GOTOWE |
| Ciągłość przez rozliczoną lukę (beta.10) | Odpoczynek zmierzony + rozliczona luka jako `Przerwa/Odpoczynek` = jeden ciągły blok (przed, po lub po obu stronach) | Rozliczenie `CardRemoved` jako odpoczynek | Reset dobowy/tygodniowy na końcu połączonego bloku; blok niesie `SourceGapId` | GOTOWE, wymaga dalszych testów terenowych |
| Rekompensata tygodniowa | Dług = 45h − rzeczywisty odpoczynek | Odpoczynek 24–<45h | Zobowiązanie przypisane do tygodnia, termin do końca 3. kolejnego tygodnia, FIFO rozliczania | W TRAKCIE (uproszczone: brak pełnego śladu spłat, termin po numerach tygodni) |
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
- **225/225 testów automatycznych** (Core 33, Telemetry.Scs 8, Engine 64, RuleEngine 42, Application 38, Reports 9, Infrastructure 31)
- Kompilacja Release: 0 błędów, 0 ostrzeżeń
- Dwa długie scenariusze terenowe potwierdzone w rzeczywistej grze (2h+7h=9h odpoczynku; wariant tygodniowy 45h)
- Dziesięć wydań beta (beta.4 → beta.10) z artefaktami i sumami SHA-256
- Usunięcie martwego, nigdy niewidocznego bloku XAML (alternatywna wersja Dashboardu) wraz z powiązanymi zasobami

---

## 8. Aktualny stan prac

**Ostatnia praca nad kodem:** usunięcie martwego bloku XAML (nieużywana, `Collapsed` wersja Dashboardu, ok. linie 132–191 w `MainWindow.xaml`) wraz z powiązanymi zasobami graficznymi. Usunięcie wykonane bezpośrednio z dysku, ponieważ repozytorium git było wtedy uszkodzone.

**Repozytorium git:** naprawione — `git init` wykonany.

**Co działa:** cała logika opisana w sekcjach 6–7, potwierdzona testami automatycznymi i dwoma scenariuszami terenowymi.

**Czego nie zweryfikowano:**
- Wizualna kontrola interfejsu po usunięciu martwego bloku XAML — kompilacja przechodzi, ale runtime nie został sprawdzony (WPF nie zgłasza brakujących zasobów na etapie kompilacji)
- Rozszerzone scenariusze terenowe dla reguły ciągłości odpoczynku przez lukę (beta.10)

**Od którego miejsca kontynuować:** wizualna weryfikacja Dashboardu (patrz sekcja 12).

---

## 9. Otwarte problemy i ryzyka

| Problem / ryzyko | Wpływ | Prawdopodobna przyczyna | Proponowane rozwiązanie | Priorytet |
|---|---|---|---|---|
| Brak wizualnej kontroli UI po usunięciu martwego kodu | Ryzyko niezauważonej regresji wizualnej mimo zielonej kompilacji | WPF nie wykrywa brakujących zasobów runtime na etapie kompilacji | Ręczne przeklikanie Dashboardu (nawigacja, sloty kart, tryby, drukowanie, nakładki) | Wysoki |
| Reguła ciągłości odpoczynku przez lukę (beta.10) przetestowana w dwóch scenariuszach | Zmiana reguły rdzeniowej może mieć niewykryte przypadki brzegowe | Ostatnia zmiana w dniu z sześcioma wydaniami | Testy terenowe: wielokrotne luki, luka na granicy tygodnia, interakcja z rekompensatą | Średni |
| Uproszczony model rekompensat tygodniowych | Brak pełnego śladu spłat; termin liczony po numerach tygodni | Świadomie odłożone jako osobny etap | Osobny projekt z pełną specyfikacją i testami | Niski (świadome) |
| Powtarzające się pomijanie kontroli wizualnej po zmianach w XAML | Systematyczne ryzyko regresji UI | Brak stałego punktu w procesie | Dopisać kontrolę wizualną jako stały punkt w `BETA_TEST_PLAN.md` | Średni |
| Skróty klawiszowe nakładek mogą kolidować z innymi aplikacjami | Możliwe zgłoszenia testerów | Standardowe ograniczenie skrótów globalnych | Udokumentować, rozważyć rekonfigurację | Niski |
| Dzielony odpoczynek 3h+9h nierozpoznawany | Aplikacja odrzuca legalny wariant jako dwa osobne bloki | Świadomie nieobsłużone | Zaprojektować jako osobną, jawną regułę | Niski |
| Brak instalatora / podpisu kodu / auto-update | Utrudniona dystrybucja i aktualizacja dla testerów | Poza zakresem obecnej fazy | Rozważyć po zamknięciu bety | Niski |
| Ryzyko rozrostu zakresu | Historia projektu pokazuje tendencję do dokładania funkcji (np. wpis manualny urósł z „luki" do pełnej warstwy z resetem dobowym) | Naturalne przy pracy jednoosobowej bez formalnego backlogu | Trzymać się etapowego podziału z jawnym kryterium „gotowe" | Średni |

---

## 10. Nierozstrzygnięte decyzje

**1. Publikacja repozytorium i forma wydania**
- Opcje: (a) publiczne repo GitHub z pełną historią i dokumentacją po zamknięciu testów; (b) prywatne repo z ograniczonym dostępem; (c) brak publikacji
- Zalety/wady: (a) buduje portfolio i daje feedback społeczności, ale wymaga README, licencji i rozstrzygnięcia kwestii warunków SCS SDK; (b)/(c) ograniczają ekspozycję i ryzyko
- Rekomendacja: (a) po zamknięciu testów bety
- Wpływ: określa harmonogram prac dokumentacyjnych

**2. Model komercjalizacji**
- Opcje: (a) całkowicie darmowe; (b) darmowe z opcjonalnym wsparciem (Patreon/Ko-fi); (c) płatne
- Zalety/wady: (c) generuje zobowiązania supportowe i ciągłości przy niszowym rynku; (a)/(b) minimalizują zobowiązania
- Rekomendacja: (a) lub (b)
- Wpływ: określa poziom zobowiązań wobec użytkowników i wymagania wobec dystrybucji

**3. Zakres testów terenowych przed szerszą dystrybucją**
- Opcje: (a) uznać obecne dwa scenariusze za wystarczające; (b) rozszerzyć o kombinacje (wielokrotne luki, granica tygodnia, interakcja z rekompensatą)
- Rekomendacja: (b) — reguła z beta.10 jest świeża i rdzeniowa
- Wpływ: określa, czy można przejść do etapu publikacji

---

## 11. Lista zadań

### Priorytet 1 — najbliższy krok

**1.1 Wizualna weryfikacja Dashboardu po usunięciu martwego XAML**
- Opis: uruchomić aplikację, przeklikać nawigację (góra/dół/OK/C), sloty kart (wkładanie/wyjmowanie), tryby (OUT/prom/załoga), drukowanie, obie nakładki S1/S2
- Oczekiwany rezultat: potwierdzenie braku regresji wizualnej i funkcjonalnej
- Zależności: brak
- Pliki: `MainWindow.xaml`, `MainViewModel.cs`, `OverlayViewModel.cs`
- Kryterium ukończenia: wszystkie elementy UI renderują się i reagują poprawnie; brak brakujących zasobów w runtime

**1.2 Pierwszy commit w naprawionym repozytorium**
- Opis: zweryfikować `.gitignore` (`bin/`, `obj/`, `*.db`, `*.db.bak.*`, `output/releases/*.zip`, `logs/`), wykonać commit bazowy
- Oczekiwany rezultat: stan projektu pod kontrolą wersji
- Zależności: 1.1 (żeby nie commitować stanu z potencjalną regresją)
- Pliki: `.gitignore`
- Kryterium ukończenia: `git status` czysty, `git log` pokazuje commit bazowy, brak dużych artefaktów w historii

### Priorytet 2 — po ukończeniu podstaw

**2.1 Rozszerzone testy terenowe reguły z beta.10**
- Scenariusze: wielokrotne luki w jednym okresie odpoczynku; luka przecinająca granicę tygodnia regulacyjnego; interakcja rozliczonej luki z rekompensatą tygodniową
- Kryterium: brak rozbieżności między licznikami w UI a raportem PDF po restarcie aplikacji

**2.2 Kontrola wizualna jako stały punkt w `BETA_TEST_PLAN.md`**
- Kryterium: dokument zawiera checklistę UI wykonywaną po każdej zmianie w XAML

**2.3 Kolory ostrzegawcze licznika „DO PRZERWY" (Faza 1 UX)**
- Kryterium: bursztyn od 4:15, czerwień od 4:30; spójne z prezentacją przekroczeń w innych licznikach

**2.4 Przygotowanie dokumentacji przedpublikacyjnej**
- Zakres: `README.md` (opis problemu, architektura, diagram `truncate-and-append`, historia przypadku `05:27`, screeny), aktualizacja `KNOWN_ISSUES.md`
- Kryterium: osoba postronna rozumie, czym jest projekt i jakie ma ograniczenia

### Priorytet 3 — rozwój późniejszy

- Warstwa zimnej retencji (365 dni) — hak przygotowany
- Pełny model rekompensat tygodniowych (ślad spłat, precyzyjny termin, przypadki graniczne wzorca dwutygodniowego)
- Instalator, podpis kodu, automatyczna aktualizacja aplikacji i pluginu
- Rozpoznawanie dzielonego odpoczynku dobowego 3h+9h jako osobnej, jawnej reguły
- Ewentualne przywrócenie reguły pierwszej godziny (obecnie odrzucona)

---

## 12. Rekomendowany następny krok

**Wykonać wizualną weryfikację Dashboardu po usunięciu martwego bloku XAML.**

**Co dokładnie zrobić:** uruchomić aplikację WPF i przejść pełną ścieżkę interfejsu — nawigacja przyciskami (góra/dół/OK/C), wkładanie i wyjmowanie kart w obu slotach, przełączanie trybów (OUT, prom, załoga), ekran drukowania, obie nakładki (S1/S2), ekran Historia z sekcją luk, ekran Raporty z generowaniem PDF.

**Dlaczego teraz:** to jedyna zmiana w projekcie wykonana bez kontroli wersji i bez weryfikacji runtime. Kompilacja przechodzi, ale WPF nie wykrywa brakujących zasobów na etapie budowania — ewentualna regresja ujawni się dopiero przy uruchomieniu. Wykonanie tego przed pierwszym commitem gwarantuje, że stan bazowy repozytorium jest sprawdzony, a nie tylko kompilowalny.

**Pliki, których dotyczy:** `MainWindow.xaml`, `MainViewModel.cs`, `OverlayViewModel.cs` oraz katalog zasobów (`Assets`).

**Jak sprawdzić poprawność:** wszystkie elementy interfejsu renderują się bez pustych miejsc i wyjątków; przyciski wywołują właściwe komendy; nakładki otwierają się skrótami i pamiętają pozycje; wygenerowany PDF ma poprawny układ (nagłówek, bilans, tabele bez nachodzenia).

---

## 13. Instrukcja dla kolejnej sesji AI

> Jesteś asystentem technicznym wspierającym rozwój projektu **ETS2 EU Digital Tachograph** — symulatora cyfrowego tachografu dla Euro Truck Simulator 2 (C#/.NET 9/WPF/SQLite/EF Core + natywny plugin C++ czytający oficjalne SCS Telemetry SDK). Projekt jest w fazie kontrolowanej bety (0.1.0-beta.10, 225 testów zielonych, kompilacja Release bez ostrzeżeń).
>
> **Zasady współpracy:**
> - Użytkownik preferuje, by **wskazywać problem i kierunek rozwiązania, a nie od razu pisać lub wdrażać kod**. Chce samodzielnie rozwiązywać zadania implementacyjne, chyba że wyraźnie poprosi o gotowy kod.
> - **Nie zmieniaj ustalonych decyzji projektowych** (sekcja 3 tego dokumentu) bez wyraźnego uzasadnienia i zgody użytkownika — w szczególności: model `ActivityGap` z enumem przyczyn, klucz idempotentności `ActivitySessionId + StartGameMinute`, brak OUT w piktogramach wpisu manualnego, ciągłość odpoczynku przez rozliczoną lukę (reguła z beta.10).
> - **Przed proponowaniem zmian zawsze sprawdź istniejący kod.** Projekt ma bogatą historię naprawionych błędów tej samej klasy; pozorne „uproszczenia" często cofają świadome poprawki (np. rozdzielenie prędkości do klasyfikacji karty i prędkości pojazdu, pusta sesja jako znacznik gałęzi).
> - Wszystkie obliczenia czasu opierają się na `game_time` z telemetrii, **nigdy** na zegarze systemowym.
> - Historia minutowa jest jedynym źródłem prawdy; liczniki i projekcje są zawsze wyliczane, nigdy przechowywane osobno.
> - Każdy naprawiony błąd powinien otrzymać test regresyjny odtwarzający dokładny scenariusz.
> - Po większych zmianach architektonicznych zaktualizuj dokumentację (`KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, ten dokument).
>
> **Ograniczenia:** aplikacja i plugin działają wyłącznie na Windows x64; telemetria SCS jest tylko do odczytu, więc aplikacja nie może fizycznie zablokować jazdy w grze — blokuje jedynie własny interfejs (co odpowiada zachowaniu prawdziwego DTCO).
>
> **Aktualne zadanie:** wizualna weryfikacja Dashboardu po usunięciu martwego bloku XAML, następnie pierwszy commit w naprawionym repozytorium git.

---

## 14. Skrócony kontekst startowy

*(wersja do wklejenia jako pierwsza wiadomość w nowym oknie kontekstowym)*

**Cel projektu:** ETS2 EU Digital Tachograph to samodzielna aplikacja desktopowa (.NET 9, C#, WPF, SQLite/EF Core) symulująca cyfrowy tachograf zgodny z logiką rozporządzenia UE 561/2006 dla graczy Euro Truck Simulator 2 (singleplayer). Odczytuje oficjalną telemetrię gry przez natywny plugin C++ (SCS Telemetry SDK, protokół v3 w pamięci współdzielonej), prowadzi historię dwóch kart kierowców (podwójna obsada) w czasie gry, liczy wszystkie regulacyjne limity i generuje raporty (PDF, CSV, JSON dla VTC, własny format `.tacho`).

**Architektura:** dziewięć projektów w rozwiązaniu — `Core` (model domenowy, czas gry, `GameClockFormatter`), `Telemetry.Scs` (odczyt pamięci współdzielonej), `Engine` (klasyfikacja ramek, sesje, luki — `ActivityHistoryProcessor`, `CrewTachographEngine`), `RuleEngine` (liczniki, naruszenia, rekompensaty — `RegulationEngine`, `RegulationState`, `CompensationSummary`), `Infrastructure` (SQLite/EF Core/migracje/retencja), `Application` (przypadki użycia, wpisy manualne — `ManualEntryService`, `ActivityGapService`), `Reports` (PDF/eksporty), `Desktop` (WPF — `MainWindow.xaml`, `MainViewModel.cs`), `ScsPlugin` (natywny C++).

**Zasada nadrzędna:** historia minutowa (`ActivityRecord`) to jedyne źródło prawdy — liczniki, projekcje (bloki „ciepłe" retencji), raporty i statystyki UI są zawsze wyliczane z niej, nigdy przechowywane osobno. Ta sama zasada dotyczy luk (`ActivityGap`).

**Najważniejsze wymagania i reguły:**
- Wszystko liczone w `game_time`, nigdy na zegarze systemowym (dotyczy też głównego LCD).
- Podwójna obsada z oknem 30h zamiast 24h; drugi kierowca może odebrać 45-minutową przerwę podczas jazdy pierwszego, ale nigdy odpoczynku dobowego.
- Cofnięcia i skoki czasu obsługiwane przez `truncate-and-append`: źródłowa gałąź nigdy nie jest niszczona, przycinana jest wyłącznie projekcja kanoniczna.
- Jawne luki aktywności (`ActivityGap`) z enumem przyczyn (`CardRemoved`, `ForwardTimeJump`, rezerwa `TelemetryUnavailable`); priorytet `CardRemoved > ForwardTimeJump` liczony per karta; najwyżej jedna otwarta luka na kartę w projekcji kanonicznej.
- Wpisy manualne rozliczają luki przez `ResolveGap` z trzema piktogramami: `Przerwa/Odpoczynek`, `Inna praca`, `Dyspozycyjność` (bez OUT). Wymagane pełne pokrycie luki, bez dziur i nakładania.
- Odpoczynek dobowy liczony jako **najdłuższy nieprzerwany blok**, nigdy jako suma — `Inna praca` i `Dyspozycyjność` przerywają ciągłość.
- **Reguła z beta.10:** ciągłość odpoczynku może przechodzić przez rozliczoną lukę `CardRemoved` (przed, po lub po obu stronach). Odwraca wcześniejszą, bardziej restrykcyjną zasadę „wyjęcie karty kończy wszystkie czynności".
- Klasyfikacja odpoczynku wynika z faktycznej długości bloku, nie z celu wybranego w UI.
- Rekonstrukcja skoków czasu: ≤2 min → ostatnia aktywność; duży skok po Jeździe → zawsze luka; duży skok przy odpoczynku → rekonstrukcja tylko gdy pojazd stał przed i po; potwierdzony załadunek/rozładunek (protokół v3) → wybrana aktywność bez luki.

**Wykonane elementy:** kompletny model domenowy i silnik reguł; pełna obsługa luk (Etapy 0–4: encja, `CardRemoved`, `ResolveGap`, reset dobowy, kreator z blokadą UI); cztery statystyki regulacyjne w UI (praca dobowa 13h, wydłużenia jazdy 2×, skrócone odpoczynki 3×, rekompensaty) z przekroczeniami pokazywanymi bez maskowania; retencja hot/warm; LCD na czasie gry; lista nierozliczonych luk w Historii; ostrzeżenie o kompletności dowodu w raportach (`evidenceComplete` w JSON, `LUKI: brak` / `LUKI NIEROZLICZONE: X` w PDF). **225/225 testów zielonych**, kompilacja Release bez ostrzeżeń.

**Naprawione błędy (z testami regresyjnymi):** przypisanie sesji przy cofnięciu czasu (`UNIQUE constraint`), idempotentność zapisu po znaczącym kluczu, fałszywa rekonstrukcja wielogodzinnej Jazdy przy skoku czasu, zła baza rekompensaty (11h→9h), utrata aktywności w gałęzi `GamePaused`, luka przycięta przez późniejszą gałąź czasu. Kluczowa regresja odniesienia: `03:53 + 01:34 = 05:27`.

**Aktualne problemy:**
1. Brak wizualnej kontroli Dashboardu po usunięciu martwego, nieużywanego bloku XAML — kompilacja przechodzi, runtime niezweryfikowany.
2. Reguła ciągłości odpoczynku przez lukę (beta.10) przetestowana tylko w dwóch scenariuszach terenowych — warto rozszerzyć o wielokrotne luki i granicę tygodnia.
3. Model rekompensat tygodniowych jest uproszczony (brak pełnego śladu spłat, termin po numerach tygodni) — świadomie odłożony jako osobny etap.

**Podjęte decyzje warte zapamiętania:** OUT świadomie wyjęty z piktogramów wpisu manualnego; reguła pierwszej godziny przy podwójnej obsadzie odrzucona z zakresu projektu; pełne rekompensaty tygodniowe i zimna retencja (365 dni) odłożone jako przyszłe etapy; dzielony odpoczynek dobowy 3h+9h nierozpoznawany (known issue); klucz idempotentności to `ActivitySessionId + StartGameMinute`, nie losowy `Id`.

**Ograniczenia:** Windows x64 wyłącznie; telemetria SCS tylko do odczytu, więc aplikacja nie blokuje fizycznie jazdy w grze, jedynie własny interfejs (zgodnie z zachowaniem prawdziwego DTCO); aplikacja nie jest certyfikowanym tachografem ani implementacją Annex 1C.

**Najbliższe zadanie:** wizualna weryfikacja Dashboardu (nawigacja, sloty kart, tryby, drukowanie, nakładki, ekrany Historia i Raporty) po usunięciu martwego bloku XAML, a następnie pierwszy commit w naprawionym repozytorium git po sprawdzeniu `.gitignore`.
