# M3.6 — Wewnętrzny smoke checkpoint (próba generalna RC)

**Projekt:** ETS2 EU Digital Tachograph  
**Artefakt:** `0.1.0-beta.12-rc0` — **wewnętrzny, niepublikowany**  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Formalny wynik **GO** dla M3.5.  
**Kryterium wyjścia:** Triaging zakończony, potwierdzone błędy naprawione **przed** M4, formalna decyzja **GO / FIX / HOLD** dla checkpointu.  
**Następny etap:** M4

> Ten dokument jest samodzielnym wydzieleniem etapu M3.6 z planu wydania beta.12.
> Nie zmienia zakresu ani gate’ów planu nadrzędnego. Etap został wstawiony między
> M3.5 a M4 decyzją właściciela z 2026-07-24.

**Cel:** złapać błędy wielogodzinnej gry **wcześnie** — przed UI freeze (M4) —
gdy poprawki mogą jeszcze lądować swobodnie, zamiast odkładać całe testowanie
in-game do końcowego smoke M7.

## Charakter etapu

- Budujemy **zamrożony wewnętrzny artefakt** `beta.12-rc0` z konkretnego commita
  (numer + commit + wewnętrzna SHA-256), jak w dyscyplinie M6.
- **To nie jest publikacja.** Nie uruchamia bramki M8, nie tworzy publicznej
  paczki i nie wchodzi na osobną linię wersji. Etykieta `-rc0` oznacza próbę
  generalną RC beta.12, nie wydanie „beta.11.5”.
- Smoke wykonujemy wyłącznie na **rozpakowanym artefakcie**, nigdy z IDE, z
  pluginem z tej samej paczki.

## Granica pokrycia

Ten checkpoint jest **przed M5 (lokalizacja PL/EN)**. Testuje funkcję i przepływy
in-game, **nie** zlokalizowane stringi. Końcowy smoke **M7 na zlokalizowanym RC
pozostaje wymagany** i nie jest zastępowany przez M3.6.

## Przygotowanie

- [ ] Zbudować Release z zamrożonego commita i spakować `beta.12-rc0`.
- [ ] Zapisać numer, commit i wewnętrzną SHA-256 artefaktu.
- [ ] Rozpakować ZIP do nowego katalogu; nie uruchamiać z IDE ani z ZIP-a.
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
zbudowaniu `beta.12-rc0`:

```text
unieważnia dotychczasowy smoke
→ wymaga nowego artefaktu
→ wymaga nowej wewnętrznej SHA-256
→ wymaga ponownego testu odpowiedniego zakresu
```

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

- **Data rozpoczęcia:**
- **Data zakończenia:**
- **Wynik:** `GO` / `FIX` / `HOLD` / `NIE DOTYCZY`
- **Artefakt / commit / SHA-256:**
- **Build Release:**
- **Testy automatyczne:**
- **Testy manualne / dowody:**
- **Otwarte błędy P0:**
- **Otwarte błędy P1:**
- **Uwagi do następnego etapu:**

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `M3_5_RAPORTY_WARIANT_B.md`,
`M4_FINALIZACJA_UI_I_UI_FREEZE.md`, `M6_RELEASE_CANDIDATE_BETA_12.md`,
`M7_SMOKE_TEST_BETA_12.md`, `BETA_TEST_PLAN.md`.
