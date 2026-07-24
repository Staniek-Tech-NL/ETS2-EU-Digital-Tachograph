# M3.5 — Etap 0: audyt istniejącego przepływu Raportów

**Data:** 2026-07-24
**Baza:** commit `bd76999` po formalnym GO M3
**Wynik:** **PASS — Etap 1 odblokowany**

## Jeden przepływ generowania

Aktualna ścieżka:

```text
profil / karta + opcjonalne OD/DO
→ MainViewModel.RefreshReportAsync
→ ReportService.CreateAsync
→ jeden ReportDto
→ kafle, ostrzeżenia i tabela
```

`ReportService.CreateAsync` ładuje kanoniczną historię i nierozliczone luki,
przycina rekordy do badanego zakresu, wylicza sumy z przyciętych rekordów i
dołącza wynik istniejącego analizatora regulacyjnego. UI nie powinno powtarzać
tych obliczeń.

## Semantyka zakresu

- zakres jest półotwarty: `[from, toExclusive)`;
- `null` dla początku oznacza pierwszą znaną minutę aktywności albo luki;
- `null` dla końca oznacza ostatni znany koniec aktywności albo zamkniętej luki;
- dotychczasowy Desktop podstawia bieżący `game_time` jako koniec, jeżeli pole
  `DO` jest puste;
- brak historii daje techniczne `0–0`; wariant B ma zamienić to na jawny pusty
  stan i zablokować eksport;
- tabela korzysta z kanonicznych `ActivityRecord`, które są blokami aktywności,
  a nie z osobnego DTO ani z surowej tabeli minutowej.

## Jeden przepływ eksportu

Aktualna ścieżka:

```text
ExportReportAsync
→ RefreshReportAsync
→ jeden nowy ReportDto
→ dialog zapisu
→ wybrany eksporter przyjmuje ten sam ReportDto
```

PDF, VTC JSON i CSV zobowiązań nie wykonują drugiego zapytania raportowego.
Surowy CSV przyjmuje `ReportDto` jako tożsamość karty i zakresu, a następnie
ładuje surową historię dokładnie dla tych granic. Wariant B zachowuje tę
semantykę i centralizuje odświeżenie przed eksportem.

## Dostępne dane kompletności

`ReportDto` udostępnia bezpośrednio:

- `UnresolvedGapCount`, `GapMinutes`;
- `TotalMinutes`, `CoveredMinutes`, `RangeMinutes`;
- `CoverageMatchesRange`, `EvidenceComplete`;
- `PendingRestAllocation`;
- naruszenia i pełne zobowiązania rekompensacyjne.

Kafle i status kompletności mogą więc być czystą projekcją DTO.

## CSV zobowiązań

CSV eksportuje dokładnie `ReportDto.CompensationObligations`, po jednym wierszu
na zobowiązanie. Kolekcja pochodzi z analizy wykonanej dla rekordów badanego
zakresu. M3.5 nie zmienia jej filtrowania ani pól.

## Wirtualizacja

Obecny `DataGrid` nie wyłącza wirtualizacji WPF, ale pokazuje techniczne kolumny
domyślnie. Wariant B zachowa wirtualizację i domyślnie pokaże czytelne bloki,
pozostawiając źródło i warunek za przełącznikiem.

## Decyzja implementacyjna

Powstaje jeden `ReportsWorkspaceViewModel`. `MainViewModel` zachowuje nawigację,
dialogi plikowe i istniejące serwisy. Workspace odpowiada za parametry, presety,
walidację, stan podglądu i projekcję `ReportDto`. Eksport zawsze:

1. kopiuje aktualny draft;
2. generuje jeden nowy `ReportDto`;
3. aktualizuje podgląd;
4. przekazuje ten sam obiekt do eksportu.

Nie zmieniamy `RuleEngine`, historii, SQLite, PDF ani kontraktów JSON/CSV.
