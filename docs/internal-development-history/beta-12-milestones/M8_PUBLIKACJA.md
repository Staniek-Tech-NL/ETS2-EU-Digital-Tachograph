# M8 — Publikacja

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status:** **GO**
**Data rozpoczęcia:** 5 sierpnia 2026
**Data zakończenia:** 5 sierpnia 2026
**Kryterium wejścia:** Decyzja **GO** po końcowym smoke teście M7.  
**Kryterium wyjścia:** Opublikowany dokładnie ten artefakt, który przeszedł smoke, wraz z dokumentacją i checksumą.  
**Następny etap:** Zamknięcie pierwszej publikacji i przejście do backlogu popublikacyjnego

> Ten dokument jest samodzielnym wydzieleniem etapu M8 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** opublikować zatwierdzony artefakt bez rozjazdu względem smoke testu.

### Zadania

- [x] Zamrozić commit źródłowy.
- [x] Potwierdzić zgodność SHA-256 z artefaktem smoke.
- [x] Ustalić publiczny numer/tag wydania bazujący na zatwierdzonym kodzie beta.12.
- [x] Nie dodawać funkcji między GO a publikacją.
- [x] Opublikować paczkę aplikacji i pluginu.
- [x] Opublikować checksumę SHA-256.
- [x] Opublikować instrukcję instalacji PL i EN.
- [x] Opublikować known issues.
- [x] Opublikować release notes.
- [x] Opublikować informację, że aplikacja jest symulatorem, a nie certyfikowanym tachografem.
- [x] Opisać sposób zgłaszania błędów i generowania raportu diagnostycznego.
- [x] Zachować `0.1.0-beta.11.1` jako historyczną bazę.
- [x] Oznaczyć `0.1.0-beta.12` jako ostatnią betę.

### Stan rozpoczęcia M8 — 5 sierpnia 2026

- **Commit źródłowy artefaktu:**
  `ffe6f7fad2c4fccfad8fc12f1a93675cc5d13c78` — zamrożony RC z GO M6/M7.
- **Commit dokumentujący GO M7:**
  `55ea567918e2223e88b6d2c5f18a6cd0c3b66a64`.
- **Artefakt:** `ETS2Tachograph-0.1.0-beta.12-win-x64.zip`, 67 029 279 bajtów,
  450 pozycji po kontrolnym odczycie.
- **SHA-256:**
  `A2B8F949E100F8683225B7A0D5A76E5C7E3434AD95AEC9596006C4A5E41F5E78`
  — ponownie potwierdzone przy wejściu do M8.
- **Zawartość obowiązkowa:** aplikacja, plugin v3, instrukcje i przewodniki PL/EN
  oraz `THIRD_PARTY_NOTICES.md` są obecne w zatwierdzonym ZIP-ie.
- **Plik checksumy:** gotowy obok ZIP-a w katalogu `output/releases`.
- **Kod po GO M7:** brak zmian funkcjonalnych; rozpoczęcie M8 zmienia wyłącznie
  dokumentację procesu publikacji.

### Decyzje właściciela — przyjęte 5 sierpnia 2026

- **Miejsce publikacji:** GitHub Releases w publicznym repozytorium
  `Staniek-Tech-NL/ETS2-EU-Digital-Tachograph`.
- **Tag:** `v0.1.0-beta.12`, wskazujący bezpośrednio commit źródłowy artefaktu
  `ffe6f7fad2c4fccfad8fc12f1a93675cc5d13c78`.
- **Typ wydania:** pre-release.
- **Licencja kodu własnego:** MIT; komponenty zewnętrzne zachowują licencje
  opisane w `docs/THIRD_PARTY_NOTICES.md`.
- **Kanał błędów:** GitHub Issues z formularzem `Bug report`.
- **Pytania i pomysły:** GitHub Discussions.
- **Podatności:** GitHub Private vulnerability reporting.
- **Model wsparcia:** best effort, bez SLA i bez wsparcia przez prywatne
  wiadomości; P0/P1 mają pierwszeństwo przed nowymi funkcjami.
- **Dystrybucja:** self-contained ZIP `win-x64`; instalator, podpis kodu
  i auto-update pozostają poza zakresem beta.12.

