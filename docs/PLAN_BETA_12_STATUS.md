# PLAN BETA.12 — TABLICA STATUSU M0–M8

**Projekt:** ETS2 EU Digital Tachograph
**Baza wydaniowa:** `0.1.0-beta.11.1` (GO 2026-07-23)
**Cel:** `0.1.0-beta.12` — ostatnia beta przed pierwszą szeroką publikacją.
**Źródło etapów:** `docs/PLAN_BETA_12_M0-M8/` (integralność SHA-256 potwierdzona z `MANIFEST_SHA256.md`).
**Ostatnia aktualizacja tablicy:** 2026-07-27

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
| **M3.5** | Raporty i statystyki: wariant B | 🟢 UI, automatyka i ręczny gate zielone | GO | M3.6 |
| **M3.6** | Wewnętrzny smoke checkpoint | 🟢 smoke rc3 zielony | GO | M3.7 |
| **M3.7** | Planer: ergonomia wprowadzania danych | 🟢 automatyka i ręczny gate zielone | GO | M4-0 |
| **M4-0** | Inwentaryzacja UI + weryfikacja rc4 | 🟢 62/62 pozycji beta.12 PASS | GO | M4 |
| **M4** | Finalizacja UI + **UI freeze** | 🟢 UI zamrożone | GO | M5 |
| **M5** | Lokalizacja PL/EN | 🟢 M5.1–M5.4 zamknięte | GO | M6 |
| M6 | Release Candidate `0.1.0-beta.12` | 🔴 HOLD poprawnościowy — P1 projekcji hot/warm | HOLD | M7 |
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

## M3 — Planer: Application Service i UI (ZAMKNIĘTY — GO)

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

### Zamknięcie M3 (GO)

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `bd76999` — complete M3 market offer UI (integracja załogi w UI)
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 501/501; Desktop 76/76, w tym unieważnianie wyniku przy zmianie snapshotu załogi
- **Testy manualne / dowody:** integracja aktywnej podwójnej obsady w UI potwierdzona przez użytkownika 2026-07-24; końcowy smoke UI zielony
- **Otwarte błędy P0:** 0 — P0 integracji załogi zamknięty w `bd76999`
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M3.5 (Wariant B Raporty) może wystartować. Etap 0 audytu na stanie po M3 (501/501); terminy w Raportach konsumują warstwę M3A. Residualny 44/45 in-game należy do M3.6/M7, nie do M3.

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

## M3.5 — Raporty i statystyki: wariant B (ZAMKNIĘTY — GO)

**Kryterium wejścia:** formalny wynik GO dla M3 — spełnione. **Stan:** M3.5
zakończone wynikiem GO po akceptacji właściciela 2026-07-24.

- **Dokument etapu:** `docs/PLAN_BETA_12_M0-M8/M3_5_RAPORTY_WARIANT_B.md`
- **Plan wykonawczy:** `docs/PLAN_WDROZENIA_RAPORTY_WARIANT_B.md`
- **Zakres:** przebudowa ekranu Raporty do wariantu B; bez zmian `RuleEngine`,
  historii kanonicznej, migracji SQLite i kontraktów PDF/JSON/CSV.
- **Audyt Etapu 0:** PASS; zachowano półotwarty zakres `[from, toExclusive)`,
  istniejące bloki aktywności i kontrakt zobowiązań.
- **Implementacja:** wydzielony workspace, cztery presety `game_time`, jawne stany
  podglądu, sześć kafli, pięć zakładek i jeden `ReportDto` dla podglądu i eksportu.
- **Niezmienniki:** bez zmian `RuleEngine`, historii kanonicznej, SQLite i
  kontraktów PDF/JSON/CSV; terminy konsumują warstwę M3A.
- **Gate automatyczny:** Release 0/0, pełna regresja 521/521, start aplikacji i
  utworzenie podglądu potwierdzone w logu.
- **Gate ręczny:** zaakceptowany po korektach nagłówka, kafli, podsumowania i
  kompletności.
