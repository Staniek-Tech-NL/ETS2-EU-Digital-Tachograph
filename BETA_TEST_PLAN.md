# Plan testów beta.10

## Przygotowanie

1. Zamknij ETS2 i poprzednią wersję aplikacji.
2. Rozpakuj paczkę do nowego katalogu. Nie uruchamiaj programu bezpośrednio z ZIP-a.
3. Protokół telemetrii ma wersję v3. Skopiuj DLL z katalogu `plugin` do
   `Euro Truck Simulator 2\bin\win_x64\plugins\`, zastępując poprzednią wersję,
   i uruchom ETS2 ponownie.
4. Uruchom `app\ETS2Tachograph.Desktop.exe`, a następnie ETS2.
5. Zachowaj `%LocalAppData%\ETS2Tachograph\tachograph.db`. Beta ma pracować na
   dotychczasowej historii; aplikacja nadal wykonuje kopię bazy przed migracją.

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

## Test 1 — lista nierozliczonych luk

1. Utwórz zamkniętą lukę przez wyjęcie i ponowne włożenie karty albo użyj istniejącej.
2. Otwórz ekran **Historia**.
3. Oczekiwane: sekcja **LUKI AKTYWNOŚCI** pokazuje kartę, slot, zakres czasu gry,
   długość, przyczynę i stan; najnowsza luka znajduje się na górze.
4. Sprawdź licznik w nagłówku. Ma obejmować wyłącznie pozycje nierozliczone.
5. Wyjmij kartę i pozostaw ją poza urządzeniem.
6. Oczekiwane: otwarta luka ma status `TRWA`, dopisek „karta nadal wyjęta”, rosnący
   czas i nieaktywną akcję rozliczenia.

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

## Zgłaszanie błędu

Po błędzie nie usuwaj bazy. Kliknij **Raport diagnostyczny** i dołącz utworzony ZIP,
problemowy PDF lub JSON oraz krótki opis: karta, slot, zakres minut gry i wykonane kroki.
