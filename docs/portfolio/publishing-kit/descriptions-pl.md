# Polskie opisy portfolio

Każda pozycja ma trzy długości do wykorzystania na różnych platformach. Liczba
znaków może się nieznacznie zmienić po normalizacji odstępów.

## 1. ETS2 EU Digital Tachograph — kompletna aplikacja

### Krótki (~300 znaków)

```text
Desktopowy symulator tachografu, który zamienia telemetrię Euro Truck Simulator 2 w audytowalną historię kierowców, liczniki czasu jazdy, narzędzia planowania i raporty PDF/CSV/JSON. Projekt łączy C++, C#/.NET 9, WPF, SQLite, EF Core, testy automatyczne oraz GitHub Actions.
```

### Średni (~600 znaków)

```text
ETS2 EU Digital Tachograph to lokalna aplikacja Windows, która przekształca telemetrię gry w audytowalną historię aktywności dwóch wirtualnych kierowców. Natywny plugin C++ publikuje wersjonowany protokół pamięci współdzielonej odczytywany przez warstwową aplikację .NET 9/WPF. System obsługuje cofanie czasu gry, liczniki jazdy i odpoczynku, ręczną rekonstrukcję historii, SQLite, nakładki, planowanie oraz eksport PDF/CSV/JSON. Proces wydania obejmuje backupy, diagnostykę, kontrolę zgodności protokołu, GitHub Actions i bramkę 571 testów. To symulator do ETS2, a nie certyfikowany tachograf.
```

### Rozszerzony (~1200 znaków)

```text
ETS2 EU Digital Tachograph to lokalny symulator desktopowy dla Windows, który zamienia telemetrię Euro Truck Simulator 2 w audytowalną historię aktywności kierowców, analizę czasu jazdy, narzędzia planowania i czytelne raporty. Rozwiązanie łączy natywny plugin C++ korzystający z oficjalnego SCS SDK z warstwową aplikacją C#/.NET 9 i WPF.

Najtrudniejszym problemem była wiarygodność czasu. Czas w ETS2 może przeskoczyć, cofnąć się albo zostać zrestartowany. Zaprojektowałem wersjonowany protokół pamięci współdzielonej oraz sesyjny model truncate-and-append, który nie pozwala podwójnie policzyć porzuconej przyszłości. Historia kanoniczna zasila dwie karty, reguły, rekonstrukcję luk, Journey Planner, SQLite i raporty.

Produkt zawiera nakładki, lokalizację PL/EN, backupy migracji, diagnostykę, kontrolę pluginu oraz eksport PDF/CSV/JSON i sesji z sumą kontrolną. Bramka wydania obejmuje 571 testów domeny, telemetrii, usług, persistence, raportów i UI. Projekt pokazuje integrację C++/.NET, modelowanie czasu, UX desktopowy, transakcje, testy i dyscyplinę wydawniczą. Nie jest certyfikowanym tachografem.
```

## 2. Journey Planner — moduł planowania jazdy i odpoczynku

### Krótki (~300 znaków)

```text
Moduł planowania ograniczeniowego w ETS2 EU Digital Tachograph, który zamienia aktualny stan kierowcy w wyjaśnialną oś jazdy, przerw, odpoczynków, oczekiwania i marginesu dostawy. Powstał w C# jako deterministyczna logika domenowa z niezmiennymi snapshotami, limitami obliczeń, WPF i testami regresji.
```

### Średni (~600 znaków)

```text
Journey Planner to moduł ETS2 EU Digital Tachograph oceniający, czy wirtualny kierowca może zakończyć trasę w oknie dostawy przy zachowaniu zaimplementowanych ograniczeń jazdy i odpoczynku. Pobiera niezmienny snapshot historii kanonicznej i stanu reguł, a następnie buduje wyjaśnialną sekwencję jazdy, przerw, odpoczynków dziennych lub tygodniowych, oczekiwania kalendarzowego i pracy po przyjeździe. Kontrola tożsamości snapshotu odrzuca wynik, gdy zmieni się telemetria, karta, sesja lub luka. Jawne limity chronią przed nieograniczonym obliczeniem. Moduł pokazuje algorytmy biznesowe, bezpieczne przejścia stanu, WPF i testy wielu warstw.
```

### Rozszerzony (~1200 znaków)

```text
Journey Planner to moduł planowania ograniczeniowego zbudowany jako część ETS2 EU Digital Tachograph. Odpowiada na pytanie, którego nie rozwiązuje sam czas trasy: czy wirtualny kierowca ukończy przejazd w oknie dostawy z uwzględnieniem obsługiwanych limitów jazdy, przerw, odpoczynku dziennego i tygodniowego oraz granic kalendarza regulacyjnego?

Aplikacja przechwytuje niezmienny snapshot historii kanonicznej, oceny reguł, luk, czasu gry, generacji świata i sesji. Deterministyczny silnik tworzy segmenty jazdy, przerwy, odpoczynku, oczekiwania i pracy. Każdy przechowuje powód, a wynik pokazuje przyjazd, zakończenie, margines, ostrzeżenia, pewność i limity.

Planowanie jest tylko do odczytu i ma ograniczony koszt. Zmiana karty, sesji, telemetrii lub luki daje status nieaktualny zamiast błędnej rekomendacji. Limity segmentów, czasu i stanów zapobiegają nieskończonym obliczeniom. Moduł pokazuje modelowanie algorytmiczne, niezmienny stan, wyjaśnialne wyniki, oddzielenie domeny od WPF i testy regresji. Obraz planera jest makietą; eksport PDF nie jest deklarowany jako wdrożony.
```

