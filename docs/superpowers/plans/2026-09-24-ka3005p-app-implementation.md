# KA3005P App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zbudować kompletną aplikację WPF do obsługi pojedynczego zasilacza KA3005P i dwóch zasilaczy w trybach Dual, bez blokowania interfejsu podczas aktywnych pomiarów i zmian nastaw.

**Architecture:** Każdy fizyczny port COM ma jedną sesję i jedną kolejkę wykonawczą. Interfejs tylko publikuje żądania i odbiera migawki stanu, a dwa urządzenia Dual pracują na oddzielnych sesjach, które mogą wykonywać operacje równolegle.

**Tech Stack:** C# 14, WPF, .NET 10, System.IO.Ports 10.0.12, xUnit 2.9.3, Microsoft.NET.Test.Sdk 18.10.1, xunit.runner.visualstudio 3.1.5, coverlet.collector 10.0.1.

**Spec:** `docs/superpowers/specs/2026-09-24-ka3005p-app-design.md`

## Global Constraints

- Docelowy system: Windows, `net10.0-windows`, WPF.
- Przypięty SDK: 10.0.401 w `global.json`.
- Kod produkcyjny projektu jest objęty PolyForm Noncommercial License 1.0.0.
- Projekt referencyjny w OneDrive jest tylko do odczytu.
- Nie dodawać zewnętrznego frameworka MVVM ani biblioteki wykresów.
- Każdy port COM ma dokładnie jednego właściciela.
- Żadna operacja COM, zapis CSV ani przeliczanie Dual nie wykonuje się na wątku WPF.
- Nie wysyłać automatycznie `VSET1?`, `ISET1?` ani `STATUS?`.
- `VOUT1?` i `IOUT1?` są pomiarami wyjścia, nie odczytem nastaw.
- Pomiary działają tylko podczas ON i nie mogą tworzyć zaległej kolejki.
- Szybkie zmiany zastępują starszą, jeszcze niewysłaną nastawę tego samego rodzaju.
- W kodzie używać tabulatorów, klamry otwierającej w nowej linii i stylu operatorów z głównego pliku AGENTS.md.
- Testy automatyczne nie mogą być opisywane jako dowód fizycznego wykonania polecenia przez zasilacz.

## Review Focus

- Odpowiedź krótsza niż pięć bajtów lub timeout: zwrócić błąd komunikacji, nie wartość 0.
- Seria kilkudziesięciu zmian podczas ON: kolejka pozostaje ograniczona, a ostatnia wartość dociera do obu sesji Dual.
- Błąd jednego portu Dual: wynik wspólnej operacji jest częściowym błędem, a stan nie jest przedstawiany jako wspólny sukces.
- OFF zgłoszone podczas wolnego odczytu: wykonać po ograniczonym timeout bieżącej operacji i przed nastawami oraz następnym pomiarem.
- Niedostępny katalog, pełny bufor lub błąd zapisu CSV: zatrzymać rejestrację i pokazać jeden trwały błąd bez zamrożenia interfejsu.

## Mapa plików

`global.json` przypina SDK. `Directory.Build.props` ustawia analizatory, nullable, kodowanie i deterministyczny build. `Ka3005P.sln` łączy trzy projekty.

`src/Ka3005P.Core`:

- `Protocol/VoltageSetpoint.cs` - setne wolta i walidacja 0,00-62,00 V.
- `Protocol/CurrentSetpoint.cs` - tysięczne ampera i walidacja 0,000-10,200 A.
- `Protocol/Ka3005PCommandCodec.cs` - kodowanie sześciu używanych poleceń i parsowanie pomiarów.
- `Transport/ISerialTransport.cs` - abstrakcja bajtowego transportu.
- `Transport/SerialPortTransport.cs` - produkcyjny adapter 9600 8N1 z wyłączonym DTR.
- `Device/IPowerSupplyDevice.cs` - operacje jednego urządzenia.
- `Device/Ka3005PDevice.cs` - pełny zapis i dokładny odczyt pięciu bajtów.
- `Sessions/PowerSupplySession.cs` - jedyny właściciel urządzenia, priorytety i pętla wykonawcza.
- `Sessions/SessionSnapshot.cs` - niezmienny stan żądany, wysłany, pomiar i błąd.
- `Sessions/SessionRequestQueue.cs` - kolejka sterowania oraz dwa zastępowalne miejsca na nastawy.
- `Dual/DualSetpointCalculator.cs` - obliczenia w jednostkach całkowitych.
- `Dual/DualPowerSupplyController.cs` - koordynacja dwóch sesji.
- `Measurements/MeasurementRecorder.cs` - ograniczony bufor próbek.
- `Measurements/CsvMeasurementWriter.cs` - plik CSV i format niezmienny regionalnie.
- `Measurements/ResistanceFormatter.cs` - bezpieczne obliczenie i zapis rezystancji.
- `Configuration/AppSettings.cs` - ustawienia użytkownika.
- `Configuration/JsonSettingsStore.cs` - zapis atomowy JSON.
- `Configuration/PortLeaseRegistry.cs` - blokada podwójnego użycia COM.
- `Diagnostics/IAppLog.cs` i `Diagnostics/FileAppLog.cs` - lokalny dziennik błędów bez modalnej lawiny komunikatów.

`src/Ka3005P.App`:

- `App.xaml` i `App.xaml.cs` - uruchomienie i składanie zależności.
- `Themes/KoradTheme.xaml` - paleta, typografia i wspólne style.
- `Infrastructure/ObservableObject.cs` - powiadomienia właściwości.
- `Infrastructure/RelayCommand.cs` i `AsyncRelayCommand.cs` - komendy bez zewnętrznego MVVM.
- `ViewModels/ManagerViewModel.cs` - tworzenie okien i wybór folderu.
- `ViewModels/SingleSupplyViewModel.cs` - stan jednej sesji.
- `ViewModels/DualSupplyViewModel.cs` - stan kontrolera Dual.
- `ViewModels/ChartViewModel.cs` - 50 ostatnich próbek.
- `Views/ManagerWindow.xaml` - odpowiednik Korad Manager.
- `Views/SingleSupplyWindow.xaml` - kompaktowy pojedynczy Korad.
- `Views/DualSupplyWindow.xaml` - tryby szeregowy, równoległy i symetryczny.
- `Views/ChartWindow.xaml` - osobne okno wykresu.
- `Controls/SetpointEditor.xaml` - edycja krokowa napięcia i prądu.
- `Controls/StatusLamps.xaml` - trzy lampki stanu.
- `Controls/CurrentChart.cs` - lekki wykres WPF bez zależności.

`tests/Ka3005P.Tests` odwzorowuje katalogi Core i ViewModels oraz zawiera `Fakes/FakeSerialTransport.cs`, `Fakes/FakePowerSupplyDevice.cs`, `Fakes/FakePowerSupplySession.cs`, `Fakes/ManualTimeProvider.cs` i `Integration/TestApplication.cs`.

---

### Task 1: Fundament rozwiązania i kodek protokołu

