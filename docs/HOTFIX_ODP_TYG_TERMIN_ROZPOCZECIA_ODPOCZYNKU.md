# HOTFIX — `ODP. TYG.`: termin rozpoczęcia odpoczynku tygodniowego na LCD

**Projekt:** ETS2 EU Digital Tachograph  
**Obszar:** wirtualny tachograf / ekran liczników karty  
**Typ zmiany:** hotfix prezentacyjny  
**Priorytet:** P1  
**Status dokumentu:** **ZAKOŃCZONY — GREEN**
**Data:** 24 lipca 2026  
**Baza wydaniowa:** `0.1.0-beta.11.1`  
**Stan bieżącego drzewa przed hotfixem:** lokalne zmiany po beta.11.1, `315/315` testów Release, build `0 błędów / 0 ostrzeżeń`  
**Planowany gate odbioru:** testy z IDE i smoke test kandydata beta.12  
**Zakres wersji:** zmiana lokalna; nie należy przypisywać jej do opublikowanego artefaktu beta.11.1

## Wynik wdrożenia — 24 lipca 2026

- Etap 1 wykonano przed implementacją: 18/18 przypadków formatera oraz 4/4
  przypadki projekcji potwierdzono w stanie **RED**.
- `RegulationState` wystawia `WeeklyRestWindowElapsedMinutes` i
  `WeeklyRestStartDeadlineGameMinute` z jednej kotwicy RuleEngine.
- `WeeklyRestWindowFormatter` jest wspólny dla S1 i S2, używa wyłącznie liczb
  `game_time`, obsługuje `1/6–6/6+` i fallback `—/6 (—)`.
- Nie zmieniono XAML, SQLite, historii, protokołu telemetrii ani progów reguł.
- Testy celowane: formatter 18/18; wybrane testy RuleEngine 11/11.
- Pełny gate Release: **338/338**, build **0 błędów / 0 ostrzeżeń**.
- Ręczny smoke LCD S1/S2 z aktywną telemetrią: **GREEN**, potwierdzony przez
  użytkownika 24 lipca 2026.

---

## 1. Status i decyzja projektowa

Obecny format licznika w kodzie (`FormatWeeklyRestWindow` w `MainViewModel`) liczy w **dół** czas pozostały do granicy sześciu okresów. Tuż po odpoczynku pokazuje domyślnie:

```text
ODP. TYG. 6/6 (144:00)
```

i maleje wraz z upływem `game_time` (dla danych z sekcji 4, tj. `elapsed = 89:39`, obecny kod renderuje `2/6 (54:21)`). Znacznik przekroczenia `6/6+` jest przy tym **martwy** — czas pozostały jest clampowany do zera, więc po `144:00` wyświetla się `0/6 (00:00)`.

Format docelowy (dla danych z sekcji 4, tj. `elapsed = 89:39`):

```text
ODP. TYG. 4/6 (D141 22:55)
```

Hotfix zmienia zatem trzy rzeczy naraz:

- **semantykę licznika** — liczenie w górę numeru bieżącego okresu `1/6 → 6/6` (zgodnie z sekcją 3.2), zamiast obecnego odliczania w dół;
- **znacznik przekroczenia** — ożywienie `6/6+` po `144:00`;
- **treść nawiasu** — zamiast czasu telemetrycznego stały termin rozpoczęcia odpoczynku w `game_time`.

Zatwierdzony kontrakt:

- numerator to **numer bieżącego** okresu 24-godzinnego, licząc od `1/6` dla świeżego okna do `6/6` w szóstym okresie;
- mianownik pozostaje stały: `6`;
- numerator zmienia się tylko na pełnej granicy doby (`24:00`, `48:00`, …), bez zaokrąglania niepełnej doby w górę;
- wartość w nawiasie nie pokazuje już dokładnego czasu telemetrycznego;
- nawias pokazuje najpóźniejszy czas gry, w którym następny odpoczynek tygodniowy powinien się rozpocząć;
- termin jest prezentowany wyłącznie w `game_time` ETS2;
- zegar Windows nie uczestniczy w obliczeniu;
- źródłowa historia, progi `RuleEngine`, telemetria, SQLite i eksporty pozostają bez zmian.

