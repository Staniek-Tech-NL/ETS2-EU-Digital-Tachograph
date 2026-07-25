# M3.6 — Wewnętrzny smoke checkpoint (próba generalna RC)

**Projekt:** ETS2 EU Digital Tachograph  
**Artefakt:** `0.1.0-beta.12-rc3` — gotowy do dalszego smoke
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status bieżący:** **FIX — RC3 GOTOWY, POZOSTAŁY SMOKE OCZEKUJE**
**Kryterium wejścia:** Formalny wynik **GO** dla M3.5.  
**Kryterium wyjścia:** Triaging zakończony, potwierdzone błędy naprawione **przed** M4, formalna decyzja **GO / FIX / HOLD** dla checkpointu.  
**Następny etap:** M4

**Wejście potwierdzone 2026-07-24:** M3.5 zakończone wynikiem **GO**;
pełna regresja 521/521, build Release 0/0, ręczny gate UI zaakceptowany.

> Ten dokument jest samodzielnym wydzieleniem etapu M3.6 z planu wydania beta.12.
> Nie zmienia zakresu ani gate’ów planu nadrzędnego. Etap został wstawiony między
> M3.5 a M4 decyzją właściciela z 2026-07-24.

**Cel:** złapać błędy wielogodzinnej gry **wcześnie** — przed UI freeze (M4) —
gdy poprawki mogą jeszcze lądować swobodnie, zamiast odkładać całe testowanie
in-game do końcowego smoke M7.

## Charakter etapu

- Budujemy **zamrożony wewnętrzny artefakt** `beta.12-rc1` z konkretnego commita
  (numer + commit + wewnętrzna SHA-256), jak w dyscyplinie M6.
- **To nie jest publikacja.** Nie uruchamia bramki M8, nie tworzy publicznej
  paczki i nie wchodzi na osobną linię wersji. Etykieta `-rc1` oznacza próbę
  generalną RC beta.12, nie wydanie „beta.11.5”.
- Smoke wykonujemy wyłącznie na **rozpakowanym artefakcie**, nigdy z IDE, z
  pluginem z tej samej paczki.

## Granica pokrycia

Ten checkpoint jest **przed M5 (lokalizacja PL/EN)**. Testuje funkcję i przepływy
in-game, **nie** zlokalizowane stringi. Końcowy smoke **M7 na zlokalizowanym RC
pozostaje wymagany** i nie jest zastępowany przez M3.6.

## Przygotowanie

- [x] Zbudować Release z zamrożonego commita i spakować `beta.12-rc1`.
- [x] Zapisać numer, commit i wewnętrzną SHA-256 artefaktu.
- [x] Rozpakować ZIP do nowego katalogu; nie uruchamiać z IDE ani z ZIP-a.
- [ ] Zainstalować DLL pluginu z tej samej paczki i uruchomić ETS2 ponownie.
- [ ] Zachować kopię istniejącej bazy oraz przygotować wariant czystej bazy.

## Scenariusze smoke — celowane w ryzyka beta.12

### Istniejąca baza (sesja wielogodzinna, aktywna telemetria v3)

- [ ] Start bez błędów; odtworzenie profili, kart, ustawień i historii.
- [ ] **Załoga w ruchu:** S1 jedzie, S2 bierze 45 min przerwy w ruchu, S2
      przejmuje bez postoju pojazdu; osobne liczniki obu kart poprawne.
- [ ] **Granica pauzy 44/45** in-game jest poprawna (residuum otwarte od M0).
- [ ] **Kalendarz przez zmianę dni i tygodnia:** terminy `ODP. DZIENNY`,
      `ODP. TYG.` i rekompensat są stabilne i zgodne z tygodniem regulacyjnym;
      sprawdzić różne `WeekEpochOffsetDays`.
- [ ] **Rekompensaty przez restart + warm:** zapłać zobowiązanie, zrestartuj,
      potwierdź `PaidOnTime` po rejestracji załogi (scenariusz z `d3e21b4`).
- [ ] Planer tworzy legalny harmonogram i unieważnia stale snapshot.
- [ ] **Wariant B Raporty:** podgląd = plik dla PDF/JSON/CSV; eksport nie używa
      nieaktualnych parametrów.
- [ ] Nakładki S1/S2 zgodne z Dashboardem; OUT/Prom; auto-Jazda i blokady od ruchu.
- [ ] Zamknięcie i ponowny start odtwarzają identyczne dane; logi bez nowych
      wyjątków i błędów bindingów.

### Czysta baza

- [ ] Pierwszy start tworzy bazę i ustawienia; profil i karta działają.
- [ ] Podstawowy przepływ telemetrii, Planer i raport podstawowy działają.
- [ ] Włożenie/wyjęcie karty i oba sloty działają.

## Reguła unieważnienia smoke

