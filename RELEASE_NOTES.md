# ETS2 EU Digital Tachograph 0.1.0-beta.12 — kandydat M6

Ostatnia beta przed pierwszą szeroką publikacją. Wydanie nie jest jeszcze
opublikowane; niezmienny ZIP z GO M6 oczekuje na końcowy smoke M7.

## Najważniejsze zmiany

- pełny interfejs `pl-PL` i `en-GB`, wybór języka w Ustawieniach stosowany
  po restarcie oraz lokalizowane raporty PDF;
- Planer tras z obsługą dwóch kierowców, zdarzeń, terminów w czasie gry,
  ostrzeżeń i zapamiętywaniem formularza;
- rozszerzone Raporty i statystyki, eksporty PDF/CSV/JSON oraz czytelne
  zobowiązania rekompensaty;
- wariant B wpisu manualnego: plan całej luki, edycja segmentów i blokada
  niepełnego zapisu;
- katalog 249 krajów ISO 3166-1 z nazwami PL/EN i kodami tachografowymi;
- finalizacja Dashboardu, wirtualnego urządzenia, obu nakładek i stanów błędów
  przy zachowaniu UI freeze;
- poprawiony licznik pauzy 44/45 oraz prezentacja `ODP. TYG.`;
- szybszy start na istniejącej bazie i hotfix projekcji hot/warm po wczytaniu
  starszego zapisu gry;
- lokalizowane widoczne wartości czasu gry (`Dzień` / `Day`) w Historii,
  Rekompensatach i wpisie manualnym.

## Weryfikacja kandydata

- 570/570 testów automatycznych;
- build Release: 0 błędów, 0 ostrzeżeń;
- `FileVersion 0.1.12.0`;
- poprawność i wydajność checkpointu M5.2-P: GO;
- starty i restarty czystej oraz aktualnej bazy: PASS;
- pełne PL/EN: GO;
- ZIP M6: SHA-256
  `A2B8F949E100F8683225B7A0D5A76E5C7E3434AD95AEC9596006C4A5E41F5E78`;
- końcowy smoke M7 pozostaje do wykonania.

Nazwy krajów wykorzystują dane Unicode CLDR. Wymagane informacje licencyjne
znajdują się w `docs/THIRD_PARTY_NOTICES.md`.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.11.1

Wydanie naprawcze zastępujące wycofanego kandydata beta.11.

- zakończony blok 24 h+ otrzymuje legalne kandydatury sposobu rozliczenia;
  użytkownik wybiera `CandidateId`, bez ręcznego dzielenia minut;
- wariant dobowy z rekompensatą używa podstawy 9 h, a wariant tygodniowy całego
  bloku zachowuje konsekwencje skróconego odpoczynku tygodniowego;
- te same minuty nie mogą jednocześnie tworzyć podstawy odpoczynku i spłacać
  rekompensaty; spłata pozostaje pełna i en bloc;
- decyzje są trwałe, wersjonowane i audytowane w SQLite; zmiana decyzji zachowuje
  `Superseded`, a zmiana kanonicznego `RestBlockId` unieważnia stary wybór;
- wspólny skok czasu podwójnej obsady jest klasyfikowany raz dla pojazdu.
  Stabilne `BreakOrRest`, `OtherWork` i `Availability` są rekonstruowane
  symetrycznie, bez wymyślania Jazdy;
- dwie referencyjne luki Dnia 141 zostały poprawione na kopii bazy bez ręcznego
  SQL. Oryginalny ślad pozostał, a rekordy korekty mają źródło
  `AutomaticCrewReconstruction`;
- UI pokazuje warianty RuleEngine, a PDF, CSV i JSON pełny ślad alokacji.
  Nierozstrzygnięty wybór oznacza raport jako niekompletny.

## Weryfikacja

- 282/282 testy automatyczne;
- RuleEngine 62/62, Engine 69/69, Application 50/50, Reports 9/9,
  Infrastructure 51/51;
