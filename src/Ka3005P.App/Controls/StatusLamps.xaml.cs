using System.Windows;
using System.Windows.Controls;

namespace Ka3005P.App.Controls;

public partial class StatusLamps : UserControl
{
	public static readonly DependencyProperty IsOfflineProperty=
		DependencyProperty.Register(
			nameof(IsOffline),
			typeof(bool),
			typeof(StatusLamps));
	public static readonly DependencyProperty IsOffProperty=
		DependencyProperty.Register(
			nameof(IsOff),
			typeof(bool),
			typeof(StatusLamps));
	public static readonly DependencyProperty IsOnProperty=
		DependencyProperty.Register(
			nameof(IsOn),
			typeof(bool),
			typeof(StatusLamps));

	public StatusLamps()
	{
		InitializeComponent();
	}

	public bool IsOffline
	{
		get => (bool)GetValue(IsOfflineProperty);
		set => SetValue(IsOfflineProperty,value);
	}

	public bool IsOff
	{
		get => (bool)GetValue(IsOffProperty);
		set => SetValue(IsOffProperty,value);
	}

	public bool IsOn
	{
		get => (bool)GetValue(IsOnProperty);
		set => SetValue(IsOnProperty,value);
	}
}