- **Wynik:** **GO**; nie utworzono paczki beta.

---

## M3.6 — Wewnętrzny smoke checkpoint (ZAMKNIĘTY — GO)

**Kryterium wejścia:** formalny wynik GO dla M3.5. **Stan:** spełnione; artefakty
`rc0`, `rc1` i `rc2` zostały unieważnione przez błędy wykryte w smoke.

- **Artefakt bieżący:** `ETS2Tachograph-0.1.0-beta.12-rc3-win-x64.zip`
- **Commit źródłowy:** `7e90a3620e68b7dc8598d733bc17eac4f5e488e6`
- **SHA-256:** `F9E85D58E37EF381D5DD435222A7594A34430B99F1E69DDD669CF608899D026E`
- **Weryfikacja paczki `rc3`:** świeże rozpakowanie, 454 pliki zgodne bajtowo,
  `FileVersion 0.1.12.3`, dokładny `ProductVersion`, plugin v3 i checksumy — PASS.
- **Artefakt historyczny, unieważniony:** `ETS2Tachograph-0.1.0-beta.12-rc2-win-x64.zip`
- **Commit historyczny:** `1c87e004b629e0b691db1eb18e0952fa5641d8fe`
- **SHA-256 historyczny:** `30CD7BD2B65D2C59DD7F2306FF6D3A129D4AA5BCC61C78F5E7381B8C9A8E5ECA`
- **Weryfikacja paczki `rc2`:** rozpakowanie, struktura, `FileVersion 0.1.12.2`,
  `ProductVersion`, plugin v3 i checksumy — PASS.
- **Artefakt historyczny, unieważniony:** `ETS2Tachograph-0.1.0-beta.12-rc1-win-x64.zip`
- **Commit historyczny:** `eb1f02d765a7c0f2cabea57e047ff74198c12975`
- **SHA-256 historyczny:** `25AD18A416A86A1D63D7F4B7C0B9D3400B9E3CB284CE2A54924DEB68D14078EE`
- **Weryfikacja paczki:** rozpakowanie, struktura, `FileVersion 0.1.12.1`,
  `ProductVersion`, plugin v3 i checksumy — PASS.
- **Powód unieważnienia:** główne okno pozostawało pod ETS po Alt+Tab; nakładki
  działały prawidłowo.
- **Odrzucona próba:** zmiana właściwości WPF `Topmost` powodowała `AppHangB1`.
- **Odrzucona poprawka `rc1`:** `SetWindowPos` z `SWP_NOACTIVATE`; ręczne
  sterowanie Z-orderem nie usuwało objawu i zostało wycofane.
- **Przyczyna potwierdzona pomiarem procesu:** po wczytaniu świata telemetria
  uruchamiała około 10 razy na sekundę pełne odświeżenie gotowości Planera,
  obejmujące historię i luki obu kart. Piętrzące się zadania nasycały dispatcher
  UI, dlatego wybrane przez Alt+Tab okno nie przetwarzało aktywacji i odmalowania.
- **Korekta:** obserwacja każdej ramki wykonuje tylko tanią walidację istniejącego
  wyniku; gotowość jest ładowana przy wejściu do Planera i przed obliczeniem,
  a odczyty obu kart są sekwencyjne na współdzielonym kontekście bazy.
  Model głównego okna pozostaje zgodny z beta.11, nakładki są nietknięte.
- **Dowód regresyjny:** przed poprawką 101 zamiast 1 odświeżeń gotowości oraz
  4 zamiast 1 równoległych odczytów; po poprawce oba testy są zielone.
- **Gate automatyczny korekty:** 522/522 sekwencyjnie, Release 0/0.
- **Gate manualny korekty:** Alt+Tab w menu i po wczytaniu zapisu — PASS,
  potwierdzony przez właściciela 2026-07-24.
- **Błąd `rc2`:** ciągła przerwa slotu 2 zapisana jako `1 min None + 44 min
  CrewBreakInMotion` nie zerowała jazdy ciągłej, ponieważ RuleEngine rozdzielał
  blok po zmianie warunku.
