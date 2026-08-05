# Raport dzienny prac — 18 lipca 2026

## 1. Podsumowanie wykonawcze

W ciągu dnia projekt ETS2 EU Digital Tachograph przeszedł od wersji beta.4 do
beta.10. Główne prace objęły:

- domknięcie mechanizmu jawnych luk i wpisów manualnych po cofnięciu czasu;
- dodanie widocznej w UI listy luk oraz informacji o kompletności raportów;
- wystawienie brakujących liczników regulacyjnych i czasu gry w interfejsie;
- rozpoznawanie załadunku i rozładunku przez oficjalną telemetrię SCS;
- usunięcie fałszywych luk tworzonych podczas ekranów operacji ładunkowych;
- zachowanie ciągłości odpoczynku po wyjęciu karty, jeżeli czas bez karty zostanie
  rozliczony wpisem manualnym jako Przerwa/Odpoczynek;
- zwiększenie zestawu automatycznych testów do 225 przypadków.

Aktualną wersją przeznaczoną do dalszego testowania jest **0.1.0-beta.10**.
Użytkownik po testach nie zgłosił kolejnych błędów.

## 2. Chronologia wydań przygotowanych dzisiaj

| Wersja | Godzina paczki | Najważniejszy zakres | Testy |
|---|---:|---|---:|
| beta.4 | 01:05 | Rozliczanie luki przyciętej przez nową gałąź czasu | 193 |
| beta.5 | 14:00 | Lista luk, kompletność raportów, nowe dane UI | 212 |
| beta.6 | etap przejściowy | Protokół v3 i zdarzenia załadunku/rozładunku | 217 |
| beta.7 | 18:29 | Poprawna wersja API pluginu SCS 1.01 | 217+ |
| beta.8 | 18:50 | Obsługa opóźnionej kolejności zdarzeń ładunku | 218 |
| beta.9 | 19:39 | Naprawa utraty aktywności podczas `GamePaused` | 221 |
| beta.10 | 21:01 | Ciągłość odpoczynku przez rozliczoną lukę | 225 |

Beta.6 była etapem technicznym i została zastąpiona przez beta.7 po wykryciu
niezgodnej deklaracji API pluginu. Beta.8 i beta.9 były kolejnymi iteracjami tej
samej diagnostyki, przy czym dopiero beta.9 usuwa rzeczywistą przyczynę błędu
zaobserwowanego w grze.

## 3. Jawne luki i wpisy manualne — beta.4

Naprawiono blokujący przypadek kreatora wpisu manualnego po cofnięciu czasu gry.
Problem dotyczył luki, której kanoniczny fragment został przycięty przez późniejszą
gałąź `truncate-and-append`.

Wykonane zmiany:

- przycięty fragment luki można normalnie rozliczyć;
- źródłowa luka pozostaje w starej, porzuconej gałęzi jako ślad audytowy;
- rozwiązanie jest materializowane w bieżącej sesji czasu;
- wpis przeżywa ponowne uruchomienie programu;
- eksport `.tacho` zachowuje powiązanie ze źródłową luką w schemacie 3;
- ponowne wysłanie identycznego rozwiązania pozostaje idempotentne;
- konflikt treści nie jest bezgłośnie ukrywany.

Zachowano podstawową zasadę projektu: źródłowa historia minuta po minucie nie jest
niszczona, a projekcja kanoniczna zastępuje wyłącznie nakładający się fragment
porzuconej przyszłości.

## 4. Lista luk i kompletność dowodu — beta.5

### 4.1. Ekran Historia

Do ekranu Historia dodano roboczą sekcję luk aktywności:

- domyślnie pokazuje tylko kanoniczne luki `Unresolved`;
- filtr **Pokaż rozliczone** udostępnia również ślad historyczny;
- licznik w nagłówku zawsze obejmuje wyłącznie pozycje wymagające reakcji;
- otwarta luka ma stan `TRWA`, długość do bieżącej minuty gry i brak akcji;
- zamknięta luka może otworzyć istniejący kreator wpisu manualnego;
- po rozliczeniu widok oraz licznik odświeżają się bez restartu;
- luki z porzuconych gałęzi czasu nie pojawiają się w widoku kanonicznym.

