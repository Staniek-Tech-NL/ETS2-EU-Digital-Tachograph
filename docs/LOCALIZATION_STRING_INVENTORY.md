# M5.1 — Inwentaryzacja tekstów lokalizacji PL/EN

**Projekt:** ETS2 EU Digital Tachograph

**Data rozpoczęcia:** 2026-07-27

**Status:** **W TOKU — PACZKI 1–10 GO**

**Języki:** `pl-PL`, `en-GB`

**Punkt wejściowy:** `2d8a760` — GO M4 i aktywny UI freeze

## Cel i reguła kompletności

Inwentaryzacja obejmuje każdy tekst widoczny dla użytkownika w Desktop, Planerze,
nakładkach, dialogach i PDF oraz wszystkie wartości domenowe wymagające
lokalnego presentera. Nie obejmuje artefaktów `bin/` i `obj/`.

Każdy kandydat otrzymuje jedną kategorię:

| Kod | Kategoria | Obsługa |
|---|---|---|
| U | tekst użytkowy | klucz w `UiStrings` albo `ReportStrings` |
| T | kontrakt techniczny | bez lokalizacji, `InvariantCulture` |
| D | diagnostyka i log | bez lokalizacji |
| P | wartość domenowa | lokalny presenter, bez zmiany enuma lub DTO |
| O | nazwa własna / dane użytkownika | wyświetlić bez tłumaczenia |

## Wynik skanu wejściowego

| Źródło | Zakres skanu | Wynik | Decyzja |
|---|---|---:|---|
| `MainWindow.xaml` | `Text`, `Content`, `Header`, `ToolTip`, `Title`, `StringFormat` poza bindingami | 251 wystąpień / 197 unikalnych | U, z wyjątkami T/O |
| `OverlayWindow.xaml` | te same atrybuty | 14 / 14 | U, `S1` i `S2` pozostają T |
| `App.xaml` | literały użytkowe | 0 | bez pracy translatorskiej |
| Desktop C# | 14 plików źródłowych, bez `bin/` i `obj/` | 828 literałów / 579 unikalnych kandydatów | U, D, P, T i O — wymagają rozdzielenia |
| Reports C# | `PdfReportExporter`, `ReportPresentationBuilder` | 114 / 98 | U i P; dane raportu pozostają T/O |
| Application Services | 16 plików usług | 94 / 89 | głównie T/D; komunikaty docierające do UI mapować w Desktop |
| Kraje | stabilne ISO + `CountryNames.pl.json` | 249 nazw PL | ISO i kod tachografowy T; nazwy U |

Liczby są kontrolą kompletności źródeł, a nie liczbą przyszłych kluczy.
Powtarzające się etykiety wspólne mają korzystać z jednego klucza semantycznego.

Każdy klucz powinien mieć potwierdzone miejsce użycia. Nazwany wyjątek stanowi
**klucz wyczerpującego pokrycia enuma**: może nie mieć aktywnego konsumenta,
jeżeli odpowiada istniejącej wartości domenowej i jest wymagany, aby jawny
presenter nie miał fallbacku. Wyjątek musi wskazywać enum, wartość i gałąź
presentera; nie obejmuje kluczy tworzonych wyłącznie dla hipotetycznej funkcji.

## Rejestr obszarów użytkowych

| ID | Obszar | Źródła | Kategoria | Docelowa obsługa | Stan |
|---|---|---|---|---|---|
| UI-01 | Powłoka, tytuł, nawigacja i wspólne akcje | `MainWindow.xaml`, `App.xaml.cs`, `MainViewModel.cs` | U/T/D/O | `UiStrings.Common_*`, `UiStrings.Navigation_*`, `UiStrings.Shell_*` | GO — katalog wiążący |
| UI-02 | Dashboard i wirtualny tachograf | `MainWindow.xaml`, `MainViewModel.cs` | U/P | zasoby + presentery aktywności, trybów i stanu kart | GO — Dashboard w paczce 2, urządzenie w paczce 3, terminy domknięte przez paczkę 5 |
| UI-03 | Historia, luki i wpis manualny | `MainWindow.xaml`, `MainViewModel.cs`, `ManualEntryPlanEditor.cs` | U/P | zasoby + presentery aktywności, źródeł, warunków, przyczyn, stanów luk i walidacji | GO w paczce 4 |
| UI-04 | Kraje i kody tachografowe | `CountryCatalog.cs`, JSON | U/T | osobne nazwy PL/EN; zapis nadal przez ISO | GO w paczce 6 |
| UI-05 | Rekompensaty | `MainWindow.xaml`, `CompensationPresentation.cs` | U/P/T | zasoby + presenter statusu; identyfikatory bez zmian | GO w paczce 7 |
| UI-06 | Raporty w Desktop | `MainWindow.xaml`, `ReportsWorkspaceViewModel.cs` | U/P/T | zasoby + presentery; formaty eksportu bez zmian | GO w paczce 8 |
| UI-07 | Kierowcy i Ustawienia | `MainWindow.xaml`, `MainViewModel.cs`, `SettingsService.cs` | U/O/T | zasoby; nazwy i numery kart bez tłumaczenia | GO w paczce 9 |
| UI-08 | Planer | `MainWindow.xaml`, `JourneyPlannerViewModel.cs` | U/P/T | zasoby + presentery faz, powodów, statusów i ostrzeżeń | GO w paczce 10 |
| UI-09 | Dialogi, potwierdzenia i komunikaty błędów | `App.xaml.cs`, `MainViewModel.cs`, view-modele | U/D/T | tekst UI w zasobach; logi i kody bez zmian | do rozpisania |
| UI-10 | Nakładki S1/S2 | `OverlayWindow.xaml`, `OverlayViewModel.cs` | U/P/T | zasoby; `S1`, `S2`, `HH:MM` bez zmian | do rozpisania |
| X-01 | Wspólne formatery czasu i terminów | `GameCalendarFormatter.cs`, `GameClockFormatter.cs`, `WeeklyRestWindowFormatter.cs` i konsumenci bindingów | U/P/T | wspólne nazwy dni i prefiksy terminów; bez duplikowania per ekran | GO w paczce 5 |
| PDF-01 | Raport PDF | `PdfReportExporter.cs`, `ReportPresentationBuilder.cs` | U/P/T/O | `ReportStrings`; dane i identyfikatory bez zmian | do rozpisania |
| DOC-01 | Instrukcja instalacji PL/EN | dokumentacja użytkowa | U | dwa jawne dokumenty językowe | późniejszy etap M5.4 |
| DOC-02 | Instrukcja podstawowa PL/EN | dokumentacja użytkowa | U | dwa jawne dokumenty językowe | późniejszy etap M5.4 |

## Katalog kluczy v1 — elementy wspólne, nawigacja i powłoka

Klucze są semantyczne i nie zawierają języka. Zasób przechowuje kompletną,
gotową do wyświetlenia etykietę wraz z wymaganą pisownią, bez sklejania jej
z innym zasobem. Rola prezentacyjna, taka jak `Action` albo `Header`, może być
częścią klucza, gdy rozróżnia rzeczywiście odmienne etykiety.

### Elementy wspólne

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Common_AddAction` | DODAJ | ADD | U |
| `Common_EditAction` | EDYTUJ | EDIT | U |
| `Common_CopyAction` | KOPIUJ | COPY | U |
| `Common_RemoveAction` | USUŃ | REMOVE | U |
| `Common_CancelAction` | ANULUJ | CANCEL | U |
| `Common_ActionHeader` | AKCJA | ACTION | U |
| `Common_ActionsHeader` | AKCJE | ACTIONS | U |
| `Common_StatusHeader` | STATUS | STATUS | U |
| `Common_SourceHeader` | ŹRÓDŁO | SOURCE | U |
| `Common_ReasonHeader` | POWÓD | REASON | U |
| `Common_From` | OD | FROM | U |
| `Common_To` | DO | TO | U |
| `Common_NoData` | Brak danych | No data | U |

### Nawigacja

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Navigation_Dashboard` | Dashboard | Dashboard | U |
| `Navigation_History` | Historia | History | U |
| `Navigation_Planner` | PLANER | JOURNEY PLANNER | U |
| `Navigation_Compensations` | Rekompensaty | Compensations | U |
| `Navigation_Reports` | Raporty | Reports | U |
| `Navigation_Drivers` | Kierowcy | Drivers | U |
| `Navigation_Settings` | Ustawienia | Settings | U |

### Powłoka i start aplikacji

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Shell_ProductName` | ETS2 Digital Tachograph | ETS2 Digital Tachograph | O |
| `Shell_SystemTitle` | SYSTEM ZARZĄDZANIA TACHOGRAFEM | TACHOGRAPH MANAGEMENT SYSTEM | U |
| `Shell_DriverAdministratorRole` | KIEROWCA / ADMINISTRATOR | DRIVER / ADMINISTRATOR | U |
| `Shell_WaitingForEts2` | Oczekiwanie na ETS2... | Waiting for ETS2... | U |
| `Shell_Ets2Paused` | ETS2 · pauza | ETS2 · paused | U |
| `Shell_TelemetryActive` | ETS2 · telemetria aktywna | ETS2 · telemetry active | U |
| `Shell_TelemetryError` | Błąd telemetrii. Szczegóły zapisano w logu diagnostycznym. | Telemetry error. Details were written to the diagnostic log. | U |
| `Shell_VersionFormat` | v{0} | v{0} | U/T |
| `Shell_VersionUnknown` | wersja nieznana | version unavailable | U |
| `Dialog_AlreadyRunning_Title` | Aplikacja już działa | Application already running | U |
| `Dialog_AlreadyRunning_Message` | ETS2 Digital Tachograph jest już uruchomiony. | ETS2 Digital Tachograph is already running. | U |
| `Dialog_StartupFailure_Title` | Nie można uruchomić tachografu | Tachograph could not be started | U |
| `Dialog_StartupFailure_Message` | Nie udało się uruchomić aplikacji. Szczegóły zapisano w logu diagnostycznym: {0} | The application could not be started. Details were written to the diagnostic log: {0} | U/T |

## Paczka 1 — kontrola kompletności

**Zakres:** elementy wspólne, powłoka i siedem głównych pozycji nawigacji.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 33 klucze — 13 `Common_*`, 7 `Navigation_*`, 13 powłoki i startu.

### Mapowanie istniejących literałów

| Źródło | Obecna wartość / rodzina | Klucz albo decyzja | Kategoria |
|---|---|---|---|
| `MainWindow.xaml:797` | `DODAJ` | `Common_AddAction` | U |
| `MainWindow.xaml:949` | `EDYTUJ` | `Common_EditAction` | U |
| `MainWindow.xaml:320,324,338` | `KOPIUJ` | `Common_CopyAction` | U |
| `MainWindow.xaml:952` | `USUŃ` | `Common_RemoveAction` | U |
| `MainWindow.xaml:845,1042` | `ANULUJ` | `Common_CancelAction` | U |
| `MainWindow.xaml:229` | `AKCJA` | `Common_ActionHeader` | U |
| `MainWindow.xaml:945` | `AKCJE` | `Common_ActionsHeader` | U |
| `MainWindow.xaml:114,151,763` | `STATUS` | `Common_StatusHeader` | U |
| `MainWindow.xaml:195,763` | `ŹRÓDŁO` | `Common_SourceHeader` | U |
| `MainWindow.xaml:544` | `POWÓD` | `Common_ReasonHeader` | U |
| `MainWindow.xaml:215,538,632,720,941` | `OD` | `Common_From` | U |
| `MainWindow.xaml:216,539,636,721,942` | `DO` | `Common_To` | U |
| `MainViewModel.cs:75` | `Brak danych` | `Common_NoData` | U |
| `MainWindow.xaml:4` | `ETS2 Digital Tachograph` | `Shell_ProductName` | O |
| `MainWindow.xaml:60` | `SYSTEM ZARZĄDZANIA TACHOGRAFEM` | `Shell_SystemTitle` | U |
| `MainWindow.xaml:61` | `ETS2 TACHO` | zachować jako niezmienny skrót produktu | O |
| `MainWindow.xaml:61` | `v{0}` | `Shell_VersionFormat` | U/T |
| `MainWindow.xaml:61` | `KIEROWCA / ADMINISTRATOR` | `Shell_DriverAdministratorRole` | U |
| `MainWindow.xaml:67` | `▣  Dashboard` | stały glif T + `Navigation_Dashboard` | T + U |
| `MainWindow.xaml:174` | `▤  Historia` | stały glif T + `Navigation_History` | T + U |
| `MainWindow.xaml:246` | `▤  Rekompensaty` | stały glif T + `Navigation_Compensations` | T + U |
| `MainWindow.xaml:352` | `⌖  PLANER` | stały glif T + `Navigation_Planner` | T + U |
| `MainWindow.xaml:571` | `▥  Raporty` | stały glif T + `Navigation_Reports` | T + U |
| `MainWindow.xaml:797` | `♙  Kierowcy` | stały glif T + `Navigation_Drivers` | T + U |
| `MainWindow.xaml:799` | `⚙  Ustawienia` | stały glif T + `Navigation_Settings` | T + U |
| `MainViewModel.cs:54` | `wersja nieznana` | `Shell_VersionUnknown` | U |
| `MainViewModel.cs:73,738` | `Oczekiwanie na ETS2...` | `Shell_WaitingForEts2` | U |
| `MainViewModel.cs:738` | `ETS2 · pauza` | `Shell_Ets2Paused` | U |
| `MainViewModel.cs:738` | `ETS2 · telemetria aktywna` | `Shell_TelemetryActive` | U |
| `MainViewModel.cs:701` | `Błąd telemetrii: {exception.Message}` | `Shell_TelemetryError`; wyjątek wyłącznie do logu | U + D |
| `App.xaml.cs:42` | komunikat o uruchomionej instancji | `Dialog_AlreadyRunning_Message` | U |
| `App.xaml.cs:43` | tytuł uruchomionej instancji | `Dialog_AlreadyRunning_Title` | U |
| `App.xaml.cs:157` | tytuł błędu startu | `Dialog_StartupFailure_Title` | U |
| `App.xaml.cs:157` | `exception.Message` | `Dialog_StartupFailure_Message` z `_diagnostics.CurrentLogPath`; wyjątek wyłącznie do logu | U/T + D |

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| `ETS2 Digital Tachograph`, `ETS2 TACHO`, `ETS2` | O | nazwa produktu i gry |
| glify `▣`, `▤`, `⌖`, `▥`, `♙`, `⚙` | T | element graficzny, oddzielony od etykiety językowej |
| `v{0}` | U/T | identyczny format wersji; placeholder kontrolowany jak zasób |
| nazwa mutexu, zmienna środowiskowa, ścieżki | T | kontrakt uruchomieniowy |
| `APP_START`, `APP_STOP`, `APP_START_FAILED` i pozostałe kody logu | D/T | stabilne zdarzenia diagnostyczne |
| komunikaty o starcie, backupie, rekonstrukcji i zamknięciu w logu | D | nie są tekstem interfejsu |
| `Kierowca ETS2`, `ETS2-DEFAULT`, `PL` profilu startowego | O/T | dane zapisywane w bazie; nie mogą zmieniać się z kulturą UI |

### Rozstrzygnięcia kolizji semantycznych

| Literały | Decyzja |
|---|---|
| `AKCJA` / `AKCJE` | dwa klucze: `Common_ActionHeader` i `Common_ActionsHeader` |
| `STATUS` / `STAN` | `Common_StatusHeader` wyłącznie dla `STATUS`; `STAN` otrzyma domenowy klucz właściciela, np. `Gap_StateHeader` |
| `POWÓD` / `PRZYCZYNA` | `Common_ReasonHeader` wyłącznie dla `POWÓD`; `PRZYCZYNA` otrzyma domenowy klucz właściciela, np. `Gap_CauseHeader` |

### Decyzje obowiązujące przed M5.2

1. **Wersaliki:** nie powstaje globalny konwerter wielkości liter. Zasób zawiera
   pełną etykietę o docelowej pisowni. Różne role lub odmiany dostają osobne,
   semantyczne klucze. Koszt jest świadomy: pisownia staje się częścią
   tłumaczenia, więc przyszła zmiana stylu wymaga edycji obu zestawów zasobów.
2. **Glify nawigacji:** glif pozostaje stałą techniczną w XAML, a etykieta jest
   pobierana z zasobu przez `StringFormat`, np.
   `StringFormat='▣  {0}'`. Jest to dozwolona zmiana lokalizacyjna po UI freeze:
   nie zmienia przepływu, hierarchii nawigacji ani geometrii kontrolki.
3. **Wyjątki w paczce 1:** `exception.Message` nie jest wyświetlany
   użytkownikowi. Paczka 1 używa wyłącznie lokalizowanego komunikatu ogólnego.
   Dialog błędu startu podaje `_diagnostics.CurrentLogPath` przez placeholder
   `{0}`. Pasek telemetrii pozostaje krótki i nie pokazuje ścieżki. Pełny wyjątek
   trafia do logu diagnostycznego.
4. **Brak mieszanego języka:** polskie komunikaty z `CountryCatalog` i innych
   warstw nie mogą przeciekać do EN przez dialog startowy ani pasek telemetrii.
5. **Znane przypadki szczegółowe:** ich lista i klucze nie należą do paczki 1.
   Powstaną wyłącznie po jawnej inwentaryzacji w paczce UI-09 — dialogi,
   potwierdzenia i komunikaty błędów. M5.3 nie może tworzyć ich doraźnie.
6. **Ponowne użycie:** każda kolejna paczka sprawdza cały zatwierdzony katalog
   wszystkich wcześniejszych paczek przed utworzeniem klucza. Reguła nie jest
   ograniczona do `Common_*`; klucz domenowy lub ekranowy również ma być użyty
   ponownie, jeśli zachowuje tę samą semantykę i rolę prezentacyjną.

### Notatki wykonawcze M5.2

1. `_diagnostics` w `App.xaml.cs` jest nullowalne. Budowanie komunikatu błędu
   startu nie może dereferencjonować go bez kontroli. Implementacja użyje
   `_diagnostics?.CurrentLogPath` oraz bezpiecznego fallbacku do oczekiwanego
   katalogu `%LocalAppData%\ETS2Tachograph\Logs`, aby obsługa awarii sama nie
   wywołała `NullReferenceException`.
2. `DiagnosticLogService.CurrentLogPath` zawiera
   `DateTime.Now:yyyy-MM-dd`. Dla `pl-PL` i `en-GB` wynik jest równoważny,
   więc nie blokuje MVP. Ścieżka jest jednak wartością maszynową i przy
   przyszłych kulturach powinna używać `InvariantCulture`. To istniejący kod
   poza zakresem M5; notatka nie upoważnia do zmiany go w tym etapie.

### Oczekiwane identyczne wartości PL/EN

Test duplikatów musi mieć jawną listę wyjątków dla paczki 1:

- `Shell_ProductName`;
- `Shell_VersionFormat`;
- `Navigation_Dashboard`;
- `Common_StatusHeader`.

### Kontrola paczki

- [x] wszystkie główne nagłówki powłoki mają decyzję;
- [x] wszystkie siedem pozycji nawigacji ma klucz;
- [x] glify oddzielono od tekstu lokalizowanego;
- [x] wszystkie stany połączenia mają klucz;
- [x] dialog pojedynczej instancji i tytuł błędu startu mają klucze;
- [x] logi, kody, nazwy produktu i zapisane dane startowe mają jawny wyjątek;
- [x] placeholdery `Shell_VersionFormat` i `Dialog_StartupFailure_Message` są
  identyczne w PL/EN;
- [x] komunikaty błędów użytkowych nie zawierają surowego `exception.Message`;
- [x] każdy klucz paczki ma co najmniej jedno potwierdzone miejsce użycia;
- [x] martwe kandydaty `Common_Close`, `Common_Save`, `Common_Refresh`,
  `Common_Select`, `Common_Export`, `Common_Import` i `Common_Confirm` usunięto;
- [x] kolizje `AKCJA/AKCJE`, `STATUS/STAN` i `POWÓD/PRZYCZYNA` rozstrzygnięto;
- [x] nie zidentyfikowano wpływu na JSON, CSV, `.tacho`, SQLite ani protokół v3;
- [x] jedyne ryzyko układu to angielskie `JOURNEY PLANNER` i `Compensations`
  w nawigacji o szerokości 195 px — do kontroli wizualnej po wdrożeniu zasobów.

### Werdykt

**GO — paczka 1 zatwierdzona.** Katalog 33 kluczy jest wiążący dla kolejnych
paczek. Paczka została zamknięta bez pozycji otwartych.

## Paczka 2 — Dashboard

**Zakres:** szybkie akcje, alerty, karty slotów S1/S2, skrót rekompensat,
wybór celu odpoczynku i licznik odpoczynku.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 58 nowych kluczy — 23 etykiety i akcje, 35 wartości dynamicznych
i domenowych. Dodatkowo Dashboard ponownie używa zatwierdzonego
`Common_StatusHeader`.

### Granica paczki

Wnętrze wirtualnego tachografu (`MainWindow.xaml:72-87`) nie należy do tej
paczki. Jego LCD, menu, etykiety przycisków, statusy kart urządzenia i skrócone
`DeviceLabel` celów odpoczynku przechodzą w całości do paczki 3. `ActivityLabel`
z `MainViewModel.cs:1821` jest drugim presenterem `DriverActivity`; także należy
do paczki 3 i musi jawnie obsłużyć wszystkie sześć wartości zamiast fallbacku
`ToString().ToUpperInvariant()`. Polskie `DeviceLabel`, takie jak
`15 MIN CZĘŚĆ 1`, `DZIENNY 9H` i `TYGODNIOWY 24H`, oznaczają, że paczka 3 jest
warunkiem kompletności wersji EN, a nie korektą kosmetyczną.

`ManualEntrySelectionMessage`, `ManualEntryQualificationMessage` oraz akcja
rozliczenia opcjonalnej luki są widoczne w panelu alertów. Etykieta akcji
należy do paczki 2, ale dynamiczne treści stanu wpisu manualnego należą do
UI-03. `OperationStatus` jest współdzieloną
powierzchnią komunikatów wielu funkcji; jej komunikaty sukcesu, walidacji
i błędów należą do UI-09. Paczka 2 inwentaryzuje kontrolki i jedyny własny alert
Dashboardu, nie tworzy przedwcześnie kluczy należących do tych obszarów.

### Etykiety i akcje

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Dashboard_QuickActionsTitle` | SZYBKIE AKCJE | QUICK ACTIONS | U |
| `Dashboard_GeneratePdfAction` | Generuj raport PDF | Generate PDF report | U/T |
| `Dashboard_RefreshReportAction` | Odśwież raport | Refresh report | U |
| `Dashboard_ExportObligationsCsvAction` | Eksportuj zobowiązania CSV | Export obligations CSV | U/T |
| `Dashboard_DiagnosticReportAction` | Raport diagnostyczny | Diagnostic report | U |
| `Dashboard_AlertsTitle` | ALERTY | ALERTS | U |
| `Dashboard_ResolveOptionalGapAction` | ROZLICZ OPCJONALNĄ LUKĘ | RESOLVE OPTIONAL GAP | U |
| `Dashboard_Slot1Title` | SLOT 1 - KIEROWCA AKTYWNY | SLOT 1 - ACTIVE DRIVER | U/T |
| `Dashboard_Slot2Title` | SLOT 2 - KIEROWCA ZMIENNIK | SLOT 2 - CO-DRIVER | U/T |
| `Dashboard_DriverNameLabel` | Imię i nazwisko: | Driver name: | U |
| `Dashboard_ActivityStatusLabel` | Status: | Activity: | U |
| `Dashboard_TimeUntilBreakLabel` | Do przerwy jazdy: | Time to break: | U |
| `Dashboard_DrivingAndTimeUntilBreakLabel` | Jazda / do przerwy: | Driving / time to break: | U |
| `Dashboard_DailyDrivingLabel` | Jazda dzienna: | Daily driving: | U |
| `Dashboard_DailyDutyLabel` | Praca dobowa: | Daily duty: | U |
| `CompensationSummary_OpenHeader` | OTWARTE | OPEN | U |
| `CompensationSummary_DebtHeader` | DŁUG | DEBT | U |
| `CompensationSummary_CompleteBeforeHeader` | UKOŃCZ PRZED | COMPLETE BEFORE | U |
| `Dashboard_SelectedBreakLabel` | WYBRANA PAUZA | SELECTED BREAK | U |
| `Dashboard_ElapsedLabel` | TRWA | ELAPSED | U |
| `Dashboard_RemainingLabel` | POZOSTAŁO | REMAINING | U |
| `Dashboard_StartBreakAction` | ROZPOCZNIJ PAUZĘ | START BREAK | U |
| `Dashboard_StartCoDriverBreakAction` | PAUZA KIEROWCY 2 | CO-DRIVER BREAK | U |

Nagłówek `STATUS` w skrócie rekompensat używa istniejącego
`Common_StatusHeader`. Pełne frazy akcji pozostają kluczami Dashboardu:
nie są składane z nieistniejących kluczy ogólnych `Refresh` albo `Export`.
`Dashboard_ActivityStatusLabel` świadomie nie jest tłumaczeniem słowo w słowo:
angielskie `Activity:` nazywa faktyczną zawartość pola trafniej niż `Status:`.

### Wartości aktywności i brak karty

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Card_NoCard` | BRAK KARTY | NO CARD | U |
| `Activity_Driving` | Jazda | Driving | P |
| `Activity_OtherWork` | Inna praca | Other work | P |
| `Activity_Availability` | Dyspozycyjność | Availability | P |
| `Activity_BreakOrRest` | Przerwa / odpoczynek | Break / rest | P |
| `Activity_Unknown` | Nieznana | Unknown | P |

`DriverActivity.OutOfScope` jest prezentowane jako techniczne `OUT` i nie
otrzymuje tłumaczenia. `DriverActivity.Unknown` ma jawny klucz
`Activity_Unknown`. Dashboardowy presenter `DriverActivity` musi więc obsłużyć
wszystkie sześć wartości bez fallbacku `activity.ToString()`.

### Cele i stany odpoczynku

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `RestTarget_Break15Part1` | Przerwa 15 min · część 1 | 15-minute break · part 1 | U |
| `RestTarget_Break30Part2` | Przerwa 30 min · część 2 | 30-minute break · part 2 | U |
| `RestTarget_Break45Full` | Przerwa 45 min · pełna | 45-minute break · full | U |
| `RestTarget_Daily9Hours` | Odpoczynek dzienny · 9 h | Daily rest period · 9 h | U |
| `RestTarget_Daily11Hours` | Odpoczynek dzienny · 11 h | Daily rest period · 11 h | U |
| `RestTarget_Weekly24Hours` | Odpoczynek tygodniowy · 24 h | Weekly rest period · 24 h | U |
| `RestTarget_Weekly45Hours` | Odpoczynek tygodniowy · 45 h | Weekly rest period · 45 h | U |
| `RestStatus_Waiting` | OCZEKUJE | WAITING | P |
| `RestStatus_InProgress` | W TRAKCIE | IN PROGRESS | P |
| `RestStatus_Completed` | ZALICZONA | COMPLETED | P |
| `RestStatus_InProgressWhileMoving` | W TRAKCIE · W RUCHU | IN PROGRESS · WHILE MOVING | P |
| `RestStatus_CompletedWhileMoving` | ZALICZONA · W RUCHU | COMPLETED · WHILE MOVING | P |

Nazwy z kolumny `Name` w `RestTargetOption` są lokalizowane tym katalogiem.
Skrócone wartości `DeviceLabel` nie są ich drugą rolą prezentacyjną i zostaną
zinwentaryzowane osobno z LCD w paczce 3.

### Skrót rekompensat

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `CompensationSummary_NoOpen` | BRAK OTWARTYCH | NO OPEN ITEMS | P |
| `CompensationSummary_Overdue` | ZALEGŁE | OVERDUE | P |
| `CompensationSummary_PaidLate` | SPŁACONO PO TERMINIE | PAID LATE | P |
| `CompensationSummary_OnTime` | W TERMINIE | ON TIME | P |

Te cztery wartości obejmują cały presenter `CompensationOverview.StatusText`.
Pozostałe statusy i etykiety widoku szczegółowego zobowiązań należą do UI-05.
Klucze skrótu są współdzielone z nakładkami S1/S2, które nie mogą tworzyć ich
duplikatów w UI-10.

### Alerty naruszeń

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Dashboard_ViolationAlertFormat` | K{0} · {1}: {2} | D{0} · {1}: {2} | U/T |
| `Dashboard_ManualEntryRequiredAlert` | WPIS MANUALNY WYMAGANY · jazda zablokowana do rozliczenia luki po wyjęciu karty. | MANUAL ENTRY REQUIRED · driving is blocked until the gap after card withdrawal is resolved. | U |
| `Violation_ContinuousDrivingExceeded` | Przekroczono czas jazdy ciągłej | Continuous driving limit exceeded | P |
| `Violation_MissingRequiredBreak` | Brak wymaganej przerwy | Required break missing | P |
| `Violation_DailyDrivingExceeded` | Przekroczono dzienny czas jazdy | Daily driving limit exceeded | P |
| `Violation_WeeklyDrivingExceeded` | Przekroczono tygodniowy czas jazdy | Weekly driving limit exceeded | P |
| `Violation_FortnightlyDrivingExceeded` | Przekroczono dwutygodniowy czas jazdy | Fortnightly driving limit exceeded | P |
| `Violation_TooManyDailyExtensions` | Zbyt wiele wydłużeń dziennego czasu jazdy | Too many daily driving extensions | P |
| `Violation_DailyRestMissing` | Brak odpoczynku dobowego | Daily rest period missing | P |
| `Violation_TooManyReducedDailyRests` | Zbyt wiele skróconych odpoczynków dobowych | Too many reduced daily rest periods | P |
| `Violation_WeeklyRestMissing` | Brak odpoczynku tygodniowego | Weekly rest period missing | P |
| `Violation_WeeklyRestPatternInvalid` | Nieprawidłowy wzorzec odpoczynków tygodniowych | Invalid weekly rest pattern | P |
| `Violation_WeeklyRestCompensationOverdue` | Zaległa rekompensata odpoczynku tygodniowego | Weekly rest compensation overdue | P |

