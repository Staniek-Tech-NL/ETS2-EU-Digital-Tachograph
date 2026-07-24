# M7 — Końcowy smoke test beta.12

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Istnieje niezmienny ZIP beta.12 z numerem, commitem i SHA-256.  
**Kryterium wyjścia:** Formalna decyzja **GO / FIX / HOLD** dla dokładnego artefaktu beta.12.  
**Następny etap:** M8

> Ten dokument jest samodzielnym wydzieleniem etapu M7 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** potwierdzić dokładny artefakt przeznaczony do publikacji.

### Przygotowanie

- [ ] Rozpakować ZIP do nowego katalogu.
- [ ] Nie uruchamiać z IDE ani bezpośrednio z ZIP-a.
- [ ] Zainstalować DLL pluginu z tej samej paczki.
- [ ] Całkowicie uruchomić ETS2 ponownie.
- [ ] Zachować kopię istniejącej bazy.
- [ ] Zapisać hash testowanego ZIP-a.

### Smoke — istniejąca baza

- [ ] Start aplikacji bez błędów.
- [ ] Odtworzenie profili, kart, ustawień i historii.
- [ ] Poprawne rekompensaty i decyzje alokacji.
- [ ] Oba sloty działają.
- [ ] Aktywna telemetria v3.
- [ ] Ruch automatycznie ustawia Jazdę.
- [ ] Blokady zależne od ruchu działają.
- [ ] Tryby OUT i Prom działają.
- [ ] Załadunek/rozładunek nie tworzy fałszywej luki.
- [ ] Wpis manualny wariant B działa.
- [ ] Kraje i `ODP. TYG.` działają.
- [ ] Granica pauzy 44/45 jest poprawna.
- [ ] Planer tworzy legalny harmonogram i poprawnie unieważnia stale snapshot.
- [ ] Nakładki S1/S2 są zgodne z Dashboardem.
- [ ] PDF, CSV, JSON i `.tacho` działają.
- [ ] Raport diagnostyczny ZIP działa.
- [ ] Zmiana języka PL/EN utrzymuje się po restarcie.
- [ ] Aplikacja zamyka się czysto.
- [ ] Ponowny start odtwarza identyczne dane.
- [ ] Logi nie zawierają nowych wyjątków ani błędów bindingów.

### Smoke — czysta baza

- [ ] Pierwszy start tworzy bazę i ustawienia.
- [ ] Utworzenie profilu i karty działa.
- [ ] Włożenie/wyjęcie karty działa.
- [ ] Podstawowy przepływ telemetrii działa.
- [ ] Planer działa z minimalnym poprawnym stanem.
- [ ] Wybór języka działa po restarcie.
- [ ] Raport i eksport podstawowy działają.

### Reguła unieważnienia smoke

Każda zmiana kodu, zasobów, konfiguracji, pluginu, paczki lub dokumentów wchodzących do ZIP-a po rozpoczęciu testu:

```text
unieważnia dotychczasowy smoke
→ wymaga nowego artefaktu
→ wymaga nowego SHA-256
→ wymaga ponownego testu odpowiedniego zakresu
```

### Decyzja M7

**GO** — wszystkie kryteria spełnione, brak P0/P1.  
**FIX** — potwierdzony błąd z minimalną poprawką, testem regresyjnym i nowym artefaktem.  
**HOLD** — naruszenie historii, RuleEngine, persistence, migracji, eksportów albo stabilności startu.

---

## Kryteria wykonania smoke

Smoke jest wykonywany wyłącznie na rozpakowanym ZIP-ie, nigdy z IDE. Należy zapisać hash testowanej paczki i użyć pluginu z tej samej paczki. Test obejmuje istniejącą bazę, czystą bazę, aktywną telemetrię, oba sloty, Planer, wariant B, lokalizację, nakładki, raporty i restart.

## Klasyfikacja wyniku

- **GO** — wszystkie kryteria spełnione; brak P0/P1.
- **FIX** — potwierdzony błąd; najpierw test regresyjny, potem minimalna poprawka, nowy artefakt, nowy hash i ponowiony zakres smoke.
- **HOLD** — naruszenie historii, RuleEngine, persistence, migracji, eksportów lub stabilności startu.

P2 może przejść dalej wyłącznie jako jawnie opisane ograniczenie bez wpływu na funkcję krytyczną.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M7

- testowanie innego artefaktu niż przeznaczony do publikacji;
- poprawka po rozpoczęciu smoke bez nowego ZIP-a i hasha;
- niepełny test obu baz, obu języków albo aktywnej telemetrii;
- zaakceptowanie błędu P0/P1 jako known issue.

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
