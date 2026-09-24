using System.Collections.ObjectModel;
using Ka3005P.App.Infrastructure;
using Ka3005P.App.Services;
using Ka3005P.Core.Measurements;

namespace Ka3005P.App.ViewModels;

public readonly record struct ChartPoint(TimeSpan Elapsed,double Value);

public sealed class ChartViewModel : ObservableObject,IDisposable
{
	private const int MaximumPoints=50;
	private const double AxisMargin=0.5;
	private readonly IOutputController outputController;
	private readonly IChartSampleSource? sampleSource;
	private readonly IMeasurementExporter? exporter;
	private readonly IFileDialogService? fileDialog;
	private bool isOutputOn;
	private double minimumY;
	private double maximumY=1;
	private string? errorMessage;
	private bool disposed;

	public ChartViewModel(IOutputController outputController)
		: this(outputController,null,null,null)
	{
	}

	public ChartViewModel(
		IOutputController outputController,
		IChartSampleSource? sampleSource,
		IMeasurementExporter? exporter,
		IFileDialogService? fileDialog)
	{
		ArgumentNullException.ThrowIfNull(outputController);
		this.outputController=outputController;
		this.sampleSource=sampleSource;
		this.exporter=exporter;
		this.fileDialog=fileDialog;
		isOutputOn=outputController.IsOutputOn;
		outputController.OutputStateChanged+=OnOutputStateChanged;
		if(sampleSource is not null)
		{
			sampleSource.ChartSampleReceived+=OnChartSampleReceived;
		}
		ToggleOutputCommand=new AsyncRelayCommand(
			ToggleOutputAsync,
			_=>true,
			exception=>ErrorMessage=exception.Message);
		SaveVoltageCommand=new AsyncRelayCommand(
			_=>SaveAsync(MeasurementExportKind.Voltage),
			_=>exporter is not null && fileDialog is not null,
			exception=>ErrorMessage=exception.Message);
		SaveCurrentCommand=new AsyncRelayCommand(
			_=>SaveAsync(MeasurementExportKind.Current),
			_=>exporter is not null && fileDialog is not null,
			exception=>ErrorMessage=exception.Message);
	}

	public ObservableCollection<ChartPoint> Points { get; }=[];
	public AsyncRelayCommand ToggleOutputCommand { get; }
	public AsyncRelayCommand SaveVoltageCommand { get; }
	public AsyncRelayCommand SaveCurrentCommand { get; }

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

	public double MinimumY
	{
		get => minimumY;
		private set => SetProperty(ref minimumY,value);
	}

	public double MaximumY
	{
		get => maximumY;
		private set => SetProperty(ref maximumY,value);
	}

	public string OutputButtonText => IsOutputOn ? "ON" : "OFF";
	public bool IsOff => !IsOutputOn;
	public bool IsOn => IsOutputOn;

	public string? ErrorMessage
	{
		get => errorMessage;
		private set => SetProperty(ref errorMessage,value);
	}

	public void AddSample(TimeSpan elapsed,double value)
	{
		Points.Add(new ChartPoint(elapsed,value));
		while(Points.Count>MaximumPoints)
		{
			Points.RemoveAt(0);
		}
		RecalculateAxis();
	}

	public void Dispose()
	{
		if(disposed)
		{
			return;
		}
		disposed=true;
		outputController.OutputStateChanged-=OnOutputStateChanged;
		if(sampleSource is not null)
		{
			sampleSource.ChartSampleReceived-=OnChartSampleReceived;
		}
	}

	private async Task ToggleOutputAsync(object? parameter)
	{
		await outputController.SetOutputAsync(
			!IsOutputOn,
			CancellationToken.None);
	}

	private async Task SaveAsync(MeasurementExportKind kind)
	{
		if(exporter is null || fileDialog is null)
		{
			return;
		}
		string name=kind == MeasurementExportKind.Voltage
			? "napiecie.csv"
			: "prad.csv";
		string? path=await fileDialog.ChooseSavePathAsync(
			name,
			CancellationToken.None);
		if(path is null)
		{
			return;
		}
		await exporter.ExportAsync(kind,path,CancellationToken.None);
		ErrorMessage=null;
	}

	private void RecalculateAxis()
	{
		if(Points.Count == 0)
		{
			MinimumY=0;
			MaximumY=1;
			return;
		}
		double minimum=Points.Min(point=>point.Value);
		double maximum=Points.Max(point=>point.Value);
		MinimumY=Math.Max(0,minimum-AxisMargin);
		MaximumY=maximum+AxisMargin;
	}

	private void OnOutputStateChanged(object? sender,bool enabled)
	{
		IsOutputOn=enabled;
	}

	private void OnChartSampleReceived(object? sender,ChartSample sample)
	{
		AddSample(sample.Elapsed,sample.CurrentAmperes);
	}
}