W `Dashboard_ViolationAlertFormat` `{0}` oznacza numer slotu, `{1}` niezmienny
artykuł, a `{2}` wynik lokalnego presentera `ViolationType`. Pole
`RuleViolation.Message` oraz nazwa enuma nie są wyświetlane bezpośrednio.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `MainWindow.xaml:90-92` | szybkie akcje, alerty i przycisk opcjonalnej luki | klucze `Dashboard_*` tej paczki; treści wpisu manualnego → UI-03, `OperationStatus` → UI-09 |
| `MainWindow.xaml:96-106,133-143` | tytuły slotów i etykiety kierowców | klucze `Dashboard_*` |
| `MainWindow.xaml:111-114,148-151` | skrót rekompensat | trzy `CompensationSummary_*Header` + `Common_StatusHeader`; status przez presenter |
| `MainWindow.xaml:119-128,156-165` | cel, licznik i akcje odpoczynku | `Dashboard_*`, `RestTarget_*`, `RestStatus_*` |
| `MainViewModel.cs:35-44` | siedem nazw i siedem etykiet LCD celów | `Name` → `RestTarget_*`; `DeviceLabel` → paczka 3 |
| `MainViewModel.cs:742,800,1072-1080` | brak karty i sześć wartości `DriverActivity` | `Card_NoCard` + pięć kluczy `Activity_*`; `OUT` pozostaje T |
| `MainViewModel.cs:1821-1827` | drugi presenter `ActivityLabel` urządzenia, fallback dla trzech wartości | paczka 3; jawna obsługa wszystkich sześciu wartości bez `ToString()` |
| `MainViewModel.cs:833-841` | alerty slotów, naruszeń i wymaganego wpisu | format Dashboardu + presenter wszystkich 11 wartości `ViolationType` |
| `MainViewModel.cs:959-1051` | pięć stanów licznika odpoczynku | `RestStatus_*`; `HH:MM` pozostaje T |
| `CompensationPresentation.cs:15-39` | cztery statusy skrótu rekompensat | `CompensationSummary_*`; kolory pozostają T |

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| numer slotu w `Dashboard_ViolationAlertFormat` | T | wartość techniczna podstawiana przez `{0}`; pełne tytuły slotów są lokalizowane w całości |
| nazwa kierowcy (`CardOwner`, `Card2Owner`) | O | dane użytkownika |
| `---`, `—`, kolory i procent postępu | T | znaczniki i wartości prezentacyjne bez języka |
| `HH:MM`, liczby limitów i separator ` / ` | T | format czasu trwania i dane liczbowe |
| `PDF`, `CSV`, `OUT` | T | formaty i ustalony kod trybu |
| artykuł naruszenia | T | niezmienna wartość podstawiana do lokalizowanego formatu |

### Kontrola paczki

- [x] wszystkie statyczne literały Dashboardu mają klucz albo jawnego właściciela późniejszej paczki;
- [x] katalog paczki 1 sprawdzono przed dodaniem kluczy; `Common_StatusHeader` jest ponownie użyty;
- [x] oba sloty używają wspólnych etykiet, celów, statusów i presentera aktywności;
- [x] wszystkie 7 celów oraz 5 stanów odpoczynku ma decyzję;
- [x] wszystkie 4 możliwe statusy `CompensationOverview` ma decyzję;
- [x] wszystkie 6 wartości `DriverActivity` ma decyzję: 5 kluczy i techniczne `OUT`;
- [x] wszystkie 11 wartości `ViolationType` ma jawny presenter bez fallbacku `ToString()`;
- [x] jedyny format paczki ma identyczny zbiór placeholderów `{0}`, `{1}`, `{2}` w PL/EN;
- [x] żaden klucz nie zmienia enumów, DTO, JSON, CSV, SQLite ani danych użytkownika;
- [x] ryzyko długości EN dotyczy tytułów slotów, celu odpoczynku, statusu w ruchu i `COMPLETE BEFORE` — do kontroli wizualnej w obu kartach;
- [x] paczka nie obejmuje wnętrza wirtualnego urządzenia, UI-03 ani UI-09.

### Werdykt

**GO — paczka 2 zatwierdzona.** Katalog 58 nowych kluczy jest wiążący dla
paczki 3 i kolejnych.

### Zależność kompletności Dashboardu

GO paczki 2 zatwierdza jej katalog, ale nie oznacza jeszcze pełnego pokrycia
wszystkich wartości dochodzących do Dashboardu przez bindingi.
`CompensationOverview.NearestDueText` i `NearestDueCompactText` korzystają
z `GameCalendarFormatter` i zawierają lokalizowane prefiksy terminu, nazwy dni
oraz etykietę dnia gry. Ich właścicielem jest przekrojowa paczka `X-01` —
wspólne formatery czasu i terminów. Pełna kompletność EN Dashboardu zależy od
GO `X-01`.

`X-01` obejmie 12 nowych kluczy: 7 nazw dni używanych przez pełny wariant
prezentacji, 4 nieurządzeniowe prefiksy `GameDeadlineSemantic`
i `GameCalendar_DayFormat` (`Dzień {0}` / `Day {0}`). Ponownie użyje 7 skrótów
`Weekday_Short_*` zatwierdzonych w paczce 3. Te same zasoby obsłużą Dashboard,
Planer, Raporty Desktop i `WeeklyRestWindowFormatter`.

## Paczka 3 — wirtualny tachograf

**Zakres:** rama urządzenia, podpowiedzi pól kierowców, trzy linie LCD, wszystkie
strony menu, skrócone cele odpoczynku, liczniki kart, stany drukowania i wpisu
manualnego na LCD oraz urządzeniowy presenter aktywności.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 79 nowych kluczy — 51 etykiet ramy i menu, 10 komunikatów LCD,
5 etykiet aktywności urządzenia, 2 statusy skrótu rekompensaty oraz
4 urządzeniowe prefiksy terminów i 7 współdzielonych skrótów dni tygodnia.

### Granica paczki

Paczka obejmuje wyłącznie powierzchnię wirtualnego urządzenia z
`MainWindow.xaml:72-87` i teksty budujące `DeviceLine1-3`. Modal wkładania
i wyjmowania karty z `MainWindow.xaml:802-824`, jego etykiety krajów,
potwierdzenia, walidacje i `OperationStatus` należą do UI-09 oraz UI-04.
Nieużywane obecnie właściwości `CardStatus` i `Card2Status` nie są tekstem
widocznym i nie otrzymują osobnych kluczy.

`Card_NoCard` z paczki 2 jest ponownie używany na LCD. `OUT`, kody krajów,
numery slotów, czas `HH:MM`, prędkość, przebieg i symbole urządzenia pozostają
wartościami technicznymi.

### Rama urządzenia

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Device_CurrentViewTitle` | BIEŻĄCY PODGLĄD TACHOGRAFU | CURRENT TACHOGRAPH VIEW | U |
| `Device_DriverButtonTooltipFormat` | Kierowca {0}: kliknij, przytrzymaj 3 s, aby wyjąć kartę | Driver {0}: click and hold for 3 s to withdraw the card | U/T |

Przecinek przed `aby` w polskiej podpowiedzi jest świadomą korektą gramatyczną
względem obecnego XAML, bez zmiany znaczenia ani zachowania przycisku.

### Pozycje menu i stany wyboru

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceMenu_Print` | WYDRUK | PRINT | U |
| `DeviceMenu_ManualEntry` | WPIS MANUALNY | MANUAL ENTRY | U |
| `DeviceMenu_BreakOrRest` | PAUZA / ODPOCZ. | BREAK / REST | U |
| `DeviceMenu_Countries` | KRAJE | COUNTRIES | U |
| `DeviceMenu_Modes` | TRYBY | MODES | U |
| `DeviceMenu_CardCounters` | LICZNIKI KART | CARD COUNTERS | U |
| `DeviceMenu_Settings` | USTAWIENIA | SETTINGS | U |
| `DeviceMenu_PrintDriver1Day` | 24H KIEROWCY 1 | 24H DRIVER 1 | U/T |
| `DeviceMenu_PrintVehicleDay` | 24H POJAZDU | 24H VEHICLE | U/T |
| `ActivityUpper_OtherWork` | INNA PRACA | OTHER WORK | P |
| `ActivityUpper_Availability` | DYSPOZYCYJNOŚĆ | AVAILABILITY | P |
| `ActivityUpper_Rest` | ODPOCZYNEK | REST | P |
| `DeviceMenu_StartCountryFormat` | START: {0} | START: {0} | U/T |
| `DeviceMenu_EndCountryFormat` | KONIEC: {0} | END: {0} | U/T |
| `DeviceMenu_OutModeFormat` | OUT {0} | OUT {0} | U/T |
| `DeviceMenu_FerryModeFormat` | PROM {0} | FERRY {0} | U/T |
| `DeviceState_On` | WŁ. | ON | U |
| `DeviceState_Off` | WYŁ. | OFF | U |
| `DeviceMenu_CardStatusFormat` | KARTA {0} {1} | CARD {0} {1} | U/T |
| `DeviceState_Ready` | GOTOWA | READY | U |
| `DeviceState_Missing` | BRAK | MISSING | U |
| `DeviceMenu_SpeedThreshold` | PRÓG PRĘDKOŚCI | SPEED THRESHOLD | U |
| `DeviceMenu_RegulatoryWeek` | TYDZIEŃ REGULACYJNY | REGULATORY WEEK | U |

Nazwy `DeviceMenu_Print`, `DeviceMenu_ManualEntry`, `DeviceMenu_Countries`,
`DeviceMenu_Modes` i `DeviceMenu_Settings` są używane zarówno jako pozycja
menu głównego, jak i tytuł odpowiadającej jej strony. Semantyka i pisownia są
w obu rolach identyczne, więc nie powstają duplikaty `*Title`.

### Skrócone cele odpoczynku LCD

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceRestTarget_Break15Part1` | 15 MIN CZĘŚĆ 1 | 15 MIN PART 1 | U |
| `DeviceRestTarget_Break30Part2` | 30 MIN CZĘŚĆ 2 | 30 MIN PART 2 | U |
| `DeviceRestTarget_Break45Full` | 45 MIN PEŁNA | 45 MIN FULL | U |
| `DeviceRestTarget_Daily9Hours` | DZIENNY 9H | DAILY 9H | U |
| `DeviceRestTarget_Daily11Hours` | DZIENNY 11H | DAILY 11H | U |
| `DeviceRestTarget_Weekly24Hours` | TYGODNIOWY 24H | WEEKLY 24H | U |
| `DeviceRestTarget_Weekly45Hours` | TYGODNIOWY 45H | WEEKLY 45H | U |

To osobne, zwarte wartości `DeviceLabel`; nie zastępują pełnych `RestTarget_*`
z paczki 2. Wszystkie siedem musi wejść do zasobów, aby LCD w wersji EN nie
pozostał częściowo polski.

### Liczniki kart

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceCounter_BreakFormat` | PAUZA {0} | BREAK {0} | U/T |
| `DeviceCounter_TargetFormat` | CEL {0} | TARGET {0} | U/T |
| `DeviceCounter_ContinuousDrivingFormat` | CIĄGŁA {0} | CONTINUOUS {0} | U/T |
| `DeviceCounter_TimeToBreakFormat` | DO PRZERWY {0} | TO BREAK {0} | U/T |
| `DeviceCounter_DailyDrivingFormat` | DZIENNA {0} | DAILY {0} | U/T |
| `DeviceCounter_DailyDutyFormat` | PRACA {0} | DUTY {0} | U/T |
| `DeviceCounter_WeeklyDrivingFormat` | TYDZIEŃ {0} | WEEK {0} | U/T |
| `DeviceCounter_FortnightlyDrivingFormat` | 2 TYG. {0} | 2 WKS {0} | U/T |
| `DeviceCounter_DailyRestDeadlineFormat` | ODP. DZIENNY {0} | DAILY REST {0} | U/T |
| `DeviceCounter_WeeklyRestDeadlineFormat` | ODP. TYG. {0} | WEEKLY REST {0} | U/T |
| `DeviceCounter_CompensationFormat` | REKOMPENSATA {0} | COMPENSATION {0} | U/T |
| `DeviceCounter_ExtensionsUsageFormat` | WYDŁUŻENIA {0} · TYDZIEŃ | EXTENSIONS {0} · WEEK | U/T |
| `DeviceCounter_ReducedDailyRestsUsageFormat` | SKRÓCONE {0} · OD ODP. TYG. | REDUCED {0} · SINCE WEEKLY REST | U/T |

Oba sloty używają tych samych 13 formatów. Wartości `{0}` są gotowymi,
niezależnie sformatowanymi licznikami. Obecne `IsExceededCounterItem` rozpoznaje
rodzaj licznika po polskim prefiksie. Implementacja M5.2 musi zastąpić to
porównaniem semantycznego identyfikatora pozycji przed podłączeniem zasobów;
logika koloru nie może zależeć od aktywnego języka.

### Tytuły stron menu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceMenu_MainTitle` | MENU GŁÓWNE | MAIN MENU | U |
| `DeviceMenu_SelectBreakTitle` | WYBIERZ PAUZĘ | SELECT BREAK | U |
| `DeviceMenu_SelectCardTitle` | WYBIERZ KARTĘ | SELECT CARD | U |
| `DeviceMenu_CardCountersTitleFormat` | LICZNIKI KARTY {0} | CARD {0} COUNTERS | U/T |
| `DeviceMenu_StartCountryTitle` | KRAJ START | START COUNTRY | U |
| `DeviceMenu_EndCountryTitle` | KRAJ KONIEC | END COUNTRY | U |

Fallback `_deviceMenuPage.ToUpperInvariant()` nie może być presenterem.
Wszystkie siedem stron głównych i sześć tytułów specjalnych mają jawne
mapowanie; techniczne identyfikatory `root`, `print`, `manual`, `rest-target`,
`countries`, `modes`, `counter-cards`, `counters-1`, `counters-2`,
`country-start`, `country-end` i `settings` nie trafiają do LCD.

### Komunikaty LCD

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Device_CardReadingFormat` | KARTA {0} - ODCZYT | CARD {0} - READING | U/T |
| `Device_DriverFallback` | KIEROWCA | DRIVER | U |
| `Device_ManualEntryRequired` | ! WPIS MANUALNY ! | ! MANUAL ENTRY ! | U |
| `Device_RequiredSlotFormat` | SLOT {0} WYMAGANY | SLOT {0} REQUIRED | U/T |
| `Device_DrivingBlocked` | JAZDA ZABLOKOWANA | DRIVING BLOCKED | U |
| `Device_ConfirmActivity` | POTWIERDŹ AKTYWNOŚĆ | CONFIRM ACTIVITY | U |
| `Device_Printing` | DRUKOWANIE... | PRINTING... | U |
| `Device_DrivingWithoutCard` | ! JAZDA BEZ KARTY ! | ! DRIVING WITHOUT CARD ! | U |
| `Device_CardErrorFormat` | X  BŁĄD KARTY {0}  X | X  CARD {0} ERROR  X | U/T |
| `Device_NoCardShortFormat` | BRAK K{0} | NO C{0} | U/T |

Paski postępu, `OK`, `C`, strzałki, `P`, `>`, `K1`, maska przebiegu, `km/h`
i `km` są symbolami albo jednostkami technicznymi i pozostają bez lokalizacji.
`DeviceMenu_PrintDriver1Day` jest ponownie używany podczas drukowania.

### Urządzeniowy presenter aktywności

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceActivity_Driving` | KIEROWNICA | DRIVING | P |
| `DeviceActivity_OtherWork` | MŁOTKI | OTHER WORK | P |
| `DeviceActivity_Availability` | GOTOWOŚĆ | AVAILABILITY | P |
| `DeviceActivity_BreakOrRest` | ŁÓŻKO | REST | P |
| `DeviceActivity_Unknown` | NIEZNANA | UNKNOWN | P |

`DriverActivity.OutOfScope` pozostaje technicznym `OUT`. Drugi presenter musi
jawnie obsłużyć wszystkie sześć wartości i nie może kończyć się
`activity.ToString().ToUpperInvariant()`. `Card_NoCard` z paczki 2 obsługuje
brak kierowcy w obu slotach. Polska kolumna zachowuje opisy piktogramów
z istniejącego LCD, natomiast angielska konsekwentnie używa terminologii
aktywności tachografowych; nie koliduje dzięki temu z `DeviceState_Ready`.

### Skrót rekompensaty na LCD

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceCompensation_Overdue` | PRZETERMINOWANA | OVERDUE | P |
| `DeviceCompensation_DueByWeekFormat` | DO TYG. {0} | DUE WK {0} | P/T |

Kwota `HH:MM`, opcjonalna liczba zobowiązań i separator ` · ` są formatowane
niezależnie od tekstowego statusu. Ten presenter jest odrębny od czterech
statusów `CompensationSummary_*` na Dashboardzie.

### Terminy i dni tygodnia na LCD

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `DeviceDeadline_CompleteByPrefix` | KONIEC≤ | END≤ | P |
| `DeviceDeadline_StartNoLaterThanPrefix` | START≤ | START≤ | P |
| `DeviceDeadline_CompleteBeforePrefix` | PRZED | BEFORE | P |
| `DeviceDeadline_AvailableFromPrefix` | OD | FROM | P |
| `Weekday_Short_Monday` | PON | MON | P |
| `Weekday_Short_Tuesday` | WT | TUE | P |
| `Weekday_Short_Wednesday` | ŚR | WED | P |
| `Weekday_Short_Thursday` | CZW | THU | P |
| `Weekday_Short_Friday` | PT | FRI | P |
| `Weekday_Short_Saturday` | SOB | SAT | P |
| `Weekday_Short_Sunday` | NDZ | SUN | P |

Urządzeniowy wariant `GameDeadlineFormatter.FormatDevice` korzysta ze wszystkich
11 wartości tej tabeli. Siedem `Weekday_Short_*` jest jednak neutralne,
ponieważ `GameWeekdayNames.Abbreviated` zasila również `FormatCompact` na
Dashboardzie, w Planerze i Raportach. `X-01` ponownie użyje tych skrótów oraz
doda nazwy dni dla pełnego wariantu prezentacji, nieurządzeniowe prefiksy
i etykietę dnia gry.
`D{0}`, numer dnia, godzina i okres `n/6` pozostają techniczne.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `MainWindow.xaml:72-87` | tytuł i dwie podpowiedzi kierowców | `Device_CurrentViewTitle`, wspólny format podpowiedzi |
| `MainViewModel.cs:35-44` | 7 polskich `DeviceLabel` | 7 kluczy `DeviceRestTarget_*` |
| `MainViewModel.cs:1737-1750` | wszystkie pozycje menu | jawne klucze menu, stanów i 13 liczników |
| `MainViewModel.cs:1752` | `WŁ.` / `WYŁ.` | `DeviceState_On` / `DeviceState_Off` |
| `MainViewModel.cs:1765-1819` | tytuły stron i komunikaty `DeviceLine1-3` | klucze `DeviceMenu_*` i `Device_*`; wartości T pozostają bez zmian |
| `MainViewModel.cs:1821-1827` | drugi presenter `DriverActivity` | 5 `DeviceActivity_*` + techniczne `OUT`; bez fallbacku |
| `MainViewModel.cs:1829-1835` | wykrywanie przekroczeń po polskim prefiksie | zastąpić semantycznym identyfikatorem pozycji |
| `MainViewModel.cs:2332-2342` | skrót rekompensaty | 2 klucze `DeviceCompensation_*` |
| `GameCalendarFormatter.cs:5-77` | skróty dni współdzielone przez `FormatCompact` i `FormatDevice`; prefiksy urządzenia | 7 `Weekday_Short_*` + 4 `DeviceDeadline_*Prefix`; pozostałe formaty → `X-01` |
| `WeeklyRestWindowFormatter.cs:25-44` | okres `n/6` + termin urządzeniowy | okres T, termin przez urządzeniowy presenter |

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| `OUT`, ISO i kod tachografowy kraju | T | stabilne kody urządzenia |
| identyfikatory stron menu | T | sterują logiką, nie są tekstem UI |
| numery slotów i kart | T | wartości przekazywane do formatów |
| `HH:MM`, `D{0}`, `n/6`, prędkość i przebieg | T | dane i formaty urządzenia |
| `km/h`, `km`, `24H` | T | jednostki i zwarty zapis urządzenia |
| `▲`, `▼`, `OK`, `C`, `P`, `>`, `K1`, `X`, nawiasy i paski postępu | T | symbole fizycznego interfejsu |
| nazwa kierowcy | O | dane użytkownika; nie jest tłumaczona ani wymuszana na uppercase przez kulturę |
| nazwa i ścieżka pliku wydruku | T | kontrakt systemu plików, nie etykieta UI |

### Oczekiwane identyczne wartości PL/EN

- `DeviceMenu_StartCountryFormat`;
- `DeviceMenu_OutModeFormat`;
- `DeviceDeadline_StartNoLaterThanPrefix`.

### Oczekiwane duplikaty wartości między różnymi kluczami

Test duplikatów wartości musi używać jawnej listy dozwolonych par. Dla katalogu
170 kluczy lista zawiera dokładnie pięć pozycji:

| Wartość EN | Klucze | Decyzja |
|---|---|---|
| `OTHER WORK` | `ActivityUpper_OtherWork`, `DeviceActivity_OtherWork` | zamierzone; pozycja menu i etykieta aktywności są odrębnymi rolami, a PL zachowuje `INNA PRACA` / `MŁOTKI` |
| `AVAILABILITY` | `ActivityUpper_Availability`, `DeviceActivity_Availability` | zamierzone; odrębne role, PL zachowuje `DYSPOZYCYJNOŚĆ` / `GOTOWOŚĆ` |
| `REST` | `ActivityUpper_Rest`, `DeviceActivity_BreakOrRest` | zamierzone; odrębne role, PL zachowuje `ODPOCZYNEK` / `ŁÓŻKO` |
| `FROM` | `Common_From`, `DeviceDeadline_AvailableFromPrefix` | osobne klucze są wymagane mimo identycznego `OD` / `FROM`: granica zakresu i semantyka terminu „dostępne od” nie są tą samą rolą |
| `OVERDUE` | `CompensationSummary_Overdue`, `DeviceCompensation_Overdue` | wspólny termin EN; PL świadomie zachowuje `ZALEGŁE` na Dashboardzie i urządzeniowe `PRZETERMINOWANA` na LCD zgodnie z UI freeze |

Wariant LCD `PRZETERMINOWANA` nie tworzy odrębnego pojęcia domenowego. Ewentualne
ujednolicenie polskiego brzmienia można rozważyć dopiero po zdjęciu UI freeze;
M5 nie zmienia zatwierdzonej polskiej treści urządzenia.

### Kontrola paczki

- [x] wszystkie 3 statyczne literały urządzenia w XAML mają klucz;
- [x] wszystkie 7 stron menu głównego, podstrony i 13 liczników mają jawne mapowanie;
- [x] wszystkie 7 skróconych `DeviceLabel` ma wersję EN;
- [x] wszystkie stany budujące `DeviceLine1-3` mają klucz albo decyzję T/O;
- [x] wszystkie 6 wartości `DriverActivity` ma decyzję w drugim presenterze;
- [x] wszystkie 4 wartości `GameDeadlineSemantic` i 7 dni tygodnia ma wariant LCD;
- [x] wszystkie formaty mają identyczne zbiory placeholderów PL/EN;
- [x] sprawdzono cały zatwierdzony katalog paczek 1–2; ponownie użyto `Card_NoCard`;
- [x] wskazano zależność koloru od polskich prefiksów i wymagane rozdzielenie semantyki od tekstu;
- [x] modal karty i komunikaty operacyjne mają jawnego właściciela w UI-04/UI-09;
- [x] nie zmienia się sterowanie urządzeniem, JSON stanu, PDF, SQLite ani telemetria.

### Werdykt

**GO — paczka 3 zatwierdzona.** Łączny katalog paczek 1–3 zawiera 170
wiążących kluczy bez powtórzeń nazw. `X-01` przechodzi do paczki 5.

## Paczka 4 — Historia, luki i wpis manualny

**Zakres:** tabela historii aktywności, rejestr luk, modal planu wpisu manualnego,
podsumowanie kwalifikacji oraz wszystkie walidacje domenowe dochodzące do
`ManualEntryValidationMessage`.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 87 nowych kluczy — 14 etykiet Historii i luk, 18 wartości
prezenterów Historii, 20 etykiet modala, 18 formatów podsumowań i kwalifikacji
oraz 17 komunikatów walidacji.

### Granica paczki

Paczka obejmuje `MainWindow.xaml:174-245`, modal wpisu manualnego
z `MainWindow.xaml:850-1048`, wartości bindowane z `MainViewModel`,
`ActivityGapListItemDto` i `ManualEntryPlanEditor`.

Potwierdzenie odrzucenia niezapisanych zmian w `MessageBox`, komunikaty
`OperationStatus` oraz błędy eksportu i importu należą do UI-09. Nazwy krajów
i dialog karty pozostają w UI-04/UI-09. `ManualEntryDayOption.DisplayName`
użyje planowanego w `X-01` formatu `GameCalendar_DayFormat` (`Dzień {0}` /
`Day {0}`), dlatego kompletność listy dni zależy od GO `X-01`.

`ResolveGapStatus` i `ManualEntryPersistenceStatus` sterują przepływem i nie są
wyświetlane użytkownikowi. Nie otrzymują kluczy. Nieaktywna obecnie ścieżka
`ManualEntryWizardDraft` również nie tworzy kandydatów; jej surowe wyjątki nie
mogą zostać podłączone do UI bez osobnej decyzji lokalizacyjnej.

### Historia i rejestr luk — etykiety

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `History_Title` | HISTORIA AKTYWNOŚCI · WSZYSTKIE KARTY | ACTIVITY HISTORY · ALL CARDS | U |
| `History_ExportDriver1TachoAction` | Eksportuj .tacho kierowcy 1 | Export driver 1 .tacho | U/T |
| `History_ImportTachoAction` | Importuj .tacho | Import .tacho | U/T |
| `History_CardHeader` | KARTA | CARD | U |
| `History_FromGameTimeHeader` | OD · CZAS GRY | FROM · GAME TIME | U |
| `History_ToGameTimeHeader` | DO · CZAS GRY | TO · GAME TIME | U |
| `Common_ActivityHeader` | AKTYWNOŚĆ | ACTIVITY | U |
| `History_ConditionHeader` | WARUNEK | CONDITION | U |
| `Gap_ShowResolved` | Pokaż rozliczone | Show resolved | U |
| `Gap_HeaderFormat` | LUKI AKTYWNOŚCI · NIEROZLICZONE: {0} | ACTIVITY GAPS · UNRESOLVED: {0} | U/T |
| `Gap_SlotHeader` | SLOT | SLOT | U/T |
| `Gap_DurationHeader` | DŁUGOŚĆ | DURATION | U |
| `Gap_CauseHeader` | PRZYCZYNA | CAUSE | U |
| `Gap_StateHeader` | STAN | STATE | U |

Historia ponownie używa `Navigation_History`, `Common_SourceHeader`,
`Common_From`, `Common_To` i `Common_ActionHeader`. Modal ponownie używa
`Common_ActivityHeader`; rola jest neutralna i nie wymaga drugiego klucza.

### Presentery Historii i luk

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ActivitySource_Telemetry` | Telemetria | Telemetry | P |
| `ActivitySource_Manual` | Ręcznie | Manual | P |
| `ActivitySource_Reconstructed` | Odtworzona | Reconstructed | P |
| `ActivitySource_Mixed` | Mieszane | Mixed | P |
| `ActivitySource_ManualEntry` | Wpis manualny | Manual entry | P |
| `ActivitySource_AutomaticCrewReconstruction` | Automatyczne odtworzenie załogi | Automatic crew reconstruction | P |
| `SpecialCondition_None` | Brak | None | P |
| `SpecialCondition_FerryCrossing` | Przeprawa promowa | Ferry crossing | P |
| `SpecialCondition_Mixed` | Mieszany | Mixed | P |
| `SpecialCondition_CrewBreakInMotion` | Przerwa załogi w ruchu | Crew break in motion | P |
| `GapReason_ForwardTimeJump` | Skok czasu | Time jump | P |
| `GapReason_CardRemoved` | Karta wyjęta | Card withdrawn | P |
| `GapReason_TelemetryUnavailable` | Brak telemetrii | Telemetry unavailable | P |
| `GapState_ResolvedFormat` | ROZLICZONA · {0} | RESOLVED · {0} | P/T |
| `GapState_Ongoing` | TRWA | ONGOING | P |
| `GapState_Unresolved` | NIEROZLICZONA | UNRESOLVED | P |
| `Gap_CardStillRemovedHelp` | karta nadal wyjęta | card still withdrawn | U |
| `Gap_ResolveAction` | Rozlicz | Resolve | U |