**Files:**
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `.gitignore`
- Create: `Ka3005P.sln`
- Create: `src/Ka3005P.Core/Ka3005P.Core.csproj`
- Create: `src/Ka3005P.App/Ka3005P.App.csproj`
- Create: `tests/Ka3005P.Tests/Ka3005P.Tests.csproj`
- Create: `src/Ka3005P.Core/Protocol/VoltageSetpoint.cs`
- Create: `src/Ka3005P.Core/Protocol/CurrentSetpoint.cs`
- Create: `src/Ka3005P.Core/Protocol/Ka3005PCommandCodec.cs`
- Test: `tests/Ka3005P.Tests/Protocol/Ka3005PCommandCodecTests.cs`

**Interfaces:**
- Produces: `VoltageSetpoint.FromHundredths(int)`, `CurrentSetpoint.FromThousandths(int)`, `Ka3005PCommandCodec.SetVoltage(VoltageSetpoint)`, `SetCurrent(CurrentSetpoint)`, `SetOutput(bool)`, `ReadVoltage()`, `ReadCurrent()`, `ParseVoltage(ReadOnlySpan<byte>)`, `ParseCurrent(ReadOnlySpan<byte>)`.

- [ ] **Step 1: Sprawdź i przygotuj SDK 10.0.401**

Run: `dotnet --list-sdks`

Expected: lista zawiera `10.0.401`. Jeżeli nie zawiera, zainstaluj zweryfikowaną oficjalną wersję:

```powershell
winget install --id Microsoft.DotNet.SDK.10 --version 10.0.401 --exact --source winget
```

Po instalacji ponownie uruchom `dotnet --list-sdks` i nie przechodź dalej bez wpisu `10.0.401`.

- [ ] **Step 2: Przypnij środowisko i utwórz projekty**

Run:

```powershell
dotnet new globaljson --sdk-version 10.0.401 --roll-forward latestPatch
dotnet new sln --format sln -n Ka3005P
dotnet new gitignore
dotnet new classlib -n Ka3005P.Core -o src/Ka3005P.Core -f net10.0
dotnet new wpf -n Ka3005P.App -o src/Ka3005P.App -f net10.0
dotnet new xunit -n Ka3005P.Tests -o tests/Ka3005P.Tests -f net10.0
dotnet sln Ka3005P.sln add src/Ka3005P.Core/Ka3005P.Core.csproj src/Ka3005P.App/Ka3005P.App.csproj tests/Ka3005P.Tests/Ka3005P.Tests.csproj
dotnet add src/Ka3005P.App/Ka3005P.App.csproj reference src/Ka3005P.Core/Ka3005P.Core.csproj
dotnet add tests/Ka3005P.Tests/Ka3005P.Tests.csproj reference src/Ka3005P.Core/Ka3005P.Core.csproj src/Ka3005P.App/Ka3005P.App.csproj
```

Add `System.IO.Ports` 10.0.12 to Core. Set test references to Microsoft.NET.Test.Sdk 18.10.1, xUnit 2.9.3, xunit.runner.visualstudio 3.1.5 and coverlet.collector 10.0.1. Runner and collector use `PrivateAssets=all`. Remove template `Class1.cs` and `UnitTest1.cs`.

`Directory.Build.props`:

```xml
<Project>
	<PropertyGroup>
		<LangVersion>14.0</LangVersion>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
		<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
		<Deterministic>true</Deterministic>
		<ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
	</PropertyGroup>
</Project>
```

Testowy plik projektu zawiera dokładnie:

```xml
<ItemGroup>
	<PackageReference Include="coverlet.collector" Version="10.0.1" PrivateAssets="all" />
	<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
	<PackageReference Include="xunit" Version="2.9.3" />
	<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" PrivateAssets="all" />
</ItemGroup>
```

- [ ] **Step 3: Napisz testy wartości granicznych i dokładnych ramek**

```csharp
[Theory]
[InlineData(0, "VSET1:00.00")]
[InlineData(1200, "VSET1:12.00")]
[InlineData(3100, "VSET1:31.00")]
public void SetVoltage_FormatsHundredths(int raw, string expected)
{
	VoltageSetpoint value=VoltageSetpoint.FromHundredths(raw);
	Assert.Equal(expected, Encoding.ASCII.GetString(Ka3005PCommandCodec.SetVoltage(value)));
}

[Fact]
public void ParseVoltage_RejectsPartialReply()
{
	Assert.Throws<ProtocolException>(()=>Ka3005PCommandCodec.ParseVoltage("12.0"u8));
}
```

Dodaj analogiczne testy dla `ISET1:0.000`, `ISET1:5.100`, `OUT0`, `OUT1`, `VOUT1?`, `IOUT1?`, polskiej kultury oraz odrzucenia wartości poza zakresem. Kodek urządzenia musi odrzucić 31,01 V i 5,101 A, nawet jeśli typ logiczny dopuszcza 62,00 V lub 10,200 A przed podziałem Dual.

- [ ] **Step 4: Uruchom test i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~Ka3005PCommandCodecTests`

Expected: FAIL, ponieważ typy protokołu jeszcze nie istnieją.

- [ ] **Step 5: Zaimplementuj typy jednostek i kodek**

```csharp
public readonly record struct VoltageSetpoint
{
	public int Hundredths { get; }

	private VoltageSetpoint(int hundredths)
	{
		Hundredths=hundredths;
	}

	public static VoltageSetpoint FromHundredths(int value)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(value);
		if(value > 6200)
		{
			throw new ArgumentOutOfRangeException(nameof(value));
		}
		return new VoltageSetpoint(value);
	}
}
```

`Ka3005PCommandCodec` formatuje wyłącznie przez `CultureInfo.InvariantCulture` i zwraca `byte[]`. Parser wymaga dokładnie pięciu bajtów i formatu `dd.dd` dla napięcia albo `d.ddd` dla prądu. Metody kodujące nastawy dodatkowo pilnują fizycznych granic jednego urządzenia: 3100 setnych wolta i 5100 tysięcznych ampera.

- [ ] **Step 6: Uruchom testy i build**

Run: `dotnet test Ka3005P.sln && dotnet build Ka3005P.sln -c Release`

Expected: PASS, zero ostrzeżeń.

- [ ] **Step 7: Commit**

```powershell
git add global.json Directory.Build.props .gitignore Ka3005P.sln src tests
git commit -m "feat: add solution and KA3005P protocol codec"
```

### Task 2: Transport szeregowy i klient urządzenia

**Files:**
- Create: `src/Ka3005P.Core/Transport/ISerialTransport.cs`
- Create: `src/Ka3005P.Core/Transport/SerialPortTransport.cs`
- Create: `src/Ka3005P.Core/Device/IPowerSupplyDevice.cs`
- Create: `src/Ka3005P.Core/Device/Ka3005PDevice.cs`
- Create: `src/Ka3005P.Core/Device/DeviceCommunicationException.cs`
- Create: `src/Ka3005P.Core/Device/DeviceMeasurement.cs`
- Create: `tests/Ka3005P.Tests/Fakes/FakeSerialTransport.cs`
- Test: `tests/Ka3005P.Tests/Device/Ka3005PDeviceTests.cs`

**Interfaces:**
- Consumes: kodek z Task 1.
- Produces: `ISerialTransport.OpenAsync(string, CancellationToken)`, `WriteAsync(ReadOnlyMemory<byte>, CancellationToken)`, `ReadAsync(Memory<byte>, CancellationToken)`; `IPowerSupplyDevice.SetVoltageAsync`, `SetCurrentAsync`, `SetOutputAsync`, `ReadMeasurementAsync`.

- [ ] **Step 1: Napisz fake transportu i test częściowych odczytów**

```csharp
[Fact]
public async Task ReadMeasurementAsync_CombinesPartialReadsWithoutConcurrentIo()
{
	FakeSerialTransport transport=new();
	transport.QueueRead("12"u8.ToArray());
	transport.QueueRead(".34"u8.ToArray());
	transport.QueueRead("0.123"u8.ToArray());
	Ka3005PDevice device=new(transport, TimeSpan.FromMilliseconds(200));

	DeviceMeasurement result=await device.ReadMeasurementAsync(CancellationToken.None);

	Assert.Equal(1234, result.VoltageHundredths);
	Assert.Equal(123, result.CurrentThousandths);
	Assert.Equal(1, transport.MaximumConcurrentOperations);
	Assert.Equal(new[]{"VOUT1?","IOUT1?"}, transport.WritesAsAscii);
}
```

Dodaj testy: timeout przed pięcioma bajtami, zero bajtów, błąd zapisu, pełny zapis każdej nastawy, brak `VSET1?`, `ISET1?` i `STATUS?`.

- [ ] **Step 2: Uruchom test i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~Ka3005PDeviceTests`

