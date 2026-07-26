# M3.7 — Planer: ergonomia wprowadzania danych

**Projekt:** ETS2 EU Digital Tachograph  
**Wydanie docelowe:** `0.1.0-beta.12`  
**Baza:** `0.1.0-beta.11.1`  
**Data planu:** 26 lipca 2026  
**Status początkowy:** **NIE ROZPOCZĘTY**  
**Kryterium wejścia:** Zamknięty smoke M3.6 (wynik **GO**).  
**Kryterium wyjścia:** Wszystkie zadania zamknięte, pełna regresja zielona, ręczny gate wizualny zaakceptowany.  
**Następny etap:** M4  
**Gałąź robocza:** `feature/planner-input-ergonomics`

> Ten dokument jest samodzielnym wydzieleniem etapu M3.7 z planu wydania beta.12.
> Nie zmienia zakresu ani gate'ów planu nadrzędnego. Etap został wstawiony między
> M3.6 a M4 decyzją właściciela z 2026-07-26.

**Cel:** zredukować ilość ręcznego wpisywania w zakładce PLANER. Ocena jednej
oferty wymaga dziś wypełnienia ośmiu pól tekstowych w formacie `HH:MM`, czyli
około czterdziestu uderzeń w klawiaturę i dwunastu tabulacji — co przy grze na
klawiaturze i przełączaniu Alt+Tab czyni planer praktycznie nieużywalnym.

## Dlaczego przed M4, a nie po publikacji

M4 wprowadza formalny **UI freeze** i wymaga zamkniętej inwentaryzacji UI, której
zakres obejmuje Planer. Wykonanie tych zmian **po** M4 oznaczałoby albo złamanie
polityki freeze'u, albo wykonanie inwentaryzacji UI dwa razy. W chwili podjęcia
decyzji M4 miał status *nie rozpoczęty*, więc okno na zmiany układu jest wciąż
otwarte i etap wchodzi bez naruszenia jakiejkolwiek bramki.

## Zakres

Wyłącznie warstwa wprowadzania danych zakładki PLANER: `JourneyPlannerViewModel`
oraz sekcja `TabItem` „PLANER" w `MainWindow.xaml`.

**Poza zakresem:** silnik planowania, kontrakty `DeliveryPlanning*`,
`DeliveryPlannerService`, prezentacja wyniku (tabela segmentów, ostrzeżenia,
podsumowanie, werdykt). Żadna zmiana nie modyfikuje sposobu liczenia planu.

## Decyzje przyjęte 2026-07-26

- Makieta panelu wejściowego **zaakceptowana** przez właściciela.
- Minuty w oknie dostawy: pełna lista `00`–`59`, bez kroku pięciominutowego —
  wyszukiwanie po wpisywanym tekście w `ComboBox` daje szybkość bez utraty
  precyzji, gdyby gra podała okno o nieokrągłej minucie.
- `TightMargin`: **wystawiony w UI** jako `PRÓG „NA STYK"`, nie usuwany z modelu.
  Kontrakt `TightMarginThresholdMinutes` pozostaje bez zmian.
