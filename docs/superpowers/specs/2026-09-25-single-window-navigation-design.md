# KA3005P App - jedno okno i dynamiczny wybór portów COM

## Cel

Przebudować interfejs pierwszej wersji tak, aby aplikacja miała jedno główne okno, uruchamiała się domyślnie w trybie pojedynczego zasilacza i pozwalała przełączać Single/Dual z menu. Usunąć menedżer oraz zależne główne okna. Zastąpić tekstowe pola portów listami rzeczywiście dostępnych portów COM.

## Potwierdzone wymagania

- Po uruchomieniu aktywny jest tryb `Pojedynczy`.
- Menu `Tryb pracy` zawiera pozycje `Pojedynczy` i `Dual` działające jak wzajemnie wykluczające się opcje.
- Zmiana Single/Dual zastępuje zawartość tego samego okna.
- Przy zmianie Single/Dual aplikacja zawsze wykonuje OFF, zatrzymuje i usuwa wszystkie sesje poprzedniego trybu, zwalnia porty oraz tworzy nowy tryb w stanie Offline.
- Nie zachowuje się sesji zasilacza 1 podczas przejścia Dual -> Single.
- Zmiana podtrybu Dual na szeregowy, równoległy albo symetryczny zawsze wykonuje OFF obu urządzeń, ale pozostawia obie sesje Online.
- Główne okno nie może być skalowane. Jedynym skalowalnym oknem pozostaje wykres.
- Port wybiera się z listy aktywnych portów COM, a nie przez wpisanie tekstu lub samego numeru.
- Lista portów odświeża się co 1 sekundę.
- W Dual ten sam port nie może być wybrany dla obu urządzeń.
- Zniknięcie portu z systemu nie zamyka aktywnej sesji na podstawie samego katalogu portów. Rzeczywisty błąd transmisji kończy sesję i pokazuje trwały błąd. OFF nie jest wtedy gwarantowany, jeżeli urządzenie fizycznie przestało odpowiadać.
- ON/OFF w głównym widoku i na wykresie ma identyczną dostępność. Przy Offline oba przyciski są nieaktywne.
- Aplikacja otrzymuje ikonę okna oraz pliku EXE z istniejącego zasobu referencyjnego.
- Zakończone etapy są wysyłane do repozytorium GitHub.

## Wybrana architektura

### Jedno główne okno

`MainWindow` jest jedynym głównym oknem WPF. Ma `ResizeMode="NoResize"` i programowo dobierany stały rozmiar klienta: 300 x 455 dla Single oraz 480 x 590 dla Dual. Użytkownik nie może zmieniać tych wymiarów. Okno ma menu:

- `Tryb pracy -> Pojedynczy`
- `Tryb pracy -> Dual`
- `Wykres`
- `Zapisz jako -> Napięcie CSV`
- `Zapisz jako -> Prąd CSV`

Pozycje trybu są zaznaczane wzajemnie wykluczająco. Kliknięcie już aktywnej pozycji niczego nie zmienia.

`MainWindowViewModel` jest właścicielem bieżącego modelu trybu, wspólnego `SerialPortMonitor` i komend nawigacji. Monitor działa przez cały czas życia głównego okna, jest współdzielony przez kolejne modele trybu i zostaje zatrzymany dopiero podczas zamykania aplikacji. Widok roboczy jest umieszczany w `ContentControl`:

- `SingleSupplyView` dla `SingleSupplyViewModel`;
- `DualSupplyView` dla `DualSupplyViewModel`.

Zmiana trybu nie ukrywa starego panelu. Stary model jest zamykany i usuwany, a nowy model oraz kontrolka są tworzone od początku.

### Cykl życia zmiany Single/Dual

Komenda zmiany głównego trybu jest asynchroniczna i nie pozwala na równoległe wykonanie:

1. Wyłącza możliwość ponownej zmiany trybu.
2. Zamyka wykres związany ze starym modelem.
3. Wywołuje `CloseAsync` starego modelu.
4. `CloseAsync` próbuje wysłać OFF do wszystkich aktywnych urządzeń.
5. Niezależnie od wyniku OFF zatrzymuje sesje, zwalnia dzierżawy portów i usuwa subskrypcje.
6. Tworzy nowy model w stanie Offline.
7. Podmienia zawartość `ContentControl`.
8. Ustawia zaznaczenie właściwej pozycji menu i dopasowuje stały rozmiar okna.