- **Korekta `rc2`:** sąsiadujące `BreakOrRest` są łączone dla kwalifikacji
  przerwy 45 min i jej licznika, ale nie dla odpoczynku dobowego/tygodniowego.
- **Dowód:** świeża baza smoke oraz dwa czerwone testy; przed poprawką
  62 min zamiast 0, po poprawce RuleEngine 168/168 i Engine 70/70.
- **Gate automatyczny:** pełna regresja 524/524, Release 0/0.
- **Gate manualny slotu 2:** PASS — `1 min postój + 44 min ruch = 45 min`,
  świeże dane i aktywna telemetria, potwierdzone przez właściciela 2026-07-25.
- **Restart aplikacji:** PASS — poprawiona aplikacja podpięła istniejącą
  telemetrię bez restartu ETS2.
- **Pozostały smoke `rc3`:** PASS — potwierdzony przez właściciela 2026-07-27.
- **Obserwacja PROM:** nierozliczona luka po skoku czasu wynika z opisanego
  ograniczenia trybu promowego i nie jest regresją `rc3`; pełne odstępstwo z
  art. 9 pozostaje jawnie odłożone poza beta.12.
- **Decyzja M3.6:** **GO** — brak otwartych błędów P0/P1.
- **Pełny smoke `rc2`:** FAIL; `rc2` unieważniony.
- **Artefakty historyczne:** `rc0`, `rc1` i `rc2` pozostają unieważnione.

- **Dokument etapu:** `docs/PLAN_BETA_12_M0-M8/M3_6_WEWNETRZNY_SMOKE_CHECKPOINT.md`
- **Charakter:** korekta przed kolejnym zamrożonym artefaktem wewnętrznym;
  **niepublikowana**, nie uruchamia bramki M8 i nie tworzy osobnego wydania
  `beta.11.5`.
- **Cel:** złapać błędy in-game przed UI freeze (M4), gdy poprawki mogą jeszcze
  lądować swobodnie; de-ryzykuje końcowy smoke M7.
- **Granica pokrycia:** przed M5 (lokalizacja) — testuje funkcję, nie
  zlokalizowane stringi; **M7 na zlokalizowanym RC pozostaje wymagany**.
- **Wyjście:** triaging zakończony, potwierdzone błędy naprawione przed M4,
  decyzja GO/FIX/HOLD dla checkpointu.

---

## M3.7 — Planer: ergonomia wprowadzania danych (ZAMKNIĘTY — GO)

Etap wstawiony między M3.6 a M4 decyzją właściciela z 2026-07-26. Szczegóły
w `docs/PLAN_BETA_12_M0-M8/M3_7_PLANER_ERGONOMIA_WEJSCIA.md`.

**Kryterium wejścia:** formalny wynik GO dla M3.6. **Stan:** spełnione
2026-07-27.

**Stan wdrożenia 2026-07-26:** właściciel polecił wprowadzić plan mimo
niezamkniętego formalnie wejścia. Zakres kroków 1–4 został zaimplementowany;
pełny ręczny gate został następnie potwierdzony przez właściciela jako zielony.
M3.7 ma wynik **GO** i odblokował M4-0. M4-0 został następnie zamknięty
wynikiem GO; właściwe M4 jest odblokowane.

- **Artefakt bieżący:** `ETS2Tachograph-0.1.0-beta.12-rc4-win-x64.zip`
- **Commit źródłowy:** `a1b8a486b52ee244984016efe268562690d4fbc4`
- **SHA-256:** `8ED073DBEF0ADEA4B589BD257A6D2DEC6A390C11F5242D97D188838F9F0F56DE`
- **Weryfikacja paczki `rc4`:** świeże rozpakowanie, 455 plików zgodnych
  bajtowo, `FileVersion 0.1.12.4`, dokładny `ProductVersion`, plugin v3
  i plik checksumy — PASS.