`DriverActivity` w tabeli Historii ponownie używa pięciu `Activity_*` z paczki 2
oraz technicznego `OUT`. `ActivitySource` ma 6 wartości, `SpecialCondition`
4 wartości, `ActivityGapReason` 3 wartości, a `ActivityGapState` 2 wartości —
wszystkie mają jawne mapowanie bez `ToString()`. `GapState_Ongoing` jest używany
również zamiast końca otwartej luki.

Obecne bindowanie `ActivityRecord.Activity`, `Source` i `Condition` wyświetla
nazwy enumów przez domyślne `ToString()`. M5.2 musi wprowadzić w Desktop wiersz
prezentacyjny. Tak samo polskie właściwości prezentacyjne nie powinny pozostawać
w `ActivityGapListItemDto`; DTO zachowuje wartości domenowe, a tekst tworzy
lokalny presenter Desktop.

### Modal wpisu manualnego — etykiety i akcje

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ManualEntry_Title` | ROZLICZ LUKĘ AKTYWNOŚCI | RESOLVE ACTIVITY GAP | U |
| `ManualEntry_Description` | Uzupełnij okres, w którym tachograf nie rejestrował aktywności. | Complete the period in which the tachograph did not record activity. | U |
| `ManualEntry_DriverLabel` | KIEROWCA | DRIVER | U |
| `ManualEntry_QuickChoiceTitle` | SZYBKI WYBÓR DLA CAŁEJ LUKI | QUICK CHOICE FOR THE ENTIRE GAP | U |
| `ManualEntry_QuickChoiceHelp` | Kliknięcie zastąpi cały plan jednym segmentem. | Clicking replaces the entire plan with one segment. | U |
| `ManualEntry_BreakOrRestAction` | PRZERWA / ODPOCZYNEK | BREAK / REST | U |
| `ManualEntryActivity_BreakOrRest` | Przerwa / Odpoczynek | Break / rest | P |
| `ManualEntry_PlanTitle` | PLAN WPISU | ENTRY PLAN | U |
| `ManualEntry_TimeHeader` | CZAS | TIME | U |
| `ManualEntry_FromDayLabel` | OD — DZIEŃ | FROM — DAY | U |
| `ManualEntry_HourLabel` | GODZINA | TIME | U |
| `ManualEntry_ToDayLabel` | DO — DZIEŃ | TO — DAY | U |
| `ManualEntry_AddOrReplaceTitle` | DODAJ LUB ZASTĄP SEGMENT | ADD OR REPLACE SEGMENT | U |
| `ManualEntry_EditSegmentTitle` | EDYTUJ SEGMENT | EDIT SEGMENT | U |
| `ManualEntry_AddOrReplaceAction` | DODAJ / ZASTĄP SEGMENT | ADD / REPLACE SEGMENT | U |
| `ManualEntry_SaveChangesAction` | ZAPISZ ZMIANY | SAVE CHANGES | U |
| `ManualEntry_SummaryTitle` | PODSUMOWANIE I WALIDACJA | SUMMARY AND VALIDATION | U |
| `ManualEntry_CoverageLabel` | Pokrycie: | Coverage: | U |
| `ManualEntry_RestoreDefaultAction` | PRZYWRÓĆ DOMYŚLNY WPIS | RESTORE DEFAULT ENTRY | U |
| `ManualEntry_ConfirmAction` | ZATWIERDŹ WPIS | CONFIRM ENTRY | U |

Etykieta `WPIS MANUALNY` ponownie używa `DeviceMenu_ManualEntry`. Szybkie akcje
i sumy ponownie używają neutralnych `ActivityUpper_OtherWork`,
`ActivityUpper_Availability` i `ActivityUpper_Rest`; akcja odpoczynku ma osobny
pełny tekst. Tabela planu
ponownie używa `Common_From`, `Common_To`, `Common_ActivityHeader`,
`Common_ActionsHeader`, `Common_EditAction` i `Common_RemoveAction`.
Przycisk anulowania używa `Common_CancelAction`, a znak `×` pozostaje T.
Opcje i wiersze wpisu ponownie używają `Activity_OtherWork` oraz
`Activity_Availability`. Odpoczynek ma osobny `ManualEntryActivity_BreakOrRest`,
aby zachować zatwierdzoną w istniejącym modalu pisownię `Przerwa / Odpoczynek`
bez zmiany polskiego tekstu po UI freeze.

### Separatory etykiet i wartości

`ManualEntry_CoverageLabel`, `ActivityUpper_Rest`, `ActivityUpper_OtherWork`
i `ActivityUpper_Availability` nie zawierają spacji końcowych. W czterech
wierszach modala obecne podwójne spacje są jedynym separatorem przed
wartością bindowaną, dlatego M5.2 musi przenieść je do XAML jako osobny,
niezależny od języka `Run`, na przykład `Text="&#x00A0;&#x00A0;"`, pomiędzy
etykietą zasobową i wartością.

Nie wolno przenosić separatora do `.resx`, polegać na końcowej spacji ani
sklejać etykiety bez odstępu. To czysto prezentacyjna korekta dopuszczalna
przez UI freeze; po wdrożeniu wymaga kontroli wszystkich czterech miejsc w PL
i EN.

### Podsumowania i kwalifikacja wpisu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ManualEntry_GapDurationFormat` | Długość luki: {0} | Gap duration: {0} | U/T |
| `ManualEntry_UnknownDriver` | Nieznany kierowca | Unknown driver | U |
| `ManualEntry_ReasonFormat` | Przyczyna: {0} | Cause: {0} | U |
| `ManualEntry_SegmentCountOneFormat` | {0} segment · {1} | {0} segment · {1} | U/T |
| `ManualEntry_SegmentCountFewFormat` | {0} segmenty · {1} | {0} segments · {1} | U/T |
| `ManualEntry_SegmentCountManyFormat` | {0} segmentów · {1} | {0} segments · {1} | U/T |
| `ManualEntry_Complete` | ✓ WPIS KOMPLETNY | ✓ ENTRY COMPLETE | U |
| `ManualEntry_MissingDurationFormat` | BRAK: {0} | MISSING: {0} | U/T |
| `ManualEntry_CoverageDetailsFormat` | Brak: {0} · Nakładanie: {1} | Missing: {0} · Overlap: {1} | U/T |
| `ManualEntry_SelectionSummaryFormat` | Zapis: odpoczynek {0}, inna praca {1}, dyspozycyjność {2}. | Entry: rest {0}, other work {1}, availability {2}. | U/T |
| `ManualEntry_NotSaved` | Wpis nie został jeszcze zapisany. | The entry has not been saved yet. | U |
| `ManualEntry_NoQualifiedRest` | Zakwalifikowano: brak ciągłego odpoczynku 9 h — bez resetu dobowego. | Qualified: no continuous 9 h rest — no daily reset. | U/T |
| `ManualEntry_QualifiedDailyReducedFormat` | Zakwalifikowano: odpoczynek dobowy skrócony; reset o {0}. | Qualified: reduced daily rest period; reset at {0}. | U/T |
| `ManualEntry_QualifiedDailyRegularFormat` | Zakwalifikowano: odpoczynek dobowy regularny; reset o {0}. | Qualified: regular daily rest period; reset at {0}. | U/T |
| `ManualEntry_QualifiedDailyReducedWeeklyReducedFormat` | Zakwalifikowano: odpoczynek dobowy skrócony, tygodniowy skrócony; reset o {0}. | Qualified: reduced daily rest period, reduced weekly rest period; reset at {0}. | U/T |
| `ManualEntry_QualifiedDailyReducedWeeklyRegularFormat` | Zakwalifikowano: odpoczynek dobowy skrócony, tygodniowy regularny; reset o {0}. | Qualified: reduced daily rest period, regular weekly rest period; reset at {0}. | U/T |
| `ManualEntry_QualifiedDailyRegularWeeklyReducedFormat` | Zakwalifikowano: odpoczynek dobowy regularny, tygodniowy skrócony; reset o {0}. | Qualified: regular daily rest period, reduced weekly rest period; reset at {0}. | U/T |
| `ManualEntry_QualifiedDailyRegularWeeklyRegularFormat` | Zakwalifikowano: odpoczynek dobowy regularny, tygodniowy regularny; reset o {0}. | Qualified: regular daily rest period, regular weekly rest period; reset at {0}. | U/T |

Trzy formaty liczby segmentów wymagają jawnego pluralizera `pl-PL`: forma
pojedyncza, forma dla liczb kończących się na 2–4 z wyjątkami 12–14 oraz forma
pozostała. EN używa liczby pojedynczej tylko dla 1. Presenter kwalifikacji
obsługuje pełny iloczyn 2 wartości `DailyRestClassification` i 3 stanów
`WeeklyRestClassification?`, bez sklejania przetłumaczonych fragmentów zdań.

Pluralizer jest świadomą naprawą istniejącego błędu tekstowego: obecny kod
wyświetla stałe `segmenty` także dla 1 i dla 5. Zmiana nie modyfikuje danych ani
przepływu, lecz zmienia polską treść po UI freeze, dlatego podlega osobnemu
testowi dla co najmniej `1`, `2`, `5`, `12`, `22` i `25`.

Nazwy kierowcy i numer karty pozostają O/T i są wstawiane do neutralnego układu
`{nazwa} · {numer}`. Zakres, godzina resetu i wszystkie czasy trwania są
formatowane przed przekazaniem do zasobu.

### Walidacja wpisu manualnego

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ManualEntryError_GapNotFound` | Nie znaleziono luki aktywności. | The activity gap was not found. | U |
| `ManualEntryError_GapNotCanonical` | Można rozliczyć tylko lukę z kanonicznej osi czasu gry. | Only a gap from the canonical game-time branch can be resolved. | U |
| `ManualEntryError_ProjectedGapCannotBeResolved` | Nie można rozliczyć projekcji luki. | A projected gap cannot be resolved. | U |
| `ManualEntryError_GapStillOpen` | Nie można rozliczyć trwającej luki aktywności. | An ongoing activity gap cannot be resolved. | U |
| `ManualEntryError_InvalidActivity` | Ta aktywność nie jest dostępna we wpisie manualnym. | This activity is not available in a manual entry. | U |
| `ManualEntryError_InvalidSegment` | Segment wpisu musi mieć dodatnią długość. | An entry segment must have a positive duration. | U |
| `ManualEntryError_IncompleteCoverage` | Wpis musi dokładnie pokrywać całą lukę. | The entry must cover the entire gap exactly. | U |
| `ManualEntryError_OutsideGap` | Zakres segmentu musi mieścić się w rozliczanej luce. | The segment range must stay within the gap being resolved. | U |
| `ManualEntryError_OverlappingSegments` | Segmenty wpisu nie mogą się nakładać. | Entry segments cannot overlap. | U |
| `ManualEntryError_HistoryCollision` | Wpis koliduje z istniejącą historią aktywności. | The entry conflicts with existing activity history. | U |
| `ManualEntryError_ResolutionConflict` | Luka została już rozliczona innym wpisem manualnym. | The gap has already been resolved with a different manual entry. | U |
| `ManualEntryError_EditedSegmentMissing` | Edytowany segment nie należy już do planu. | The edited segment is no longer part of the plan. | U |
| `ManualEntryError_RemovedSegmentMissing` | Usuwany segment nie należy już do planu. | The segment being removed is no longer part of the plan. | U |
| `ManualEntryError_DefaultRestCannotBeRemoved` | Odpoczynek jest domyślnym wypełnieniem luki. | Rest is the default gap allocation and cannot be removed. | U |
| `ManualEntryError_SelectDayFormat` | Pole {0}: wybierz dzień. | Field {0}: select a day. | U |
| `ManualEntryError_EnterTimeFormat` | Pole {0}: wpisz godzinę w formacie HH:MM. | Field {0}: enter a time in HH:MM format. | U/T |
| `ManualEntryError_ApplyFailed` | Nie udało się zastosować wpisu manualnego. Szczegóły zapisano w logu diagnostycznym. | The manual entry could not be applied. Details were written to the diagnostic log. | U |

Wszystkie 11 wartości `ManualEntryError` mapuje się 1:1 na pierwszych
11 kluczy. Błędy lokalnego edytora korzystają z tych samych kluczy, gdy
semantyka jest równa (`InvalidActivity`, `InvalidSegment`,
`IncompleteCoverage`, `OutsideGap`, `OverlappingSegments`), oraz z trzech
kluczy dla stanów specyficznych dla edycji. `{0}` w dwóch formatach pola jest
lokalizowaną etykietą `Common_From` albo `Common_To`.

`ManualEntryValidationException.Error` jest źródłem decyzji prezentera.
Angielskie `exception.Message` z warstwy Application i polskie komunikaty
`InvalidOperationException` z edytora nie mogą być bezpośrednio przypisywane do
`ManualEntryValidationMessage`. Szczegóły techniczne, identyfikatory rekordów
i pełny wyjątek trafiają wyłącznie do diagnostyki. Nieoczekiwane odrzucenie
podczas zastosowania wyniku w silniku używa ogólnego
`ManualEntryError_ApplyFailed`, dzięki czemu angielskie wyjątki Engine nie
przeciekają do polskiego UI.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `MainWindow.xaml:174-245` | Historia, dwie tabele, filtr luk | klucze Historii i luk + ponowne użycie zatwierdzonego katalogu |
| `MainWindow.xaml:850-1048` | modal wpisu manualnego | 20 kluczy modala + wspólne akcje i etykiety |
| `ActivityRecord` wiązany w Historii | `DriverActivity`, `ActivitySource`, `SpecialCondition` przez `ToString()` | Desktop row presenter; istniejące enumy i zapis bez zmian |
| `ActivityGapDtos.cs:20-43` | polskie przyczyny, stany, pomoc i akcja w DTO | presenter Desktop; DTO pozostaje domenowe |
| `MainViewModel.cs:597-638` | tytuły edytora, liczba segmentów, pokrycie | klucze i pluralizer paczki 4 |
| `MainViewModel.cs:659` | licznik nierozliczonych luk | `Gap_HeaderFormat` |
| `MainViewModel.cs:1316-1341` | zakres, długość, kierowca, przyczyna i stan początkowy | formaty paczki 4; dzień → `X-01` |
| `MainViewModel.cs:1466-1510,1569-1581` | podsumowanie i kwalifikacja | 12 formatów podsumowania + 6 wariantów kwalifikacji |
| `MainViewModel.cs:1355-1452,1517-1555` | surowe wyjątki lokalnego edytora | jawne klucze walidacji, bez `exception.Message` |
| `ManualEntryService.cs:31-174` | 11 wartości `ManualEntryError`, angielskie komunikaty | presenter kodu błędu w Desktop; surowa treść tylko do logu |
| `ManualEntryPlanEditor.cs:7-234` | dzień, aktywności i wyjątki edytora | `X-01`, ponowne użycie `Activity_*`, klucze walidacji |

`ManualEntrySegmentCountText` nie może po lokalizacji usuwać polskiego prefiksu
przez `ManualEntryDurationText.Replace("Długość luki: ", string.Empty)`.
M5.2 przekaże do formatu liczby segmentów bezpośrednio surową, sformatowaną
wartość czasu trwania luki; logika nie może parsować tekstu zasobu.

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| `.tacho` | T | format kontraktowy |
| numer karty, identyfikator luki i rekordu | T/O | dane oraz identyfikatory audytowe |
| `S{0}`, numery slotów | T | zwarty identyfikator slotu |
| `HH:MM`, strzałka zakresu, `·`, `✓`, `×`, `—` | T | format czasu i symbole interfejsu |
| kolory aktywności | T | prezentacja niezależna od języka |
| `ResolveGapStatus`, `ManualEntryPersistenceStatus` | T | wewnętrzne sterowanie przepływem |
| treść diagnostyki | D | nie jest tekstem interfejsu |

### Oczekiwane identyczne wartości PL/EN

- `Gap_SlotHeader`;
- `ManualEntry_SegmentCountOneFormat`.

### Nowe oczekiwane duplikaty wartości między kluczami

Paczka 4 dodaje siedem dozwolonych par do pięciu zapisanych po paczce 3.
Globalna lista dla katalogu paczek 1–4 ma zatem 12 pozycji:

| Wartość | Klucze | Decyzja |
|---|---|---|
| PL `TRWA` | `Dashboard_ElapsedLabel`, `GapState_Ongoing` | różne semantyki: czas, który upłynął, oraz trwająca luka |
| PL/EN `KIEROWCA` / `DRIVER` | `Device_DriverFallback`, `ManualEntry_DriverLabel` | wartość zastępcza LCD i etykieta pola modala są odrębnymi rolami |
| EN `BREAK / REST` | `DeviceMenu_BreakOrRest`, `ManualEntry_BreakOrRestAction` | odrębna pozycja menu urządzenia i pełna akcja modala; PL ma różne brzmienie |
| EN `Break / rest` | `Activity_BreakOrRest`, `ManualEntryActivity_BreakOrRest` | osobne klucze zachowują różną zatwierdzoną pisownię PL |
| EN `Mixed` | `ActivitySource_Mixed`, `SpecialCondition_Mixed` | dwa różne enumy domenowe; PL rozróżnia rodzaj gramatyczny |
| EN `TIME` | `ManualEntry_TimeHeader`, `ManualEntry_HourLabel` | czas trwania i godzina są odmiennymi polami; PL je rozróżnia |
| EN `{0} segments · {1}` | `ManualEntry_SegmentCountFewFormat`, `ManualEntry_SegmentCountManyFormat` | EN ma jedną formę mnogą, PL wymaga dwóch |

### Kontrola paczki

- [x] wszystkie literały Historii i modala mają klucz, ponowne użycie albo jawnego właściciela;
- [x] sprawdzono cały katalog 170 kluczy z paczek 1–3 przed utworzeniem nowych nazw;
- [x] wszystkie 6 `DriverActivity`, 6 `ActivitySource`, 4 `SpecialCondition`, 3 `ActivityGapReason` i 2 `ActivityGapState` ma decyzję;
- [x] wszystkie 11 wartości `ManualEntryError` ma jawny komunikat użytkowy;
- [x] wszystkie 6 kombinacji kwalifikacji odpoczynku ma pełny format bez sklejania fragmentów;
- [x] wszystkie formaty mają zgodne zbiory placeholderów PL/EN;
- [x] cztery końcowe separatory spacji mają decyzję przeniesienia z zasobów do XAML w M5.2;
- [x] surowe `ToString()` i `exception.Message` nie należą do docelowej ścieżki UI;
- [x] dane audytowe, enumy, DTO, `.tacho`, SQLite i logika rozliczenia pozostają bez zmian;
- [x] potwierdzenie odrzucenia zmian i `OperationStatus` mają właściciela w UI-09;
- [x] `GameCalendar_DayFormat` ma właściciela w `X-01`.

### Werdykt

**GO — paczka 4 zatwierdzona.** Zamknięta bez pozycji otwartych. Łączny,
wiążący katalog paczek 1–4 zawiera 257 unikalnych nazw i 12 jawnie dozwolonych
par powtórzonych wartości.

## Paczka 5 — X-01: wspólne formatery czasu i terminów

**Zakres:** prezentacja absolutnego czasu gry, nazw dni tygodnia i czterech
semantyk terminu na Dashboardzie, w Historii, wpisie manualnym, Rekompensatach,
Planerze, Raportach Desktop oraz licznikach odpoczynku tygodniowego.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 12 nowych kluczy — 7 nazw dni dla pełnego wariantu prezentacji,
4 nieurządzeniowe prefiksy terminu oraz 1 format etykiety dnia gry. Paczka
ponownie używa 7 `Weekday_Short_*` i 4 `DeviceDeadline_*Prefix` z paczki 3.

Łączny katalog paczek 1–5 zawiera 269 unikalnych nazw. Paczka 5 nie dodaje
żadnej pary powtórzonych wartości, dlatego globalna lista 12 dozwolonych par
pozostaje bez zmian.

### Granica paczki

`X-01` lokalizuje tekst tworzony przez wspólne formatery Desktop. Nie
inwentaryzuje pozostałych etykiet Planera, Raportów ani Rekompensat — należą
odpowiednio do UI-08, UI-06 i UI-05. Pełne nazwy dni w polach wyboru Planera
(`Poniedziałek`, `Wtorek`, ...) są osobną rolą kontrolki i pozostają w UI-08.
Paczka 5 obejmuje natomiast wartości `Pon`, `Wt`, ... używane przez zatwierdzony
pełny format kalendarza M3A.

Wcześniejsze określenie „7 pełnych nazw dni” oznaczało wartości metody
`GameWeekdayNames.Full`, a nie nieskrócone nazwy leksykalne. Nazwy kluczy
`Weekday_Display_*` usuwają tę nieścisłość i zachowują polski UI freeze:
`Pon · Dzień 29 · 00:00`, nie `Poniedziałek · Dzień 29 · 00:00`.

M5.2 doda do wartości `Weekday_Display_*` i `Weekday_Short_*` komentarze
`.resx`, że są to odrębne role różniące się wymaganą pisownią (`Pon` / `PON`,
`Mon` / `MON`). Tłumacz ani narzędzie nie może automatycznie ujednolicić obu
rodzin.

PDF-01 ma osobny katalog `ReportStrings` i własną kontrolę układu dokumentu.
Nie może pobierać `UiStrings` z Desktop. Wspólny pozostaje model liczbowy czasu,
ale tekst PDF zostanie rozpisany w paczce PDF-01.

### Nazwy dni dla pełnego wariantu prezentacji

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Weekday_Display_Monday` | Pon | Mon | P |
| `Weekday_Display_Tuesday` | Wt | Tue | P |
| `Weekday_Display_Wednesday` | Śr | Wed | P |
| `Weekday_Display_Thursday` | Czw | Thu | P |
| `Weekday_Display_Friday` | Pt | Fri | P |
| `Weekday_Display_Saturday` | Sob | Sat | P |
| `Weekday_Display_Sunday` | Ndz | Sun | P |

`GameWeekdayNames.Full` zostaje zastąpiony wyczerpującym presenterem korzystającym
z tych siedmiu kluczy. Nazwa metody może pozostać ze względu na mały zakres
zmiany, ale nie może sugerować ponownego użycia pełnych nazw z listy Planera.
Każda z siedmiu wartości `GameWeekday` ma dokładnie jedno mapowanie, bez
fallbacku `ToString()`.

`GameWeekdayNames.Abbreviated` ponownie używa zatwierdzonych w paczce 3
`Weekday_Short_Monday`–`Weekday_Short_Sunday`. Te wartości zachowują wersaliki
i zasilają format kompaktowy oraz LCD:

```text
PL: PON · D29 · 00:00
EN: MON · D29 · 00:00
```

### Semantyka terminów

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Deadline_CompleteByPrefix` | Ukończ do | Complete by | P |
| `Deadline_StartNoLaterThanPrefix` | Rozpocznij najpóźniej | Start no later than | P |
| `Deadline_CompleteBeforePrefix` | Ukończ przed | Complete before | P |
| `Deadline_AvailableFromPrefix` | Jazda dostępna od | Driving available from | P |

Cztery klucze mapują 1:1 wszystkie wartości `GameDeadlineSemantic`. Dwukropek,
spacja i separatory kalendarza pozostają stałą strukturą formattera. Zasób nie
zawiera końcowej interpunkcji ani odstępu.

Wariant urządzeniowy nie używa tych kluczy. `FormatDevice` nadal mapuje ten sam
enum na cztery zatwierdzone `DeviceDeadline_*Prefix`, ponieważ LCD ma inną rolę,
pisownię i ograniczenie szerokości. `Deadline_StartNoLaterThanPrefix` oraz
`DeviceDeadline_StartNoLaterThanPrefix` nie są duplikatem wartości
(`Rozpocznij najpóźniej` / `START≤`, `Start no later than` / `START≤`).

`Deadline_AvailableFromPrefix` nie ma jeszcze aktywnego konsumenta produkcyjnego,
ale odpowiada istniejącej wartości `GameDeadlineSemantic.AvailableFrom`
i istniejącej gałęzi presentera. Pozostaje wymagany dla wyczerpującego mapowania
oraz planowanego segmentu `CalendarWait`; M5 nie uruchamia nowej funkcji Planera.
Jest to nazwany wyjątek „klucz wyczerpującego pokrycia enuma”, a nie martwy
klucz funkcjonalny.

### Etykieta dnia i formaty złożone

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `GameCalendar_DayFormat` | Dzień {0} | Day {0} | U/T |

`{0}` jest dodatnim numerem dnia wyliczonym przez `GameCalendarResolver`
według istniejącej reguły `floor(GameMinute / 1440) + 1`. Oba języki mają
identyczny zbiór placeholderów. Liczba jest formatowana
`InvariantCulture`; lokalizacja nie zmienia wartości, granicy ani zaokrąglenia.

Formattery składają lokalizowane elementy ze stałymi separatorami:

```text
FormatFull:
PL: Pon · Dzień 29 · 00:00
EN: Mon · Day 29 · 00:00

