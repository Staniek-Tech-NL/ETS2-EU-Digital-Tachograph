# M3.5 — Raporty i statystyki: wariant B

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status bieżący:** **ZAKOŃCZONY — GO**
**Kryterium wejścia:** Formalny wynik **GO** dla M3.  
**Kryterium wyjścia:** Gate Etapu 7 planu wykonawczego wariantu B spełniony, pełna checklista UI zielona, build Release 0/0.  
**Następny etap:** M3.6

> Ten dokument jest samodzielnym wydzieleniem etapu M3.5 z planu wydania beta.12.
> Nie zmienia zakresu ani gate’ów planu nadrzędnego. Etap został wstawiony między
> M3 a M4 decyzją właściciela z 2026-07-24 (Okno A: przed UI freeze).

**Cel:** przebudować ekran `RAPORTY I STATYSTYKI` do wariantu B (centrum
raportowe: KONFIGURACJA → KONTROLA DANYCH → PODGLĄD → EKSPORT) bez zmiany silnika
raportowego, historii kanonicznej ani kontraktów maszynowych.

## Źródło wykonawcze

Pełny plan zadań, kontrakty funkcjonalne, macierz testów, ręczna regresja z IDE
i kryteria akceptacji znajdują się w:

```text
docs/PLAN_WDROZENIA_RAPORTY_WARIANT_B.md
```

Ten dokument etapu jest cienką warstwą wpinającą tamten plan w łańcuch M0–M8. W
razie rozbieżności obowiązuje niniejszy plik w zakresie kryteriów wejścia/wyjścia
i pozycji w łańcuchu; treść wykonawcza pozostaje w planie wariantu B.

## Odświeżony stan wejściowy (obowiązuje zamiast migawki z planu wariantu B)

Plan wykonawczy powstał na etapie M0 i opisuje bazę `315/315` sprzed prac
Planera. Rzeczywisty stan wejścia do M3.5:

- baza automatyczna po M3A: **478/478**, build Release **0/0** (2026-07-24);
- Planer jest już wpięty w `MainWindow.xaml` i `MainViewModel.cs`;
- wspólna warstwa prezentacji terminów `GameCalendar`/`GameDeadline` (M3A) jest
  dostępna i **ma być konsumowana** przez terminy w Raportach, nie kodowana
  ponownie;
- **Etap 0 audytu wariantu B musi zostać wykonany na stanie po M3**, nie na
  migawce sprzed M1.

## Zakres

W zakresie i poza zakresem — zgodnie z sekcją 4 planu wariantu B. Kluczowe
granice powtórzone dla jednoznaczności:

- **W zakresie:** przebudowa sekcji Raporty w XAML, wydzielenie
  `ReportsWorkspaceViewModel`, presety `game_time`, jawne stany podglądu, pasek
  kompletności, kafle, zakładki, wspólny przepływ podgląd → eksport, testy
  Desktop i regresja eksportów.
- **Poza zakresem:** zmiana `RuleEngine`, historii kanonicznej, progów prawnych,
  migracji SQLite, protokołu pluginu v3, wyglądu PDF, nazw pól JSON; dynamiczna
  lokalizacja PL/EN; Planer podróży.

## Niezmienniki

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. UI nie wylicza reguł tachografu ani zobowiązań.
3. Raport działa na `game_time`, nie na zegarze Windows.
4. Terminy w Raportach korzystają z warstwy `GameCalendar`/`GameDeadline` z M3A.
5. JSON, CSV, `.tacho`, SQLite i protokół telemetrii pozostają kompatybilne.
6. Eksport z nieaktualnego podglądu najpierw przelicza raport.
7. Plik eksportowy i podgląd po eksporcie pochodzą z tego samego `ReportDto`.

## Gate M3.5

- [x] Etap 0 audytu wykonany na stanie po M3; jeden przepływ generowania i jeden
      przepływ eksportu bez otwartej niejasności;
- [x] kryteria akceptacji właściwe dla M3.5 spełnione; rozszerzony smoke
      artefaktu pozostaje bramką M3.6;