Expected: FAIL, brak transportu i klienta.

- [ ] **Step 3: Zaimplementuj interfejsy i dokładny odczyt**

```csharp
private async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken)
{
	int offset=0;
	while(offset < buffer.Length)
	{
		int read=await transport.ReadAsync(buffer[offset..], cancellationToken);
		if(read == 0)
		{
			throw new DeviceCommunicationException("Port został zamknięty przed odebraniem pełnej odpowiedzi.");
		}
		offset+=read;
	}
}
```

`SerialPortTransport` ustawia 9600 bit/s, 8 bitów danych, brak parzystości, jeden bit stopu, DTR wyłączone oraz osobne, ograniczone timeouty. Klient zawsze kończy pełny zapis przed następną operacją.

- [ ] **Step 4: Uruchom testy, w tym z opóźnieniem i anulowaniem**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter "FullyQualifiedName~Protocol|FullyQualifiedName~Device"`

Expected: PASS, w rejestrze fake brak równoległych operacji.

- [ ] **Step 5: Commit**

```powershell
git add src/Ka3005P.Core/Transport src/Ka3005P.Core/Device tests/Ka3005P.Tests/Fakes tests/Ka3005P.Tests/Device
git commit -m "feat: add serialized KA3005P device transport"
```

### Task 3: Kolejka sesji, priorytety i zastępowanie nastaw

**Files:**
- Create: `src/Ka3005P.Core/Sessions/IPowerSupplySession.cs`
- Create: `src/Ka3005P.Core/Sessions/SessionRequestQueue.cs`
- Create: `src/Ka3005P.Core/Sessions/PowerSupplySession.cs`
- Create: `src/Ka3005P.Core/Sessions/SessionSnapshot.cs`
- Create: `src/Ka3005P.Core/Sessions/OutputState.cs`
- Create: `src/Ka3005P.Core/Sessions/SessionError.cs`
- Create: `tests/Ka3005P.Tests/Fakes/FakePowerSupplyDevice.cs`
- Test: `tests/Ka3005P.Tests/Sessions/SessionRequestQueueTests.cs`
- Test: `tests/Ka3005P.Tests/Sessions/PowerSupplySessionTests.cs`

**Interfaces:**
- Consumes: `IPowerSupplyDevice` z Task 2.
- Produces: `IPowerSupplySession.RequestVoltage(VoltageSetpoint)`, `RequestCurrent(CurrentSetpoint)`, `SetOutputAsync(bool, CancellationToken)`, `StartAsync`, `StopAsync`, `SnapshotChanged`.

- [ ] **Step 1: Napisz test zastępowania oczekujących nastaw**

```csharp
[Fact]
public void EnqueueVoltage_ReplacesOlderPendingVoltage()
{
	SessionRequestQueue queue=new();
	queue.SetVoltage(VoltageSetpoint.FromHundredths(1200));
	queue.SetVoltage(VoltageSetpoint.FromHundredths(1250));

	SessionRequest request=queue.TakeNext();

	SetVoltageRequest voltage=Assert.IsType<SetVoltageRequest>(request);
	Assert.Equal(1250, voltage.Value.Hundredths);
	Assert.False(queue.TryTakeNext(out _));
}
```

Dodaj testy: napięcie i prąd mają oddzielne miejsca, OFF wyprzedza obie nastawy, żądanie zamknięcia wyprzedza wszystko, wielokrotne OFF nie tworzy nieograniczonej kolejki.

- [ ] **Step 2: Uruchom test kolejki i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~SessionRequestQueueTests`

Expected: FAIL, brak kolejki.

- [ ] **Step 3: Zaimplementuj ograniczoną kolejkę**

```csharp
public void SetVoltage(VoltageSetpoint value)
{
	lock(gate)
	{
		pendingVoltage=new SetVoltageRequest(value);
	}
	signal.Release();
}

public bool TryTakeNext([NotNullWhen(true)] out SessionRequest? request)
{
	lock(gate)
	{
		request=TakeControl()
			?? TakeVoltage()
			?? TakeCurrent();
		return request is not null;
	}
}
```

`SemaphoreSlim` służy tylko do budzenia pętli, a liczba jego sygnałów nie jest liczbą poleceń. Pętla po obudzeniu opróżnia aktualny stan priorytetów.

- [ ] **Step 4: Napisz test sesji z wolnym urządzeniem**

```csharp
[Fact]
public async Task RapidChanges_DoNotWaitForDevice_AndSendLatestPendingValue()
{
	FakePowerSupplyDevice device=new();
	device.BlockNextOperation();
	await using PowerSupplySession session=new(device, TimeProvider.System);
	await session.StartAsync(CancellationToken.None);

	session.RequestVoltage(VoltageSetpoint.FromHundredths(1200));
	await device.WaitUntilOperationStartsAsync();
	for(int value=1201;value<=1250;value++)
	{
		session.RequestVoltage(VoltageSetpoint.FromHundredths(value));
	}
	device.ReleaseOperation();
	await device.WaitForVoltageAsync(1250);

	Assert.Equal(new[]{1200,1250}, device.VoltageWrites);
	Assert.Equal(1, device.MaximumConcurrentOperations);
}
```

Dodaj test OFF podczas wolnego odczytu: po zwolnieniu lub timeout bieżącego odczytu następną operacją musi być OFF, bez wykonania oczekującej nastawy.

- [ ] **Step 5: Zaimplementuj pętlę sesji**

```csharp
private async Task RunAsync(CancellationToken cancellationToken)
{
	while(!cancellationToken.IsCancellationRequested)
	{
		await requests.WaitAsync(cancellationToken);
		while(requests.TryTakeNext(out SessionRequest? request))
		{
			await ExecuteAsync(request, cancellationToken);
		}
	}
}
```

Każde wykonanie aktualizuje `SessionSnapshot`. Pełny zapis ustawia ostatnią wysłaną wartość. Błąd ustawia `SessionError`, ale nie blokuje publikacji kolejnych żądań przez UI.