FormatCompact:
PL: PON · D29 · 00:00
EN: MON · D29 · 00:00

GameClock w UI:
PL: Dzień 29, 00:00
EN: Day 29, 00:00
```

`D{0}`, `HH:MM`, `n/6`, `—/6`, `6/6+`, przecinek, dwukropek, nawiasy,
środkowa kropka i strzałka zakresu pozostają techniczną strukturą prezentacji.
Dla `pl-PL` i `en-GB` kolejność elementów jest wspólna i nie wymaga osobnych
pełnych formatów zdaniowych.

### Rozdzielenie UI od formatów technicznych

`GameClockFormatter` z projektu Core jest używany nie tylko przez UI, lecz także
przez JSON Application, diagnostykę oraz właściwości zgodnościowe encji i DTO.
Nie może czytać bieżącej kultury ani odwoływać się do `UiStrings`; w przeciwnym
razie eksport i log zależałyby od języka procesu.

M5.2 wprowadzi lokalny presenter czasu gry w Desktop, oparty na
`GameCalendar_DayFormat`, i skieruje do niego wszystkie widoczne użycia:

- wiersze Historii i rejestru luk;
- zakres oraz segmenty wpisu manualnego;
- widoki Rekompensat;
- wyniki Planera i Raportów Desktop;
- wartości terminów na Dashboardzie i w nakładkach.

`ActivityRecord` i `ActivityGapListItemDto` zachowują minuty domenowe. Desktop
nie może polegać na polskich `StartGameTimeText`, `EndGameTimeText`,
`ResolvedAtGameTimeText` ani na `GameClockFormatter.Format` jako źródle tekstu
użytkowego. Jest to zgodne z decyzją paczki 4 o lokalnym wierszu prezentacyjnym.

Techniczny JSON tworzony przez `ReportService`, raport diagnostyczny oraz wpisy
logu pozostają poza lokalizacją i zachowują istniejący stabilny format.
`GameClockFormatter.TryParse` również pozostaje niezmiennym parserem technicznym;
M5 nie uzależnia jego gramatyki od aktywnego języka.

Martwe `MainViewModel.GameTimeText` nie ma bindingu w XAML ani innego konsumenta
UI. Nie otrzymuje nowego klucza. M5.2 może pozostawić tę właściwość bez zmian
albo usunąć ją dopiero w osobnej, jawnej zmianie porządkowej — pakiet 5 nie
autoryzuje takiego usunięcia.

### `WeeklyRestWindowFormatter`

Teksty okresu `1/6`–`6/6+` i fallbacki `—/6`, `(—)` pozostają techniczne.
Wariant standardowy składa okres z
`Deadline_StartNoLaterThanPrefix`, `Weekday_Short_*` i `D{0}`. Wariant LCD
składa ten sam okres z `DeviceDeadline_StartNoLaterThanPrefix`,
`Weekday_Short_*` i `D{0}`.

Brak terminu nie tworzy lokalizowanego zdania. Zachowany zostaje dokładny
fallback:

```text
4/6 (—)
—/6 (—)
```

Lokalizacja nie może przeliczać okna sześciu okresów, tworzyć terminu z tekstu
ani zmieniać granicy `WeeklyRestStartDeadlineGameMinute`.

### Mapowanie źródeł i konsumentów

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `GameCalendarFormatter.cs:5-31` | `Pon`–`Ndz`, `PON`–`NDZ` | 7 `Weekday_Display_*` + ponowne użycie 7 `Weekday_Short_*` |
| `GameCalendarFormatter.cs:34-44` | pełny i kompaktowy moment kalendarza | `Weekday_Display_*`, `Weekday_Short_*`, `GameCalendar_DayFormat`; struktura T |
| `GameCalendarFormatter.cs:51-77` | 4 prefiksy pełne i 4 prefiksy LCD | 4 `Deadline_*Prefix` + ponowne użycie 4 `DeviceDeadline_*Prefix` |
| `WeeklyRestWindowFormatter.cs:7-48` | okres `n/6`, termin i fallback | zasoby terminów; okres i fallback T |
| `CompensationPresentation.cs:15-45,142-176` | najbliższy termin, szczegóły i zakresy | wspólny presenter czasu; pozostałe etykiety → UI-05 |
| `JourneyPlannerViewModel.cs:86-93,473-474,676-678` | pełne nazwy opcji i kompaktowe terminy | opcje → UI-08; wyniki czasu → wspólny presenter |
| `ReportsWorkspaceViewModel.cs:556,768-795` | opcje dnia, kompaktowy czas i długie okresy | dzień i czas → wspólny presenter; opis okresu → UI-06 |
| `ManualEntryPlanEditor.cs:7-22` | `Dzień {0}` i zakres segmentu | `GameCalendar_DayFormat` + wspólny presenter czasu |
| `ActivityGapDtos.cs:20-36`, `ActivityRecord.cs:20-21` | polskie teksty czasu w modelach | wartości liczbowe do lokalnego wiersza Desktop; tekst modelu nie trafia do UI |
| `MainViewModel.cs:784-825,2319-2326` | terminy dobowe i tygodniowe LCD | istniejące klucze urządzeniowe; bez nowych kluczy |
| `GameClockFormatter.cs:10-48` | stabilny format i parser Core | bez zależności od kultury; widoczne użycia przejmuje Desktop |
| `ReportService.cs:114,183-184,229-230` | tekst czasu w JSON | T — kontrakt eksportu bez zmian |

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| `GameTime.TotalMinutes`, `GameWeek`, `GameWeekday` | T | dane i enumy domenowe |
| `D{0}`, `HH:MM`, `n/6`, `—/6`, `6/6+` | T | zwarte, językowo neutralne formaty |
| `·`, `:`, `,`, `→`, `–`, `(`, `)` | T | separatory zatwierdzone dla PL i EN |
| treść JSON i diagnostyki | T/D | stabilny eksport i materiał techniczny |
| pełne nazwy opcji dnia w Planerze | U | osobna rola i właściciel UI-08 |
| formaty czasu w PDF | U/T | osobny właściciel PDF-01 i `ReportStrings` |

### Oczekiwane identyczne wartości i duplikaty

Paczka 5 nie dodaje wartości identycznych po obu stronach PL/EN. Żaden z 12
nowych tekstów nie powtarza dokładnie wartości zatwierdzonego katalogu.
Globalna lista 12 dozwolonych par wartości z paczki 4 pozostaje kompletna
i bez zmian.

### Kontrola paczki

- [x] wszystkie 7 wartości `GameWeekday` ma wariant pełnej prezentacji i ponownie użyty wariant kompaktowy;
- [x] wszystkie 4 wartości `GameDeadlineSemantic` ma pełny i urządzeniowy wariant bez fallbacku;
- [x] `GameCalendar_DayFormat` ma zgodny placeholder `{0}` w PL/EN;
- [x] zachowano dokładne polskie formaty M3A i regułę numeracji dnia od 1;
- [x] rozdzielono tytułowe skróty `Pon`–`Ndz` od pełnych nazw opcji Planera;
- [x] wszystkie widoczne użycia `GameClockFormatter.Format` mają drogę do lokalnego presentera Desktop;
- [x] JSON, diagnostyka, parser, enumy i minuty domenowe pozostają niezależne od kultury UI;
- [x] okres `n/6`, fallback i znaki graniczne nie są parsowane z tekstu zasobu;
- [x] sprawdzono katalog 257 kluczy z paczek 1–4; brak nowych duplikatów nazw i wartości;
- [x] paczka nie dodaje funkcji Planera ani nie zmienia reguł terminów;
- [x] EN wymaga kontroli szerokości dla `Start no later than` w standardowym wariancie licznika oraz pełnych tooltipów `Complete before`.

### Punkt kontrolny

- 12 nowych kluczy;
- 269 unikalnych nazw globalnie;
- 0 nowych par powtórzonych wartości;
- 12 dozwolonych par globalnie;
- 7/7 wartości `GameWeekday`;
- 4/4 wartości `GameDeadlineSemantic`;
- 0 zmian w kodzie i XAML.

### Werdykt

**GO — paczka 5 (`X-01`) zatwierdzona.** Zamknięta bez pozycji otwartych.
Łączny, wiążący katalog paczek 1–5 zawiera 269 unikalnych nazw i 12 jawnie
dozwolonych par powtórzonych wartości. Zależności kompletności Dashboardu
oraz listy dni w modalu wpisu manualnego od `X-01` są zamknięte.

## Paczka 6 — UI-04: kraje i kody tachografowe

**Zakres:** 249 lokalizowanych nazw krajów, ładowanie i sortowanie katalogu,
wyszukiwanie po nazwie i kodach oraz modal wyboru kraju przy wkładaniu
i wyjmowaniu karty.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 260 nowych kluczy — 249 `Country_*` i 11 tekstów modala karty.
Łączny katalog paczek 1–6 zawiera 529 unikalnych nazw.

Paczka nie dodaje duplikatów wartości między różnymi kluczami. Globalna lista
12 dozwolonych par z paczki 4 pozostaje bez zmian. Wśród 249 nazw krajów
75 wartości jest świadomie identycznych w PL i EN.

### Granica paczki

Stabilny katalog `Countries.iso3166-1.json` pozostaje jedynym źródłem kodu
`ISO 3166-1 alpha-2`, klucza nazwy, kodu tachografowego, kodu numerycznego
i regionu fallback. Język zmienia wyłącznie `DisplayName` i kolejność
alfabetyczną listy. Nie zmienia:

- wartości zapisywanej w historii i stanie urządzenia — nadal ISO alpha-2;
- kodów tachografowych, w tym `EUR` i `WLD`;
- kodów numerycznych 0–255;
- działania `ResolveLegacyCode`;
- identyfikatorów `Country_*`.

Komunikaty sukcesu, walidacji i odrzuceń przypisywane do `OperationStatus`
pozostają w UI-09. Paczka 6 obejmuje treść samego modala kraju. Wpisy
diagnostyczne i wyjątki integralności katalogu są D; dzięki decyzji paczki 1
nie mogą trafić surowo do komunikatu błędu startu.

Menu `KRAJE` w wirtualnym urządzeniu, jego tytuły oraz formaty `START: {0}`
i `KONIEC: {0}` mają klucze z paczki 3. Paczka 6 dostarcza dane krajów i regułę
sortowania, ale nie tworzy drugich kluczy LCD.

### Modal wyboru kraju

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `CardDialog_SelectCountryPrompt` | WYBIERZ KRAJ | SELECT COUNTRY | U |
| `CardDialog_AvailableCardLabel` | Dostępna karta | Available card | U |
| `CardDialog_StartCountryLabel` | Kraj rozpoczęcia | Start country | U |
| `CardDialog_EndCountryLabel` | Kraj zakończenia | End country | U |
| `CardDialog_CountryPlaceholder` | Wybierz kraj... | Select a country... | U |
| `CardDialog_ConfirmAction` | POTWIERDŹ | CONFIRM | U |
| `CardDialog_InsertTitleFormat` | WŁOŻENIE KARTY - CZYTNIK {0} | CARD INSERTION - READER {0} | U/T |
| `CardDialog_InsertMessage` | Potwierdź kierowcę i kraj rozpoczęcia zmiany. Karta zostanie przypisana do kierowcy prowadzącego. | Confirm the driver and the country where the shift starts. The card will be assigned to the active driver. | U |
| `CardDialog_EjectTitleFormat` | WYJĘCIE KARTY - CZYTNIK {0} | CARD EJECTION - READER {0} | U/T |
| `CardDialog_EjectMessage` | Wybierz kraj zakończenia zmiany. Dane zostaną zapisane przed wysunięciem karty. | Select the country where the shift ends. The data will be saved before the card is ejected. | U |
| `CardDialog_DriverCardLabel` | KARTA KIEROWCY | DRIVER CARD | U |

`{0}` w dwóch tytułach oznacza numer czytnika i ma zgodny zbiór placeholderów
PL/EN. `ANULUJ` ponownie używa `Common_CancelAction`. Techniczne `OK` pozostaje
oddzielone od `CardDialog_ConfirmAction`; dwie spacje pomiędzy etykietą i `OK`
są strukturą XAML, nie częścią zasobu. Tak samo numer slotu i odstęp przed
`CardDialog_DriverCardLabel` pozostają strukturą prezentacyjną.

Nazwa profilu i numer karty są danymi użytkownika O. `CountryOption.DisplayText`
zachowuje wspólny format techniczny `{ISO} — {DisplayName}` i nie potrzebuje
osobnego klucza.

### Katalog nazw krajów PL/EN

Źródłem nazw jest Unicode CLDR, zgodnie z metadanymi istniejącego pliku PL.
M5.2 doda `Resources/CountryNames.en-GB.json` z dokładnie tym samym zbiorem
249 kluczy co `CountryNames.pl.json`. Nazwy EN są wariantem `en-GB`;
nie są kopiowane z pola ISO ani tłumaczone w locie. Wartości zatwierdzone
w poniższej tabeli są wiążącym snapshotem dla beta.12; późniejsza aktualizacja
CLDR wymaga osobnej decyzji i nie może wejść automatycznie podczas M5.2.

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Country_AD` | Andora | Andorra | U |
| `Country_AE` | Zjednoczone Emiraty Arabskie | United Arab Emirates | U |
| `Country_AF` | Afganistan | Afghanistan | U |
| `Country_AG` | Antigua i Barbuda | Antigua & Barbuda | U |
| `Country_AI` | Anguilla | Anguilla | U |
| `Country_AL` | Albania | Albania | U |
| `Country_AM` | Armenia | Armenia | U |
| `Country_AO` | Angola | Angola | U |
| `Country_AQ` | Antarktyda | Antarctica | U |
| `Country_AR` | Argentyna | Argentina | U |
| `Country_AS` | Samoa Amerykańskie | American Samoa | U |
| `Country_AT` | Austria | Austria | U |
| `Country_AU` | Australia | Australia | U |
| `Country_AW` | Aruba | Aruba | U |
| `Country_AX` | Wyspy Alandzkie | Åland Islands | U |
| `Country_AZ` | Azerbejdżan | Azerbaijan | U |
| `Country_BA` | Bośnia i Hercegowina | Bosnia & Herzegovina | U |
| `Country_BB` | Barbados | Barbados | U |
| `Country_BD` | Bangladesz | Bangladesh | U |
| `Country_BE` | Belgia | Belgium | U |
| `Country_BF` | Burkina Faso | Burkina Faso | U |
| `Country_BG` | Bułgaria | Bulgaria | U |
| `Country_BH` | Bahrajn | Bahrain | U |
| `Country_BI` | Burundi | Burundi | U |
| `Country_BJ` | Benin | Benin | U |
| `Country_BL` | Saint-Barthélemy | St Barthélemy | U |
| `Country_BM` | Bermudy | Bermuda | U |
| `Country_BN` | Brunei | Brunei | U |
| `Country_BO` | Boliwia | Bolivia | U |
| `Country_BQ` | Niderlandy Karaibskie | Caribbean Netherlands | U |
| `Country_BR` | Brazylia | Brazil | U |
| `Country_BS` | Bahamy | Bahamas | U |
| `Country_BT` | Bhutan | Bhutan | U |
| `Country_BV` | Wyspa Bouveta | Bouvet Island | U |
| `Country_BW` | Botswana | Botswana | U |
| `Country_BY` | Białoruś | Belarus | U |
| `Country_BZ` | Belize | Belize | U |
| `Country_CA` | Kanada | Canada | U |
| `Country_CC` | Wyspy Kokosowe | Cocos (Keeling) Islands | U |
| `Country_CD` | Demokratyczna Republika Konga | Congo - Kinshasa | U |
| `Country_CF` | Republika Środkowoafrykańska | Central African Republic | U |
| `Country_CG` | Kongo | Congo - Brazzaville | U |
| `Country_CH` | Szwajcaria | Switzerland | U |
| `Country_CI` | Côte d’Ivoire | Côte d’Ivoire | U |
| `Country_CK` | Wyspy Cooka | Cook Islands | U |
| `Country_CL` | Chile | Chile | U |
| `Country_CM` | Kamerun | Cameroon | U |
| `Country_CN` | Chiny | China | U |
| `Country_CO` | Kolumbia | Colombia | U |
| `Country_CR` | Kostaryka | Costa Rica | U |
| `Country_CU` | Kuba | Cuba | U |
| `Country_CV` | Republika Zielonego Przylądka | Cape Verde | U |
| `Country_CW` | Curaçao | Curaçao | U |
| `Country_CX` | Wyspa Bożego Narodzenia | Christmas Island | U |
| `Country_CY` | Cypr | Cyprus | U |
| `Country_CZ` | Czechy | Czechia | U |
| `Country_DE` | Niemcy | Germany | U |
| `Country_DJ` | Dżibuti | Djibouti | U |
| `Country_DK` | Dania | Denmark | U |
| `Country_DM` | Dominika | Dominica | U |
| `Country_DO` | Dominikana | Dominican Republic | U |
| `Country_DZ` | Algieria | Algeria | U |
| `Country_EC` | Ekwador | Ecuador | U |
| `Country_EE` | Estonia | Estonia | U |
| `Country_EG` | Egipt | Egypt | U |
| `Country_EH` | Sahara Zachodnia | Western Sahara | U |
| `Country_ER` | Erytrea | Eritrea | U |
| `Country_ES` | Hiszpania | Spain | U |
| `Country_ET` | Etiopia | Ethiopia | U |
| `Country_FI` | Finlandia | Finland | U |
| `Country_FJ` | Fidżi | Fiji | U |
| `Country_FK` | Falklandy | Falkland Islands | U |
| `Country_FM` | Mikronezja | Micronesia | U |
| `Country_FO` | Wyspy Owcze | Faroe Islands | U |
| `Country_FR` | Francja | France | U |
| `Country_GA` | Gabon | Gabon | U |
| `Country_GB` | Wielka Brytania | United Kingdom | U |
| `Country_GD` | Grenada | Grenada | U |
| `Country_GE` | Gruzja | Georgia | U |
| `Country_GF` | Gujana Francuska | French Guiana | U |
| `Country_GG` | Guernsey | Guernsey | U |
| `Country_GH` | Ghana | Ghana | U |
| `Country_GI` | Gibraltar | Gibraltar | U |
| `Country_GL` | Grenlandia | Greenland | U |
| `Country_GM` | Gambia | Gambia | U |
| `Country_GN` | Gwinea | Guinea | U |
| `Country_GP` | Gwadelupa | Guadeloupe | U |
| `Country_GQ` | Gwinea Równikowa | Equatorial Guinea | U |
| `Country_GR` | Grecja | Greece | U |
| `Country_GS` | Georgia Południowa i Sandwich Południowy | South Georgia & South Sandwich Islands | U |
| `Country_GT` | Gwatemala | Guatemala | U |
| `Country_GU` | Guam | Guam | U |
| `Country_GW` | Gwinea Bissau | Guinea-Bissau | U |
| `Country_GY` | Gujana | Guyana | U |
| `Country_HK` | SRA Hongkong (Chiny) | Hong Kong SAR China | U |
| `Country_HM` | Wyspy Heard i McDonalda | Heard & McDonald Islands | U |
| `Country_HN` | Honduras | Honduras | U |
| `Country_HR` | Chorwacja | Croatia | U |
| `Country_HT` | Haiti | Haiti | U |
| `Country_HU` | Węgry | Hungary | U |
| `Country_ID` | Indonezja | Indonesia | U |
| `Country_IE` | Irlandia | Ireland | U |
| `Country_IL` | Izrael | Israel | U |
| `Country_IM` | Wyspa Man | Isle of Man | U |
| `Country_IN` | Indie | India | U |
| `Country_IO` | Brytyjskie Terytorium Oceanu Indyjskiego | British Indian Ocean Territory | U |
| `Country_IQ` | Irak | Iraq | U |
| `Country_IR` | Iran | Iran | U |
| `Country_IS` | Islandia | Iceland | U |
| `Country_IT` | Włochy | Italy | U |
| `Country_JE` | Jersey | Jersey | U |
| `Country_JM` | Jamajka | Jamaica | U |
| `Country_JO` | Jordania | Jordan | U |
| `Country_JP` | Japonia | Japan | U |
| `Country_KE` | Kenia | Kenya | U |
| `Country_KG` | Kirgistan | Kyrgyzstan | U |
| `Country_KH` | Kambodża | Cambodia | U |
| `Country_KI` | Kiribati | Kiribati | U |
| `Country_KM` | Komory | Comoros | U |
| `Country_KN` | Saint Kitts i Nevis | St Kitts & Nevis | U |
| `Country_KP` | Korea Północna | North Korea | U |
| `Country_KR` | Korea Południowa | South Korea | U |
| `Country_KW` | Kuwejt | Kuwait | U |
| `Country_KY` | Kajmany | Cayman Islands | U |
| `Country_KZ` | Kazachstan | Kazakhstan | U |
| `Country_LA` | Laos | Laos | U |
| `Country_LB` | Liban | Lebanon | U |
| `Country_LC` | Saint Lucia | St Lucia | U |
| `Country_LI` | Liechtenstein | Liechtenstein | U |
| `Country_LK` | Sri Lanka | Sri Lanka | U |
| `Country_LR` | Liberia | Liberia | U |
| `Country_LS` | Lesotho | Lesotho | U |
| `Country_LT` | Litwa | Lithuania | U |
| `Country_LU` | Luksemburg | Luxembourg | U |
| `Country_LV` | Łotwa | Latvia | U |
| `Country_LY` | Libia | Libya | U |
| `Country_MA` | Maroko | Morocco | U |
| `Country_MC` | Monako | Monaco | U |
| `Country_MD` | Mołdawia | Moldova | U |
| `Country_ME` | Czarnogóra | Montenegro | U |
| `Country_MF` | Saint-Martin | St Martin | U |
| `Country_MG` | Madagaskar | Madagascar | U |
| `Country_MH` | Wyspy Marshalla | Marshall Islands | U |
| `Country_MK` | Macedonia Północna | North Macedonia | U |
| `Country_ML` | Mali | Mali | U |
| `Country_MM` | Mjanma (Birma) | Myanmar (Burma) | U |
| `Country_MN` | Mongolia | Mongolia | U |
| `Country_MO` | SRA Makau (Chiny) | Macao SAR China | U |
| `Country_MP` | Mariany Północne | Northern Mariana Islands | U |
| `Country_MQ` | Martynika | Martinique | U |
| `Country_MR` | Mauretania | Mauritania | U |
| `Country_MS` | Montserrat | Montserrat | U |
| `Country_MT` | Malta | Malta | U |
| `Country_MU` | Mauritius | Mauritius | U |
| `Country_MV` | Malediwy | Maldives | U |
| `Country_MW` | Malawi | Malawi | U |
| `Country_MX` | Meksyk | Mexico | U |
| `Country_MY` | Malezja | Malaysia | U |
| `Country_MZ` | Mozambik | Mozambique | U |
| `Country_NA` | Namibia | Namibia | U |
| `Country_NC` | Nowa Kaledonia | New Caledonia | U |
| `Country_NE` | Niger | Niger | U |
| `Country_NF` | Norfolk | Norfolk Island | U |
| `Country_NG` | Nigeria | Nigeria | U |
| `Country_NI` | Nikaragua | Nicaragua | U |
| `Country_NL` | Holandia | Netherlands | U |
| `Country_NO` | Norwegia | Norway | U |
| `Country_NP` | Nepal | Nepal | U |
| `Country_NR` | Nauru | Nauru | U |
| `Country_NU` | Niue | Niue | U |
| `Country_NZ` | Nowa Zelandia | New Zealand | U |
| `Country_OM` | Oman | Oman | U |
| `Country_PA` | Panama | Panama | U |
| `Country_PE` | Peru | Peru | U |
| `Country_PF` | Polinezja Francuska | French Polynesia | U |
| `Country_PG` | Papua-Nowa Gwinea | Papua New Guinea | U |
| `Country_PH` | Filipiny | Philippines | U |
| `Country_PK` | Pakistan | Pakistan | U |
| `Country_PL` | Polska | Poland | U |
| `Country_PM` | Saint-Pierre i Miquelon | St Pierre & Miquelon | U |
| `Country_PN` | Pitcairn | Pitcairn Islands | U |
| `Country_PR` | Portoryko | Puerto Rico | U |
| `Country_PS` | Terytoria Palestyńskie | Palestinian Territories | U |
| `Country_PT` | Portugalia | Portugal | U |
| `Country_PW` | Palau | Palau | U |
| `Country_PY` | Paragwaj | Paraguay | U |
| `Country_QA` | Katar | Qatar | U |
| `Country_RE` | Reunion | Réunion | U |
| `Country_RO` | Rumunia | Romania | U |
| `Country_RS` | Serbia | Serbia | U |
| `Country_RU` | Rosja | Russia | U |
| `Country_RW` | Rwanda | Rwanda | U |
| `Country_SA` | Arabia Saudyjska | Saudi Arabia | U |
| `Country_SB` | Wyspy Salomona | Solomon Islands | U |
| `Country_SC` | Seszele | Seychelles | U |
| `Country_SD` | Sudan | Sudan | U |
| `Country_SE` | Szwecja | Sweden | U |
| `Country_SG` | Singapur | Singapore | U |
| `Country_SH` | Wyspa Świętej Heleny | St Helena | U |
| `Country_SI` | Słowenia | Slovenia | U |
| `Country_SJ` | Svalbard i Jan Mayen | Svalbard & Jan Mayen | U |
| `Country_SK` | Słowacja | Slovakia | U |
| `Country_SL` | Sierra Leone | Sierra Leone | U |
| `Country_SM` | San Marino | San Marino | U |
| `Country_SN` | Senegal | Senegal | U |
| `Country_SO` | Somalia | Somalia | U |
| `Country_SR` | Surinam | Suriname | U |
| `Country_SS` | Sudan Południowy | South Sudan | U |
| `Country_ST` | Wyspy Świętego Tomasza i Książęca | São Tomé & Príncipe | U |
| `Country_SV` | Salwador | El Salvador | U |
| `Country_SX` | Sint Maarten | Sint Maarten | U |
| `Country_SY` | Syria | Syria | U |
| `Country_SZ` | Eswatini | Eswatini | U |
| `Country_TC` | Turks i Caicos | Turks & Caicos Islands | U |
| `Country_TD` | Czad | Chad | U |
| `Country_TF` | Francuskie Terytoria Południowe i Antarktyczne | French Southern Territories | U |
| `Country_TG` | Togo | Togo | U |
| `Country_TH` | Tajlandia | Thailand | U |
| `Country_TJ` | Tadżykistan | Tajikistan | U |
| `Country_TK` | Tokelau | Tokelau | U |
| `Country_TL` | Timor Wschodni | Timor-Leste | U |
| `Country_TM` | Turkmenistan | Turkmenistan | U |
| `Country_TN` | Tunezja | Tunisia | U |
| `Country_TO` | Tonga | Tonga | U |
| `Country_TR` | Turcja | Türkiye | U |
| `Country_TT` | Trynidad i Tobago | Trinidad & Tobago | U |
| `Country_TV` | Tuvalu | Tuvalu | U |
| `Country_TW` | Tajwan | Taiwan | U |
| `Country_TZ` | Tanzania | Tanzania | U |
| `Country_UA` | Ukraina | Ukraine | U |
| `Country_UG` | Uganda | Uganda | U |
| `Country_UM` | Dalekie Wyspy Mniejsze Stanów Zjednoczonych | US Outlying Islands | U |
| `Country_US` | Stany Zjednoczone | United States | U |
| `Country_UY` | Urugwaj | Uruguay | U |
| `Country_UZ` | Uzbekistan | Uzbekistan | U |
| `Country_VA` | Watykan | Vatican City | U |
| `Country_VC` | Saint Vincent i Grenadyny | St Vincent & the Grenadines | U |
| `Country_VE` | Wenezuela | Venezuela | U |
| `Country_VG` | Brytyjskie Wyspy Dziewicze | British Virgin Islands | U |
| `Country_VI` | Wyspy Dziewicze Stanów Zjednoczonych | US Virgin Islands | U |
| `Country_VN` | Wietnam | Vietnam | U |
| `Country_VU` | Vanuatu | Vanuatu | U |
| `Country_WF` | Wallis i Futuna | Wallis & Futuna | U |
| `Country_WS` | Samoa | Samoa | U |
| `Country_YE` | Jemen | Yemen | U |
| `Country_YT` | Majotta | Mayotte | U |
| `Country_ZA` | Republika Południowej Afryki | South Africa | U |
| `Country_ZM` | Zambia | Zambia | U |
| `Country_ZW` | Zimbabwe | Zimbabwe | U |