- build Release: 0 błędów, 0 ostrzeżeń;
- migracja i dwa restarty sprawdzone na kopii właściwej bazy;
- po restarcie dokładnie dwa audytowane rekordy rekonstrukcji i zero
  nierozliczonych luk referencyjnych;
- końcowy smoke terenowy z aktywną telemetrią wykonany 23 lipca 2026;
- wszystkie pozycje checklisty smoke testu zakończone wynikiem zielonym;
- decyzja wydaniowa: **GO** dla `0.1.0-beta.11.1`.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.11 — WYCOFANY KANDYDAT

Ta paczka nie przeszła do końcowego smoke testu z powodu błędnej alokacji
niejednoznacznego odpoczynku 24 h+ i fałszywych luk wspólnego skoku załogi.

Pełny model rekompensat skróconego odpoczynku tygodniowego.

- dług powstaje dopiero po zamknięciu kanonicznego skróconego odpoczynku
  tygodniowego i wynosi `45 h - długość odpoczynku`;
- zobowiązanie jest przypisane do tygodnia skrócenia, ma ścisły termin
  `DueAtExclusive` oraz status `OpenOnTime`, `Overdue`, `PaidOnTime` albo
  `PaidLate`;
- spłata następuje wyłącznie en bloc przez jeden kwalifikujący odpoczynek
  trwający co najmniej 9 godzin; okruchy z kilku odpoczynków nie są sumowane;
- kilka zobowiązań jest obsługiwanych deterministycznie według FIFO terminu i
  stabilnych tie-breakerów;
- pełny ślad obejmuje `ObligationId`, źródłowy odpoczynek, pierwotny i pozostały
  dług, blok oraz zakres spłaty i `SettledAt`;
- Dashboard i nakładki pokazują warstwowe podsumowanie, a zakładka
  `Rekompensaty` pełne zobowiązania obu kart;
- PDF zawiera tabelę zobowiązań, CSV zapisuje jeden rekord na zobowiązanie, a
  JSON pełne `CompensationObligations`; dotychczasowe podsumowanie pozostaje
  projekcją pochodną dla kompatybilności;
- stabilność całego kontraktu jest sprawdzana także po zamknięciu i ponownym
  otwarciu plikowej bazy SQLite.

## Dane referencyjne

- Staniek: `1253 min` (`20:53`) otwartego długu;
- Doboś: `1192 min` (`19:52`) otwartego długu;
- wcześniejsze wartości `18 min` i `353 min` wynikały z nielegalnego sumowania
  nadwyżek z wielu odpoczynków i nie są już używane.

## Zgodność

- protokół pluginu SCS bez zmian, nadal wersja 3;
- brak nowej migracji EF Core i brak wymogu czyszczenia danych użytkownika;
- stan regulacyjny pozostaje projekcją historii kanonicznej, nie drugim źródłem
  prawdy;
- surowy minutowy CSV pozostaje dostępny dla diagnostyki i retencji.

## Weryfikacja

- 262/262 testy automatyczne;
- RuleEngine 55/55, Application 45/45, Reports 9/9, Infrastructure 48/48;
- dwa referencyjne testy Stanka i Dobosia oraz macierz progu, FIFO, terminu i
  restartu;
- build Release bez błędów i ostrzeżeń;
- końcowy smoke test terenowy z aktywną telemetrią pozostaje do wykonania przez
  testera po zbudowaniu paczki.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.10.1

Hotfix wydania beta.10.

- naprawiono błąd startu aplikacji powodowany przez nakładające się rekordy
  kanonicznej historii aktywności;
- nowa sesja dodaje do wcześniejszej historii wyłącznie niepokryte fragmenty;
- wpisy manualne uzupełniające wcześniejsze luki zostają zachowane w całości;
- nakładające się rekordy są wykrywane wcześnie, z czytelną diagnostyką zamiast
  błędu bazy danych;
- poprawiono archiwizację bloków ciepłych dla kart ze zdublowanymi minutami.