### 4.2. Raporty PDF i VTC JSON

Raport przestał wyglądać na kompletny, gdy zawiera brakujące minuty:

- przed eksportem zakres jest ponownie analizowany;
- UI ostrzega o liczbie nierozliczonych luk i ich łącznym czasie;
- ostrzeżenie nie blokuje wygenerowania raportu;
- PDF pokazuje `LUKI: brak` albo liczbę i czas brakujących danych;
- JSON VTC zawiera sekcję `completeness`, bilans minut i `evidenceComplete`;
- rozliczone luki nie obniżają kompletności dowodu;
- zachowany jest bilans: aktywności + nierozliczone luki = rozpiętość zakresu.

### 4.3. Pozostałe dane UI

- główny LCD pokazuje czas gry zamiast zegara Windows;
- wystawiono licznik pracy dobowej;
- wystawiono wykorzystane wydłużenia jazdy dziennej w tygodniu regulacyjnym;
- wystawiono skrócone odpoczynki dobowe od ostatniego odpoczynku tygodniowego;
- wystawiono podsumowanie rekompensat z liczbą zobowiązań i najbliższym terminem;
- przekroczenia nie są maskowane — wartości ponad limit pozostają widoczne.

## 5. Załadunek i rozładunek — beta.6 do beta.9

### 5.1. Pierwotny problem

ETS2 przesuwa czas gry podczas załadunku lub rozładunku. Bez dodatkowego sygnału
wygląda to tak samo jak ręczny duży skok `g_set_time`, dlatego procesor tworzył
opcjonalną lukę i otwierał kreator wpisu.

### 5.2. Protokół v3 i plugin

Do natywnego pluginu dodano licznik generacji operacji ładunkowej:

- plugin korzysta z oficjalnych zdarzeń konfiguracji zlecenia i zakończenia pracy;
- protokół shared memory został podniesiony do v3;
- aplikacja rozróżnia potwierdzony załadunek/rozładunek od zwykłego skoku czasu;
- brakujące minuty zachowują aktywność wybraną w tachografie osobno dla karty;
- obsługiwane są: Inna praca, Dyspozycyjność i Przerwa/Odpoczynek.

Beta.7 poprawiła deklarację pluginu na SCS Telemetry API 1.01. Usunęło to błąd
inicjalizacji `event introduced in 1.1` i przywróciło wykrywanie ETS2.

### 5.3. Kolejność zdarzeń — beta.8

Dodano zabezpieczenia na sytuację, w której skok czasu i potwierdzenie ładunku
przychodzą w sąsiednich ramkach:

- plugin wstrzymuje pierwszą ramkę po wznowieniu;
- silnik może wycofać świeżo utworzoną lukę po późnym potwierdzeniu operacji;
- usunięcie luki i dopisanie odtworzonych minut odbywa się w jednej partii;
- opcjonalny kreator zamyka się, jeżeli luka została automatycznie wycofana;
- log diagnostyczny zapisuje zmianę znacznika operacji ładunkowej.

### 5.4. Ostateczna diagnoza na podstawie rzeczywistych logów — beta.9

Zweryfikowano uruchomioną aplikację oraz plugin:

- uruchomiony był właściwy plik beta.8;
- hash zainstalowanego pluginu był identyczny z pluginem z paczki;
- plugin poprawnie inicjalizował protokół v3;
- gra zgłaszała zmianę znacznika ładunku przed skokiem czasu.

Rzeczywista sekwencja w logu aplikacji:

1. `19:30:16` — `TELEMETRY_STATUS`, ETS2 w pauzie;
2. `19:30:17` — `CARGO_OPERATION_MARKER`, znacznik przy Dniu 130, 05:00;
3. `19:30:22` — `GAME_TIME_JUMP` do Dnia 130, 05:21;
4. silnik błędnie tworzył lukę `[05:01, 05:21)`.