- [x] macierz testów automatycznych `RPT-RNG`, `RPT-STATE`, `RPT-EXP` i zgodność
      danych zielone;
- [x] ręczny gate UI zatwierdzony przez właściciela produktu;
- [x] terminy w Raportach korzystają z warstwy M3A, bez lokalnej reimplementacji;
- [x] eksporty PDF/JSON/CSV zachowują dotychczasowe kontrakty danych;
- [x] build Release 0/0 i pełny pakiet testów zielony;
- [x] aplikacja uruchamia się po zmianach XAML; start i utworzenie podglądu
      potwierdzone w logu diagnostycznym;
- [x] **nie utworzono paczki beta** — wynik wchodzi do wewnętrznego smoke M3.6.

## Stan wykonania 2026-07-24

- Etap 0: **PASS** — wynik w `docs/M3_5_ETAP_0_AUDYT_RAPORTOW.md`.
- Wydzielono `ReportsWorkspaceViewModel`; `MainViewModel` odpowiada wyłącznie za
  integrację, nawigację i wybór pliku.
- Działają cztery presety `game_time`, jawny pusty stan, walidacja własnego
  zakresu, statusy podglądu, sześć kafli i pięć zakładek.
- Eksport nieaktualnego widoku najpierw tworzy nowy podgląd, a następnie przekazuje
  ten sam `ReportDto` do PDF, VTC JSON, CSV zobowiązań albo surowego CSV.
- Release: **0 błędów / 0 ostrzeżeń**.
- Pełna regresja: **521/521**.
- Test startu: **PASS** — aplikacja osiągnęła `APP_READY` i utworzyła podgląd
  raportu z historii produkcyjnej.
- Ręczny gate UI: **PASS** — właściciel zatwierdził widok po korektach
  kosmetycznych nagłówka, kafli, podsumowania i kompletności.
- Formalny wynik etapu: **GO**. Rozszerzone sprawdzenie eksportów na zamrożonym
  artefakcie pozostaje scenariuszem M3.6.

## Zasady obowiązujące na tym etapie

1. Każdy potwierdzony błąd otrzymuje test regresyjny przed poprawką.
2. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
3. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
4. Nie rozszerzać zakresu „przy okazji”; semantyka CSV zobowiązań i historii
   kanonicznej nie jest zmieniana w ramach przebudowy XAML.
5. Nowe teksty otrzymują nazwy gotowe do przyszłej migracji `.resx` (M5).

## Najważniejsze ryzyka M3.5

- dalsze rozrastanie `MainViewModel` zamiast wydzielenia workspace;
- podgląd i plik z różnych wyników dla tych samych parametrów;
- off-by-one dnia lub końca zakresu `game_time`;
- reimplementacja terminów zamiast użycia warstwy M3A;
- kolizja XAML z zakładką Planera z M3, jeśli M3 nie jest domknięte.

## Szablon aktualizacji statusu

- **Data rozpoczęcia:** 2026-07-24
- **Data zakończenia:** 2026-07-24
- **Wynik:** **GO**
- **Commit / punkt przywracania:** commit domknięcia M3.5 zawierający ten dokument
- **Build Release:** 0 błędów / 0 ostrzeżeń
- **Testy automatyczne:** 521/521
- **Testy manualne / dowody:** start aplikacji i utworzenie podglądu
  potwierdzone; ręczny gate UI zatwierdzony przez właściciela
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M3.6 odblokowane; przygotować zamrożony,
  niepublikowany artefakt `beta.12-rc0`.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Plan wykonawczy:** `PLAN_WDROZENIA_RAPORTY_WARIANT_B.md`  
**Dokumenty powiązane:** `M3_PLANER_APPLICATION_SERVICE_I_UI.md`,
`M3A_GAME_CALENDAR_AND_DEADLINE_PRESENTATION.md`,
`M3_6_WEWNETRZNY_SMOKE_CHECKPOINT.md`, `M4_FINALIZACJA_UI_I_UI_FREEZE.md`,
`BETA_TEST_PLAN.md`, `RAPORT_PRAC_UI_2026-07-23.md`.