- Wejście względne („dostawa za HH:MM") **odrzucone** dla ofert z rynku. ETS2
  prezentuje okno dostawy jako dni tygodnia, więc obecny model dzień + godzina
  jest zgodny z ekranem gry i konwersja byłaby dodaniem pracy, nie ujęciem.
- Autouzupełnianie z telemetrii (`delivery.time`, `planned_distance.km`)
  **nie wchodzi** do beta.12 — wymaga protokołu v4 i jednoczesnej wymiany DLL.
  Pozycja pozostaje w backlogu po publikacji.

## Zadania

### Krok 1 — poprawki tanie (jeden commit)

- [ ] Zastąpić domyślne wartości resztkowe okrągłymi i realistycznymi:
      dojazd po ładunek `01:00`, odbiór `00:15`, rozładunek `00:30`,
      praca po dostawie `00:00`.
- [ ] Ustawić `IsDefault="True"` na przycisku `OBLICZ PLAN` — Enter uruchamia
      obliczenie.
- [ ] Nadać `TabIndex` zgodny z kolejnością czytania panelu.
- [ ] Rozszerzyć `TryParseDuration` o zapis bez dwukropka (`90`), zapis
      godzinowy (`1h30`) i ułamkowy (`1,5` / `1.5`); tolerować białe znaki.
- [ ] Testy jednostkowe nowych formatów w `JourneyPlannerViewModelTests`.

### Krok 2 — okno dostawy z list rozwijanych

- [ ] Zamienić `WindowStartTime` i `WindowEndTime` ze `string` na godzinę i
      minutę jako `int`.
- [ ] Wystawić w XAML trzy listy w rzędzie: dzień, godzina `00`–`23`,
      minuta `00`–`59`, dla obu krańców okna.
- [ ] Usunąć `TryParseClock` i odwołania do niego w `ParseCommon`
      oraz `RefreshInputPreviews`.
- [ ] Skorygować komunikat walidacji — fragment o zakresie `00:00–23:59`
      przestaje mieć zastosowanie.

### Krok 3 — presety, steppery i walidacja per pole

- [ ] Presety pod polami czasu trwania: `15m`, `30m`, `1h`, `2h`
      (zestaw dobrany do pola), wzorem „SZYBKI ZAKRES" z zakładki Raporty.
- [ ] Strzałki ↑↓ zmieniają wartość o 5 minut, PgUp/PgDn o 60.
- [ ] Czerwona ramka na polu z niepoprawnym czasem trwania oraz nazwa pola
      w komunikacie walidacji zamiast komunikatu zbiorczego.
- [ ] Wystawić `PRÓG „NA STYK"` jako pole edytowalne.

### Krok 4 — trwałość i stabilność wpisywania

- [ ] Zapamiętywać wejścia planera między sesjami wraz ze znacznikiem
      pochodzenia wartości; wzorzec jak `SaveDeviceState`.
- [ ] Usunąć wymazywanie wyniku i migotanie przycisku przy każdym znaku:
      `UpdateSourceTrigger=LostFocus` albo debounce w `InputChanged`.
- [ ] Sprawdzić, czy zmiana nie przywraca zachowania naprawionego
      w `1c87e00` (refresh storm planera).

## Gate M3.7

- wszystkie zadania kroków 1–4 zamknięte;
- pełna regresja automatyczna zielona, build Release 0 błędów i 0 ostrzeżeń;
- brak otwartych P0/P1;
- ręczny gate wizualny: oba tryby planera, oba sloty, komplet pól, walidacja
  błędnego czasu trwania, Enter, tabulacja przez cały panel, restart aplikacji
  z odtworzeniem zapamiętanych wejść;
- brak błędów bindingów w logu diagnostycznym.

## Wpływ na dalsze etapy

- **M4** rusza dopiero po **GO** dla M3.7. Inwentaryzacja UI obejmie już nowy
  wygląd panelu — inaczej zamrozilibyśmy stary układ i wykonali inwentaryzację
  dwukrotnie.
- **M5** dostaje kilka nowych stringów do `pl-PL` i `en-GB`: etykiety presetów,
  `PRÓG „NA STYK"`, znacznik zapamiętanych wartości oraz nowe komunikaty
  walidacji per pole.
- **M7** pozostaje wymagany w pełnym zakresie. Zmiany dotykają XAML i modelu
  widoku, więc smoke M3.6 na artefakcie `rc3` **nie pokrywa** tego etapu.

## Ryzyka

- Zmiana typu właściwości okna dostawy dotyka publicznego API modelu widoku.
  Ryzyko ograniczone: istniejące testy `JourneyPlannerViewModelTests` odwołują
  się wyłącznie do `TryParseDuration`, żaden nie używa `WindowStartTime`
  ani `WindowEndTime`.
- Krok 4 dotyka ścieżki odświeżania, która była już źródłem awarii
  wydajnościowej (`1c87e00`). Wymaga sprawdzenia pod obciążeniem telemetrii,
  nie tylko w teście jednostkowym.
- Rozszerzony parser czasów trwania musi pozostać odporny na przepełnienie —
  obecna implementacja używa `checked` i ten warunek nie może zniknąć.
