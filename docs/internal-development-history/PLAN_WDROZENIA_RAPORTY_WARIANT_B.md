# Plan wdrożenia — Raporty i statystyki, wariant B

**Projekt:** ETS2 EU Digital Tachograph  
**Obszar:** ekran `RAPORTY I STATYSTYKI`  
**Wariant:** B — centrum raportowe  
**Status dokumentu:** zatwierdzony plan wykonawczy, implementacja nierozpoczęta  
**Data:** 24 lipca 2026  
**Baza wydaniowa:** `0.1.0-beta.11.1`  
**Stan bieżącego drzewa przed rozpoczęciem:** zmiany lokalne po beta.11.1, `315/315` testów Release, build `0 błędów / 0 ostrzeżeń`  
**Docelowy gate:** testy z IDE i smoke test kandydata beta.12  
**Makieta referencyjna:** `docs/images/reports-dashboard.png`

---

## 1. Status i decyzja projektowa

Wariant B został zaakceptowany jako docelowa przebudowa ekranu raportów. Zmiana ma uporządkować cały przepływ:

```text
KONFIGURACJA
→ KONTROLA DANYCH
→ PODGLĄD
→ EKSPORT
```

Nie jest to nowy silnik raportowy. Historia kanoniczna, `RuleEngine`, `ReportService`, kontrakty PDF/JSON/CSV, SQLite i protokół telemetrii pozostają źródłem danych oraz logiki. Nowy ekran ma jedynie bezpiecznie prowadzić użytkownika przez wybór parametrów, ocenę kompletności, podgląd i eksport.

Wdrożenie będzie prowadzone jako lokalny zakres po beta.11.1. Nie należy przypisywać go do opublikowanego artefaktu beta.11.1 ani publikować nowej paczki bez osobnej decyzji wydaniowej.

---

## 2. Cel biznesowy i użytkowy

### 2.1. Problem obecnego ekranu

Obecny ekran łączy w jednym miejscu:

- wybór kierowcy lub karty;
- ręczny zakres czasu gry;
- generowanie raportu;
- podsumowania;
- techniczną tabelę historii;
- trzy niezależne przyciski eksportu.

Użytkownik nie otrzymuje jednoznacznej odpowiedzi:

- dla którego kierowcy i karty powstanie plik;
- jaki dokładnie zakres zostanie użyty;
- czy widoczny podgląd odpowiada aktualnym parametrom;
- czy dane są kompletne;
- czy eksport użyje bieżącego, czy wcześniej wygenerowanego raportu;
- czym różnią się PDF, VTC JSON, CSV zobowiązań i surowy CSV.

### 2.2. Cel wariantu B

Po wdrożeniu użytkownik ma przejść przez raport bez znajomości technicznych szczegółów aplikacji:

1. wybrać kierowcę i kartę;
2. wybrać typowy albo własny zakres `game_time`;
3. natychmiast zobaczyć kompletność danych;
4. przejrzeć podsumowanie, aktywności, naruszenia, rekompensaty i luki;
5. wyeksportować właściwy format z dokładnie tych samych parametrów.

### 2.3. Mierzalny rezultat

Zmiana jest udana, gdy:

- nie istnieje niejawny „pusty zakres”;
- każdy eksport ma widocznego kierowcę, kartę i zakres;
- podgląd ma jawny stan `aktualny / nieaktualny / niekompletny / błąd`;
- eksport nigdy nie używa parametrów innych niż opisane na ekranie;
- informacje użytkowe są po polsku, a dane techniczne są ukryte domyślnie;
- PDF, JSON i CSV zachowują dotychczasowe kontrakty danych;
- pełna regresja UI i eksportów pozostaje zielona.

---

## 3. Zatwierdzony kontrakt wizualny wariantu B

## 3.1. Pasek etapów

Nad konfiguracją znajduje się informacyjny pasek:

```text
1 KONFIGURACJA  —  2 KONTROLA DANYCH  —  3 PODGLĄD  —  4 EKSPORT
```

Pasek nie jest kreatorem z osobnymi stronami i nie może blokować swobodnej pracy. Pokazuje jedynie stan procesu:

- `KONFIGURACJA` — parametry są poprawne;
- `KONTROLA DANYCH` — raport został przeliczony, a kompletność oceniona;
- `PODGLĄD` — dostępny jest aktualny wynik;
- `EKSPORT` — co najmniej jeden eksport jest dostępny.

Status nie może być przekazywany wyłącznie kolorem. Aktywny etap otrzymuje również numer, tekst i odpowiedni opis dla czytnika interfejsu.

## 3.2. Sekcja konfiguracji

Sekcja zawiera:

### Kierowca / karta

Format widoczny:

```text
Arkadiusz — karta Staniek
```

Opcjonalnie można dodać bieżący slot jako informację pomocniczą:

```text
Arkadiusz — karta Staniek · S1
```

Slot nie jest częścią trwałej tożsamości karty i nie może wpływać na historyczny raport.

### Szybkie zakresy

MVP wariantu B zawiera:

- `BIEŻĄCY TYDZIEŃ` — bieżący tydzień regulacyjny z uwzględnieniem `WeekEpochOffsetDays`;
- `OSTATNIE 24 H` — ostatnie 1440 minut czasu gry;
- `CAŁA HISTORIA` — pełny dostępny zakres wybranej karty;
- `WŁASNY ZAKRES` — jawny wybór dnia i godziny początku oraz końca.

### Własny zakres

Kontrolki:

```text
OD  [ Dzień 124 ▼ ] [ 23:00 ]
DO  [ Dzień 139 ▼ ] [ 16:34 ]
```

Pod nimi:

```text
Zakres obejmuje: 14 dni 17:34
```