Błąd OFF jest prezentowany użytkownikowi, ale nie zatrzymuje zwalniania zasobów. Nowy tryb nadal uruchamia się Offline.

### Zmiana podtrybu Dual

Zmiana `Szeregowy`, `Równoległy` albo `Symetryczny` jest komendą asynchroniczną i nie pozwala na równoległe wykonanie:

1. Jeżeli wyjście jest ON, wykonuje wspólne OFF na obu zasilaczach.
2. Jeżeli OFF nie powiedzie się dla któregokolwiek urządzenia, pokazuje błąd, pozostawia poprzedni podtryb i nie wysyła nowych nastaw. Sesje pozostają dostępne do jawnego rozłączenia lub zostają zakończone przez obsługę błędu transmisji.
3. Po poprawnym OFF zachowuje obie sesje i dzierżawy portów.
4. Ustawia nowy podtryb kontrolera.
5. Przelicza oraz publikuje fizyczne nastawy obu urządzeń.
6. Pozostawia wspólny stan wyjścia OFF.

Użytkownik musi ponownie świadomie wykonać ON.

## Katalog aktywnych portów COM

### Interfejs

`ISerialPortCatalog` udostępnia:

```csharp
IReadOnlyList<string> GetPortNames();
```

Produkcja używa `SerialPort.GetPortNames()`. Tryb demonstracyjny używa `DemoSerialPortCatalog`, który udostępnia stabilny zestaw `COM5`, `COM6` i `COM7`, aby interfejs demonstracyjny działał także bez fizycznych portów. Wynik każdego katalogu jest:

- normalizowany do wielkich liter;
- deduplikowany bez uwzględniania wielkości liter;
- sortowany naturalnie według numeru, więc `COM2` występuje przed `COM10`;
- publikowany jako niemodyfikowalna migawka.

`SerialPortMonitor` odświeża katalog co 1 sekundę poza wątkiem WPF i publikuje zmianę tylko wtedy, gdy migawka rzeczywiście się zmieniła. `MainWindowViewModel` przenosi aktualizację kolekcji na wątek UI i przekazuje migawkę aktywnemu modelowi trybu.

### Single

`SingleSupplyViewModel` ma kolekcję `AvailablePorts` i `SelectedPort`. `ComboBox` nie pozwala wpisywać dowolnego tekstu. Po utworzeniu model próbuje przywrócić `SinglePort` z ustawień, jeżeli nadal występuje w katalogu. W przeciwnym razie automatycznie wybiera pierwszy dostępny port. Connect jest aktywny tylko wtedy, gdy wybrano istniejący port i aplikacja jest Offline.

Podczas Online lista pozostaje widoczna, ale wybór portu jest zablokowany. Zniknięcie portu z katalogu nie usuwa bieżącej wartości z kontrolki do czasu zakończenia sesji. Błąd rzeczywistej transmisji przełącza model w Offline i pokazuje komunikat.

### Dual

`DualSupplyViewModel` ma:

- `AvailableFirstPorts`;
- `AvailableSecondPorts`;
- `SelectedFirstPort`;
- `SelectedSecondPort`.

Po wybraniu portu 1 jest on usuwany z listy portu 2. Po wybraniu portu 2 jest usuwany z listy portu 1, z wyjątkiem aktualnie wybranej wartości własnej kontrolki. Daje to dwie poprawne listy bez możliwości wybrania tego samego portu.

Po utworzeniu model próbuje przywrócić `DualFirstPort` i `DualSecondPort` z ustawień, o ile oba nadal istnieją i są różne. Brakujące wybory uzupełnia pierwszymi dostępnymi, wzajemnie różnymi portami. Jeżeli istnieje mniej niż dwa porty, Connect pozostaje nieaktywny.

Podczas Online oba `ComboBox` są zablokowane. Connect wymaga dwóch różnych, istniejących portów.

## Wspólny stan wykresu

`IOutputController` zostaje rozszerzony o stan połączenia oraz zdarzenie jego zmiany. `ChartViewModel.ToggleOutputCommand.CanExecute` zwraca `true` wyłącznie wtedy, gdy bieżący model jest Online. Ta sama reguła zasila przycisk w głównym widoku.

Wykres nie tworzy sesji ani dodatkowej pętli odpytywania. Nadal subskrybuje próbki istniejącego modelu. `ChartWindowService` utrzymuje co najwyżej jedno okno wykresu dla aktywnego modelu i udostępnia jego jawne zamknięcie. Podczas zmiany Single/Dual okno wykresu jest zamykane przed usunięciem starego modelu.

