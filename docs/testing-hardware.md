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

Te testy nie dowodzą, że konkretny fizyczny zasilacz wykonał polecenie. Pierwsza wersja nie została jeszcze potwierdzona na rzeczywistym KA3005P w ramach tej sesji.

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

Skalowania 125%, 150% i 200% nie zostały faktycznie przełączone w tej sesji, ponieważ wymagałoby to zmiany ustawień wyświetlania systemu użytkownika. Pozostają częścią procedury odbioru. Test sprzętowy również pozostaje niewykonany.

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
