# MINI-PROJEKT — LOKALIZACJA PL/EN

**Projekt nadrzędny:** ETS2 EU Digital Tachograph  
**Nazwa mini-projektu:** Lokalizacja interfejsu i raportów PL/EN  
**Stan dokumentu:** realizacja rozpoczęta — M5.1

**Data przygotowania:** 23 lipca 2026  
**Wersja bazowa:** `0.1.0-beta.11.1`  
**Warunek rozpoczęcia:** spełniony — formalny werdykt **GO** po smoke teście beta.11.1
**Warunek zakończenia:** gotowość do pierwszej szerokiej publikacji  
**Języki MVP:** `pl-PL`, `en-GB`

## Aktualizacja stanu lokalnego

Mini-projekt rozpoczęto 2026-07-27 po formalnym GO M4. Stan wejściowy kodu
zawierał już pierwszy wersjonowany zasób językowy:

- `Data/Countries.iso3166-1.json` przechowuje stabilne dane ISO i kody
  tachografowe;
- `Resources/CountryNames.pl.json` przechowuje polskie nazwy 249 krajów;
- historia zapisuje ISO, nie przetłumaczoną nazwę.

Dedykowana gałąź to `codex/m5-localization-pl-en`. Pierwszym etapem pozostaje
inwentaryzacja; fundament `.resx` nie może jej wyprzedzić.

Przy wdrażaniu `.resx` nie wolno duplikować ani zastępować stabilnych kodów
ISO. Nazwy krajów należy włączyć do wspólnego mechanizmu lokalizacji albo
utrzymać jako wersjonowane katalogi językowe o tym samym kontrakcie.

---

## 1. Status i decyzja projektowa

Mini-projekt lokalizacji zostaje przyjęty jako osobny etap pomiędzy:

```text
GO dla beta.11.1
→ lokalizacja PL/EN
→ regresja obu języków
→ pierwsza szeroka publikacja
```

Lokalizacja:

- nie wchodzi do zamrożonego artefaktu beta.11.1;
- nie zmienia wyniku zakończonego smoke testu beta.11.1;
- może być przygotowywana dopiero po formalnym GO;
- musi być realizowana na osobnej gałęzi;
- nie może zmieniać reguł domenowych, danych, protokołu telemetrii ani kontraktów eksportowych;
- obejmuje wyłącznie warstwę prezentacji oraz treści przeznaczone dla użytkownika.

Dodatkowe języki poza polskim i angielskim nie należą do tego mini-projektu. Będą rozważane dopiero po szerokiej publikacji, na podstawie rzeczywistego zainteresowania użytkowników.

---

## 2. Cel mini-projektu

Celem jest przygotowanie aplikacji do używania przez użytkowników polsko- i anglojęzycznych bez utraty:

- czytelności interfejsu;
- realistycznego charakteru tachografu;
- poprawności danych;
- zgodności UI z RuleEngine;
- stabilności raportów;
- identycznych wyników domenowych niezależnie od języka.

Wersja angielska ma być pełnoprawna, a nie „częściowo przetłumaczona”. Użytkownik nie powinien natrafiać na losową mieszankę polskich i angielskich komunikatów.

---

## 3. Zakres MVP lokalizacji

### 3.1 Elementy objęte lokalizacją

#### Główne UI

- pasek nawigacji;
- Dashboard;
- Historia;
- Rekomendacje i ostrzeżenia;
- Rekomensaty;
- Raporty;
- Kierowcy;
- Ustawienia;
- nagłówki sekcji;
- opisy pól;
- przyciski;
- etykiety statusów;
- komunikaty błędów i ostrzeżeń;
- puste stany i podpowiedzi;
- teksty naruszeń widoczne dla użytkownika.

#### Wirtualne urządzenie tachografu

- menu urządzenia;
- nazwy aktywności;
- komunikaty wkładania i wyjmowania karty;
- tryby OUT i prom;
- komunikaty slotów;
- teksty dotyczące załogi;
- kraj rozpoczęcia i zakończenia;
- cele pauzy i odpoczynku;
- komunikaty blokad.

#### Nakładki S1/S2