Wewnętrznie zakres ma korzystać z minut `game_time`. Dla nowych kontraktów rekomendowany jest przedział półotwarty:

```text
[FromGameMinuteInclusive, ToGameMinuteExclusive)
```

Jeżeli obecny `ReportService` używa innej semantyki, mapper UI ma ją zachować. Zmiana znaczenia istniejącego zakresu nie należy do tego projektu.

### Odświeżanie

Przycisk główny:

```text
ODŚWIEŻ PODGLĄD
```

Zachowanie:

- zmiana kierowcy albo gotowego zakresu może odświeżyć podgląd automatycznie po pojedynczej zmianie;
- ręczna edycja dnia lub godziny oznacza podgląd jako nieaktualny;
- `Enter` w ostatnim polu lub przycisk `ODŚWIEŻ PODGLĄD` uruchamia przeliczenie;
- podczas obliczania drugi request nie może zostać uruchomiony;
- ostatni prawidłowy podgląd może pozostać widoczny, ale musi być oznaczony jako nieaktualny.

## 3.3. Kontrola danych i pasek statusu

Po wygenerowaniu raportu pojawia się stały pasek stanu.

Przykład kompletny:

```text
✓ PODGLĄD AKTUALNY
Dane kompletne · 0 luk · 1 naruszenie · 1 otwarte zobowiązanie
```

Przykład niekompletny:

```text
⚠ RAPORT NIEKOMPLETNY
2 nierozliczone luki · 01:17 · materiał dowodowy niekompletny
[ POKAŻ LUKI ]
```

Przykład po zmianie parametrów:

```text
⚠ PARAMETRY ZOSTAŁY ZMIENIONE
Odśwież podgląd przed analizą lub eksportem.
```

Pasek powinien prezentować co najmniej:

- `EvidenceComplete`;
- liczbę i łączny czas nierozliczonych luk;
- `CoverageMatchesRange`;
- liczbę nierozstrzygniętych alokacji odpoczynku, jeżeli występują;
- liczbę naruszeń;
- liczbę otwartych zobowiązań rekompensacyjnych.

Nierozliczone luki i nierozstrzygnięte alokacje nie blokują automatycznie eksportu, jeżeli obecny kontrakt raportowy na to pozwala. Muszą jednak być jawnie pokazane w UI i w odpowiednich eksportach.

## 3.4. Kafle podsumowania

Zatwierdzony zestaw:

- `JAZDA`;
- `PRACA`;
- `GOTOWOŚĆ`;
- `ODPOCZYNEK`;
- `OTWARTY DŁUG`;
- `NARUSZENIA`.

Zasady:

- wartości czasu pozostają w formacie `HH:MM`, również powyżej 24 godzin;
- `OTWARTY DŁUG` jest sumą otwartych zobowiązań bieżącego raportu zgodnie z istniejącym DTO;
- brak wartości jest pokazywany jako `—`, nie jako pusty binding;
- przekroczenia i naruszenia nie są przycinane do limitu;
- kafle są tylko prezentacją `ReportDto`, bez lokalnych obliczeń domenowych w ViewModelu.

## 3.5. Zakładki podglądu

Zatwierdzone zakładki:

1. `PODSUMOWANIE`;
2. `AKTYWNOŚCI`;
3. `NARUSZENIA`;
4. `REKOMPENSATY`;
5. `KOMPLETNOŚĆ`.

Zakładki `NARUSZENIA` i `REKOMPENSATY` pokazują badge z liczbą pozycji, jeżeli liczba jest większa od zera.

### Podsumowanie

Pokazuje:

- wybranego kierowcę i kartę;
- zakres;
- sumy aktywności;
- liczbę rekordów lub bloków;
- liczbę naruszeń;
- stan kompletności;
- otwarte, zaległe i zamknięte zobowiązania;
- informację o czasie wygenerowania podglądu.

### Aktywności

Domyślnie prezentowane są czytelne bloki, a nie surowe minuty:

| Od | Do | Czas | Aktywność | Uwagi |
|---|---|---:|---|---|
| D124 23:00 | D124 23:02 | 00:02 | Dyspozycyjność | — |
| D124 23:02 | D124 23:11 | 00:09 | Jazda | — |

Zasady:

- nie powtarzać nazwy karty w każdym wierszu, ponieważ raport dotyczy jednej wybranej karty;
- nazwy `Driving`, `OtherWork`, `Availability`, `BreakOrRest` mapować na tekst użytkowy;
- tabela ma korzystać z wirtualizacji i nie może blokować UI przy dużej historii;
- domyślny widok nie pokazuje kolumn `Źródło` i `Warunek`.

Przełącznik:

```text
[ ] Pokaż dane techniczne
```

Po włączeniu może dodać:

- źródło (`Telemetry`, `ManualEntry`, `Reconstructed`, `Mixed`);
- warunek specjalny;
- identyfikator sesji albo inne pole diagnostyczne tylko wtedy, gdy jest już dostępne w DTO i ma rzeczywistą wartość dla testera.

Włączenie danych technicznych nie może zmieniać eksportów ani przeliczać raportu.

### Naruszenia

Minimalne kolumny:

- czas lub zakres;
- reguła / artykuł;
- nazwa naruszenia;
- wartość rzeczywista;
- limit lub wymaganie;
- karta.

Jeżeli obecne DTO nie zawiera części tych pól, ekran pokazuje wyłącznie dane już dostępne. Ten mini-projekt nie rozszerza katalogu naruszeń ani logiki `RuleEngine`.

### Rekompensaty

Widok korzysta z istniejącego pełnego kontraktu zobowiązań i pokazuje co najmniej:

- status;
- pełny i pozostały dług;
- termin `DueAtExclusive` w formacie czasu gry;
- źródłowy odpoczynek;
- blok spłacający i `SettledAt`, jeżeli istnieją;
- skrócone identyfikatory z możliwością skopiowania pełnej wartości.

Widok nie może tworzyć własnej projekcji rekompensat. Ma prezentować te same zobowiązania, które trafiają do PDF, CSV i JSON.

### Kompletność

Widok pokazuje:

- zakres raportu w minutach;
- sumę minut aktywności;
- sumę minut luk;
- wynik bilansu zakresu;
- liczbę i czas nierozliczonych luk;
- `EvidenceComplete`;
- nierozstrzygnięte decyzje alokacji odpoczynku;
- akcję `POKAŻ LUKI`, która przechodzi do Historii i zachowuje parametry raportu.

## 3.6. Eksport

Eksport PDF jest akcją główną:

```text
[ EKSPORTUJ PDF ]
```

Pozostałe eksporty znajdują się w menu:

```text
[ WIĘCEJ EKSPORTÓW ▼ ]

VTC JSON
CSV zobowiązań
Surowy CSV aktywności
```

Zasady:

- każdy eksport korzysta z aktualnie widocznych parametrów raportu;
- przed eksportem raport jest przeliczany zgodnie z dotychczasowym kontraktem;
- wynik tego przeliczenia jednocześnie aktualizuje podgląd;
- plik jest generowany z dokładnie tego samego `ReportDto`, które po przeliczeniu trafia na ekran;
- przy błędzie przeliczenia nie wolno eksportować starego podglądu jako nowego raportu;
- anulowanie okna zapisu nie jest błędem i nie zmienia podglądu;
- techniczne JSON/CSV zachowują `InvariantCulture` i dotychczasowe nazwy pól;
- CSV zobowiązań nadal zapisuje jeden rekord na zobowiązanie;
- surowy CSV pozostaje minutowy;
- PDF nadal używa zwiniętych bloków aktywności.

Operacje `.tacho`, import `.tacho` i raport diagnostyczny pozostają w dotychczasowych miejscach. Ich przenoszenie do nowego menu nie należy do tego wariantu, chyba że zostanie zatwierdzone osobno.

---

## 4. Zakres techniczny

## 4.1. W zakresie

- przebudowa sekcji Raporty w `MainWindow.xaml`;
- wydzielenie stanu roboczego raportów z głównego ViewModelu;
- jednoznaczny model parametrów raportu;
- szybkie zakresy `game_time`;
- walidacja własnego zakresu;
- jawne stany podglądu;
- pasek kompletności;
- kafle podsumowania;
- zakładki podglądu;
- użytkowe mapowanie nazw aktywności, źródeł i statusów;
- ukrywanie danych technicznych;
- bezpieczny wspólny przepływ podgląd → eksport;
- testy Desktop i ewentualne testy Application/Reports wymagane przez nową fasadę;
- aktualizacja checklisty raportów i dokumentacji zmian lokalnych.

## 4.2. Poza zakresem

- zmiana zasad `RuleEngine`;
- zmiana historii kanonicznej;
- zmiana progów prawnych;
- nowa migracja SQLite;
- zmiana protokołu pluginu v3;
- przebudowa wyglądu PDF;
- zmiana nazw pól JSON;
- lokalizowanie technicznego CSV;
- zapisane szablony raportów;
- historia wygenerowanych plików;
- raporty cykliczne;
- porównywanie kierowców lub zakresów;
- wykresy i dashboard analityczny;
- dynamiczna lokalizacja PL/EN;
- przenoszenie importu/eksportu `.tacho`;
- Planer podróży.

## 4.3. Niezmienniki

1. Historia minutowa pozostaje jedynym źródłem prawdy.
2. UI nie wylicza reguł tachografu ani zobowiązań.
3. Raport działa na `game_time`, nie na zegarze Windows.
4. Numer dnia pozostaje zgodny z:

   ```text
   displayedDay = floor(GameMinute / 1440) + 1
   ```

5. JSON, CSV, `.tacho`, SQLite i protokół telemetrii pozostają kompatybilne.
6. Ostrzeżenie o niekompletności nie może ukrywać ani poprawiać danych.
7. Eksport z nieaktualnego podglądu najpierw przelicza raport.
8. Brak danych jest stanem UI, nie wartością zastępczą w historii.

---

## 5. Docelowy przepływ danych

```text
Użytkownik wybiera kartę i zakres
  → ReportQueryDraft
    → walidacja parametrów
      → ReportService / istniejąca warstwa Application
        → ReportDto + kompletność + zobowiązania
          → ReportPreviewSnapshot
            → kafle i zakładki
            → eksport PDF / JSON / CSV z tego samego wyniku
```

### 5.1. Proponowany model stanu Desktop

Nazwy są propozycją i mogą zostać dopasowane do istniejącej konwencji projektu po audycie kodu.

```csharp
public enum ReportRangePreset
{
    CurrentRegulatoryWeek,
    Last24GameHours,
    AllHistory,
    Custom
}

public enum ReportPreviewStatus
{
    NoSelection,
    InvalidParameters,
    Loading,
    Current,
    CurrentIncomplete,
    OutOfDate,
    Error
}

public enum ReportWorkspaceTab
{
    Summary,
    Activities,
    Violations,
    Compensations,
    Completeness
}

public sealed record ReportQueryDraft(
    Guid DriverCardId,
    long FromGameMinuteInclusive,
    long ToGameMinuteExclusive,
    ReportRangePreset RangePreset);

public sealed record ReportPreviewIdentity(
    Guid DriverCardId,
    long FromGameMinuteInclusive,
    long ToGameMinuteExclusive,
    long? HistoryHighWaterMark,
    DateTimeOffset GeneratedAtUtc);

public sealed record ReportPreviewSnapshot(
    ReportPreviewIdentity Identity,
    ReportDto Report);
```