## Zgodność

- protokół pluginu SCS bez zmian, nadal wersja 3;
- brak zmian wymagających czyszczenia albo migracji danych użytkownika;
- zachowaj dotychczasową bazę — aplikacja nadal wykonuje jej kopię przed migracją.

## Weryfikacja

- 239 testów automatycznych;
- 14 testów regresyjnych projekcji kanonicznej, w tym przypadki odtworzone
  z danych terenowych obu kart;
- kontrola na kopii bazy z testów: zero nakładek i zdublowanych początków,
  `ArchiveWarmAsync` przechodzi i jest idempotentne, backfill wpisów manualnych
  pozostaje nienaruszony;
- kompilacja Release bez ostrzeżeń.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.10

Poprawka ciągłości odpoczynku po wyjęciu i ponownym włożeniu karty.

- wyjęcie karty nadal otwiera jawną lukę i zatrzymuje automatyczny zapis;
- po rozliczeniu brakującego czasu jako Przerwa/Odpoczynek sąsiadujące odcinki
  odpoczynku są traktowane jako jeden ciągły blok;
- działa w obu kierunkach: odpoczynek przed wyjęciem oraz po ponownym włożeniu;
- wpis manualny zachowuje `SourceGapId`, więc ślad audytowy nie znika;
- Inna praca, Dyspozycyjność, nierozliczona luka albo brak ciągłości minutowej
  nadal przerywają odpoczynek.

## Weryfikacja

- 225 testów automatycznych;
- regresje `2 h + 7 h = 9 h`, `7 h + 2 h = 9 h` oraz odpoczynek po obu
  stronach rozliczonej luki;
- test pełnej ścieżki: wyjęcie karty → włożenie → wpis manualny → reset dobowy;
- WPF zbudowane bez błędów i ostrzeżeń.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.9

Poprawka utraty wybranej aktywności podczas załadunku i rozładunku.

- plugin beta.8 prawidłowo przekazywał znacznik operacji ładunkowej, ale silnik
  zerował zapamiętaną aktywność po otrzymaniu ramki `GamePaused`;
- aktywność sprzed pauzy jest teraz przechowywana osobno i służy wyłącznie do
  sklasyfikowania potwierdzonego czasu załadunku lub rozładunku;
- czas operacji zachowuje wybór z tachografu: Inna praca, Dyspozycyjność albo
  Przerwa/Odpoczynek;
- zwykły skok `g_set_time` bez znacznika operacji nadal tworzy lukę;
- ramki pauzy/menu nadal nie dopisują czasu rzeczywistego do historii.

## Weryfikacja

- 221 testów automatycznych;
- test regresji odtwarza rzeczywistą kolejność z logu: aktywność → pauza ze
  znacznikiem ładunku → wznowienie po skoku 20 minut;
- przypadek sprawdzony dla wszystkich trzech ręcznie wybieranych aktywności.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.8

Poprawka kolejności zdarzeń podczas rzeczywistego załadunku i rozładunku.

- ETS2 może wysłać pierwszą ramkę z przeskoczonym czasem przed zmianą
  `cargo.loaded`; plugin wstrzymuje tę pierwszą ramkę po wznowieniu;
- jeżeli potwierdzenie ładunku mimo wszystko dotrze jedną ramkę później, silnik
  wycofuje świeżą lukę i zastępuje ją wybraną aktywnością;
- usunięcie luki i zapis odtworzonych minut trafiają do jednego zestawu zapisu;
- opcjonalny kreator zamyka się, jeśli luka została automatycznie wycofana;
- log diagnostyczny zapisuje zmianę znacznika operacji ładunkowej.

## Weryfikacja

- 218 testów automatycznych;
- test regresji dokładnej kolejności zaobserwowanej w ETS2: skok czasu, a dopiero
  w następnej ramce `cargo.loaded=true`;
- plugin x64 oraz WPF zbudowane bez błędów i ostrzeżeń.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.7