- etykiety liczników;
- statusy;
- opisy trybu;
- tekst rekompensaty;
- komunikaty braku danych;
- skróty i jednostki tylko wtedy, gdy są tekstem użytkowym.

#### Dialogi i kreatory

- wkładanie i wyjmowanie karty;
- kreator wpisu manualnego;
- rozliczanie luk;
- decyzja alokacji odpoczynku i rekompensaty;
- potwierdzenia;
- komunikaty walidacyjne;
- raport diagnostyczny;
- import i eksport.

#### Raport PDF

- tytuły;
- nagłówki;
- nazwy aktywności;
- statusy zobowiązań;
- opisy luk;
- podsumowania;
- ostrzeżenia;
- legenda;
- podpisy tabel;
- tekst dotyczący kompletności materiału dowodowego;
- informacja, że aplikacja jest symulatorem, a nie certyfikowanym tachografem.

#### Podstawowa dokumentacja dla użytkownika

- instrukcja instalacji;
- instrukcja pluginu;
- pierwsze uruchomienie;
- obsługa kart;
- podstawowe skróty;
- opis ograniczeń;
- zgłaszanie błędów.

---

### 3.2 Elementy poza zakresem

Nie lokalizować ani nie zmieniać:

- nazw enumów;
- nazw klas i metod;
- nazw pól JSON;
- nazw pól technicznego CSV;
- kontraktu `.tacho`;
- identyfikatorów zobowiązań;
- `ObligationId`;
- `RestBlockId`;
- `CandidateId`;
- wartości technicznych zapisanych w SQLite;
- nazw tabel i kolumn bazy;
- protokołu telemetrii v3;
- nazw map pamięci współdzielonej;
- kodów naruszeń;
- kodów błędów;
- nazw zdarzeń logowania;
- danych diagnostycznych przeznaczonych dla dewelopera.

Kontrakty maszynowe pozostają niezmienne. Język wpływa wyłącznie na prezentację.

---

## 4. Założenia architektoniczne

### 4.1 Technologia

Podstawą lokalizacji interfejsu i raportów będą zasoby `.resx`. Katalog krajów
jest istniejącym wyjątkiem danych referencyjnych: stabilne rekordy ISO pozostają
w JSON, a lokalizowane nazwy mogą pozostać w osobnych plikach językowych albo
zostać podłączone do `.resx` bez zmiany zapisywanych kodów.

Proponowana struktura:

```text
src/ETS2Tachograph.Desktop/
└── Resources/
    ├── UiStrings.resx
    ├── UiStrings.pl-PL.resx
    └── UiStrings.en-GB.resx

src/ETS2Tachograph.Reports/
└── Resources/
    ├── ReportStrings.resx
    ├── ReportStrings.pl-PL.resx
    └── ReportStrings.en-GB.resx
```

Bazowy `.resx` może być:

- neutralny i angielski;
- albo neutralny i polski.

Decyzję trzeba podjąć przed rozpoczęciem implementacji. Rekomendacja:

> Bazowe zasoby neutralne w języku angielskim, a `pl-PL` jako jawny zestaw lokalizacyjny.

Powód:

- angielski będzie językiem szerokiej publikacji;
- zmniejszy ryzyko nieprzetłumaczonych polskich tekstów;
- kolejne języki będą dziedziczyły stabilny angielski fallback.

---

### 4.2 Źródło języka

Ustawienie języka ma być przechowywane jako trwałe ustawienie aplikacji.

Proponowany kontrakt:

```csharp
public enum ApplicationLanguage
{
    Polish,
    English
}
```

albo, preferowane:

```csharp
public sealed record LanguagePreference(
    string CultureName);
```

Dopuszczalne wartości MVP:

```text
pl-PL
en-GB
```

Nie zapisywać nazw wyświetlanych jako logiki domenowej. UI może prezentować:

```text
Polski
English
```

---

### 4.3 Zmiana języka

W MVP zmiana języka obowiązuje po ponownym uruchomieniu aplikacji.

Przepływ:

1. użytkownik wybiera język w Ustawieniach;
2. ustawienie zostaje zapisane;
3. UI pokazuje komunikat o wymaganym restarcie;
4. po ponownym uruchomieniu aplikacja ustawia właściwą kulturę przed utworzeniem okien;
5. wszystkie ekrany, nakładki i raporty używają wybranej kultury.

