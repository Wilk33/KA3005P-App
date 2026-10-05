# KA3005P App

> [!IMPORTANT]
> **To repozytorium zawiera historyczną linię rozwoju zakończoną na wersji 0.1.7.**
>
> Aktywny rozwój aplikacji Korad KA3005P jest kontynuowany w repozytorium [Wilk33/LabStation](https://github.com/Wilk33/LabStation), w katalogu [KA3005P](https://github.com/Wilk33/LabStation/tree/main/KA3005P). Bieżąca wersja w LabStation to 0.2.9. Nowe poprawki, wydania i wspólny panel dla wszystkich przyrządów są publikowane wyłącznie tam.

Natywna aplikacja Windows do obsługi zasilaczy Korad KA3005P, zachowująca funkcjonalność i styl projektu referencyjnego C++ Builder. Nowa architektura rozdziela interfejs od transmisji szeregowej, dzięki czemu zmiana nastaw podczas aktywnego wyjścia nie czeka na zakończenie pomiaru.

## Zakres historycznej wersji 0.1.7

Pierwsza wersja obejmuje pojedynczy zasilacz oraz Dual Korad w trybie szeregowym, równoległym i symetrycznym. Tryb wybiera się w jednym, kompaktowym oknie głównym. Aplikacja ma czerwony wykres prądu z opcjonalną zieloną linią napięcia, eksport CSV, obliczanie rezystancji zwarciowej, blokadę współdzielenia portów i tryb demonstracyjny bez sprzętu.

Projekt używa C#, WPF i .NET 10. Projekt techniczny, w tym rozwiązanie problemu lagów podczas zmiany nastaw, znajduje się w [specyfikacji](docs/superpowers/specs/2026-09-24-ka3005p-app-design.md).

## Wymagania

- Windows 10 lub Windows 11 w wersji x64.
- Jeden port COM dla pojedynczego zasilacza albo dwa różne porty COM dla Dual.
- Parametry transmisji są ustawiane przez aplikację: 9600 bit/s, 8 bitów danych, brak parzystości, 1 bit stopu, DTR wyłączone.

## Uruchomienie

Gotowy pakiet znajduje się w `artifacts/publish/win-x64-single`. Jest samodzielny i nie wymaga instalowania środowiska .NET. Uruchom `Ka3005P.App.exe`, wybierz aktywny port z listy i kliknij `Offline`, aby nawiązać połączenie. Po połączeniu przycisk zmieni opis na `Online`; ponowne kliknięcie rozłączy urządzenie i wcześniej wymusi `OFF`.

Tryb demonstracyjny nie otwiera portów COM:

```powershell
Ka3005P.App.exe --demo
```

W demo można przełączać tryb pojedynczy i Dual, połączyć fikcyjne porty, zmieniać nastawy, używać ON/OFF, obserwować pomiary, wykres i eksport CSV. Pomiar przy OFF wynosi 0 V i 0 A, nawet jeśli nastawa pozostaje zapisana.

Tytuł każdego okna zawiera wersję aplikacji. Natywne paski tytułu i menu przyjmują jasny albo ciemny motyw aplikacji ustawiony w Windows. Menu `O aplikacji` otwiera osobne okno danych autora albo pełnego tekstu licencji PolyForm Noncommercial License 1.0.0. Ikona programu pochodzi z referencyjnej grafiki `KoradS150.png`.

## Budowanie i testy

Wymagany jest SDK przypięty w `global.json`.

```powershell
dotnet restore Ka3005P.sln
dotnet build Ka3005P.sln -c Release
dotnet test Ka3005P.sln -c Release
```

Publikacja pakietu x64:

```powershell
dotnet publish src/Ka3005P.App/Ka3005P.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish/win-x64-single
```

Test dwóch fizycznych zasilaczy, kończący pracę poleceniem `OUT0`:

```powershell
dotnet run --project tools/Ka3005P.HardwareSmoke/Ka3005P.HardwareSmoke.csproj -c Release -- COM3 COM4
```

## Sterowanie i pomiary

- Nastawy napięcia i ograniczenia prądu są wysyłane poleceniami zapisu. Aplikacja celowo nie odpytuje `VSET1?`, `ISET1?` ani `STATUS?`.
- `VOUT1?` i `IOUT1?` oznaczają pomiar wyjścia. Nie są potwierdzeniem nastawy i przy OFF mogą zwracać zero.
- Szybkie zmiany tej samej nastawy zastępują starszą zmianę oczekującą w kolejce. Ostatnia wartość zostaje wysłana po zakończeniu bieżącej operacji COM.
- OFF ma pierwszeństwo przed oczekującymi nastawami i kolejnym pomiarem.
- Wykres korzysta z tego samego strumienia próbek co okno główne i nie uruchamia dodatkowego odpytywania.

## Tryby Dual

- Szeregowy - logiczne napięcie jest dzielone pomiędzy oba zasilacze, a limit prądu jest wspólny.
- Równoległy - logiczny prąd jest dzielony pomiędzy oba zasilacze, a napięcie jest wspólne.
- Symetryczny - oba zasilacze otrzymują tę samą wartość, a aplikacja prezentuje pierwszą gałąź ze znakiem ujemnym.

Wybór trybu w aplikacji nie przełącza przewodów. Przed włączeniem wyjścia użytkownik musi ręcznie wykonać połączenia właściwe dla wybranego układu i sprawdzić polaryzację. Trybu nie można zmienić podczas ON.

## Eksport

Menu `Zapisz jako` zapisuje napięcie, prąd albo oba pomiary do jednego pliku CSV z numerem próbki i czasem od początku sesji. Układ kolumn odpowiada wybranemu trybowi. Pliki używają kropki dziesiętnej, średnika jako separatora i jednostek w drugim wierszu.

## Zakres referencyjny

- Pojedynczy zasilacz i tryb Dual przełączane w jednym oknie głównym.
- Dual Korad: tryb szeregowy, równoległy i symetryczny.
- Porty COM, nastawy napięcia i prądu, ON/OFF, pomiary i sygnalizacja stanu.
- Osobne okno wykresu z domyślną linią prądu i opcjonalną linią napięcia, całkowitymi wartościami liczbowymi, obliczaniem rezystancji zwarciowej i eksportem CSV.
- Automatycznie odświeżane listy aktywnych portów COM z blokadą powtórnego wyboru portu w Dual.
- Kompaktowe szare okna, jasne cyfry Consolas i oryginalne ikony.

## Zasoby

Kopie oryginalnych ikon i grafik znajdują się w `assets/reference`. Plik `provenance.json` zawiera źródła i sumy SHA-256. Projekt referencyjny w OneDrive pozostaje źródłem do odczytu.

## Licencja

Projekt jest udostępniany na warunkach **PolyForm Noncommercial License 1.0.0**. Pełny, niezmieniony tekst znajduje się w [LICENSE](LICENSE).

Źródło tekstu: [PolyForm Project](https://polyformproject.org/licenses/noncommercial/1.0.0).

Licencje zależności znajdują się w [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Procedura testu na rzeczywistym sprzęcie jest opisana w [docs/testing-hardware.md](docs/testing-hardware.md).

## Autorstwo i rozwój

Projekt był tworzony przez Codex GPT-5.6-Sol pod kierunkiem Mateusza Skipora, na podstawie jego wymagań, decyzji projektowych i testów urządzeń.
