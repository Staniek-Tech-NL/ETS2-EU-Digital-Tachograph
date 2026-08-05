# M4-0 — Inwentaryzacja UI przed UI freeze

**Projekt:** ETS2 EU Digital Tachograph

**Wydanie docelowe:** `0.1.0-beta.12`

**Data rozpoczęcia:** 27 lipca 2026

**Status bieżący:** **ZAMKNIĘTY — GO**

**Kryterium wejścia:** formalne GO M3.6 i M3.7 — spełnione.

**Kryterium wyjścia:** kompletna lista z decyzją dla każdego elementu, osobista
weryfikacja właściciela na rozpakowanym rc4, commit i formalne **GO M4-0**.

**Następny etap:** M4 — Finalizacja całego interfejsu i UI freeze.

## Zasada dowodowa

Kompletność wykazu wynika z odczytu `MainWindow.xaml`, `OverlayWindow.xaml` oraz
powiązanych view-modeli. Taki odczyt potwierdza istnienie kontrolki, bindingu i
komendy, ale nie potwierdza działania, widoczności, układu ani braku błędów
bindingów.

Stan faktyczny w kolumnie **Weryfikacja rc4** uzupełnia osobiście właściciel,
wyłącznie na uruchomionym, świeżo rozpakowanym artefakcie:

`ETS2Tachograph-0.1.0-beta.12-rc4-win-x64.zip`

- commit źródłowy: `a1b8a486b52ee244984016efe268562690d4fbc4`;
- SHA-256:
  `8ED073DBEF0ADEA4B589BD257A6D2DEC6A390C11F5242D97D188838F9F0F56DE`;
- `⬜` — oczekuje na test właściciela;
- `PASS` — zachowanie i układ potwierdzone;
- `M4` — potwierdzony problem przypisany do naprawy w M4;
- `N/D` — świadomie poza zakresem beta.12.

M4 nie rozpoczyna się, dopóki ten dokument nie otrzyma formalnego GO. Wyników
rc4 nie wolno zastępować wnioskami z kodu ani uruchomieniem aplikacji z IDE.

## Inwentaryzacja