Nie implementować dynamicznego przełączania całego WPF w locie w pierwszej wersji. Zwiększyłoby to złożoność, ryzyko wycieków bindingów i liczbę stanów do testowania.

---

### 4.4 Język raportu PDF

Domyślnie PDF używa języka aktywnego UI.

Możliwe rozszerzenie późniejsze:

```text
Język raportu:
- Jak aplikacja
- Polski
- English
```

Nie należy dodawać tego wyboru w MVP, chyba że jego koszt będzie minimalny i nie wpłynie na gate lokalizacji.

---

### 4.5 Formatowanie danych

Lokalizacja nie może zmieniać znaczenia danych.

#### Czas trwania

Pozostaje:

```text
HH:MM
```

Bez formatu dziesiętnego.

#### Czas gry

Format musi zachować aktualną regułę:

```text
displayedDay = floor(GameMinute / 1440) + 1
```

Przykłady:

```text
pl-PL: Dzień 141, 15:30
en-GB: Day 141, 15:30
```

#### Liczby

Nie zmieniać wartości ani zaokrągleń. Dopuszczalne jest lokalne formatowanie separatorów tylko w tekstach prezentacyjnych, jeśli nie narusza ono testów i raportów.

#### Nazwy własne

Nie tłumaczyć:

- nazw kierowców;
- nazw profili;
- identyfikatorów;
- nazw plików użytkownika;
- nazw VTC;
- nazw miast lub krajów pochodzących z danych użytkownika.

---

## 5. Inwentaryzacja tekstów

Przed implementacją należy utworzyć listę wszystkich tekstów wpisanych na stałe.

### 5.1 Obszary do przeszukania

- `MainWindow.xaml`;
- `OverlayWindow.xaml`;
- `App.xaml`;
- wszystkie pozostałe pliki XAML;
- `MainViewModel.cs`;
- `OverlayViewModel.cs`;
- ViewModele dialogów;
- serwisy Application generujące komunikaty;
- `PdfReportExporter.cs`;
- `ReportPresentationBuilder.cs`;
- kreatory;
- komunikaty wyjątków widoczne dla użytkownika;
- formatery statusów;
- menu tachografu;
- teksty testowe asertywne, które odzwierciedlają UI.

### 5.2 Klasyfikacja tekstów

Każdy tekst należy oznaczyć jako:

1. **tekst użytkowy do lokalizacji**;
2. **kod techniczny bez lokalizacji**;
3. **tekst diagnostyczny dla logu**;
4. **wartość domenowa wymagająca lokalnego prezentera**;
5. **nazwa własna lub dane użytkownika**.

Nie tłumaczyć bezpośrednio `ToString()` enumów. Dodać osobne prezentery lub mapery lokalizacyjne.

---

## 6. Konwencja kluczy zasobów

Klucze muszą być stabilne i semantyczne.

Dobre przykłady:

```text
Navigation_Dashboard
Navigation_History
Navigation_Compensations
Dashboard_ContinuousDriving
Dashboard_TimeUntilBreak
Activity_Driving
Activity_OtherWork
Activity_Availability
Activity_BreakOrRest
Compensation_Status_OpenOnTime
Compensation_Status_Overdue
Dialog_RestAllocation_Title
Dialog_RestAllocation_Confirm
Common_Save
Common_Cancel
Common_NotAvailable
```

Złe przykłady:

```text
Text1
ButtonText2
PolishDashboardTitle
Label_123
```

Klucz nie może zawierać języka ani odnosić się do bieżącego położenia w XAML.

---

## 7. Etapy realizacji

## Etap 0 — gate wejściowy

**Status początkowy:** gate wydaniowy beta.11.1 spełniony 23 lipca 2026;
oczekuje na osobną decyzję o rozpoczęciu mini-projektu.

Warunki:

- [x] smoke test beta.11.1 zaliczony;
- [x] decyzja GO zapisana;
- [x] repozytorium czyste poza prywatnym, nietkniętym katalogiem `.claude/`;
- [x] artefakt beta.11.1 zamrożony;
- [x] późniejsze poprawki UI rozdzielone od artefaktu beta.11.1 i lokalizacji;
- [x] utworzona osobna gałąź mini-projektu:
      `codex/m5-localization-pl-en`.