## Ikona aplikacji

Do repozytorium zostanie skopiowany istniejący `Korad Manager_Icon.ico` z projektu referencyjnego. Nazwa historyczna zasobu nie wpływa na interfejs i nie przywraca koncepcji menedżera.

Plik projektu ustawia:

```xml
<ApplicationIcon>Assets\KA3005P.ico</ApplicationIcon>
```

`MainWindow` i `ChartWindow` używają tej samej ikony. Ikona jest osadzona w EXE i widoczna na pasku zadań, pasku tytułu oraz w Eksploratorze Windows.

## Usuwane elementy

- `ManagerWindow`;
- `ManagerViewModel`;
- `IWindowService` i `WindowService`;
- `SingleSupplyWindow`;
- `DualSupplyWindow`;
- opcja uruchamiania dwóch niezależnych okien Single.

Logika Single i Dual pozostaje w istniejących modelach po dostosowaniu wyboru portów i cyklu życia. Kod sterowania urządzeniami, kolejki sesji, brak automatycznych `VSET1?`, `ISET1?` i `STATUS?` oraz pomiary `VOUT1?`/`IOUT1?` pozostają bez zmian.

## Obsługa błędów

- Brak portów pokazuje pustą listę i nieaktywny Connect.
- Błąd odświeżenia katalogu nie kończy aktywnej sesji. Zachowuje ostatnią poprawną migawkę i pokazuje jeden komunikat diagnostyczny.
- Błąd otwarcia portu wskazuje konkretny port.
- Błąd drugiego portu Dual zamyka utworzoną sesję pierwszego portu i zwalnia obie dzierżawy.
- Zniknięcie portu podczas Online jest rozstrzygane przez rzeczywistą operację transportu, nie przez sam monitor nazw.
- Każdy `DeviceCommunicationException` podczas sterowania lub pomiaru oznacza utratę połączenia. Model zatrzymuje uszkodzoną sesję, zwalnia port, przechodzi do Offline i zachowuje komunikat błędu do chwili kolejnej jawnej operacji użytkownika.
- W Dual utrata jednej sesji kończy także drugą sesję po próbie OFF sprawnego zasilacza, ponieważ tryb Dual nie może pozostać częściowo połączony.
- Zmiana trybu zawsze próbuje zwolnić wszystkie zasoby także po błędzie OFF.

## Testy akceptacyjne

- Start aplikacji pokazuje Single bez okna menedżera.
- Główne okno Single i Dual ma zablokowaną zmianę rozmiaru.
- Wykres pozostaje skalowalny.
- Menu trybu zaznacza dokładnie jedną opcję.
- Zmiana Single/Dual wykonuje OFF, zamyka stare sesje i tworzy nowy model Offline.
- Zmiana podtrybu Dual podczas ON wykonuje OFF obu urządzeń, zachowuje sesje i wysyła nowe nastawy dopiero po poprawnym OFF obu urządzeń.
- Błąd OFF podczas zmiany podtrybu Dual pozostawia poprzedni podtryb i nie wysyła nowych nastaw.
- Listy COM odświeżają się po zmianie katalogu bez ponownego uruchomienia aplikacji.
- Naturalne sortowanie daje `COM2` przed `COM10`.
- Wybrany port 1 nie jest dostępny na liście portu 2 i odwrotnie.
- Single przywraca zapisany dostępny port albo wybiera pierwszy port, a Dual wybiera dwa różne dostępne porty.
- Connect nie jest możliwy bez poprawnego wyboru portu.
- Główne ON/OFF i wykres mają identyczne `CanExecute` przy Offline oraz Online.
- Zamknięcie głównego okna wykonuje OFF i zwalnia wszystkie sesje.
- Opublikowany EXE ma ikonę aplikacji.
- Tryb demonstracyjny udostępnia `COM5`, `COM6` i `COM7` niezależnie od sprzętu komputera.
- Pełny zestaw dotychczasowych testów protokołu i kolejek nadal przechodzi.

## Ograniczenia

- Monitor portów opiera się na `SerialPort.GetPortNames()` i nie identyfikuje modelu urządzenia podłączonego do portu.
- Lista potwierdza obecność nazwy portu w Windows, nie poprawność komunikacji z KA3005P.
- Testy automatyczne nie potwierdzają fizycznego wykonania OFF ani nastawy przez zasilacz.
- Zmiana ustawień DPI 125%, 150% i 200% pozostaje częścią ręcznego odbioru.