Hotfix zmienia sposób prezentacji oraz — tylko jeżeli jest to potrzebne — rozszerza projekcję aplikacyjną o jawny termin. Nie tworzy drugiej logiki regulacyjnej w warstwie Desktop.

---

## 2. Problem użytkowy

Obecny licznik (odliczający w dół, np. `6/6 (144:00)` → `0/6`) pokazuje czas związany z oknem, ale wymaga od użytkownika samodzielnego obliczenia, do kiedy sześciodniowe okno się zamyka.

Użytkownik potrzebuje informacji operacyjnej:

> Do którego dnia i której godziny czasu gry należy rozpocząć odpoczynek tygodniowy?

Nowy zapis zachowuje zwarty licznik okresów, a jednocześnie podaje jednoznaczny termin:

```text
4/6 (D141 22:55)
```

Znaczenie:

- trwa czwarty z sześciu okresów 24-godzinnych (minęły trzy pełne okresy);
- granica sześciu okresów przypada na Dzień 141 o 22:55 czasu gry.

---

## 3. Kontrakt prezentacji

### 3.1. Format podstawowy

```text
{numerOkresu}/6 (D{dzień} {HH:mm})
```

Przykład:

```text
4/6 (D141 22:55)
```

Format kompaktowy jest obowiązujący na LCD. Pełna forma może być użyta w przyszłym tooltipie lub widoku szczegółowym:

```text
4/6 — rozpocznij odpoczynek najpóźniej: Dzień 141, 22:55
```

Pełna forma nie należy do obowiązkowego zakresu tego hotfixu.

### 3.2. Numer bieżącego okresu

```text
currentPeriod = min(floor(elapsedMinutes / 1440) + 1, 6)
```

Zasady:

- licznik jest 1-based: świeże okno (`elapsed = 0`) pokazuje `1/6`, nie `0/6`;
- numerator to numer bieżącego, trwającego okresu 24-godzinnego (`1`..`6`);
- inkrementacja następuje dokładnie na pełnej granicy doby: `24:00 → 2/6`, `48:00 → 3/6`, … `120:00 → 6/6`;
- wartość maksymalna bez znacznika przekroczenia: `6`;
- dokładnie przy `144:00` nadal wyświetlane jest `6/6` (ostatni legalny moment rozpoczęcia);
- po przekroczeniu `144:00` (od `144:01`) wyświetlane jest `6/6+`;
- znak `+` oznacza przekroczenie sześciu okresów w liczniku prezentacyjnym; ocena naruszenia nadal należy wyłącznie do `RuleEngine`.

### 3.3. Termin rozpoczęcia odpoczynku

Preferowane źródło:

```text
WeeklyRestStartDeadlineGameMinute
```

lub istniejące pole o równoważnej semantyce, już wyliczane przez `RuleEngine` / warstwę Application.

Jeżeli projekcja udostępnia wyłącznie kotwicę końca poprzedniego zakwalifikowanego odpoczynku tygodniowego, termin można wyprowadzić w warstwie Application:

```text
WeeklyRestStartDeadlineGameMinute
= PreviousQualifiedWeeklyRestEndGameMinute + 6 × 1440
```

Nie wolno:

- parsować tekstu `89:39` z UI;
- liczyć terminu w XAML;
- wyprowadzać terminu z zegara Windows;
- utrzymywać osobnej kopii reguły w `MainViewModel` i `OverlayViewModel`;
- przesuwać terminu przy każdej klatce, jeżeli kotwica odpoczynku się nie zmieniła.

### 3.4. Formatowanie `game_time`

Dla wartości `deadlineGameMinute`:

```text
displayedDay = floor(deadlineGameMinute / 1440) + 1
minuteOfDay  = deadlineGameMinute % 1440
hour         = floor(minuteOfDay / 60)
minute       = minuteOfDay % 60
```