`HistoryHighWaterMark` należy dodać tylko wtedy, gdy można go uzyskać bez łamania istniejącej architektury. Minimalny wariant wykrywa nieaktualność na podstawie zmiany parametrów; eksport i tak zawsze wykonuje ponowne przeliczenie.

### 5.2. Rekomendowane wydzielenie ViewModelu

Aby nie powiększać dalej `MainViewModel`, rekomendowany jest osobny:

```text
ReportsWorkspaceViewModel
```

Odpowiedzialności:

- parametry raportu;
- zakresy presetów;
- walidacja;
- stan ładowania i błędu;
- bieżący `ReportPreviewSnapshot`;
- wybór zakładki;
- widoczność danych technicznych;
- komendy odświeżania i eksportu;
- komunikaty sukcesu lub błędu.

`MainViewModel` zachowuje wyłącznie nawigację i udostępnia instancję workspace. Jeżeli obecna architektura nie wspiera łatwo zagnieżdżonych ViewModeli, dopuszczalny jest etap przejściowy, ale nowe właściwości raportowe powinny zostać logicznie zgrupowane i pokryte testami.

---

## 6. Kontrakty funkcjonalne

## 6.1. Kierowca i karta

- lista zawiera tylko profile z kartą możliwą do raportowania;
- pozycja pokazuje kierowcę i kartę razem;
- zmiana wyboru zachowuje wybrany preset zakresu, ale przelicza jego granice dla nowej karty;
- gdy karta nie ma historii, UI pokazuje pusty stan i blokuje eksport;
- nawigacja do innej zakładki aplikacji i powrót nie gubi bieżącego wyboru w tej samej sesji.

## 6.2. Zakres

### Bieżący tydzień

- wyznaczany z istniejącej konfiguracji tygodnia regulacyjnego;
- kończy się na bieżącym znanym `game_time` albo końcu dostępnej historii;
- początek jest przycinany do najstarszej dostępnej minuty karty.

### Ostatnie 24 h

- zakres 1440 minut zakończony na bieżącym znanym `game_time`;
- jeżeli karta ma krótszą historię, zakres jest przycinany do jej początku.

### Cała historia

- od pierwszej do ostatniej dostępnej minuty kanonicznej historii karty;
- brak historii daje pusty stan, nie zakres `0–0` przedstawiony jako poprawny.

### Własny zakres

- `Do > Od`;
- obie wartości należą do obsługiwanego przedziału `long`;
- minuty w polu czasu należą do `00–59`;
- godzina należy do `00–23`;
- zakres nie może być pusty;
- zakres poza historią jest dozwolony tylko wtedy, gdy obecny `ReportService` poprawnie go bilansuje; w przeciwnym razie UI przycina go lub pokazuje walidację zgodnie z audytem Etapu 0;
- walidacja jest widoczna przed uruchomieniem raportu.

## 6.3. Stany podglądu

| Stan | Zachowanie UI | Eksport |
|---|---|---|
| `NoSelection` | prośba o wybór karty | zablokowany |
| `InvalidParameters` | komunikat walidacji przy polach | zablokowany |
| `Loading` | spinner/progress, blokada ponownego requestu | zablokowany |
| `Current` | zielony pasek, aktualne dane | dostępny |
| `CurrentIncomplete` | bursztynowy pasek i szczegóły | dostępny zgodnie z kontraktem |
| `OutOfDate` | stary podgląd przygaszony, komunikat o zmianie parametrów | eksport najpierw odświeża |
| `Error` | błąd z możliwością ponowienia | zablokowany do poprawnego przeliczenia |

## 6.4. Atomowość podglądu i eksportu

Najważniejszy niezmiennik wariantu B:

> Plik eksportowy i podgląd po zakończeniu eksportu muszą pochodzić z tego samego wyniku raportowego dla tych samych parametrów.

Rekomendowana sekwencja:

1. skopiuj bieżący `ReportQueryDraft` do lokalnego requestu;
2. przelicz raport;
3. utwórz `ReportPreviewSnapshot`;
4. podmień podgląd;
5. przekaż dokładnie ten sam `ReportDto` do wybranego eksportera;
6. pokaż ścieżkę pliku i status powodzenia.

Jeżeli obecny `ExportService` sam pobiera dane drugi raz, Etap 0 musi ustalić, czy można dodać bezpieczne przeciążenie przyjmujące gotowy `ReportDto`. Nie wolno tworzyć równoległego drugiego sposobu liczenia raportu.

## 6.5. Zakres CSV zobowiązań

Wariant B nie zmienia semantyki filtrowania zobowiązań. CSV ma eksportować dokładnie kolekcję `CompensationObligations` obecną w wygenerowanym `ReportDto`.

Etap 0 musi udokumentować aktualne zachowanie:

- zobowiązania związane wyłącznie ze źródłem w badanym zakresie;
- zobowiązania aktywne w badanym zakresie;
- albo wszystkie zobowiązania wybranej karty.

Jeżeli obecny wynik nie odpowiada oczekiwaniom produktu, wymaga to osobnej decyzji domenowo-raportowej. Nie należy zmieniać tego „przy okazji” przebudowy XAML.

---

## 7. Etapy wdrożenia

## Etap 0 — audyt istniejącego przepływu

**Cel:** ustalić faktyczny kontrakt bez zgadywania na podstawie UI.

Zadania:

