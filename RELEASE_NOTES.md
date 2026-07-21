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