Wynik LCD:

```text
D{displayedDay} {hour:00}:{minute:00}
```

Przykłady:

```text
D1 00:00
D9 07:05
D141 22:55
```

Obowiązuje projektowy niezmiennik numeracji dni:

```text
Dzień wyświetlany = floor(GameMinute / 1440) + 1
```

### 3.5. Brak danych

Jeżeli termin nie może zostać wiarygodnie ustalony, LCD nie może pokazywać zmyślonej daty.

Zatwierdzony fallback:

```text
—/6 (—)
```

Jeżeli liczba okresów jest dostępna, ale brakuje wyłącznie terminu, dopuszczalne jest:

```text
4/6 (—)
```

Taki stan powinien zostać zapisany diagnostycznie, ale nie może powodować wyjątku UI.

---

## 4. Przykład referencyjny

Dane:

```text
bieżący game_time: Dzień 139, 16:34
surowa minuta:     199714
upłynęło:          89:39
```

Przeliczenie czasu wykorzystanego:

```text
89 × 60 + 39 = 5379 minut
floor(5379 / 1440) + 1 = 4 → bieżący (4.) okres → 4/6
```

Pozostało do granicy sześciu okresów:

```text
144:00 − 89:39 = 54:21
```

Termin:

```text
Dzień 139, 16:34 + 54:21
= Dzień 141, 22:55
```

Oczekiwany LCD:

```text
ODP. TYG. 4/6 (D141 22:55)
```

Wartość `D141 22:55` ma pozostać stała przy kolejnych minutach czasu gry, dopóki nie zmieni się kotwica poprzedniego odpoczynku tygodniowego lub historia kanoniczna.

---

## 5. Tabela stanów granicznych

Poniższa tabela zakłada ten sam termin `D141 22:55` i zmienia wyłącznie czas wykorzystany.

| Czas wykorzystany | Oczekiwany LCD |
|---:|---|
| `00:00` | `1/6 (D141 22:55)` |
| `23:59` | `1/6 (D141 22:55)` |
| `24:00` | `2/6 (D141 22:55)` |
| `47:59` | `2/6 (D141 22:55)` |
| `48:00` | `3/6 (D141 22:55)` |
| `72:00` | `4/6 (D141 22:55)` |
| `89:39` | `4/6 (D141 22:55)` |
| `95:59` | `4/6 (D141 22:55)` |
| `96:00` | `5/6 (D141 22:55)` |
| `120:00` | `6/6 (D141 22:55)` |
| `143:59` | `6/6 (D141 22:55)` |
| `144:00` | `6/6 (D141 22:55)` |
| `144:01` | `6/6+ (D141 22:55)` |

Nie wolno pokazywać:

```text
7/6
8/6
```

Po przekroczeniu mianownik pozostaje `6`, a termin w nawiasie nie przesuwa się do przodu.

---

## 6. Odpowiedzialność warstw

### 6.1. `RuleEngine`

Odpowiada za:

- kwalifikację odpoczynków tygodniowych;
- kotwicę sześciu okresów 24-godzinnych;
- termin rozpoczęcia kolejnego odpoczynku tygodniowego albo dane potrzebne do jego wyprowadzenia;
- status regulacyjny i naruszenia.

Hotfix nie może zmieniać progów, klasyfikacji ani sposobu tworzenia historii.

### 6.2. Application / DTO

Odpowiada za:

- przekazanie terminu do Desktopu;
- zapewnienie, że liczba okresów i termin pochodzą z tego samego snapshotu;
- bezpieczny stan braku danych;
- brak zależności od tekstu prezentacyjnego.

Proponowane pole projekcji:

```csharp
long? WeeklyRestStartDeadlineGameMinute
```

Nazwa może zostać dopasowana do istniejącego kontraktu, ale musi jednoznacznie wskazywać, że chodzi o termin **rozpoczęcia**, nie zakończenia odpoczynku.