### Kontrakt plików językowych

M5.2 zachowuje `Data/Countries.iso3166-1.json` bez zmian i ładuje osobny plik
nazw według wybranego języka:

| Kultura UI | Zasób nazw |
|---|---|
| `pl-PL` | istniejący `Resources/CountryNames.pl.json` |
| `en-GB` | nowy `Resources/CountryNames.en-GB.json` |

Oba pliki nazw muszą mieć `schemaVersion=1`, deklarację kultury i dokładnie
249 niepustych wartości. Zbiory kluczy obu plików oraz pola `nameKey`
w katalogu ISO muszą być identyczne. Brak, nadmiar, duplikat albo nieznana
kultura jest błędem integralności przy starcie; nie wolno mieszać języków
przez fallback pojedynczej nazwy.

M5.2 doda osobny test parzystości magazynu krajów. Test ma porównać trzy zbiory:
249 pól `nameKey` z `Countries.iso3166-1.json`, 249 kluczy
`CountryNames.pl.json` i 249 kluczy `CountryNames.en-GB.json`, a następnie
potwierdzić brak wartości pustych. Ogólny test parzystości `.resx` nie obejmuje
`Country_*` i nie zastępuje tej kontroli.

Bazowy wybór kultury następuje przed pierwszym odwołaniem do statycznego
`CountryCatalog.Options`. MVP przełącza język po restarcie, więc jedna
załadowana lista na proces pozostaje poprawna.

### Sortowanie i wyszukiwanie

Obecne sortowanie jest na stałe oparte na `pl-PL`. M5.2 użyje czystej kolacji
`CompareInfo` wybranej kultury UI, bez `IgnoreNonSpace`, a przy rzeczywistym
remisie stabilnego ISO alpha-2. Diakrytyka musi uczestniczyć w naturalnej
kolejności właściwej dla `pl-PL` albo `en-GB`. Lista PL i EN może mieć inną
kolejność; to oczekiwany skutek lokalizacji, nie zmiana danych.

Wyszukiwanie w `CountryComboBox_PreviewTextInput` zachowuje kolejność kryteriów:

1. dokładny ISO;
2. dokładny nieambiguouszny kod tachografowy;
3. prefiks ISO;
4. prefiks lokalizowanej nazwy;
5. prefiks nieambiguousznego kodu tachografowego.

Porównanie nazwy korzysta z tej samej jawnej kultury co sortowanie, ale dla
dopasowania używa `IgnoreCase | IgnoreNonSpace`. Dzięki temu wpisanie `Wlochy`
może znaleźć `Włochy`, nie zaburzając kolejności sortowania. `EUR` i `WLD`
nadal nie wybierają kraju, ponieważ nie identyfikują jednej pozycji. Zmiana
języka nie może wpływać na przywracanie ostatniego kraju — zapis i lookup używają
ISO.

### Oczekiwane identyczne wartości PL/EN

Dokładnie 75 nazw jest identycznych w obu plikach:

`Country_AI`, `Country_AL`, `Country_AM`, `Country_AO`, `Country_AT`,
`Country_AU`, `Country_AW`, `Country_BB`, `Country_BF`, `Country_BI`,
`Country_BJ`, `Country_BN`, `Country_BT`, `Country_BW`, `Country_BZ`,
`Country_CI`, `Country_CL`, `Country_CW`, `Country_EE`, `Country_GA`,
`Country_GD`, `Country_GG`, `Country_GH`, `Country_GI`, `Country_GM`,
`Country_GU`, `Country_HN`, `Country_HT`, `Country_IR`, `Country_JE`,
`Country_KI`, `Country_LA`, `Country_LI`, `Country_LK`, `Country_LR`,
`Country_LS`, `Country_ML`, `Country_MN`, `Country_MS`, `Country_MT`,
`Country_MU`, `Country_MW`, `Country_NA`, `Country_NE`, `Country_NG`,
`Country_NP`, `Country_NR`, `Country_NU`, `Country_OM`, `Country_PA`,
`Country_PE`, `Country_PK`, `Country_PW`, `Country_RS`, `Country_RW`,
`Country_SD`, `Country_SL`, `Country_SM`, `Country_SN`, `Country_SO`,
`Country_SX`, `Country_SY`, `Country_SZ`, `Country_TG`, `Country_TK`,
`Country_TM`, `Country_TO`, `Country_TV`, `Country_TZ`, `Country_UG`,
`Country_UZ`, `Country_VU`, `Country_WS`, `Country_ZM`, `Country_ZW`.

To identyczność wartości tego samego klucza między językami, a nie duplikaty
między różnymi kluczami. Nie zwiększa listy 12 dozwolonych par.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `Countries.iso3166-1.json` | 249 rekordów technicznych i 249 `nameKey` | T — bez zmian |
| `CountryNames.pl.json` | 249 unikalnych nazw PL | 249 `Country_*` |
| nowy `CountryNames.en-GB.json` | 249 nazw EN | ten sam zbiór `Country_*` |
| `CountryCatalog.cs:7-18` | `DisplayText = ISO — nazwa` | ISO T + zlokalizowane `DisplayName`; struktura bez zasobu |
| `CountryCatalog.cs:49-108` | ładowanie PL i sortowanie `pl-PL` | wybór pliku i `CompareInfo` według kultury UI |
| `CountryCatalog.cs:110-128` | polskie wyjątki integralności | D; pełny wyjątek tylko do logu |
| `MainWindow.xaml.cs:79-118` | wyszukiwanie ISO, kodu i nazwy | jawna kultura wybranego języka; reguły bez zmian |
| `MainWindow.xaml:804-844` | modal wyboru kraju | 4 nowe klucze statyczne + ponowne użycie `Common_CancelAction` |
| `MainViewModel.cs:515-538` | etykieta kraju i wiersz karty | 3 klucze modala + wartości T/O |
| `MainViewModel.cs:1086-1130` | tytuły i opisy wkładania/wyjmowania | 4 klucze modala |
| `MainViewModel.cs:1132-1212,1695` | komunikaty `OperationStatus` | UI-09; ISO i kod tachografowy jako placeholdery T |
| `MainViewModel.cs:1674-1695,1739-1797` | menu krajów LCD | klucze paczki 3 + techniczne ISO/kody |
| `MainViewModel.cs:1850-1989` | migracja, zapis i ostatnie kraje | wyłącznie ISO i kody; bez lokalizowanych nazw |

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| ISO alpha-2 i `Country_*` | T | stabilne identyfikatory |
| kod i kod numeryczny tachografu | T | kontrakt urządzenia |
| `EUR`, `WLD`, `regionFallback` | T | techniczne fallbacki katalogu |
| schema, data snapshotu i nazwy pól JSON | T | kontrakt danych referencyjnych |
| nazwa profilu, numer karty | O | dane użytkownika |
| `OK`, numer czytnika, `—` i odstępy układu | T | struktura prezentacyjna |
| wyjątki integralności i logi operacji karty | D | diagnostyka |

### Ryzyka i kontrola wizualna

Lista ma szerokość 260 px. Test PL/EN musi objąć co najmniej:

- `TF` — `Francuskie Terytoria Południowe i Antarktyczne` (46 znaków);
- `UM` — `Dalekie Wyspy Mniejsze Stanów Zjednoczonych` (43);
- `GS` — `South Georgia & South Sandwich Islands` (38);
- `IO` — `British Indian Ocean Territory` (30);
- oba tytuły modala dla czytników 1 i 2.

UI freeze nie zezwala na zmianę przepływu ani modelu wyboru. Dopuszczalne są
`TextTrimming`, tooltip lub zwiększenie szerokości rozwijanej części bez zmiany
szerokości samego pola, jeżeli test EN wykaże obcięcie uniemożliwiające
rozróżnienie nazw.

### Kontrola paczki

- [x] katalog ISO ma 249 rekordów i 249 unikalnych `nameKey`;
- [x] katalog PL ma 249 niepustych, unikalnych wartości;
- [x] katalog EN ma 249 niepustych, unikalnych wartości;
- [x] zbiory kluczy ISO, PL i EN są identyczne;
- [x] 12 tekstów modala ma decyzję: 11 nowych kluczy i ponowne użycie `Common_CancelAction`;
- [x] oba formaty z `{0}` mają zgodne placeholdery PL/EN;
- [x] 75 identycznych nazw PL/EN jest jawnie wymienionych;
- [x] 260 nowych nazw kluczy nie koliduje z katalogiem paczek 1–5;
- [x] brak nowych duplikatów wartości między różnymi kluczami;
- [x] zapis, migracja i przywracanie używają ISO, nie `DisplayName`;
- [x] kody tachografowe i numeryczne pozostają bez zmian;
- [x] sortowanie i wyszukiwanie mają jawną kulturę PL/EN;
- [x] sortowanie zachowuje diakrytykę, a wyszukiwanie używa `IgnoreNonSpace`;
- [x] osobny test porównuje 249 `nameKey` ISO z oboma plikami JSON nazw;
- [x] `OperationStatus` i surowe wyjątki mają właściciela poza paczką;
- [x] brak zmian w kodzie, XAML i JSON.

### Punkt kontrolny przed GO

- 249 kluczy nazw krajów;
- 11 kluczy modala;
- 260 nowych kluczy;
- 529 unikalnych nazw globalnie;
- 75 identycznych wartości PL/EN;
- 0 nowych par powtórzonych wartości;
- 12 dozwolonych par globalnie;
- 0 zmian wykonawczych.

### Werdykt

**GO — paczka 6 zatwierdzona.** Zamknięta bez pozycji otwartych. Łączny,
wiążący katalog paczek 1–6 zawiera 529 unikalnych nazw i 12 jawnie dozwolonych
par powtórzonych wartości.

## Paczka 7 — UI-05: Rekompensaty

**Zakres:** ekran pełnego śladu rekompensat, wybór sposobu alokacji odpoczynku,
szczegóły zobowiązań oraz presentery `RestAllocationPurpose`
i `WeeklyRestCompensationStatusDto`.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 32 nowe klucze — 14 etykiet ekranu, 2 warianty nagłówka,
11 wartości karty wyboru alokacji oraz 5 wartości szczegółów zobowiązania.
Łączny katalog paczek 1–7 zawiera 561 unikalnych nazw.

Paczka dodaje jedną dozwoloną parę powtórzonej wartości EN (`PAID LATE`).
Globalna lista rośnie z 12 do 13 pozycji.

### Granica paczki

Paczka obejmuje zakładkę `MainWindow.xaml:246-351`,
`CompensationPresentation.cs` i nagłówek szczegółów w `MainViewModel`.
Nie obejmuje:

- skrótu rekompensat na Dashboardzie i nakładkach — klucze paczki 2;
- skrótu LCD — klucze paczki 3;
- wspólnych terminów i czasu gry — paczka 5 (`X-01`);
- tabel rekompensat w obszarze Raporty — UI-06;
- eksportu technicznego CSV — kontrakt bez lokalizacji;
- komunikatów `OperationStatus` i treści wyjątków — UI-09;
- raportu PDF — PDF-01.

Identyfikatory zobowiązania, bloku źródłowego, bloku spłacającego i kandydata
są techniczne. Mogą być skracane wyłącznie wizualnie; kopiowanie zawsze zwraca
pełną wartość. Lokalizacja nie zmienia decyzji alokacji ani jej audytowego śladu.

### Etykiety ekranu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Compensation_Title` | REKOMPENSATY ODPOCZYNKU TYGODNIOWEGO | WEEKLY REST COMPENSATION | U |
| `Compensation_Description` | Pełny ślad z historii wybranej karty. Identyfikatory są skrócone wyłącznie na ekranie. | Full trace from the selected card's history. Identifiers are shortened on screen only. | U |
| `Compensation_RefreshDetailsAction` | ODŚWIEŻ SZCZEGÓŁY | REFRESH DETAILS | U |
| `RestAllocation_Title` | WYBIERZ SPOSÓB ROZLICZENIA ODPOCZYNKU | CHOOSE HOW TO ALLOCATE THE REST PERIOD | U |
| `RestAllocation_Warning` | Do czasu wyboru Planer i pełna ocena raportowa pozostają niewiarygodne. | Until a choice is made, the Journey Planner and the full report assessment remain unreliable. | U |
| `RestAllocation_SelectVariantAction` | WYBIERZ TEN WARIANT | SELECT THIS OPTION | U |
| `Compensation_ObligationIdHeader` | IDENTYFIKATOR ZOBOWIĄZANIA | OBLIGATION IDENTIFIER | U |
| `Compensation_SourceRestEndHeader` | ŹRÓDŁOWY ODPOCZYNEK · KONIEC | SOURCE REST PERIOD · END | U |
| `Compensation_OriginalDebtHeader` | PEŁNY DŁUG | ORIGINAL DEBT | U |
| `Compensation_ReductionWeekHeader` | TYDZIEŃ SKRÓCENIA | REDUCTION WEEK | U |
| `Compensation_ExclusiveDeadlineHeader` | TERMIN WYŁĄCZNY | EXCLUSIVE DEADLINE | U |
| `Compensation_PaymentBlockHeader` | BLOK SPŁACAJĄCY | PAYMENT BLOCK | U |
| `Compensation_PaymentRangeHeader` | ZAKRES MINUT SPŁATY | PAYMENT MINUTE RANGE | U |
| `Compensation_SettledAtLabel` | Moment spłaty (SettledAt) | Settlement time (SettledAt) | U/T |

Nawigacja ponownie używa `Navigation_Compensations`, trzy przyciski kopiowania
używają `Common_CopyAction`, a `POZOSTAŁO` używa
`Dashboard_RemainingLabel`. Badge `KARTA` ponownie używa
`History_CardHeader`: mimo historycznego prefiksu wartość i semantyka karty są
te same, a reguła paczki 1 nakazuje sprawdzanie całego katalogu, nie tylko
`Common_*`. Nie powstają duplikaty tych etykiet.

### Nagłówek szczegółów

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Compensation_NoObligations` | Brak zobowiązań w bieżących projekcjach kart. | No obligations in the current card projections. | U |
| `Compensation_DetailsHeaderFormat` | Zobowiązania: {0} · otwarte: {1} | Obligations: {0} · open: {1} | U/T |

`{0}` oznacza liczbę wszystkich zobowiązań, a `{1}` liczbę otwartych.
Oba języki mają identyczny zbiór placeholderów. Format używa etykiet liczników,
więc nie wymaga odmiany rzeczownika zależnej od liczby.

### Karta wyboru alokacji odpoczynku

#### Cel alokacji

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `RestAllocationPurpose_DailyRestWithCompensation` | DOBOWY + REKOMPENSATA | DAILY REST + COMPENSATION | P |
| `RestAllocationPurpose_ReducedWeeklyRestOnly` | SKRÓCONY TYGODNIOWY | REDUCED WEEKLY REST | P |
| `RestAllocationPurpose_ReducedWeeklyRestWithCompensation` | SKRÓCONY TYGODNIOWY + REKOMPENSATA | REDUCED WEEKLY REST + COMPENSATION | P |
| `RestAllocationPurpose_RegularWeeklyRestOnly` | REGULARNY TYGODNIOWY | REGULAR WEEKLY REST | P |
| `RestAllocationPurpose_RegularWeeklyRestWithCompensation` | REGULARNY TYGODNIOWY + REKOMPENSATA | REGULAR WEEKLY REST + COMPENSATION | P |

Wszystkie pięć wartości `RestAllocationPurpose` ma mapowanie 1:1.
`PurposeLabel` nie może kończyć się fallbackiem
`purpose.ToString().ToUpperInvariant()`. Nieznana wartość jest błędem
programistycznym i nie trafia do UI.

#### Wynik alokacji

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `RestAllocation_OldDebtPaymentFormat` | Stary dług: spłata {0} | Old debt: payment {0} | U/T |
| `RestAllocation_OldDebtNoPayment` | Stary dług: bez spłaty | Old debt: no payment | U |
| `RestAllocation_NewDebtNone` | Nowy dług: brak | New debt: none | U |
| `RestAllocation_NewDebtFormat` | Nowy dług: {0} | New debt: {0} | U/T |
| `RestAllocation_WeeklyQualified` | Odpoczynek tygodniowy: zaliczony | Weekly rest period: qualified | U |
| `RestAllocation_WeeklyNotQualified` | Odpoczynek tygodniowy: niezaliczony | Weekly rest period: not qualified | U |

`{0}` jest czasem trwania `HH:MM`. `RangeText` używa presentera czasu z `X-01`,
a `AllocationText` pozostaje technicznym zestawieniem czasów
`HH:MM + HH:MM`. Tekst nie jest parsowany z powrotem do decyzji.

`RestAllocationDecisionStatus` (`Active`, `Superseded`, `Invalidated`),
`IsPending` i `HasInvalidDecision` sterują danymi i przepływem, lecz na tym
ekranie nie są prezentowane jako tekst. Nie otrzymują kluczy w UI-05.

### Szczegóły zobowiązania

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Compensation_ReductionWeekFormat` | Tydzień {0} | Week {0} | U/T |
| `CompensationStatus_OpenOnTime` | OTWARTE · W TERMINIE | OPEN · ON TIME | P |
| `CompensationStatus_Overdue` | OTWARTE · ZALEGŁE | OPEN · OVERDUE | P |
| `CompensationStatus_PaidOnTime` | SPŁACONE W TERMINIE | PAID ON TIME | P |
| `CompensationStatus_PaidLate` | SPŁACONE PO TERMINIE | PAID LATE | P |

Wszystkie cztery wartości `WeeklyRestCompensationStatusDto` mają jawne
mapowanie bez `ToString()`. Kolory pozostają techniczne i nadal wynikają
z enuma, nie z przetłumaczonego tekstu.

`CompensationOverview` na Dashboardzie nadal używa czterech zatwierdzonych
`CompensationSummary_*`. Nie należy sklejać statusów szczegółowych z ich
fragmentów ani ponownie używać tekstu mieszanego wielkością liter z UI-06.

Źródłowy koniec odpoczynku, zakres spłaty, moment spłaty i termin wyłączny
korzystają z presentera `X-01`. `HH:MM`, `—`, separator `–`, nawiasy i numery
tygodni są strukturą techniczną. `DueAtExclusive` zachowuje semantykę
`CompleteBefore`; lokalizacja nie odejmuje minuty od granicy.

### Oczekiwane duplikaty wartości

Paczka dodaje jedną parę:

| Wartość EN | Klucze | Decyzja |
|---|---|---|
| `PAID LATE` | `CompensationSummary_PaidLate`, `CompensationStatus_PaidLate` | wspólny termin EN, lecz PL zachowuje zatwierdzone różne role i formy `SPŁACONO PO TERMINIE` / `SPŁACONE PO TERMINIE` |

Pozostałe 12 par zatwierdzonych po paczce 4 pozostaje bez zmian. Żaden nowy
klucz paczki 7 nie ma identycznej wartości PL/EN.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `MainWindow.xaml:246-351` | 19 literałów | 14 nowych kluczy + `Navigation_Compensations`, `Common_CopyAction` i `Dashboard_RemainingLabel` |
| `MainViewModel.cs:336-338` | pusty stan i licznik zobowiązań | 2 klucze nagłówka |
| `MainViewModel.cs:2228-2298` | budowa szczegółów, wybór decyzji, `OperationStatus` | tekst wierszy → UI-05; komunikaty operacyjne → UI-09 |
| `CompensationPresentation.cs:8-45` | cztery statusy skrótu Dashboardu | ponowne użycie `CompensationSummary_*` i `X-01` |
| `CompensationPresentation.cs:52-119` | 5 celów i 6 wyników alokacji | 11 kluczy `RestAllocation*` |
| `CompensationPresentation.cs:122-198` | status, tydzień, czasy i identyfikatory | 5 nowych kluczy + `History_CardHeader` + `X-01` |
| `WeeklyRestCompensationDto.cs:6-12` | 4 wartości statusu | wyczerpujący presenter szczegółów |
| `RestAllocation.cs:5-12` | 5 wartości celu | wyczerpujący presenter karty wyboru |

Trzy wystąpienia `KOPIUJ` korzystają z jednego `Common_CopyAction`; dlatego
19 literałów XAML składa się na 14 nowych kluczy oraz pięć rozstrzygniętych
wystąpień ponownego użycia.

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| identyfikatory zobowiązania, bloków, kandydata i decyzji | T | audyt i kopiowanie pełnej wartości |
| numer karty i nazwa profilu | O/T | dane użytkownika i identyfikator |
| `HH:MM`, `—`, `·`, `–`, `+`, nawiasy | T | struktura czasu i układu |
| `SettledAt`, `DueAtExclusive` | T | jawne nazwy techniczne/semantyka granicy |
| `RestAllocationDecisionStatus`, `IsPending`, `HasInvalidDecision` | T | sterowanie przepływem |
| kolory statusów | T | presenter wizualny oparty na enumie |
| CSV rekompensat | T | chroniony kontrakt eksportu |

### Ryzyka i kontrola wizualna

Największe ryzyko EN występuje w kartach alokacji o szerokości 330 px:

- `REDUCED WEEKLY REST + COMPENSATION`;
- `REGULAR WEEKLY REST + COMPENSATION`;
- `Weekly rest period: not qualified`;
- ostrzeżenie o niewiarygodnym Planerze i raporcie.

Test obejmuje co najmniej jeden wariant bez rekompensaty, jeden z rekompensatą,
status otwarty, zaległy, spłacony w terminie i spłacony po terminie oraz pełne
identyfikatory w tooltipach i schowku. Dopuszczalne jest zawijanie tekstu,
jeśli nie zmienia kolejności ani znaczenia decyzji.

### Kontrola paczki

- [x] wszystkie 19 literałów XAML ma nowy klucz albo jawne ponowne użycie;
- [x] wszystkie 5 wartości `RestAllocationPurpose` ma jawny presenter;
- [x] wszystkie 4 wartości `WeeklyRestCompensationStatusDto` ma jawny presenter;
- [x] statusy skrótu Dashboardu pozostają w zatwierdzonych `CompensationSummary_*`;
- [x] wspólne daty i terminy korzystają z `X-01`;
- [x] wszystkie formaty mają zgodne placeholdery PL/EN;
- [x] 32 nowe nazwy nie kolidują z katalogiem paczek 1–6;
- [x] nowa para `PAID LATE` jest jawnie dozwolona;
- [x] identyfikatory, enumy, DTO, SQLite, CSV i decyzje audytowe pozostają bez zmian;
- [x] tekst zasobu nie steruje kolorem, wyborem kandydata ani statusem decyzji;
- [x] `OperationStatus` i wyjątki pozostają przypisane do UI-09;
- [x] brak zmian w kodzie i XAML.

### Punkt kontrolny przed GO

- 14 kluczy etykiet ekranu;
- 2 klucze nagłówka;
- 11 kluczy alokacji;
- 5 kluczy szczegółów;
- 32 nowe klucze;
- 561 unikalnych nazw globalnie;
- 1 nowa para powtórzonej wartości;
- 13 dozwolonych par globalnie;
- 0 zmian wykonawczych.

### Werdykt

**GO — paczka 7 zatwierdzona.** Zamknięta bez pozycji otwartych. Łączny,
wiążący katalog paczek 1–7 zawiera 561 unikalnych nazw i 13 jawnie dozwolonych
par powtórzonych wartości.

## Paczka 8 — UI-06: Raporty w Desktop

**Zakres:** konfiguracja i podgląd raportu w Desktop, tabele aktywności,
naruszeń i rekompensat, kontrola kompletności oraz nazwy formatów eksportu.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 100 nowych kluczy. Łączny katalog paczek 1–8 zawiera
661 unikalnych nazw.

Paczka dodaje pięć nowych dozwolonych par powtórzonych wartości. Globalna
lista rośnie z 13 do 18 pozycji.

### Granica paczki

Paczka obejmuje `MainWindow.xaml:571-795`,
`ReportsWorkspaceViewModel.cs`, prezentację bilansu z `ReportDto`
i mapowanie wartości domenowych, które rzeczywiście trafiają do Raportów
Desktop. Nie obejmuje:

- treści i metadanych raportu PDF — `PDF-01`;
- kontraktów i nazw pól w VTC JSON oraz obu CSV — pozostają techniczne;
- filtrów systemowego okna zapisu, komunikatów `OperationStatus`,
  potwierdzeń zapisu i szczegółowych komunikatów błędów — `UI-09`;
- tekstu `exception.Message` — wyjątek trafia wyłącznie do diagnostyki;
- nazw plików eksportu i ich rozszerzeń — kontrakt techniczny;
- danych użytkownika, numerów kart, artykułów prawnych, identyfikatorów
  zobowiązań i bloków;
- `GapSummaryText` używanego przez PDF — `PDF-01`.

Widok może ponownie używać zatwierdzonych kluczy z wcześniejszych paczek,
ale nie może pobierać gotowych polskich tekstów z DTO. `CoverageBalanceText`
nie jest wiążącą prezentacją Desktop: M5.2 buduje bilans z wartości liczbowych
i lokalnego formatu `ReportCoverage_BalanceFormat`.

### Nagłówek i konfiguracja

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Report_Title` | RAPORTY I STATYSTYKI | REPORTS AND STATISTICS | U |
| `Report_Description` | Centrum raportowe · dane oparte na czasie gry | Reporting centre · game-time data | U/T |
| `ReportStep_Configuration` | 1  KONFIGURACJA | 1  CONFIGURATION | U/T |
| `ReportStep_DataCheck` | 2  KONTROLA DANYCH | 2  DATA CHECK | U/T |
| `ReportStep_Preview` | 3  PODGLĄD | 3  PREVIEW | U/T |
| `ReportStep_Export` | 4  EKSPORT | 4  EXPORT | U/T |
| `Report_DriverCardLabel` | KIEROWCA / KARTA | DRIVER / CARD | U |
| `Report_QuickRangeLabel` | SZYBKI ZAKRES | QUICK RANGE | U |
| `ReportRange_CurrentWeekAction` | BIEŻĄCY TYDZIEŃ | CURRENT WEEK | U |
| `ReportRange_Last24HoursAction` | OSTATNIE 24 H | LAST 24 HOURS | U/T |
| `ReportRange_AllHistoryAction` | CAŁA HISTORIA | ALL HISTORY | U |
| `ReportRange_CustomAction` | WŁASNY ZAKRES | CUSTOM RANGE | U |
| `ReportRange_CustomGameTimeLabel` | WŁASNY ZAKRES · CZAS GRY | CUSTOM RANGE · GAME TIME | U/T |
| `Report_RefreshPreviewAction` | ODŚWIEŻ PODGLĄD | REFRESH PREVIEW | U |
| `Report_ShowGapsAction` | POKAŻ LUKI | SHOW GAPS | U |

Numer kroku i separator między numerem a etykietą są częścią zatwierdzonego
tekstu widoku. Glif zakładki pozostaje strukturą XAML, a jej etykieta ponownie
używa `Navigation_Reports`.

### Kafelki i podsumowanie

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportMetric_Driving` | JAZDA | DRIVING | U/P |
| `ReportMetric_Work` | PRACA | WORK | U/P |
| `ReportMetric_OpenDebt` | OTWARTY DŁUG | OPEN DEBT | U/P |
| `Report_ViolationsLabel` | NARUSZENIA | VIOLATIONS | U |
| `ReportTab_Summary` | PODSUMOWANIE | SUMMARY | U |
| `Report_RangeLabel` | ZAKRES | RANGE | U |
| `ReportSummary_GeneratedAtLabel` | WYGENEROWANO | GENERATED | U |
| `ReportSummary_ActivityBlocksLabel` | BLOKI AKTYWNOŚCI | ACTIVITY BLOCKS | U |
| `Report_ActivitiesLabel` | AKTYWNOŚCI | ACTIVITIES | U |