- [x] parser czasu: `HH:MM`, minuty, `1h30`, `1,5` / `1.5`, białe znaki
- [x] okno dostawy: dzień + godzina `00`–`23` + minuta `00`–`59`
- [x] presety, kroki klawiatury, walidacja per pole i `PRÓG „NA STYK"`
- [x] zapis i odtworzenie wejść ze znacznikiem pochodzenia
- [x] brak odświeżania przy każdym znaku (`LostFocus`; Enter wymusza zapis pola)
- [x] pełna regresja automatyczna: **538/538**
- [x] build Release: **0 błędów / 0 ostrzeżeń**
- [x] kontrola wizualna układu i kompletu pól w uruchomionej aplikacji
- [x] pełny ręczny gate klawiatury, obu trybów/slotów, restartu i logu bindingów
      — zielony, potwierdzenie właściciela 2026-07-26

- **Powód:** ocena jednej oferty wymaga dziś wypełnienia ośmiu pól `HH:MM`,
  co przy grze na klawiaturze czyni Planer praktycznie nieużywalnym.
- **Gałąź robocza:** `feature/planner-input-ergonomics`.
- **Zakres:** wyłącznie warstwa wejściowa — `JourneyPlannerViewModel` oraz
  sekcja PLANER w `MainWindow.xaml`. Silnik planowania i prezentacja wyniku
  pozostają nietknięte.
- **Dlaczego przed M4:** M4 wprowadza UI freeze i obejmuje inwentaryzacją
  Planer. Wykonanie zmian po M4 oznaczałoby złamanie polityki freeze'u albo
  podwójną inwentaryzację. W chwili decyzji M4 nie był rozpoczęty.
- **Decyzje:** makieta zaakceptowana; minuty jako pełna lista `00`–`59`;
  `TightMargin` wystawiony w UI jako `PRÓG „NA STYK"`; wejście względne
  odrzucone, bo ETS2 pokazuje okno dostawy jako dni tygodnia; autouzupełnianie
  z telemetrii odłożone po publikacji (wymaga protokołu v4).
- **Wpływ na M7:** zmiany dotykają XAML i modelu widoku, więc smoke M3.6
  na artefakcie `rc3` nie pokrywa tego etapu. Pełny M7 pozostaje wymagany.

---

## M4-0 — Inwentaryzacja UI (ZAMKNIĘTY — GO)

Etap przygotowawczy dodany decyzją właściciela 2026-07-27, ponieważ M0
deklarowało zieloną inwentaryzację bez zachowania źródłowego wykazu.

- **Dokument:** `docs/PLAN_BETA_12_M0-M8/M4_0_INWENTARYZACJA_UI.md`
- **Metoda:** kompletność z XAML i view-modeli; stan faktyczny wyłącznie z
  osobistej weryfikacji właściciela na rozpakowanym rc4.
- **Wynik rc4:** 62/62 pozycji `beta.12` — PASS; 4 pozycje poza zakresem — N/D.
- **Otwarte P0/P1:** 0/0; pozycji przypisanych do naprawy w M4: 0.
- **Gate:** wykaz zatwierdzony, wszystkie obserwacje przypisane, formalne
  **GO M4-0**.
- **Wpływ:** właściwe M4 rozpoczęto 2026-07-27.

---

## M4 — Finalizacja UI i UI freeze (ZAMKNIĘTY — GO)

- **Data rozpoczęcia:** 2026-07-27.
- **Wejście:** GO M4-0; 62/62 pozycji `beta.12` PASS, 4 N/D i 0 pozycji
  przypisanych do naprawy w M4.
- **Punkt rozpoczęcia:** `9a127c9`.
- **Dryf względem rc4:** 0 zmian w kodzie źródłowym i testach od
  `a1b8a486b52ee244984016efe268562690d4fbc4`.
- **Build Release:** 0 błędów, 0 ostrzeżeń.
- **Testy automatyczne:** 538/538 PASS.
- **Audyt statyczny UI:** 3/3 XAML poprawne, 8/8 procedur zdarzeń istnieje,
  0 placeholderów oraz 0 statycznie wyłączonych lub ukrytych martwych bloków.
