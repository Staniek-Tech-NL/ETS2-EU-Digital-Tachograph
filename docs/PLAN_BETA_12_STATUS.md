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
| **M1** | Planer: kontrakty i czerwone testy | 🟢 rozszerzony o załogę | GO | M2 |
| **M2** | Planer: silnik zdarzeniowy | 🟢 rozszerzenie załogi domknięte | GO | M3A |
| **M3A** | Game Calendar & Deadline Presentation | 🟢 ręczny gate UI zielony | GO | M3 |
| **M3** | Planer: Application Service i UI | 🟢 M3-R3 automatycznie i ręcznie zielone | GO | M3.5 |
| **M3.5** | Raporty i statystyki: wariant B | ⚪ nie rozpoczęty (dokumentacja domknięta) | — | M3.6 |
| **M3.6** | Wewnętrzny smoke checkpoint `beta.12-rc0` | ⚪ nie rozpoczęty (dokumentacja domknięta) | — | M4 |
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

## M1 — Planer: kontrakty i czerwone testy (ROZSZERZONY O ZAŁOGĘ)

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
| `MultiManningCrew` i oś S1/S2 | ✅ zrobione | Snapshot obu kart i segment równoległych aktywności |
| `JP-CREW-P0-01–06` | ✅ zrobione | Sześć blokujących przypadków planowania załogi |

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
- [x] wspólna oś czasu pojazdu z równoległymi aktywnościami S1/S2
- [x] przyszłe zmiany prowadzącego bez postoju pojazdu
- [x] przerwa 45 min zmiennika w ruchu zeruje tylko jazdę ciągłą
- [x] testy `JP-CREW-P0-01–06` zielone
- [x] pełna macierz granic 9/10 h, 56/90 h i 30 h dla obu kart
- [x] zgodność projekcji z bieżącym `RegulationEngine`
- [x] osobne terminy odpoczynku tygodniowego obu kart i przejścia granicy tygodnia
- [x] kontrolowana obsługa luk, braku telemetrii i skoku czasu
- [x] przerwa zmiennika w ruchu zachowana jako osobny warunek także po retencji historii

### Zamknięcie M2

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik historyczny:** **GO** dla modelu jednej karty
- **Wynik bieżący:** **GO — rozszerzenie planowania załogi domknięte**
- **Commit / punkt przywracania:** `751cd07` — deterministyczny silnik zdarzeniowy M2
- **Punkt przywracania rozszerzenia załogi:** `db34de2`
- **Domknięcie granic i zgodności załogi:** `0178d4c`
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 443/443; pakiet M2 załogi 40/40, w tym
  `JP-CREW-P0-01–06`, macierz granic i zgodność z `RegulationEngine`
- **Testy manualne / dowody:** nie dotyczy — brak zmian UI
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M3 może wznowić implementację integracji
  snapshotu obu kart, wspólnej osi czasu i prezentacji zmian prowadzącego.

---

## M3A — Game Calendar & Deadline Presentation

**M3A-0:** **PASS Z UWAGĄ FORMALIZACYJNĄ**. Istniejąca semantyka
`GameWeek.From` zostaje formalnie określona jako tydzień rozpoczynający się w
poniedziałek 00:00 w kalendarzu gry przesuniętym przez surowy
`WeekEpochOffsetDays`.

- **Specyfikacja:** **ZATWIERDZONA — GO**
- **Implementacja:** **ZAKOŃCZONA — GO**
- **Zakres pierwszej tury:** `ODP. DZIENNY`, `ODP. TYG.`, terminy rekompensat
- **Poza pierwszą turą:** `DO PRZERWY`, Planer rynku, problem 44/45
- **Dokument:** `docs/PLAN_BETA_12_M0-M8/M3A_GAME_CALENDAR_AND_DEADLINE_PRESENTATION.md`