Przyczyną nie był plugin ani kolejność zdarzeń. W gałęzi `GamePaused` procesor
wykonywał `_lastActivity = null`. Po wznowieniu znał znacznik załadunku, ale nie
miał już informacji, że przed pauzą wybrano Inną pracę.

Naprawa:

- aktywność i warunek sprzed pauzy są przechowywane w osobnym stanie;
- sam czas rzeczywisty pauzy nadal nie jest dopisywany do historii;
- zapamiętana aktywność służy do klasyfikacji potwierdzonego interwału ładunkowego;
- stan jest czyszczony po pierwszej aktywnej ramce lub resecie sesji;
- zwykły `g_set_time` bez znacznika operacji nie jest błędnie uznawany za załadunek.

Najpierw dodano test odtwarzający dokładną sekwencję z logu. Przed poprawką test
tworzył 20-minutową lukę. Po poprawce ten sam zakres jest zapisany jako wybrana
aktywność. Regresję rozszerzono na wszystkie trzy ręczne aktywności.

## 6. Zachowanie `g_set_time`, snu i pauzy

Po zmianach obowiązuje następująca polityka:

| Scenariusz | Wynik |
|---|---|
| Skok do 2 minut | Rekonstrukcja ostatnią aktywnością |
| Duży skok po Jeździe | Luka, nigdy sztuczna wielogodzinna Jazda |
| Duży skok po Innej pracy lub Dyspozycyjności | Opcjonalna luka |
| Duży skok przy odpoczynku, pojazd stał przed i po | Rekonstrukcja odpoczynku |
| Potwierdzony załadunek/rozładunek | Wybrana aktywność, bez luki |
| Slot 2 podczas ruchu pojazdu | Brak długiego odpoczynku; obowiązują reguły załogi |
| Pauza/menu bez zmiany czasu gry | Brak dopisywania czasu rzeczywistego |

Dlatego 9-godzinny skip może zostać zaliczony jako odpoczynek, jeżeli przed i po
skoku jest wybrana Przerwa/Odpoczynek oraz pojazd pozostaje nieruchomy. W innym
przypadku duży skok pozostaje jawną luką.

## 7. Ciągłość odpoczynku po wyjęciu karty — beta.10

### 7.1. Korekta wcześniejszej decyzji

Poprzednia reguła traktowała wyjęcie karty jako bezwarunkowe przerwanie bloku
odpoczynku. Było to zbyt restrykcyjne: koniec automatycznego rejestrowania nie
oznacza automatycznie końca rzeczywistej czynności kierowcy.

Nowa zasada:

- wyjęcie karty nadal kończy automatyczny zapis i otwiera `CardRemoved`;
- do ponownego włożenia czas pozostaje nierozliczoną luką audytową;
- po wpisie manualnym oznaczonym jako Przerwa/Odpoczynek sąsiadujące minutowo
  odcinki odpoczynku tworzą jeden blok regulacyjny;
- połączenie działa przed luką, po luce oraz jednocześnie po obu jej stronach;
- połączony blok nadal niesie `SourceGapId`, więc raport zachowuje pochodzenie;
- Inna praca i Dyspozycyjność przerywają ciągłość;
- nierozliczona luka lub rzeczywista dziura w minutach także ją przerywa.

### 7.2. Reset dobowy

Przykład objęty testem:

```text
02:00 odpoczynku z kartą
+ 07:00 bez karty, rozliczone jako odpoczynek
= 09:00 ciągłego odpoczynku
```

Silnik wykonuje reset dobowy na końcu dziewiątej godziny. Analogicznie działa
układ 7 godzin wpisu manualnego + 2 godziny odpoczynku zmierzonego.

### 7.3. Odpoczynek tygodniowy

Ta sama projekcja jest używana do klasyfikacji odpoczynku tygodniowego:

- co najmniej 24 godziny — skrócony odpoczynek tygodniowy;
- co najmniej 45 godzin — regularny odpoczynek tygodniowy;
- typ wynika z faktycznej długości, nie z celu wybranego w UI;
- skrócony odpoczynek nadal tworzy zobowiązanie rekompensaty.

Przykład: 5 godzin odpoczynku z kartą + 40 godzin bez karty rozliczone jako
odpoczynek daje ciągły regularny odpoczynek tygodniowy 45 godzin.