## 3. Rekonstrukcja luk aktywności i wpis manualny

### Krótki (~300 znaków)

```text
Walidowany workflow w ETS2 EU Digital Tachograph do rekonstrukcji brakujących okresów aktywności. Użytkownik buduje pełne segmenty odpoczynku, pracy lub dyspozycyjności, a walidacja usługowa i atomowa transakcja SQLite chronią historię kanoniczną, pochodzenie danych, ponowienia i przeliczenie reguł.
```

### Średni (~600 znaków)

```text
Rekonstrukcja luk aktywności to moduł ETS2 EU Digital Tachograph dla okresów, w których wyjęta karta, brak telemetrii albo skok czasu pozostawiły brakujące dane. Edytor WPF obsługuje szybki wybór dla całej luki oraz precyzyjne plany z segmentów odpoczynku, innej pracy i dyspozycyjności. Blokuje niepokryte minuty, nakładanie, błędne zakresy i niedozwolone aktywności. Serwis ponownie waliduje plan wobec historii kanonicznej, po czym zapisuje rozliczenie i rekordy źródłowe w jednej transakcji SQLite. Identyczne ponowienie jest idempotentne, a konflikt jawnie odrzucany. Stan reguł jest przeliczany z zapisanej osi czasu.
```

### Rozszerzony (~1200 znaków)

```text
Rekonstrukcja luk aktywności to moduł edycji i walidacji zbudowany jako część ETS2 EU Digital Tachograph. Obsługuje okresy, w których wyjęcie karty kierowcy, niedostępna telemetria albo skok czasu gry do przodu uniemożliwiają wiarygodne określenie aktywności.

Workflow WPF pozwala sklasyfikować całą lukę albo zbudować plan z odpoczynku, pracy i dyspozycyjności. Edytor obsługuje zastępowanie, edycję, dzielenie, usuwanie i pokrycie. Zatwierdzenie jest niedostępne, dopóki każda minuta nie ma jednej klasyfikacji.

Poprawność nie zależy od UI. Serwis sprawdza typy, długości, pokrycie, kolejność, nakładanie, granice, gałąź kanoniczną i kolizje historii. EF Core zapisuje lukę i rekordy w jednej transakcji SQLite. Identyczne ponowienie jest idempotentne, a inna wersja daje konflikt. Rekordy zachowują źródło ManualEntry i identyfikator luki. Po zapisie historia jest ładowana ponownie, a reguły przeliczane. Moduł pokazuje złożony UX, wielopoziomową walidację, integralność czasu, transakcje i odporność na ponowienia.
```

## 4. Raportowanie, analityka i pipeline eksportu

### Krótki (~300 znaków)

```text
Moduł ETS2 EU Digital Tachograph zamieniający kanoniczną historię aktywności w czytelne statystyki, kontrolę kompletności, naruszenia i eksport PDF/CSV/JSON. Zachowuje dokładne dane diagnostyczne, a sąsiednie rekordy agreguje w zwarte, zrozumiałe bloki raportu.
```

### Średni (~600 znaków)

```text
Reporting & Analytics to moduł ETS2 EU Digital Tachograph przetwarzający kanoniczną historię kierowcy dla gotowych lub własnych zakresów czasu gry. Wylicza jazdę, pracę, dyspozycyjność, odpoczynek, OUT, naruszenia, rekompensaty i jawny stan kompletności. Baza zachowuje szczegółowe rekordy dla reguł i diagnostyki, natomiast warstwa PDF łączy sąsiednie aktywności w czytelne bloki. Surowy CSV zachowuje precyzję, VTC JSON udostępnia dane strukturalne, a format sesji z sumą kontrolną przechowuje sesje, luki i pochodzenie. Moduł pokazuje agregację czasu, model jakości danych, lokalizację i wiele formatów serializacji.
```

### Rozszerzony (~1200 znaków)

```text
Reporting & Analytics to moduł przetwarzania danych i eksportu zbudowany jako część ETS2 EU Digital Tachograph. Zamienia kanoniczną historię kierowcy w czytelne podsumowania, dowody kompletności, naruszenia, informacje o rekompensatach oraz formaty dla ludzi i integracji.

Pierwszy PDF minuta po minucie przekraczał 40 stron dla doby gry. Oddzieliłem prawdę zapisaną od prezentacji: precyzyjna aktywność pozostaje dla reguł i CSV, a raport scala sąsiednie rekordy i umieszcza luki na osi czasu. PDF jest zwarty bez utraty danych źródłowych.

Kompletność jest jawna: model udostępnia aktywność, luki, pokrycie, zakres, bilans, decyzje odpoczynku i status dowodowy. Zapytania używają tej samej gałęzi kanonicznej co RuleEngine, więc porzucone dane nie wracają. Wyniki obejmują lokalizowany PDF, CSV aktywności i rekompensat, VTC JSON oraz sesję chronioną SHA-256. Moduł pokazuje agregację czasu, projekcję zakresów, jakość danych, serializację, lokalizację i testy wielu warstw.
```