Kafelek `GOTOWOŚĆ` ponownie używa `DeviceActivity_Availability`, a kafelek
`ODPOCZYNEK` — `ActivityUpper_Rest`. Prefiks pierwszego klucza jest historyczny,
lecz wartość i semantyka aktywności są identyczne w obu miejscach; tworzenie
drugiej identycznej pary byłoby sprzeczne z regułą przeglądu całego katalogu.

`PRACA` pozostaje krótką etykietą istniejącego kafelka, mimo że wartość pochodzi
z `OtherWorkMinutes`. Pełny presenter aktywności w tabeli nadal używa
`Activity_OtherWork` (`Inna praca` / `Other work`).

### Tabele aktywności, naruszeń i rekompensat

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Report_ShowTechnicalData` | Pokaż dane techniczne | Show technical data | U/T |
| `ReportActivity_SourceFormat` | Źródło: {0} | Source: {0} | U/P |
| `ReportActivity_ConditionFormat` | Warunek: {0} | Condition: {0} | U/P |
| `ReportViolation_ArticleHeader` | ARTYKUŁ | ARTICLE | U/T |
| `ReportViolation_NameHeader` | NARUSZENIE | VIOLATION | U |
| `ReportViolation_ExcessHeader` | PRZEKROCZENIE | EXCESS | U |
| `Report_CompensationsLabel` | REKOMPENSATY | COMPENSATIONS | U |
| `ReportCompensation_DueAtHeader` | TERMIN | DEADLINE | U/P |
| `ReportCompensation_PaymentHeader` | SPŁATA | PAYMENT | U |
| `ReportCompensation_SettledAtHeader` | ROZLICZONO | SETTLED | U/P |

Nagłówki ponownie używają:

- `Common_From`, `Common_To`, `ManualEntry_TimeHeader`
  i `Common_ActivityHeader` w tabeli aktywności;
- `ManualEntry_TimeHeader` w tabeli naruszeń;
- `Common_StatusHeader`, `CompensationSummary_DebtHeader`,
  `Dashboard_RemainingLabel` i `Common_SourceHeader` w tabeli rekompensat.

Historyczne prefiksy `ManualEntry_*`, `CompensationSummary_*`
i `Dashboard_*` są zaakceptowanym długiem nazewniczym zatwierdzonego katalogu.
Semantyka nagłówków jest identyczna, dlatego paczka nie tworzy ich kopii.

`ReportActivity_ConditionFormat` nie zawiera początkowej spacji ani separatora.
M5.2 przenosi ` · ` do osobnego elementu XAML między źródłem i warunkiem,
zgodnie z regułą paczki 4: interpunkcja układu nie należy do zasobu.

### Kompletność i eksport

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportTab_Completeness` | KOMPLETNOŚĆ | COMPLETENESS | U |
| `ReportCompleteness_GapsLabel` | LUKI | GAPS | U |
| `ReportCompleteness_EvidenceLabel` | MATERIAŁ DOWODOWY | EVIDENCE | U |
| `ReportCompleteness_PendingAllocationsLabel` | OCZEKUJĄCE ALOKACJE | PENDING ALLOCATIONS | U |
| `ReportCompleteness_BalanceLabel` | BILANS | BALANCE | U |
| `ReportExport_Title` | EKSPORT | EXPORT | U |
| `ReportExport_Description` | Przed zapisem podgląd jest przeliczany. Plik i ekran korzystają z tego samego raportu. | The preview is recalculated before saving. The file and screen use the same report. | U |
| `ReportExport_PdfAction` | EKSPORTUJ PDF | EXPORT PDF | U/T |
| `ReportExport_MoreHeader` | WIĘCEJ EKSPORTÓW | MORE EXPORTS | U |
| `ReportExport_VtcJson` | VTC JSON | VTC JSON | U/T |
| `ReportExport_CompensationCsvAction` | CSV ZOBOWIĄZAŃ | OBLIGATIONS CSV | U/T |
| `ReportExport_RawActivityCsvAction` | SUROWY CSV AKTYWNOŚCI | RAW ACTIVITY CSV | U/T |

`Report_RangeLabel`, `Report_ActivitiesLabel` i `Report_ViolationsLabel` są
neutralne, ponieważ ten sam tekst pełni więcej niż jedną rolę w obrębie
Raportów. Nazwa `ReportTab_*` pozostaje tylko tam, gdzie tekst występuje
wyłącznie jako zakładka.

### Presety zakresu i wybór karty

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportRange_CurrentWeekDescription` | Bieżący tydzień regulacyjny | Current regulatory week | P |
| `ReportRange_Last24HoursDescription` | Ostatnie 24 godziny czasu gry | Last 24 hours of game time | P/T |
| `ReportRange_AllHistoryDescription` | Cała dostępna historia karty | All available card history | P |
| `ReportRange_CustomDescription` | Własny zakres czasu gry | Custom game-time range | P/T |
| `Report_DriverCardFormat` | {0} — karta {1} | {0} — card {1} | U/O/T |

W formacie karty `{0}` jest nazwą profilu, a `{1}` numerem karty. Obie wartości
pozostają danymi użytkownika/technicznymi. Cztery wartości `ReportRangePreset`
mają jawne opisy i akcje bez fallbacku. `GameCalendar_DayFormat` z `X-01`
obsługuje wszystkie pozycje listy dni.

### Stany i walidacja podglądu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportStatus_SelectDriverAndRange` | Wybierz kierowcę i zakres raportu. | Select a driver and report range. | U |
| `ReportStatus_NoReportableCard` | Brak karty możliwej do raportowania. | No card is available for reporting. | U |
| `ReportStatus_SelectedCardNoHistory` | Wybrana karta nie ma historii. | The selected card has no history. | U |
| `ReportStatus_ExportUnavailableNoData` | Eksport jest niedostępny do czasu pojawienia się danych. | Export is unavailable until data becomes available. | U |
| `ReportStatus_CalculatingPreview` | PRZELICZANIE PODGLĄDU | RECALCULATING PREVIEW | U |
| `ReportStatus_ReadingCanonicalHistory` | Trwa odczyt kanonicznej historii karty. | Reading canonical card history. | U/T |
| `ReportStatus_PreviewError` | BŁĄD PODGLĄDU | PREVIEW ERROR | U |
| `ReportStatus_PreviewErrorDetail` | Nie udało się wygenerować podglądu raportu. Szczegóły zapisano w logu diagnostycznym. | The report preview could not be generated. Details were written to the diagnostic log. | U |
| `ReportValidation_SelectDriverCard` | Wybierz kierowcę i kartę. | Select a driver and card. | U |
| `ReportValidation_EndAfterStart` | Koniec zakresu musi być późniejszy niż początek. | The end of the range must be later than the start. | U |
| `ReportValidation_SelectStartEndDay` | Wybierz dzień początku i końca. | Select the start and end day. | U |
| `ReportValidation_TimeFormat` | Godzina musi mieć format HH:MM w zakresie 00:00–23:59. | Time must use the HH:MM format in the range 00:00–23:59. | U/T |
| `ReportStatus_ParametersChanged` | PARAMETRY ZOSTAŁY ZMIENIONE | PARAMETERS HAVE CHANGED | U |
| `ReportStatus_RefreshBeforeAnalysisExport` | Odśwież podgląd przed analizą lub eksportem. | Refresh the preview before analysis or export. | U |
| `ReportStatus_PreviewCurrent` | PODGLĄD AKTUALNY | PREVIEW UP TO DATE | U |
| `ReportStatus_ReportIncomplete` | RAPORT NIEKOMPLETNY | REPORT INCOMPLETE | U |

`ReportPreviewStatus` nie jest wyświetlany przez `ToString()`. Jego siedem
wartości steruje następującymi rodzinami: `NoSelection` ma dwa jawne warianty
braku wyboru/danych, `InvalidParameters` używa `ReportValidation_*`, `Loading`
używa pary `CalculatingPreview`/`ReadingCanonicalHistory`, `Current`
i `CurrentIncomplete` używają jawnych statusów i opisów, `OutOfDate` używa
pary o zmienionych parametrach, a `Error` — ogólnego komunikatu diagnostycznego.

Oba bloki `catch` w `ReportsWorkspaceViewModel` muszą zapisać wyjątek do
diagnostyki, zanim pokażą ogólny tekst. `exception.Message` nie może trafić
ani do `StatusDetail`, ani do `OperationStatus`; szczegółowy komunikat
operacyjny i ewentualna ścieżka logu należą do `UI-09`.

### Opisy stanu i pluralizacja

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportStatus_DataCompleteFormat` | Dane kompletne · {0} · {1} | Data complete · {0} · {1} | U/T |
| `ReportStatus_ViolationCountOneFormat` | {0} naruszenie | {0} violation | U/T |
| `ReportStatus_ViolationCountFewFormat` | {0} naruszenia | {0} violations | U/T |
| `ReportStatus_ViolationCountManyFormat` | {0} naruszeń | {0} violations | U/T |
| `ReportStatus_OpenObligationCountOneFormat` | {0} otwarte zobowiązanie | {0} open obligation | U/T |
| `ReportStatus_OpenObligationCountFewFormat` | {0} otwarte zobowiązania | {0} open obligations | U/T |
| `ReportStatus_OpenObligationCountManyFormat` | {0} otwartych zobowiązań | {0} open obligations | U/T |
| `ReportStatus_DataIncompleteFormat` | {0} · {1} · {2} · {3} | {0} · {1} · {2} · {3} | U/T |
| `ReportStatus_UnresolvedGapCountOneFormat` | {0} nierozliczona luka | {0} unresolved gap | U/T |
| `ReportStatus_UnresolvedGapCountFewFormat` | {0} nierozliczone luki | {0} unresolved gaps | U/T |
| `ReportStatus_UnresolvedGapCountManyFormat` | {0} nierozliczonych luk | {0} unresolved gaps | U/T |
| `ReportStatus_PendingAllocationCountOneFormat` | {0} oczekująca alokacja | {0} pending allocation | U/T |
| `ReportStatus_PendingAllocationCountFewFormat` | {0} oczekujące alokacje | {0} pending allocations | U/T |
| `ReportStatus_PendingAllocationCountManyFormat` | {0} oczekujących alokacji | {0} pending allocations | U/T |
| `ReportCoverage_MissingFormat` | Brak pokrycia: {0} | Missing coverage: {0} | U/T |
| `ReportCoverage_ExcessFormat` | Nadmiar pokrycia: {0} | Excess coverage: {0} | U/T |
| `ReportCoverage_Matches` | Pokrycie zakresu zgodne | Range coverage matches | U |

`ReportStatus_DataCompleteFormat` otrzymuje dwie gotowe, odmienione frazy.
`ReportStatus_DataIncompleteFormat` otrzymuje kolejno frazę liczby luk, czas luk,
stan pokrycia i frazę liczby oczekujących alokacji. Tekst nie jest składany
z pojedynczych przetłumaczonych rzeczowników.

Pluralizer PL stosuje trzy formy jak w paczce 4: `1`, końcówki `2–4`
z wyłączeniem `12–14`, oraz pozostałe. EN rozróżnia `1` i liczbę mnogą.
Jest to świadoma korekta obecnych stałych form `naruszeń`, `zobowiązań`,
`luk` i `alokacji`, które dziś dają błędne zdania dla części liczników.

### Bilans, dowód i opis czasu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportEvidence_Complete` | KOMPLETNY | COMPLETE | P |
| `ReportEvidence_Incomplete` | NIEKOMPLETNY | INCOMPLETE | P |
| `ReportCoverage_BalanceFormat` | {0} + {1} = {2} / zakres {3} | {0} + {1} = {2} / range {3} | U/T |
| `ReportRange_DescriptionFormat` | Zakres obejmuje: {0} | Range includes: {0} | U/T |
| `ReportRange_Invalid` | Zakres jest nieprawidłowy. | The range is invalid. | U |
| `ReportDuration_DayOneFormat` | {0} dzień {1} | {0} day {1} | U/T |
| `ReportDuration_DaysFormat` | {0} dni {1} | {0} days {1} | U/T |

W bilansie `{0}` to suma aktywności, `{1}` czas luk, `{2}` suma pokrycia,
a `{3}` zakres. Etykieta `BILANS` istnieje osobno w XAML, dlatego format nie
powtarza prefiksu `BILANS:` z `ReportDto.CoverageBalanceText`.

W formatach okresu `{0}` jest liczbą pełnych dni, a `{1}` pozostałym czasem
`HH:MM`. Polski i angielski wymagają osobnej formy wyłącznie dla jednego dnia;
wszystkie pozostałe liczby używają `DaysFormat`. `HH:MM`, `→`, `·`, `/`
i znak `—` pozostają strukturą techniczną.

### Presentery wierszy

Wsteczna korekta po porównaniu projektowanego katalogu z istniejącym
`ReportsWorkspaceViewModel.ActivityName` zachowuje dwa teksty zamrożonego UI:

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Activity_Rest` | Odpoczynek | Rest | P |
| `ReportActivity_OutOfScope` | Poza zakresem | Out of scope | P |

`Activity_Rest` ma neutralną nazwę, ponieważ ten sam tekst i znaczenie przejmuje
również Planer w paczce 10. Nie może zastąpić `Activity_BreakOrRest`
(`Przerwa / odpoczynek`) ani wersalikowego `ActivityUpper_Rest`.
`ReportActivity_OutOfScope` pozostaje osobnym kluczem Raportów: istniejący
presenter pokazuje czytelną frazę `Poza zakresem`, a nie kod `OUT`.

Wyczerpujące mapowanie tabeli aktywności wynosi:

| `DriverActivity` | Klucz / wartość |
|---|---|
| `Driving` | `Activity_Driving` |
| `OtherWork` | `Activity_OtherWork` |
| `Availability` | `Activity_Availability` |
| `BreakOrRest` | `Activity_Rest` |
| `OutOfScope` | `ReportActivity_OutOfScope` |
| `Unknown` | `Activity_Unknown` |

Tabela ponownie używa zatem czterech wcześniejszych `Activity_*`, dodaje jeden
wspólny `Activity_Rest` i jeden właściwy Raportom
`ReportActivity_OutOfScope`. Ponownie używa także sześciu `ActivitySource_*`
i czterech `SpecialCondition_*`. Obecne fallbacki
`record.Source.ToString()`, `record.Condition.ToString()` i
`activity.ToString()` nie mogą trafić do UI; każdy presenter jest jawny
i wyczerpujący.

Tabela naruszeń ponownie używa jedenastu `Violation_*` z paczki 2.
`ReportViolationDto.Type` pozostaje techniczną reprezentacją stabilnej wartości
`ViolationType`; presenter mapuje dokładnie jedenaście znanych wartości i nie
wyświetla surowego `ToString()`.

Statusy rekompensat zachowują mieszaną wielkość liter istniejącej tabeli
Raportów i dlatego nie przejmują wersalikowych kluczy paczki 7:

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportCompensationStatus_OpenOnTime` | Otwarte · w terminie | Open · on time | P |
| `ReportCompensationStatus_Overdue` | Zaległe | Overdue | P |
| `ReportCompensationStatus_PaidOnTime` | Spłacone w terminie | Paid on time | P |
| `ReportCompensationStatus_PaidLate` | Spłacone po terminie | Paid late | P |

Cztery wartości `WeeklyRestCompensationStatusDto` mają mapowanie 1:1 bez
fallbacku. Identyfikatory bloków są skracane wyłącznie wizualnie do 12 znaków;
wartość domenowa i eksport pozostają bez zmian.

Przed rozpoczęciem `PDF-01` trzeba jawnie zdecydować, czy raport PDF ponownie
używa jednej z trzech zatwierdzonych rodzin `WeeklyRestCompensationStatusDto`
(`CompensationSummary_*`, `CompensationStatus_*`,
`ReportCompensationStatus_*`), czy otrzymuje własną rodzinę uzasadnioną
odmienną pisownią. Decyzja musi poprzedzać katalog PDF, aby nie powstała
niezauważona piąta reprezentacja tego samego enuma obok
`DeviceCompensation_Overdue`.

### Nazwy formatów eksportu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `ReportExport_Pdf` | PDF | PDF | P/T |
| `ReportExport_CompensationCsvName` | CSV zobowiązań | obligations CSV | P/T |
| `ReportExport_RawActivityCsvName` | surowy CSV aktywności | raw activity CSV | P/T |

`ReportExport_VtcJson` jest jednocześnie neutralną nazwą formatu i tekstem
przycisku. Wszystkie cztery wartości `ReportExportFormat` mają jawne mapowanie
bez `format.ToString()`. `UI-09` użyje tych nazw w komunikatach operacyjnych
i filtrach okna zapisu; wzorce `*.pdf`, `*.json`, `*.csv` i rozszerzenia
pozostają techniczne.

### `RuleFindingLevel` — decyzja bez kluczy

`RuleFindingLevel` ma trzy wartości (`Information`, `Warning`, `Violation`),
ale nie ma aktywnego konsumenta w Raportach Desktop. `RuleFinding` nie jest
przenoszony do `ReportDto`; `RegulationReportAnalyzer` przekazuje do raportu
`RuleViolation` jako `ReportViolationDto`, którego widoczną kategorią jest
`ViolationType`.

Tworzenie trzech kluczy dla `RuleFindingLevel` dałoby martwy katalog.
W wyniku GO paczki 8 typ otrzymuje w rejestrze status
**świadomie wykluczony — brak prezentacji tekstowej w aktywnym przepływie
UI-06**. Jeżeli przyszła
funkcja zacznie prezentować `RuleFinding`, musi otworzyć osobną inwentaryzację
z rzeczywistym konsumentem.

`ReportWorkspaceTab` również nie wymaga presentera: enum steruje indeksem
pięciu statycznie nazwanych zakładek. `ReportPreviewStatus` steruje macierzą
stanów opisaną wyżej, a nie tekstem 1:1.

### Oczekiwane duplikaty wartości

Paczka dodaje pięć par:

| Wartość | Klucze | Decyzja |
|---|---|---|
| EN `DRIVING` | `DeviceActivity_Driving`, `ReportMetric_Driving` | urządzeniowy symbol kierownicy i pełna etykieta kafelka mają różne zatwierdzone teksty PL |
| EN `{0} violations` | `ReportStatus_ViolationCountFewFormat`, `ReportStatus_ViolationCountManyFormat` | EN ma jedną formę mnogą, PL wymaga dwóch |
| EN `{0} open obligations` | `ReportStatus_OpenObligationCountFewFormat`, `ReportStatus_OpenObligationCountManyFormat` | EN ma jedną formę mnogą, PL wymaga dwóch |
| EN `{0} unresolved gaps` | `ReportStatus_UnresolvedGapCountFewFormat`, `ReportStatus_UnresolvedGapCountManyFormat` | EN ma jedną formę mnogą, PL wymaga dwóch |
| EN `{0} pending allocations` | `ReportStatus_PendingAllocationCountFewFormat`, `ReportStatus_PendingAllocationCountManyFormat` | EN ma jedną formę mnogą, PL wymaga dwóch |

Pozostałe 13 par zatwierdzonych po paczce 7 pozostaje bez zmian. Żaden nowy
klucz paczki 8 nie ma wartości identycznej z innym kluczem w obu językach.
Porównanie jest dokładne (`Ordinal`): wielkość liter jest częścią wiążącej
wartości zasobu, zgodnie z decyzją o zachowaniu pisowni z paczki 1.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `MainWindow.xaml:571-795` | 62 atrybuty `Text`/`Content`/`Header` + 2 widoczne `StringFormat` | 64 teksty lokalizacyjne: 46 nowych kluczy, 14 wystąpień ponownego użycia i 4 powtórne użycia nowych kluczy w obrębie widoku |
| `ReportsWorkspaceViewModel.cs:14-48` | 4 enumy sterujące ekranem | jawne akcje/opisy/nazwy albo świadome sterowanie techniczne |
| `ReportsWorkspaceViewModel.cs:118-136,227-233` | stan początkowy i opisy presetów | 5 kluczy presetów/wyboru oraz stany ekranu |
| `ReportsWorkspaceViewModel.cs:351-496` | wybór karty, ładowanie, błędy, publikacja statusu | UI-06 dla stanu ekranu; `OperationStatus` i szczegóły → UI-09 |
| `ReportsWorkspaceViewModel.cs:545-623` | dzień, walidacja i zmiana parametrów | `X-01` + 6 kluczy walidacji/stanu |
| `ReportsWorkspaceViewModel.cs:625-735` | kompletność, liczniki i wiersze | pluralizacja, bilans, presentery oraz `X-01` |
| `ReportsWorkspaceViewModel.cs:751-830` | format czasu, statusy pokrycia, aktywności, rekompensaty i eksport | zasoby UI-06; fallbacki `ToString()` usunięte |
| `ReportDto.cs:34-40` | polskie `GapSummaryText` i `CoverageBalanceText` | bilans Desktop z liczb; tekst PDF → PDF-01 |
| `RegulationReportAnalyzer.cs:31-34` | techniczne `ViolationType.ToString()` w DTO | lokalny presenter 11 `Violation_*`; kontrakt eksportu bez zmian |
| `MainViewModel.cs:2081-2131` | okno zapisu, filtr, nazwa pliku i `OperationStatus` | nazwy formatów z UI-06; dialogi i komunikaty → UI-09 |
| `RuleContracts.cs:15-26` | `RuleFindingLevel` bez konsumenta Desktop | świadomie bez kluczy |

Rozliczenie 64 tekstów lokalizacyjnych XAML:

- 14 wystąpień używa wcześniejszych kluczy:
  `Navigation_Reports` (1), `Common_From` (2), `Common_To` (2),
  `DeviceActivity_Availability` (1), `ActivityUpper_Rest` (1),
  `ManualEntry_TimeHeader` (2), `Common_ActivityHeader` (1),
  `Common_StatusHeader` (1), `CompensationSummary_DebtHeader` (1),
  `Dashboard_RemainingLabel` (1), `Common_SourceHeader` (1);
- 50 wystąpień używa 46 nowych kluczy, ponieważ
  `Report_DriverCardLabel`, `Report_RangeLabel`, `Report_ActivitiesLabel`
  i `Report_ViolationsLabel` występują po dwa razy.

Licznik 62 obejmuje wyłącznie bezpośrednie atrybuty `Text`, `Content`
i `Header`. Dwa dodatkowe widoczne teksty są formatami bindingu:
`MainWindow.xaml:729` (`Źródło: {0}`) i `MainWindow.xaml:730`
(` · Warunek: {0}`). Oba otrzymują nowe klucze
`ReportActivity_SourceFormat` i `ReportActivity_ConditionFormat`, dlatego
dowód kompletności katalogu obejmuje łącznie 64 wystąpienia.

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| numery kart, nazwy kierowców | O/T | dane użytkownika i identyfikatory |
| artykuł prawny, identyfikatory zobowiązań i bloków | T | audyt i odwołanie do źródła |
| minuty domenowe, `HH:MM`, ISO data wygenerowania | T | stabilna reprezentacja czasu i dowodu |
| `ReportWorkspaceTab`, indeks zakładki, kolory statusów | T | sterowanie przepływem i prezentacją |
| `RuleFindingLevel` | T | brak aktywnego konsumenta tekstowego |
| rozszerzenia, wzorce filtrów i nazwa pliku | T | kontrakt systemu plików |
| nazwy pól i wartości VTC JSON/CSV | T | chroniony kontrakt eksportu |
| `—`, `→`, `·`, `/`, wielokropek skróconego identyfikatora | T | struktura układu |

### Ryzyka i kontrola wizualna

Największe ryzyko EN występuje w krokach i zakładkach o stałej szerokości,
w panelu eksportu 240 px oraz w sześciu kafelkach metryk:

- `2  DATA CHECK`, `PENDING ALLOCATIONS`, `ACTIVITY BLOCKS`;
- `CUSTOM RANGE · GAME TIME`, `REFRESH PREVIEW`;
- `The preview is recalculated before saving...`;
- `RAW ACTIVITY CSV`, `OBLIGATIONS CSV`;
- pełne frazy statusu z liczbą luk, pokryciem i oczekującymi alokacjami.

Test wizualny obejmuje PL i EN dla braku karty, braku historii, ładowania,
podglądu kompletnego, podglądu niekompletnego, błędu, zmienionych parametrów,
czterech presetów, wszystkich pięciu zakładek i rozwiniętego panelu eksportu.
Pluralizacja jest sprawdzana co najmniej dla `0`, `1`, `2`, `5`, `12`, `22`
i `25`.

### Kontrola paczki

- [x] wszystkie 62 bezpośrednie teksty XAML i 2 widoczne `StringFormat`
  mają nowy klucz albo jawne ponowne użycie;
- [x] 46 nowych kluczy statycznych rozlicza 50 wystąpień;
- [x] wszystkie 4 wartości `ReportRangePreset` mają akcję i opis;
- [x] wszystkie 7 wartości `ReportPreviewStatus` mają jawny sposób prezentacji;
- [x] wszystkie 5 wartości `ReportWorkspaceTab` ma statyczną etykietę i techniczny indeks;
- [x] wszystkie 4 wartości `ReportExportFormat` mają nazwę bez fallbacku;
- [x] wszystkie 6 `DriverActivity`, 6 `ActivitySource`, 4 `SpecialCondition`,
  11 `ViolationType` i 4 `WeeklyRestCompensationStatusDto` mają jawne mapowanie;
- [x] wszystkie 6 gałęzi istniejącego presentera `DriverActivity` porównano
  wartość po wartości z projektowanymi zasobami; `Odpoczynek` i
  `Poza zakresem` pozostają bez zmiany;
- [x] `RuleFindingLevel` ma decyzję opartą na rzeczywistym przepływie danych;
- [x] formaty mają zgodne zbiory placeholderów PL/EN;
- [x] pluralizacja ma jawne formy PL/EN i przypadki graniczne;
- [x] 100 nowych nazw nie koliduje z katalogiem paczek 1–7;
- [x] pięć nowych par powtórzonych wartości jest jawnie dozwolonych;
- [x] `exception.Message` i fallbacki `ToString()` nie są planowane jako tekst UI;
- [x] PDF, JSON, CSV, nazwy plików, DTO i wartości domenowe pozostają bez zmian;
- [x] brak zmian w kodzie i XAML.

### Punkt kontrolny po GO

- 46 kluczy statycznego widoku;
- 5 kluczy presetów i wyboru karty;
- 16 kluczy bazowych stanów i walidacji;
- 17 kluczy opisów stanu, liczników i pokrycia;
- 7 kluczy bilansu, dowodu i czasu;
- 2 klucze zachowujące istniejącą prezentację aktywności;
- 4 klucze statusów rekompensat;
- 3 klucze nazw formatów eksportu;
- 100 nowych kluczy;
- 661 unikalnych nazw globalnie;
- 5 nowych par powtórzonych wartości;
- 18 dozwolonych par globalnie;
- 0 zmian wykonawczych.

### Werdykt

**GO — paczka 8 zatwierdzona.** Zamknięta bez pozycji otwartych. Łączny,
wiążący katalog paczek 1–8 zawiera 661 unikalnych nazw i 18 jawnie dozwolonych
par powtórzonych wartości. `RuleFindingLevel` jest świadomie wykluczony
z prezentacji tekstowej, ponieważ nie ma aktywnego konsumenta w UI-06.

## Paczka 9 — UI-07: Kierowcy i Ustawienia