Pakiet decyzji jest zamknięty i został wykonany.

### Wynik publikacji — GO 5 sierpnia 2026

- **Repozytorium:**
  <https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph> — publiczne,
  domyślna gałąź `main`.
- **Wydanie:**
  <https://github.com/Staniek-Tech-NL/ETS2-EU-Digital-Tachograph/releases/tag/v0.1.0-beta.12>
  — publiczne pre-release.
- **Tag:** opisany tag `v0.1.0-beta.12`; zdalna dereferencja `^{}` wskazuje
  dokładnie `ffe6f7fad2c4fccfad8fc12f1a93675cc5d13c78`.
- **Assety:** dokładny ZIP z GO M7 oraz plik `.sha256`; GitHub potwierdza rozmiar
  ZIP-a `67 029 279` bajtów i digest
  `sha256:a2b8f949e100f8683225b7a0d5a76e5c7e3434ad95aec9596006c4a5e41f5e78`.
- **Kontrola po przesłaniu:** oba assety pobrano z draft release; ponownie
  pobrany ZIP ma SHA-256
  `A2B8F949E100F8683225B7A0D5A76E5C7E3434AD95AEC9596006C4A5E41F5E78`.
- **Kanały projektu:** Issues i Discussions są włączone; Private vulnerability
  reporting aktywne; formularz `Bug report`, MIT, `SUPPORT.md` i `SECURITY.md`
  są opublikowane na `main`.
- **Integralność zakresu:** między GO M7 a publikacją nie zmieniono kodu,
  pluginu ani zatwierdzonego ZIP-a. Zmiany obejmowały wyłącznie dokumentację,
  licencję i konfigurację społecznościową GitHub.
- **Otwarte P0/P1:** 0/0.

**Decyzja M8: GO.** Kryterium wyjścia zostało spełnione; łańcuch M0–M8 jest
zamknięty, a backlog popublikacyjny został odblokowany.

### Gate publikacji

- artefakt publikowany jest dokładnie tym, który przeszedł smoke;
- dokumentacja PL/EN jest dostępna;
- checksumy są opublikowane;
- brak nieudokumentowanych zmian;
- model licencji, repozytorium i wsparcia jest jawnie określony;
- kanał zgłoszeń błędów jest gotowy.

---

## Decyzje zamknięte przed publikacją

- [x] publiczny numer i tag bazujący na zatwierdzonym kodzie beta.12;
- [x] miejsce publikacji paczki;
- [x] widoczność repozytorium;
- [x] licencja;
- [x] model wsparcia i kanał zgłoszeń;
- [x] dystrybucja jako self-contained ZIP `win-x64`;
- [x] instalator, podpis i auto-update poza zakresem.

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

- **Data rozpoczęcia:** 2026-08-05
- **Data zakończenia:** 2026-08-05
- **Wynik:** **GO**
- **Commit / punkt przywracania:** RC `ffe6f7fad2c4fccfad8fc12f1a93675cc5d13c78`;
  GO M7 `55ea567918e2223e88b6d2c5f18a6cd0c3b66a64`
- **Build Release:** 0 błędów / 0 ostrzeżeń; nie przebudowywano po GO M7
- **Testy automatyczne:** 570/570 PASS z zamrożonego RC
- **Testy manualne / dowody:** M7 GO; ponowna zgodność SHA-256 i audyt
  450 pozycji ZIP-a; pobranie assetów z GitHub i zgodność SHA-256 PASS
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Uwagi do następnego etapu:** łańcuch M0–M8 zamknięty; przejść do backlogu
  popublikacyjnego bez zmiany historycznego artefaktu beta.12

---

**Źródło nadrzędne:** `PLAN_WYDANIA_BETA_12_I_PUBLIKACJI.md`  
**Dokumenty powiązane:** `PROJECT_HANDOFF.md`, `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `BETA_TEST_PLAN.md`, `JOURNEY_PLANNER_MVP_PLAN.md`, `MINI_PROJEKT_LOKALIZACJA_PL_EN.md`, `RAPORT_PRAC_UI_2026-07-23.md`, `WEEKLY_REST_COMPENSATION_DOMAIN_SPEC.md`, `WEEKLY_REST_COMPENSATION_TEST_MATRIX.md`.