### 6.3. Desktop

Odpowiada wyłącznie za:

- obliczenie numeru bieżącego okresu (1-based) z przekazanych minut albo użycie gotowej wartości projekcji;
- format `1/6`–`6/6+`;
- format `D141 22:55`;
- fallback `—`;
- prezentację identyczną dla karty 1 i karty 2.

Preferowane jest wydzielenie czystego formatera, np.:

```text
WeeklyRestWindowFormatter
```

zamiast dalszego rozbudowywania składania tekstu bezpośrednio w `MainViewModel`.

### 6.4. XAML

XAML ma jedynie wyświetlać gotowy tekst. Nie może zawierać:

- konwertera obliczającego regułę sześciu okresów;
- dodawania 144 godzin;
- numeracji dnia;
- logiki przekroczenia.

---

## 7. Plan wdrożenia

### Etap 0 — audyt istniejącej projekcji

1. Znaleźć aktualne źródło wartości `89:39`.
2. Potwierdzić, czy `RuleEngine`, snapshot lub DTO już udostępnia:
   - początek sześciu okresów;
   - koniec poprzedniego zakwalifikowanego odpoczynku tygodniowego;
   - gotowy termin rozpoczęcia następnego odpoczynku.
3. Sprawdzić, czy obie karty korzystają ze wspólnego formatera.
4. Potwierdzić, że aktualna wartość pochodzi z `game_time`, nie z zegara systemowego.

**Gate etapu:** wskazane jedno źródło prawdy; brak planu liczenia terminu z tekstu UI.

### Etap 1 — czerwone testy

Najpierw dodać testy:

- przykładu `89:39 → 4/6 (D141 22:55)`;
- wszystkich granic z tabeli w sekcji 5;
- numeracji dnia `+1`;
- przejścia przez północ;
- niezmienności terminu przy postępie czasu;
- fallbacku braku danych;
- niezależnych danych S1 i S2;
- odtworzenia po restarcie.

**Gate etapu:** testy nowego kontraktu zawodzą przed implementacją.

### Etap 2 — projekcja terminu

W kolejności preferencji:

1. użyć istniejącego terminu z `RuleEngine` / snapshotu;
2. jeżeli istnieje tylko kotwica, wyprowadzić termin w Application;
3. jeżeli brak obu wartości, rozszerzyć kontrakt projekcji bez duplikowania historii i reguł w Desktop.

**Gate etapu:** Desktop otrzymuje jawny `gameMinute` terminu z tego samego snapshotu co licznik.

### Etap 3 — formatter LCD

1. Wyliczyć numer bieżącego okresu 1-based (`floor(elapsed/1440)+1`, obcięte do `6`), bez zaokrąglania niepełnej doby w górę.
2. Usunąć dokładny czas telemetryczny z nawiasu.
3. Dodać formatowanie terminu `D{n} HH:mm`.
4. Zachować `6/6+` po przekroczeniu.
5. Dodać bezpieczny fallback.

**Gate etapu:** wszystkie testy formatera są zielone.

### Etap 4 — podłączenie obu kart

1. Podłączyć identyczny kontrakt dla czytnika 1 i czytnika 2.
2. Sprawdzić menu liczników obu kart.
3. Nie zmieniać innych ekranów ani eksportów przy okazji.
4. Zmienić XAML tylko wtedy, gdy nowy tekst nie mieści się w istniejącym polu.

**Gate etapu:** S1 i S2 pokazują własny termin, bez przeciekania danych między kartami.

### Etap 5 — dokumentacja

Zaktualizować bieżące dokumenty, w których występuje stary przykład `3/6 (89:39)`:

- `README.md`;
- `BETA_TEST_PLAN.md`;
- `RELEASE_NOTES.md` — sekcja zmian lokalnych / kandydata beta.12;
- `PROJECT_HANDOFF.md`;
- `UI_VISIBLE_DATA_REPORT_BETA4.md` — bieżąca aktualizacja;
- `RAPORT_PRAC_UI_2026-07-23.md` — dodać notę, że wcześniejszy format został zastąpiony hotfixem, bez przepisywania historycznego przebiegu prac;
- ewentualne inne wystąpienia znalezione przez wyszukiwanie repozytorium.

