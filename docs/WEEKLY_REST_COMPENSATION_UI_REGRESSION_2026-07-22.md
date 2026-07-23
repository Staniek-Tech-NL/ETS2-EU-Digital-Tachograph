# Regresja UI — rekompensata skróconego odpoczynku tygodniowego

**Data:** 22 lipca 2026  
**Wynik:** zaliczona dla funkcji dostępnych bez aktywnej telemetrii ETS2
**Charakter dokumentu:** historyczna migawka regresji UI kandydata beta.11; liczby testów poniżej zachowano jako wynik z dnia wykonania.

| Obszar | Wynik | Potwierdzenie |
|---|---|---|
| Dashboard — karta 1 | Zaliczony | Doboś: 1 otwarte zobowiązanie, 19:52 długu, najbliższy termin Dzień 155, status w terminie. |
| Dashboard — karta 2 | Zaliczony | Staniek: 1 otwarte zobowiązanie, 20:53 długu, najbliższy termin Dzień 155, status w terminie. |
| Nakładka S1/S2 | Zaliczony | Obie nakładki mieszczą cztery pola podsumowania bez prezentowania pełnego śladu. |
| Szczegóły — Doboś | Zaliczony | Pełny dług i pozostało 19:52 (1192 min), źródłowy odpoczynek, tydzień, termin, status oraz brak bloku spłacającego. |
| Szczegóły — Staniek | Zaliczony | Pełny dług i pozostało 20:53 (1253 min), źródłowy odpoczynek, tydzień, termin, status oraz brak bloku spłacającego. |
| Identyfikatory | Zaliczony | Skrócone na ekranie, pełna wartość w podpowiedzi; kopiowanie pełnego identyfikatora potwierdzone. |
| Pusty ślad spłaty | Zaliczony | Brak bloku i zakresu jest prezentowany jako `—`; przycisk kopiowania nieistniejącego identyfikatora jest wyłączony. |
| Istniejące zakładki | Zaliczony | Historia, Raporty, Kierowcy i Ustawienia otwierają się i zachowują dotychczasową zawartość. |
| Sterowanie urządzeniem | Zaliczony | Menu, nawigacja, anulowanie, zmiana aktywności, pauza, OUT, PROM oraz wydruk 24 h. OUT i PROM przywrócono do stanu wyłączonego. |
| Restart aplikacji | Zaliczony | Profile Julia/Doboś i Arkadiusz/Staniek zostały odtworzone, a szczegóły zobowiązań ponownie przeliczone z historii. |
| Układ przy zmianach XAML | Zaliczony | Po korekcie wysokości panelu przyciski Dashboardu i nowe podsumowania nie są obcięte; stały rozmiar nakładek pozostał czytelny. |

## Ograniczenia środowiska

Aplikacja pracowała w stanie `Oczekiwanie na ETS2...`. Automatyczne przejście do jazdy i blokady dostępne wyłącznie podczas aktywnej telemetrii gry nie mogły zostać wykonane w tej sesji. Oba zajęte sloty kart zostały zweryfikowane po restarcie; ponowne otwarcie dialogu wkładania karty wymagałoby wcześniejszego ręcznego przytrzymania przycisku wysuwania.

## Kontrola techniczna

- Kompilacja Desktop Release: bez ostrzeżeń i błędów.
- Pełny pakiet rozwiązania: 262/262 testy zaliczone.
- RuleEngine: 55/55 testów zaliczonych.

## Aktualizacja beta.11.1 — 23 lipca 2026

- Bieżący gate automatyczny: 282/282; RuleEngine 62/62; Engine 69/69;
  Application 50/50; Reports 9/9; Infrastructure 51/51.
- Zakładka `Rekompensaty` prezentuje kandydatury wygenerowane przez RuleEngine:
  rolę bloku, podstawę `540/1440/2700`, stary i nowy dług oraz informację,
  czy wariant zalicza odpoczynek tygodniowy.
- Brak wyboru daje `PendingRestAllocation`; Raporty pokazują ostrzeżenie i
  `EvidenceComplete = false`.
- PDF, CSV i JSON zawierają `RestBlockId`, `CandidateId`, wybraną rolę, czas
  decyzji, zobowiązania, podstawę i nowy dług.
- Start i restart UI beta.11.1 zostały sprawdzone na kopii właściwej bazy.
- Końcowa kontrola z aktywną telemetrią, pełna checklista XAML oraz decyzja
  GO/FIX/HOLD pozostają do wykonania osobiście przez testera.