- [ ] **Step 6: Uruchom testy sesji**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~Sessions`

Expected: PASS, maksymalna współbieżność jednego urządzenia równa 1, kolejka nie rośnie wraz z liczbą zmian.

- [ ] **Step 7: Commit**

```powershell
git add src/Ka3005P.Core/Sessions tests/Ka3005P.Tests/Sessions tests/Ka3005P.Tests/Fakes/FakePowerSupplyDevice.cs
git commit -m "feat: add responsive power supply session queue"
```

### Task 4: Pętla pomiarowa bez zaległych cykli

**Files:**
- Modify: `src/Ka3005P.Core/Sessions/PowerSupplySession.cs`
- Modify: `src/Ka3005P.Core/Sessions/SessionRequestQueue.cs`
- Modify: `src/Ka3005P.Core/Sessions/SessionSnapshot.cs`
- Create: `src/Ka3005P.Core/Measurements/MeasurementSample.cs`
- Create: `tests/Ka3005P.Tests/Fakes/ManualTimeProvider.cs`
- Test: `tests/Ka3005P.Tests/Sessions/MeasurementLoopTests.cs`

**Interfaces:**
- Consumes: sesję z Task 3 i `IPowerSupplyDevice.ReadMeasurementAsync`.
- Produces: `SessionSnapshot.LastMeasurement`, `SessionSnapshot.MeasurementAge`, `MeasurementReceived`.

- [ ] **Step 1: Napisz test braku pomiarów przy OFF i braku backlogu**

```csharp
[Fact]
public async Task Polling_StopsAtOff_AndNeverQueuesSecondMeasurement()
{
	ManualTimeProvider time=new();
	FakePowerSupplyDevice device=new();
	await using PowerSupplySession session=new(device,time,TimeSpan.FromMilliseconds(100));
	await session.StartAsync(CancellationToken.None);

	time.Advance(TimeSpan.FromSeconds(1));
	Assert.Equal(0,device.MeasurementReads);

	await session.SetOutputAsync(true,CancellationToken.None);
	device.BlockNextMeasurement();
	time.Advance(TimeSpan.FromSeconds(1));
	await device.WaitUntilMeasurementStartsAsync();
	time.Advance(TimeSpan.FromSeconds(5));

	Assert.Equal(1,device.MeasurementReads);
	device.ReleaseOperation();
	await session.SetOutputAsync(false,CancellationToken.None);
	time.Advance(TimeSpan.FromSeconds(1));
	Assert.Equal(1,device.MeasurementReads);
}
```

- [ ] **Step 2: Uruchom test i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~MeasurementLoopTests`

Expected: FAIL, sesja nie planuje pomiarów.

- [ ] **Step 3: Dodaj pojedyncze zastępowalne żądanie pomiaru**

```csharp
public void RequestMeasurement()
{
	lock(gate)
	{
		pendingMeasurement??=new ReadMeasurementRequest();
	}
	signal.Release();
}
```

Pętla czasu wywołuje `RequestMeasurement` dopiero po poprzednim cyklu. Żądanie nastawy lub OFF ma wyższy priorytet. Publikacja próbki używa monotonicznego `TimeProvider.GetTimestamp()`.

- [ ] **Step 4: Dodaj test wartości pomiarowych przy OFF**

Test ma potwierdzić, że 0 V i 0 A odebrane tuż przed zakończeniem ON pozostają pomiarem wyjścia i nigdy nie zmieniają pól nastaw ani ostatnich wysłanych wartości.

- [ ] **Step 5: Uruchom wszystkie testy Core**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter "FullyQualifiedName~Protocol|FullyQualifiedName~Device|FullyQualifiedName~Sessions"`

Expected: PASS bez testów zależnych od rzeczywistego czasu ściennego.

- [ ] **Step 6: Commit**

```powershell
git add src/Ka3005P.Core/Sessions src/Ka3005P.Core/Measurements/MeasurementSample.cs tests/Ka3005P.Tests/Sessions tests/Ka3005P.Tests/Fakes/ManualTimeProvider.cs
git commit -m "feat: add non-overlapping measurement polling"
```

### Task 5: Obliczenia i koordynacja Dual Korad

**Files:**
- Create: `src/Ka3005P.Core/Dual/DualMode.cs`
- Create: `src/Ka3005P.Core/Dual/DualPhysicalSetpoints.cs`
- Create: `src/Ka3005P.Core/Dual/DualMeasurement.cs`
- Create: `src/Ka3005P.Core/Dual/DualOperationResult.cs`
- Create: `src/Ka3005P.Core/Dual/DualSetpointCalculator.cs`
- Create: `src/Ka3005P.Core/Dual/DualPowerSupplyController.cs`
- Create: `tests/Ka3005P.Tests/Fakes/FakePowerSupplySession.cs`
- Test: `tests/Ka3005P.Tests/Dual/DualSetpointCalculatorTests.cs`
- Test: `tests/Ka3005P.Tests/Dual/DualPowerSupplyControllerTests.cs`

**Interfaces:**
- Consumes: dwie instancje `IPowerSupplySession`.
- Produces: `Calculate(DualMode, VoltageSetpoint, CurrentSetpoint)`, `RequestVoltage`, `RequestCurrent`, `SetOutputAsync`, `DualSnapshotChanged`.

- [ ] **Step 1: Napisz testy podziału jednostek całkowitych**

```csharp
[Theory]
[InlineData(0,0,0)]
[InlineData(1,1,0)]
[InlineData(2,1,1)]
[InlineData(2501,1250,1251)]
[InlineData(6200,3100,3100)]
public void Series_SplitsVoltageWithoutLosingHundredth(int total,int first,int second)
{
	DualPhysicalSetpoints result=DualSetpointCalculator.Calculate(
		DualMode.Series,
		VoltageSetpoint.FromHundredths(total),
		CurrentSetpoint.FromThousandths(1000));

	Assert.Equal(first,result.FirstVoltage.Hundredths);
	Assert.Equal(second,result.SecondVoltage.Hundredths);
	Assert.Equal(total,result.FirstVoltage.Hundredths+result.SecondVoltage.Hundredths);
}
```

Dodaj analogiczne testy prądu równoległego 0-10200, jednakowych wartości symetrycznych, granic każdego trybu i odrzucenia wartości spoza zakresu trybu.

- [ ] **Step 2: Uruchom test kalkulatora i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~DualSetpointCalculatorTests`

Expected: FAIL, brak typów Dual.

- [ ] **Step 3: Zaimplementuj kalkulator i agregację pomiarów**

```csharp
private static (int First,int Second) Split(int total)
{
	int first=total/2;
	return (first,total-first);
}
```

Szeregowy pomiar zwraca `U1+U2` i `Max(I1,I2)`. Równoległy zwraca `Max(U1,U2)` i `I1+I2`. Symetryczny zachowuje osobne gałęzie i znaki wymagane przez eksport.

- [ ] **Step 4: Napisz test równoległości i częściowego błędu**

```csharp
[Fact]
public async Task SetOutputAsync_RunsBothSessionsAndReportsPartialFailure()
{
	FakePowerSupplySession first=new();
	FakePowerSupplySession second=new(){OutputFailure=new IOException("COM8")};
	DualPowerSupplyController controller=new(first,second);

	DualOperationResult result=await controller.SetOutputAsync(true,CancellationToken.None);

	Assert.True(first.OutputRequested);
	Assert.True(second.OutputRequested);
	Assert.True(result.IsPartialFailure);
	Assert.False(result.IsSuccess);
}
```

