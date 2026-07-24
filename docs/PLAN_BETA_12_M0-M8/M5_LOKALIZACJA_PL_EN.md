# M5 — Lokalizacja PL/EN

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Formalny **UI freeze** po M4.  
**Kryterium wyjścia:** Kompletne PL/EN, zielone regresje obu języków i niezmienione kontrakty maszynowe.  
**Następny etap:** M6

> Ten dokument jest samodzielnym wydzieleniem etapu M5 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** przygotować pełnoprawne, spójne wersje `pl-PL` i `en-GB`.

### Etap M5.1 — inwentaryzacja

- [ ] Utworzyć `docs/LOCALIZATION_STRING_INVENTORY.md`.
- [ ] Sklasyfikować każdy tekst jako użytkowy, techniczny, diagnostyczny, domenowy lub własny.
- [ ] Zidentyfikować statusy i enumy wymagające presenterów.
- [ ] Zidentyfikować ekrany narażone na przepełnienie.
- [ ] Zatwierdzić słownik domenowy PL/EN.

### Etap M5.2 — fundament

- [ ] Dodać zasoby `.resx` dla Desktopu i raportów.
- [ ] Dodać trwałe ustawienie kultury.
- [ ] Dodać wybór języka w Ustawieniach.
- [ ] Dodać komunikat o zastosowaniu po restarcie.
- [ ] Dodać bezpieczny fallback.
- [ ] Ustawić kulturę przed utworzeniem okien WPF.

### Etap M5.3 — Desktop i Planer

- [ ] Zlokalizować nawigację i wspólne elementy.
- [ ] Zlokalizować Dashboard i tachograf.
- [ ] Zlokalizować Historię, luki i wpis manualny.
- [ ] Zlokalizować Rekomensaty.
- [ ] Zlokalizować Raporty, Kierowców i Ustawienia.
- [ ] Zlokalizować Planer i wszystkie jego statusy, segmenty oraz ostrzeżenia.
- [ ] Zlokalizować dialogi i nakładki.
- [ ] Zlokalizować nazwy krajów bez zmiany ISO.

### Etap M5.4 — PDF i dokumentacja

- [ ] Zlokalizować raport PDF.
- [ ] Potwierdzić identyczność danych PDF PL i EN.
- [ ] Przygotować instrukcję instalacji PL i EN.
- [ ] Przygotować podstawową instrukcję użytkową PL i EN.

### Gate M5

- kompletne zasoby PL i EN;
- zgodne placeholdery;
- brak pustych tłumaczeń;
- brak niezamierzonego mieszanego języka;
- pełna regresja UI PL zielona;
- pełna regresja UI EN zielona;
- PDF PL i EN poprawne wizualnie;
- RuleEngine i Planer zwracają identyczne dane niezależnie od języka;
- JSON, CSV techniczny, `.tacho`, SQLite i protokół v3 pozostają niezmienione.

---

## Kontrakt lokalizacji

Języki MVP: `pl-PL` i `en-GB`. Lokalizacja obejmuje Desktop, Planer, wirtualny tachograf, nakładki, dialogi i kreatory, aktywności, statusy, ostrzeżenia, rekompensaty, luki, PDF oraz podstawową dokumentację użytkownika.

Założenia:

- zasoby `.resx` dla UI i raportów;
- wybór języka w Ustawieniach;
- zastosowanie po restarcie;
- bezpieczny fallback, rekomendowany `en-GB`;
- PDF w języku aktywnego UI;
- nazwy krajów lokalizowane bez zmiany stabilnych kodów ISO;
- brak wpływu kultury na JSON, techniczny CSV, `.tacho`, SQLite i protokół v3.

Nie lokalizować enumów, klas, metod, pól kontraktów maszynowych, identyfikatorów, kodów naruszeń i błędów ani technicznych nazw logowania.

## Testy obowiązkowe

- zgodność zestawu kluczy PL i EN;
- brak pustych wartości i niezgodnych placeholderów;
- pełna prezentacja wszystkich aktywności, statusów luk, rekompensat i Planera;
- bezpieczny fallback dla nieobsługiwanej lub uszkodzonej kultury;
- restart zachowujący wybór języka;
- pełna checklista UI w `pl-PL` i `en-GB`;
- wizualna kontrola PDF PL i EN;
- identyczność danych domenowych i eksportowych między językami.

## Poza zakresem M5

- języki inne niż PL/EN;
- dynamiczne przełączanie całego WPF bez restartu;
- osobny język raportu niezależny od UI;
- lokalizacja logów technicznych i kontraktów maszynowych.

## Zasady obowiązujące na tym etapie

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. RuleEngine nie jest zastępowany logiką w UI ani w Planerze.
3. Każdy potwierdzony błąd otrzymuje dokładny test regresyjny przed poprawką.
4. Każda zmiana XAML wymaga pełnej checklisty regresji UI.
5. Kontrakty maszynowe używają `InvariantCulture` i nie zależą od języka UI.
6. Nie rozszerzać zakresu „przy okazji”.
7. Po UI freeze dopuszczalne są tylko poprawki błędów, lokalizacji i przepełnień.
8. Zmiana kodu lub zawartości paczki po zbudowaniu RC unieważnia wykonany smoke.

## Najważniejsze ryzyka M5

- częściowa lokalizacja i mieszany język;
- angielskie teksty nie mieszczą się w zamrożonym UI;
- sklejanie zdań z fragmentów zasobów;
- wpływ kultury na serializację techniczną;
- rozjazd danych PDF PL/EN.

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
