# M5 — Lokalizacja PL/EN

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 24 lipca 2026  
**Status bieżący:** **GO — M5 ZAMKNIĘTE**

**Data rozpoczęcia:** 27 lipca 2026
**Data zakończenia:** 27 lipca 2026

**Kryterium wejścia:** Formalny **UI freeze** po M4.  
**Kryterium wyjścia:** Kompletne PL/EN, zielone regresje obu języków i niezmienione kontrakty maszynowe.  
**Następny etap:** M6

> Ten dokument jest samodzielnym wydzieleniem etapu M5 z planu wydania beta.12. Nie zmienia zakresu ani gate’ów planu nadrzędnego.

**Cel:** przygotować pełnoprawne, spójne wersje `pl-PL` i `en-GB`.

## Otwarcie etapu

- **Wejście:** spełnione — M4 zamknięte wynikiem GO, UI freeze obowiązuje.
- **Punkt wejściowy:** `2d8a760` — commit zamykający M4.
- **Gałąź:** `codex/m5-localization-pl-en`.
- **Pierwszy artefakt:** `docs/internal-development-history/LOCALIZATION_STRING_INVENTORY.md`.
- **Granica:** lokalizowana jest wyłącznie prezentacja; JSON, techniczny CSV,
  `.tacho`, SQLite, protokół v3, identyfikatory i kody pozostają niezmienne.
- **Kolejność:** kompletna inwentaryzacja i słownik przed wdrożeniem `.resx`.

### Etap M5.1 — inwentaryzacja

- [x] Utworzyć `docs/internal-development-history/LOCALIZATION_STRING_INVENTORY.md`.
- [x] Sklasyfikować każdy tekst jako użytkowy, techniczny, diagnostyczny, domenowy lub własny.
- [x] Zidentyfikować statusy i enumy wymagające presenterów.
- [x] Zidentyfikować ekrany narażone na przepełnienie.
- [x] Zatwierdzić słownik domenowy PL/EN.

**Postęp paczek:**

- [x] Paczki 1–13: **GO**, 952 unikalne nazwy, 31 dozwolonych par
  powtórzonych wartości i 0 pozycji otwartych.
- [x] Rejestr presenterów: 21 rozstrzygniętych, 9 świadomie wykluczonych,
  0 pozostałych.

### Etap M5.2 — fundament

- [x] Dodać zasoby `.resx` dla Desktopu i raportów.
- [x] Dodać trwałe ustawienie kultury.
- [x] Dodać wybór języka w Ustawieniach.
- [x] Dodać komunikat o zastosowaniu po restarcie.
- [x] Dodać bezpieczny fallback.
- [x] Ustawić kulturę przed utworzeniem okien WPF.

**Stan wykonawczy:** implementacja kompletna, **DO WERYFIKACJI** przed GO.

- `UiStrings`: 626 wpisów w `pl-PL` i `en-GB`;
- `ReportStrings`: 99 wpisów w `pl-PL` i `en-GB`, w tym 22 klucze mostu;
- nazwy krajów: dwa magazyny po 249 wpisów przy niezmienionym katalogu ISO;
- preferencja `%LocalAppData%\ETS2Tachograph\ui-culture.json`, schemat 1,
  zapis przez plik tymczasowy i atomową zamianę;
- brak pliku zachowuje `pl-PL`; wartość uszkodzona albo nieobsługiwana
  uruchamia fallback `en-GB` i zapis diagnostyczny;
- kultura bieżąca, domyślna kultura wątków i język bindingów WPF są ustawiane
  przed utworzeniem `MainViewModel`, `MainWindow` i `PdfReportExporter`;
- jedyna nowa kontrolka względem UI freeze to wybór języka w Ustawieniach;
- build rozwiązania: 0 błędów, 0 ostrzeżeń;
- pełna regresja automatyczna: 557/557 testów zielonych.

**Pozostała weryfikacja manualna M5.2:**

- pierwsze uruchomienie bez pliku preferencji pozostawia fundament i sekcję
  Ustawień po polsku;
- zapis `en-GB`, restart i ponowne uruchomienie odtwarzają wybór w fundamencie
  oraz sekcji Ustawień;
- uszkodzony lub nieobsługiwany wpis uruchamia bezpieczny fallback EN
  w fundamencie i sekcji Ustawień;
- kontrolka języka, komunikat restartu i liczby w Ustawieniach są czytelne
  w obu kulturach.

M5.2 nie lokalizuje jeszcze pozostałych ekranów. Mieszany interfejs po wyborze
`en-GB` — angielskie Ustawienia przy polskich widokach domenowych — jest
oczekiwanym stanem przejściowym i znika dopiero w M5.3. Nie jest dowodem
nieodtworzenia preferencji po restarcie.