M3A nie dodaje nowego anchoru, nie zmienia znaczenia offsetu i nie normalizuje
wartości `-6…6`. Core ma pozostać jednym źródłem kanonicznych granic tygodnia.

Wynik automatyczny 2026-07-24:

- publiczne granice Core zgodne z dotychczasową formułą dla
  `{-1, 0, +1, +6}`;
- RuleEngine 157/157;
- pełna regresja Release 478/478;
- build Release 0 błędów / 0 ostrzeżeń;
- odtworzenie historii warm + hot oraz ponowne zastosowanie zapisanej decyzji
  rekompensaty po restarcie mają test regresyjny;
- brak nowych błędów P0/P1 w gate’ach automatycznych.

Ręczny gate `M3A-UI-01…04` oraz pełna checklista UI zostały potwierdzone przez
użytkownika 2026-07-24 wynikiem zielonym. **Formalny wynik M3A: GO.**

---

## M3 — Planer: Application Service i UI (REDESIGN — M3-R0 ZATWIERDZONE)

**Kryterium wejścia:** `M2-CREW GO AND M3A GO`. **Stan:** spełnione;
M2-CREW i M3A mają formalny wynik GO.

**Decyzja produktowa 2026-07-24:** stary prototyp M3 został odrzucony. Nie
kontynuujemy formularza opartego na ręcznie podawanym czasie do końca dostawy
i jednym `RemainingDriveMinutes`.

Obowiązujący model rozdziela:

```text
MarketOffer
ActiveDelivery
```

M3-R0 jest zatwierdzone. Pakiet `M3-P0-01…08` przeszedł od kontrolowanej
czerwieni do zielonego wyniku 9/9. M3-R2 dostarcza silnik obu przypadków użycia
oraz nowy Application Service z atomowym snapshotem S1/S2 i unieważnianiem
wyniku. Testy Application: 59/59; pełna regresja Release: 491/491.

`M3-P0-08` wykrył i zamknął rozjazd offsetu tygodnia w ścieżce M2-CREW:
lokalne obliczenie zostało zastąpione kanonicznym `GameWeek` z M3A.
UI ma być wzorowane na makiecie
`docs/images/ChatGPT Image 24 lip 2026, 17_15_10.png`, z polami wynikającymi
z nowego kontraktu, a nie ze starego formularza. Następny krok: M3-R3 UI.

M3-R3 jest automatycznie zielone: nowy ViewModel i XAML obsługują oba przypadki
użycia. Wygaśnięcie oferty pozostaje czasem względnym, natomiast granice okna
dostawy użytkownik podaje jako dzień tygodnia + godzinę; Application Service
rozstrzyga najbliższe wystąpienia przez M3A względem atomowego snapshotu.
Tabela pokazuje wspólną oś pojazdu i równoległe aktywności S1/S2. Testy Desktop:
76/76; pełna regresja Release: 501/501. Końcowy smoke użytkownika z 2026-07-24
jest zielony; UI działa zgodnie z zatwierdzonym kontraktem. M3 otrzymuje GO.

### Gate M3

- [x] przepływ formularz → `JourneyPlannerService` → harmonogram działa
- [x] atomowy snapshot korzysta z kanonicznej historii, luk i stanu regulacyjnego
- [x] zmiana stanu unieważnia poprzedni wynik
- [x] wybór S1/S2 nie aktywuje sam podwójnej obsady
- [x] brak zapisu hipotetycznego planu do historii
- [x] build Release i pełna regresja automatyczna są zielone
- [x] kontrolny smoke wizualny zakładki przy 1280×800 jest zielony
- [x] końcowy test ręczny użytkownika
- [x] snapshot obu kart aktywnej załogi
- [x] wspólna oś pojazdu i równoległe aktywności S1/S2 w UI
- [x] przyszłe zmiany prowadzącego oraz przerwy zmiennika w ruchu