Każda zmiana kodu, zasobów, konfiguracji, pluginu lub zawartości paczki po
zbudowaniu `beta.12-rc*`:

```text
unieważnia dotychczasowy smoke
→ wymaga nowego artefaktu
→ wymaga nowej wewnętrznej SHA-256
→ wymaga ponownego testu odpowiedniego zakresu
```

### Unieważnienie `rc0` — 2026-07-24

Artefakt `rc0` został unieważniony po potwierdzeniu problemu z głównym oknem:
po przełączeniu Alt+Tab aplikacja pozostawała pod pełnoekranowym lub borderless
ETS. Nakładki S1/S2 działały prawidłowo i nie zostały zmienione.

Pierwsza próba oparta na zmianie właściwości WPF `Topmost` powodowała
reentrantny cykl aktywacji i `AppHangB1`; została odrzucona. Poprawka docelowa
używa niereentrantnego `SetWindowPos` z `SWP_NOACTIVATE`, podnosząc wyłącznie
aktywne główne okno i przywracając jego normalny poziom po oddaniu fokusu do ETS.
Nakładki pozostają nietknięte.

Regresja Desktop: **97/97**, build: **0/0**. Rzeczywisty test procesu:
start → minimalizacja → przywrócenie/aktywacja → responsywność → zamknięcie
z kodem `0` — **PASS**. Ręczny test z ETS został potwierdzony przez właściciela
wynikiem **PASS**. Następnym artefaktem jest `rc1`; `rc0` pozostaje historyczny
i unieważniony.

### Unieważnienie `rc1` — 2026-07-24

Test po wczytaniu zapisu wykazał, że mechanizm `SetWindowPos` wprowadzony w
`eb1f02d` nie rozwiązuje problemu również przy ETS uruchomionym w oknie.
Artefakt `rc1` został unieważniony. Ręczne sterowanie Z-orderem oraz testy tej
odrzuconej polityki usunięto, przywracając model okna identyczny z beta.11.
Nakładki S1/S2 pozostały nietknięte.

Ponowny test po tej eliminacji nadal odtwarzał objaw, dlatego wcześniejsze
przypisanie przyczyny do Z-orderu uznano za niepełne. Audyt działającego procesu
po wczytaniu świata wykazał nasycenie głównego wątku UI. Ścieżka dodana w M3
wywoływała `RefreshReadinessAsync()` przy każdej ramce telemetrii (około 10 Hz).
Każde wywołanie ładowało pełną historię i luki obu kart, a cztery odczyty były
uruchamiane równolegle na współdzielonym kontekście bazy. Zadania piętrzyły się,
więc po Alt+Tab system wybierał aplikację, ale jej dispatcher nie przetwarzał
aktywacji i odmalowania okna.

Chirurgiczna korekta zachowuje na ścieżce każdej ramki wyłącznie tanią
walidację tożsamości istniejącego wyniku. Gotowość Planera jest odświeżana przy
wejściu do zakładki oraz przed obliczeniem planu, a odczyty historii obu kart są
wykonywane sekwencyjnie na współdzielonym kontekście. Dwa testy regresyjne
przypinają brak odświeżania gotowości przez telemetrię oraz maksymalnie jeden
aktywny odczyt repozytorium. Testy wykazały przed poprawką odpowiednio
**101 zamiast 1** wywołań oraz **4 zamiast 1** równoległych odczytów; po
poprawce są zielone.

Pełna regresja sekwencyjna: **522/522**, build Release:
**0 błędów / 0 ostrzeżeń**. Ręczny test Alt+Tab po wczytaniu zapisu został
potwierdzony przez właściciela wynikiem **PASS** 2026-07-24. Korekta jest gotowa
do commita i zbudowania kolejnego artefaktu.

### Unieważnienie `rc2` — 2026-07-25

Pełny smoke na świeżych danych wykazał regresję kwalifikacji przerwy karty
w slocie 2. Kanoniczna historia karty `Staniek` prawidłowo zapisała jeden
ciągły blok `BreakOrRest`:

```text
1 min  — postój, Condition=None
44 min — jazda pojazdu, Condition=CrewBreakInMotion
razem  — 45 min ciągłej przerwy
```

Licznik załogi kończył przerwę po 45 minutach, ale RuleEngine analizował oba
warunki jako dwa osobne biegi `1 + 44`. Żaden bieg samodzielnie nie osiągał
45 minut, dlatego jazda ciągła karty nie była zerowana. Test regresyjny przed
poprawką odtworzył stan **62 min zamiast 0**.

Korekta scala sąsiadujące biegi `BreakOrRest` wyłącznie na potrzeby art. 7:
kwalifikacji przerwy 45 minut oraz licznika bieżącej przerwy. Warunek
`CrewBreakInMotion` nadal wyklucza te minuty z odpoczynku dobowego i
tygodniowego. Historia, źródła, warunki i zapisane rekordy nie są zmieniane;
świeże dane smoke zostaną prawidłowo przeliczone po uruchomieniu poprawionego
RuleEngine.