## 8. Integralność danych i zgodność wsteczna

- nie zmieniono minutowej historii źródłowej;
- nie usunięto ani nie ukryto encji `ActivityGap`;
- rozliczone segmenty nadal mają `ActivitySource.ManualEntry`;
- zachowany jest opcjonalny `SourceGapId`;
- beta.10 nie wymaga migracji SQLite;
- istniejące rozliczone wpisy są ponownie oceniane według nowej reguły po
  przeliczeniu historii;
- plugin v3 z beta.9 i beta.10 jest identyczny;
- zwykłe skoki czasu i nierozliczone luki zachowują wcześniejsze znaczenie.

## 9. Testy i jakość

Końcowy pełny przebieg beta.10:

- Core: 33 testy;
- Telemetry.Scs: 8 testów;
- Engine: 64 testy;
- RuleEngine: 42 testy;
- Application: 38 testów;
- Reports: 9 testów;
- Infrastructure: 31 testów;
- **łącznie: 225/225 zaliczonych**.

Dodatkowe regresje wykonane dzisiaj:

- aktywność → `GamePaused` → znacznik ładunku → skok 20 minut;
- wszystkie trzy ręczne aktywności podczas operacji ładunkowej;
- `2 h + 7 h = 9 h` odpoczynku;
- `7 h + 2 h = 9 h` odpoczynku;
- odpoczynek po obu stronach rozliczonej luki;
- nierozliczona luka nadal przerywa ciągłość;
- pełna ścieżka Engine: wyjęcie → włożenie → wpis → reset dobowy;
- wcześniejsze przypadki z Inną pracą i Dyspozycyjnością nadal nie zerują dnia.

Aplikacja WPF została zbudowana w konfiguracji Release z wynikiem:

- 0 błędów;
- 0 ostrzeżeń.

## 10. Artefakty końcowe

### Beta.9

- plik: `output/releases/ETS2Tachograph-0.1.0-beta.9-win-x64.zip`;
- SHA-256: `5C3EB466E22D2DDC4955AD5D8207BB06F0A2D7917ABFD1F415FDA215E42B1EF2`.

### Beta.10 — aktualna

- plik: `output/releases/ETS2Tachograph-0.1.0-beta.10-win-x64.zip`;
- rozmiar: 67 028 733 bajty;
- SHA-256: `C21874B82FBB4227B089339DC53ED1E03AD2481173F6F9B94A782A7DBF054428`;
- SHA-256 pluginu v3:
  `4F73CBFE0893A9D734E22173F7CDDC46B3C78F562B6CCF58288FDB0A73D97D02`.

## 11. Stan na koniec dnia

- aktualna wersja testowa: **0.1.0-beta.10**;
- telemetria ETS2 i plugin v3 działają;
- załadunek i rozładunek zachowują wybraną aktywność;
- `g_set_time` zachowuje wcześniejszą politykę bezpieczeństwa;
- wpisy manualne zachowują ślad audytowy;
- odpoczynek dobowy i tygodniowy może pozostać ciągły przez rozliczony czas bez karty;
- wszystkie testy automatyczne są zielone;
- po końcowych testach użytkownik nie znalazł kolejnych błędów;
- brak aktualnie potwierdzonego nowego błędu blokującego beta-testy.

## 12. Zalecany kolejny test terenowy

Przed uznaniem beta.10 za kandydata do szerszej publikacji warto wykonać dwa
długie scenariusze w rzeczywistej grze:

1. odpoczynek dobowy: 2 godziny z kartą + 7 godzin bez karty, następnie wpis
   manualny i kontrola licznika oraz PDF;
2. odpoczynek tygodniowy: część z kartą, część bez karty, łącznie minimum 45
   godzin, następnie kontrola kwalifikacji, rekompensat i raportu.

Po tych testach pozostaje zebrać raport diagnostyczny i PDF, potwierdzić brak
rozbieżności po ponownym uruchomieniu aplikacji i oznaczyć wersję jako kandydat
do kolejnego etapu publikacji.
