# Raport danych widocznych w UI — ETS2 EU Digital Tachograph

**Nazwa pliku:** zachowana ze względów zgodności z wcześniejszymi odnośnikami
**Baza wydaniowa:** 0.1.0-beta.11.1
**Stan lokalny:** nieopublikowany wariant B i katalog ISO
**Zakres:** wyłącznie informacje, które użytkownik może odczytać z interfejsu WPF,
ekranu tachografu, jego menu albo nakładek. Raport nie obejmuje danych dostępnych
jedynie w bazie SQLite, kodzie lub wewnętrznym stanie silnika.

## Aktualizacja bieżąca

Do widocznych danych i operacji doszły:

- `ODP. TYG.` w formacie ukończonych okresów 24 h oraz dokładnego czasu,
  np. `3/6 (89:39)`;
- przeszukiwalny wybór kraju w formacie `PL — Polska`, z zapamiętywaniem
  ostatniego wyboru per karta;
- wariant B wpisu manualnego: kontekst kierowcy i luki, szybkie akcje, pełna
  lista segmentów, edycja dnia/godziny, sumy aktywności i status pokrycia;
- przycisk `ZATWIERDŹ WPIS` aktywny wyłącznie dla kompletnego planu.

Lista kraju prezentuje kod ISO i nazwę, natomiast LCD może używać krótszego kodu
tachografowego. Tekst list rozwijanych edytora segmentu jest czarny na jasnym
tle. Dalsze sekcje opisujące wcześniejsze ekrany pozostają bazą funkcjonalną,
ale nazwy starych przycisków kreatora manualnego nie mają pierwszeństwa nad
niniejszą aktualizacją i `BETA_TEST_PLAN.md`.

## 1. Górny pasek aplikacji

Na każdym ekranie widoczny jest stan połączenia z ETS2:

- `Oczekiwanie na ETS2...` — brak aktywnej ramki telemetrii;
- `ETS2 · telemetria aktywna` — gra działa i przesyła dane;
- `ETS2 · pauza` — gra lub telemetria jest zatrzymana;
- komunikat błędu telemetrii — na przykład niezgodna wersja pluginu.

Pasek pokazuje również nazwę aplikacji `ETS2 TACHO` i rolę
`KIEROWCA / ADMINISTRATOR`.

## 2. Dashboard — ekran LCD tachografu

Na głównym wyświetlaczu można odczytać:

| Informacja | Przykład | Znaczenie / źródło |
|---|---|---|
| Godzina | `14:30` | Obecnie zegar systemu Windows w formacie `HH:mm` |
| Prędkość | `0 km/h` | Aktualna prędkość z telemetrii ETS2 |
| Aktywność slotu 1 | `KIEROWNICA`, `MŁOTKI`, `GOTOWOŚĆ`, `ŁÓŻKO` | Bieżąca aktywność pierwszej karty |
| Aktywność slotu 2 | analogiczne oznaczenia | Bieżąca aktywność drugiej karty |
| Licznik kilometrów | np. `123456.7 km` | Licznik aplikacji, zapamiętywany między uruchomieniami |
| Stan karty 1 | `K1` albo `BRAK K1` | Czy w czytniku 1 znajduje się karta |
| Trwająca pauza S1 | `P 00:17 > 00:28` | Czas pauzy i czas pozostały do wybranego celu |

Podczas jazdy bez karty w slocie 1 ekran naprzemiennie pokazuje:

- `! JAZDA BEZ KARTY !`;
- `X BŁĄD KARTY 1 X`.

Ekran pokazuje również stany specjalne:

- odczytywanie wkładanej karty i nazwę kierowcy;
- `DRUKOWANIE...` oraz rodzaj wydruku;
- `! WPIS MANUALNY !`, numer slotu i informację o blokadzie;
- aktualnie wybraną pozycję menu.

### Uwaga o czasie

Główny LCD pokazuje obecnie godzinę systemową Windows. Czas gry ETS2 jest używany
w historii, raportach i kreatorze wpisu manualnego, gdzie występuje jako
`Dzień X, HH:mm`.