| Obszar | Element | Stan oczekiwany | Stan aktualny z kodu | Decyzja | Uzasadnienie | Weryfikacja rc4 |
|---|---|---|---|---|---|---|
| Powłoka | Nagłówek aplikacji | Nazwa, telemetria, wersja i rola są czytelne oraz aktualne | Binding statusu i wersji istnieje | beta.12 | Podstawowa orientacja użytkownika | PASS |
| Powłoka | Nawigacja boczna | Wszystkie ekrany są dostępne, aktywna pozycja jest jednoznaczna | Siedem kart w jednym `TabControl` | beta.12 | Główna nawigacja produktu | PASS |
| Powłoka | Minimalny rozmiar i skalowanie | Brak obcięć, nakładania i utraty akcji | Układ używa mieszaniny stałych szerokości, `Grid`, `WrapPanel` i `Viewbox` | beta.12 | Gate M4 wymaga kontroli przepełnień | PASS |
| Powłoka | Klawiatura i fokus | Logiczna kolejność Tab, Enter/Escape i widoczny fokus | Część ekranów ma jawne `TabIndex`, komendy i skróty; brak pełnego dowodu z kodu | beta.12 | Dostępność bez zmiany funkcji | PASS |
| Powłoka | Spójność komunikatów | Sukces, ostrzeżenie, walidacja i błąd są rozróżnialne | Wspólny `OperationStatus` oraz lokalne statusy Planera i Raportów | beta.12 | M4 ujednolica istniejące komunikaty | PASS |
| Powłoka | Dialogi plikowe | Anulowanie, sukces i błąd nie pozostawiają mylącego stanu | Import i eksport mają obsługę wyniku i wyjątków | beta.12 | Pełny przepływ istniejących funkcji | PASS |
| Powłoka | Błędy bindingów | Brak błędów i wyjątków UI w logu całej sesji | Nie można potwierdzić statycznie | beta.12 | Twardy gate M4 | PASS |
| Dashboard | Wirtualne urządzenie | Panel skaluje się, LCD jest czytelny, hotspoty pokrywają przyciski | Obraz panelu, LCD i osiem hotspotów istnieją | beta.12 | Główna powierzchnia operacyjna | PASS |
| Dashboard | Menu LCD | Góra/dół/OK/anuluj prowadzą przez istniejące funkcje bez martwych pozycji | Komendy i stan menu istnieją w `MainViewModel` | beta.12 | Wymaga testu działania | PASS |
| Dashboard | Karty S1/S2 | Kliknięcie i przytrzymanie 3 s poprawnie wkładają/wyjmują właściwą kartę | Osobne hotspoty i komendy dla obu slotów | beta.12 | Krytyczny przepływ obu kart | PASS |
| Dashboard | Szybkie akcje | PDF, odświeżenie, CSV zobowiązań i diagnostyka dają właściwy wynik | Cztery przyciski są podłączone do komend | beta.12 | Istniejące eksporty | PASS |
| Dashboard | Alerty | Naruszenia, opcjonalna luka i komunikaty operacji są widoczne bez danych zastępczych | Lista, akcja luki i trzy komunikaty istnieją | beta.12 | Kontrola stanów ostrzegawczych | PASS |
| Dashboard | Karta stanu S1 | Kierowca, aktywność, liczniki, rekompensata i cel pauzy są spójne | Wszystkie bindingi istnieją | beta.12 | Dane podstawowego kierowcy | PASS |
| Dashboard | Karta stanu S2 | Dane, liczniki, rekompensata i pauza zmiennika są niezależne od S1 | Osobne bindingi i komenda pauzy istnieją | beta.12 | Wymóg dwóch slotów | PASS |
| Dashboard | Cele pauzy | Wybór, start, czas trwania, pozostały czas i zaliczenie są zgodne między ekranami | Kontrolki istnieją dla obu slotów | beta.12 | Potwierdzenie spójności Dashboard/overlay | PASS |
| Dashboard | Brak karty / brak telemetrii | Użytkownik widzi jawny, stabilny stan pusty | View-model ma teksty zastępcze; brak osobnej planszy pustego stanu | beta.12 | Brak nowej funkcji, tylko domknięcie prezentacji | PASS |
| Dashboard | OUT i Prom | Tryb jest widoczny i przełączalny, bez sugerowania pełnego art. 9 | Komendy i oznaczenie trybów istnieją | beta.12 | Obecny zakres produktu | PASS |
| Dashboard | PROM — pełne odstępstwo art. 9 | Brak nowych obietnic i automatycznego scalania odpoczynku | Produkcyjna integracja nie istnieje | poza zakresem | Backlog po beta.12 | N/D |
| Nakładki | Overlay S1 i S2 | Każdy slot ma niezależne dane, skrót i zapamiętaną pozycję | Jeden widok parametryzowany numerem slotu | beta.12 | Wymóg dwóch nakładek | PASS |
| Nakładki | Zawartość i spójność | Aktywność, jazda, pauza, praca, rekompensaty, tryby i połączenie zgadzają się z Dashboardem | Wszystkie pola są zbindowane | beta.12 | Gate spójności powierzchni | PASS |
| Nakładki | Przeciąganie i widoczność | Przeciąganie nie aktywuje przypadkowych akcji; Alt+1/Alt+2 działa stabilnie | Uchwyt przeciągania i etykiety skrótów istnieją | beta.12 | Wymaga testu okien Windows | PASS |
| Historia | Lista aktywności | Obie karty, czas gry, aktywność, źródło i warunek są czytelne | Tabela sześciokolumnowa istnieje | beta.12 | Audyt historii minutowej | PASS |
| Historia | Import/eksport `.tacho` | Anulowanie, poprawny plik i błąd mają jednoznaczny wynik | Dwie komendy istnieją | beta.12 | Istniejący przepływ danych | PASS |
| Historia | Lista luk | Filtr rozliczonych, licznik, stany otwarte/zamknięte i akcja rozliczenia działają | Tabela i akcja warunkowa istnieją | beta.12 | Krytyczna kompletność danych | PASS |
| Historia | Pusty stan | Brak historii lub luk jest wyjaśniony, nie wygląda jak błąd | Brak jawnej planszy pustego stanu w XAML | beta.12 | Brak prezentacyjny do rozstrzygnięcia w M4 | PASS |
| Wpis manualny | Modal wymagany i opcjonalny | Wymagany wpis nie daje się pominąć; opcjonalny można anulować | Widoczność anulowania jest warunkowa | beta.12 | Istniejąca zasada blokady | PASS |
| Wpis manualny | Nagłówek i zakres | Slot, kierowca, przyczyna, granice i długość luki są jednoznaczne | Wszystkie pola są zbindowane | beta.12 | Użytkownik musi znać zakres decyzji | PASS |
| Wpis manualny | Szybki wybór całej luki | Odpoczynek, praca i dyspozycyjność zastępują cały plan zgodnie z opisem | Trzy komendy istnieją | beta.12 | Wariant B | PASS |
| Wpis manualny | Plan segmentów | Edycja, usuwanie i podwójne kliknięcie wybierają właściwy segment | Tabela i komendy istnieją | beta.12 | Wariant B | PASS |
| Wpis manualny | Edytor czasu | Dzień, godzina, aktywność i długość walidują półotwarty zakres | Kontrolki i komunikat walidacji istnieją | beta.12 | Ochrona ciągłości minut | PASS |
| Wpis manualny | Podsumowanie i zatwierdzenie | Pokrycie, sumy i kompletność blokują nielegalny zapis | Pola podsumowania i komenda zatwierdzenia istnieją | beta.12 | Krytyczny gate danych | PASS |
| Kraj/kod | Dialog karty | Wybór profilu i kraju działa osobno dla początku i końca | Dialog, katalog krajów i etykieta zależna od operacji istnieją | beta.12 | Obowiązujący zakres tachografowy | PASS |
| Kraj/kod | Wyszukiwanie kraju | Pisanie i wybór z klawiatury nie gubią zaznaczenia ani fokusu | Dedykowana obsługa `PreviewTextInput` istnieje | beta.12 | Ergonomia istniejącej funkcji | PASS |
| Rekompensaty | Wybór karty i odświeżenie | Widok pokazuje dane wybranej karty i stabilnie reaguje na zmianę | ComboBox i komenda odświeżenia istnieją | beta.12 | Podstawowy filtr | PASS |
| Rekompensaty | Oczekująca alokacja | Warianty są czytelne, a wybór jasno pokazuje skutki | Karty wariantów i komenda wyboru istnieją | beta.12 | Krytyczna decyzja użytkownika | PASS |
| Rekompensaty | Szczegóły zobowiązania | Status, dług, termin, źródło i spłata są kompletne | Widok kart szczegółowych istnieje | beta.12 | Pełny ślad audytowy | PASS |
| Rekompensaty | Kopiowanie identyfikatorów | Pełny identyfikator kopiuje się, a brak identyfikatora spłaty wyłącza akcję | ToolTipy i komendy kopiowania istnieją | beta.12 | Czytelność bez utraty śladu | PASS |
| Rekompensaty | Pusty stan i błąd | Brak zobowiązań oraz błąd odczytu są rozróżnialne | Status jest ustawiany w view-modelu; brak jawnej planszy pustego stanu | beta.12 | Domknięcie prezentacji w M4 | PASS |
| Raporty | Wybór kierowcy i zakresu | Presety oraz własny zakres działają w czasie gry | Kontrolki i walidacja istnieją | beta.12 | Konfiguracja raportu | PASS |
| Raporty | Stan ładowania/błędu/danych | Użytkownik rozróżnia ładowanie, brak danych, błąd i gotowy podgląd | Model stanów i komunikaty istnieją | beta.12 | Wymaga potwierdzenia renderowania | PASS |
| Raporty | Ostrzeżenie o lukach | Nierozliczone luki są jawne, a akcja prowadzi do Historii | Status i przycisk „Pokaż luki” istnieją | beta.12 | Kompletność materiału dowodowego | PASS |
| Raporty | Kafelki statystyk | Jazda, praca, gotowość, odpoczynek, dług i naruszenia zgadzają się z zakresem | Sześć kafelków istnieje | beta.12 | Wariant B | PASS |
| Raporty | Zakładki podglądu | Podsumowanie, aktywności, naruszenia, rekompensaty i kompletność są dostępne | Pięć zakładek istnieje | beta.12 | Wariant B | PASS |
| Raporty | Dane techniczne | Przełącznik pokazuje źródło i warunek bez zmiany raportu | CheckBox i szczegóły wiersza istnieją | beta.12 | Istniejąca diagnostyka | PASS |
| Raporty | Eksporty | PDF, JSON VTC, CSV zobowiązań i surowy CSV odpowiadają podglądowi | Cztery komendy eksportu istnieją | beta.12 | Gate zgodności plik/ekran | PASS |
| Raporty | Pusty stan | Brak kierowcy lub danych ma jasną instrukcję, a nie pustą tabelę | View-model ma komunikaty; widok nie ma osobnej planszy | beta.12 | Domknięcie prezentacji w M4 | PASS |
| Kierowcy | Lista profili | Aktywny profil i data utworzenia są czytelne | Tabela istnieje | beta.12 | Zarządzanie kartami | PASS |
| Kierowcy | Tworzenie i aktywacja | Walidacja nazwy/karty, sukces i błąd są jednoznaczne | Pola, dwie komendy i status istnieją | beta.12 | Pełny istniejący przepływ | PASS |
| Kierowcy | Pusty stan | Pierwsze uruchomienie prowadzi do utworzenia profilu | Brak osobnej planszy; formularz jest stale widoczny | beta.12 | Wymaga oceny użyteczności rc4 | PASS |
| Ustawienia | Próg jazdy i tydzień | Wartości są walidowane, zapisane i jawnie stosowane po restarcie | Dwa pola, komenda i komunikat istnieją | beta.12 | Istniejąca konfiguracja | PASS |
| Ustawienia | Niepoprawne wartości | Błąd nie zamyka aplikacji i wskazuje pole lub przyczynę | Wyjątek trafia do wspólnego statusu; brak walidacji per pole w XAML | beta.12 | Domknięcie walidacji w M4 | PASS |
| Planer | Tryb i pierwszy kierowca | Rynek/aktywna dostawa oraz S1/S2 zmieniają właściwy kontrakt | RadioButtony i wybór slotu istnieją | beta.12 | M3/M3.7 zamknięte funkcjonalnie | PASS |
| Planer | Pola czasu i presety | Parser, klawisze, presety i utrata fokusu działają spójnie | Dedykowany styl i komendy istnieją | beta.12 | Ergonomia M3.7 | PASS |
| Planer | Okno dostawy | Dzień, godzina i minuta mają poprawny zakres i kolejność Tab | Sześć list i jawne `TabIndex` istnieją | beta.12 | Ergonomia M3.7 | PASS |
| Planer | Walidacja i gotowość | Błędy pól i brak wiarygodnych danych blokują obliczenie z jasnym powodem | Komunikat oraz lista problemów istnieją | beta.12 | Bezpieczeństwo wyniku | PASS |
| Planer | Podsumowanie wyniku | Czas, wygaśnięcie, okno, odbiór, przyjazd, koniec i margines są czytelne | Siedem pól podsumowania istnieje | beta.12 | Wynik obliczenia | PASS |
| Planer | Harmonogram załogi | Segmenty pokazują pojazd, S1, S2, czas i powód bez obcięć | Tabela ośmiokolumnowa istnieje | beta.12 | Wynik dla jednej lub dwóch kart | PASS |
| Planer | Ostrzeżenia i podsumowanie | Ograniczenia nie są ukrywane; brak wyniku nie pozostawia starego planu | Dwie kolekcje wynikowe istnieją | beta.12 | Bezpieczeństwo i stale snapshot | PASS |
| Planer | Pamięć wejść | Restart przywraca wejścia i pokazuje ich pochodzenie | `JourneyPlannerInputStateStore` i `InputOriginText` istnieją | beta.12 | M3.7 | PASS |
| Planer | Eksport planu | Nie dodawać nowego eksportu podczas M4 | Brak kontrolki i kontraktu eksportu planu | poza zakresem | Nowa funkcja Planera | N/D |
| ODP. TYG. | Terminy na powierzchniach UI | Dashboard, Planer, Rekompensaty i Raporty pokazują spójne terminy czasu gry | Formattery i bindingi istnieją | beta.12 | Wcześniej zatwierdzony zakres beta.12 | PASS |
| Przekrojowe | Puste kolekcje | Historia, luki, rekompensaty, raporty i wyniki Planera mają jawny sensowny stan | Część opiera się wyłącznie na pustej tabeli/kolekcji | beta.12 | Jawne zadanie prezentacyjne M4 | PASS |
| Przekrojowe | Stany błędów | Błędy bazy, importu, eksportu i obliczeń nie pozostawiają starego sukcesu | Obsługa wyjątków istnieje w view-modelach | beta.12 | Wymaga testu end-to-end | PASS |
| Przekrojowe | Dwa sloty | Każdy kluczowy przepływ działa dla właściwej karty i nie miesza stanu | Osobne pola/komendy istnieją, lecz nie wszędzie UI wybiera slot | beta.12 | Gate M4 | PASS |
| Przekrojowe | Teksty PL/EN | W M4 sprawdzać miejsce na dłuższe teksty, bez tłumaczenia treści | Teksty są obecnie osadzone głównie po polsku | poza zakresem | Tłumaczenie należy do M5 | N/D |
| Przekrojowe | Nowe funkcje | Nie dodawać funkcji podczas porządkowania i freeze | Brak technicznej blokady; obowiązuje gate dokumentacyjny | poza zakresem | Ochrona zakresu M4 | N/D |

