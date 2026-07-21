# Raport z testów terenowych — 21 lipca 2026

**Wersja:** 0.1.0-beta.10
**Dzień testów:** 2
**Przedmiot:** reguła ciągłości odpoczynku przez rozliczoną lukę `CardRemoved` (beta.10)
**Dane wejściowe:** `raport-Doboś.csv`, `raport-Staniek.csv` — surowy eksport CSV historii minutowej

---

## 1. Werdykt

**Testy dnia 2 zaliczone na zielono.** Decyzja właściciela projektu z 21.07.2026.

Reguła ciągłości działa w zakresie objętym testem: odpoczynek zmierzony i rozliczona luka
`CardRemoved` tworzą jeden blok regulacyjny, reset dobowy jest przyznawany na końcu
połączonego bloku, a wartości nie zmieniają się po restarcie aplikacji.

---

## 2. Dane wejściowe

| | Doboś | Staniek |
|---|---|---|
| Rekordów historii | 15 595 | 13 660 |
| Zakres czasu gry | D124 23:00 → D139 16:33 | D124 23:00 → D139 16:33 |
| Rozliczonych luk | 21 | 14 |
| Zakwalifikowanych odpoczynków | 11 | 10 |

Źródła rekordów: `Telemetry`, `Reconstructed`, `ManualEntry`.

---

## 3. Scenariusz główny — końcówka dnia 137

Zweryfikowany dla obu kart.

| Kierowca | Blok odpoczynku | Długość | Luki w bloku | Reset dobowy |
|---|---|---|---|---|
| Doboś | D137 15:11 → D139 16:33 | 2962 min (49 h 22) | 1 | przyznany |
| Staniek | D137 10:36 → D139 16:32 | 3236 min (53 h 56) | 1 | przyznany |

Oba bloki powstały ze sklejenia odpoczynku zmierzonego telemetrią z wpisem manualnym
rozliczającym lukę. Ślad audytowy zachowany — blok niesie `SourceGapId`.

**Wynik: zgodny z oczekiwanym.**

---

## 4. Kontrola slotu 2 podczas jazdy

Sprawdzane cyklicznie od beta.1, potwierdzone ponownie 21.07.2026.

Ruch pojazdu zawsze przerywa odpoczynek kierowcy w slocie 2. Odpoczynek dobowy nie może
zostać zbudowany w jadącym pojeździe, zgodnie z Art. 8(8) rozporządzenia 561/2006.

Mechanizm potwierdzony również w kodzie: pierwsza klatka telemetrii z prędkością powyżej
progu cofa start przerwy załogowej do początku odpoczynku
([CrewTachographEngine.cs:290](../src/ETS2Tachograph.Engine/CrewTachographEngine.cs)),
przekracza limit 45 minut i przełącza slot 2 na `Dyspozycyjność`, zanim zdąży wejść
jakiekolwiek działanie użytkownika. UI w gałęzi „pojazd jedzie" nie pozwala wybrać innego
celu pauzy niż 45 minut.

**Wynik: zgodny z oczekiwanym. Scenariusz uznany za domknięty, wycofany z dalszych testów.**

---

## 5. Metoda weryfikacji

Wyniki liczone przez `RegulationEngine` skompilowany z bieżących źródeł beta.10, uruchomiony
na rekordach z eksportu CSV. Binaria obecne w `bin/` pochodzą z 18.07 i są starsze niż
beta.10, dlatego nie zostały użyte.

Kod projektu nie był modyfikowany. Narzędzia pomiarowe powstały poza repozytorium.

---

## 6. Obserwacje poza zakresem werdyktu

Poniższe pochodzą ze skanu pełnej historii obu kart, nie ze scenariuszy testowych dnia 2.
Odnotowane dla porządku dokumentacyjnego. **Decyzją właściciela projektu pozostają bez zmian
w kodzie.**

### 6.1 Bloki z wieloma rozliczonymi lukami

Karta Doboś: brak bloków odpoczynku zawierających więcej niż jedną lukę.

Karta Staniek: dwa takie bloki.

| Dzień | Zakres | Odpoczynek ciągły | Luki | Wynik w beta.10 |
|---|---|---|---|---|
| 129 | 01:12 → 14:18 | 786 min (13 h 06) | 4 | rozbity na 89 + 223 + 448 + 26; brak kwalifikacji |
| 130 | D130 00:46 → D131 05:02 | 1696 min (28 h 16) | 2 | ujęty jako 1447 min od 04:55 |

Dzień 130 zachowuje poprawną klasyfikację — 1447 min przekracza próg odpoczynku dobowego
i 24 h skróconego tygodniowego. Różnica 249 min wchodzi natomiast w wyliczenie rekompensaty
tygodniowej, liczonej od niedoboru względem 45 h.

### 6.2 Zakres poprawki beta.10

Beta.10 objęła przypadek jednej luki w bloku odpoczynku. Zielone wyniki dni 134, 137 oraz
blok 1447 min z dnia 130 mieszczą się w tym zakresie. Bloki z wieloma lukami idą inną
ścieżką scalania w `HistoryAnalysis`.

### 6.3 Pytanie otwarte

Do rozstrzygnięcia przy ewentualnym powrocie do tematu: czy warunek wymagający, by jedna
strona sklejenia pochodziła z telemetrii, był świadomą blokadą przed łączeniem dwóch wpisów
manualnych. Odpowiedź wyznacza kształt ewentualnej poprawki. Historia git nie rozstrzyga —
plik w całości pochodzi z commita odtwarzającego repozytorium.

---

## 7. Zmiany w dokumentacji wykonane tego dnia

| Commit | Zakres |
|---|---|
| `51cad1f` | checklista regresji UI po każdej zmianie w XAML — `BETA_TEST_PLAN.md` |
| `caec0af` | usunięcie sekcji 13 z handoffu, przenumerowanie kolejnej |
| `0d9e226` | doprecyzowanie kryteriów restartu dla nakładek |

Trwałość ustawień nakładek zweryfikowana w kodzie: pozycje slotów S1 i S2 są zapisywane
w `%LocalAppData%\ETS2Tachograph\overlay-position-s{1,2}.json` i odtwarzane przy starcie;
widoczność nie jest utrwalana.

---

## 8. Stan po dniu 2

- Reguła ciągłości odpoczynku przez pojedynczą lukę — zweryfikowana terenowo na dwóch kartach.
- Slot 2 w ruchu — domknięte, wycofane z planu testów.
- Kod beta.10 bez zmian.
- Pozostałe scenariusze z listy ryzyk: luka na granicy tygodnia regulacyjnego, interakcja
  z rekompensatą tygodniową.