Dodaj test, że blokada pierwszej sesji nie powstrzymuje rozpoczęcia operacji drugiej oraz że nieudane ON wywołuje próbę OFF na obu sesjach.

- [ ] **Step 5: Zaimplementuj kontroler**

Użyj `Task.WhenAll` dla dwóch niezależnych sesji, ale przechwyć wynik każdej osobno. `RequestVoltage` i `RequestCurrent` tylko wyliczają wartości i publikują je do obu ograniczonych kolejek.

- [ ] **Step 6: Uruchom testy Dual i pełny Core**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter "FullyQualifiedName~Dual|FullyQualifiedName~Sessions"`

Expected: PASS, w tym częściowy błąd bez wspólnego sukcesu.

- [ ] **Step 7: Commit**

```powershell
git add src/Ka3005P.Core/Dual tests/Ka3005P.Tests/Dual
git commit -m "feat: add dual supply modes and coordination"
```

### Task 6: Rejestracja pomiarów, CSV, ustawienia i dzierżawa portów

**Files:**
- Create: `src/Ka3005P.Core/Measurements/MeasurementRecorder.cs`
- Create: `src/Ka3005P.Core/Measurements/CsvMeasurementWriter.cs`
- Create: `src/Ka3005P.Core/Measurements/ResistanceFormatter.cs`
- Create: `src/Ka3005P.Core/Measurements/RecordingState.cs`
- Create: `src/Ka3005P.Core/Configuration/AppSettings.cs`
- Create: `src/Ka3005P.Core/Configuration/JsonSettingsStore.cs`
- Create: `src/Ka3005P.Core/Configuration/PortLeaseRegistry.cs`
- Create: `src/Ka3005P.Core/Diagnostics/IAppLog.cs`
- Create: `src/Ka3005P.Core/Diagnostics/FileAppLog.cs`
- Test: `tests/Ka3005P.Tests/Measurements/CsvMeasurementWriterTests.cs`
- Test: `tests/Ka3005P.Tests/Measurements/MeasurementRecorderTests.cs`
- Test: `tests/Ka3005P.Tests/Configuration/JsonSettingsStoreTests.cs`
- Test: `tests/Ka3005P.Tests/Configuration/PortLeaseRegistryTests.cs`
- Test: `tests/Ka3005P.Tests/Measurements/ResistanceFormatterTests.cs`
- Test: `tests/Ka3005P.Tests/Diagnostics/FileAppLogTests.cs`

**Interfaces:**
- Consumes: `MeasurementSample` i `DualMeasurement`.
- Produces: `MeasurementRecorder.TryRecord(MeasurementSample)`, `CompleteAsync`, `RecordingFailed`; `ISettingsStore.LoadAsync/SaveAsync`; `PortLeaseRegistry.TryAcquire(string,out PortLease)`.

- [ ] **Step 1: Napisz test dokładnego formatu CSV**

```csharp
[Fact]
public async Task WriteSingleAsync_UsesSemicolonAndInvariantNumbers()
{
	CultureInfo original=CultureInfo.CurrentCulture;
	CultureInfo.CurrentCulture=new CultureInfo("pl-PL");
	try
	{
		StringWriter output=new();
		CsvMeasurementWriter writer=new(output);
		await writer.WriteHeaderAsync(MeasurementLayout.Single,MeasurementExportKind.Voltage,CancellationToken.None);
		await writer.WriteAsync(new MeasurementSample(TimeSpan.FromMilliseconds(150),1234,123),MeasurementExportKind.Voltage,CancellationToken.None);

		Assert.Equal("Time;Voltage;\n[s];[V];\n0.150;12.34;\n",output.ToString());
	}
	finally
	{
		CultureInfo.CurrentCulture=original;
	}
}
```

Dodaj testy układów szeregowego, równoległego i symetrycznego, pustej wartości pomiaru oraz znaków gałęzi symetrycznej.

- [ ] **Step 2: Napisz test przepełnienia bufora**

```csharp
[Fact]
public void TryRecord_WhenBufferIsFull_StopsRecorderAndReportsOneFailure()
{
	BlockingMeasurementSink sink=new();
	MeasurementRecorder recorder=new(sink,capacity:2);
	recorder.TryRecord(Sample(1));
	recorder.TryRecord(Sample(2));

	Assert.False(recorder.TryRecord(Sample(3)));
	Assert.Equal(RecordingState.Faulted,recorder.State);
	Assert.Single(recorder.Failures);
}
```

- [ ] **Step 3: Uruchom testy i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter "FullyQualifiedName~Measurements|FullyQualifiedName~Configuration"`

Expected: FAIL, brak rejestratora i konfiguracji.

- [ ] **Step 4: Zaimplementuj ograniczony kanał i zapis**

```csharp
channel=Channel.CreateBounded<MeasurementSample>(new BoundedChannelOptions(capacity)
{
	SingleReader=true,
	SingleWriter=false,
	FullMode=BoundedChannelFullMode.Wait
});

public bool TryRecord(MeasurementSample sample)
{
	if(State != RecordingState.Running || !channel.Writer.TryWrite(sample))
	{
		FailOnce("Bufor zapisu pomiarów jest pełny.");
		return false;
	}
	return true;
}
```

Zapisujący działa w jednym zadaniu. `CompleteAsync` kończy kanał, opróżnia go i zamyka plik. Wyjątek dysku kończy rejestrację i publikuje pojedynczy trwały błąd.

- [ ] **Step 5: Zaimplementuj ustawienia i rejestr portów**

`JsonSettingsStore` zapisuje do pliku tymczasowego w tym samym katalogu, opróżnia strumień, a następnie zastępuje właściwy plik. Uszkodzony JSON zwraca ustawienia domyślne i kopiuje wadliwy plik do nazwy z końcówką `.invalid`.

```csharp
public bool TryAcquire(string portName,[NotNullWhen(true)] out PortLease? lease)
{
	string key=portName.Trim().ToUpperInvariant();
	lock(gate)
	{
		if(!leasedPorts.Add(key))
		{
			lease=null;
			return false;
		}
		lease=new PortLease(key,Release);
		return true;
	}
}
```

- [ ] **Step 6: Dodaj rezystancję i dziennik błędów**

`ResistanceFormatter.TryFormat(int voltageHundredths,int currentThousandths,out string value)` zwraca `false` przy zerowym prądzie lub braku pomiaru. Testy obejmują mikroohmy, miliohmy, ohmy i dzielenie przez zero. `FileAppLog` zapisuje czas, poziom, nazwę operacji, port i wyjątek w lokalnym katalogu aplikacji. Test powtarzanego błędu pomiaru potwierdza wiele wpisów w dzienniku, ale tylko jedną zmianę stanu błędu publikowaną do UI.

- [ ] **Step 7: Uruchom testy i build**

Run: `dotnet test Ka3005P.sln && dotnet build Ka3005P.sln -c Release`

Expected: PASS, zero ostrzeżeń.

- [ ] **Step 8: Commit**

```powershell
git add src/Ka3005P.Core/Measurements src/Ka3005P.Core/Configuration src/Ka3005P.Core/Diagnostics tests/Ka3005P.Tests/Measurements tests/Ka3005P.Tests/Configuration tests/Ka3005P.Tests/Diagnostics
git commit -m "feat: add measurement recording and settings"
```

