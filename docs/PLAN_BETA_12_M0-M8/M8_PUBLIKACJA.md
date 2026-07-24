# M8 — Publikacja

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Decyzja **GO** po końcowym smoke teście M7.  
**Kryterium wyjścia:** Opublikowany dokładnie ten artefakt, który przeszedł smoke, wraz z dokumentacją i checksumą.  
**Następny etap:** Zamknięcie pierwszej publikacji i przejście do backlogu popublikacyjnego

> Ten dokument jest samodzielnym wydzieleniem etapu M8 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** opublikować zatwierdzony artefakt bez rozjazdu względem smoke testu.

### Zadania

- [ ] Zamrozić commit źródłowy.
- [ ] Potwierdzić zgodność SHA-256 z artefaktem smoke.
- [ ] Ustalić publiczny numer/tag wydania bazujący na zatwierdzonym kodzie beta.12.
- [ ] Nie dodawać funkcji między GO a publikacją.
- [ ] Opublikować paczkę aplikacji i pluginu.
- [ ] Opublikować checksumę SHA-256.
- [ ] Opublikować instrukcję instalacji PL i EN.
- [ ] Opublikować known issues.
- [ ] Opublikować release notes.
- [ ] Opublikować informację, że aplikacja jest symulatorem, a nie certyfikowanym tachografem.
- [ ] Opisać sposób zgłaszania błędów i generowania raportu diagnostycznego.
- [ ] Zachować `0.1.0-beta.11.1` jako historyczną bazę.
- [ ] Oznaczyć `0.1.0-beta.12` jako ostatnią betę.

### Gate publikacji

- artefakt publikowany jest dokładnie tym, który przeszedł smoke;
- dokumentacja PL/EN jest dostępna;
- checksumy są opublikowane;
- brak nieudokumentowanych zmian;
- model licencji, repozytorium i wsparcia jest jawnie określony;
- kanał zgłoszeń błędów jest gotowy.

---

## Decyzje wymagane przed publikacją

- publiczny numer lub tag bazujący na zatwierdzonym kodzie beta.12;
- miejsce publikacji paczki;
- widoczność repozytorium;
- licencja;
- model wsparcia i kanał zgłoszeń;
- potwierdzenie dystrybucji jako self-contained ZIP `win-x64`;
- potwierdzenie, że instalator, podpis i auto-update pozostają poza zakresem.

## Pakiet publikacyjny

- dokładny ZIP zatwierdzony w M7;
- SHA-256;
- aplikacja i właściwa DLL pluginu v3;
- instrukcja instalacji PL i EN;
- release notes;
- known issues;
- informacja prawna, że aplikacja jest symulatorem;
- instrukcja zgłaszania błędów i generowania raportu diagnostycznego.

Między GO a publikacją nie wolno dodawać funkcji ani zmieniać zawartości artefaktu.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M8

- publikacja artefaktu różnego od zatwierdzonego w M7;
- brak licencji, jawnego modelu wsparcia albo kanału zgłoszeń;
- nieopublikowana lub błędna checksuma;
- dodanie funkcji między GO a publikacją.

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
