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
| **M1** | Planer: kontrakty i czerwone testy | 🟢 GO | GO | M2 |
| **M2** | Planer: silnik zdarzeniowy | 🟢 GO | GO | M3 |
| **M3** | Planer: Application Service i UI | 🔴 HOLD — test ręczny | HOLD | M4 |
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

## M1 — Planer: kontrakty i czerwone testy (ZAMKNIĘTY — GO)

**Kryterium wejścia:** formalny wynik GO dla M0. **Stan:** spełnione.

### Zadania i stan

| Zadanie M1 | Stan | Uwaga |
|---|---|---|
| Zatwierdzenie specyfikacji 2.2 | ✅ zrobione | Zatwierdzona dla beta.12; doprecyzowano `DriverActivity` i `MultiManningActive` |
| Kontrakty Planera | ✅ zrobione | Request, result, status, confidence, segmenty, warningi, usage i limity |
| Snapshot i tożsamość | ✅ zrobione | Porównanie sesji, świata, czasu, karty i high-water mark |
| Okno odpoczynku dobowego | ✅ zrobione | Osobny termin ukończenia oraz start wariantu 9/11 h |
| `JP-P0-01–08` | ✅ zrobione | Osiem testów blokujących M2 |
| `JP-ST-01–08` | ✅ zrobione | Pełny kontrakt ośmiu statusów terminalnych |
| Determinizm i limity bezpieczeństwa | ✅ zrobione | Czerwone testy postępu, determinizmu i trzech limitów |
| Izolacja od UI i persistence | ✅ zrobione | Brak referencji WPF, EF Core, SQLite i Infrastructure |

### Gate M1

- [x] kontrakty zaakceptowane
- [x] testy blokujące istnieją i prawidłowo zawodzą bez implementacji M2
- [x] brak zależności Planera od WPF
- [x] brak operacji zapisu do SQLite w kontrakcie Planera

### Zamknięcie M1

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `879eda5` — kontrakty i czerwone testy M1
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 355/355 regresji i kontraktów; 13/13 testów
  `Stage=M2Red` prawidłowo czerwonych na celowej granicy M2
- **Testy manualne / dowody:** nie dotyczy — brak zmian UI
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M2 może rozpocząć implementację silnika i
  zazielenianie czerwonego pakietu.

---

## M2 — Planer: silnik zdarzeniowy (ZAMKNIĘTY — GO)

**Kryterium wejścia:** zaakceptowane kontrakty i czerwone testy M1.
**Stan:** spełnione przez commit `879eda5`.

### Gate M2

- [x] wszystkie testy `JP-P0-01–08` zielone
- [x] testy algorytmu, statusów, snapshotu i limitów zielone
- [x] deterministyczny wynik dla identycznego requestu
- [x] kontrolowane `NoLegalContinuation` i `CalculationLimitReached`
- [x] deduplikacja stanu i dodatni postęp każdego segmentu
- [x] brak zapisu do SQLite i brak mutacji wejściowego `RegulationState`

### Zamknięcie M2

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `751cd07` — deterministyczny silnik zdarzeniowy M2
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 385/385
- **Testy manualne / dowody:** nie dotyczy — brak zmian UI
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M3 może rozpocząć warstwę Application i UI.

---

## M3 — Planer: Application Service i UI (IMPLEMENTACJA ZAKOŃCZONA — HOLD)

**Kryterium wejścia:** zielony silnik M2. **Stan:** spełnione przez commit
`751cd07`.

### Gate M3

- [x] przepływ formularz → `JourneyPlannerService` → harmonogram działa
- [x] atomowy snapshot korzysta z kanonicznej historii, luk i stanu regulacyjnego
- [x] zmiana stanu unieważnia poprzedni wynik
- [x] wybór S1/S2 nie aktywuje sam podwójnej obsady
- [x] brak zapisu hipotetycznego planu do historii
- [x] build Release i pełna regresja automatyczna są zielone
- [x] kontrolny smoke wizualny zakładki przy 1280×800 jest zielony
- [ ] końcowy test ręczny użytkownika

### Stan zamknięcia M3

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia implementacji:** 2026-07-24
- **Wynik:** **HOLD** — użytkownik wykona końcowy test ręczny
- **Commit / punkt przywracania:** `e8efc61` — implementacja Application Service i UI M3
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 402/402 po poprawkach z pierwszego smoke
- **Testy manualne / dowody:** kontrolny smoke wizualny aplikacji zielony; pełna
  checklista ręczna pozostaje po stronie użytkownika
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M4 pozostaje zamknięty do czasu formalnego GO M3.

Pierwszy smoke użytkownika wykrył dwa blokery: stale snapshot Planera przy
telemetrii oraz brak wykonanej rekompensaty karty Staniek w widoku. Planer
poprawiono w `95f9777`. Dalsza analiza potwierdziła, że rekompensata `20:53`
została prawidłowo wykonana i zapisana, natomiast zoptymalizowany odczyt historii
usuwał jej źródłowy odpoczynek podczas ponownego stosowania granicy pustej
starszej sesji. Odczyt warm został poprawiony bez unieważniania decyzji i bez
wymagania ponownego wyboru; oba przypadki mają regresje automatyczne. Wymagany
jest ponowny test ręczny użytkownika.

---

## M4–M8 — do rozpoczęcia

Każdy etap otwieramy dopiero po **GO** poprzedniego. Szczegółowe zadania i gate'y
w `docs/PLAN_BETA_12_M0-M8/`. Poniżej rejestr decyzji wypełniany przy zamykaniu
kolejnych etapów (szablon: data start/koniec, wynik, commit, build, testy auto,
dowody manualne, P0, P1, uwagi).

- **M4** — walidacja gotowej inwentaryzacji UI + formalny **UI freeze**. — *nie rozpoczęty*
- **M5** — pełne `pl-PL` i `en-GB`, zielone regresje obu języków. — *nie rozpoczęty*
- **M6** — niezmienny RC beta.12: numer + commit + SHA-256, ZIP zamrożony. — *nie rozpoczęty*
- **M7** — smoke na rozpakowanym ZIP-ie (istniejąca + czysta baza) → GO/FIX/HOLD. — *nie rozpoczęty*
- **M8** — publikacja dokładnie artefaktu z GO + checksuma + dokumentacja PL/EN. — *nie rozpoczęty*
