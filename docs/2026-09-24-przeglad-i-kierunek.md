# Przegląd referencji i kierunek nowej aplikacji

Data: 2026-09-24.
Status: rozpoznanie i propozycja architektury, przed implementacją.

## Ustalenia użytkownika

- Katalog roboczy: `D:\Programowanie\Projekty\KA3005P app`.
- Referencja tylko do odczytu: `C:\Users\mskip\OneDrive\Dokumenty\Embarcadero\Studio\Projects\Korad 2.0`.
- Kod C++ Builder wyznacza funkcjonalność i wygląd; technologię nowej aplikacji wybiera wykonawca.
- Ikony i grafiki mogą pochodzić z referencji.
- Repozytorium: https://github.com/Wilk33/KA3005P-App.git.
- Licencja: PolyForm Noncommercial License 1.0.0.

## Stan katalogów

Katalog roboczy był pusty. Sklonowano wskazane repozytorium; Git potwierdził, że repozytorium jest puste. Ustawiony origin wskazuje podany adres. Nie wykonano publikacji ani push.

Referencja zawiera trzy projekty C++ Builder/VCL: `Korad`, `Dual Korad` i `Korad Manager`. Znajdują się tam również pliki i katalogi generowane przez środowisko. Do nowego projektu skopiowano wyłącznie 12 plików ICO/PNG z katalogu głównego referencji.

Każdą kopię grafiki porównano z oryginałem za pomocą SHA-256. Pochodzenie zapisano w `assets/reference/provenance.json`.

## Funkcjonalność potwierdzona odczytem kodu

| Obszar | Zachowanie referencji | Główne źródło względem katalogu referencji |
| --- | --- | --- |
| Pojedynczy zasilacz | Wybór COM, Online/Offline, nastawy U/I, wyjście ON/OFF, odczyt U/I | `Korad/Unit1.cpp`, `Korad/Unit1.dfm` |
| Nastawy | Krok 0,01 V i 0,001 A, zatwierdzanie Enter, wartości graniczne 31 V i 5,1 A | `Korad/Unit1.cpp` |
| Łączenie | Po otwarciu portu polecenie OFF, następnie wysłanie nastaw; OFF przy rozłączaniu i zamykaniu | `Korad/Unit1.cpp` |
| Wykres | Osobne okno, 50 ostatnich próbek prądu, automatyczna skala Y, zielona linia, przycisk ON/OFF | `Korad/ChartForm.cpp`, `Korad/ChartForm.dfm` |
| Synchronizacja ON/OFF | Okno wykresu zmienia flagę; Timer2 w głównym oknie wykonuje zmianę wyjścia | `Korad/ChartForm.cpp`, końcówka `Korad/Unit1.cpp` |
| Pomiary | Timer 100 ms; rejestracja podczas ON; przy nowym ON reset czasu i plików pomiarowych | `Korad/Unit1.cpp`, `Korad/Unit1.dfm` |
| Rezystancja | U/I, widoczna przy prądzie bliskim nastawie; jednostki mikroohm, miliohm, ohm | `Korad/Unit1.cpp` |
| Eksport | Oddzielne pliki napięcia i prądu, separator średnik, czas i jednostki, Zapisz jako | `Korad/Unit1.cpp` |
| Dual - szeregowy | Dwa COM; podział nastawy U, wspólny limit I; pomiar U1+U2 oraz max(I1,I2) | `Dual Korad/Unit1.cpp` |
| Dual - równoległy | Dwa COM; wspólna nastawa U, podział limitu I; pomiar max(U1,U2) oraz I1+I2 | `Dual Korad/Unit1.cpp` |
| Dual - symetryczny | Jednakowe nastawy obu urządzeń; osobne odczyty; eksport pierwszej gałęzi ze znakiem minus | `Dual Korad/Unit1.cpp` |
| Menedżer | Uruchamianie jednej/dwóch instancji Korad lub Dual Korad, zamykanie instancji, folder zapisu | `Korad Manager/Korad_Manager.cpp` |
| Generator | Pozycja menu ma Visible=False; nie stanowi dostępnej funkcji w przeglądanej wersji | `Korad/Unit1.dfm`, `Dual Korad/Unit1.dfm` |

Wartości graniczne pochodzą z kodu, nie z weryfikacji parametrów producenta: pojedynczy 31 V / 5,1 A, szeregowy 62 V / 5,1 A, równoległy 31 V / 10,2 A, symetryczny nastawa 31 V / 5,1 A na urządzenie. Fizyczny układ połączeń nie został zweryfikowany. Tryb programu rozdziela nastawy i przelicza pomiary, a nie przełącza fizycznych przewodów.

## Komunikacja w referencji

`SerialPort.cpp` konfiguruje 9600 bit/s, 8 bitów danych, brak parzystości, jeden bit stopu, DTR wyłączone. Polecenia są wysyłane bez CR/LF.

`ka3005p_CB.cpp` zawiera polecenia VSET1:, ISET1:, OUT0, OUT1, VOUT1?, IOUT1?, VSET1?, ISET1? i STATUS?. Odpowiedzi liczbowe są czytane jako pięć bajtów, status jako jeden bajt. Metody VSET1?, ISET1? i STATUS? nie są wywoływane przez główny interfejs. Użytkownik potwierdził, że brak odczytu zwrotnego nastaw był świadomym wyborem ze względu na ograniczenia transmisji i czas oczekiwania. Nowa aplikacja zachowa to zachowanie. VOUT1? i IOUT1? są pomiarami wyjścia, a nie odczytem nastaw.

## Wygląd