- [ ] odnaleźć wszystkie komendy raportowe w `MainViewModel`;
- [ ] opisać ścieżkę `wybór → ReportService → ReportDto → eksport`;
- [ ] potwierdzić semantykę pustego zakresu;
- [ ] potwierdzić granice początku i końca raportu;
- [ ] sprawdzić, czy PDF/JSON/CSV wykonują osobne zapytania;
- [ ] ustalić, czy można eksportować gotowy `ReportDto`;
- [ ] potwierdzić, skąd pochodzą sumy aktywności;
- [ ] potwierdzić dostępne dane kompletności;
- [ ] potwierdzić kontrakt CSV zobowiązań;
- [ ] sprawdzić, czy tabela raportu korzysta z minut czy agregowanych bloków;
- [ ] sprawdzić wirtualizację obecnego `DataGrid`;
- [ ] zapisać wynik audytu w komentarzu do zadania lub krótkim pliku technicznym.

**Gate Etapu 0:** znany jest jeden przepływ generowania i jeden przepływ eksportu; nie ma otwartej niejasności mogącej zmienić dane.

---

## Etap 1 — model stanu i testy przed XAML

**Cel:** zbudować logikę workspace bez przebudowy wyglądu.

Zadania:

- [ ] dodać `ReportRangePreset`;
- [ ] dodać model parametrów raportu;
- [ ] dodać formatter/parser dnia i godziny, wykorzystując istniejący `GameClockFormatter` tam, gdzie jest to możliwe;
- [ ] dodać `ReportPreviewStatus`;
- [ ] dodać `ReportPreviewSnapshot`;
- [ ] dodać `ReportsWorkspaceViewModel` albo równoważnie wydzielony moduł;
- [ ] zaimplementować komendę odświeżenia;
- [ ] zaimplementować oznaczanie podglądu jako nieaktualnego;
- [ ] zaimplementować zabezpieczenie przed równoległym odświeżaniem;
- [ ] dodać testy presetów, walidacji i przejść stanów.

**Gate Etapu 1:** nowe testy Desktop są zielone, obecny ekran nadal działa, pełny build pozostaje 0/0.

---

## Etap 2 — konfiguracja i zakresy w XAML

**Cel:** wdrożyć górny panel makiety bez ruszania tabeli i eksportów.

Zadania:

- [ ] dodać pasek czterech etapów;
- [ ] zastąpić niejednoznaczny selektor formatem `Kierowca — karta`;
- [ ] dodać przyciski presetów;
- [ ] dodać kontrolki dnia i godziny dla własnego zakresu;
- [ ] dodać opis długości zakresu;
- [ ] dodać `ODŚWIEŻ PODGLĄD`;
- [ ] dodać komunikaty walidacyjne przy polach;
- [ ] zachować wybór po przejściu do innej zakładki i powrocie;
- [ ] sprawdzić tab order i obsługę klawiatury.

**Gate Etapu 2:** zakresy działają na danych rzeczywistych z IDE; żaden nie tworzy błędnego dnia `+1` ani odwróconych granic.

---

## Etap 3 — kontrola danych, kafle i stany

**Cel:** użytkownik przed tabelą rozumie jakość raportu.

Zadania:

- [ ] dodać pasek `PODGLĄD AKTUALNY / NIEKOMPLETNY / NIEAKTUALNY / BŁĄD`;
- [ ] dodać akcję `POKAŻ LUKI`;
- [ ] dodać sześć kafli podsumowania;
- [ ] podłączyć otwarty dług i naruszenia wyłącznie z DTO;
- [ ] pokazać liczbę nierozstrzygniętych alokacji;
- [ ] dodać pusty stan dla braku historii;
- [ ] dodać stan błędu z ponowieniem;
- [ ] upewnić się, że ostrzeżenie nie blokuje dozwolonego eksportu.

**Gate Etapu 3:** wartości na kaflach są zgodne z dotychczasowym raportem i eksportami dla obu kart referencyjnych.

---

## Etap 4 — zakładki i uproszczona tabela

**Cel:** zastąpić techniczną kopię Historii czytelnym podglądem raportu.

Zadania:

- [ ] dodać pięć zakładek;
- [ ] dodać badge naruszeń i rekompensat;
- [ ] zbudować tabelę bloków aktywności;
- [ ] dodać kolumnę czasu trwania;
- [ ] zmapować aktywności na tekst użytkowy;
- [ ] dodać przełącznik danych technicznych;
- [ ] zachować wirtualizację i przewijanie wyłącznie obszaru tabeli;
- [ ] dodać widok naruszeń;
- [ ] dodać widok rekompensat bez duplikacji obliczeń;
- [ ] dodać widok kompletności i bilansu;
- [ ] zachować wybraną zakładkę przy odświeżeniu.

**Gate Etapu 4:** duży raport jest responsywny, a widok techniczny i użytkowy pokazują ten sam zestaw bloków.

---

## Etap 5 — wspólna ścieżka eksportu

**Cel:** usunąć niejednoznaczność przycisków i zagwarantować zgodność podglądu z plikiem.

Zadania:

- [ ] wdrożyć główny przycisk `EKSPORTUJ PDF`;
- [ ] wdrożyć menu `WIĘCEJ EKSPORTÓW`;
- [ ] podłączyć VTC JSON;
- [ ] podłączyć CSV zobowiązań;
- [ ] podłączyć surowy CSV aktywności;
- [ ] przed każdym eksportem wykonać jedno przeliczenie;
- [ ] zaktualizować podgląd tym samym wynikiem;
- [ ] obsłużyć anulowanie dialogu zapisu;
- [ ] obsłużyć błąd zapisu bez utraty podglądu;
- [ ] pokazać jednoznaczny komunikat z typem i ścieżką pliku;
- [ ] potwierdzić brak zmian w kontraktach technicznych.

**Gate Etapu 5:** dla jednego requestu PDF, JSON i CSV są zgodne z widocznym podglądem oraz zachowują dotychczasowe dane referencyjne.

