# M4 — Finalizacja całego interfejsu i UI freeze

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status bieżący:** **ZAMKNIĘTY — GO / UI FREEZE**

**Data rozpoczęcia:** 27 lipca 2026

**Data zakończenia:** 27 lipca 2026

**Kryterium wejścia:** Formalne **GO M4-0** po zatwierdzeniu kompletnej
inwentaryzacji UI i osobistej weryfikacji rozpakowanego rc4.
**Kryterium wyjścia:** Wszystkie funkcje UI zakończone i formalny **UI freeze**.  
**Następny etap:** M5

> Ten dokument jest samodzielnym wydzieleniem etapu M4 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zamknąć funkcjonalnie cały interfejs przed lokalizacją. Źródłem zakresu
jest `M4_0_INWENTARYZACJA_UI.md`; M4 nie tworzy ani nie rozszerza własnej
inwentaryzacji.

## Otwarcie etapu

- **Wejście:** spełnione — M4-0 zamknięte wynikiem GO.
- **Wynik inwentaryzacji:** 62/62 pozycji `beta.12` PASS, 4 pozycje poza
  zakresem N/D, 0 pozycji przypisanych do naprawy w M4.
- **Artefakt dowodowy:** rozpakowany
  `ETS2Tachograph-0.1.0-beta.12-rc4-win-x64.zip`, commit źródłowy
  `a1b8a486b52ee244984016efe268562690d4fbc4`.
- **Dryf kodu:** 0 zmian w `src/`, `tests/`, `native/`, `tools/` i rozwiązaniu
  między commitem rc4 a punktem rozpoczęcia M4.
- **Build Release:** 0 błędów, 0 ostrzeżeń.
- **Testy automatyczne:** 538/538 PASS.
- **Decyzja otwarcia:** M4 realizuje walidację gotowego interfejsu i formalny
  freeze; brak podstaw do zmian funkcjonalnych lub zmian XAML na starcie.

### Zadania

- [x] Zamknąć wszystkie pozycje z inwentaryzacji UI.
- [x] Ujednolicić nawigację, skróty, potwierdzenia i błędy.
- [x] Ujednolicić Dashboard, urządzenie i nakładki.
- [x] Sprawdzić wszystkie puste stany i stany błędów.
- [x] Sprawdzić oba sloty we wszystkich kluczowych przepływach.
- [x] Sprawdzić restart i trwałość ustawień.
- [x] Sprawdzić raporty, wydruki i eksporty.
- [x] Sprawdzić minimalny rozmiar okna i skalowanie.
- [x] Potwierdzić brak błędów bindingów i nieużywanych elementów.
- [x] Wprowadzić zamrożenie układu UI.

### Gate M4

- [x] wszystkie funkcje UI zakończone;
- [x] brak otwartych P0/P1;
- [x] brak nieprzypisanych elementów z inwentaryzacji;
- [x] pełna checklista XAML zielona;
- [x] formalny **UI freeze**.

## Walidacja gate'u

| Warunek | Wynik | Dowód |
|---|---|---|
| Gotowe funkcje UI | PASS | M4-0: 62/62 pozycji `beta.12` PASS na rozpakowanym rc4 |
| P0/P1 | PASS | 0/0 w zatwierdzonej inwentaryzacji |
| Przypisanie elementów | PASS | 62 `beta.12`, 4 N/D, 0 bez decyzji i 0 przypisanych do naprawy M4 |
| Checklista XAML | PASS | rc4: minimalny rozmiar, skalowanie, klawiatura, oba sloty, nakładki, restart, puste stany oraz log bindingów zielone |
| Brak dryfu po rc4 | PASS | 0 zmian w kodzie i testach między `a1b8a486` a punktem rozpoczęcia M4 |
| Kompilacja i automatyka | PASS | Release 0 błędów / 0 ostrzeżeń; 538/538 testów |
| Audyt statyczny UI | PASS | 3/3 XAML poprawne, 8/8 procedur zdarzeń istnieje, 0 placeholderów i 0 statycznie wyłączonych lub ukrytych martwych bloków |
| UI freeze | PASS | formalna zgoda właściciela 2026-07-27; M4 zamknięte wynikiem GO |

## Decyzja końcowa

- **Data zakończenia:** 2026-07-27.
- **Wynik:** **GO — UI FREEZE**.
- **Punkt przywracania:** commit zamykający M4.
- **Build Release:** 0 błędów, 0 ostrzeżeń.
- **Testy automatyczne:** 538/538 PASS.
- **Testy manualne / dowody:** 62/62 pozycji `beta.12` PASS na uruchomionym,
  rozpakowanym rc4; 4 pozycje poza zakresem N/D.
- **Otwarte błędy P0:** 0.
- **Otwarte błędy P1:** 0.
- **Decyzja:** układ oraz istniejące przepływy UI zostają zamrożone. Od tego
  punktu obowiązuje polityka zmian po freeze opisana poniżej.
- **Następny etap:** M5 odblokowany.

---

## Definicja dokończonego interfejsu

Zakres obejmuje Dashboard, Historię i luki, wariant B wpisu manualnego, katalog krajów i kod tachografowy, `ODP. TYG.`, Rekomensaty, Raporty i eksporty, Kierowców, Ustawienia, wirtualne urządzenie, oba sloty, nakładki S1/S2, tryby OUT i Prom, dialogi, komunikaty, walidacje, puste stany oraz Planer.

Interfejs jest ukończony, gdy wszystkie funkcje mają pełny przepływ od wejścia do wyniku, brak martwych lub częściowo wdrożonych kontrolek, brak błędów bindingów i wyjątków UI, a Dashboard, urządzenie i nakładki pokazują spójny stan. Układ musi pozostać używalny przy minimalnym wspieranym rozmiarze okna oraz dla dłuższych tekstów angielskich.

## Polityka UI freeze

Po formalnym zamknięciu M4 nie dodaje się nowych przepływów ani reorganizacji ekranu. Dopuszczalne są wyłącznie:

- poprawki potwierdzonych błędów;
- dostosowania wymagane przez lokalizację;
- usunięcie obcięć i przepełnień;
- poprawki dostępności i czytelności, które nie zmieniają funkcji.

Każda zmiana XAML po freeze nadal wymaga pełnej checklisty regresji.

## Poza zakresem M4

- tłumaczenie PL/EN;
- nowe funkcje Planera;
- instalator, podpis kodu i auto-update;
- tematy backlogu wymienione jako poza beta.12.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M4

- niekontrolowane dodawanie nowych funkcji podczas porządkowania UI;
- pozostawienie nieużywanych kontrolek lub niejawnych przepływów;
- obcięcia, błędy klawiatury i niespójne potwierdzenia;
- zamrożenie UI przed zamknięciem wszystkich pozycji inwentaryzacji.

## Szablon aktualizacji statusu

- **Data rozpoczęcia:** 2026-07-27
- **Data zakończenia:** 2026-07-27
- **Wynik:** **GO — UI FREEZE**
- **Commit / punkt przywracania:** commit zamykający M4
- **Build Release:** 0 błędów, 0 ostrzeżeń
- **Testy automatyczne:** 538/538 PASS
- **Testy manualne / dowody:** rc4, 62/62 pozycji `beta.12` PASS; 4 N/D
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M5 odblokowany; zmiany UI podlegają polityce
  freeze i nie mogą dodawać nowych przepływów.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `M4_0_INWENTARYZACJA_UI.md`, `PROJECT_HANDOFF.md`,
`README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`,
`JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`,
`RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`,
`WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