Utworzona gałąź:

```text
codex/m5-localization-pl-en
```

Gate:

> Formalny GO dla beta.11.1 został zapisany. Lokalizacja rozpoczyna się dopiero
> po osobnej decyzji właściciela projektu i utworzeniu dedykowanej gałęzi.

---

## Etap 1 — audyt tekstów i kontraktów

Zadania:

- [ ] zinwentaryzować teksty UI;
- [ ] zinwentaryzować teksty PDF;
- [ ] oznaczyć kontrakty, których nie wolno tłumaczyć;
- [ ] wskazać teksty generowane dynamicznie;
- [ ] wskazać statusy i enumy wymagające presenterów;
- [ ] zapisać listę brakujących tekstów;
- [ ] policzyć liczbę kluczy zasobów;
- [ ] zidentyfikować ekrany narażone na przepełnienie.

Artefakt:

```text
docs/internal-development-history/LOCALIZATION_STRING_INVENTORY.md
```

Gate:

> Każdy tekst ma kategorię i docelowy sposób obsługi.

---

## Etap 2 — fundament lokalizacji

Zadania:

- [ ] utworzyć zasoby `.resx`;
- [ ] skonfigurować kulturę aplikacji przed startem WPF;
- [ ] dodać trwałe ustawienie `CultureName`;
- [ ] dodać wybór języka w Ustawieniach;
- [ ] dodać komunikat o restarcie;
- [ ] dodać bezpieczny fallback;
- [ ] obsłużyć brak lub niepoprawną wartość ustawienia;
- [ ] dodać testy ustawienia kultury;
- [ ] potwierdzić brak migracji domenowej, jeśli ustawienie nie jest przechowywane w SQLite.

Polityka fallbacku:

```text
nieznana kultura
→ en-GB
```

albo:

```text
brak ustawienia
→ kultura systemowa, jeśli obsługiwana
→ w przeciwnym razie en-GB
```

Rekomendacja MVP:

```text
brak ustawienia:
- polski system → pl-PL
- pozostałe systemy → en-GB
```

Po pierwszym świadomym wyborze użytkownika ustawienie ma pierwszeństwo przed kulturą systemową.

Gate:

> Po restarcie aplikacja uruchamia się w wybranym języku, a nieobsługiwana kultura nie blokuje startu.

---

## Etap 3 — lokalizacja Desktop/WPF

Kolejność:

1. nawigacja i wspólne przyciski;
2. Dashboard;
3. wirtualne urządzenie tachografu;
4. Historia i luki;
5. Rekomensaty i decyzje alokacji;
6. Raporty;
7. Kierowcy i Ustawienia;
8. dialogi;
9. nakładki S1/S2;
10. komunikaty błędów i puste stany.

Zasady:

- nie umieszczać logiki tłumaczeniowej w ViewModelach;
- nie budować zdań przez sklejanie kilku zasobów, jeśli szyk może się różnić;
- używać parametrów formatowania;
- nie tłumaczyć identyfikatorów;
- nie lokalizować wartości zapisanych w bazie;
- nie zmieniać bindingów domenowych;
- po każdej zmianie XAML wykonać obowiązującą checklistę regresji.

Przykład:

```text
zamiast:
"Pozostało " + duration

użyć:
Compensation_RemainingFormat = "Pozostało: {0}"
Compensation_RemainingFormat = "Remaining: {0}"
```

Gate:

> Wszystkie ekrany są kompletne w obu językach i nie zawierają tekstów wpisanych na stałe poza zaakceptowanymi wyjątkami.

---

## Etap 4 — lokalizacja raportu PDF

Zadania:

- [ ] oddzielić tekst raportu od danych;
- [ ] dodać `ReportStrings`;
- [ ] lokalizować nagłówki i etykiety;
- [ ] lokalizować nazwy aktywności;
- [ ] lokalizować statusy rekompensat;
- [ ] lokalizować ostrzeżenia o lukach;
- [ ] lokalizować informację o kompletności;
- [ ] lokalizować zastrzeżenie prawne;
- [ ] zachować identyczne dane i identyfikatory;
- [ ] zweryfikować podział stron;
- [ ] zweryfikować szerokości kolumn;
- [ ] sprawdzić polskie znaki;
- [ ] sprawdzić angielskie teksty dłuższe od polskich.