---

## Etap 6 — dopracowanie UI i przygotowanie do lokalizacji

**Cel:** zakończyć ekran bez uruchamiania pełnego mini-projektu PL/EN.

Zadania:

- [ ] poprawić szerokości, marginesy i wyrównania zgodnie z makietą;
- [ ] użyć `Auto`, `MinWidth`, `TextTrimming` i tooltipów zamiast kruchych stałych szerokości;
- [ ] sprawdzić 100%, 125% i 150% skalowania Windows;
- [ ] sprawdzić minimalną wspieraną wielkość okna;
- [ ] zapewnić widoczny focus klawiatury;
- [ ] nie opierać statusów tylko na kolorze;
- [ ] nadać nowym tekstom semantyczne nazwy, aby nie utrudnić przyszłej migracji do `.resx`;
- [ ] nie wdrażać teraz przełączania PL/EN.

**Gate Etapu 6:** brak obcięć, nakładania kontrolek i błędów bindingów w logu.

---

## Etap 7 — pełna regresja i dokumentacja

**Cel:** przygotować zmianę do smoke testu beta.12.

Zadania:

- [ ] uruchomić pełny build Release;
- [ ] uruchomić pełny pakiet testów;
- [ ] wykonać pełną checklistę XAML;
- [ ] wykonać scenariusze raportowe z luką i bez luki;
- [ ] wykonać eksport PDF, JSON i obu CSV;
- [ ] porównać liczby i identyfikatory;
- [ ] zamknąć i ponownie uruchomić aplikację;
- [ ] sprawdzić brak nowych błędów bindingów;
- [ ] zaktualizować `BETA_TEST_PLAN.md`;
- [ ] zaktualizować `README.md`, `RELEASE_NOTES.md`, `KNOWN_ISSUES.md` i handoff dopiero po potwierdzeniu rzeczywiście wdrożonego zakresu;
- [ ] nie tworzyć paczki beta bez osobnej decyzji.

**Gate Etapu 7:** zmiana jest gotowa do wejścia w smoke test beta.12.

---

## 8. Plan testów automatycznych

## 8.1. Presety zakresów

| ID | Scenariusz | Oczekiwany wynik |
|---|---|---|
| RPT-RNG-01 | Bieżący tydzień przy standardowym offsetcie | poprawny początek tygodnia i koniec na bieżącej minucie |
| RPT-RNG-02 | Bieżący tydzień przy innym `WeekEpochOffsetDays` | granica zgodna z konfiguracją |
| RPT-RNG-03 | Ostatnie 24 h przy historii dłuższej niż 1440 min | dokładnie 1440 min |
| RPT-RNG-04 | Ostatnie 24 h przy krótszej historii | zakres przycięty do początku historii |
| RPT-RNG-05 | Cała historia | pierwszy i ostatni dostępny punkt karty |
| RPT-RNG-06 | Brak historii | pusty stan i zablokowany eksport |
| RPT-RNG-07 | Własny zakres przez północ | poprawne minuty i wyświetlane dni |
| RPT-RNG-08 | `Do == Od` | walidacja, brak requestu |
| RPT-RNG-09 | `Do < Od` | walidacja, brak requestu |
| RPT-RNG-10 | minuta `59` i następna godzina | brak błędu o jedną minutę |

## 8.2. Stany podglądu

| ID | Scenariusz | Oczekiwany wynik |
|---|---|---|
| RPT-STATE-01 | poprawny raport kompletny | `Current` |
| RPT-STATE-02 | raport z nierozliczoną luką | `CurrentIncomplete` |
| RPT-STATE-03 | zmiana parametru po wygenerowaniu | `OutOfDate` |
| RPT-STATE-04 | błąd serwisu | `Error`, brak utraty ostatniego snapshotu |
| RPT-STATE-05 | ponowienie po błędzie | nowy poprawny snapshot |
| RPT-STATE-06 | dwa szybkie odświeżenia | jeden aktywny request lub bezpieczne anulowanie starszego |
| RPT-STATE-07 | zmiana zakładki podglądu | brak przeliczenia raportu |
| RPT-STATE-08 | włączenie danych technicznych | brak przeliczenia raportu |

## 8.3. Eksport

| ID | Scenariusz | Oczekiwany wynik |
|---|---|---|
| RPT-EXP-01 | PDF z aktualnego podglądu | plik i podgląd z tych samych parametrów |
| RPT-EXP-02 | PDF po zmianie parametrów | najpierw odświeżenie, potem eksport |
| RPT-EXP-03 | VTC JSON | niezmienione nazwy pól i kompletność |
| RPT-EXP-04 | CSV zobowiązań | jeden rekord na zobowiązanie |
| RPT-EXP-05 | surowy CSV | dane minutowe, dotychczasowy kontrakt |
| RPT-EXP-06 | anulowanie okna zapisu | brak błędu i brak pliku |
| RPT-EXP-07 | błąd zapisu | czytelny błąd, podgląd pozostaje |
| RPT-EXP-08 | błąd ponownego przeliczenia | stary snapshot nie jest eksportowany pod nowymi parametrami |
| RPT-EXP-09 | raport niekompletny | ostrzeżenie widoczne, eksport dozwolony |
| RPT-EXP-10 | pending allocation | podgląd i eksport zgodnie oznaczone jako niekompletne |

## 8.4. Zgodność danych

Minimalne asercje:

