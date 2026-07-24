# PLAN BETA.12 — TABLICA STATUSU M0–M8

**Projekt:** ETS2 EU Digital Tachograph
**Baza wydaniowa:** `0.1.0-beta.11.1` (GO 2026-07-23)
**Cel:** `0.1.0-beta.12` — ostatnia beta przed pierwszą szeroką publikacją.
**Źródło etapów:** `docs/PLAN_BETA_12_M0-M8/` (integralność SHA-256 potwierdzona z `MANIFEST_SHA256.md`).
**Ostatnia aktualizacja tablicy:** 2026-07-24

> Ten plik jest żywym pulpitem postępu. Nie zmienia zakresu ani gate'ów planu
> nadrzędnego — odzwierciedla tylko stan realizacji. Reguła przejścia: następny
> etap startuje dopiero po formalnym **GO** poprzedniego; P0/P1 blokują przejście.

## Skrót łańcucha

| Etap | Zakres | Status | Wynik | Blokuje |
|---|---|---|---|---|
| **M0** | Stabilizacja stanu wejściowego | 🟡 W TOKU | — | M1 |
| M1 | Planer: kontrakty i czerwone testy | ⚪ nie rozpoczęty | — | M2 |
| M2 | Planer: silnik zdarzeniowy | ⚪ nie rozpoczęty | — | M3 |
| M3 | Planer: Application Service i UI | ⚪ nie rozpoczęty | — | M4 |
| M4 | Finalizacja UI + **UI freeze** | ⚪ nie rozpoczęty | — | M5 |
| M5 | Lokalizacja PL/EN | ⚪ nie rozpoczęty | — | M6 |
| M6 | Release Candidate `0.1.0-beta.12` | ⚪ nie rozpoczęty | — | M7 |
| M7 | Końcowy smoke beta.12 | ⚪ nie rozpoczęty | — | M8 |
| M8 | Publikacja | ⚪ nie rozpoczęty | — | — |

Legenda: ⚪ nie rozpoczęty · 🟡 w toku · 🟢 GO · 🔴 FIX/HOLD

---

## M0 — Stabilizacja stanu wejściowego (AKTYWNY)

**Kryterium wejścia:** baza beta.11.1, lokalny gate zielony, zielona regresja
granicy pauzy 44/45. **Stan:** spełnione (gate 315/315, build Release 0/0,
regresja `41+3=44` zielona 2026-07-24).

### Zadania i stan

| Zadanie M0 | Stan | Uwaga |
|---|---|---|
| `KNOWN_ISSUES.md`: 44/45 naprawione lokalnie 2026-07-24 | ✅ zrobione | Sekcja „Naprawione lokalnie po beta.11.1" + ref. `41+3=44` + gate 315/315 |
| `BETA_TEST_PLAN.md`: Test 1A zaliczony, scenariusz `41+3=44` | 🟡 w edycji | Redagowane w tym oknie |
| Pełna checklista UI bieżącego drzewa | ⚪ do wykonania | Manualna, obszar WPF Desktop |
| Wariant B wpisu manualnego | ⚪ do wykonania | |
| Katalog krajów + kod tachografowy | ⚪ do wykonania | |
| `ODP. TYG.` na progach 89:39 / 96:00 / 144:00+ | ⚪ do wykonania | |
| Oba sloty, nakładki, OUT, Prom, restart, logi | ⚪ do wykonania | |
| Aktywna telemetria, auto-Jazda, blokady od ruchu | ⚪ do wykonania | |
| Eksporty i zachowanie danych po restarcie | ⚪ do wykonania | |
| Inwentaryzacja pozostałych elementów UI (`beta.12` / `poza zakresem`) | ⚪ do wykonania | Wejście do M4 |

### Gate M0 (do zamknięcia)

- [ ] pełna regresja lokalnego drzewa zielona
- [x] brak otwartych P0/P1 (stan na 2026-07-24)
- [ ] wszystkie znane rozbieżności opisane
- [x] build Release 0/0
- [x] pełny pakiet testów automatycznych zielony (315/315)
- [ ] kontrolowany punkt przywracania (commit) po zamknięciu docs + checklisty

### Otwarte pozycje / ryzyka

- Residualny **manualny test wizualny 44→45 w grze** — regresja automatyczna
  `41+3=44` jest zielona; pełne potwierdzenie in-game granicy 44/45 jest też
  wymagane w smoke M7.
- Rozbieżność nazewnicza: dokument M0 opisuje bazę jako „310/310" (migawka sprzed
  hotfixa), bieżące drzewo to 315/315 po dołożeniu testów granicy 44/45.

### Szablon zamknięcia M0

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** —
- **Wynik:** — (`GO` / `FIX` / `HOLD`)
- **Commit / punkt przywracania:** —
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 315/315
- **Testy manualne / dowody:** checklista UI — w toku
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** —

---

## M1–M8 — do rozpoczęcia

Każdy etap otwieramy dopiero po **GO** poprzedniego. Szczegółowe zadania i gate'y
w `docs/PLAN_BETA_12_M0-M8/`. Poniżej rejestr decyzji wypełniany przy zamykaniu
kolejnych etapów (szablon: data start/koniec, wynik, commit, build, testy auto,
dowody manualne, P0, P1, uwagi).

- **M1** — kontrakty request/result/status + czerwone testy `JP-P0-01–08`,
  `JP-ST-01–08`; brak zależności od WPF i zapisu do SQLite. — *nie rozpoczęty*
- **M2** — deterministyczny silnik „Najwcześniejsza legalna", niezmienniki,
  kontrolowane zakończenie. — *nie rozpoczęty*
- **M3** — `JourneyPlannerService` + ViewModel + zakładka PLANER, unieważnianie
  wyniku. — *nie rozpoczęty*
- **M4** — domknięcie inwentaryzacji UI + formalny **UI freeze**. — *nie rozpoczęty*
- **M5** — pełne `pl-PL` i `en-GB`, zielone regresje obu języków. — *nie rozpoczęty*
- **M6** — niezmienny RC beta.12: numer + commit + SHA-256, ZIP zamrożony. — *nie rozpoczęty*
- **M7** — smoke na rozpakowanym ZIP-ie (istniejąca + czysta baza) → GO/FIX/HOLD. — *nie rozpoczęty*
- **M8** — publikacja dokładnie artefaktu z GO + checksuma + dokumentacja PL/EN. — *nie rozpoczęty*
