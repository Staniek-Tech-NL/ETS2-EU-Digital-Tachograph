# PLAN BETA.12 — KAMIENIE MILOWE M0–M8

**Projekt:** ETS2 EU Digital Tachograph  
**Data planu nadrzędnego:** 24 lipca 2026  
**Baza wydaniowa:** `0.1.0-beta.11.1`  
**Cel:** przygotowanie `0.1.0-beta.12` jako ostatniej bety przed pierwszą szeroką publikacją.

## Obowiązujący ciąg

```text
M0 stabilizacja
→ M1 kontrakty i czerwone testy Planera
→ M2 silnik Planera
→ M3A Game Calendar & Deadline Presentation
→ M3 Application Service i UI Planera
→ M4 finalizacja UI i UI freeze
→ M5 lokalizacja PL/EN
→ M6 Release Candidate beta.12
→ M7 końcowy smoke beta.12
→ M8 publikacja
```

## Pliki

| Etap | Status początkowy | Plik |
|---|---|---|
| M0 | DO WYKONANIA | [M0 — Stabilizacja stanu wejściowego](M0_STABILIZACJA_STANU_WEJSCIOWEGO.md) |
| M1 | NIE ROZPOCZĘTY | [M1 — Planer: kontrakty i czerwone testy](M1_PLANER_KONTRAKTY_I_CZERWONE_TESTY.md) |
| M2 | NIE ROZPOCZĘTY | [M2 — Planer: silnik zdarzeniowy](M2_PLANER_SILNIK_ZDARZENIOWY.md) |
| M3A | AUTOMATYCZNIE DOMKNIĘTY / HOLD DO RĘCZNEGO GATE’U UI | [M3A — Game Calendar & Deadline Presentation](M3A_GAME_CALENDAR_AND_DEADLINE_PRESENTATION.md) |
| M3 | NIE ROZPOCZĘTY | [M3 — Planer: Application Service i UI](M3_PLANER_APPLICATION_SERVICE_I_UI.md) |
| M4 | NIE ROZPOCZĘTY | [M4 — Finalizacja całego interfejsu i UI freeze](M4_FINALIZACJA_UI_I_UI_FREEZE.md) |
| M5 | NIE ROZPOCZĘTY | [M5 — Lokalizacja PL/EN](M5_LOKALIZACJA_PL_EN.md) |
| M6 | NIE ROZPOCZĘTY | [M6 — Release Candidate `0.1.0-beta.12`](M6_RELEASE_CANDIDATE_BETA_12.md) |
| M7 | NIE ROZPOCZĘTY | [M7 — Końcowy smoke test beta.12](M7_SMOKE_TEST_BETA_12.md) |
| M8 | NIE ROZPOCZĘTY | [M8 — Publikacja](M8_PUBLIKACJA.md) |


## Zasady przejścia

- Następny etap rozpoczyna się dopiero po spełnieniu gate’u poprzedniego.
- P0 i P1 blokują przejście dalej.
- Każdy potwierdzony błąd najpierw otrzymuje test regresyjny.
- Zmiana po zbudowaniu RC unieważnia smoke i wymaga nowego artefaktu oraz SHA-256.
- Artefakt opublikowany musi być dokładnie tym, który uzyskał decyzję GO w M7.

## Zakres obowiązkowy beta.12

1. Planer podróży — strategia „Najwcześniejsza legalna”.
2. Dokończenie funkcjonalności całego interfejsu.
3. Pełna lokalizacja `pl-PL` i `en-GB`.
4. Pełna regresja wydaniowa.
5. Niezmienny artefakt beta.12 i końcowy smoke.
6. Publikacja po decyzji GO.

## Poza zakresem bez formalnej zmiany

Kolejne języki, dynamiczna zmiana języka, nowe strategie Planera, przyszłe zmiany kierowców, nowa strategia 15+30, reguła pierwszej godziny multi-manning, odpoczynek 3+9, ciągłość przez wiele luk, cold retention, automatyczne kasowanie historii, Annex 1C, instalator, podpis, auto-update i nowe integracje zewnętrzne.

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`.
