# Dane referencyjne — rekompensaty odpoczynków tygodniowych

**Stan na:** 22 lipca 2026  
**Status:** zatwierdzone dane referencyjne do testów regresyjnych  
**Źródło:** `output/ODZYSK-BAZY/tachograph.db`  
**SHA-256 źródła:** `5E6E9D9C50A762CBE2A36A6D2624FBE45D714FEA7AB951DAB51688958123E3C2`  
**Rozmiar źródła:** `7 491 584` bajty  
**Ostatnia modyfikacja źródła (UTC):** `2026-07-21T20:39:41.3516267Z`  
**Punkt oceny:** `game_time = 199714`, czyli Dzień 139, `16:34` według reguły `floor(minute / 1440) + 1`  
**Przesunięcie tygodnia:** `WeekEpochOffsetDays = 0`

Ta kopia jest właściwym punktem odniesienia, ponieważ odtwarza dokładnie wartości widoczne w obecnym modelu: Staniek `18 min`, Doboś `353 min`. Aktywna baza z 22 lipca zawiera późniejsze rekordy do `game_time = 201533` i nie reprezentuje już tego samego scenariusza.

## Metoda odtworzenia

Historia została złożona tak samo jak `LoadDriverHistoryAsync`: bloki warm połączono z niearchiwalnymi rekordami hot, stosując granice sesji i `truncate-and-append`. Następnie odtworzono `HistoryAnalysis.Runs`, w tym łączenie sąsiadujących odpoczynków przez pojedynczy `SourceGapId`, wybór kwalifikowanych odpoczynków oraz bieżący algorytm `ProjectCompensations`.

Obecny silnik tworzy dług `2700 − długość skróconego odpoczynku tygodniowego`, a potem częściowo zmniejsza go każdą nadwyżką:

- ponad `540 min` dla kwalifikowanego odpoczynku dobowego;
- ponad `2700 min` dla regularnego odpoczynku tygodniowego.

Nowy wynik zastosowano według zatwierdzonej specyfikacji domenowej: jeden zamknięty ciągły blok musi pomieścić cały pozostały dług en bloc; niewystarczająca nadwyżka nie zmniejsza długu i nie tworzy kredytu na później.

## Staniek

### Źródłowy skrócony odpoczynek

| Pole | Wartość |
|---|---|
| Zakres | `186055–187502`, Dzień 130 `04:55` → Dzień 131 `05:02` |
| Długość | `1447 min` = `24:07` |
| Klasyfikacja | zakończony skrócony odpoczynek tygodniowy |
| Tydzień źródłowy | indeks `18`, polityka `StartWeek` |
| Źródło | `ManualEntry`, sesja `16` |
| `SourceGapId` | `0F368EE5-460D-43C8-9059-F28B5165C7E3` |
| Pierwotny dług | `2700 − 1447 = 1253 min` = `20:53` |

### Bloki, z których obecny silnik naliczył kredyt

| Lp. | Zakres bloku | Długość | Podstawa obecnego kredytu | Kredyt odjęty od długu | Pochodzenie |
|---:|---|---:|---:|---:|---|
| 1 | `188105–188767`, D131 `15:05` → D132 `02:07` | `662 min` (`11:02`) | `662 − 540` | `122 min` | Telemetry `3 min` + Reconstructed `659 min`, sesja `16` |
| 2 | `190059–190743`, D132 `23:39` → D133 `11:03` | `684 min` (`11:24`) | `684 − 540` | `144 min` | Telemetry `9 min` + Reconstructed `675 min`, sesja `17` |
| 3 | `192051–192774`, D134 `08:51` → D134 `20:54` | `723 min` (`12:03`) | `723 − 540` | `183 min` | ManualEntry `723 min`, sesja `19`, `SourceGapId=2231AB20-B921-4442-80AB-49FBDDA88E22` |
| 4 | `194086–194749`, D135 `18:46` → D136 `05:49` | `663 min` (`11:03`) | `663 − 540` | `123 min` | Telemetry `4 min` + Reconstructed `659 min`, sesja `19` |
| 5 | `195807–196474`, D136 `23:27` → D137 `10:34` | `667 min` (`11:07`) | `667 − 540` | `127 min` | Telemetry `8 min` + Reconstructed `659 min`, sesja `20` |
| 6 | `196476–199712`, D137 `10:36` → D139 `16:32` | `3236 min` (`53:56`) | `3236 − 2700` | `536 min` | Telemetry `1 min` + ManualEntry `3235 min`, sesja `20`, `SourceGapId=ACC1278D-0FB1-4591-8006-ECE231EF7350` |