Gate:

> PDF PL i PDF EN zawierają identyczne dane, różnią się wyłącznie tekstem prezentacyjnym.

---

## Etap 5 — testy automatyczne

Minimalny zestaw:

### Testy kultury

- brak ustawienia na polskim systemie → `pl-PL`;
- brak ustawienia na innym systemie → `en-GB`;
- zapis `pl-PL` → polski po restarcie;
- zapis `en-GB` → angielski po restarcie;
- nieobsługiwana kultura → fallback;
- uszkodzone ustawienie → bezpieczny start.

### Testy zasobów

- każdy wymagany klucz istnieje w `pl-PL`;
- każdy wymagany klucz istnieje w `en-GB`;
- brak pustych tłumaczeń;
- brak niezamierzonych duplikatów;
- placeholdery `{0}`, `{1}` są zgodne między językami;
- brak kluczy obecnych tylko w jednym języku.

### Testy presenterów

- wszystkie aktywności mają tłumaczenie;
- wszystkie statusy rekompensat mają tłumaczenie;
- wszystkie statusy luk mają tłumaczenie;
- wszystkie kody decyzji alokacji mają prezentację;
- nieznana wartość daje bezpieczny tekst techniczny, a nie wyjątek.

### Testy raportów

- PDF PL i EN powstają;
- oba zawierają te same liczby i identyfikatory;
- dane rekompensat są identyczne;
- bilans luk jest identyczny;
- liczba stron może się różnić, ale dane nie mogą zniknąć;
- JSON i techniczny CSV są bitowo lub semantycznie niezmienione przez język UI.

Gate:

> Pełny pakiet testów jest zielony, a wybór języka nie wpływa na wynik RuleEngine ani dane eksportowe.

---

## Etap 6 — ręczna regresja UI PL

Wykonać pełną checklistę `BETA_TEST_PLAN.md` w języku polskim:

- [ ] start aplikacji;
- [ ] Dashboard;
- [ ] wszystkie zakładki;
- [ ] oba sloty;
- [ ] tryby;
- [ ] Historia;
- [ ] Rekomensaty;
- [ ] Raporty;
- [ ] nakładki;
- [ ] dialogi;
- [ ] restart;
- [ ] pozycje nakładek;
- [ ] telemetria;
- [ ] automatyczna Jazda;
- [ ] blokady zależne od ruchu.

Dodatkowo:

- [ ] brak obciętych tekstów;
- [ ] brak zastępczych nazw kluczy;
- [ ] polskie znaki renderują się prawidłowo;
- [ ] tooltipy są kompletne;
- [ ] wszystkie komunikaty są po polsku.

Gate:

> UI PL przechodzi pełną regresję funkcjonalną i wizualną.

---

## Etap 7 — ręczna regresja UI EN

Powtórzyć całą checklistę w `en-GB`.

Szczególnie sprawdzić:

- [ ] dłuższe etykiety;
- [ ] szerokość lewego menu;
- [ ] przyciski Dashboardu;
- [ ] dialog decyzji alokacji;
- [ ] nakładki S1/S2;
- [ ] tabelę rekompensat;
- [ ] przyciski eksportu;
- [ ] komunikaty błędów;
- [ ] puste stany;
- [ ] tooltipy identyfikatorów;
- [ ] czytelność na mniejszej szerokości okna;
- [ ] poprawność terminologii tachografu.

Gate:

> UI EN przechodzi pełną regresję funkcjonalną i wizualną bez obcięć ani mieszanego języka.

---

## Etap 8 — gate wydania lokalizacyjnego

Warunki:

- [x] GO beta.11.1;
- [ ] fundament `.resx` gotowy;
- [ ] PL kompletne;
- [ ] EN kompletne;
- [ ] pełny pakiet testów zielony;
- [ ] build Release 0 błędów i 0 ostrzeżeń;
- [ ] checklista UI PL zaliczona;
- [ ] checklista UI EN zaliczona;
- [ ] PDF PL zaliczony wizualnie;
- [ ] PDF EN zaliczony wizualnie;
- [ ] JSON niezmieniony;
- [ ] techniczny CSV niezmieniony;
- [ ] `.tacho` niezmienione;
- [ ] SQLite bez nieplanowanej migracji;
- [ ] plugin v3 bez zmian;
- [ ] restart zachowuje język;
- [ ] dokumentacja instalacyjna PL/EN gotowa;
- [x] pakiet publikacyjny zawiera wymaganą notę licencyjną Unicode CLDR dla
      redystrybuowanych danych `CountryNames.pl.json`
      i `CountryNames.en-GB.json`;
- [ ] repozytorium czyste;
- [ ] artefakt self-contained `win-x64`;
- [ ] ZIP i SHA-256;
- [ ] smoke test wydania lokalizacyjnego.

Rekomendowany numer wydania należy ustalić po GO beta.11.1. Nie reużywać numeru istniejącego artefaktu.

---

## 8. Proponowane commity

1. `docs(localization): zinwentaryzuj teksty UI i raportów`
2. `feat(localization): dodaj fundament resx i wybór kultury`
3. `feat(localization): przenieś wspólne teksty Desktop do zasobów`
4. `feat(localization): zlokalizuj tachograf, dashboard i nakładki`
5. `feat(localization): zlokalizuj historię, rekompensaty i raporty`
6. `feat(reports): dodaj polski i angielski raport PDF`
7. `test(localization): sprawdź kompletność zasobów i kontrakty`
8. `docs(localization): dodaj instrukcję PL i EN`
9. `chore(release): przygotuj wydanie PL/EN`

Po każdym commicie:

- build;
- testy właściwego projektu;
- `git diff --check`;
- brak nieplanowanych zmian w kontraktach technicznych.

---

## 9. Kryteria akceptacji

Mini-projekt jest ukończony, gdy:

1. aplikacja uruchamia się w `pl-PL` i `en-GB`;
2. język jest wybierany w Ustawieniach;
3. wybór pozostaje po restarcie;
4. nieobsługiwana kultura nie blokuje startu;
5. wszystkie teksty użytkowe Desktop są lokalizowane;
6. wirtualny tachograf jest lokalizowany;
7. nakładki są lokalizowane;
8. dialogi i komunikaty są lokalizowane;
9. PDF jest dostępny w języku UI;
10. dane PDF są identyczne między językami;
11. JSON, techniczny CSV, `.tacho`, SQLite i protokół v3 pozostają niezmienne;
12. RuleEngine zwraca identyczne wyniki niezależnie od języka;
13. UI nie ma obcięć po angielsku;
14. nie występuje mieszany język poza świadomie zachowanymi kodami technicznymi;
15. pełna regresja PL jest zaliczona;
16. pełna regresja EN jest zaliczona;
17. build Release kończy się 0/0;
18. wszystkie testy są zielone;
19. artefakt wydania ma ZIP i SHA-256;
20. smoke test wydania lokalizacyjnego jest zaliczony.

---

## 10. Ryzyka i ograniczenia

| Ryzyko | Wpływ | Ograniczenie |
|---|---|---|
| Teksty angielskie nie mieszczą się w UI | Obcięcia, nieczytelne przyciski | Pełna regresja EN, elastyczne szerokości, unikanie sztywnych wysokości |
| Częściowa lokalizacja | Mieszany język i wrażenie niedokończenia | Inwentaryzacja, test kompletności kluczy |
| Tłumaczenie zmienia kontrakt techniczny | Uszkodzenie integracji i eksportów | Nie lokalizować nazw pól, kodów ani identyfikatorów |
| Sklejanie zdań z fragmentów | Niepoprawna gramatyka | Pełne wzorce z placeholderami |
| Dynamiczna zmiana języka komplikuje WPF | Bindingi i stare okna z różnymi kulturami | MVP stosuje zmianę po restarcie |
| PDF ma inne łamanie stron | Utrata czytelności lub danych | Osobna kontrola wizualna PL/EN |
| Lokalizacja opóźnia publikację | Przesunięcie terminu | Tylko PL/EN, bez kolejnych języków |
| Brak spójnej terminologii | Różne nazwy tych samych reguł | Słownik domenowy PL/EN |
| Nowe teksty omijają `.resx` | Narastanie długu lokalizacyjnego | Reguła code review i test wykrywający literały |
| Kultury wpływają na serializację | Rozjazd liczb lub dat w JSON/CSV | `InvariantCulture` dla kontraktów technicznych |