Pilna poprawka uruchamiania pluginu z protokołem v3.

- plugin deklaruje SCS Telemetry API 1.01 wymagane przez zdarzenia `gameplay`;
- usuwa błąd inicjalizacji `event introduced in 1.1` widoczny w `game.log.txt`;
- zachowuje poprawkę załadunku i rozładunku z beta.6.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.6

Szósta paczka do beta-testów. Naprawia błędne tworzenie luk podczas ekranów
załadunku i rozładunku w ETS2.

## Załadunek i rozładunek

- plugin korzysta z oficjalnych zdarzeń konfiguracji zlecenia i `job.delivered`;
- kontrolowany skok czasu podczas załadunku lub rozładunku nie tworzy już luki;
- brakujące minuty zachowują aktywność wybraną na danej karcie przed operacją:
  Inna praca, Dyspozycyjność albo Przerwa/Odpoczynek;
- zwykły duży skok czasu, w tym `g_set_time`, nadal pozostaje luką;
- protokół telemetrii został podniesiony do v3, dlatego DLL z paczki beta.6 jest
  wymagana i musi zastąpić plugin v2.

## Weryfikacja

- 217 testów automatycznych;
- osobne regresje dla wszystkich trzech aktywności i obu slotów;
- plugin x64 skompilowany bez błędów i ostrzeżeń.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.5

Piąta paczka do beta-testów. Dodaje roboczą listę luk aktywności oraz jawne
oznaczenie kompletności raportów.

## Historia i rozliczanie luk

- ekran **Historia** pokazuje kanoniczne luki obu kart, najnowsze na górze;
- domyślnie widoczne są wyłącznie luki nierozliczone, a filtr **Pokaż rozliczone**
  udostępnia pełny ślad audytowy;
- otwarta luka ma status `TRWA`, aktualizowaną długość i nie oferuje rozliczenia;
- zamkniętą lukę można rozliczyć istniejącym kreatorem w trybie opcjonalnym;
- po zapisie lista i licznik odświeżają się bez restartu;
- luki z porzuconych gałęzi czasu nie pojawiają się w projekcji roboczej.

## Kompletność raportów

- przed eksportem raport jest zawsze przeliczany ponownie;
- zakres z nierozliczonymi lukami pokazuje ostrzeżenie i akcję **Pokaż luki**;
- ostrzeżenie nie blokuje eksportu PDF, JSON ani surowego CSV;
- PDF jawnie pokazuje `LUKI: brak` albo liczbę i łączny czas luk oraz bilans zakresu;
- VTC JSON zawiera sekcję `completeness` z licznikiem, minutami luk, bilansem
  i polem `evidenceComplete`;
- rozliczone luki nie obniżają kompletności raportu.

## Dalsze ulepszenia UI

- główny LCD pokazuje czas gry zamiast zegara Windows;
- wystawione są liczniki pracy dobowej, wydłużeń jazdy dziennej, skróconych
  odpoczynków oraz rekompensat, z właściwymi horyzontami resetu.

## Weryfikacja

- 212 testów automatycznych;
- pełna kompilacja rozwiązania bez błędów i ostrzeżeń;
- nagłówek PDF z lukami i bilansem sprawdzony po wyrenderowaniu strony.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.4

Poprawka blokującego błędu kreatora wpisu manualnego po cofnięciu czasu gry.

## Naprawa

- przyciętą lukę z aktualnej gałęzi czasu można teraz normalnie rozliczyć;
- źródłowa luka z porzuconej gałęzi pozostaje nietknięta jako ślad audytowy;
- rozliczony fragment jest zapisywany w bieżącej sesji i poprawnie odtwarzany po restarcie;
- ponowne zatwierdzenie identycznego wpisu pozostaje idempotentne;
- eksport `.tacho` zachowuje powiązanie fragmentu z luką źródłową (schemat 3).

## Weryfikacja

- 193 testy automatyczne;
- regresja SQLite dla przyciętej luki i regresja restartu silnika;
- aplikacja WPF kompiluje się bez ostrzeżeń.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.3

