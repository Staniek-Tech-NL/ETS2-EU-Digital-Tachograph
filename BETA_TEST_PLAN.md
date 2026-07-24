# Plan testów — beta.11.1 i bieżące zmiany lokalne

Sekcje terenowe zachowują kontrakt paczki beta.11.1. Checklista UI obejmuje
również bieżące, nieopublikowane zmiany lokalne. Nie wolno na jej podstawie
tworzyć ani publikować nowej paczki beta bez osobnej decyzji.

## Przygotowanie

1. Zamknij ETS2 i poprzednią wersję aplikacji.
2. Rozpakuj paczkę do nowego katalogu. Nie uruchamiaj programu bezpośrednio z ZIP-a.
3. Protokół telemetrii pozostaje w wersji v3. Skopiuj DLL z katalogu `plugin` do
   `Euro Truck Simulator 2\bin\win_x64\plugins\`, zastępując poprzednią wersję,
   i uruchom ETS2 ponownie.
4. Uruchom `app\ETS2Tachograph.Desktop.exe`, a następnie ETS2.
5. Zachowaj `%LocalAppData%\ETS2Tachograph\tachograph.db`. Beta.11.1 ma pracować na
   dotychczasowej historii; aplikacja nadal wykonuje kopię bazy przed migracją.

## Checklista regresji UI — po każdej zmianie w XAML

Wykonuj w całości i w tej kolejności po każdej modyfikacji `MainWindow.xaml`,
`OverlayWindow.xaml` lub `App.xaml`, także gdy zmiana wygląda na kosmetyczną.
Zmiana w XAML nie jest gotowa, dopóki wszystkie punkty nie są odhaczone.

### 1. Kompilacja i start

- [ ] `dotnet build ETS2Tachograph.sln` kończy się bez błędów i bez nowych ostrzeżeń XAML.
- [ ] `app\ETS2Tachograph.Desktop.exe` uruchamia się i pokazuje okno główne
      (brak `XamlParseException` na starcie).
- [ ] Okno da się zmaksymalizować i przywrócić; nagłówek `SYSTEM ZARZĄDZANIA TACHOGRAFEM`
      i pasek stanu połączenia są widoczne, żadna sekcja nie jest ucięta.

### 2. Dashboard

- [ ] Kafle `JAZDA`, `PRACA`, `GOTOWOŚĆ`, `ODPOCZYNEK` renderują się z wartościami,
      a nie z pustymi polami lub literalnym tekstem `{Binding …}`.
- [ ] Liczniki `Jazda dzienna:`, `Praca dobowa:`, `Do przerwy jazdy:` aktualizują się
      przy podłączonym ETS2 (telemetria v3).
- [ ] Sekcje `ALERTY` i `NARUSZENIA` pokazują pozycje lub pustą listę bez wyjątku.
- [ ] `BIEŻĄCY PODGLĄD TACHOGRAFU` wyświetla trzy linie ekranu urządzenia,
      a przyciski góra / dół / OK / anuluj przewijają menu.
- [ ] `SZYBKIE AKCJE` działają: `ROZPOCZNIJ PAUZĘ`, `PAUZA KIEROWCY 2`.

### 3. Nawigacja

- [ ] Wszystkie zakładki lewego paska otwierają się i wracają:
      **Dashboard**, **Historia**, **Rekompensaty**, **Raporty**, **Kierowcy**, **Ustawienia**.
- [ ] Przełączanie zakładek tam i z powrotem nie gubi stanu (filtry, wybór karty, zakres).
- [ ] Skrót z raportu (`POKAŻ LUKI`) przenosi na Historię i podświetla sekcję luk.

### 4. Slot 1

- [ ] Włożenie karty do slotu 1 otwiera dialog karty z poprawnym tytułem i slotem.
- [ ] `SLOT 1 - KIEROWCA AKTYWNY` pokazuje właściciela karty i bieżącą aktywność.
- [ ] Zmiana aktywności (Jazda / Inna praca / Dyspozycyjność / Przerwa) przechodzi
      na kafle i na liczniki.
- [ ] Wyjęcie karty przełącza panel w stan bez karty i nie zeruje historii.

### 5. Slot 2

- [ ] Włożenie karty do slotu 2 wypełnia `SLOT 2 - KIEROWCA ZMIENNIK`.
- [ ] Liczniki kierowcy 2 (jazda dzienna, praca dobowa, do przerwy) liczą niezależnie
      od kierowcy 1.
- [ ] `PAUZA KIEROWCY 2` startuje odpoczynek tylko dla slotu 2.
- [ ] Wyjęcie karty ze slotu 2 nie wpływa na panel slotu 1.

### 6. Tryby

- [ ] W menu urządzenia strona trybów przełącza **OUT** — linia trybów pokazuje `OUT`.
- [ ] Ta sama strona przełącza **Prom** — linia trybów pokazuje `Prom`.
- [ ] Wyłączenie obu trybów wraca do `Tryb zwykły`.
- [ ] Druga karta zmienia opis obsady na `podwójna obsada (30 h)`, a jej wyjęcie
      wraca do `pojedyncza obsada (24 h)`.
- [ ] Bez karty w slocie 1 próba zmiany trybu daje komunikat `Włóż kartę do slotu 1.`
      zamiast wyjątku.

### 7. Historia

- [ ] `HISTORIA AKTYWNOŚCI · WSZYSTKIE KARTY` listuje rekordy obu kart.
- [ ] Sekcja `LUKI AKTYWNOŚCI` pokazuje kartę, slot, zakres, długość, przyczynę i stan.
- [ ] Licznik luk w nagłówku zgadza się z liczbą pozycji nierozliczonych.
- [ ] Przełącznik `Pokaż rozliczone` filtruje listę w obie strony.
- [ ] `Rozlicz` otwiera wariant B kreatora; cała luka jest początkowo jednym
      segmentem `Przerwa / Odpoczynek`.
- [ ] Szybkie akcje zastępują całą lukę odpoczynkiem, inną pracą albo
      dyspozycyjnością.
- [ ] `DODAJ / ZASTĄP SEGMENT` automatycznie dzieli plan, a sąsiednie segmenty
      tej samej aktywności są scalane.
- [ ] `EDYTUJ`, podwójne kliknięcie i `USUŃ` działają zgodnie z planem;
      usunięcie pracy lub dyspozycyjności przywraca odpoczynek.
- [ ] Pola dnia i godziny obsługują zakres przechodzący przez północ i nie
      pozwalają wyjść poza granice luki.
- [ ] Podsumowanie pokazuje pokrycie oraz sumy trzech aktywności;
      `ZATWIERDŹ WPIS` jest aktywny wyłącznie dla kompletnego planu.
- [ ] `ANULUJ`, `Esc` i krzyżyk wymagają potwierdzenia, jeśli kopia robocza
      została zmieniona.
- [ ] Listy aktywności i dni mają czarny tekst na jasnym tle.

### 7A. Kraje i licznik odpoczynku tygodniowego

- [ ] Dialog rozpoczęcia i zakończenia pokazuje listę `PL — Polska` zamiast
      dowolnego pola tekstowego.
- [ ] Wyszukiwanie działa po ISO i początku polskiej nazwy kraju.
- [ ] `POTWIERDŹ OK` jest nieaktywne bez poprawnego wyboru.
- [ ] Ostatni kraj jest odtwarzany osobno dla każdej karty, bez automatycznego
      zatwierdzania.
- [ ] LCD używa kodu tachografowego, a historia zachowuje stabilny kod ISO.
- [ ] Dla `89:39` pole `ODP. TYG.` pokazuje dokładnie `3/6 (89:39)`.
- [ ] Próg zmienia się na `4/6` dopiero przy `96:00`; po `144:00` nadmiar jest
      prezentowany jako `6/6+ (HH:MM)`.

### 8. Raporty

- [ ] `RAPORTY I STATYSTYKI` renderuje wybór karty i zakresu; `Odśwież raport` przelicza.
- [ ] Podsumowania (jazda, praca, odpoczynek, rekompensata, liczba naruszeń) mają wartości.
- [ ] Ostrzeżenie o nierozliczonych lukach pojawia się i nie blokuje eksportu.
- [ ] Eksporty działają i tworzą plik: `PDF`, `VTC JSON`, `CSV ZOBOWIĄZANIA`,
      `Eksportuj .tacho kierowcy 1`, `Importuj .tacho`.
- [ ] `Raport diagnostyczny` tworzy ZIP.

### 9. Nakładki S1 / S2

- [ ] `ALT+1` pokazuje i ukrywa nakładkę slotu 1, `ALT+2` — slotu 2.
- [ ] Etykieta slotu w nakładce to odpowiednio `S1` i `S2`.
- [ ] Pola `JAZDA CIĄGŁA`, `DO PRZERWY`, `DZIENNA / LIMIT`, `PRACA DOBOWA`,
      `CEL PAUZY`, `POZOSTAŁO`, `REKOMPENSATA` mają wartości zgodne z Dashboardem.
- [ ] Linia trybów w nakładce zgadza się z trybem ustawionym w punkcie 6.
- [ ] Nakładkę da się przeciągnąć; pozostaje nad oknem gry i nie kradnie fokusu.
- [ ] Nakładka odświeża się na żywo razem z telemetrią.

### 10. Restart i ponowna kontrola

- [ ] Zamknij aplikację — zamyka się czysto, bez procesu zostającego w tle
      i bez osieroconych okien nakładek.
- [ ] Uruchom ponownie i sprawdź, że utrzymały się: profil kierowcy, stan kart,
      tryby (OUT / prom), ustawienia z zakładki Ustawienia.
- [ ] Po restarcie aplikacja uruchamia się poprawnie, a nakładki można ponownie
      otworzyć i obsługiwać.
- [ ] Pozycje nakładek S1 i S2 są zgodne z zapisanymi ustawieniami — nakładka
      przeciągnięta przed restartem wraca w to samo miejsce. Widoczność nie jest
      zapisywana: po restarcie obie nakładki są ukryte do czasu `ALT+1` / `ALT+2`.
- [ ] Historia i stan rozliczenia luk są takie same jak przed restartem.
- [ ] Powtórz punkty 2, 3 i 9 na świeżo uruchomionej aplikacji.
- [ ] Sprawdź log diagnostyczny — brak nowych błędów bindowania i wyjątków UI.

## Test 0 — załadunek i rozładunek

1. Na postoju wybierz na karcie aktywność, np. **Inna praca**.
2. Rozpocznij załadunek lub zakończ zlecenie z rozładunkiem.
3. Oczekiwane: skok czasu gry nie otwiera kreatora i nie tworzy luki.
4. W Historii sprawdź, że cały czas operacji otrzymał wcześniej wybraną aktywność.
5. Powtórz dla **Dyspozycyjności** i **Przerwy/Odpoczynku**.
6. Kontrolnie wykonaj duży skok przez `g_set_time`. Oczekiwane: nadal powstaje luka.
7. Najważniejsza sekwencja regresji: po wyborze aktywności rozpocznij operację,
   podczas której ETS2 pokazuje pauzę/menu i przesuwa czas o około 20 minut.
   Oczekiwane: po wznowieniu nie pojawia się kreator wpisu manualnego.

## Test 0A — ciągłość odpoczynku po wyjęciu karty

1. Włóż kartę i rozpocznij **Przerwę/Odpoczynek**.
2. Po 2 godzinach czasu gry wyjmij kartę.
3. Pozostaw kartę wyjętą przez 7 godzin czasu gry, następnie włóż ją ponownie.
4. W kreatorze zatwierdź całą lukę jako **Przerwa/Odpoczynek**.
5. Oczekiwane: licznik i RuleEngine widzą jeden ciągły odpoczynek 9 godzin,
   a reset dobowy jest zapisany na końcu rozliczonego bloku.
6. Powtórz kontrolnie, dodając w luce segment Innej pracy albo Dyspozycyjności.
   Oczekiwane: segment przerywa ciągłość i odcinki nie sumują się przez niego.

## Test 0B — alokacja rekompensaty beta.11.1

1. Potwierdź na Dashboardzie i w zakładce **Rekompensaty**:
   - Staniek: `1253 min / 20:53`;
   - Doboś: `1192 min / 19:52`.
2. Dla każdego zobowiązania sprawdź źródłowy odpoczynek, pełny i pozostały dług,
   tydzień skrócenia, termin wyłączny, status oraz brak bloku spłacającego.
3. Potwierdź, że kilka krótszych nadwyżek nie zmniejsza długu. Jeden blok o minutę
   za krótki nie spłaca niczego, a dokładnie wystarczający spłaca całość en bloc.
4. Zamknij i uruchom aplikację ponownie. Wszystkie wartości i identyfikatory mają
   pozostać identyczne.
5. Dla zakończonego bloku 24 h+ wybierz kolejno dostępne warianty:
   `DailyRestWithCompensation` oraz `ReducedWeeklyRestOnly`.
6. Sprawdź, że UI pokazuje podstawę, stary i nowy dług oraz zaliczenie tygodnia
   dokładnie tak samo jak PDF, CSV i JSON.
7. Bez wyboru raport ma pokazywać ostrzeżenie i `EvidenceComplete = false`.

## Test 0C — wspólny skok czasu załogi beta.11.1

1. Gdy Staniek odpoczywa, wykonaj skok czasu przy stabilnej Innej pracy albo
   Dyspozycyjności Dobosia. Oczekiwane: brak luki obu kart.
2. Powtórz symetrycznie, gdy odpoczywa Doboś.
3. Kontrolnie powtórz bez odpoczywającej karty, z Jazdą oraz ze zmianą aktywności.
   Oczekiwane: bezpieczna luka pozostaje.
4. Po restarcie sprawdź, że korekta Dnia 141 nadal ma źródło
   `AutomaticCrewReconstruction`, a pierwotne luki są widoczne w audycie jako
   rozliczone.

## Test 1 — lista nierozliczonych luk

1. Utwórz zamkniętą lukę przez wyjęcie i ponowne włożenie karty albo użyj istniejącej.
2. Otwórz ekran **Historia**.
3. Oczekiwane: sekcja **LUKI AKTYWNOŚCI** pokazuje kartę, slot, zakres czasu gry,
   długość, przyczynę i stan; najnowsza luka znajduje się na górze.
4. Sprawdź licznik w nagłówku. Ma obejmować wyłącznie pozycje nierozliczone.
5. Wyjmij kartę i pozostaw ją poza urządzeniem.
6. Oczekiwane: otwarta luka ma status `TRWA`, dopisek „karta nadal wyjęta”, rosnący
   czas i nieaktywną akcję rozliczenia.

## Test 1A — granica licznika pauzy 44/45 min

**Status: ZALICZONY (lokalnie 2026-07-24).** Hotfix licznika pauzy potwierdzony
scenariuszem referencyjnym `41 min reconstructed + 3 min telemetry = 44 min`:
Dashboard, urządzenie oraz overlay slotów 1 i 2 pokazują `00:44`, `00:01` do celu
i status `W TRAKCIE`; 45. minuta daje `ZALICZONA` z resetem licznika jazdy
ciągłej. Regresja pokryta testami automatycznymi granicy 44/45 (gate 315/315,
raport `docs/BUGFIX_REPORT_QUALIFIED_BREAK_COUNTER_2026-07-24.md`). Pozostaje
wizualne potwierdzenie granicy in-game w ramach smoke (M7).

Procedura powtórzenia:

1. Rozpocznij pauzę możliwie blisko granicy minuty gry.
2. Przy 44 zatwierdzonych minutach porównaj Dashboard, urządzenie, overlay oraz
   licznik jazdy ciągłej.
3. Oczekiwane regulacyjnie: brak resetu jazdy i dokładnie jedna minuta do pełnej
   przerwy (`00:44`, `00:01` do celu, `W TRAKCIE`). Wynik `45:00 / ZALICZONA`
   przy 44 minutach oznaczałby nawrót naprawionego problemu prezentacyjnego
   beta.11.1 — wtedy zapisz FIX.
4. Po następnej pełnej minucie oczekiwane jest zaliczenie i reset licznika.
5. Nie koryguj ręcznie historii ani progu RuleEngine podczas tego testu.

## Test 2 — rozliczenie z Historii i ślad audytowy

1. Przy zamkniętej luce kliknij **Rozlicz**.
2. Najpierw użyj **Anuluj**.
3. Oczekiwane: kreator zamyka się bez zapisu — wejście z Historii jest opcjonalne.
4. Otwórz kreator ponownie i zapisz całą lukę jako odpoczynek albo dodaj blok pracy.
5. Oczekiwane: przy wyłączonym filtrze wiersz znika, a licznik maleje bez restartu.
6. Włącz **Pokaż rozliczone**.
7. Oczekiwane: wiersz wraca ze stanem `ROZLICZONA`, czasem rozliczenia i bez akcji.
8. Uruchom aplikację ponownie i potwierdź, że stan pozostaje zachowany.

## Test 3 — projekcja po cofnięciu czasu

1. Utwórz lukę, a następnie wczytaj zapis gry cofający `game_time` przed jej początek.
2. Otwórz listę luk, również z filtrem rozliczonych.
3. Oczekiwane: luka z porzuconej gałęzi nie jest widoczna. Jeżeli karta nadal jest
   wyjęta, widoczna jest tylko jedna nowa otwarta luka z aktualnej gałęzi.

## Test 4 — ostrzeżenie przed raportem

1. W raporcie wybierz kartę i zakres zawierający co najmniej jedną nierozliczoną lukę.
2. Kliknij **GENERUJ RAPORT**.
3. Oczekiwane: pojawia się ostrzeżenie z liczbą luk i ich łącznym czasem oraz informacją,
   że raport będzie niekompletny.
4. Kliknij **POKAŻ LUKI**.
5. Oczekiwane: aplikacja przechodzi do sekcji luk na ekranie Historia.
6. Wróć do raportu i wyeksportuj PDF oraz JSON.
7. Oczekiwane: ostrzeżenie nie blokuje żadnego eksportu.

## Test 5 — PDF, JSON i bilans zakresu

1. Otwórz PDF wygenerowany dla zakresu z luką.
2. Oczekiwane: nagłówek zawiera `LUKI NIEROZLICZONE: X · HH:MM` oraz bilans
   `aktywności + luki = pokrycie / zakres`.
3. Otwórz JSON i znajdź sekcję `completeness`.
4. Oczekiwane: `unresolvedGapCount` i `unresolvedGapMinutes` są niezerowe,
   `balanceMatchesRange` jest prawdziwe, a `evidenceComplete` fałszywe.
5. Wygeneruj raport dla zakresu bez nierozliczonych luk.
6. Oczekiwane: PDF mówi `LUKI: brak`, a JSON ma `unresolvedGapCount: 0` oraz
   `evidenceComplete: true`.
7. W PDF sprawdź czytelną tabelę każdego zobowiązania: status, dług, termin,
   `ObligationId`, źródło, blok i zakres spłaty oraz `SettledAt`.
8. W CSV sprawdź nagłówek i dokładnie jeden rekord na zobowiązanie.
9. W JSON sprawdź pełne `compensationObligations`; sekcja `compensation` musi
   pozostać zgodnym podsumowaniem pochodnym.

## Końcowy smoke test terenowy artefaktu beta.11.1

- [x] Staniek: `1253 min / 20:53` w Dashboardzie, szczegółach i eksportach.
- [x] Doboś: `1192 min / 19:52` w Dashboardzie, szczegółach i eksportach.
- [x] PDF, CSV i JSON są zgodne z pełnym kontraktem DTO.
- [x] Po restarcie aplikacji wyniki i identyfikatory są identyczne.
- [x] Przy aktywnej telemetrii ruch automatycznie ustawia **Jazdę**.
- [x] Blokady zależne od ruchu działają zgodnie z opisem UI; pamiętaj, że SCS jest
      tylko do odczytu i nie zatrzymuje fizycznie ciężarówki.

**Wynik z 23 lipca 2026:** wszystkie testy zielone. Decyzja: **GO** dla
artefaktu `0.1.0-beta.11.1`.

## Zgłaszanie błędu

Po błędzie nie usuwaj bazy. Kliknij **Raport diagnostyczny** i dołącz utworzony ZIP,
problemowy PDF lub JSON oraz krótki opis: karta, slot, zakres minut gry i wykonane kroki.
