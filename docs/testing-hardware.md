# Weryfikacja KA3005P App

## Zakres sprawdzony automatycznie

Testy automatyczne używają atrap transportu i urządzeń. Potwierdzają między innymi:

- dokładne ramki `VSET1:`, `ISET1:`, `OUT0`, `OUT1`, `VOUT1?` i `IOUT1?`;
- brak automatycznych zapytań `VSET1?`, `ISET1?` i `STATUS?`;
- łączenie częściowych odczytów i błąd przy niepełnej odpowiedzi;
- wyłączny dostęp jednej sesji do portu i brak nakładających się operacji;
- ograniczoną kolejkę nastaw oraz priorytet OFF;
- brak blokowania szybkich zmian nastaw podczas trwającego pomiaru Dual;
- obliczenia trybów szeregowego, równoległego i symetrycznego;
- wspólny stan ON/OFF okna głównego i wykresu;
- format eksportu CSV.

Testy automatyczne są uzupełniane testem fizycznym opisanym niżej. Same atrapy nadal nie dowodzą wykonania polecenia przez urządzenie.

## Kontrola w trybie demonstracyjnym

Uruchom:

```powershell
dotnet run --project src/Ka3005P.App -- --demo
```

Sprawdź kolejno:

1. Otwórz pojedynczy Korad, wybierz `Offline`, ustaw 12,00 V i 1,000 A, a następnie ON.
2. Zmieniaj napięcie i prąd podczas ON. Edytory i okno muszą reagować natychmiast.
3. Otwórz wykres. Stan ON/OFF musi pozostać wspólny, a wykres ma zachować najwyżej 50 punktów.
4. Zapisz osobno napięcie i prąd do CSV.
5. Otwórz Dual, połącz dwa różne fikcyjne porty i sprawdź trzy tryby przy OFF.
6. Powtórz kontrolę przy skalowaniu Windows 100%, 125%, 150% i 200%. Sprawdź polskie teksty, widoczność fokusu, obsługę klawiatury, kontrast lampek i brak uciętych wartości.

### Wynik kontroli pierwszej wersji - 2026-09-25

Opublikowany pakiet `win-x64` uruchomiono z argumentem `--demo` bez portów COM. Windows zgłosił dla kontrolowanych okien 96 DPI, czyli skalowanie 100%. Sprawdzono menedżer, pojedynczy Korad, Dual Korad, przejście `Offline -> Online -> ON`, pomiar 12,00 V z symulowanym prądem obciążenia, wspólny stan ON w oknie wykresu oraz zamykanie niepołączonych okien pojedynczego i Dual. Proces pozostał aktywny po obu operacjach zamknięcia. Po kontroli usunięto dwa wykryte problemy: brak jawnego szarego tła w oknach pochodnych oraz brak ponownej oceny aktywności komendy ON/OFF po połączeniu.

Skalowania 125%, 150% i 200% nie zostały faktycznie przełączone w tej sesji, ponieważ wymagałoby to zmiany ustawień wyświetlania systemu użytkownika. Pozostają częścią procedury odbioru.

### Wynik testu fizycznego - 2026-09-25

Windows wykrył dwa urządzenia `USB\\VID_0416&PID_5011` jako COM3 i COM4. Wyjścia zasilaczy były połączone równolegle i pracowały w ograniczeniu prądu.

Surowa sonda transmisji potwierdziła:

- zapis polecenia do sterownika Windows trwał zwykle 4-9 ms;
- pełna odpowiedź na zapytanie pomiarowe COM3 trwała około 42-56 ms, a COM4 około 26-30 ms;
- odpowiedź ma dokładnie 5 bajtów ASCII;
- przy nastawie 1,00 V i 0,100 A urządzenia zwracały `00.00` V oraz `0.098`-`0.100` A;
- po zmianie do 0,120 A oba urządzenia zwróciły `0.119` A;
- po końcowej nastawie 1,50 V i 0,200 A na urządzenie kod aplikacji zmierzył 0,00 V i 0,399 A łącznie;
- końcowe `OUT0` wykonano na obu portach.

Test bez odstępów pomiędzy 84 bezpośrednimi zapisami spowodował reset USB urządzenia COM4. Projekt referencyjny stosuje 45 ms przerwy po operacjach. Nowy transport zachowuje ten odstęp w wątku roboczym, więc interfejs nie jest blokowany. Kolejka nadal scala oczekujące nastawy i wysyła najnowszą wartość.

Podczas pierwszej próby asynchroniczny `BaseStream.ReadAsync` nie respektował niezawodnie anulowania na porcie szeregowym Windows. Transport został zmieniony na ograniczone czasowo operacje `SerialPort.Read` i `SerialPort.Write`. Po tej zmianie brak odpowiedzi kończy operację błędem zamiast zawieszać proces.

Powtarzalny test właściwej kolejki aplikacji:

```powershell
dotnet run --project tools/Ka3005P.HardwareSmoke/Ka3005P.HardwareSmoke.csproj -c Release -- COM3 COM4
```

Surowy zapis odpowiedzi i czasów transmisji:

```powershell
dotnet run --project tools/Ka3005P.HardwareSmoke/Ka3005P.HardwareSmoke.csproj -c Release -- --probe COM3 COM4
```

Oba tryby narzędzia wykonują `OUT0` w bloku `finally`.

## Procedura testu fizycznego

Przed testem ustaw bezpieczne limity odpowiednie dla obciążenia. Zmiana trybu w aplikacji nie zmienia okablowania.

1. Jeden zasilacz, wyjście OFF:
	- połącz właściwy port COM;
	- ustaw małe napięcie i limit prądu;
	- potwierdź na panelu zasilacza zmianę nastawy;
	- sprawdź, że pomiar wyjścia może pozostać zerowy przy OFF.
2. Jeden zasilacz, wyjście ON:
	- podłącz bezpieczne obciążenie;
	- włącz wyjście;
	- zmieniaj nastawy podczas aktywnego pomiaru;
	- sprawdź reakcję interfejsu, panelu zasilacza, wykres i CSV;
	- użyj OFF podczas pomiaru i potwierdź szybkie wyłączenie.
3. Dwa zasilacze:
	- przy OFF ręcznie przygotuj i zweryfikuj połączenie szeregowe;
	- wykonaj test małych nastaw, pomiarów i OFF;
	- wyłącz oba urządzenia i dopiero wtedy przygotuj połączenie równoległe;
	- wykonaj ten sam test, zwracając uwagę na podział prądu;
	- wyłącz oba urządzenia i przygotuj układ symetryczny z poprawnym punktem wspólnym;
	- wykonaj ten sam test, weryfikując znaki i polaryzację miernikiem.
4. Awaria częściowa:
	- przy OFF odłącz jeden port Dual;
	- sprawdź, czy aplikacja wskazuje właściwe urządzenie i nie pokazuje wspólnego sukcesu;
	- po teście ponownie wyłącz oba wyjścia z paneli urządzeń.

Wynik testu fizycznego powinien zawierać model i wersję urządzenia, wersję Windows, użyte porty COM, rodzaj obciążenia, tryb połączenia, wartości nastaw oraz zauważone różnice względem paneli zasilaczy.
