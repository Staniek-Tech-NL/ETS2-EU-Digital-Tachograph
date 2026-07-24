# Raport prac rozwojowych UI

**Projekt:** ETS2 EU Digital Tachograph  
**Data:** 23 lipca 2026  
**Status:** wdrożone lokalnie, bez publikacji wersji beta

## 1. Cel prac

Celem prac było rozwinięcie i uporządkowanie interfejsu aplikacji bez zmiany
kanonicznej historii aktywności, zasad RuleEngine, telemetrii ani formatu
utrwalania rozliczonych luk.

Zakres objął:

1. korektę prezentacji licznika `ODP. TYG.`;
2. zastąpienie ręcznego pola kraju kontrolką wyboru;
3. wprowadzenie katalogu państw ISO 3166-1 alpha-2;
4. przebudowę karty wpisu manualnego według zatwierdzonego wariantu B;
5. poprawki uruchomieniowe i czytelności nowego widoku;
6. rozszerzenie testów automatycznych.

## 2. Licznik `ODP. TYG.`

Ustalono i wdrożono prezentację:

```text
pełne zakończone okresy 24 h / 6 (dokładny czas telemetryczny)
```

Przykład:

```text
89:39 → 3/6 (89:39)
```

> Aktualizacja 24 lipca 2026: powyższy format jest historycznym wynikiem prac
> z 23 lipca. Hotfix beta.12 zastąpił go licznikiem bieżącego okresu 1-based
> i stałym terminem `game_time`: `89:39 → 4/6 (D141 22:55)`.

Licznik jest obliczany przez dzielenie całkowite surowej liczby minut przez
1440. Nie stosuje się zaokrąglania dni w górę. Po przekroczeniu 144 godzin
prezentowany jest zapis `6/6+ (HH:MM)`.

Zmiana dotyczy wyłącznie formatowania LCD. Źródło czasu, telemetria i
RuleEngine pozostały bez zmian.

## 3. Wybór kraju

Pole tekstowe kodu kraju zostało zastąpione listą wyboru używaną przy:

- rozpoczęciu zmiany;
- zakończeniu zmiany;
- czytniku 1;
- czytniku 2.

Lista:

- korzysta z pełnego katalogu ISO 3166-1 alpha-2;
- pokazuje kod i polską nazwę, np. `PL — Polska`;
- przechowuje stabilny kod ISO;
- zachowuje osobny kod tachografowy używany na LCD;
- wspiera wyszukiwanie z klawiatury;
- zapamiętuje ostatni kraj dla danej karty;
- nie pozwala zatwierdzić operacji bez poprawnego wyboru.

Dane krajów są przechowywane poza XAML:

- `src/ETS2Tachograph.Desktop/Data/Countries.iso3166-1.json`;
- `src/ETS2Tachograph.Desktop/Resources/CountryNames.pl.json`.

Logika katalogu znajduje się w:

- `src/ETS2Tachograph.Desktop/ViewModels/CountryCatalog.cs`.

## 4. Wpis manualny — wariant B

Zatwierdzona makieta została wdrożona jako docelowy układ dialogu.

### 4.1. Stan początkowy

Po otwarciu dialogu cała luka jest wypełniana jednym segmentem:

```text
Przerwa / Odpoczynek
```

Edycja odbywa się na kopii roboczej. Historia nie jest zmieniana przed
wybraniem przycisku `ZATWIERDŹ WPIS`.

### 4.2. Szybkie akcje

Dodano trzy operacje zastępujące cały plan jednym segmentem:

- `PRZERWA / ODPOCZYNEK`;
- `INNA PRACA`;
- `DYSPOZYCYJNOŚĆ`.

### 4.3. Dodawanie i zastępowanie segmentów

Użytkownik wybiera:

- aktywność;
- dzień i godzinę rozpoczęcia;
- dzień i godzinę zakończenia.

Zakres jest ograniczony do rozliczanej luki i obsługuje przejście przez
północ. Czas jest liczony z dokładnością jednej minuty.

Wstawienie segmentu automatycznie dzieli istniejący plan. Przykład:

```text
Odpoczynek 00:00–00:30
+ Inna praca 00:10–00:20
=
Odpoczynek 00:00–00:10
Inna praca 00:10–00:20
Odpoczynek 00:20–00:30
```

Sąsiadujące segmenty tej samej aktywności są automatycznie scalane.

### 4.4. Edycja i usuwanie

