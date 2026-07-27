# M5.1 — Inwentaryzacja tekstów lokalizacji PL/EN

**Projekt:** ETS2 EU Digital Tachograph

**Data rozpoczęcia:** 2026-07-27

**Status:** **W TOKU**

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
| UI-01 | Powłoka, tytuł, nawigacja i wspólne akcje | `MainWindow.xaml`, `App.xaml.cs` | U | `UiStrings.Common_*`, `UiStrings.Navigation_*` | do rozpisania na klucze |
| UI-02 | Dashboard i wirtualny tachograf | `MainWindow.xaml`, `MainViewModel.cs` | U/P | zasoby + presentery aktywności, trybów i stanu kart | do rozpisania |
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