- sumy kafli = sumy w `ReportDto`;
- suma czasu bloków użytkowych = suma czasu tabeli technicznej;
- aktywności + nierozliczone luki = pokrycie zakresu zgodnie z obecnym bilansem;
- liczba naruszeń na kaflu = liczba pozycji zakładki;
- liczba zobowiązań w badge = liczba pozycji zakładki;
- otwarty dług = suma właściwych `RemainingMinutes`;
- `EvidenceComplete` jest identyczne w podglądzie i JSON;
- PDF, JSON i CSV zachowują stabilne `ObligationId`;
- język i format UI nie zmieniają danych maszynowych.

---

## 9. Ręczna regresja z IDE

## 9.1. Scenariusze danych

Wykonać co najmniej:

1. karta z historią i bez luk;
2. karta z nierozliczoną luką;
3. karta z rozliczoną luką;
4. karta z naruszeniem;
5. karta bez naruszeń;
6. karta z otwartym zobowiązaniem;
7. karta z `PendingRestAllocation`;
8. karta bez historii;
9. zakres obejmujący dane hot i warm;
10. zakres po cofnięciu czasu, oparty na projekcji kanonicznej.

## 9.2. Scenariusz podstawowy

1. Otwórz `Raporty`.
2. Wybierz `Arkadiusz — karta Staniek`.
3. Wybierz `CAŁA HISTORIA`.
4. Potwierdź jawne granice zakresu.
5. Odśwież podgląd.
6. Porównaj kafle z dotychczasowym raportem.
7. Otwórz wszystkie pięć zakładek.
8. Włącz i wyłącz dane techniczne.
9. Wyeksportuj PDF.
10. Wyeksportuj VTC JSON.
11. Wyeksportuj CSV zobowiązań.
12. Wyeksportuj surowy CSV.
13. Porównaj identyfikatory i sumy.

## 9.3. Scenariusz nieaktualnego podglądu

1. Wygeneruj raport.
2. Zmień godzinę końcową.
3. Potwierdź stan `PARAMETRY ZOSTAŁY ZMIENIONE`.
4. Kliknij eksport PDF bez ręcznego odświeżenia.
5. Oczekiwane: ekran najpierw przelicza raport, aktualizuje podgląd, a dopiero potem zapisuje PDF.

## 9.4. Scenariusz luki

1. Wybierz zakres zawierający nierozliczoną lukę.
2. Potwierdź liczbę i czas luk.
3. Otwórz `KOMPLETNOŚĆ`.
4. Kliknij `POKAŻ LUKI`.
5. Wróć do Raportów.
6. Potwierdź zachowanie parametrów i wybranej zakładki.
7. Wyeksportuj PDF i JSON.
8. Potwierdź, że ostrzeżenie nie zniknęło i nie zablokowało eksportu.

## 9.5. Kontrola wizualna

- rozdzielczość 1366×768;
- rozdzielczość 1920×1080;
- skalowanie 100%;
- skalowanie 125%;
- skalowanie 150%;
- maksymalizacja i przywrócenie okna;
- brak przewijania całego ekranu przy typowym raporcie;
- przewijanie ograniczone do tabeli;
- stale dostępne przyciski eksportu;
- brak uciętych badge, kafli i menu eksportów;
- czytelny focus klawiatury;
- brak błędów bindingów w logu.

---

## 10. Przewidywane pliki

Poniższa lista jest planem, nie potwierdzeniem faktycznego diffu. Etap 0 ma ją zweryfikować.

| Plik / projekt | Planowana odpowiedzialność |
|---|---|
| `src/ETS2Tachograph.Desktop/Views/MainWindow.xaml` | układ wariantu B |
| `src/ETS2Tachograph.Desktop/ViewModels/MainViewModel.cs` | nawigacja i podłączenie workspace; ograniczenie bezpośredniej logiki raportów |
| `src/ETS2Tachograph.Desktop/ViewModels/ReportsWorkspaceViewModel.cs` | parametry, statusy, zakładki i komendy |
| `src/ETS2Tachograph.Desktop/ViewModels/ReportRangePreset.cs` | typy zakresów |
| `src/ETS2Tachograph.Desktop/ViewModels/ReportPreviewSnapshot.cs` | tożsamość i stan podglądu |
| `src/ETS2Tachograph.Application/Services/ReportService.cs` | tylko jeżeli potrzebna jest bezpieczna fasada generowania jednego DTO |
| `src/ETS2Tachograph.Application/Dtos/ReportDto.cs` | bez zmiany kontraktu eksportowego; ewentualnie oddzielne metadane UI poza JSON |
| `src/ETS2Tachograph.Reports/PdfReportExporter.cs` | bez zmian wizualnych; ewentualne przeciążenie przyjmujące gotowy DTO |
| `src/ETS2Tachograph.Reports/ReportPresentationBuilder.cs` | zachowanie bloków i mapowania danych PDF |
| `tests/ETS2Tachograph.Desktop.Tests/ReportsWorkspaceViewModelTests.cs` | presety, walidacja, stany i eksport |
| `tests/ETS2Tachograph.Application.Tests/...` | tylko gdy zmieni się fasada Application |
| `tests/ETS2Tachograph.Reports.Tests/...` | regresja niezmienionych eksportów |
| `BETA_TEST_PLAN.md` | rozszerzona checklista wariantu B |
| `RAPORT_PRAC_UI_*.md` | dokumentacja rzeczywiście wykonanej pracy |

---

## 11. Proponowane commity

1. `test(reports-ui): dodaj regresje zakresów i stanów podglądu`
2. `refactor(reports-ui): wydziel workspace raportów z MainViewModel`
3. `feat(reports-ui): dodaj konfigurację i presety game_time`
4. `feat(reports-ui): dodaj kontrolę kompletności i kafle podsumowania`
5. `feat(reports-ui): dodaj zakładki i czytelne bloki aktywności`
6. `feat(reports-ui): ujednolić podgląd i eksport z jednego DTO`
7. `style(reports-ui): dopracuj układ wariantu B i dostępność`
8. `test(reports-ui): rozszerz regresję eksportów i XAML`
9. `docs(reports-ui): opisz wdrożony wariant B i gate beta.12`

