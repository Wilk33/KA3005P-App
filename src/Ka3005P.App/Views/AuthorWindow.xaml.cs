using System.Windows;

namespace Ka3005P.App.Views;

public partial class AuthorWindow : Window
{
	public AuthorWindow()
	{
		InitializeComponent();
		Title="Autor - "+AppInformation.DisplayName;
	}
}
