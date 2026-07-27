# Instalacja - ETS2 Digital Tachograph

## Wymagania

- Windows x64;
- Euro Truck Simulator 2;
- paczka aplikacji `win-x64` zawierająca katalog aplikacji i katalog `plugin`.

Aplikacja jest publikowana jako self-contained, więc nie wymaga osobnej
instalacji .NET.

## 1. Rozpakowanie aplikacji

1. Pobierz właściwy ZIP wydania.
2. Rozpakuj całą zawartość do wybranego katalogu. Nie uruchamiaj aplikacji
   bezpośrednio z archiwum.
3. Nie przenoś pojedynczych plików między różnymi wersjami paczki.

## 2. Instalacja pluginu SCS

1. Zamknij ETS2.
2. W rozpakowanej paczce znajdź:

   ```text
   plugin\ETS2Tachograph.ScsPlugin.dll
   ```

3. Jeżeli Windows oznaczył DLL jako pobraną z Internetu, kliknij ją prawym
   przyciskiem, wybierz **Właściwości**, zaznacz **Odblokuj** i zatwierdź.
4. Skopiuj DLL do:

   ```text
   Euro Truck Simulator 2\bin\win_x64\plugins\
   ```

   Najczęstsza ścieżka instalacji Steam:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\plugins\
   ```

5. Uruchom ETS2 i zaakceptuj komunikat o użyciu SDK.

Po każdej wymianie pluginu uruchom grę ponownie. Polecenie `sdk reload` jest
przeznaczone wyłącznie do pracy deweloperskiej.

## 3. Pierwsze uruchomienie

1. Uruchom `ETS2Tachograph.Desktop.exe` z rozpakowanego katalogu aplikacji.
2. Poczekaj, aż status połączenia potwierdzi aktywną telemetrię z ETS2.
3. Językiem domyślnym pierwszego uruchomienia jest polski.
4. Aby wybrać angielski, przejdź do **Ustawienia**, wybierz
   **English (United Kingdom)**, zapisz ustawienia i uruchom aplikację ponownie.

Zmiana języka obowiązuje po restarcie aplikacji. Raport PDF używa języka
aktywnego w chwili eksportu.

## Dane i diagnostyka

Dane użytkownika są przechowywane poza katalogiem aplikacji:

```text
%LocalAppData%\ETS2Tachograph\
```

Najważniejsze elementy:

- `tachograph.db` - baza SQLite;
- `tachograph.db.bak.RRRRMMDD-GGMMSS-fff` - kopie sprzed migracji;
- `ui-culture.json` - wybrany język interfejsu;
- `Logs\tachograph-RRRR-MM-DD.log` - log diagnostyczny;
- `Printouts\` - wydruki wirtualnego urządzenia.

Aktualizacja plików aplikacji nie usuwa tej bazy. Przed ręcznym usuwaniem
katalogu danych wykonaj jego kopię.

## Najczęstsze problemy

- **Brak połączenia z ETS2:** sprawdź położenie DLL, zaakceptowanie SDK
  i uruchom grę ponownie.
- **Niezgodna wersja protokołu:** zastąp plugin DLL wersją z tej samej paczki
  co aplikacja i ponownie uruchom ETS2.
- **Aplikacja nie startuje:** odczytaj ścieżkę pokazaną w komunikacie błędu
  albo sprawdź najnowszy plik w katalogu `Logs`.
- **Potrzebne dane do zgłoszenia:** na Dashboardzie wybierz
  **Raport diagnostyczny** i zachowaj utworzony ZIP.

Program jest symulatorem wspomagającym rozgrywkę. Nie jest certyfikowanym
tachografem ani narzędziem do rozliczeń prawnych lub pracowniczych.