Zrzuty użytkownika i pliki DFM wyznaczają: kompaktowe szare okna, jaśniejsze pola nastaw, jasne cyfry Consolas, wartości wyrównane do prawej, okrągłe kontrolki żółta/czerwona/zielona, niewielkie przyciski i klasyczne menu. Wykres pozostaje osobnym skalowalnym oknem.

DFM głównego okna Korad podaje obszar klienta 253 x 318, wykresu 496 x 398. To rozmiary referencyjne, nie nakaz ignorowania skalowania DPI. Istotne są proporcje i czytelność.

## Kierunek techniczny

Rekomendowany wybór: **C# + WPF + .NET 10**, aplikacja Windows z interfejsem po polsku.

WPF zapewnia okna, style, wiązanie danych i grafikę wektorową, co odpowiada temu interfejsowi. Pozwala odtworzyć wygląd i wykres bez przenoszenia komponentów VCL. Microsoft dokumentuje WPF jako technologię tylko dla Windows i .NET 10 jako wydanie LTS.

Rozważone alternatywy: C# + WinForms ułatwiłby odtworzenie klasycznych kontrolek, lecz WPF wybieram ze względu na kontrolę stylu i skalowania. Wieloplatformowy interfejs byłby dodatkowym zakresem bez wskazanego wymagania Linux/macOS.

Sprawdzony lokalnie stan SDK: dostępne `9.0.304`. SDK .NET 10 trzeba przygotować przed kompilacją docelowej wersji. W tym etapie nie instalowano zależności.

Proponowane zależności produkcyjne: WPF/.NET i `System.IO.Ports`. Wykres 50 próbek można narysować w WPF, a konfigurację zapisać przez `System.Text.Json`, bez osobnej biblioteki wykresów ani frameworka MVVM. Dokładne wersje pakietów należy przypiąć podczas przygotowania kompilacji.

## Proponowany podział

- Warstwa protokołu: budowanie komend, odczyt dokładnej liczby bajtów, walidacja odpowiedzi i wartości.
- Sesja urządzenia: jeden właściciel portu COM i kolejka operacji, limity czasu, stan połączenia, pomiary w tle.
- Sterowanie Dual: podział nastaw w jednostkach całkowitych 0,01 V / 0,001 A, synchronizacja dwóch sesji, wspólne działanie ON/OFF.
- Model sesji pomiarowej: rzeczywisty czas monotoniczny, próbki, obliczenia, eksport i katalog zapisu.
- Interfejs WPF: menedżer, okna pojedyncze, okno Dual i okna wykresu, współdzielące stan i komendy.

Wstępnie pełny zakres obejmuje wszystkie trzy projekty referencyjne. Jeden program może zapewniać te same tryby i wiele okien, bez uruchamiania i przymusowego zabijania osobnych procesów.

## Zachowanie do poprawienia przy odtwarzaniu funkcji

1. Walidacja przed wysłaniem: w pojedynczym Korad wpis powyżej maksimum zmienia wyświetlany tekst na maksimum, ale do UpdateVoltage/UpdateCurrent trafia pierwotna wartość. Trzeba walidować wartość wysyłaną, w tym dolną granicę.
2. Częściowe odpowiedzi: getV/getI ignorują liczbę odczytanych bajtów. Brak odpowiedzi ma być błędem/brakiem danych, bez fabrykowania zera.
3. Czas pomiarów: obecne dodawanie 0,1 s po cyklu nie mierzy opóźnień komunikacji. Nowy zapis powinien używać rzeczywistego czasu.
4. Operacje COM nie powinny blokować interfejsu. Polecenia i odczyty jednej sesji muszą być wykonywane kolejno.
5. Stan ON/OFF powinien mieć jedno źródło w sesji, współdzielone z wykresem. Błąd zapisu nie może oznaczać potwierdzonego przełączenia.
6. Rezystancja przy prądzie zerowym powinna być niedostępna, bez dzielenia przez zero.
7. Menedżer powinien zamykać sesje w sposób uporządkowany. Referencja używa TerminateProcess, które pomija logikę FormClose.
8. W Dual częściowy błąd łączenia/zapisu wymaga obsługi obu sesji i jawnego stanu błędu. Nie należy zgłaszać sukcesu na podstawie operacji wykonanej tylko na jednym urządzeniu.

To wnioski ze statycznego odczytu, a nie wyniki uruchomienia referencji. Zgodność funkcjonalna oznacza zachowanie operacji dostępnych użytkownikowi; nie wymaga odtwarzania błędów.

## Weryfikacja przyszłej implementacji

Testy protokołu na symulowanym transporcie: ramki, błędne i częściowe odpowiedzi, limity, utrata połączenia. Testy logiki Dual: nieparzyste jednostki nastaw, zakresy i awaria jednego urządzenia. Testy eksportu: polskie ustawienia regionalne, czas i kolumny. Kontrola wyglądu obu okien i skalowania DPI.

Test fizycznego zasilacza jest osobnym etapem: testy programowe nie potwierdzają rzeczywistego wykonania poleceń ani fizycznych połączeń Dual. W tym przeglądzie nie otwierano portów COM i nie wysyłano poleceń.

## Źródła

- Pliki wymienione w tabeli, `Korad/SerialPort.cpp`, `Korad/ka3005p_CB.cpp` oraz `Dual Korad/PowerSupplyThread.cpp` w podanym katalogu referencyjnym.
- Dwa zrzuty ekranu załączone przez użytkownika.
- [Repozytorium użytkownika](https://github.com/Wilk33/KA3005P-App).
- [Oficjalny tekst licencji](https://polyformproject.org/licenses/noncommercial/1.0.0.txt).
- [Microsoft: przegląd WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/).
- [Microsoft: tworzenie aplikacji WPF na .NET 10 LTS](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/get-started/create-app-visual-studio).