### Historyczny stan pierwszego wariantu M3

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia implementacji:** 2026-07-24
- **Wynik:** **HOLD** — najpierw integracja planowania załogi, potem test ręczny
- **Commit / punkt przywracania:** `e8efc61` — implementacja Application Service i UI M3
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 402/402 po poprawkach z pierwszego smoke
- **Testy manualne / dowody:** wynik historyczny, unieważniony przez redesign
- **Otwarte błędy P0:** 1 — warstwa Application/UI nie korzysta jeszcze z
  kompletnego silnika aktywnej podwójnej obsady
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M4 pozostaje zamknięty do czasu integracji
  `MultiManningCrew` i formalnego GO M3.

Pierwszy smoke użytkownika wykrył dwa blokery: stale snapshot Planera przy
telemetrii oraz brak wykonanej rekompensaty karty Staniek w widoku. Planer
poprawiono w `95f9777`. Dalsza analiza potwierdziła, że rekompensata `20:53`
została prawidłowo wykonana i zapisana, natomiast zoptymalizowany odczyt historii
usuwał jej źródłowy odpoczynek podczas ponownego stosowania granicy pustej
starszej sesji. Odczyt warm został poprawiony bez unieważniania decyzji i bez
wymagania ponownego wyboru; oba przypadki mają regresje automatyczne. Wymagany
jest ponowny test ręczny użytkownika. Naprawa rekompensaty: `af97b39`.

---

## M3.5 — Raporty i statystyki: wariant B (NIE ROZPOCZĘTY)

**Kryterium wejścia:** formalny wynik GO dla M3 — spełnione. **Stan:** M3.5
odblokowane, jeszcze nierozpoczęte. Etap wstawiony między M3 a M4 decyzją
właściciela 2026-07-24 (Okno A: przed UI freeze).

- **Dokument etapu:** `docs/PLAN_BETA_12_M0-M8/M3_5_RAPORTY_WARIANT_B.md`
- **Plan wykonawczy:** `docs/PLAN_WDROZENIA_RAPORTY_WARIANT_B.md`
- **Zakres:** przebudowa ekranu Raporty do wariantu B; bez zmian `RuleEngine`,
  historii kanonicznej, migracji SQLite i kontraktów PDF/JSON/CSV.
- **Uwaga wejściowa:** Etap 0 audytu wykonać na stanie po M3 (478/478, Planer w
  `MainWindow.xaml`), nie na migawce `315/315` z planu wariantu B. Terminy w
  Raportach konsumują warstwę `GameCalendar` z M3A, bez reimplementacji.
- **Wyjście:** gate Etapu 7 wariantu B + pełna checklista UI + Release 0/0; **bez
  tworzenia paczki beta** — wynik wchodzi do wewnętrznego smoke M3.6.

---

## M3.6 — Wewnętrzny smoke checkpoint `beta.12-rc0` (NIE ROZPOCZĘTY)

**Kryterium wejścia:** formalny wynik GO dla M3.5. **Stan:** oczekuje.

- **Dokument etapu:** `docs/PLAN_BETA_12_M0-M8/M3_6_WEWNETRZNY_SMOKE_CHECKPOINT.md`
- **Charakter:** zamrożony **wewnętrzny** artefakt `0.1.0-beta.12-rc0` do
  wielogodzinnego smoke; **niepublikowany**, nie uruchamia bramki M8, nie tworzy
  osobnego wydania `beta.11.5`.
- **Cel:** złapać błędy in-game przed UI freeze (M4), gdy poprawki mogą jeszcze
  lądować swobodnie; de-ryzykuje końcowy smoke M7.
- **Granica pokrycia:** przed M5 (lokalizacja) — testuje funkcję, nie
  zlokalizowane stringi; **M7 na zlokalizowanym RC pozostaje wymagany**.
- **Wyjście:** triaging zakończony, potwierdzone błędy naprawione przed M4,
  decyzja GO/FIX/HOLD dla checkpointu.

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