- **Zakres:** walidacja gotowego UI oraz formalny freeze; bez nowych
  funkcji, bez tłumaczeń M5 i bez pełnego PROM art. 9.
- **Data zakończenia:** 2026-07-27.
- **Wynik:** **GO — UI FREEZE**.
- **Otwarte P0/P1:** 0/0.
- **Gate:** wszystkie warunki zielone; formalna zgoda właściciela udzielona
  2026-07-27.
- **Wpływ:** M5 odblokowany. Od commita zamykającego M4 obowiązuje polityka
  UI freeze z dokumentu etapu.

---

## M5 — Lokalizacja PL/EN (GO)

- **Data rozpoczęcia:** 2026-07-27.
- **Data zakończenia:** 2026-07-27.
- **Wejście:** GO M4 i aktywny UI freeze.
- **Punkt wejściowy:** `2d8a760`.
- **Gałąź:** `codex/m5-localization-pl-en`.
- **Stan:** **GO**; M5.1–M5.4 zamknięte bez pozycji otwartych. M6 odblokowany
  z zachowaniem obowiązku ponownego pomiaru M5.2-P przed zamrożeniem RC.
- **Artefakt:** `docs/LOCALIZATION_STRING_INVENTORY.md`.
- **Paczka 1:** elementy wspólne, powłoka i nawigacja — 33 wiążące klucze,
  0 pozycji otwartych.
- **Paczka 2:** Dashboard — 58 wiążących kluczy, 0 pozycji otwartych.
- **Paczka 3:** wirtualny tachograf — 79 wiążących kluczy,
  0 pozycji otwartych.
- **Paczka 4:** Historia, luki i wpis manualny — 87 wiążących kluczy,
  0 pozycji otwartych.
- **Paczka 5:** `X-01`, wspólne formatery czasu i terminów — 12 wiążących
  kluczy, 0 pozycji otwartych.
- **Paczka 6:** `UI-04`, kraje i kody tachografowe — 260 wiążących kluczy,
  0 pozycji otwartych.
- **Paczka 7:** `UI-05`, Rekompensaty — 32 wiążące klucze,
  0 pozycji otwartych.
- **Paczka 8:** `UI-06`, Raporty w Desktop — 100 wiążących kluczy,
  0 pozycji otwartych.
- **Paczka 9:** `UI-07`, Kierowcy i Ustawienia — 18 wiążących kluczy,
  0 pozycji otwartych.
- **Paczka 10:** `UI-08`, Planer — 103 wiążące klucze,
  0 pozycji otwartych.
- **Paczka 11:** `UI-09`, dialogi, potwierdzenia i komunikaty operacyjne —
  77 wiążących kluczy, 0 pozycji otwartych.
- **Paczka 12:** `UI-10`, nakładki S1/S2 — 16 wiążących kluczy,
  0 pozycji otwartych.
- **Paczka 13:** `PDF-01`, raport PDF — 77 nowych wiążących nazw,
  22 ponowne użycia i 99 wpisów `ReportStrings` na język,
  0 pozycji otwartych.
- **Katalog łączny:** 952 klucze, 952 unikalne nazwy; jawna lista
  31 dozwolonych par powtórzonych wartości między różnymi rolami.
- **X-01:** GO; zależności kompletności Dashboardu i listy dni w modalu wpisu
  manualnego są zamknięte.
- **Rejestr presenterów:** 30 typów — 21 rozstrzygniętych,
  9 świadomie wykluczonych, 0 pozostałych.
- **Pozostałe obszary M5.1:** brak;
  `DOC-01/02` pozostają odłożone do M5.4.
- **Przejęte zobowiązania:** `UI-09` jest zamknięte; szczegółowe komunikaty
  błędów, `OperationStatus` i potwierdzenia mają wiążące klucze oraz wspólną
  politykę diagnostyczną. `UI-10` jest zamknięte; wszystkie tekstowe bindingi
  obu nakładek, w tym `ConnectionStatus`, mają klucze albo kategorię techniczną.
  `PDF-01` jest zamknięte; metadane, treść i presentery PDF mają katalog oraz
  bramkę renderowania PL/EN.