Dwa testy test-first pokrywają bezpośrednio RuleEngine oraz pełny przepływ
`CrewTachographEngine`. Gate po poprawce: RuleEngine **168/168**, Engine
**70/70**, pełna regresja **524/524**, build Release
**0 błędów / 0 ostrzeżeń**. Artefakt `rc2` pozostaje unieważniony.

Ręczny retest na świeżych danych i aktywnej telemetrii został potwierdzony przez
właściciela 2026-07-25: karta w slocie 2 prawidłowo zaliczyła ciągłą przerwę
`1 min postój + 44 min ruch = 45 min`. Ponowne uruchomienie wyłącznie poprawionej
aplikacji podpięło istniejącą telemetrię bez restartu ETS2. Zakres poprawki jest
zielony; następnym krokiem jest commit i nowy artefakt `rc3`.

## Decyzja M3.6

- **GO** — scenariusze spełnione, brak P0/P1; można wejść w M4 (UI freeze).
- **FIX** — potwierdzony błąd: najpierw test regresyjny, potem minimalna
  poprawka; poprawka ląduje **przed** M4, po czym budujemy nowy `rc` i ponawiamy
  odpowiedni zakres smoke.
- **HOLD** — naruszenie historii, RuleEngine, persistence, migracji, eksportów
  albo stabilności startu.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Poprawki z M3.6 muszą wejść przed M4; po UI freeze obowiązują ograniczenia M4.
6. Nie rozszerzać zakresu „przy okazji”.

## Najważniejsze ryzyka M3.6

- testowanie innego builda niż zamrożony artefakt;
- poprawka po rozpoczęciu smoke bez nowego artefaktu i hasha;
- mylenie checkpointu z publikacją albo z osobnym wydaniem `beta.11.5`;
- pominięcie czystej bazy lub aktywnej telemetrii;
- przeoczenie, że lokalizacja (M5) nie jest jeszcze objęta — konieczny M7.

## Szablon aktualizacji statusu

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:**
- **Wynik:** `FIX — RC3 GOTOWY, POZOSTAŁY SMOKE OCZEKUJE`
- **Artefakt bieżący:** `ETS2Tachograph-0.1.0-beta.12-rc3-win-x64.zip`
- **Commit bieżący:** `7e90a3620e68b7dc8598d733bc17eac4f5e488e6`
- **SHA-256 bieżący:** `F9E85D58E37EF381D5DD435222A7594A34430B99F1E69DDD669CF608899D026E`
- **Artefakt historyczny, unieważniony:** `ETS2Tachograph-0.1.0-beta.12-rc2-win-x64.zip`
- **Commit historyczny:** `1c87e004b629e0b691db1eb18e0952fa5641d8fe`
- **SHA-256 historyczny:** `30CD7BD2B65D2C59DD7F2306FF6D3A129D4AA5BCC61C78F5E7381B8C9A8E5ECA`
- **Artefakt historyczny, unieważniony:** `ETS2Tachograph-0.1.0-beta.12-rc1-win-x64.zip`
- **Commit historyczny:** `eb1f02d765a7c0f2cabea57e047ff74198c12975`
- **SHA-256 historyczny:** `25AD18A416A86A1D63D7F4B7C0B9D3400B9E3CB284CE2A54924DEB68D14078EE`
- **Artefakt historyczny, unieważniony:** `ETS2Tachograph-0.1.0-beta.12-rc0-win-x64.zip`
- **Commit historyczny:** `0abe849d01cd3e01c871d812adcc7c8c6eb31830`
- **SHA-256 historyczny:** `727C51F40515EF3909E3282C553451711D665CD688F3C72ABE0DDEEB92073406`
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 524/524 sekwencyjnie po poprawce przerwy slotu 2
- **Testy manualne / dowody:** Alt+Tab — PASS; przerwa slotu 2 `1 + 44 = 45`
  — PASS na świeżych danych i aktywnej telemetrii
- **Weryfikacja paczki `rc3`:** świeże rozpakowanie, 454 pliki zgodne bajtowo,
  `FileVersion 0.1.12.3`, dokładny `ProductVersion`, plugin v3 i checksumy — PASS
- **Weryfikacja paczki `rc2`:** struktura i checksumy — PASS, funkcjonalny smoke
  — FAIL
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** wznowić pozostały zakres smoke na rozpakowanym
  artefakcie `rc3`.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `M3_5_RAPORTY_WARIANT_B.md`,
`M4_FINALIZACJA_UI_I_UI_FREEZE.md`, `M6_RELEASE_CANDIDATE_BETA_12.md`,
`M7_SMOKE_TEST_BETA_12.md`, `BETA_TEST_PLAN.md`.
