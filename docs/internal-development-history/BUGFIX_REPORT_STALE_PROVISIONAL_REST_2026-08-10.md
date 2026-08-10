# Raport naprawy — nieaktualna aktywność tymczasowa odpoczynku

**Wydanie:** `0.1.0-beta.12.1`  
**Data:** 10 sierpnia 2026  
**Klasyfikacja:** P1 — błędna prezentacja bieżącego licznika pauzy

## Objaw

Po rekonstruowanym odpoczynku slotu 1 kanoniczna historia zawierała prawidłowe
8:57 przerwy. Po ręcznym przełączeniu na „Inna praca” interfejs mógł nadal
korzystać z wcześniejszej aktywności tymczasowej i pokazać 9:01.

Historia minutowa i wynik RuleEngine pozostawały prawidłowe. Błąd dotyczył
bieżącego snapshotu używanego przez Dashboard, LCD i nakładkę.

## Przyczyna

`TachographEngine.SetManualActivity()` odświeżał tryb ręczny, ale zachowywał
poprzednią `TachographSnapshot.ProvisionalActivity`. Warstwa prezentacji celowo
nadaje aktywności tymczasowej pierwszeństwo, dlatego nieaktualny odpoczynek był
widoczny do czasu nadejścia kolejnej ramki telemetrii.

## Naprawa

Ręczna zmiana aktywności wywołuje odświeżenie snapshotu z jawnym wyczyszczeniem
`ProvisionalActivity`. Pozostałe odświeżenia trybów zachowują dotychczasowe
zachowanie.

Nie zmieniono historii minutowej, RuleEngine, progów 44/45, XAML, schematu
SQLite, protokołu v3 ani pluginu.

## Test regresyjny

`Manual_work_after_reconstructed_8h57_break_clears_stale_provisional_rest`
odtwarza dokładną sekwencję:

1. rozpoczęcie odpoczynku;
2. skok czasu rekonstruujący 8:57 przerwy;
3. potwierdzenie tymczasowego odpoczynku;
4. ręczne przełączenie na inną pracę;
5. potwierdzenie natychmiastowego wyczyszczenia aktywności tymczasowej.

Przed poprawką test był czerwony: oczekiwał `null`, a otrzymywał
`BreakOrRest`. Po poprawce jest zielony.

## Gate automatyczny

- test regresyjny: PASS;
- pełna regresja: 571/571 PASS;
- build Release: 0 błędów, 0 ostrzeżeń.

Końcowy smoke jest wykonywany na niezmiennym artefakcie wydania i zostanie
odnotowany wraz z jego SHA-256.