- **Decyzja statusów PDF:** PDF ponownie używa `ReportCompensationStatus_*`.
  Zysk semantyczny i brak piątej rodziny kosztują prawdopodobną zmianę
  szerokości kolumny `95 pt` w M5.3; jest ona z góry autoryzowana wyłącznie
  jako korekta przepełnienia, bez zmiany kolejności, danych i semantyki.
- **M5.2 — zasoby:** `UiStrings` ma po 626 wpisów, `ReportStrings` po 99,
  a oba magazyny nazw krajów po 249; parzystość nazw, brak pustych wartości,
  placeholdery i 22 klucze mostu UI/PDF są objęte testami.
- **M5.2 — kultura:** `%LocalAppData%\ETS2Tachograph\ui-culture.json`
  przechowuje wyłącznie `pl-PL` albo `en-GB` w schemacie 1. Brak pliku zachowuje
  polski, a uszkodzona lub nieobsługiwana wartość bezpiecznie wybiera `en-GB`
  i trafia do diagnostyki.
- **M5.2 — start:** kultura procesu, domyślna kultura wątków i język bindingów
  WPF są ustawiane przed utworzeniem ViewModelu, okna i eksportera PDF.
- **M5.2 — testy:** build 0 błędów / 0 ostrzeżeń; pełna regresja 558/558.
- **M5.2 — granica kontroli języka:** przed M5.3 tylko fundament i Ustawienia
  przełączają się na EN. Pozostałe widoki zachowują polskie literały zgodnie
  z kolejnością etapów; mieszany interfejs jest oczekiwanym stanem przejściowym.
- **Checkpoint wydajnościowy M5.2-P:** wydzielony jako osobna bramka.
  Na świeżej kopii bieżącej bazy automatyczna korekta luk spadła z 30,84 s
  do 1,31 s, archiwizacja trzech kart z około 16,35 s do 4,01 s, a łączna
  praca repozytorium z około 49 s do 5,36 s. Automat nadal pozostawił te same
  4 nierozliczone luki. Status: **wydajność GO warunkowe, poprawność HOLD**.
  Wydajność warunkowo, ponieważ archiwizacja nadal czyta całą surową historię,
  a cold retention nie jest zaimplementowane; osobisty pomiar tworzy bazę
  odniesienia, a przed M6 pomiar jest powtarzany i musi pozostać poniżej 10 s
  pracy repozytorium oraz bez wzrostu `APP_START` → `APP_READY` większego
  niż 50%. Szczegóły:
  `docs/PLAN_BETA_12_M0-M8/M5_2_CHECKPOINT_WYDAJNOSCI_STARTU.md`.
- **P1 poprawnościowy z M5.2-P:** pomiar rozliczył 0 luk, więc nie objął
  ścieżki rozliczania luki. Kontrola wykazała, że projekcja hot/warm nie
  przycina bloku ciepłego do kotwicy sesji leżącej poniżej progu warm i
  pozwala na nachodzenie, czego projekcja surowa nie dopuszcza. RuleEngine
  przesuwa wtedy reset dobowy — `LastDailyRestResetAt` 600 → 1300 i
  `MinutesUntilDailyRestDeadline` 740 → 1440, czyli około 12 godzin więcej
  do terminu odpoczynku. Defekt istniał przed checkpointem i obejmuje
  Dashboard; checkpoint zdjął ochronę ze ścieżki wpisu manualnego. Pozycja
  aktywna w `KNOWN_ISSUES.md`, test odtwarzający `BackwardBranchProjectionTests`.
- **Następny krok:** M6 pozostaje wstrzymany do zamknięcia bramki zgodności
  projekcji. Przed zamrożeniem RC: naprawa nachodzenia, zielony
  `BackwardBranchProjectionTests`, złoty zrzut, idempotencja retencji, trzy
  rozmiary bazy, pełna regresja oraz powtórzony pomiar M5.2-P zgodnie z jego
  progami.