**Gate etapu:** w aktywnej dokumentacji nie pozostaje stary format jako bieżący kontrakt.

### Etap 6 — pełny gate

1. Uruchomić pełny pakiet testów Release.
2. Zbudować rozwiązanie Release bez błędów i nowych ostrzeżeń.
3. Uruchomić aplikację z IDE.
4. Wykonać ręczny smoke opisany w sekcji 9.
5. Jeżeli zmieniono XAML, wykonać pełną checklistę regresji UI z `BETA_TEST_PLAN.md`.
6. Zachować hotfix do smoke testu beta.12.

---

## 8. Macierz testów automatycznych

| ID | Scenariusz | Wejście | Oczekiwany wynik |
|---|---|---|---|
| WRF-01 | Przykład referencyjny | `now=199714`, `elapsed=5379`, `deadline=202975` | `4/6 (D141 22:55)` |
| WRF-02 | Świeże okno / przed pierwszą dobą | `23:59` | `1/6 (...)` |
| WRF-03 | Pierwsza pełna doba | `24:00` | `2/6 (...)` |
| WRF-04 | Próg `96:00` | `95:59` / `96:00` | `4/6` / `5/6` |
| WRF-05 | Ostatnia minuta przed granicą | `143:59` | `6/6 (...)` |
| WRF-06 | Dokładna granica | `144:00` | `6/6 (...)` |
| WRF-07 | Przekroczenie | `144:01` | `6/6+ (...)` |
| WRF-08 | Numeracja dnia | `deadline=0` | `D1 00:00` |
| WRF-09 | Przejście przez północ | termin na kolejnym dniu | poprawny dzień i `00:xx` |
| WRF-10 | Termin stały | kilka kolejnych wartości `now` przy tej samej kotwicy | identyczny nawias |
| WRF-11 | Brak terminu | `deadline=null` | `4/6 (—)` albo `—/6 (—)` zgodnie z dostępnością danych |
| WRF-12 | Dwie karty | różne kotwice S1/S2 | dwa niezależne terminy |
| WRF-13 | Restart | ta sama historia kanoniczna przed i po restarcie | identyczny tekst |
| WRF-14 | Cofnięcie / nowa gałąź | zmieniona historia kanoniczna | termin przeliczony z nowej projekcji, bez starego cache |
| WRF-15 | Pauza gry | `running == 0` | tekst nie przesuwa się od czasu rzeczywistego |
| WRF-16 | Brak regresji domenowej | istniejące testy `RuleEngine` | identyczne wyniki progów i naruszeń |

Przykładowe nazwy testów:

```text
WeeklyRestWindowFormatter_FormatsReferenceDeadline
WeeklyRestWindowFormatter_UsesFullCompletedPeriodsWithoutRounding
WeeklyRestWindowFormatter_AddsPlusOnlyAfterSixFullPeriods
WeeklyRestWindowFormatter_FormatsDisplayedDayWithPlusOneRule
WeeklyRestWindowFormatter_DoesNotMoveDeadlineForSameAnchor
WeeklyRestWindowFormatter_UsesSafeFallbackWhenDeadlineMissing
MainViewModel_ProjectsIndependentWeeklyRestDeadlinesForBothSlots
```

---

## 9. Ręczny smoke test z IDE

### 9.1. Podstawowa prezentacja

- [ ] Otwórz menu liczników karty 1.
- [ ] Potwierdź format `x/6 (Dxxx HH:mm)`.
- [ ] Otwórz menu liczników karty 2.
- [ ] Potwierdź niezależną wartość drugiej karty.
- [ ] Sprawdź, że tekst mieści się na LCD i nie nachodzi na ikony ani nawigację.

### 9.2. Stabilność terminu