**Zakres:** lista i tworzenie profili kierowców, wybór profilu aktywnego,
ustawienia tachografu oraz wymagany przez M5.2 trwały wybór języka UI.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 18 nowych kluczy — 14 dla istniejącego widoku oraz 4 dla
kontrolki języka wymaganej kontraktem M5. Łączny katalog paczek 1–9 zawiera
679 unikalnych nazw.

Paczka nie dodaje par powtórzonych wartości między różnymi kluczami.
Globalna lista 18 dozwolonych par pozostaje bez zmian.

### Granica paczki

Paczka obejmuje `MainWindow.xaml:797-799`, pola i komendy kierowców/ustawień
w `MainViewModel`, `DriverService`, `SettingsService` oraz osobną persystencję
wybranej kultury UI. Nie obejmuje:

- komunikatów sukcesu, walidacji i błędów publikowanych przez
  `OperationStatus` — ich właścicielem pozostaje `UI-09`;
- dialogów kart w slotach S1/S2 — `UI-09`;
- nazw kierowców, numerów kart i identyfikatorów profili — dane O/T;
- kodu kraju wydania karty, dat ważności i reguł profilu — dane domenowe;
- dynamicznej zmiany języka bez restartu — jawnie poza zakresem M5;
- języków innych niż `pl-PL` i `en-GB`;
- zmian kontraktów `.tacho`, VTC JSON, CSV ani SQLite.

Dodanie wyboru języka nie jest nową funkcją „przy okazji”: wynika wprost
z M5.2 i końcowego smoke M7. Kontrolka ma dwa warianty i nie przeładowuje
otwartych okien; zmiana obowiązuje dopiero po ponownym uruchomieniu.

### Kierowcy

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Drivers_Title` | PROFILE KIEROWCÓW | DRIVER PROFILES | U |
| `Drivers_ActiveHeader` | AKTYWNY | ACTIVE | U |
| `Drivers_CreatedAtHeader` | UTWORZONO | CREATED | U/T |
| `Drivers_NewProfileTitle` | NOWY PROFIL | NEW PROFILE | U |
| `Drivers_DriverNameTooltip` | Nazwa kierowcy | Driver name | U/O |
| `Drivers_CardNumberTooltip` | Numer karty | Card number | U/T |
| `Drivers_SetActiveAction` | USTAW AKTYWNY | SET ACTIVE | U |

Zakładka ponownie używa `Navigation_Drivers`, przycisk `DODAJ` —
`Common_AddAction`, a nagłówek `KIEROWCA` —
`ManualEntry_DriverLabel`. Ostatni prefiks jest historycznym długiem
zatwierdzonego katalogu, lecz rola etykiety kierowcy i pisownia są identyczne.

Kolumna `AKTYWNY` pozostaje polem logicznym prezentowanym przez
`DataGridCheckBoxColumn`; nie powstają teksty `Tak`/`Nie`. `CreatedAtUtc`
nie może korzystać z przypadkowego `ToString()` WPF. Warstwa prezentacji
konwertuje moment na lokalną strefę i krótki format daty/czasu aktywnej kultury,
bez zmiany wartości UTC w DTO i SQLite.

Konwersja `CreatedAtUtc` do lokalnej strefy jest świadomą decyzją produktową
zatwierdzoną z GO paczki 9, a nie neutralnym skutkiem formatowania. Widoczna
godzina zmienia się o przesunięcie strefy użytkownika względem UTC, natomiast
instant, kolejność rekordów i dane w DTO/SQLite pozostają identyczne. Test
regresyjny musi osobno potwierdzić ten sam instant przed i po prezentacji oraz
oczekiwaną godzinę lokalną w strefie innej niż UTC.

### Ustawienia tachografu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Settings_Title` | USTAWIENIA TACHOGRAFU | TACHOGRAPH SETTINGS | U |
| `Settings_RestartNotice` | Parametry zostaną zastosowane przy następnym uruchomieniu. | The settings will be applied the next time the application starts. | U |
| `Settings_DrivingThresholdLabel` | Próg wykrywania jazdy | Driving detection threshold | U |
| `Settings_DrivingThresholdDescription` | Prędkość, od której tachograf wybiera jazdę automatycznie. | The speed at which the tachograph selects driving automatically. | U |
| `Settings_WeekOffsetLabel` | Przesunięcie tygodnia | Week offset | U |
| `Settings_WeekOffsetDescription` | Korekta początku tygodnia regulacyjnego w dniach. | Adjustment of the regulatory week's start in days. | U/T |
| `Settings_SaveAction` | ZAPISZ USTAWIENIA | SAVE SETTINGS | U |

Zakładka ponownie używa `Navigation_Settings`. `km/h`, zakres `0–20`,
zakres dni `-6–6` oraz wartości liczbowe pozostają techniczne. Binding liczby
zmiennoprzecinkowej używa aktywnej kultury (`1,5` w PL, `1.5` w EN), natomiast
SQLite przechowuje tę samą wartość liczbową niezależnie od języka.

### Trwały wybór języka

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Settings_LanguageLabel` | Język interfejsu | Interface language | U |
| `Settings_LanguageDescription` | Zmiana języka zostanie zastosowana przy następnym uruchomieniu. | The language change will be applied the next time the application starts. | U |
| `Language_Polish` | Polski | Polish | U/T |
| `Language_EnglishUnitedKingdom` | Angielski (Wielka Brytania) | English (United Kingdom) | U/T |

Wartość prezentacyjna nie jest zapisywana. Model opcji zawiera stabilne
`CultureName` (`pl-PL` albo `en-GB`) i lokalizowaną nazwę. Zapis, porównanie
i odtworzenie wyboru zawsze używają `CultureName`, nigdy tekstu zasobu.

Chroniony schemat SQLite pozostaje bez zmian: `SettingsDto`,
`TachographSettingsEntity` i `SettingsRepository` nadal przechowują wyłącznie
próg jazdy i przesunięcie tygodnia. M5.2 dodaje osobny
`UiCulturePreferenceStore` w Desktop, wzorowany na istniejącym magazynie
wejść Planera. Plik `%LocalAppData%\ETS2Tachograph\ui-culture.json` ma
techniczny schemat `{ "schemaVersion": 1, "cultureName": "pl-PL" }`.
Nie jest to VTC JSON ani kontrakt eksportu.

Brak pliku zachowuje `pl-PL`, czyli język istniejących instalacji.
Nieobsługiwana, pusta albo uszkodzona wartość po odczycie uruchamia bezpieczny
fallback `en-GB`, zgodnie z planem M5, i zapisuje zdarzenie diagnostyczne bez
wyświetlania surowego kodu użytkownikowi. Zapis używa pliku tymczasowego
i atomowej zamiany, aby awaria nie pozostawiła częściowego JSON.

Kultura jest ustawiana w `App.xaml.cs` po odczycie ustawień, lecz przed
utworzeniem `MainWindow`, `MainViewModel` i raportera PDF. Implementacja ustawia
bieżącą i domyślną kulturę wątków oraz język WPF, aby bindingi liczb i dat nie
pozostały w domyślnym `en-US`. PDF używa tej samej aktywnej kultury; osobny
język raportu pozostaje poza zakresem.

### Zobowiązania przekazane do UI-09

Wszystkie poniższe przypadki są zinwentaryzowane, ale nie tworzą kluczy
pakietu 9, ponieważ jedynym konsumentem jest wspólny `OperationStatus`
lub komunikat błędu:

| Przypadek | Obecny tekst / źródło | Wymagana decyzja UI-09 |
|---|---|---|
| profil utworzony | `Profil utworzony.` | lokalizowane potwierdzenie |
| profil ustawiony jako aktywny | `Profil zapisany. Uruchom aplikację ponownie, aby rozpocząć jego sesję.` | lokalizowane potwierdzenie restartu |
| ustawienia zapisane | `Ustawienia zapisane. Zostaną zastosowane przy następnym uruchomieniu.` | lokalizowane potwierdzenie restartu |
| brak nazwy kierowcy | `Driver display name is required.` | znany błąd pola, bez `exception.Message` |
| brak numeru karty | `Driver card number is required.` | znany błąd pola, bez `exception.Message` |
| błędna kolejność dat karty | `Driver card expiry must not precede its validity date.` | znany błąd domenowy; dziś brak aktywnej ścieżki edycji dat |
| próg poza `0–20 km/h` | `Driving threshold must be between 0 and 20 km/h.` | znany błąd pola, bez `exception.Message` |
| przesunięcie poza `-6–6` | `Week offset must be between -6 and 6 days.` | znany błąd pola, bez `exception.Message` |
| nieobsługiwana kultura przy zapisie | nowy przypadek M5.2 | znany błąd pola; dozwolone tylko `pl-PL` i `en-GB` |
| błąd zapisu preferencji języka | nowy przypadek M5.2 | ogólny komunikat, szczegół wyłącznie w diagnostyce |
| nieznany błąd zapisu profilu/ustawień | obecnie `ex.Message` | komunikat ogólny, wyjątek tylko do diagnostyki |

`DriverService` i `SettingsService` mogą zachować angielskie komunikaty
wyjątków jako diagnostykę/developer contract, ale Desktop nie może ich
wyświetlać bezpośrednio. M5.2/M5.3 powinny użyć jawnych wyników walidacji,
kodów błędów albo kontrolowanego mapowania; tekst zasobu nie steruje logiką.

### Mapowanie źródeł

| Źródło | Obecna wartość / rodzina | Decyzja |
|---|---|---|
| `MainWindow.xaml:797` | 10 wystąpień Kierowców | 7 nowych kluczy + `Navigation_Drivers`, `Common_AddAction`, `ManualEntry_DriverLabel` |
| `MainWindow.xaml:799` | 8 wystąpień Ustawień | 7 nowych kluczy + `Navigation_Settings` |
| `M5_LOKALIZACJA_PL_EN.md:43-50,84-96` | trwała kultura, wybór języka, restart i fallback | 4 nowe klucze + pole `CultureName` |
| `MainViewModel.cs:385-413` | pola profilu i ustawień | bindingi danych; bez lokalizacji O/T |
| `MainViewModel.cs:2015-2043` | tworzenie/aktywacja profilu i zapis ustawień | przepływ bez zmian; komunikaty → UI-09 |
| `DriverService.cs:14-29` | 3 walidacje profilu | jawne przypadki UI-09; brak wycieku tekstu wyjątku |
| `SettingsService.cs:8-18` | 2 walidacje liczb | jawne przypadki UI-09; serwis tachografu bez odpowiedzialności za kulturę |
| nowy `UiCulturePreferenceStore` | walidacja `pl-PL` / `en-GB`, fallback i zapis atomowy | persystencja kultury niezależna od SQLite |
| `JourneyPlannerInputStateStore.cs:31-57` | wzorzec osobnego magazynu preferencji Desktop | nowy, wersjonowany `UiCulturePreferenceStore` |
| `SettingsDto.cs`, `SettingsRepository.cs`, `Entities.cs` | ustawienia tachografu w SQLite | bez zmian schematu i znaczenia |
| `App.xaml.cs:56-147` | inicjalizacja przed utworzeniem okna | odczyt preferencji i zastosowanie kultury przed UI i PDF |

### Rozliczenie tekstów widoku

W istniejącym XAML są dokładnie 18 wystąpień i 18 unikalnych wartości:

- 4 wystąpienia ponownie używają zatwierdzonego katalogu:
  `Navigation_Drivers`, `Navigation_Settings`, `Common_AddAction`
  i `ManualEntry_DriverLabel`;
- pozostałe 14 otrzymuje 14 nowych kluczy;
- wymagany przez M5.2 wybór języka dodaje 4 nowe klucze, których nie ma jeszcze
  w zamrożonym XAML.

Łącznie paczka zawiera 18 nowych nazw. Nie ma placeholderów ani powtórzonych
wartości między różnymi nowymi kluczami.

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| nazwa kierowcy, numer karty | O/T | dane użytkownika i identyfikator |
| identyfikator profilu, `IsActive` | T | dane i sterowanie |
| kod kraju wydania, daty ważności karty | T | dane tachografowe |
| `CreatedAtUtc` | T | instant pozostaje UTC; zatwierdzona prezentacja świadomie konwertuje go do lokalnej strefy i formatu kultury |
| `pl-PL`, `en-GB` | T | stabilne identyfikatory kultury |
| `km/h`, `0–20`, `-6–6` | T | jednostka i granice walidacji |
| wartości `double`/`int` w SQLite | T | dane liczbowe niezależne od kultury |
| `schemaVersion`, `cultureName`, nazwa pliku preferencji | T | wewnętrzny kontrakt ustawienia UI |

### Ryzyka i kontrola wizualna

Największe ryzyko EN występuje w zwartej zakładce Ustawień o szerokości
600 px i w wierszu nowego profilu:

- `The settings will be applied the next time the application starts.`;
- `Driving detection threshold`;
- `Adjustment of the regulatory week's start in days.`;
- `English (United Kingdom)` w kontrolce o docelowej szerokości 180 px;
- `SET ACTIVE` obok pól nazwy i numeru karty.

Test wizualny obejmuje oba języki, pustą i długą nazwę kierowcy, długi numer
karty, oba warianty języka oraz format daty utworzenia. Test funkcjonalny
obejmuje zapis `pl-PL`, zapis `en-GB`, restart po każdym wyborze, brak pliku
preferencji, nieznaną kulturę, uszkodzony JSON i przerwany zapis, przecinek
dziesiętny PL, kropkę EN oraz identyczne wartości liczbowe po ponownym odczycie.
Schemat istniejącej bazy SQLite musi pozostać identyczny.

### Kontrola paczki

- [x] wszystkie 18 istniejących wystąpień XAML ma klucz albo jawne ponowne użycie;
- [x] 14 nowych kluczy istniejącego widoku nie koliduje z paczkami 1–8;
- [x] 4 klucze wyboru języka realizują jawny wymóg M5.2;
- [x] `CultureName` jest oddzielone od lokalizowanej nazwy języka;
- [x] brak preferencji (`pl-PL`) i fallback uszkodzonej wartości (`en-GB`)
  mają odrębne znaczenie;
- [x] kultura jest stosowana przed utworzeniem UI i PDF;
- [x] wybór języka nie zmienia schematu SQLite ani kontraktów eksportu;
- [x] nazwy, numery kart, identyfikatory i wartości SQLite nie są tłumaczone;
- [x] 11 przypadków `OperationStatus`/błędów ma właściciela w UI-09;
- [x] brak nowych duplikatów wartości i brak placeholderów;
- [x] brak zmian w kodzie i XAML.

### Punkt kontrolny po GO

- 7 nowych kluczy Kierowców;
- 7 nowych kluczy istniejących Ustawień;
- 4 nowe klucze wyboru języka;
- 18 nowych kluczy;
- 679 unikalnych nazw globalnie;
- 0 nowych par powtórzonych wartości;
- 18 dozwolonych par globalnie;
- 0 zmian wykonawczych.

### Werdykt

**GO — paczka 9 zatwierdzona.** Zamknięta bez pozycji otwartych. `UI-07`
jest zamknięte, a łączny, wiążący katalog paczek 1–9 zawiera 679 unikalnych
nazw i 18 jawnie dozwolonych par powtórzonych wartości.

## Paczka 10 — UI-08: Planer

**Zakres:** konfiguracja oferty i aktywnej dostawy, gotowość obu kart,
walidacja czasu, wynik, harmonogram, ostrzeżenia i podsumowanie Planera.

**Stan:** **ZAMKNIĘTA — GO**

**Data zatwierdzenia:** 2026-07-27

**Pozycje otwarte:** 0

**Katalog:** 103 nowe klucze. Łączny katalog paczek 1–10 zawiera
782 unikalne nazwy.

Paczka dodaje dwie nowe dozwolone pary powtórzonych wartości. Globalna
lista zawiera 20 pozycji.

### Granica paczki

Paczka obejmuje `MainWindow.xaml:352-568`,
`JourneyPlannerViewModel.cs`, `DeliveryPlannerService.cs`,
`DeliveryPlanningContracts.cs`, aktywną ścieżkę
`DeliveryPlanningEngine` → `CrewJourneyPlanningEngine` oraz wartości domenowe,
które rzeczywiście trafiają do widoku. Nie obejmuje:

- szczegółów czterech nieznanych wyjątków z ładowania snapshotu, obliczania
  planu oraz odczytu i zapisu preferencji wejścia — ich właścicielem jest
  `UI-09`, a `exception.Message` trafia wyłącznie do diagnostyki;
- technicznych kontraktów silnika, limitów wyszukiwania, identyfikatorów
  snapshotu i nazw właściwości komend;
- zmiany algorytmu, reguł prawnych, wartości czasu ani wyniku planowania;
- osobnego, niepodłączonego do Desktop przepływu `JourneyPlannerService`;
- komunikatów PDF i eksportów;
- reorganizacji zamrożonego układu Planera.

Zasoby należą do Desktop. Warstwa Application nie otrzymuje zależności od
lokalizacji: dwa znane przypadki gotowości są mapowane w Desktop z jawnego stanu
lub stabilnego kodu, a nie z polskiego tekstu `DeliveryPlannerReadiness.Issues`.

### Nagłówek i instrukcja

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Planner_Title` | PLANER PODRÓŻY | JOURNEY PLANNER | U |
| `Planner_StrategyDescription` | Strategia: najwcześniejsza legalna · obliczenia tylko do odczytu | Strategy: earliest legal · read-only calculations | U |
| `Planner_TimeInputInstructions` | Pola czasu: wpisz wartość albo zmieniaj klawiszami — ↑ / ↓ o ±5 min, PgUp / PgDn o ±60 min; obok pól są też przyciski skrótu, a okno dostawy wybierasz z list dzień / godzina / minuta. | Time fields: enter a value or use the keys — ↑ / ↓ by ±5 min, PgUp / PgDn by ±60 min; preset buttons are also available, and the delivery window is selected from the day / hour / minute lists. | U/T |

Nagłówek zakładki ponownie używa `Navigation_Planner`. `Planner_Title` ma tę
samą wartość EN, lecz inną zatwierdzoną wartość PL i inną rolę nagłówka strony;
para jest jawnie dozwolona w sekcji duplikatów.

### Konfiguracja

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Planner_ModeLabel` | TRYB PLANERA | PLANNER MODE | U |
| `PlannerMode_MarketOffer` | OFERTA Z RYNKU | MARKET OFFER | U/P |
| `PlannerMode_ActiveDelivery` | AKTYWNA DOSTAWA | ACTIVE DELIVERY | U/P |
| `Planner_FirstDriverLabel` | PROWADZI PIERWSZY | DRIVES FIRST | U |
| `Planner_DriveToPickupLabel` | DOJAZD PO ŁADUNEK | DRIVE TO PICKUP | U |
| `Planner_OfferExpiresInLabel` | OFERTA WYGASA ZA | OFFER EXPIRES IN | U |
| `Planner_PickupLabel` | ODBIÓR | PICKUP | U |
| `Planner_LoadedRouteLabel` | TRASA Z ŁADUNKIEM | LOADED ROUTE | U |
| `Planner_DeliveryWindowFromLabel` | OKNO DOSTAWY OD | DELIVERY WINDOW FROM | U |
| `Planner_DeliveryWindowToLabel` | OKNO DOSTAWY DO | DELIVERY WINDOW TO | U |
| `Planner_HourTooltip` | Godzina | Hour | U |
| `Planner_MinuteTooltip` | Minuta | Minute | U |
| `Planner_UnloadingLabel` | ROZŁADUNEK | UNLOADING | U |
| `Planner_PostDeliveryWorkLabel` | PRACA PO | WORK AFTER | U |
| `Planner_TightThresholdLabel` | PRÓG „NA STYK” | “TIGHT” THRESHOLD | U |
| `Planner_CalculateAction` | OBLICZ PLAN | CALCULATE PLAN | U |

`PlannerMode_*` mapują 1:1 oba elementy `DeliveryPlanningUseCase`. Te same
klucze zasilają kontrolki trybu i modele opcji; niewidoczne pole `Name` nie
otrzymuje drugiej wersji o odmiennej pisowni.

Przyciski `15m`, `30m`, `1h`, `2h`, przesunięcia `±5 min` i `±60 min`,
formaty `HH:MM`, `1h30`, `1,5`/`1.5` oraz wartości godzin i minut pozostają
techniczne. Parser nadal akceptuje kropkę i przecinek niezależnie od języka,
ale instrukcja i przykład walidacji pokazują separator aktywnej kultury.

### Sloty i pełne nazwy dni

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerSlot_Driver` | S1 — kierowca | S1 — driver | U/T |
| `PlannerSlot_CoDriver` | S2 — zmiennik | S2 — co-driver | U/T |
| `Weekday_Full_Monday` | Poniedziałek | Monday | P |
| `Weekday_Full_Tuesday` | Wtorek | Tuesday | P |
| `Weekday_Full_Wednesday` | Środa | Wednesday | P |
| `Weekday_Full_Thursday` | Czwartek | Thursday | P |
| `Weekday_Full_Friday` | Piątek | Friday | P |
| `Weekday_Full_Saturday` | Sobota | Saturday | P |
| `Weekday_Full_Sunday` | Niedziela | Sunday | P |

Pełne nazwy dni są trzecią zatwierdzoną rolą `GameWeekday`, odrębną od
`Weekday_Display_*` (`Pon`) i `Weekday_Short_*` (`PON`). M5.2 dopisuje komentarze
do wszystkich trzech rodzin, aby narzędzie translatorskie nie ujednoliciło
wartości różniących się długością lub wielkością liter.

### Gotowość, stan i pochodzenie wejścia

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerStatus_CheckingReadiness` | Sprawdzanie gotowości planera… | Checking planner readiness… | U |
| `PlannerCrew_WaitingForSnapshot` | Oczekiwanie na snapshot obu kart… | Waiting for a snapshot of both cards… | U/T |
| `PlannerInput_DefaultValuesAutosave` | Wartości domyślne · zapis automatyczny | Default values · autosave | U |
| `PlannerReadiness_CurrentCrewSnapshotRequired` | Wymagany jest aktualny snapshot telemetryczny obu kart w podwójnej obsadzie. | A current telemetry snapshot of both cards in multi-manning is required. | U/T |
| `PlannerReadiness_ResolveCardRemovalGap` | Rozlicz lukę po wyjęciu karty przed obliczeniem planu. | Resolve the card-removal gap before calculating the plan. | U/P |
| `PlannerVerdict_UnreliableData` | BRAK WIARYGODNYCH DANYCH | NO RELIABLE DATA | U/P |
| `PlannerCrew_CrewFormat` | S1: {0} · S2: {1} · podwójna obsada 30 h | S1: {0} · S2: {1} · 30 h multi-manning | U/O/T |
| `PlannerCrew_Incomplete` | Brak kompletnej podwójnej obsady S1/S2 | Incomplete S1/S2 multi-manning crew | U/T |
| `PlannerStatus_DataReady` | Dane gotowe — wybierz kierowcę i oblicz plan. | Data ready — select the driver and calculate the plan. | U |
| `PlannerStatus_CrewStateChanged` | Stan załogi zmienił się. Oblicz plan ponownie. | The crew state has changed. Calculate the plan again. | U |
| `PlannerValidation_NoCurrentSnapshot` | Brak aktualnego snapshotu obu kart. | No current snapshot of both cards is available. | U/T |
| `PlannerStatus_InputChanged` | Zmieniono dane. Oblicz plan ponownie. | The input has changed. Calculate the plan again. | U |
| `PlannerInput_UserValuesAutosave` | Wartości użytkownika · zapis automatyczny | User values · autosave | U |
| `PlannerInput_RestoredValues` | Przywrócono wartości z poprzedniej sesji | Values restored from the previous session | U |

W `PlannerCrew_CrewFormat` `{0}` i `{1}` są niezmienionymi nazwami kierowców.
`S1`, `S2` i `30 h` pozostają elementami technicznymi pełnego formatu.
`PlannerVerdict_UnreliableData` jest wspólnym tekstem statusu gotowości i wyniku;
nie powstaje drugi identyczny klucz.

### Kafelki wyniku i tabela

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `Planner_CurrentGameTimeLabel` | AKTUALNY CZAS W GRZE | CURRENT GAME TIME | U/T |
| `Planner_OfferExpiresLabel` | OFERTA WYGASA | OFFER EXPIRES | U |
| `Planner_DeliveryWindowLabel` | OKNO DOSTAWY | DELIVERY WINDOW | U |
| `Planner_CargoPickupLabel` | ODBIÓR ŁADUNKU | CARGO PICKUP | U |
| `Planner_DeliveryArrivalLabel` | PRZYJAZD NA DOSTAWĘ | DELIVERY ARRIVAL | U |
| `Planner_DeliveryCompletionLabel` | KONIEC DOSTAWY | DELIVERY COMPLETION | U |
| `Planner_VerdictMarginLabel` | WERDYKT / MARGINES | VERDICT / MARGIN | U |
| `Planner_VehicleHeader` | POJAZD | VEHICLE | U |
| `Planner_WarningsTitle` | OSTRZEŻENIA I OGRANICZENIA | WARNINGS AND LIMITATIONS | U |
| `Planner_SummaryTitle` | PODSUMOWANIE PLANU | PLAN SUMMARY | U |
| `Planner_NotApplicable` | Nie dotyczy | Not applicable | U |
| `PlannerTime_FromPrefix` | Od | From | U |
| `PlannerTime_ToPrefix` | Do | To | U |
| `PlannerVehicle_Parked` | Postój | Stationary | P |
| `PlannerSummary_PlanRejected` | Plan nie spełnia warunków przyjęcia | The plan does not meet the acceptance conditions | U |
| `PlannerSummary_PlanFitsWindow` | Plan mieści się w oknie dostawy | The plan fits within the delivery window | U |
| `PlannerSummary_BothCardsIncluded` | Harmonogram uwzględnia obie karty S1/S2 | The schedule includes both S1/S2 cards | U/T |
| `PlannerSummary_MarginFormat` | Zapas do końca okna: {0} | Margin to the end of the window: {0} | U/T |

Tabela ponownie używa `Common_From`, `Common_To`, `ManualEntry_TimeHeader`
i `Common_ReasonHeader`. `#`, `S1` i `S2` pozostają techniczne.
Stan poruszającego się pojazdu ponownie używa `Activity_Driving`; tekst i
znaczenie są zgodne, więc nie powstaje duplikat `Jazda` / `Driving`.

Komórki S1/S2 zachowują istniejący tekst `Odpoczynek` dla aktywnego
`BreakOrRest` przez wspólny klucz `Activity_Rest` dodany we wstecznej korekcie
paczki 8. Pozostałe mapowanie ponownie używa `Activity_Driving`,
`Activity_OtherWork`, `Activity_Availability` i `Activity_Unknown`; techniczne
`OUT` pozostaje wyłącznie dla `OutOfScope`.

Obecny fallback `_ => "Odpoczynek"` zostaje zastąpiony sześcioma jawnymi
gałęziami. Aktywny silnik załogi emituje w komórkach Planera `Driving`,
`OtherWork`, `Availability` i `BreakOrRest`, więc zmiana nie modyfikuje żadnego
dzisiejszego tekstu. `OutOfScope` i `Unknown` są wyczerpującymi, nieaktywnymi
gałęziami presentera; nie wolno ich przedstawiać jako odpoczynku.

Format okna składa `PlannerTime_FromPrefix`, `PlannerTime_ToPrefix`, stałe
`: ` i znak nowej linii oraz dwa momenty z presentera czasu paczki 5. Prefiksy
nie zawierają końcowej interpunkcji ani odstępu. `—`, podpisane `+HH:MM` /
`−HH:MM`, numer wiersza i wartości czasu pozostają techniczne.

Glify `✓` i `✕` są strukturą prezentacji poprzedzającą zasób podsumowania,
nie częścią tłumaczenia.