### Bilans kontrolny

| Obliczenie | Wynik |
|---|---:|
| Suma kredytów obecnego silnika | `122 + 144 + 183 + 123 + 127 + 536 = 1235 min` |
| Wartość pokazywana obecnie | `1253 − 1235 = 18 min` |
| Największa nadwyżka pojedynczego bloku | `536 min` |
| Pełna kwota wymagana en bloc | `1253 min` |
| Blok spełniający pełne en bloc | brak |
| **Wartość oczekiwana w nowym modelu** | **`1253 min` = `20:53`** |

Żaden z sześciu bloków nie mieści pełnych `1253 min` rekompensaty ponad właściwe minimum odpoczynku bazowego. Wszystkie obecne odjęcia są częściowymi „okruchami”, dlatego w nowym modelu nie następuje żadna spłata.

## Doboś

### Źródłowy skrócony odpoczynek

| Pole | Wartość |
|---|---|
| Zakres | `187260–188768`, Dzień 131 `01:00` → Dzień 132 `02:08` |
| Długość | `1508 min` = `25:08` |
| Klasyfikacja | zakończony skrócony odpoczynek tygodniowy |
| Tydzień źródłowy | indeks `18`, polityka `StartWeek` |
| Źródło | Telemetry `5 min` + Reconstructed `237 min` + ManualEntry `1266 min`, sesja `18` |
| `SourceGapId` | `81C8CB6D-1FE0-4ADF-9E0A-AF91910573EC` |
| Pierwotny dług | `2700 − 1508 = 1192 min` = `19:52` |

### Bloki, z których obecny silnik naliczył kredyt

| Lp. | Zakres bloku | Długość | Podstawa obecnego kredytu | Kredyt odjęty od długu | Pochodzenie |
|---:|---|---:|---:|---:|---|
| 1 | `190059–190742`, D132 `23:39` → D133 `11:02` | `683 min` (`11:23`) | `683 − 540` | `143 min` | Telemetry `8 min` + Reconstructed `675 min`, sesja `19` |
| 2 | `192051–192775`, D134 `08:51` → D134 `20:55` | `724 min` (`12:04`) | `724 − 540` | `184 min` | ManualEntry `724 min`, sesja `21`, `SourceGapId=2B70FAC9-B06F-4EF7-ADDA-C34E3B98F4CE` |
| 3 | `194086–194749`, D135 `18:46` → D136 `05:49` | `663 min` (`11:03`) | `663 − 540` | `123 min` | Telemetry `4 min` + Reconstructed `659 min`, sesja `23` |
| 4 | `195807–196474`, D136 `23:27` → D137 `10:34` | `667 min` (`11:07`) | `667 − 540` | `127 min` | Telemetry `8 min` + Reconstructed `659 min`, sesja `24` |
| 5 | `196751–199713`, D137 `15:11` → D139 `16:33` | `2962 min` (`49:22`) | `2962 − 2700` | `262 min` | Telemetry `2 min` + Reconstructed `239 min` + ManualEntry `2721 min`, sesja `24`, `SourceGapId=EFDC2D8D-7CE7-4525-A16C-70D585269377` |

### Bilans kontrolny

| Obliczenie | Wynik |
|---|---:|
| Suma kredytów obecnego silnika | `143 + 184 + 123 + 127 + 262 = 839 min` |
| Wartość pokazywana obecnie | `1192 − 839 = 353 min` |
| Największa nadwyżka pojedynczego bloku | `262 min` |
| Pełna kwota wymagana en bloc | `1192 min` |
| Blok spełniający pełne en bloc | brak |
| **Wartość oczekiwana w nowym modelu** | **`1192 min` = `19:52`** |

Żaden z pięciu bloków nie mieści pełnych `1192 min` rekompensaty ponad właściwe minimum odpoczynku bazowego. Wszystkie obecne odjęcia są częściowymi „okruchami”, dlatego w nowym modelu nie następuje żadna spłata.

## Oczekiwany wynik regresji

| Karta | Pierwotny dług | Obecny nieprawidłowy kredyt | Obecny wynik | Pełny blok en bloc w historii | Oczekiwany wynik nowego modelu |
|---|---:|---:|---:|---|---:|
| Staniek | `1253 min` | `1235 min` z 6 bloków | `18 min` | brak | **`1253 min`** |
| Doboś | `1192 min` | `839 min` z 5 bloków | `353 min` | brak | **`1192 min`** |

Te wartości są danymi wejściowymi dla testów regresyjnych pełnego modelu rekompensat. Test ma odtworzyć wskazane zakresy bloków, a nie jedynie podać końcową liczbę długu.