- [ ] Zapisz widoczny termin.
- [ ] Przesuń `game_time` o kilka minut bez kwalifikującego odpoczynku tygodniowego.
- [ ] Potwierdź, że licznik czasu postępuje, ale termin w nawiasie pozostaje ten sam.
- [ ] Przejdź przez północ czasu gry i sprawdź numer dnia.

### 9.3. Granice okresów

- [ ] Sprawdź przejście `1/6 → 2/6` dopiero przy pełnym `24:00`.
- [ ] Sprawdź `4/6 → 5/6` dopiero przy `96:00`.
- [ ] Sprawdź `5/6 → 6/6` przy `120:00`.
- [ ] Sprawdź `6/6+` po przekroczeniu granicy.

### 9.4. Restart i telemetria

- [ ] Zamknij aplikację.
- [ ] Uruchom ponownie przy tej samej bazie.
- [ ] Potwierdź identyczny termin obu kart.
- [ ] Wstrzymaj grę / otwórz menu ETS2.
- [ ] Potwierdź, że licznik nie rośnie od czasu Windows.
- [ ] Wznów telemetrię i sprawdź dalszą aktualizację.

### 9.5. Diagnostyka

- [ ] Brak nowych błędów bindingów.
- [ ] Brak wyjątków formatera przy braku karty lub danych.
- [ ] Brak różnicy między wartością po restarcie i przed restartem dla tej samej historii.

---

## 10. Pliki prawdopodobnie objęte zmianą

Dokładna lista zależy od wyniku audytu Etapu 0.

### Kod

- `src/ETS2Tachograph.Desktop/ViewModels/MainViewModel.cs`;
- ewentualnie nowy `src/ETS2Tachograph.Desktop/Presentation/WeeklyRestWindowFormatter.cs`;
- kontrakt DTO / snapshotu w `ETS2Tachograph.Application`, jeżeli termin nie jest jeszcze wystawiony;
- `src/ETS2Tachograph.Desktop/Views/MainWindow.xaml` wyłącznie wtedy, gdy konieczna jest korekta szerokości.

### Testy

- nowy `tests/ETS2Tachograph.Desktop.Tests/WeeklyRestWindowFormatterTests.cs`;
- ewentualny test projekcji w `ETS2Tachograph.Application.Tests`;
- aktualizacja testów istniejącego formatera `ODP. TYG.`.

### Dokumentacja

- pliki wymienione w Etapie 5.

---

## 11. Ryzyka i zabezpieczenia

| Ryzyko | Skutek | Zabezpieczenie |
|---|---|---|
| Termin liczony z tekstu `89:39` | dryf, błędy parsowania i lokalizacji | użyć liczby minut / terminu ze snapshotu |
| Termin liczony osobno od licznika | `4/6` i data z różnych stanów | jeden atomowy snapshot dla obu wartości |
| Użycie zegara Windows | termin zmienia się podczas pauzy lub poza ETS2 | wyłącznie `game_time` |
| Błąd dnia o `−1` | wyświetlenie Dnia 140 zamiast 141 | test reguły `floor(minute/1440)+1` |
| Przesuwający się nawias | użytkownik nigdy nie widzi stałej granicy | termin oparty na stałej kotwicy odpoczynku |
| Duplikacja reguły w Desktop | rozjazd z `RuleEngine` | termin wystawiony przez Application / snapshot |
| Zbyt długi tekst LCD | obcięcie lub nachodzenie | kompaktowy format `D141 22:55`, kontrola wizualna |
| Błędna interpretacja `+` jako samodzielnego naruszenia | fałszywy komunikat prawny | `+` pozostaje markerem prezentacyjnym; status pochodzi z `RuleEngine` |
| Cache po cofnięciu czasu | termin ze starej gałęzi | odświeżenie z bieżącej historii kanonicznej i test restartu/branch |
| Przypadkowa zmiana innych ekranów | scope creep i regresje | hotfix ograniczony do `ODP. TYG.` i wymaganej projekcji |