### Walidacja pól czasu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerValidation_DurationFormat` | {0}: wpisz czas jako HH:MM, minuty, 1h30 albo 1,5. | {0}: enter a time as HH:MM, minutes, 1h30 or 1.5. | U/T |
| `PlannerValidation_TimeLabel` | Czas | Time | U |
| `PlannerValidation_CorrectTimeField` | Popraw oznaczone pole czasu. | Correct the highlighted time field. | U |
| `PlannerField_OfferExpiresIn` | Oferta wygasa za | Offer expires in | U |
| `PlannerField_Pickup` | Odbiór | Pickup | U |
| `PlannerField_TightThreshold` | Próg „na styk” | “Tight” threshold | U |

Pozostałe cztery nazwy pól w komunikacie walidacji ponownie używają
`PlannerPhase_DriveToPickup`, `PlannerPhase_DriveWithCargo`,
`PlannerPhase_Unloading` i `PlannerPhase_PostDeliveryWork`. Rozdzielenie rodzin
wersalikowych i zdaniowych jest celowe: zgodnie z decyzją paczki 1 pisownia
pozostaje częścią zasobu, a globalny konwerter wielkości liter nie powstaje.

### Werdykt i przyczyny przerwania

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerVerdict_PickupMissed` | NIE ZDĄŻYSZ ODEBRAĆ | PICKUP DEADLINE MISSED | U/P |
| `PlannerVerdict_DeliveryMissed` | NIE ZDĄŻYSZ DOSTARCZYĆ | DELIVERY DEADLINE MISSED | U/P |
| `PlannerVerdict_Take` | MOŻNA PRZYJĄĆ | CAN ACCEPT | U/P |
| `PlannerVerdict_Tight` | NA STYK | TIGHT | U/P |
| `PlannerVerdict_Reject` | BRAK LEGALNEJ KONTYNUACJI | NO LEGAL CONTINUATION | U/P |
| `PlannerFailure_OfferExpired` | Odbiór nie zakończy się przed wygaśnięciem oferty. | Pickup will not be completed before the offer expires. | U/P |
| `PlannerFailure_DeliveryWindowMissed` | Dostawa nie zakończy się przed końcem okna. | Delivery will not be completed before the end of the window. | U/P |
| `PlannerFailure_NoLegalContinuation` | Brak legalnej kontynuacji harmonogramu. | There is no legal continuation of the schedule. | U/P |
| `PlannerFailure_InsufficientData` | Brak wiarygodnych danych obu kart. | Reliable data from both cards is unavailable. | U/P |
| `PlannerFailure_StaleSnapshot` | Dane załogi zmieniły się podczas obliczeń. | The crew data changed during calculation. | U/P |
| `PlannerFailure_CalculationLimitReached` | Osiągnięto limit obliczeń przed znalezieniem planu. | The calculation limit was reached before a plan was found. | U/P |
| `PlannerFailure_NotImplemented` | Planer nie obsługuje tego scenariusza. | The planner does not support this scenario. | U/P |

`DeliveryPlanVerdict` ma wyczerpujące mapowanie `Take` → `PlannerVerdict_Take`,
`Tight` → `PlannerVerdict_Tight`, `Reject` → `PlannerVerdict_Reject`.
`OfferExpired`, `DeliveryWindowMissed`, `InsufficientData` i `StaleSnapshot`
zachowują pierwszeństwo szczegółowego nagłówka. `NoLegalContinuation`,
`CalculationLimitReached` i `NotImplemented` zachowują obecny ogólny nagłówek
odrzucenia, a dokładną przyczynę podaje lista ostrzeżeń.

Wszystkie osiem wartości `DeliveryPlanFailureReason` ma jawną decyzję. `None`
oznacza brak komunikatu i nie otrzymuje sztucznego klucza; pozostałe siedem
wartości mapuje się 1:1 na `PlannerFailure_*`. `NotImplemented` nie ma obecnie
producenta, ale jego klucz jest nazwanym wyjątkiem wyczerpującego pokrycia enuma.

### Fazy harmonogramu

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerPhase_DriveToPickup` | Dojazd po ładunek | Drive to pickup | P |
| `PlannerPhase_Pickup` | Załadunek | Pickup | P |
| `PlannerPhase_DriveWithCargo` | Trasa z ładunkiem | Loaded route | P |
| `PlannerPhase_WaitForDeliveryWindow` | Oczekiwanie na okno dostawy | Waiting for the delivery window | P |
| `PlannerPhase_Unloading` | Rozładunek | Unloading | P |
| `PlannerPhase_PostDeliveryWork` | Praca po dostawie | Post-delivery work | P |
| `PlannerPhase_RegulatoryInterruption` | Przerwa regulacyjna | Regulatory interruption | P |

Siedem kluczy mapuje 1:1 wszystkie wartości `DeliveryPlanPhase`. Faza
`RegulatoryInterruption` zwykle ma dokładniejszy `JourneyPlanSegmentReason`,
ale nadal otrzymuje klucz wymagany dla presentera bez fallbacku.

### Powody segmentów regulacyjnych

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerReason_RemainingRouteDrive` | Pozostała trasa | Remaining route | P |
| `PlannerReason_ContinuousDrivingBreak` | Przerwa po jeździe ciągłej | Continuous-driving break | P |
| `PlannerReason_SplitBreakCompletion` | Dokończenie przerwy dzielonej | Split-break completion | P |
| `PlannerReason_DailyRestDeadline` | Termin odpoczynku dobowego | Daily-rest deadline | P |
| `PlannerReason_DailyDrivingLimit` | Limit jazdy dziennej | Daily driving limit | P |
| `PlannerReason_WeeklyRestRequirement` | Wymagany odpoczynek tygodniowy | Weekly rest required | P |
| `PlannerReason_WeeklyDrivingLimitReached` | Osiągnięty tygodniowy limit jazdy | Weekly driving limit reached | P |
| `PlannerReason_BiweeklyDrivingLimitReached` | Osiągnięty dwutygodniowy limit jazdy | Fortnightly driving limit reached | P |
| `PlannerReason_WaitForNewRegulatoryWeek` | Oczekiwanie na nowy tydzień regulacyjny | Waiting for a new regulatory week | P |
| `PlannerReason_WaitForBiweeklyCapacity` | Oczekiwanie na dostępny limit dwutygodniowy | Waiting for available fortnightly capacity | P |
| `PlannerReason_OperationalBufferAfterArrival` | Praca po przyjeździe | Post-arrival work | P |
| `PlannerReason_CrewDriverChange` | Zmiana kierowcy | Driver change | P |

`segment.RegulatoryReason?.ToString()` nie może trafić do UI. Presenter mapuje
jawnie wszystkie 12 wartości `JourneyPlanSegmentReason`, bez fallbacku.
`SplitBreakCompletion`, `WeeklyDrivingLimitReached` i
`BiweeklyDrivingLimitReached` nie są dziś emitowane przez aktywny silnik załogi;
ich klucze są nazwanymi wyjątkami wyczerpującego pokrycia enuma.

### Ostrzeżenia silnika

| Klucz | Polski | English | Kategoria |
|---|---|---|---|
| `PlannerWarning_IncompleteHistory` | Historia aktywności jest niekompletna; wynik może być ograniczony. | The activity history is incomplete; the result may be limited. | U/P |
| `PlannerWarning_LastSavedState` | Plan oparto na ostatnim zapisanym stanie. | The plan is based on the last saved state. | U/P |
| `PlannerWarning_CompensationModelLimited` | Model rekompensat ogranicza wiarygodność planu. | The compensation model limits the plan's reliability. | U/P |
| `PlannerWarning_ReducedWeeklyRestUnavailable` | Skrócony odpoczynek tygodniowy nie jest dostępny. | Reduced weekly rest is unavailable. | U/P |
| `PlannerWarning_MultiManningPlanningUnsupported` | Ten wariant planowania podwójnej obsady nie jest obsługiwany. | This multi-manning planning variant is not supported. | U/P |
| `PlannerWarning_RegulatoryExceptionUsed` | Plan korzysta z wyjątku regulacyjnego. | The plan uses a regulatory exception. | U/P |

`warning.Code` i angielskie `warning.Context` nie są tekstem użytkowym.
Presenter mapuje wszystkie sześć wartości `JourneyPlanWarningCode` na pełne
komunikaty i nie skleja nazwy enuma z kontekstem. Kontekst pozostaje wyłącznie
w diagnostyce. `CompensationModelLimited` i `RegulatoryExceptionUsed` nie mają
dziś producenta, a `ReducedWeeklyRestUnavailable` powstaje tylko w niepodłączonej
ścieżce pojedynczego kierowcy; trzy klucze są nazwanymi wyjątkami
wyczerpującego pokrycia enuma.

### Typy domenowe bez tekstowego presentera

Sześć typów z rejestru nie dociera do UI jako tekst i zostaje świadomie
wykluczonych:

| Typ | Decyzja |
|---|---|
| `JourneyPlanningMode` | steruje wyborem silnika; aktywna ścieżka przekazuje stałe `MultiManningCrew`, a etykiety użytkowe opisuje `DeliveryPlanningUseCase` |
| `JourneyPlanStatus` | silnik redukuje status do `DeliveryPlanFailureReason`; Desktop nie wyświetla nazwy statusu |
| `JourneyPlanConfidence` | pozostaje metadanym wyniku; aktywne ograniczenia wymagające komunikatu mają osobny `JourneyPlanWarningCode`, brak etykiety poziomu |
| `JourneyPlanSegmentType` | kontrakt niepodłączonego planera jednego kierowcy; aktywny wynik używa `DeliveryPlanPhase`, aktywności i powodu |
| `JourneyPlanWarningSeverity` | wartość sterująca przyszłym stylem, bez tekstu; obecny widok nie rozróżnia stylu |
| `JourneyPlanSnapshotMismatch` | porównanie tożsamości i decyzja o przeliczeniu; szczegół enuma nie jest pokazywany |

Jednowartościowy `JourneyOperationalBufferPolicy.OtherWorkAfterArrival` nie
należy do rejestru presenterów: jest stałą strategii przekazywaną do silnika
i ma klasyfikację T. Widoczną nazwę odpowiadającego segmentu zapewnia
`PlannerReason_OperationalBufferAfterArrival`.

Wykluczenie nie pozwala na przyszłe `ToString()` w UI. Jeżeli którykolwiek typ
otrzyma aktywnego konsumenta tekstowego, wymaga nowej decyzji katalogowej.

Po zatwierdzeniu paczki rejestr 30 typów ma 20 rozstrzygniętych,
10 świadomie wykluczonych i 0 pozostałych.

### Zobowiązania przekazane do UI-09

| Przypadek | Obecne źródło | Decyzja UI-09 |
|---|---|---|
| wyjątek pobrania snapshotu | `JourneyPlannerViewModel.cs:210-215` | ogólny lokalizowany komunikat; wyjątek tylko do diagnostyki |
| wyjątek obliczania planu | `JourneyPlannerViewModel.cs:354-357` | ogólny lokalizowany komunikat; wyjątek tylko do diagnostyki |
| wyjątek odtworzenia wejścia | `JourneyPlannerViewModel.cs:625-628` | ogólny lokalizowany komunikat o użyciu wartości domyślnych |
| wyjątek zapisu wejścia | `JourneyPlannerViewModel.cs:644-647` | ogólny lokalizowany komunikat; bieżące wartości pozostają w pamięci |

Żaden z tych przypadków nie może zachować placeholdera z
`exception.Message`. UI-09 tworzy klucze i wspólną politykę diagnostyczną;
paczka 10 nie tworzy ich z wyprzedzeniem ani nie duplikuje.

### Rozliczenie literałów XAML

W `MainWindow.xaml:352-568` są dokładnie 53 wystąpienia tekstowe i 41
unikalnych wartości:

- 31 wystąpień / 29 unikalnych wartości otrzymuje nowe klucze paczki 10;
- 5 wystąpień ponownie używa `Navigation_Planner`, `Common_From`,
  `Common_To`, `ManualEntry_TimeHeader` i `Common_ReasonHeader`;
- 17 wystąpień jest technicznych: 14 przycisków presetów oraz `#`, `S1`, `S2`;
- powtórzone są wyłącznie tooltipy `Godzina` i `Minuta`, każdy po dwa razy.

Pozostałe 74 nowe klucze pokrywają opcje, pełne nazwy dni, statusy,
formaty wyniku, walidację oraz wyczerpujące presentery aktywnych enumów.

### Nowe dozwolone pary powtórzonych wartości

| Wartość | Klucze | Decyzja |
|---|---|---|
| EN `JOURNEY PLANNER` | `Navigation_Planner`, `Planner_Title` | nawigacja i nagłówek strony mają odmienne zatwierdzone wartości PL (`PLANER` / `PLANER PODRÓŻY`); wspólny klucz zmieniłby polski UI freeze |
| EN `Pickup` | `PlannerField_Pickup`, `PlannerPhase_Pickup` | pole oznacza czas odbioru (`Odbiór`), a faza wykonanie załadunku (`Załadunek`); identyczne EN nie znosi różnicy pojęć PL |

Poza tymi pozycjami 103 nowe klucze nie tworzą identycznych wartości między
różnymi nazwami ani wewnątrz paczki, ani wobec wiążącego katalogu 679 kluczy.

### Elementy świadomie bez lokalizacji

| Element | Kategoria | Uzasadnienie |
|---|---|---|
| `S1`, `S2`, nazwy i numery kart | T/O | role techniczne i dane użytkownika |
| minuty gry, `GameTime`, identyfikatory i generacja snapshotu | T | dane domenowe i kontrola świeżości |
| `HH:MM`, `D{0}`, `+`/`−`, `—`, `#` | T | wspólny format techniczny |
| `15m`, `30m`, `1h`, `2h`, wartości komend | T | wejście i parametry komend |
| kolory werdyktu i kody ostrzeżeń | T | sterowanie prezentacją / diagnostyka |
| limity segmentów, czasu i odwiedzonych stanów | T | bezpieczniki algorytmu |
| `exception.Message`, `warning.Context` | D | szczegół wyłącznie w logu |

### Ryzyka i kontrola wizualna

Planer jest ekranem wysokiego ryzyka EN: konfiguracja używa stałych szerokości
115–240 px, tabela ma osiem kolumn, a prawa karta ma ograniczoną szerokość.
Kontrola M5.3 obejmuje:

- pełne etykiety `OFFER EXPIRES IN`, `DELIVERY WINDOW FROM/TO`,
  `“TIGHT” THRESHOLD` i `WARNINGS AND LIMITATIONS`;
- oba tryby, oba sloty i siedem pełnych nazw dni;
- poprawny separator dziesiętny w instrukcji PL/EN przy parserze akceptującym
  oba warianty;
- wszystkie siedem faz i 12 powodów, w tym najdłuższe wartości EN;
- pustą listę, kilka segmentów oraz limit wysokości przy wielu ostrzeżeniach;
- długie nazwy obu kierowców w `PlannerCrew_CrewFormat`;
- brak surowych nazw enumów i tekstów wyjątków.

Zmiana szerokości, zawijania albo tooltipu jest dozwolona tylko jako korekta
przepełnienia zgodna z UI freeze. Nie wolno zmieniać kolejności pól ani
przepływu obliczania.

### Kontrola paczki

- [x] wszystkie 53 wystąpienia XAML mają klucz, jawne ponowne użycie albo
  klasyfikację techniczną;
- [x] wszystkie widoczne literały `JourneyPlannerViewModel` i dwa komunikaty
  gotowości usługi mają klucz albo jawnego właściciela UI-09;
- [x] wszystkie 6 gałęzi istniejącego presentera `DriverActivity` porównano
  wartość po wartości z projektowanymi zasobami; aktywne cztery wartości
  zachowują dzisiejszą polską prezentację;
- [x] `DeliveryPlanningUseCase`, `DeliveryPlanVerdict`,
  `DeliveryPlanFailureReason`, `DeliveryPlanPhase`,
  `JourneyPlanSegmentReason` i `JourneyPlanWarningCode` mają wyczerpujące
  mapowania bez `ToString()` i fallbacku tekstowego;
- [x] sześć pozostałych typów Planera ma udowodniony brak konsumenta
  tekstowego i jawną decyzję o wykluczeniu;
- [x] wszystkie formaty mają zgodne zbiory placeholderów PL/EN;
- [x] sprawdzono cały wiążący katalog 679 kluczy przed utworzeniem nowych nazw;
- [x] dwie nowe pary powtórzonych wartości są jawne i uzasadnione;
- [x] dane, algorytm, kontrakty i reguły prawne pozostają bez zmian;
- [x] brak zmian w kodzie i XAML.

### Punkt kontrolny po GO

- 29 nowych kluczy statycznego XAML;
- 9 nowych kluczy slotów i pełnych nazw dni;
- 14 nowych kluczy gotowości, statusu i pochodzenia wejścia;
- 8 nowych kluczy wyników i podsumowania;
- 6 nowych kluczy walidacji;
- 5 dodatkowych kluczy werdyktu (`PlannerVerdict_UnreliableData` policzony
  w statusach);
- 7 kluczy przyczyn przerwania;
- 7 kluczy faz;
- 12 kluczy powodów regulacyjnych;
- 6 kluczy ostrzeżeń;
- 103 nowe klucze;
- 782 unikalne nazwy globalnie;
- 2 nowe i 20 globalnych dozwolonych par powtórzonych wartości;
- 20 typów rozstrzygniętych, 10 wykluczonych, 0 pozostałych po GO;
- 0 zmian wykonawczych.

### Werdykt

**GO — paczka 10 zatwierdzona.** Zamknięta bez pozycji otwartych. `UI-08`
jest zamknięte, a łączny, wiążący katalog paczek 1–10 zawiera 782 unikalne
nazwy i 20 jawnie dozwolonych par powtórzonych wartości. Rejestr presenterów
obejmuje 30 typów: 20 rozstrzygniętych, 10 świadomie wykluczonych i 0 otwartych.

## Słownik domenowy PL/EN — część 1

Terminologia angielska opiera się na oficjalnym brzmieniu rozporządzenia
(WE) nr 561/2006 oraz materiałach Komisji Europejnych dotyczących czasu jazdy,
odpoczynków i tachografów:

- <https://eur-lex.europa.eu/eli/reg/2006/561/2024-05-22>
- <https://transport.ec.europa.eu/transport-modes/road/social-provisions/driving-time-and-rest-periods_en>
- <https://transport.ec.europa.eu/transport-modes/road/tachograph_en>

Słownik jest źródłem terminologii, nie rejestrem kluczy zasobów. Nazwy kluczy
i ich role prezentacyjne są wiążące wyłącznie w zatwierdzonych katalogach paczek.

| Polski | English | Uwagi |
|---|---|---|
| Jazda | Driving | aktywność tachografowa |
| Inna praca | Other work | termin oficjalny UE |
| Dyspozycyjność | Availability | w opisie pełnym: period of availability |
| Przerwa / odpoczynek | Break / rest | wspólna aktywność UI; prawnie dwa odrębne pojęcia |
| Nieznana | Unknown | jawny stan prezentacyjny, bez nazwy enuma jako fallbacku |
| Jazda ciągła | Continuous driving | licznik od ostatniej kwalifikowanej przerwy |
| Do przerwy | Time to break | krótka etykieta UI |
| Jazda dzienna | Daily driving | czas jazdy |
| Praca dobowa | Daily duty | etykieta produktu dla okna pracy |
| Odpoczynek dobowy | Daily rest period | termin oficjalny UE |
| Regularny odpoczynek dobowy | Regular daily rest period | termin oficjalny UE |
| Skrócony odpoczynek dobowy | Reduced daily rest period | termin oficjalny UE |
| Odpoczynek tygodniowy | Weekly rest period | termin oficjalny UE |
| Regularny odpoczynek tygodniowy | Regular weekly rest period | termin oficjalny UE |
| Skrócony odpoczynek tygodniowy | Reduced weekly rest period | termin oficjalny UE |
| Podwójna obsada | Multi-manning | termin używany przez Komisję Europejską |
| Pojedyncza obsada | Single driver | czytelniejsze niż nieoficjalne single-manning |
| Kierowca aktywny | Active driver | rola prezentacyjna |
| Kierowca zmiennik | Co-driver | rola prezentacyjna |
| Rekompensata | Compensation | rekompensata skróconego odpoczynku |
| Zobowiązanie | Compensation obligation | pojęcie produktu, nie identyfikator techniczny |
| Pozostały dług | Remaining compensation due | tekst użytkowy; pole maszynowe bez zmian |
| Termin | Due date | prezentacja terminu |
| Luka aktywności | Activity gap | pojęcie produktu |
| Luka nierozliczona | Unresolved activity gap | stan prezentacyjny |
| Wpis manualny | Manual entry | termin tachografowy |

## Polityka placeholderów

- para PL/EN dla tego samego klucza musi mieć identyczny zbiór placeholderów;
- placeholdery są pozycyjne: `{0}`, `{1}`, bez sklejania fragmentów zdań;
- wartości techniczne są formatowane przed przekazaniem albo z
  `InvariantCulture`, jeśli należą do kontraktu maszynowego;
- test kompletności ma porównywać również powtórzenia i indeksy placeholderów;
- produkt, identyfikatory, ISO, `S1`, `S2`, `OUT`, `HH:MM` i `Dxxx HH:mm`
  pozostają wartościami technicznymi lub własnymi, nie tłumaczeniami.

## Wartości domenowe wymagające presenterów

Nie wolno lokalizować przez zmianę nazw enumów ani przez zapisywanie
przetłumaczonych wartości. Rejestr obejmuje 30 typów: 20 rozstrzygniętych,
10 świadomie wykluczonych z prezentacji tekstowej i 0 pozostałych.

| Typ | Status | Paczka / decyzja |
|---|---|---|
| `DriverActivity` | rozstrzygnięty | paczki 2 i 3 — presentery Dashboardu i LCD |
| `ActivitySource` | rozstrzygnięty | paczka 4 |
| `ActivityGapReason` | rozstrzygnięty | paczka 4 |
| `ActivityGapState` | rozstrzygnięty | paczka 4 |
| `SpecialCondition` | rozstrzygnięty | paczka 4 |
| `GameWeekday` | rozstrzygnięty | paczki 3 i 5 |
| `GameDeadlineSemantic` | rozstrzygnięty | paczki 3 i 5 |
| `WeeklyRestCompensationStatus` | rozstrzygnięty | paczka 7 — wyczerpujące mapowanie do DTO |
| `WeeklyRestCompensationStatusDto` | rozstrzygnięty | paczki 2, 3 i 7 |
| `DailyRestClassification` | rozstrzygnięty | paczka 4 |
| `WeeklyRestClassification` | rozstrzygnięty | paczka 4 |
| `RestAllocationPurpose` | rozstrzygnięty | paczka 7 |
| `ViolationType` | rozstrzygnięty | paczka 2 |
| `ManualEntryError` | rozstrzygnięty | paczka 4 |
| `RestAllocationDecisionStatus` | świadomie wykluczony | paczka 7 — sterowanie audytem, bez tekstu UI |
| `ResolveGapStatus` | świadomie wykluczony | paczka 4 — sterowanie przepływem |
| `ManualEntryPersistenceStatus` | świadomie wykluczony | paczka 4 — sterowanie przepływem |
| `RuleFindingLevel` | świadomie wykluczony | paczka 8 — brak aktywnego konsumenta tekstowego w UI-06 |
| `DeliveryPlanningUseCase` | rozstrzygnięty | paczka 10 — dwie opcje trybu |
| `DeliveryPlanVerdict` | rozstrzygnięty | paczka 10 — wyczerpujący presenter werdyktu |
| `DeliveryPlanFailureReason` | rozstrzygnięty | paczka 10 — 7 komunikatów i jawne `None` |
| `DeliveryPlanPhase` | rozstrzygnięty | paczka 10 — 7 faz |
| `JourneyPlanningMode` | świadomie wykluczony | paczka 10 — sterowanie wyborem silnika |
| `JourneyPlanStatus` | świadomie wykluczony | paczka 10 — redukcja do `DeliveryPlanFailureReason` |
| `JourneyPlanConfidence` | świadomie wykluczony | paczka 10 — metadane; ostrzeżenia mają osobne kody |
| `JourneyPlanSegmentType` | świadomie wykluczony | paczka 10 — brak konsumenta w aktywnym UI |
| `JourneyPlanSegmentReason` | rozstrzygnięty | paczka 10 — 12 powodów |
| `JourneyPlanWarningCode` | rozstrzygnięty | paczka 10 — 6 komunikatów |
| `JourneyPlanWarningSeverity` | świadomie wykluczony | paczka 10 — sterowanie stylem bez tekstu |
| `JourneyPlanSnapshotMismatch` | świadomie wykluczony | paczka 10 — kontrola świeżości bez tekstu |

Miejsca fallbacku oparte na `ToString()` istnieją obecnie w Desktop i Reports.
W M5 muszą zostać zastąpione jawnym presenterem dla tekstu użytkowego.
Po każdej paczce rozstrzygającej typ należy zaktualizować jego status i numer
paczki; zamknięcie M5.1 wymaga zera pozycji `pozostały`.

## Kontrakty bez lokalizacji

| Kontrakt | Reguła |
|---|---|
| JSON VTC i pola JSON | nazwy pól, wartości techniczne i format liczb bez zmian |
| techniczny CSV | nagłówki kontraktowe i format danych bez zmian |
| `.tacho` | format i wartości bez zmian |
| SQLite | tabele, kolumny, enumy i zapisane identyfikatory bez zmian |
| telemetria v3 | protokół, mapy pamięci i kody bez zmian |
| identyfikatory | `ObligationId`, `RestBlockId`, `CandidateId`, numery kart bez zmian |
| kody | ISO, kody tachografowe, kody naruszeń i błędów bez zmian |
| diagnostyka | nazwy zdarzeń i treści przeznaczone wyłącznie dla logu bez lokalizacji |
| czas i liczby maszynowe | `InvariantCulture`; format czasu trwania `HH:MM` |

## Ekrany o podwyższonym ryzyku przepełnień

| Priorytet | Powierzchnia | Ryzyko |
|---|---|---|
| wysokie | boczna nawigacja i nagłówki zakładek | stała szerokość 195 px i dłuższe nazwy EN |
| wysokie | Planer — konfiguracja i tabela harmonogramu | wiele kolumn, stałe etykiety i długie powody regulacyjne |
| wysokie | wpis manualny i dialog kraju | modalne szerokości, komunikaty walidacji i przyciski |
| wysokie | Rekompensaty i szczegóły zobowiązania | długie statusy i terminy |
| wysokie | PDF | szerokości kolumn, podział stron i dłuższe nagłówki EN |
| średnie | Dashboard i wirtualny tachograf | mały LCD i zwarte karty statusu |
| średnie | Raporty | zakładki, kafelki oraz ostrzeżenia o lukach |
| średnie | nakładki S1/S2 | stała szerokość 340 px |
| średnie | Ustawienia | komunikat o restarcie i nowy wybór języka |

Zmiany rozmiarów są dopuszczalne wyłącznie jako usunięcie przepełnienia zgodnie
z polityką UI freeze; nie mogą reorganizować przepływów.

## Decyzje architektoniczne wejścia

- neutralny zasób i fallback: `en-GB`;
- jawny satelicki zestaw: `pl-PL`;
- brak zapisanego wyboru: polski system → `pl-PL`, pozostałe → `en-GB`;
- wartość nieobsługiwana lub uszkodzona → `en-GB`;
- zmiana języka obowiązuje po restarcie;
- PDF używa języka aktywnego UI;
- nazwy krajów są lokalizowane, lecz ISO pozostaje źródłem prawdy;
- kontrakty maszynowe zawsze używają `InvariantCulture`.

## Pozostałe kroki M5.1

- [ ] rozpisać kandydatów U/P na stabilne klucze semantyczne;
- [ ] oddzielić literały D/T/O w plikach C#;
- [ ] zatwierdzić pełny słownik domenowy PL/EN;
- [ ] sprawdzić zgodność placeholderów projektowanych formatów;
- [ ] zamknąć M5.1 formalnym wynikiem GO przed dodaniem `.resx`.