---

## 11. Słownik początkowy PL/EN

| Polski | English |
|---|---|
| Jazda | Driving |
| Inna praca | Other work |
| Dyspozycyjność | Availability |
| Przerwa / odpoczynek | Break / rest |
| Jazda ciągła | Continuous driving |
| Do przerwy | Time to break |
| Jazda dzienna | Daily driving |
| Praca dobowa | Daily duty |
| Odpoczynek dobowy | Daily rest |
| Odpoczynek tygodniowy | Weekly rest |
| Skrócony odpoczynek tygodniowy | Reduced weekly rest |
| Regularny odpoczynek tygodniowy | Regular weekly rest |
| Rekompensata | Compensation |
| Zobowiązanie | Obligation |
| Pozostały dług | Remaining debt |
| Termin | Deadline |
| W terminie | On time |
| Zaległa | Overdue |
| Spłacona w terminie | Paid on time |
| Spłacona po terminie | Paid late |
| Luka aktywności | Activity gap |
| Luka nierozliczona | Unresolved gap |
| Wpis manualny | Manual entry |
| Karta wyjęta | Card removed |
| Skok czasu do przodu | Forward time jump |
| Podwójna obsada | Multi-manning |
| Pojedyncza obsada | Single-manning |
| Kierowca aktywny | Active driver |
| Kierowca zmiennik | Co-driver |
| Raport diagnostyczny | Diagnostic report |
| Materiał dowodowy kompletny | Evidence complete |
| Oczekiwanie na ETS2 | Waiting for ETS2 |
| Ustawienia | Settings |
| Język | Language |
| Zastosuj po restarcie | Apply after restart |

Słownik jest punktem startowym. Przed implementacją powinien zostać zweryfikowany pod kątem spójności z terminologią stosowaną w oficjalnych angielskich materiałach dotyczących czasu jazdy i odpoczynku.

---

## 12. Poza zakresem mini-projektu

- dynamiczna zmiana języka bez restartu;
- więcej niż dwa języki;
- tłumaczenie logów technicznych;
- tłumaczenie kodów błędów;
- tłumaczenie nazw pól JSON;
- tłumaczenie technicznego CSV;
- lokalizacja formatu `.tacho`;
- lokalizacja schematu SQLite;
- automatyczne pobieranie tłumaczeń;
- zewnętrzna platforma translatorska;
- automatyczne tłumaczenie maszynowe w runtime;
- osobny język dla każdego raportu;
- lokalizacja dokumentacji deweloperskiej;
- tłumaczenie nazw własnych użytkownika;
- instalator wielojęzyczny;
- dodatkowe języki europejskie.

---

## 13. Rekomendowany następny krok

Formalny GO beta.11.1 został zapisany 23 lipca 2026. Nie zmieniać zamrożonego
artefaktu i nie scalać do niego późniejszych zmian UI ani lokalizacji.

Po decyzji właściciela lokalizację rozpoczęto na gałęzi
`codex/m5-localization-pl-en`. Obowiązują kolejne kroki:

1. wykonać Etap 1 — inwentaryzację tekstów;
2. zatwierdzić neutralny język zasobów i politykę fallbacku;
3. dopiero potem wdrażać `.resx`.

---

## 14. Definicja ukończenia

> Mini-projekt lokalizacji PL/EN jest ukończony, gdy ta sama wersja ETS2 EU Digital Tachograph działa poprawnie po polsku i po angielsku, zachowuje identyczne wyniki domenowe oraz dane eksportowe, prezentuje kompletny i czytelny interfejs w obu językach, generuje poprawne raporty PDF i przechodzi osobny gate regresji PL/EN przed pierwszą szeroką publikacją.
