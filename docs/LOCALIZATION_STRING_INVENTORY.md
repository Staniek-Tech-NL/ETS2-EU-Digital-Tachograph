# M5.1 — Inwentaryzacja tekstów lokalizacji PL/EN

**Projekt:** ETS2 EU Digital Tachograph

**Data rozpoczęcia:** 2026-07-27

**Status:** **W TOKU — PACZKI 1–2 GO**

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

## Rejestr obszarów użytkowych

| ID | Obszar | Źródła | Kategoria | Docelowa obsługa | Stan |
|---|---|---|---|---|---|
| UI-01 | Powłoka, tytuł, nawigacja i wspólne akcje | `MainWindow.xaml`, `App.xaml.cs`, `MainViewModel.cs` | U/T/D/O | `UiStrings.Common_*`, `UiStrings.Navigation_*`, `UiStrings.Shell_*` | GO — katalog wiążący |
| UI-02 | Dashboard i wirtualny tachograf | `MainWindow.xaml`, `MainViewModel.cs` | U/P | zasoby + presentery aktywności, trybów i stanu kart | Dashboard GO w paczce 2; urządzenie pozostaje do paczki 3 |
| UI-03 | Historia, luki i wpis manualny | `MainWindow.xaml`, `MainViewModel.cs`, `ManualEntryPlanEditor.cs` | U/P | zasoby + presentery aktywności, przyczyn i stanów luk | do rozpisania |
| UI-04 | Kraje i kody tachografowe | `CountryCatalog.cs`, JSON | U/T | osobne nazwy PL/EN; zapis nadal przez ISO | do rozpisania |
| UI-05 | Rekompensaty | `MainWindow.xaml`, `CompensationPresentation.cs` | U/P/T | zasoby + presenter statusu; identyfikatory bez zmian | do rozpisania |
| UI-06 | Raporty w Desktop | `MainWindow.xaml`, `ReportsWorkspaceViewModel.cs` | U/P/T | zasoby + presentery; formaty eksportu bez zmian | do rozpisania |
| UI-07 | Kierowcy i Ustawienia | `MainWindow.xaml`, `MainViewModel.cs`, `SettingsService.cs` | U/O/T | zasoby; nazwy i numery kart bez tłumaczenia | do rozpisania |
| UI-08 | Planer | `MainWindow.xaml`, `JourneyPlannerViewModel.cs` | U/P/T | zasoby + presentery faz, powodów, statusów i ostrzeżeń | do rozpisania |
| UI-09 | Dialogi, potwierdzenia i komunikaty błędów | `App.xaml.cs`, `MainViewModel.cs`, view-modele | U/D/T | tekst UI w zasobach; logi i kody bez zmian | do rozpisania |
| UI-10 | Nakładki S1/S2 | `OverlayWindow.xaml`, `OverlayViewModel.cs` | U/P/T | zasoby; `S1`, `S2`, `HH:MM` bez zmian | do rozpisania |
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
przetłumaczonych wartości. Presentery muszą objąć co najmniej:

- `DriverActivity`, `ActivitySource`, `ActivityGapReason`, `ActivityGapState`;
- `SpecialCondition`, `GameWeekday`, `GameDeadlineSemantic`;
- `WeeklyRestCompensationStatus`, `WeeklyRestCompensationStatusDto`;
- `DailyRestClassification`, `WeeklyRestClassification`;
- `RestAllocationPurpose`, `RestAllocationDecisionStatus`;
- `ViolationType`, `RuleFindingLevel`;
- `ResolveGapStatus`, `ManualEntryError`, `ManualEntryPersistenceStatus`;
- `DeliveryPlanningUseCase`, `DeliveryPlanVerdict`,
  `DeliveryPlanFailureReason`, `DeliveryPlanPhase`;
- `JourneyPlanningMode`, `JourneyPlanStatus`, `JourneyPlanConfidence`,
  `JourneyPlanSegmentType`, `JourneyPlanSegmentReason`,
  `JourneyPlanWarningCode`, `JourneyPlanWarningSeverity`,
  `JourneyPlanSnapshotMismatch`.

Miejsca fallbacku oparte na `ToString()` istnieją obecnie w Desktop i Reports.
W M5 muszą zostać zastąpione jawnym presenterem dla tekstu użytkowego.

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