- **Jawny wyjątek od UI freeze:** M5.2 dodaje dokładnie jedną nową kontrolkę
  do zamrożonego interfejsu — wybór `pl-PL` / `en-GB` w Ustawieniach.
  Jest wymagana planem M5, nie wprowadza dynamicznej zmiany bez restartu
  i stanowi jedyną autoryzowaną różnicę względem inwentaryzacji UI z M4.
  Smoke M7 porównuje ekran z bazą M4 powiększoną o ten jeden element;
  każda inna nowa kontrolka nadal narusza UI freeze.
- **Zmiany wykonawcze:** fundament zasobów i kultury, magazyn preferencji,
  lokalizowana sekcja Ustawień oraz jedna autoryzowana kontrolka języka.
- **M5.3 — paczka 1:** **GO**, zamknięta bez pozycji otwartych. Powłoka,
  nawigacja, wspólne akcje i nagłówki oraz status telemetrii korzystają
  z wiążących zasobów. Build WPF, 128/128 testów Desktopu oraz osobista
  kontrola wizualna PL/EN są zielone.
- **M5.3 — paczki 2–12:** **GO**, zamknięte bez pozycji otwartych.
  Dashboard, tachograf LCD, Historia, wpis manualny, kraje, Rekompensaty,
  Raporty, Kierowcy, Ustawienia, Planer, dialogi i nakładki są zlokalizowane.
  Pełna regresja 558/558, regresja Desktop 129/129 oraz osobisty smoke PL/EN
  są zielone.
- **M5.4 — implementacja:** raport PDF jest zlokalizowany w `pl-PL` i `en-GB`;
  metadane, treść, presentery, puste stany i stopka używają zasobów. Pełne
  statusy rekompensat mieszczą się po autoryzowanym poszerzeniu kolumny z
  95 pt do 125 pt, a układ checkpointów nie ma kolizji nagłówków.
- **M5.4 — dane i testy:** próbki PL/EN wygenerowane z identycznego raportu
  zachowują wspólne identyfikatory, wartości czasu i dane techniczne. Render
  dwóch stron w obu językach: PASS; Reports 11/11 i pełna regresja 561/561:
  PASS; build Release: 0 błędów, 0 ostrzeżeń.
- **M5.4 — dokumentacja:** gotowe `docs/INSTALLATION_PL.md`,
  `docs/INSTALLATION_EN.md`, `docs/USER_GUIDE_PL.md` oraz
  `docs/USER_GUIDE_EN.md`; README prowadzi do wszystkich czterech plików.
- **M5.4 — decyzja:** **GO**, zatwierdzone przez właściciela 2026-07-27 po
  osobistym smoke eksportu i oględzinach PDF PL/EN; 0 pozycji otwartych.
- **Zakres językowy:** `pl-PL` i `en-GB`.
- **Kontrakty chronione:** JSON, techniczny CSV, `.tacho`, SQLite, protokół v3,
  identyfikatory i kody techniczne.

---

## M6–M8 — do rozpoczęcia

Każdy etap otwieramy dopiero po **GO** poprzedniego. Szczegółowe zadania i gate'y
w `docs/PLAN_BETA_12_M0-M8/`. Poniżej rejestr decyzji wypełniany przy zamykaniu
kolejnych etapów (szablon: data start/koniec, wynik, commit, build, testy auto,
dowody manualne, P0, P1, uwagi).

- **M4-0** — inwentaryzacja UI + osobista weryfikacja rc4. — *GO*
- **M4** — realizacja zatwierdzonej inwentaryzacji + formalny **UI freeze**. — *GO*
- **M5** — pełne `pl-PL` i `en-GB`, zielone regresje obu języków.
  — *GO*
- **M6** — niezmienny RC beta.12: numer + commit + SHA-256, ZIP zamrożony. — *nie rozpoczęty*
- **M7** — smoke na rozpakowanym ZIP-ie (istniejąca + czysta baza) → GO/FIX/HOLD. — *nie rozpoczęty*
- **M8** — publikacja dokładnie artefaktu z GO + checksuma + dokumentacja PL/EN. — *nie rozpoczęty*