- przycisk `EDYTUJ` wczytuje segment do formularza;
- dwukrotne kliknięcie wiersza uruchamia tę samą edycję;
- zmniejszenie segmentu pracy lub dyspozycyjności przywraca odpoczynek
  w zwolnionym zakresie;
- `USUŃ` zastępuje segment pracy lub dyspozycyjności odpoczynkiem;
- usuwanie domyślnego segmentu odpoczynku jest zablokowane;
- po zmianach wykonywane jest ponowne automatyczne scalanie.

### 4.5. Walidacja i podsumowanie

Dialog aktualizuje na bieżąco:

- pokrycie luki;
- brakujące minuty;
- nakładanie zakresów;
- łączny odpoczynek;
- łączną inną pracę;
- łączną dyspozycyjność;
- status kompletności wpisu.

Przycisk `ZATWIERDŹ WPIS` jest dostępny tylko dla kompletnego planu.
Anulowanie zmienionego planu wymaga potwierdzenia odrzucenia zmian.

### 4.6. Zachowane kontrakty

Przebudowa nie zmieniła:

- semantyki `SourceGapId`;
- kanonicznej historii aktywności;
- działania RuleEngine;
- kwalifikowania ciągłości odpoczynku;
- zapisu bazy danych;
- raportów i eksportu;
- źródła danych telemetrycznych.

## 5. Poprawki po wdrożeniu

### 5.1. Błąd uruchamiania aplikacji

Po pierwszej przebudowie aplikacja nie uruchamiała się z powodu powiązań
`Run.Text`, które WPF interpretował jako dwukierunkowe i próbował zapisywać
wartości do właściwości tylko do odczytu.

Powiązania podsumowania zostały ustawione jawnie jako `Mode=OneWay`.

Po poprawce:

- aplikacja została rzeczywiście uruchomiona diagnostycznie;
- proces pozostał aktywny;
- instancję diagnostyczną zamknięto po potwierdzeniu prawidłowego startu.

### 5.2. Czytelność list rozwijanych

W sekcji `DODAJ LUB ZASTĄP SEGMENT` biały tekst list rozwijanych był
nieczytelny na jasnym tle.

Zmieniono kolor tekstu na czarny oraz ustawiono białe tło dla:

- listy aktywności;
- listy dnia rozpoczęcia;
- listy dnia zakończenia.

## 6. Testy

Dodano testy edytora planu wpisu manualnego obejmujące:

- domyślne pokrycie odpoczynkiem;
- dzielenie segmentu;
- zastępowanie zakresu przecinającego kilka segmentów;
- scalanie sąsiadujących aktywności;
- usuwanie segmentu i przywracanie odpoczynku;
- blokadę usunięcia odpoczynku;
- edycję oraz zwrot zwolnionych minut do odpoczynku;
- brak modyfikacji planu po odrzuconej edycji;
- walidację zakresu;
- odrzucenie niedozwolonej aktywności;
- zachowanie wszystkich trzech dozwolonych aktywności przy zapisie.

Końcowy wynik pełnej regresji:

```text
310 testów zaliczonych
0 testów niezaliczonych
0 błędów kompilacji
0 ostrzeżeń kompilacji
```

## 7. Najważniejsze zmienione pliki

- `src/ETS2Tachograph.Desktop/ViewModels/MainViewModel.cs`
- `src/ETS2Tachograph.Desktop/ViewModels/ManualEntryPlanEditor.cs`
- `src/ETS2Tachograph.Desktop/ViewModels/CountryCatalog.cs`
- `src/ETS2Tachograph.Desktop/Views/MainWindow.xaml`
- `src/ETS2Tachograph.Desktop/Views/MainWindow.xaml.cs`
- `src/ETS2Tachograph.Desktop/Data/Countries.iso3166-1.json`
- `src/ETS2Tachograph.Desktop/Resources/CountryNames.pl.json`
- `tests/ETS2Tachograph.Desktop.Tests/ManualEntryPlanEditorTests.cs`
- `tests/ETS2Tachograph.Desktop.Tests/CountryCatalogTests.cs`

## 8. Status końcowy

Prace zostały wykonane lokalnie w katalogu projektu. Nie wykonano:

- publikacji wersji beta;
- wdrożenia produkcyjnego;
- wysłania zmian do zdalnego repozytorium;
- modyfikacji danych użytkownika.

Aktualny stan nadaje się do dalszych lokalnych testów funkcjonalnych i
wizualnych.
