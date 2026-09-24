using System.Globalization;
using Ka3005P.App.Infrastructure;
using Ka3005P.App.Services;
using Ka3005P.Core.Configuration;
using Ka3005P.Core.Dual;
using Ka3005P.Core.Protocol;
using Ka3005P.Core.Sessions;

namespace Ka3005P.App.ViewModels;

public sealed class DualSupplyViewModel : ObservableObject
{
	private static readonly CultureInfo PolishCulture=
		CultureInfo.GetCultureInfo("pl-PL");

	private readonly PortLeaseRegistry? leases;
	private readonly ISingleSessionFactory? sessionFactory;
	private readonly SynchronizationContext? uiContext=SynchronizationContext.Current;
	private DualPowerSupplyController? controller;
	private PortLease? firstLease;
	private PortLease? secondLease;
	private DualMode mode=DualMode.Series;
	private string firstPortName="COM5";
	private string secondPortName="COM6";
	private string voltageText="12,00";
	private string currentText="1,000";
	private string measuredVoltageText="0,00 V";
	private string measuredCurrentText="0,000 A";
	private string firstMeasurementText="0,00 V / 0,000 A";
	private string secondMeasurementText="0,00 V / 0,000 A";
	private string? errorMessage;
	private int voltageHundredths=1200;
	private int currentThousandths=1000;
	private bool isConnected;
	private bool isOutputOn;
	private bool setpointsAreValid=true;
	private bool closing;

	public DualSupplyViewModel(DualPowerSupplyController controller)
	{
		ArgumentNullException.ThrowIfNull(controller);
		this.controller=controller;
		mode=controller.Mode;
		isConnected=true;
		AttachController(controller);
		ConnectCommand=new AsyncRelayCommand(_=>Task.CompletedTask,_=>false);
		InitializeCommands();
		UpdatePhysicalSetpointTexts();
	}

	public DualSupplyViewModel(
		PortLeaseRegistry leases,
		ISingleSessionFactory sessionFactory)
	{
		ArgumentNullException.ThrowIfNull(leases);
		ArgumentNullException.ThrowIfNull(sessionFactory);
		this.leases=leases;
		this.sessionFactory=sessionFactory;
		ConnectCommand=new AsyncRelayCommand(
			ConnectAsync,
			_=>!IsConnected,
			exception=>ErrorMessage=exception.Message);
		InitializeCommands();
		UpdatePhysicalSetpointTexts();
	}

	public AsyncRelayCommand ConnectCommand { get; private set; }=null!;
	public RelayCommand IncrementVoltageCommand { get; private set; }=null!;
	public RelayCommand DecrementVoltageCommand { get; private set; }=null!;
	public RelayCommand IncrementCurrentCommand { get; private set; }=null!;
	public RelayCommand DecrementCurrentCommand { get; private set; }=null!;
	public RelayCommand CommitVoltageCommand { get; private set; }=null!;
	public RelayCommand CommitCurrentCommand { get; private set; }=null!;
	public AsyncRelayCommand ToggleOutputCommand { get; private set; }=null!;