---

## 12. Poza zakresem hotfixu

Hotfix nie obejmuje:

- zmiany kwalifikacji odpoczynku tygodniowego;
- zmiany progu sześciu okresów 24-godzinnych;
- zmiany sposobu tworzenia lub resetowania długu rekompensacyjnego;
- modyfikacji historii minutowej;
- migracji SQLite;
- zmiany protokołu telemetrii v3;
- zmiany PDF, JSON, CSV ani `.tacho`;
- dodania odliczania w czasie rzeczywistym;
- dynamicznej lokalizacji PL/EN;
- przebudowy Dashboardu, overlayu lub Raportów;
- zmiany znaczenia trwającego odpoczynku tygodniowego poza istniejącą projekcją `RuleEngine`;
- Planera podróży.

Jeżeli audyt wykaże, że termin nie istnieje w obecnej projekcji, dopuszczalne jest minimalne rozszerzenie DTO. Nie jest dopuszczalne tworzenie nowej, niezależnej oceny regulacyjnej w Desktop.

---

## 13. Proponowane commity

1. `docs(hotfix): opisz termin rozpoczęcia odpoczynku w liczniku ODP TYG`
2. `test(ui): dodaj regresje formatu ODP TYG z terminem game time`
3. `fix(application): wystaw termin rozpoczęcia odpoczynku tygodniowego`
4. `fix(ui): pokaż Dzień i godzinę zamiast czasu telemetrycznego ODP TYG`
5. `docs(beta12): zaktualizuj test plan i dokumentację licznika`

Po każdym commicie:

- build właściwego projektu;
- odpowiednie testy;
- `git diff --check`;
- brak zmian w progach `RuleEngine`;
- brak nieplanowanej migracji i zmiany protokołu.

---

## 14. Kryteria akceptacji

Hotfix jest ukończony, gdy:

1. `ODP. TYG.` pokazuje format `x/6 (Dxxx HH:mm)`;
2. przykład referencyjny daje dokładnie `4/6 (D141 22:55)`;
3. numerator to numer bieżącego okresu (1-based, `1/6` dla świeżego okna), bez zaokrąglania niepełnej doby w górę;
4. przejście na `5/6` następuje dopiero przy `96:00`;
5. dokładnie przy `144:00` widoczne jest `6/6`;
6. po przekroczeniu widoczne jest `6/6+`;
7. termin jest stały dla tej samej kotwicy;
8. termin korzysta wyłącznie z `game_time`;
9. dzień jest formatowany z regułą `+1`;
10. obie karty pokazują własne dane;
11. brak danych nie powoduje wyjątku ani zmyślonego terminu;
12. po restarcie ta sama historia daje identyczny wynik;
13. cofnięcie czasu nie pozostawia terminu ze starej gałęzi;
14. pauza/menu ETS2 nie przesuwa wartości od zegara Windows;
15. `RuleEngine` zwraca identyczne wyniki jak przed hotfixem;
16. SQLite i protokół v3 pozostają bez zmian;
17. pełny pakiet testów Release jest zielony;
18. build Release kończy się `0 błędów / 0 ostrzeżeń`;
19. kontrola wizualna LCD S1/S2 jest zaliczona;
20. aktywna dokumentacja nie opisuje już `3/6 (89:39)` jako bieżącego formatu;
21. smoke test kandydata beta.12 potwierdza działanie na rzeczywistej telemetrii.

---

## 15. Definicja ukończenia

> Hotfix `ODP. TYG.` jest ukończony, gdy licznik zachowuje zatwierdzoną formę okresów `1/6–6/6+`, lecz zamiast mało użytecznego czasu telemetrycznego pokazuje stały, poprawnie sformatowany termin rozpoczęcia kolejnego odpoczynku tygodniowego w `game_time`, identycznie po restarcie i dla obu kart, bez zmiany historii, progów `RuleEngine`, SQLite, protokołu telemetrii ani kontraktów eksportowych.