## 3. Dashboard — karta kierowcy w slocie 1

W dolnej karcie `SLOT 1 — KIEROWCA AKTYWNY` widoczne są:

- imię i nazwisko właściciela włożonej karty;
- aktualna aktywność kierowcy;
- czas pozostały do wymaganej przerwy w jeździe;
- wybrany cel pauzy lub odpoczynku;
- czas trwania bieżącej pauzy;
- czas pozostały do wybranego celu;
- stan celu: `OCZEKUJE`, `W TRAKCIE` albo `ZALICZONA`.

Dostępne cele pauzy i odpoczynku:

- przerwa 15 minut — część pierwsza;
- przerwa 30 minut — część druga;
- pełna przerwa 45 minut;
- odpoczynek dzienny 9 godzin;
- odpoczynek dzienny 11 godzin;
- odpoczynek tygodniowy 24 godziny;
- odpoczynek tygodniowy 45 godzin.

Cel wybrany w UI steruje prezentacją postępu. Faktyczna kwalifikacja prawna wynika
z rzeczywistej długości zarejestrowanego odpoczynku.

## 4. Dashboard — karta kierowcy w slocie 2

W karcie `SLOT 2 — KIEROWCA ZMIENNIK` można odczytać:

- imię i nazwisko kierowcy;
- aktualną aktywność;
- jazdę ciągłą;
- czas pozostały do przerwy;
- wybrany cel pauzy;
- czas trwania pauzy;
- czas pozostały;
- status pauzy.

Jeżeli pojazd jedzie, slot 2 automatycznie używa dozwolonej przerwy 45 minut.
Wtedy UI może pokazać:

- `W TRAKCIE · W RUCHU`;
- `ZALICZONA · W RUCHU`.

## 5. Panel alertów i komunikatów

Panel `ALERTY` pokazuje:

- naruszenia kierowcy 1 z oznaczeniem `K1`;
- naruszenia kierowcy 2 z oznaczeniem `K2`;
- artykuł lub identyfikator reguły;
- typ naruszenia;
- wymagany wpis manualny po wyjęciu karty;
- informację, że jazda jest zablokowana logicznie do czasu rozliczenia;
- opcjonalną lukę po skoku czasu;
- wybrany podział wpisu manualnego;
- wynik kwalifikacji odpoczynku;
- komunikaty powodzenia albo błędu ostatniej operacji.

Przykładowe komunikaty operacyjne:

- karta została włożona lub wysunięta;
- zapisano kraj rozpoczęcia lub zakończenia;
- rozpoczęto wybraną pauzę;
- wpis manualny został zapisany;
- raport został utworzony;
- ustawienia zostaną zastosowane po restarcie;
- operacja została odrzucona podczas jazdy.

## 6. Menu fizycznego tachografu

Menu otwiera przycisk `OK`. Strzałki wybierają pozycję, `OK` zatwierdza, a `C`
wraca. Z poziomu menu można odczytać więcej liczników niż na stałych kartach
dashboardu.

### Menu główne

- Wydruk;
- Wpis manualny;
- Pauza / odpoczynek;
- Kraje;
- Tryby;
- Liczniki kart;
- Ustawienia.

### Liczniki karty 1 i karty 2

Dla każdej karty osobno menu pokazuje:

| Licznik | Znaczenie |
|---|---|
| `PAUZA` | Czas trwania aktualnej pauzy |
| `CEL` | Czas pozostały do wybranego celu pauzy |
| `CIĄGŁA` | Jazda ciągła od ostatniej zaliczonej przerwy |
| `DO PRZERWY` | Czas pozostały do wymaganej przerwy |
| `DZIENNA` | Łączna jazda w aktualnym okresie dobowym |
| `TYDZIEŃ` | Łączna jazda tygodniowa |
| `2 TYG.` | Łączna jazda w okresie dwutygodniowym |
| `ODP. DZIENNY` | Czas pozostały do terminu odpoczynku dziennego |
| `ODP. TYG.` | Czas pozostały do terminu odpoczynku tygodniowego |