## Pokrycie kodowe

Podstawowe źródła:

- `src/ETS2Tachograph.Desktop/Views/MainWindow.xaml` — powłoka, siedem ekranów,
  dialog karty i wariant B wpisu manualnego;
- `src/ETS2Tachograph.Desktop/Views/OverlayWindow.xaml` — nakładki S1/S2;
- `MainViewModel`, `JourneyPlannerViewModel`, `ReportsWorkspaceViewModel`,
  `OverlayViewModel` i `ManualEntryPlanEditor` — stan, komendy, walidacja oraz
  komunikaty;
- zatwierdzona makieta M3:
  `docs/images/journey-planner-mockup.png`.

## Osobista checklista rc4

- [x] Uruchomić aplikację wyłącznie z rozpakowanego rc4 i potwierdzić wersję.
- [x] Wykonać wiersze `beta.12` na istniejącej bazie z aktywną telemetrią.
- [x] Powtórzyć puste stany na czystej bazie.
- [x] Sprawdzić S1 i S2, nakładki, restart oraz trwałość ustawień.
- [x] Sprawdzić minimalny rozmiar okna, skalowanie, klawiaturę i przepełnienia.
- [x] Sprawdzić log sesji pod kątem bindingów i wyjątków UI.
- [x] Wpisać przy każdym wierszu `PASS` albo `M4`.
- [x] Potwierdzić, że żaden element nie pozostał bez decyzji.

## Gate M4-0

- [x] kompletność powierzchni UI ustalona z kodu;
- [x] każdy element ma decyzję `beta.12` albo `poza zakresem`;
- [x] nowe funkcje nie zostały włączone do M4;
- [x] osobista weryfikacja właściciela na rozpakowanym rc4 zakończona;
- [x] wszystkie problemy mają przypisanie do M4, P0/P1 albo świadome odłożenie;
- [x] dokument zatwierdzony i włączony do commita zamykającego;
- [x] formalna decyzja **GO M4-0**.

## Decyzja końcowa

- **Data zakończenia:** 2026-07-27
- **Wynik:** **GO**
- **Weryfikacja rc4:** 62/62 pozycji `beta.12` — PASS
- **Poza zakresem:** 4 pozycje — N/D
- **Pozycje przypisane do M4:** 0
- **Otwarte błędy P0:** 0
- **Otwarte błędy P1:** 0
- **Decyzja:** wykaz jest kompletnym, zatwierdzonym wejściem do M4; właściwe M4
  jest odblokowane, ale pozostaje nierozpoczęte do osobnej decyzji startowej.