Po każdym commicie:

- testy właściwego projektu;
- build rozwiązania;
- `git diff --check`;
- rzeczywisty start aplikacji po zmianie XAML;
- brak nowych ostrzeżeń i błędów bindingów.

---

## 12. Ryzyka i działania ograniczające

| Ryzyko | Wpływ | Działanie ograniczające |
|---|---|---|
| Dalsze rozrastanie `MainViewModel` | trudna konserwacja i regresje | wydzielić `ReportsWorkspaceViewModel` |
| Podgląd i plik mają różne dane | utrata zaufania do raportu | jeden request i jeden `ReportDto` na operację eksportu |
| Off-by-one dnia lub końca zakresu | błędne minuty i bilans | jawna semantyka granic, testy półotwartych zakresów |
| Zmiana kontraktu JSON/CSV | uszkodzenie integracji VTC | testy kontraktowe i `InvariantCulture` |
| Duża tabela blokuje UI | nieużyteczny ekran dla długiej historii | agregowane bloki, wirtualizacja, brak surowych minut domyślnie |
| Zbyt szeroki XAML | obcięcia na 1366×768 i 125% | `Auto`, `MinWidth`, kontrola skalowania, scroll tylko tabeli |
| Ostrzeżenie o lukach zacznie blokować eksport | regresja kontraktu beta.5+ | osobne testy eksportu niekompletnego raportu |
| Próba naprawy semantyki CSV „przy okazji” | scope creep i zmiana danych | audyt i osobna decyzja, bez zmian w tym mini-projekcie |
| Nowe polskie literały utrudnią lokalizację | dodatkowy dług przed PL/EN | semantyczne grupowanie tekstów, bez uruchamiania pełnej lokalizacji |
| Równoległa telemetria zmienia wynik podczas pracy | podgląd szybko się starzeje | status nieaktualności i ponowne przeliczenie przed eksportem |
| Błąd zapisu pliku usuwa poprawny podgląd | utrata pracy użytkownika | rozdzielić wynik raportu od rezultatu zapisu na dysk |

---

## 13. Kryteria akceptacji

Wariant B jest ukończony, gdy wszystkie poniższe warunki są spełnione:

1. Ekran pokazuje kierowcę i kartę w jednej pozycji.
2. Nie istnieje niejawny pusty zakres.
3. Działają cztery zatwierdzone presety.
4. Własny zakres obsługuje dzień i godzinę `game_time` bez ręcznego wpisywania pełnego tekstu.
5. Użytkownik widzi długość wybranego zakresu.
6. Zmiana parametrów oznacza podgląd jako nieaktualny.
7. Podgląd ma jawny stan kompletności.
8. `POKAŻ LUKI` prowadzi do właściwej sekcji Historii.
9. Sześć kafli pokazuje wartości zgodne z DTO.
10. Dostępnych jest pięć zakładek.
11. Aktywności są prezentowane jako czytelne bloki.
12. Dane techniczne są domyślnie ukryte.
13. Widok naruszeń zgadza się z licznikiem.
14. Widok rekompensat zgadza się z PDF, CSV i JSON.
15. Widok kompletności pokazuje bilans i `EvidenceComplete`.
16. PDF jest głównym eksportem.
17. VTC JSON, CSV zobowiązań i surowy CSV są w menu dodatkowym.
18. Eksport nie używa starego podglądu pod nowymi parametrami.
19. Podgląd po eksporcie odpowiada wygenerowanemu plikowi.
20. Nierozliczone luki nie blokują dozwolonych eksportów.
21. JSON, CSV, `.tacho`, SQLite i plugin v3 nie zmieniają kontraktu.
22. Nie powstaje nowa migracja bazy.
23. `RuleEngine` nie jest modyfikowany.
24. Pełny pakiet testów jest zielony.
25. Build Release kończy się `0 błędów / 0 ostrzeżeń`.
26. Aplikacja rzeczywiście uruchamia się po zmianach XAML.
27. Pełna checklista UI jest zaliczona.
28. Eksporty zostały otwarte i zweryfikowane, nie tylko utworzone.
29. Restart nie gubi podstawowego stanu nawigacji i nie uszkadza raportów.
30. Smoke test beta.12 potwierdza przepływ z aktywną telemetrią.

---

## 14. Blokery i decyzje przed kodowaniem

Brak blokera uniemożliwiającego rozpoczęcie Etapu 0.

Przed właściwą implementacją muszą zostać potwierdzone trzy fakty w kodzie:

1. **Semantyka końca zakresu** — czy obecny serwis używa końca włącznie, czy wyłącznie.
2. **Źródło danych tabeli** — surowe minuty czy gotowe bloki raportowe.
3. **Zakres CSV zobowiązań** — dokładnie jakie zobowiązania znajdują się dziś w `ReportDto` dla wybranego okresu.

Wynik audytu ma zostać zachowany bez „poprawiania” kontraktu w ramach samej przebudowy UI.

---

## 15. Definicja ukończenia

> Wariant B ekranu Raporty jest ukończony, gdy użytkownik może w jednym spójnym przepływie wybrać kartę i jawny zakres `game_time`, ocenić kompletność danych, przejrzeć te same sumy, aktywności, naruszenia i zobowiązania, które znajdują się w raportach, a następnie wyeksportować PDF, VTC JSON, CSV zobowiązań lub surowy CSV bez ryzyka użycia nieaktualnych parametrów — przy zachowaniu niezmienionej historii kanonicznej, `RuleEngine`, SQLite, protokołu v3 i kontraktów maszynowych.