Ekran wyboru karty pokazuje też, czy `KARTA 1` i `KARTA 2` są gotowe, czy ich brak.

### Kraje

Menu pokazuje aktualnie wybrane:

- państwo rozpoczęcia pracy;
- państwo zakończenia pracy.

Dostępne kody: `PL`, `DE`, `CZ`, `SK`, `AT`, `FR`, `NL`, `BE`, `ES`, `IT`,
`DK`, `SE`, `NO`, `FI`, `LT`, `LV`, `EE`.

### Tryby

Można odczytać, czy włączone są:

- `OUT`;
- `PROM`.

UI pokazuje również, czy tachograf pracuje jako:

- pojedyncza obsada — okno 24 godziny;
- podwójna obsada — okno 30 godzin.

## 7. Nakładki na grę S1 i S2

Nakładki są wywoływane skrótami:

- `Alt+1` — karta ze slotu 1;
- `Alt+2` — karta ze slotu 2;
- `Alt+Q` — dodatkowy skrót dla slotu 1.

Każda nakładka pokazuje wyłącznie dane swojego slotu:

- oznaczenie `S1` lub `S2`;
- bieżącą aktywność;
- jazdę ciągłą;
- czas pozostały do przerwy;
- jazdę dzienną;
- wybrany cel pauzy;
- czas trwania pauzy;
- czas pozostały do celu;
- status pauzy;
- tryb zwykły, OUT albo Prom;
- pojedynczą lub podwójną obsadę;
- stan połączenia z ETS2.

Położenie każdej nakładki jest zapisywane osobno i można je zmienić, przeciągając
jej górny pasek.

## 8. Ekran Historia

Tabela `HISTORIA AKTYWNOŚCI · WSZYSTKIE KARTY` pokazuje:

- identyfikator karty;
- początek aktywności jako czas gry `Dzień X, HH:mm`;
- koniec aktywności jako czas gry;
- rodzaj aktywności;
- źródło danych;
- warunek specjalny.

Możliwe źródła widoczne w tabeli obejmują między innymi:

- `Telemetry`;
- `Reconstructed`;
- `ManualEntry`;
- `Mixed` dla zagregowanych danych o mieszanym źródle.

Warunek specjalny informuje między innymi o odcinku promowym. Historia obejmuje
wszystkie znane profile i karty, a po imporcie lub rozliczeniu luki jest odświeżana.

Na tym ekranie dostępne są również operacje eksportu `.tacho` i importu `.tacho`.

## 9. Ekran Raporty

Przed wygenerowaniem raportu UI pozwala odczytać lub wybrać:

- profil kierowcy i jego kartę;
- początek zakresu w czasie gry;
- koniec zakresu w czasie gry.

Po wygenerowaniu raportu widoczne jest podsumowanie:

- łączna Jazda;
- łączna Inna praca;
- łączna Dyspozycyjność/Gotowość;
- łączny Odpoczynek;
- liczba naruszeń;
- liczba rekordów raportu w komunikacie stanu.

Tabela raportu pokazuje:

- kartę;
- czas gry od/do;
- aktywność;
- źródło;
- warunek specjalny.

Z tego ekranu można wygenerować:

- surowy CSV;
- raport PDF;
- VTC JSON.

## 10. Ekran Kierowcy

Lista profili pokazuje:

- nazwę kierowcy;
- informację, czy profil jest aktywny;
- datę i godzinę utworzenia profilu.

Formularz nowego profilu pokazuje wprowadzane:

- imię/nazwę kierowcy;
- numer nowej karty;
- wynik operacji tworzenia lub aktywowania profilu.

## 11. Ekran Ustawienia

UI pokazuje dwie wartości konfiguracyjne:

- próg prędkości wykrywania Jazdy w km/h;
- przesunięcie początku tygodnia regulacyjnego w dniach.

Po zapisaniu widoczna jest informacja, że ustawienia zostaną zastosowane przy
następnym uruchomieniu aplikacji.