### Task 7: Infrastruktura WPF, motyw i menedżer okien

**Files:**
- Modify: `src/Ka3005P.App/App.xaml`
- Modify: `src/Ka3005P.App/App.xaml.cs`
- Create: `src/Ka3005P.App/Themes/KoradTheme.xaml`
- Create: `src/Ka3005P.App/Infrastructure/ObservableObject.cs`
- Create: `src/Ka3005P.App/Infrastructure/RelayCommand.cs`
- Create: `src/Ka3005P.App/Infrastructure/AsyncRelayCommand.cs`
- Create: `src/Ka3005P.App/Services/IWindowService.cs`
- Create: `src/Ka3005P.App/Services/WindowService.cs`
- Create: `src/Ka3005P.App/ViewModels/ManagerViewModel.cs`
- Create: `src/Ka3005P.App/Views/ManagerWindow.xaml`
- Create: `src/Ka3005P.App/Views/ManagerWindow.xaml.cs`
- Test: `tests/Ka3005P.Tests/ViewModels/ManagerViewModelTests.cs`

**Interfaces:**
- Consumes: `ISettingsStore`, `PortLeaseRegistry` i fabryki sesji.
- Produces: komendy `OpenSingleCommand`, `OpenTwoSinglesCommand`, `OpenDualCommand`, `ChooseExportFolderCommand`.

- [ ] **Step 1: Napisz test menedżera bez tworzenia prawdziwych okien**

```csharp
[Fact]
public void OpenTwoSingles_CreatesTwoIndependentWindowRequests()
{
	FakeWindowService windows=new();
	ManagerViewModel viewModel=new(windows,new FakeSettingsStore());

	viewModel.OpenTwoSinglesCommand.Execute(null);

	Assert.Collection(
		windows.Requests,
		request=>Assert.Equal(WindowKind.Single,request.Kind),
		request=>Assert.Equal(WindowKind.Single,request.Kind));
}
```

Dodaj test zapisu folderu eksportu oraz test, że menedżer nie kończy istniejących sesji przez zamykanie nowo tworzonego okna.

- [ ] **Step 2: Uruchom test i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~ManagerViewModelTests`

Expected: FAIL, brak infrastruktury aplikacji.

- [ ] **Step 3: Zaimplementuj minimalne klasy MVVM**

```csharp
protected bool SetProperty<T>(ref T field,T value,[CallerMemberName] string? propertyName=null)
{
	if(EqualityComparer<T>.Default.Equals(field,value))
	{
		return false;
	}
	field=value;
	PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(propertyName));
	return true;
}
```

`AsyncRelayCommand` wyłącza ponowne wykonanie tylko dla tej samej operacji, przechwytuje wyjątek do jawnego handlera i zawsze przywraca stan w `finally`.

- [ ] **Step 4: Zbuduj motyw i okno menedżera**

`KoradTheme.xaml` definiuje zasoby: `KoradBackground=#686868`, `KoradInput=#8A8A8A`, `KoradText=#FFFFFF`, `KoradOffline=#808000`, `KoradOff=#800000`, `KoradOn=#008000`, `KoradOnActive=#00FF00`, font Consolas dla wartości i Segoe UI dla menu.

Menedżer używa ikon `KoradS`, `KoradD` i `KoradM` skopiowanych do zasobów projektu z zachowaniem manifestu pochodzenia.

- [ ] **Step 5: Uruchom testy i kompilację WPF**

Run: `dotnet test Ka3005P.sln && dotnet build src/Ka3005P.App/Ka3005P.App.csproj -c Release`

Expected: PASS, zasoby XAML kompilują się.

- [ ] **Step 6: Commit**

```powershell
git add src/Ka3005P.App tests/Ka3005P.Tests/ViewModels
git commit -m "feat: add WPF theme and session manager"
```

### Task 8: Pojedynczy zasilacz i nieblokujące nastawy

**Files:**
- Create: `src/Ka3005P.App/Controls/SetpointEditor.xaml`
- Create: `src/Ka3005P.App/Controls/SetpointEditor.xaml.cs`
- Create: `src/Ka3005P.App/Controls/StatusLamps.xaml`
- Create: `src/Ka3005P.App/Controls/StatusLamps.xaml.cs`
- Create: `src/Ka3005P.App/ViewModels/SingleSupplyViewModel.cs`
- Create: `src/Ka3005P.App/Views/SingleSupplyWindow.xaml`
- Create: `src/Ka3005P.App/Views/SingleSupplyWindow.xaml.cs`
- Test: `tests/Ka3005P.Tests/ViewModels/SingleSupplyViewModelTests.cs`

**Interfaces:**
- Consumes: `IPowerSupplySession`, `PortLeaseRegistry`, `MeasurementRecorder`.
- Produces: właściwości portu, nastaw, pomiarów, lampek, błędu i komendy Connect, Output, Increment, Decrement, Save.

- [ ] **Step 1: Napisz test, że zmiana nastawy nie oczekuje na COM**

```csharp
[Fact]
public void IncrementVoltage_UpdatesUiAndQueuesRequestSynchronously()
{
	FakePowerSupplySession session=new();
	SingleSupplyViewModel viewModel=new(session){VoltageText="12,00"};

	viewModel.IncrementVoltageCommand.Execute(null);

	Assert.Equal("12,01",viewModel.VoltageText);
	Assert.Equal(1201,session.RequestedVoltage?.Hundredths);
	Assert.False(viewModel.HasValidationError);
}
```

Dodaj testy separatora kropka/przecinek, Enter, limitów 31,00 V i 5,100 A, wartości ujemnej, pustego pola, konfliktu portu oraz aktualizacji pomiaru bez zmiany nastawy.
Dodaj test rezystancji: pomiar 12,00 V i 1,000 A pokazuje 12 ohm, a prąd 0,000 A ukrywa wartość.

- [ ] **Step 2: Uruchom test i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~SingleSupplyViewModelTests`

Expected: FAIL, brak modelu widoku.

- [ ] **Step 3: Zaimplementuj walidację przed publikacją**

```csharp
private void ApplyVoltage(int hundredths)
{
	VoltageSetpoint value=VoltageSetpoint.FromHundredths(hundredths);
	voltageHundredths=value.Hundredths;
	VoltageText=FormatVoltage(value);
	session.RequestVoltage(value);
}
```

Parser wejścia akceptuje przecinek i kropkę, ale nie wysyła niczego przed pełną walidacją. Format ekranu używa polskiego przecinka.
`ResistanceText` korzysta wyłącznie z `ResistanceFormatter` i jest niewidoczne, kiedy bieżący pomiar prądu jest zerowy albo niedostępny.

- [ ] **Step 4: Zbuduj okno zgodne z referencją**

Obszar klienta bazuje na proporcji 253 x 318, z obsługą DPI. Układ zawiera menu Wykres i Zapisz jako, wybór COM, Online/Offline, dwa edytory nastaw, trzy lampki, ON/OFF oraz duże pomiary. Wartości nastaw są oddzielone od pomiarów.

- [ ] **Step 5: Dodaj test zamykania**

Test modelu widoku potwierdza kolejność: żądanie OFF, zatrzymanie sesji, zwolnienie dzierżawy portu, zakończenie rejestratora. Błąd OFF nie pomija zwolnienia zasobów i zostaje pokazany.

- [ ] **Step 6: Uruchom testy i build**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~SingleSupplyViewModelTests && dotnet build src/Ka3005P.App/Ka3005P.App.csproj -c Release`