	public DualMode Mode
	{
		get => mode;
		set
		{
			if(mode == value)
			{
				return;
			}
			if(IsOutputOn)
			{
				ErrorMessage="Tryb można zmienić dopiero po wyłączeniu wyjścia.";
				OnPropertyChanged();
				return;
			}

			mode=value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(MaximumVoltageText));
			OnPropertyChanged(nameof(MaximumCurrentText));
			ValidateSetpoints();
			if(SetpointsAreValid)
			{
				controller?.SetMode(value);
				UpdatePhysicalSetpointTexts();
			}
		}
	}

	public string FirstPortName
	{
		get => firstPortName;
		set => SetProperty(ref firstPortName,value);
	}

	public string SecondPortName
	{
		get => secondPortName;
		set => SetProperty(ref secondPortName,value);
	}

	public string VoltageText
	{
		get => voltageText;
		set => SetProperty(ref voltageText,value);
	}

	public string CurrentText
	{
		get => currentText;
		set => SetProperty(ref currentText,value);
	}

	public string MaximumVoltageText =>
		Mode == DualMode.Series ? "62,00" : "31,00";
	public string MaximumCurrentText =>
		Mode == DualMode.Parallel ? "10,200" : "5,100";

	public bool SetpointsAreValid
	{
		get => setpointsAreValid;
		private set => SetProperty(ref setpointsAreValid,value);
	}

	public bool IsConnected
	{
		get => isConnected;
		private set
		{
			if(SetProperty(ref isConnected,value))
			{
				OnPropertyChanged(nameof(ConnectionStatus));
				OnPropertyChanged(nameof(IsOffline));
			}
		}
	}

	public bool IsOutputOn
	{
		get => isOutputOn;
		private set
		{
			if(SetProperty(ref isOutputOn,value))
			{
				OnPropertyChanged(nameof(OutputButtonText));
				OnPropertyChanged(nameof(IsOff));
				OnPropertyChanged(nameof(IsOn));
			}
		}
	}

	public string ConnectionStatus => IsConnected ? "Online" : "Offline";
	public string OutputButtonText => IsOutputOn ? "ON" : "OFF";
	public bool IsOffline => !IsConnected;
	public bool IsOff => IsConnected && !IsOutputOn;
	public bool IsOn => IsConnected && IsOutputOn;

	public string MeasuredVoltageText
	{
		get => measuredVoltageText;
		private set => SetProperty(ref measuredVoltageText,value);
	}

	public string MeasuredCurrentText
	{
		get => measuredCurrentText;
		private set => SetProperty(ref measuredCurrentText,value);
	}

	public string FirstMeasurementText
	{
		get => firstMeasurementText;
		private set => SetProperty(ref firstMeasurementText,value);
	}

	public string SecondMeasurementText
	{
		get => secondMeasurementText;
		private set => SetProperty(ref secondMeasurementText,value);
	}

	public string FirstPhysicalSetpointText { get; private set; }=string.Empty;
	public string SecondPhysicalSetpointText { get; private set; }=string.Empty;

	public string? ErrorMessage
	{
		get => errorMessage;
		private set => SetProperty(ref errorMessage,value);
	}

	public void SetVoltageFromHundredths(int value)
	{
		ApplyVoltage(value);
	}

	public async ValueTask CloseAsync(CancellationToken cancellationToken)
	{
		if(closing)
		{
			return;
		}
		closing=true;
		try
		{
			if(controller is not null)
			{
				try
				{
					await controller.SetOutputAsync(false,cancellationToken);
				}
				catch(Exception exception)
				{
					ErrorMessage=exception.Message;
				}
				finally
				{
					DetachController(controller);
					await controller.DisposeAsync();
				}
			}
		}
		finally
		{
			controller=null;
			firstLease?.Dispose();
			secondLease?.Dispose();
			firstLease=null;
			secondLease=null;
			IsConnected=false;
			IsOutputOn=false;
		}
	}

	private void InitializeCommands()
	{
		IncrementVoltageCommand=new RelayCommand(_=>ChangeVoltage(1));
		DecrementVoltageCommand=new RelayCommand(_=>ChangeVoltage(-1));
		IncrementCurrentCommand=new RelayCommand(_=>ChangeCurrent(1));
		DecrementCurrentCommand=new RelayCommand(_=>ChangeCurrent(-1));
		CommitVoltageCommand=new RelayCommand(_=>CommitVoltage());
		CommitCurrentCommand=new RelayCommand(_=>CommitCurrent());
		ToggleOutputCommand=new AsyncRelayCommand(
			ToggleOutputAsync,
			_=>IsConnected,
			exception=>ErrorMessage=exception.Message);
	}

	private async Task ConnectAsync(object? parameter)
	{
		if(leases is null || sessionFactory is null)
		{
			return;
		}
		string firstPort=FirstPortName.Trim().ToUpperInvariant();
		string secondPort=SecondPortName.Trim().ToUpperInvariant();
		if(string.Equals(firstPort,secondPort,StringComparison.OrdinalIgnoreCase))
		{
			ErrorMessage="Dla trybu Dual wybierz dwa różne porty COM.";
			return;
		}
		if(!leases.TryAcquire(firstPort,out PortLease? acquiredFirst))
		{
			ErrorMessage=$"Port {firstPort} jest już używany.";
			return;
		}
		if(!leases.TryAcquire(secondPort,out PortLease? acquiredSecond))
		{
			acquiredFirst.Dispose();
			ErrorMessage=$"Port {secondPort} jest już używany.";
			return;
		}

		SessionCreationResult firstResult;
		SessionCreationResult secondResult;
		try
		{
			Task<SessionCreationResult> firstTask=
				CreateSessionAsync(firstPort,sessionFactory);
			Task<SessionCreationResult> secondTask=
				CreateSessionAsync(secondPort,sessionFactory);
			await Task.WhenAll(firstTask,secondTask);
			firstResult=await firstTask;
			secondResult=await secondTask;
		}
		catch
		{
			acquiredFirst.Dispose();
			acquiredSecond.Dispose();
			throw;
		}

		if(firstResult.Session is null || secondResult.Session is null)
		{
			if(firstResult.Session is not null)
			{
				await firstResult.Session.DisposeAsync();
			}
			if(secondResult.Session is not null)
			{
				await secondResult.Session.DisposeAsync();
			}
			acquiredFirst.Dispose();
			acquiredSecond.Dispose();
			string failedPort=firstResult.Error is not null ? firstPort : secondPort;
			Exception error=firstResult.Error ?? secondResult.Error!;
			ErrorMessage=$"Nie można połączyć portu {failedPort}: {error.Message}";
			return;
		}

		firstLease=acquiredFirst;
		secondLease=acquiredSecond;
		controller=new DualPowerSupplyController(
			firstResult.Session,
			secondResult.Session,
			Mode);
		AttachController(controller);
		IsConnected=true;
		ErrorMessage=null;
		controller.RequestVoltage(VoltageSetpoint.FromHundredths(voltageHundredths));
		controller.RequestCurrent(CurrentSetpoint.FromThousandths(currentThousandths));
		UpdatePhysicalSetpointTexts();
	}

	private static async Task<SessionCreationResult> CreateSessionAsync(
		string port,
		ISingleSessionFactory factory)
	{
		try
		{
			IPowerSupplySession created=await factory.CreateAsync(
				port,
				CancellationToken.None);
			return new SessionCreationResult(created,null);
		}
		catch(Exception exception)
		{
			return new SessionCreationResult(null,exception);
		}
	}

	private async Task ToggleOutputAsync(object? parameter)
	{
		DualPowerSupplyController current=controller ??
			throw new InvalidOperationException("Brak połączenia Dual.");
		DualOperationResult result=await current.SetOutputAsync(
			!IsOutputOn,
			CancellationToken.None);
		if(result.IsSuccess)
		{
			IsOutputOn=result.RequestedEnabled;
			ErrorMessage=null;
		}
		else
		{
			IsOutputOn=false;
			ErrorMessage=BuildOperationError(result);
		}
	}

	private static string BuildOperationError(DualOperationResult result)
	{
		List<string> parts=[];
		if(result.FirstError is not null)
		{
			parts.Add("zasilacz 1: "+result.FirstError.Message);
		}
		if(result.SecondError is not null)
		{
			parts.Add("zasilacz 2: "+result.SecondError.Message);
		}
		return string.Join("; ",parts);
	}

	private void ChangeVoltage(int delta)
	{
		if(!TryParseScaled(VoltageText,100,GetMaximumVoltage(),out int value))
		{
			Invalidate("Nieprawidłowa nastawa napięcia.");
			return;
		}
		ApplyVoltage(Math.Clamp(value+delta,0,GetMaximumVoltage()));
	}

	private void ChangeCurrent(int delta)
	{
		if(!TryParseScaled(CurrentText,1000,GetMaximumCurrent(),out int value))
		{
			Invalidate("Nieprawidłowe ograniczenie prądu.");
			return;
		}
		ApplyCurrent(Math.Clamp(value+delta,0,GetMaximumCurrent()));
	}

	private void CommitVoltage()
	{
		if(!TryParseScaled(VoltageText,100,GetMaximumVoltage(),out int value))
		{
			Invalidate("Nieprawidłowa nastawa napięcia.");
			return;
		}
		ApplyVoltage(value);
	}

	private void CommitCurrent()
	{
		if(!TryParseScaled(CurrentText,1000,GetMaximumCurrent(),out int value))
		{
			Invalidate("Nieprawidłowe ograniczenie prądu.");
			return;
		}
		ApplyCurrent(value);
	}

	private void ApplyVoltage(int value)
	{
		if(value < 0 || value > GetMaximumVoltage())
		{
			Invalidate("Napięcie jest poza zakresem bieżącego trybu.");
			return;
		}
		VoltageSetpoint logical=VoltageSetpoint.FromHundredths(value);
		DualSetpointCalculator.Calculate(
			Mode,
			logical,
			CurrentSetpoint.FromThousandths(currentThousandths));
		voltageHundredths=value;
		VoltageText=(value/100m).ToString("0.00",PolishCulture);
		SetpointsAreValid=true;
		ErrorMessage=null;
		controller?.RequestVoltage(logical);
		UpdatePhysicalSetpointTexts();
	}

	private void ApplyCurrent(int value)
	{
		if(value < 0 || value > GetMaximumCurrent())
		{
			Invalidate("Prąd jest poza zakresem bieżącego trybu.");
			return;
		}
		CurrentSetpoint logical=CurrentSetpoint.FromThousandths(value);
		DualSetpointCalculator.Calculate(
			Mode,
			VoltageSetpoint.FromHundredths(voltageHundredths),
			logical);
		currentThousandths=value;
		CurrentText=(value/1000m).ToString("0.000",PolishCulture);
		SetpointsAreValid=true;
		ErrorMessage=null;
		controller?.RequestCurrent(logical);
		UpdatePhysicalSetpointTexts();
	}

	private void ValidateSetpoints()
	{
		SetpointsAreValid=
			voltageHundredths<=GetMaximumVoltage() &&
			currentThousandths<=GetMaximumCurrent();
		if(!SetpointsAreValid)
		{
			ErrorMessage="Nastawa przekracza zakres wybranego trybu.";
		}
	}

	private void UpdatePhysicalSetpointTexts()
	{
		if(!SetpointsAreValid)
		{
			return;
		}
		DualPhysicalSetpoints physical=DualSetpointCalculator.Calculate(
			Mode,
			VoltageSetpoint.FromHundredths(voltageHundredths),
			CurrentSetpoint.FromThousandths(currentThousandths));
		FirstPhysicalSetpointText=
			FormatVoltage(physical.FirstVoltage.Hundredths)+" / "+
			FormatCurrent(physical.FirstCurrent.Thousandths);
		SecondPhysicalSetpointText=
			FormatVoltage(physical.SecondVoltage.Hundredths)+" / "+
			FormatCurrent(physical.SecondCurrent.Thousandths);
		OnPropertyChanged(nameof(FirstPhysicalSetpointText));
		OnPropertyChanged(nameof(SecondPhysicalSetpointText));
	}

	private void AttachController(DualPowerSupplyController value)
	{
		value.DualSnapshotChanged+=OnDualSnapshotChanged;
		value.MeasurementReceived+=OnMeasurementReceived;
	}

	private void DetachController(DualPowerSupplyController value)
	{
		value.DualSnapshotChanged-=OnDualSnapshotChanged;
		value.MeasurementReceived-=OnMeasurementReceived;
	}

	private void OnDualSnapshotChanged(object? sender,DualControllerSnapshot snapshot)
	{
		Dispatch(()=>
		{
			if(snapshot.LastOutputOperation is { IsSuccess: false } operation)
			{
				ErrorMessage=BuildOperationError(operation);
			}
		});
	}

	private void OnMeasurementReceived(object? sender,DualMeasurement value)
	{
		Dispatch(()=>
		{
			MeasuredVoltageText=FormatVoltage(value.VoltageHundredths);
			MeasuredCurrentText=FormatCurrent(value.CurrentThousandths);
			FirstMeasurementText=
				FormatVoltage(value.First.VoltageHundredths)+" / "+
				FormatCurrent(value.First.CurrentThousandths);
			SecondMeasurementText=
				FormatVoltage(value.Second.VoltageHundredths)+" / "+
				FormatCurrent(value.Second.CurrentThousandths);
		});
	}

	private void Dispatch(Action action)
	{
		if(uiContext is null || SynchronizationContext.Current == uiContext)
		{
			action();
		}
		else
		{
			uiContext.Post(_=>action(),null);
		}
	}

	private void Invalidate(string message)
	{
		SetpointsAreValid=false;
		ErrorMessage=message;
	}

	private int GetMaximumVoltage()
	{
		return Mode == DualMode.Series ? 6200 : 3100;
	}

	private int GetMaximumCurrent()
	{
		return Mode == DualMode.Parallel ? 10200 : 5100;
	}

	private static string FormatVoltage(int value)
	{
		return (value/100m).ToString("0.00",PolishCulture)+" V";
	}

	private static string FormatCurrent(int value)
	{
		return (value/1000m).ToString("0.000",PolishCulture)+" A";
	}

	private static bool TryParseScaled(
		string text,
		int scale,
		int maximum,
		out int value)
	{
		value=0;
		string normalized=text.Trim().Replace(',','.');
		if(!decimal.TryParse(
			normalized,
			NumberStyles.AllowDecimalPoint,
			CultureInfo.InvariantCulture,
			out decimal parsed) ||
			parsed < 0)
		{
			return false;
		}
		decimal scaled=parsed*scale;
		if(scaled != decimal.Truncate(scaled) || scaled > maximum)
		{
			return false;
		}
		value=(int)scaled;
		return true;
	}

	private sealed record SessionCreationResult(
		IPowerSupplySession? Session,
		Exception? Error);
}
