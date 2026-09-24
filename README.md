# KA3005P App

Nowa aplikacja Windows do obsługi zasilaczy Korad KA3005P, odtwarzająca funkcjonalność i styl projektu referencyjnego C++ Builder.

## Stan projektu

2026-09-24: zakończono wstępny przegląd referencji. Repozytorium zawiera licencję, kopie zasobów graficznych i opis zakresu. Kod nowej aplikacji nie został jeszcze zaimplementowany.

Wybrany kierunek techniczny: C#, WPF, .NET 10. Szczegóły i ograniczenia opisano w [przeglądzie projektu](docs/2026-09-24-przeglad-i-kierunek.md). Zatwierdzony projekt techniczny, w tym rozwiązanie problemu lagów podczas zmiany nastaw, znajduje się w [specyfikacji](docs/superpowers/specs/2026-09-24-ka3005p-app-design.md).

## Zakres referencyjny

- Pojedynczy zasilacz i dwie niezależne sesje.
- Dual Korad: tryb szeregowy, równoległy i symetryczny.
- Porty COM, nastawy napięcia i prądu, ON/OFF, pomiary i sygnalizacja stanu.
- Osobne okno wykresu prądu, obliczanie rezystancji, eksport CSV.
- Menedżer sesji i wybór folderu zapisu.
- Kompaktowe szare okna, jasne cyfry Consolas i oryginalne ikony.

## Zasoby

Kopie oryginalnych ikon i grafik znajdują się w `assets/reference`. Plik `provenance.json` zawiera źródła i sumy SHA-256. Projekt referencyjny w OneDrive pozostaje źródłem do odczytu.

## Licencja

Projekt jest udostępniany na warunkach **PolyForm Noncommercial License 1.0.0**. Pełny, niezmieniony tekst znajduje się w [LICENSE](LICENSE).

Źródło tekstu: [PolyForm Project](https://polyformproject.org/licenses/noncommercial/1.0.0).

Licencje przyszłych zależności i komponentów zewnętrznych zachowują własne warunki.