Expected: PASS, zero ostrzeżeń.

- [ ] **Step 7: Commit**

```powershell
git add src/Ka3005P.App/Controls src/Ka3005P.App/ViewModels/SingleSupplyViewModel.cs src/Ka3005P.App/Views/SingleSupplyWindow.xaml* tests/Ka3005P.Tests/ViewModels/SingleSupplyViewModelTests.cs
git commit -m "feat: add responsive single supply window"
```

### Task 9: Dual Korad w interfejsie WPF

**Files:**
- Create: `src/Ka3005P.App/ViewModels/DualSupplyViewModel.cs`
- Create: `src/Ka3005P.App/Views/DualSupplyWindow.xaml`
- Create: `src/Ka3005P.App/Views/DualSupplyWindow.xaml.cs`
- Test: `tests/Ka3005P.Tests/ViewModels/DualSupplyViewModelTests.cs`

**Interfaces:**
- Consumes: `DualPowerSupplyController`, dwie dzierżawy portów, rejestrator Dual.
- Produces: tryb, dwa porty, logiczne nastawy i pomiary, szczegóły urządzeń 1/2, wspólne komendy połączenia i ON/OFF.

- [ ] **Step 1: Napisz test zmiany trybu i zakresów**

```csharp
[Theory]
[InlineData(DualMode.Series,"62,00","5,100")]
[InlineData(DualMode.Parallel,"31,00","10,200")]
[InlineData(DualMode.Symmetrical,"31,00","5,100")]
public void ChangeMode_UpdatesLimitsAndRevalidatesSetpoints(
	DualMode mode,
	string maxVoltage,
	string maxCurrent)
{
	DualSupplyViewModel viewModel=CreateViewModel();
	viewModel.Mode=mode;

	Assert.Equal(maxVoltage,viewModel.MaximumVoltageText);
	Assert.Equal(maxCurrent,viewModel.MaximumCurrentText);
	Assert.True(viewModel.SetpointsAreValid);
}
```

Dodaj test tych samych numerów COM, częściowego błędu połączenia, wspólnego OFF, natychmiastowego przyjęcia szybkiej serii nastaw i osobnych wyników urządzeń.