## 12. Okno wkładania i wyjmowania karty

Okno karty pokazuje:

- numer obsługiwanego slotu;
- czy operacja dotyczy wkładania czy wysuwania;
- listę dostępnych profili/kart;
- nazwę wybranego kierowcy;
- kod kraju rozpoczęcia lub zakończenia;
- komunikat powodzenia albo przyczynę odrzucenia.

Podczas krótkiego procesu odczytu nazwisko kierowcy i pasek postępu pojawiają się
również na LCD tachografu.

## 13. Kreator wpisu manualnego

Kreator pokazuje:

- czy wpis jest wymagany, czy opcjonalny;
- slot, którego dotyczy;
- początek luki w czasie gry;
- koniec luki w czasie gry;
- całkowitą długość luki;
- wprowadzone bloki Innej pracy: od, do i czas trwania;
- łączny wybrany odpoczynek;
- łączną Inną pracę;
- oczekiwaną lub faktyczną kwalifikację odpoczynku;
- błędy walidacji, na przykład dziura, nakładanie lub wyjście poza zakres.

Pozostałe minuty luki są domyślnie prezentowane jako Przerwa/Odpoczynek. Przy luce
po wyjęciu karty przycisk anulowania jest ukryty. Luka po skoku czasu może być
pozostawiona bez wpisu.

## 14. Dane dostępne w raportach plikowych, ale nie stale na ekranie

Po wyeksportowaniu PDF, CSV, JSON albo `.tacho` użytkownik może odczytać bardziej
szczegółowe dane niż w głównych kartach UI, w szczególności:

- pełne bloki historii;
- szczegółowe źródła rekordów;
- segmenty wpisów manualnych;
- naruszenia z badanego zakresu;
- dane minutowe w surowym CSV;
- powiązania wpisu z luką w `.tacho`.

## 15. Dane, których beta.4 nie pokazuje bezpośrednio

Poniższe informacje istnieją w silniku albo bazie, lecz obecny aktywny układ UI
nie prezentuje ich bezpośrednio lub prezentuje tylko częściowo:

- dokładny numer karty na stałej karcie dashboardu;
- kraj i termin ważności karty w tabeli profili;
- graficzny procent postępu pauzy — dostępne są czas i status, ale nie ma widocznego
  paska procentowego w aktywnym układzie;
- czas trwania bieżącej aktywności jako osobna wartość;
- czas gry ETS2 na głównym LCD — główny LCD używa obecnie zegara Windows;
- pełny opis naruszenia na ekranie Raporty — ekran pokazuje liczbę, a szczegóły są
  dostępne w alertach i wygenerowanym raporcie;
- jawna lista wszystkich nierozliczonych luk — UI pokazuje tylko lukę aktualnie
  wymagającą lub oferowaną do rozliczenia;
- surowy `highWaterMark`, numer sesji/gałęzi i identyfikatory techniczne;
- statystyki retencji hot/warm;
- pełny ślad rekompensat tygodniowych.

## 16. Skrócona lista najważniejszych odczytów

Z poziomu UI użytkownik może obecnie sprawdzić:

1. połączenie z ETS2, pauzę i błędy pluginu;
2. prędkość, godzinę, przebieg oraz stan obu slotów;
3. aktywność obu kierowców;
4. jazdę ciągłą i czas do przerwy;
5. jazdę dzienną, tygodniową i dwutygodniową przez menu;
6. terminy odpoczynku dziennego i tygodniowego przez menu;
7. cel, przebieg i pozostały czas pauzy obu kierowców;
8. tryby OUT, Prom oraz pojedynczą/podwójną obsadę;
9. kraj rozpoczęcia i zakończenia;
10. naruszenia i wymagane wpisy manualne;
11. historię aktywności wszystkich kart w czasie gry;
12. sumy raportowe Jazda/Praca/Gotowość/Odpoczynek i liczbę naruszeń;
13. profile kierowców oraz ustawienia tachografu;
14. status każdej wykonanej operacji.
