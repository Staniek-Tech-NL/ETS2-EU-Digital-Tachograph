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
| **M0** | Stabilizacja stanu wejściowego | 🟢 GO | GO | M1 |
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

## M0 — Stabilizacja stanu wejściowego (ZAMKNIĘTY — GO)

**Kryterium wejścia:** baza beta.11.1, lokalny gate zielony, zielona regresja
granicy pauzy 44/45. **Stan:** spełnione (gate 338/338, build Release 0/0,
regresja `41+3=44` zielona 2026-07-24).

### Zadania i stan

| Zadanie M0 | Stan | Uwaga |
|---|---|---|
| `KNOWN_ISSUES.md`: 44/45 naprawione lokalnie 2026-07-24 | ✅ zrobione | Sekcja „Naprawione lokalnie po beta.11.1" + ref. `41+3=44` + gate 315/315 |
| `BETA_TEST_PLAN.md`: Test 1A zaliczony, scenariusz `41+3=44` | ✅ zrobione | Regresja automatyczna i procedura powtórzenia opisana |
| Pełna checklista UI bieżącego drzewa | ✅ zrobione | Wynik ręczny użytkownika 10/10 zielony, 2026-07-24 |
| Wariant B wpisu manualnego | ✅ zrobione | Ujęty w ręcznym wyniku 10/10 |
| Katalog krajów + kod tachografowy | ✅ zrobione | Ujęty w ręcznym wyniku 10/10 |
| `ODP. TYG.`: `1/6–6/6+` + termin `Dxxx HH:mm` | ✅ zrobione | WRF-01–16, gate 338/338 i ręczny smoke LCD S1/S2 zielone |
| Oba sloty, nakładki, OUT, Prom, restart, logi | ✅ zrobione | Ujęte w ręcznym wyniku 10/10 |
| Aktywna telemetria, auto-Jazda, blokady od ruchu | ✅ zrobione | Ujęte w ręcznym wyniku 10/10 |
| Eksporty i zachowanie danych po restarcie | ✅ zrobione | Ujęte w ręcznym wyniku 10/10 |
| Inwentaryzacja pozostałych elementów UI (`beta.12` / `poza zakresem`) | ✅ zrobione | Wynik ręczny użytkownika zielony, 2026-07-24; wejście do M4 gotowe |

### Gate M0 (do zamknięcia)

- [x] pełna regresja lokalnego drzewa zielona (338/338)
- [x] brak otwartych P0/P1
- [x] wszystkie znane rozbieżności opisane
- [x] build Release 0/0
- [x] pełny pakiet testów automatycznych zielony (338/338)
- [x] kontrolowany punkt przywracania: commit hotfixu ODP.TYG. po zielonym smoke teście

### Otwarte pozycje / ryzyka

- Residualny **manualny test wizualny 44→45 w grze** — regresja automatyczna
  `41+3=44` jest zielona; pełne potwierdzenie in-game granicy 44/45 jest też
  wymagane w smoke M7.
- Rozbieżność nazewnicza: dokument M0 opisuje bazę jako „310/310" (migawka sprzed
  hotfixów), bieżące drzewo to 338/338 po regresjach granicy 44/45 i WRF-01–16.
- Hotfix P1 `ODP. TYG.` zamknięty: gate automatyczny i ręczny smoke LCD S1/S2
  z aktywną telemetrią są zielone.

### Szablon zamknięcia M0

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `50ee50a` — hotfix ODP.TYG.
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 338/338
- **Testy manualne / dowody:** checklista UI 10/10 zielona; smoke LCD S1/S2 po hotfixie zielony; inwentaryzacja UI zielona
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M1 może wystartować; inwentaryzacja UI jest gotowa jako wejście do M4.

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
- **M4** — walidacja gotowej inwentaryzacji UI + formalny **UI freeze**. — *nie rozpoczęty*
- **M5** — pełne `pl-PL` i `en-GB`, zielone regresje obu języków. — *nie rozpoczęty*
- **M6** — niezmienny RC beta.12: numer + commit + SHA-256, ZIP zamrożony. — *nie rozpoczęty*
- **M7** — smoke na rozpakowanym ZIP-ie (istniejąca + czysta baza) → GO/FIX/HOLD. — *nie rozpoczęty*
- **M8** — publikacja dokładnie artefaktu z GO + checksuma + dokumentacja PL/EN. — *nie rozpoczęty*
