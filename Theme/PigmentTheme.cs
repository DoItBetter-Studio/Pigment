using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace SteelEditor.Echo.Theme
{
	public static class PigmentTheme
	{
		// Backgrounds
		public static readonly IBrush WindowBackground = new SolidColorBrush(Color.Parse("#2D2D30"));
		public static readonly IBrush ContainerBackground = new SolidColorBrush(Color.Parse("#1E1E1E"));
		public static readonly IBrush HeaderBackground = new SolidColorBrush(Color.Parse("#141414"));

		// Controls
		public static readonly IBrush ButtonBackground = new SolidColorBrush(Color.Parse("#2D2D2D"));
		public static readonly IBrush ButtonHover = new SolidColorBrush(Color.Parse("#39394B"));
		public static readonly IBrush ButtonHighlight = new SolidColorBrush(Color.Parse("#280CB4"));
		public static readonly IBrush ButtonAdd = new SolidColorBrush(Color.Parse("#287828"));
		public static readonly IBrush ButtonDelete = new SolidColorBrush(Color.Parse("#641E1E"));
		public static readonly IBrush BorderBrush = new SolidColorBrush(Color.Parse("#464646"));

		// Accents
		public static readonly IBrush AccentPrimary = new SolidColorBrush(Color.Parse("#1E90FF")); // DodgerBlue
		public static readonly IBrush AccentHighlight = new SolidColorBrush(Color.Parse("#007ACC"));

		// Text
		public static readonly IBrush TextPrimary = Brushes.White;
		public static readonly IBrush TextMuted = Brushes.LightGray;

		// Custom Font Sizes/Styles (Optional)
		public const double HeaderFontSize = 12;

		public static WindowIcon? Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Pigment/Assets/Pigment.ico")));
		public static Bitmap? WindowBitmap = new Bitmap(AssetLoader.Open(new Uri("avares://Pigment/Assets/Pigment.ico")));

		public static SolidColorBrush GetBrush(string hex) => SolidColorBrush.Parse(hex);
	}
}
