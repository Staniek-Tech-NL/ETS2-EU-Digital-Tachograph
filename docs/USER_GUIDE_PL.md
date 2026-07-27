# Podstawowa instrukcja użytkowa - ETS2 Digital Tachograph

## Zanim rozpoczniesz jazdę

1. Uruchom ETS2 i wczytaj profil.
2. Uruchom `ETS2Tachograph.Desktop.exe`.
3. Poczekaj na aktywne połączenie z telemetrią.
4. Na Dashboardzie wybierz slot karty i włóż kartę kierowcy. Podaj kraj
   rozpoczęcia, jeżeli aplikacja o niego poprosi.

Nie prowadź bez właściwej karty w aktywnym slocie. Aplikacja automatycznie
wykrywa jazdę na podstawie prędkości.

## Dashboard i wirtualny tachograf

Dashboard pokazuje oba sloty, bieżącą aktywność, czas do przerwy, limity jazdy
i zobowiązania rekompensacyjne. Z jego szybkich akcji można odświeżyć raport,
utworzyć PDF, wyeksportować CSV zobowiązań oraz zapisać raport diagnostyczny.

Na wirtualnym tachografie:

- użyj strzałek do poruszania się po menu;
- użyj `OK` do zatwierdzania;
- wybierz pracę, dyspozycyjność albo odpoczynek, gdy aktywność nie jest jazdą;
- tryby OUT i prom włączaj tylko zgodnie z faktycznym przebiegiem sesji;
- przed wyjęciem karty zatrzymaj pojazd i wybierz kraj zakończenia.

## Przerwy, odpoczynki i drugi kierowca

W każdym slocie można wybrać cel odpoczynku i rozpocząć przerwę. Dla drugiego
kierowcy dostępna jest przerwa podczas jazdy, jeśli pozwala na to bieżący stan.
Źródłem kwalifikacji pozostaje RuleEngine; sam wybór celu nie gwarantuje
zaliczenia przerwy.

## Historia i wpis manualny

Widok **Historia** pokazuje aktywności i luki dla wybranej karty. Gdy po
ponownym włożeniu karty powstanie nierozliczona luka:

1. otwórz wpis manualny;
2. rozpisz cały brakujący przedział na odpoczynek, inną pracę lub
   dyspozycyjność;
3. sprawdź, czy segmenty pokrywają lukę bez nakładania i bez przerw;
4. zatwierdź wpis.

Z tego widoku można także importować pliki `.tacho` i eksportować sesję
kierowcy. Nie edytuj ręcznie plików `.tacho`.

## Rekompensaty

Widok **Rekompensaty** pokazuje otwarte, zaległe i rozliczone zobowiązania.
Sprawdzaj termin oraz przypisane odcinki odpoczynku. Status zmienia się na
podstawie zapisanej historii, nie przez ręczne oznaczenie w interfejsie.

## Raporty i eksport

W widoku **Raporty**:

1. wybierz kierowcę i zakres czasu;
2. odśwież analizę;
3. przejrzyj podsumowanie, aktywności, naruszenia, luki i rekompensaty;
4. wyeksportuj PDF, surowy CSV, CSV rekompensat albo VTC JSON.

PDF jest lokalizowany zgodnie z aktywnym językiem aplikacji. Formaty techniczne
CSV, JSON i `.tacho` nie zmieniają kontraktu po zmianie języka.

## Planer

Planer tworzy warianty podróży na podstawie bieżącego stanu kart, limitów,
przerw, odpoczynków i rekompensat. Traktuj wynik jako plan symulacyjny:

- rozwiąż wskazane luki lub brak karty przed planowaniem;
- sprawdź ostrzeżenia oraz gotowość planu;
- po zmianie historii lub ustawień wygeneruj plan ponownie;
- nie traktuj wyniku jako porady prawnej.

## Nakładki

- `Alt+1` - pokaż lub ukryj nakładkę slotu 1;
- `Alt+2` - pokaż lub ukryj nakładkę slotu 2;
- `Alt+Q` - dodatkowy skrót dla slotu 1.

Nakładkę można przeciągnąć za górny pasek. Pozycje `S1` i `S2` są zapisywane
oddzielnie.

## Język i ustawienia

W **Ustawieniach** można zmienić próg wykrywania jazdy, przesunięcie początku
tygodnia oraz język interfejsu. Zapisana zmiana języka zaczyna obowiązywać po
ponownym uruchomieniu aplikacji.

## Zgłaszanie problemu

1. Zapisz raport diagnostyczny z Dashboardu.
2. Zanotuj wersję aplikacji, wersję ETS2 i kroki prowadzące do błędu.
3. Dołącz ZIP diagnostyczny oraz zrzut ekranu, ale nie publikuj prywatnych
   danych kierowców.

Program jest symulatorem i nie jest certyfikowanym tachografem.