Trzecia lokalna paczka do beta-testów. Wprowadza jawne luki aktywności, wpisy
manualne oraz kreator wymagany po ponownym włożeniu karty.

## Wpisy manualne

- `CardRemoved` wymusza wpis i blokuje obsługę tachografu do rozliczenia;
- `ForwardTimeJump` pozostaje opcjonalny i nie blokuje jazdy;
- jeden klik zapisuje całą lukę jako odpoczynek, a dodatkowe bloki Innej pracy
  tworzą wpis mieszany bez dziur;
- wynik rozróżnia wybrany cel od faktycznej kwalifikacji odpoczynku;
- ciągły odpoczynek minimum 9 godzin resetuje okres dobowy retroaktywnie na końcu
  bloku; Inna praca i Dyspozycyjność przerywają ciągłość.

## Weryfikacja

- 191 testów automatycznych w konfiguracji Release;
- aplikacja self-contained `win-x64`;
- plugin SCS `Release|x64`, protokół v2.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.2

Druga lokalna paczka do beta-testów. Aplikacja i plugin muszą zostać zaktualizowane
razem, ponieważ protokół shared memory v2 nie jest binarnie zgodny z v1.

## Naprawy blokujące

- zamknięte minuty są zapisywane pod sesją, do której rzeczywiście należą;
- granica cofnięcia czasu jest zapisywana atomowo dla obu kart;
- ponowny identyczny zapis minuty jest rozpoznawany po sesji i minucie, niezależnie
  od losowego GUID-a;
- sprzeczna treść tej samej minuty nie zatrzymuje telemetrii i trafia do logu jako
  `ACTIVITY_RECORD_CONFLICT`.

## Protokół v2

- 28-bajtowa struktura i mapa `Local\ETS2Tachograph.Telemetry.v2`;
- `world_generation` zwiększane przez `frame_start.timer_restart`;
- pierwsza wartość generacji jest tylko punktem odniesienia;
- późniejsza zmiana tworzy wspólną granicę sesji obu kart również przy identycznym
  albo późniejszym czasie gry;
- aplikacja wykrywa pozostawioną mapę v1 i zgłasza konieczność wymiany DLL.

## Weryfikacja

- 122 testy automatyczne;
- natywny plugin `Release|x64`, aplikacja WPF i monitor budują się bez ostrzeżeń.

---

# ETS2 EU Digital Tachograph 0.1.0-beta.1

Pierwsza wersjonowana paczka do kontrolowanych beta-testów.

## Najważniejsze funkcje

- oficjalna telemetria SCS SDK 1.14 i wersjonowany protokół shared memory v1;
- panel WPF, realistyczny tachograf, dwie karty kierowców i nakładki `S1`/`S2`;
- liczniki jazdy i odpoczynku, podwójna obsada, OUT, prom oraz kraje start/koniec;
- trwała historia SQLite, import/eksport, PDF, surowy CSV, VTC JSON i diagnostyka ZIP;
- warstwowa retencja hot/warm oparta na `highWaterMark`;
- czytelne bloki aktywności i punkty przełomowe w raporcie PDF.

## Zabezpieczenia tej wersji

- backup SQLite przed każdą migracją;
- blokada drugiej instancji aplikacji i monitora;
- jednoznaczny alarm niezgodnej wersji pluginu;
- ignorowanie ramek `running == 0` przez zapis i high-water mark;
- regresje `03:53 + 01:34 = 05:27`, suma bloków równa rozpiętości zegara oraz
  reset jazdy dziennej pojedynczej karty po 9 godzinach odpoczynku.

## Przed testem

Usuń starą DLL pluginu z `bin\win_x64\plugins`, skopiuj DLL z tej paczki i uruchom
ETS2 ponownie. Szczegóły instalacji oraz znane ograniczenia znajdują się w
`README.md` i `KNOWN_ISSUES.md`.