**Checkpoint wydajnościowy M5.2-P:** znalezisko zostało wydzielone do
[osobnej bramki](M5_2_CHECKPOINT_WYDAJNOSCI_STARTU.md). Na świeżej kopii
tej samej bazy praca repozytorium objęta startem spadła z około 49 sekund
do 5,36 sekundy. Zasoby lokalizacji nie były źródłem opóźnienia. Checkpoint
ma GO warunkowe: archiwizacja nadal rośnie liniowo z całą zachowaną historią,
dlatego osobisty pomiar tworzy bazę odniesienia, a kontrola jest powtarzana
przed M6.

### Etap M5.3 — Desktop i Planer

- [x] Zlokalizować nawigację i wspólne elementy.
- [x] Zlokalizować Dashboard i tachograf.
- [x] Zlokalizować Historię, luki i wpis manualny.
- [x] Zlokalizować Rekomensaty.
- [x] Zlokalizować Raporty, Kierowców i Ustawienia.
- [x] Zlokalizować Planer i wszystkie jego statusy, segmenty oraz ostrzeżenia.
- [x] Zlokalizować dialogi i nakładki.
- [x] Zlokalizować nazwy krajów bez zmiany ISO.

**Postęp wykonawczy M5.3:**

- paczka 1 (`UI-01`): **GO**, zamknięta bez pozycji otwartych; powłoka,
  siedem pozycji nawigacji, wspólne akcje
  i nagłówki oraz status telemetrii podpięte do wiążących kluczy;
- komunikat błędu telemetrii nie publikuje `exception.Message` w UI;
- surowe literały objęte paczką 1 usunięte z XAML i stanu początkowego VM;
- build WPF oraz 128/128 testów Desktopu: PASS;
- osobista kontrola wizualna PL/EN, w tym szerokość `JOURNEY PLANNER`: PASS.
- paczki 2–12 (`UI-02`–`UI-10` oraz `X-01`): **GO**, zamknięte bez
  pozycji otwartych; wszystkie powierzchnie Desktopu, Planer, tachograf LCD,
  dialogi i nakładki korzystają z wiążących zasobów;
- wybór `en-GB` obowiązuje procesowo także na wątkach telemetrycznych; test
  regresyjny chroni menu LCD przed powrotem do `pl-PL`;
- pełna regresja rozwiązania 558/558 oraz końcowa regresja Desktop 129/129:
  PASS;
- osobisty smoke PL/EN całego M5.3: **GO**.

### Etap M5.4 — PDF i dokumentacja

- [x] Zlokalizować raport PDF.
- [x] Potwierdzić identyczność danych PDF PL i EN.
- [x] Przygotować instrukcję instalacji PL i EN.
- [x] Przygotować podstawową instrukcję użytkową PL i EN.

**Postęp wykonawczy M5.4:**

- metadane, sekcje, tabele, statusy, presentery, puste stany i stopka PDF
  korzystają z `ReportStrings` oraz kultury przechwyconej na początku eksportu;
- formaty techniczne czasu pozostają niezmienne, a eksport nie modyfikuje
  danych źródłowego `ReportDto`;
- PDF ponownie używa pełnych statusów `ReportCompensationStatus_*`; zgodnie
  z decyzją `PDF-01` kolumna statusu wzrosła z 95 pt do 125 pt, bez zmiany
  kolejności ani znaczenia danych;
- próbki PL i EN zbudowane z identycznego raportu mają po dwie strony;
  render obu języków jest czytelny, pełne statusy mieszczą się w tabeli,
  a nagłówki checkpointów nie nachodzą na siebie;
- kontrola tekstu PDF potwierdziła obecność tych samych identyfikatorów,
  wartości czasu i danych technicznych w PL oraz EN;
- testy Reports 11/11 oraz pełna regresja rozwiązania 561/561: PASS;
- build Release całego rozwiązania: 0 błędów, 0 ostrzeżeń;
- dokumentacja gotowa:
  `docs/INSTALLATION_PL.md`, `docs/INSTALLATION_EN.md`,
  `docs/USER_GUIDE_PL.md` i `docs/USER_GUIDE_EN.md`.

**Stan decyzji:** **GO — M5.4**, zatwierdzone przez właściciela 27 lipca 2026
po osobistym smoke eksportu i oględzinach PDF PL/EN. Etap jest zamknięty bez
pozycji otwartych.

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

**Wynik gate M5:** **GO**. M5.1–M5.4 są zamknięte, a M6 jest odblokowany.
Warunek checkpointu wydajnościowego M5.2-P pozostaje obowiązującym pomiarem
wejściowym przed zamrożeniem RC w M6.

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
