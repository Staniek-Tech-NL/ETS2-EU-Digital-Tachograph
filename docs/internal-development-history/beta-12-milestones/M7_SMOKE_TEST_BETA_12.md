# M7 — Końcowy smoke test beta.12

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status:** **GO**
**Data rozpoczęcia:** 5 sierpnia 2026
**Data zakończenia:** 5 sierpnia 2026
**Kryterium wejścia:** Istnieje niezmienny ZIP beta.12 z numerem, commitem i SHA-256.  
**Kryterium wyjścia:** Formalna decyzja **GO / FIX / HOLD** dla dokładnego artefaktu beta.12.  
**Następny etap:** M8

> Ten dokument jest samodzielnym wydzieleniem etapu M7 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** potwierdzić dokładny artefakt przeznaczony do publikacji.

### Przygotowanie

- [x] Rozpakować ZIP do nowego katalogu.
- [x] Nie uruchamiać z IDE ani bezpośrednio z ZIP-a.
- [x] Zainstalować DLL pluginu z tej samej paczki.
- [x] Całkowicie uruchomić ETS2 ponownie.
- [x] Zachować kopię istniejącej bazy.
- [x] Zapisać hash testowanego ZIP-a.

### Smoke — istniejąca baza

- [x] Start aplikacji bez błędów.
- [x] Odtworzenie profili, kart, ustawień i historii.
- [x] Poprawne rekompensaty i decyzje alokacji.
- [x] Oba sloty działają.
- [x] Aktywna telemetria v3.
- [x] Ruch automatycznie ustawia Jazdę.
- [x] Blokady zależne od ruchu działają.
- [x] Tryby OUT i Prom działają.
- [x] Załadunek/rozładunek nie tworzy fałszywej luki.
- [x] Wpis manualny wariant B działa.
- [x] Kraje i `ODP. TYG.` działają.
- [x] Granica pauzy 44/45 jest poprawna.
- [x] Planer tworzy legalny harmonogram i poprawnie unieważnia stale snapshot.
- [x] Nakładki S1/S2 są zgodne z Dashboardem.
- [x] PDF, CSV, JSON i `.tacho` działają.
- [x] Raport diagnostyczny ZIP działa.
- [x] Zmiana języka PL/EN utrzymuje się po restarcie.
- [x] Aplikacja zamyka się czysto.
- [x] Ponowny start odtwarza identyczne dane.
- [x] Logi nie zawierają nowych wyjątków ani błędów bindingów.

### Smoke — czysta baza

- [x] Pierwszy start tworzy bazę i ustawienia.
- [x] Utworzenie profilu i karty działa.
- [x] Włożenie/wyjęcie karty działa.
- [x] Podstawowy przepływ telemetrii działa.
- [x] Planer działa z minimalnym poprawnym stanem.
- [x] Wybór języka działa po restarcie.
- [x] Raport i eksport podstawowy działają.

### Reguła unieważnienia smoke

Każda zmiana kodu, zasobów, konfiguracji, pluginu, paczki lub dokumentów wchodzących do ZIP-a po rozpoczęciu testu:

```text
unieważnia dotychczasowy smoke
→ wymaga nowego artefaktu
→ wymaga nowego SHA-256
→ wymaga ponownego testu odpowiedniego zakresu
```

### Decyzja M7

**GO — 5 sierpnia 2026.** Użytkownik potwierdził, że wszystkie pozycje
końcowego smoke testu są zielone. Brak otwartych błędów P0/P1. M8 jest
odblokowany. Decyzja dotyczy wyłącznie niezmiennego artefaktu
`ETS2Tachograph-0.1.0-beta.12-win-x64.zip` o SHA-256
`A2B8F949E100F8683225B7A0D5A76E5C7E3434AD95AEC9596006C4A5E41F5E78`.

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

- **Data rozpoczęcia:** 2026-08-05
- **Data zakończenia:** 2026-08-05
- **Wynik:** **GO**
- **Commit / punkt przywracania:** `ffe6f7fad2c4fccfad8fc12f1a93675cc5d13c78`
  — źródło zamrożonego RC
- **Build Release:** 0 błędów / 0 ostrzeżeń; `FileVersion 0.1.12.0`
- **Testy automatyczne:** 570/570 PASS z bramki M6
- **Testy manualne / dowody:** użytkownik potwierdził pełną checklistę M7 jako
  zieloną na rozpakowanym ZIP-ie, dla istniejącej i czystej bazy
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** M8 odblokowany; opublikować dokładnie ZIP
  zatwierdzony w M7 wraz z checksumą i dokumentacją PL/EN

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