- [ ] **Step 2: Uruchom test i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter FullyQualifiedName~DualSupplyViewModelTests`

Expected: FAIL, brak modelu widoku.

- [ ] **Step 3: Zaimplementuj model widoku**

```csharp
private void ApplyCurrent(int thousandths)
{
	CurrentSetpoint logical=CurrentSetpoint.FromThousandths(thousandths);
	DualPhysicalSetpoints physical=DualSetpointCalculator.Calculate(Mode,Voltage,logical);
	currentThousandths=logical.Thousandths;
	CurrentText=FormatCurrent(logical);
	controller.RequestCurrent(logical);
	FirstCurrentText=FormatCurrent(physical.FirstCurrent);
	SecondCurrentText=FormatCurrent(physical.SecondCurrent);
}
```

Widok nie wywołuje transportu. Stan częściowego błędu pokazuje, którego COM dotyczy problem, i nie ustawia wspólnej lampki Online/ON.

- [ ] **Step 4: Zbuduj okno Dual**

Okno zachowuje styl pojedynczego Korada, dodaje drugi COM, wybór trybu i niewielki panel odczytów urządzenia 1 oraz 2. Główne wartości pozostają wartościami logicznymi trybu. Zmiana trybu jest zablokowana przy ON, aby interpretacja pomiarów i pliku nie zmieniła się w środku sesji.

- [ ] **Step 5: Uruchom testy oraz build**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter "FullyQualifiedName~Dual|FullyQualifiedName~DualSupplyViewModel" && dotnet build src/Ka3005P.App/Ka3005P.App.csproj -c Release`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/Ka3005P.App/ViewModels/DualSupplyViewModel.cs src/Ka3005P.App/Views/DualSupplyWindow.xaml* tests/Ka3005P.Tests/ViewModels/DualSupplyViewModelTests.cs
git commit -m "feat: add responsive dual supply window"
```

### Task 10: Wykres, wspólny ON/OFF i Zapisz jako

**Files:**
- Create: `src/Ka3005P.App/ViewModels/ChartViewModel.cs`
- Create: `src/Ka3005P.App/Controls/CurrentChart.cs`
- Create: `src/Ka3005P.App/Views/ChartWindow.xaml`
- Create: `src/Ka3005P.App/Views/ChartWindow.xaml.cs`
- Create: `src/Ka3005P.App/Services/IFileDialogService.cs`
- Create: `src/Ka3005P.App/Services/FileDialogService.cs`
- Modify: `src/Ka3005P.App/ViewModels/SingleSupplyViewModel.cs`
- Modify: `src/Ka3005P.App/ViewModels/DualSupplyViewModel.cs`
- Test: `tests/Ka3005P.Tests/ViewModels/ChartViewModelTests.cs`
- Test: `tests/Ka3005P.Tests/ViewModels/ExportCommandTests.cs`

**Interfaces:**
- Consumes: ten sam strumień `MeasurementReceived` i tę samą komendę wyjścia co okno główne.
- Produces: `IReadOnlyList<ChartPoint> Points`, `MinimumY`, `MaximumY`, `ToggleOutputCommand`, `SaveVoltageCommand`, `SaveCurrentCommand`.

- [ ] **Step 1: Napisz test okna 50 próbek**

```csharp
[Fact]
public void AddSample_KeepsLatestFiftyAndScalesAxis()
{
	ChartViewModel viewModel=new(new FakeOutputController());
	for(int i=0;i<60;i++)
	{
		viewModel.AddSample(TimeSpan.FromMilliseconds(i*100),i);
	}

	Assert.Equal(50,viewModel.Points.Count);
	Assert.Equal(10,viewModel.Points[0].Value);
	Assert.Equal(59,viewModel.Points[^1].Value);
	Assert.Equal(9.5,viewModel.MinimumY);
	Assert.Equal(59.5,viewModel.MaximumY);
}
```

Dodaj test wartości stałej, wartości bliskich zera, zamknięcia i ponownego otwarcia okna oraz braku utworzenia dodatkowej pętli pomiarowej.

- [ ] **Step 2: Napisz test wspólnego ON/OFF**

Test tworzy główny i wykresowy model widoku nad tym samym kontrolerem, wykonuje ON z wykresu i potwierdza, że oba modele pokazują ten sam stan bez drugiego polecenia synchronizującego.

- [ ] **Step 3: Uruchom testy i potwierdź RED**

Run: `dotnet test tests/Ka3005P.Tests/Ka3005P.Tests.csproj --filter "FullyQualifiedName~ChartViewModel|FullyQualifiedName~ExportCommand"`

Expected: FAIL, brak wykresu i usług dialogów.

- [ ] **Step 4: Zaimplementuj wykres bez biblioteki zewnętrznej**

`CurrentChart.OnRender` rysuje tło, poziome linie siatki, etykiety osi i zieloną `StreamGeometry`. Dla pustej serii pokazuje samą siatkę. Dla stałej wartości zapewnia zakres co najmniej 1,0 A, a dla pozostałych wartości margines 0,5 A zgodny z referencją.

- [ ] **Step 5: Zaimplementuj Zapisz jako**

`FileDialogService` zwraca wybraną ścieżkę albo `null`. Model widoku kopiuje zakończony plik sesji do wybranej ścieżki asynchronicznie. Menu udostępnia osobny eksport napięcia i prądu, zachowując układ kolumn właściwy dla trybu.

- [ ] **Step 6: Uruchom testy i build**

Run: `dotnet test Ka3005P.sln && dotnet build src/Ka3005P.App/Ka3005P.App.csproj -c Release`

Expected: PASS, zero ostrzeżeń.

- [ ] **Step 7: Commit**

```powershell
git add src/Ka3005P.App/Controls/CurrentChart.cs src/Ka3005P.App/ViewModels src/Ka3005P.App/Views/ChartWindow.xaml* src/Ka3005P.App/Services tests/Ka3005P.Tests/ViewModels
git commit -m "feat: add live chart and measurement export"
```

### Task 11: Test responsywności, pakiet Windows i dokumentacja

**Files:**
- Create: `tests/Ka3005P.Tests/Integration/ResponsiveDualOperationTests.cs`
- Create: `tests/Ka3005P.Tests/Integration/CommandTrafficTests.cs`
- Create: `tests/Ka3005P.Tests/Integration/TestApplication.cs`
- Create: `src/Ka3005P.App/Demo/DemoPowerSupplyDevice.cs`
- Create: `docs/testing-hardware.md`
- Create: `THIRD-PARTY-NOTICES.md`
- Modify: `README.md`
- Modify: `src/Ka3005P.App/App.xaml.cs`
- Modify: `src/Ka3005P.App/Ka3005P.App.csproj`

**Interfaces:**
- Consumes: cały przepływ ViewModel -> Dual controller -> dwie sesje -> dwa fake urządzenia.
- Produces: opublikowany pakiet `artifacts/publish/win-x64` i udokumentowaną procedurę testu fizycznego.

- [ ] **Step 1: Napisz test regresji lagów Dual Korad**

```csharp
[Fact]
public async Task ActiveDualPolling_RapidSetpointChangesRemainNonBlocking()
{
	FakePowerSupplyDevice first=new(){OperationDelay=TimeSpan.FromMilliseconds(500)};
	FakePowerSupplyDevice second=new(){OperationDelay=TimeSpan.FromMilliseconds(500)};
	await using TestApplication app=await TestApplication.StartDualAsync(first,second);
	await app.ViewModel.SetOutputAsync(true);
	await first.WaitUntilMeasurementStartsAsync();

	for(int value=1200;value<=1250;value++)
	{
		app.ViewModel.SetVoltageFromHundredths(value);
	}

	Assert.Equal(1250,app.ViewModel.VoltageHundredths);
	Assert.True(app.FirstSession.PendingSetpointCount<=2);
	Assert.True(app.SecondSession.PendingSetpointCount<=2);
	first.ReleaseOperation();
	second.ReleaseOperation();
	await app.WaitForBothVoltageWritesAsync(1250);
}
```

Test nie używa rzeczywistego portu ani `Thread.Sleep`. Kończy się przez jawne sygnały fake i ma ogólny timeout testu chroniący przed zakleszczeniem.

- [ ] **Step 2: Napisz test budżetu transmisji**

```csharp
[Fact]
public async Task RuntimeTraffic_NeverQueriesSetpointsOrStatus()
{
	await using TestApplication app=await TestApplication.RunTypicalDualSessionAsync();

	string[] forbidden={"VSET1?","ISET1?","STATUS?"};
	Assert.DoesNotContain(app.AllWritesAsAscii,write=>forbidden.Contains(write));
	Assert.All(
		app.AllQueriesAsAscii,
		query=>Assert.Contains(query,new[]{"VOUT1?","IOUT1?"}));
}
```

- [ ] **Step 3: Uruchom pełny zestaw testów wielokrotnie**

Run:

```powershell
dotnet test Ka3005P.sln -c Release
dotnet test Ka3005P.sln -c Release
dotnet test Ka3005P.sln -c Release
```

Expected: trzy przejścia bez flakiness, zakleszczeń i ostrzeżeń.

- [ ] **Step 4: Dodaj tryb demonstracyjny i wykonaj kontrolę wizualną**

`App.xaml.cs` rozpoznaje argument `--demo` i buduje oba okna z `DemoPowerSupplyDevice`, bez otwierania portów COM. `DemoPowerSupplyDevice` implementuje ten sam interfejs co urządzenie szeregowe, symuluje opóźnienia i wartości `VOUT1?`/`IOUT1?`, ale zachowuje rzeczywisty przepływ kolejek i kontrolerów. `TestApplication` udostępnia `StartDualAsync`, `RunTypicalDualSessionAsync`, obie sesje, zapisane ramki i jawne sygnały oczekiwania użyte w testach integracyjnych.

Uruchom pojedyncze, Dual i wykres poleceniem `dotnet run --project src/Ka3005P.App -- --demo`. Sprawdź 100%, 125%, 150% i 200% DPI, polskie teksty, nawigację klawiaturą, widoczność fokusu, kontrast lampek oraz zgodność proporcji ze zrzutami referencyjnymi. Zapisz wyniki i zauważone odstępstwa w `docs/testing-hardware.md`.

- [ ] **Step 5: Uzupełnij dokumentację i licencje zależności**

`README.md` ma zawierać wymagania, build, test, uruchomienie, konfigurację COM, opis trybów i jasne ostrzeżenie, że wybór trybu nie przełącza fizycznych przewodów. `THIRD-PARTY-NOTICES.md` wymienia System.IO.Ports, xUnit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio i coverlet.collector wraz z ich licencjami oraz linkami.

`docs/testing-hardware.md` oddziela testy wykonane na fake od testów fizycznych i zawiera sekwencję: jedno urządzenie OFF, jedno urządzenie ON z małym limitem, zmiana nastawy podczas ON, następnie dwa urządzenia w każdym ręcznie przygotowanym trybie.

- [ ] **Step 6: Opublikuj pakiet framework-dependent**

Run:

```powershell
dotnet publish src/Ka3005P.App/Ka3005P.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o artifacts/publish/win-x64
```

Sprawdź, że pakiet zawiera aplikację, plik `LICENSE`, `THIRD-PARTY-NOTICES.md` i ikony. Uruchom aplikację z opublikowanego katalogu w trybie demonstracyjnym bez portów COM.

- [ ] **Step 7: Końcowa weryfikacja**

Run:

```powershell
dotnet test Ka3005P.sln -c Release
dotnet build Ka3005P.sln -c Release
git diff --check
git status --short
```

Expected: testy i build PASS, `git diff --check` bez wyjścia, status zawiera wyłącznie planowane zmiany dokumentacji i pakietu, bez plików tymczasowych.

- [ ] **Step 8: Commit**

```powershell
git add README.md THIRD-PARTY-NOTICES.md docs/testing-hardware.md src/Ka3005P.App tests/Ka3005P.Tests/Integration
git commit -m "test: verify responsive dual operation"
```