---

## Po publikacji beta.12 — backlog

Pozycje świadomie odłożone poza łańcuch M0–M8. Nie mają gate'ów, nie blokują
publikacji i **nie wchodzą do beta.12**. Otwieramy je dopiero po zamknięciu M8.

### PROM — pełne odstępstwo promowe z art. 9

Decyzja z 2026-07-26: odłożone po publikacji. Powód — scalanie przerwanego
odpoczynku promowego dotyka tej samej maszynerii granic warunku, którą naprawiał
hotfix `7e90a36` („preserve moving break across condition boundary"), a M4 zamyka
UI freeze. To nie jest zmiana kosmetyczna, lecz nowa ścieżka w silniku liczników.

**Stan wejściowy.** Scaffolding istnieje w trzech warstwach i wymaga spięcia:

- Core: `FerryRestDerogation.Evaluate` sprawdza maks. dwie przerwy, łącznie do
  60 minut, dostęp do miejsca do spania, odpoczynek 11 h / 24 h / 45 h oraz
  przeprawę min. 8 h dla regularnego tygodniowego. **Zero wywołań
  produkcyjnych** — jedynym wywołującym jest
  `tests/ETS2Tachograph.Core.Tests/Ferry/FerryRestDerogationTests.cs`.
- Infrastructure: tabela `FerryRestRecords` i `FerryRestRecordEntity` istnieją od
  migracji `InitialPersistence`, ale **nic do nich nie pisze ani z nich nie
  czyta**.
- Engine/UI: `ActivityHistoryProcessor` przypina `SpecialCondition.FerryCrossing`,
  raporty prezentują odcinek jako `- prom`, `MainViewModel` pokazuje `Tryb: Prom`.
  Ta część działa i nie jest przedmiotem zadania.

**Zakres prac (kolejność obowiązująca).**

1. Spięcie Core z silnikiem: złożenie `FerryRestRequest` z realnej historii —
   bloki `BreakOrRest` z warunkiem `FerryCrossing`, przerwy między nimi jako
   `Interruptions`, `RestExcludingInterruptions` z sumy bloków. Największy zakres
   i największe ryzyko regresji.
2. Decyzja produktowa **przed kodowaniem UI**: `RestType`, `HasSleepingFacility`
   i `ScheduledCrossingDuration` nie są dostępne w telemetrii. Rekomendacja —
   deklaracja kierowcy w dialogu, wzorem wyboru roli bloku przy rekompensatach;
   zgadywanie przez silnik odrzucone.
3. Persystencja: weryfikacja, czy schemat `FerryRestRecords` odpowiada temu, co
   faktycznie ma być zapisywane. Możliwa nowa migracja — nie zakładać zgodności.
4. UI i raporty: prezentacja scalonego odpoczynku promowego oraz powodu odrzucenia
   z `FerryRestAssessment.Reason`. Komunikaty są dziś po angielsku — po M5 wymagają
   kluczy `.resx` w `pl-PL` i `en-GB`.

**Warunek zamknięcia.** Testy jednostkowe `FerryRestDerogation` już są zielone,
więc gate musi wymagać testów **integracyjnych**. Scenariusz odniesienia:
odpoczynek 11 h przerwany dwukrotnie — wjazdem i zjazdem z promu, łącznie 40 min
przerw — daje jeden zaliczony odpoczynek regularny dobowy, a nie trzy osobne bloki.

**Obserwacja P2 do rozstrzygnięcia w kroku 1.** `condition` jest jednowartościowy
i `FerryModeEnabled` wygrywa przed `CrewBreakInMotion`
(`ActivityHistoryProcessor.cs:449`). Podczas przeprawy w podwójnej obsadzie minuty
slotu 2 tracą oznaczenie przerwy w ruchu. Nie ustalono, czy to celowe. Nie ruszać
przed publikacją.
