using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SteelEditor.Echo.Theme;

namespace SteelEditor.Pigment
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			Resources["OverlayCornerRadius"] = new CornerRadius(0);

			if (Application.Current?.Resources is { } resources)
			{
				resources["ButtonBackgroundPointerOver"] = PigmentTheme.ButtonHover;
				resources["ListBoxItemParagraphBackgroundPointerOver"] = PigmentTheme.ButtonHover;
			}

			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.MainWindow = new MainWindow();
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}