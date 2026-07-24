# M4 — Finalizacja całego interfejsu i UI freeze

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Planer M3 jest zintegrowany i przechodzi testy funkcjonalne oraz wizualne.  
**Kryterium wyjścia:** Wszystkie funkcje UI zakończone i formalny **UI freeze**.  
**Następny etap:** M5

> Ten dokument jest samodzielnym wydzieleniem etapu M4 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** zamknąć funkcjonalnie cały interfejs przed lokalizacją.

### Zadania

- [ ] Zamknąć wszystkie pozycje z inwentaryzacji UI.
- [ ] Ujednolicić nawigację, skróty, potwierdzenia i błędy.
- [ ] Ujednolicić Dashboard, urządzenie i nakładki.
- [ ] Sprawdzić wszystkie puste stany i stany błędów.
- [ ] Sprawdzić oba sloty we wszystkich kluczowych przepływach.
- [ ] Sprawdzić restart i trwałość ustawień.
- [ ] Sprawdzić raporty, wydruki i eksporty.
- [ ] Sprawdzić minimalny rozmiar okna i skalowanie.
- [ ] Usunąć błędy bindingów i nieużywane elementy.
- [ ] Wprowadzić zamrożenie układu UI.

### Gate M4

- wszystkie funkcje UI zakończone;
- brak otwartych P0/P1;
- brak nieprzypisanych elementów z inwentaryzacji;
- pełna checklista XAML zielona;
- formalny **UI freeze**.

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

- **Data rozpoczęcia:**
- **Data zakończenia:**
- **Wynik:** `GO` / `FIX` / `HOLD` / `NIE DOTYCZY`
- **Commit / punkt przywracania:**
- **Build Release:**
- **Testy automatyczne:**
- **Testy manualne / dowody:**
- **Otwarte błędy P0:**
- **Otwarte błędy P1:**
- **Uwagi do następnego etapu:**

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
