using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SteelEditor.Pigment.Editor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace SteelEditor.Pigment.Controls
{
	public class ElementListControl : Canvas
	{
		private class ElementEntry
		{
			public string Name;
			public string Filename = "No image";
			public bool Expanded = false;
			public Bitmap Thumbnail = null;

			public int Y;          // current top y in list space
			public int Height;     // current height

			public const int HeaderHeight = 28;
			public const int PickerSize = 100;
			public const int FilenameHeight = 20;
			public const int CollapsedHeight = HeaderHeight;
			public const int ExpandedHeight = HeaderHeight + PickerSize + FilenameHeight + 16;

			public ElementEntry(string name)
			{
				Name = name;
				Height = CollapsedHeight;
			}

			public Rect HeaderRect(double width) => new Rect(0, Y, width, HeaderHeight);
			public Rect PickerRect(double width) => new Rect((width - PickerSize) / 2, Y + HeaderHeight + 6, PickerSize, PickerSize);
			public Rect FilenameRect(double width) => new Rect(0, Y + HeaderHeight + PickerSize + 8, width, FilenameHeight);
		}

		// Panel.Render is sealed, so all custom painting happens on this internal,
		// non-hit-testable child control rather than on the Canvas itself.
		private class Surface : Control
		{
			private readonly Action<DrawingContext> _render;
			public Surface(Action<DrawingContext> render)
			{
				_render = render;
				IsHitTestVisible = false;
			}
			public override void Render(DrawingContext context) => _render(context);
		}

		private readonly Surface _surface;
		private readonly List<ElementEntry> _entries = new();
		private int _scrollOffset = 0;
		private int _hoveredIndex = -1;
		private int _totalHeight = 0;

		private static readonly IBrush BrushBackground = new SolidColorBrush(Color.FromRgb(37, 37, 38));
		private static readonly IBrush BrushHeader = new SolidColorBrush(Color.FromRgb(45, 45, 48));
		private static readonly IBrush BrushHeaderHover = new SolidColorBrush(Color.FromRgb(62, 62, 64));
		private static readonly IBrush BrushAccent = new SolidColorBrush(Color.FromRgb(0, 122, 204));
		private static readonly IBrush BrushPicker = new SolidColorBrush(Color.FromRgb(20, 20, 20));
		private static readonly IBrush BrushText = Brushes.White;
		private static readonly IBrush BrushSubtext = new SolidColorBrush(Color.FromRgb(140, 140, 140));
		private static readonly IPen PenBottomBorder = new Pen(new SolidColorBrush(Color.FromRgb(20, 20, 20)));
		private static readonly IPen PenAccent = new Pen(BrushAccent);
		private static readonly IPen PenCross = new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)));
		private const double FontHeaderSize = 13;
		private const double FontFilenameSize = 11;

		private SkinDocument _document;

		public ElementListControl()
		{
			// Canvas (like any Panel) only hit-tests where it has a Background brush —
			// without this, clicks/hover over the custom-painted area would never register.
			Background = Brushes.Transparent;

			_surface = new Surface(RenderSurface);
			Children.Add(_surface);
			Canvas.SetLeft(_surface, 0);
			Canvas.SetTop(_surface, 0);

			SizeChanged += (_, _) =>
			{
				_surface.Width = Bounds.Width;
				_surface.Height = Bounds.Height;
				RecalcLayout();
				Redraw();
			};
		}

		private void Redraw() => _surface.InvalidateVisual();

		public void SetElements(string[] names, SkinDocument document)
		{
			_document = document;
			_entries.Clear();
			foreach (var name in names)
				_entries.Add(new ElementEntry(name));
			RecalcLayout();
			Redraw();
		}

		private void RecalcLayout()
		{
			int y = 0;
			foreach (var entry in _entries)
			{
				entry.Y = y - _scrollOffset;
				y += entry.Height;
			}
			_totalHeight = y;
		}

		private static void DrawText(DrawingContext context, string text, Point origin, IBrush brush, double size, TextAlignment align = TextAlignment.Left)
		{
			var formatted = new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface("Segoe UI"),
				size,
				brush)
			{
				TextAlignment = align,
			};
			context.DrawText(formatted, origin);
		}

		private void RenderSurface(DrawingContext context)
		{
			context.FillRectangle(BrushBackground, new Rect(Bounds.Size));

			for (int i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];

				if (entry.Y + entry.Height < 0 || entry.Y > Bounds.Height)
					continue;

				// Header
				var headerRect = entry.HeaderRect(Bounds.Width);
				bool hovered = _hoveredIndex == i;
				context.FillRectangle(hovered ? BrushHeaderHover : BrushHeader, headerRect);

				// Arrow
				DrawText(context, entry.Expanded ? "▼" : "▶", new Point(6, entry.Y + 7), BrushSubtext, FontHeaderSize);

				// Element name
				DrawText(context, entry.Name, new Point(24, entry.Y + 7), BrushText, FontHeaderSize);

				// Bottom border
				context.DrawLine(PenBottomBorder,
					new Point(0, entry.Y + ElementEntry.HeaderHeight - 1),
					new Point(Bounds.Width, entry.Y + ElementEntry.HeaderHeight - 1));

				if (entry.Expanded)
				{
					var pickerRect = entry.PickerRect(Bounds.Width);
					context.FillRectangle(BrushPicker, pickerRect);
					context.DrawRectangle(PenAccent, pickerRect);

					if (entry.Thumbnail != null)
					{
						context.DrawImage(entry.Thumbnail, new Rect(entry.Thumbnail.Size), pickerRect);

						var clearRect = new Rect(pickerRect.Right - 36, pickerRect.Bottom - 16, 34, 14);
						context.FillRectangle(new SolidColorBrush(Color.FromArgb(180, 20, 20, 20)), clearRect);
						DrawText(context, "Clear", clearRect.Position, new SolidColorBrush(Color.FromRgb(200, 80, 80)), FontFilenameSize);
					}
					else
					{
						context.DrawLine(PenCross, pickerRect.TopLeft, pickerRect.BottomRight);
						context.DrawLine(PenCross, pickerRect.TopRight, pickerRect.BottomLeft);
					}

					var filenameRect = entry.FilenameRect(Bounds.Width);
					DrawText(context, entry.Filename,
						new Point(filenameRect.Center.X, filenameRect.Y),
						entry.Thumbnail != null ? BrushText : BrushSubtext,
						FontFilenameSize, TextAlignment.Center);
				}
			}
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);
			var pos = e.GetPosition(this);
			int prev = _hoveredIndex;
			_hoveredIndex = -1;

			for (int i = 0; i < _entries.Count; i++)
			{
				if (_entries[i].HeaderRect(Bounds.Width).Contains(pos))
				{
					_hoveredIndex = i;
					break;
				}
			}

			if (_hoveredIndex != prev)
				Redraw();
		}

		protected override void OnPointerExited(PointerEventArgs e)
		{
			base.OnPointerExited(e);
			_hoveredIndex = -1;
			Redraw();
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);
			if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
				return;

			var pos = e.GetPosition(this);

			for (int i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];

				// Header click — toggle expand
				if (entry.HeaderRect(Bounds.Width).Contains(pos))
				{
					entry.Expanded = !entry.Expanded;
					entry.Height = entry.Expanded ? ElementEntry.ExpandedHeight : ElementEntry.CollapsedHeight;
					RecalcLayout();
					Redraw();
					return;
				}

				if (entry.Expanded && entry.Thumbnail != null)
				{
					var pickerRect = entry.PickerRect(Bounds.Width);
					var clearRect = new Rect(pickerRect.Right - 36, pickerRect.Bottom - 16, 34, 14);

					if (clearRect.Contains(pos))
					{
						entry.Thumbnail?.Dispose();
						entry.Thumbnail = null;
						entry.Filename = "No image";
						_document.SetElement(entry.Name, null);
						Redraw();
						return;
					}
				}

				// Picker click — open file dialog
				if (entry.Expanded && entry.PickerRect(Bounds.Width).Contains(pos))
				{
					_ = OpenImageForEntryAsync(entry);
					return;
				}
			}
		}

		private async Task OpenImageForEntryAsync(ElementEntry entry)
		{
			var topLevel = TopLevel.GetTopLevel(this);
			if (topLevel?.StorageProvider == null) return;

			var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = $"Select image for {entry.Name}",
				AllowMultiple = false,
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Image Files") { Patterns = new[] { "*.png", "*.bmp" } },
					FilePickerFileTypes.All,
				}
			});

			if (files == null || files.Count == 0) return;
			string path = files[0].Path.LocalPath;

			var bmp = BitmapUtil.LoadBitmap(path);
			var result = _document.SetElement(entry.Name, bmp, path);

			if (result == PaletteSetResult.PaletteMismatch)
			{
				bool proceed = await ShowYesNoDialogAsync(
					"Palette Mismatch",
					$"The image assigned to '{entry.Name}' has a different palette than the established skin palette.\n\nContinue anyway?");

				if (!proceed)
				{
					bmp.Dispose();
					_document.SetElement(entry.Name, null);
					return;
				}
			}

			entry.Filename = Path.GetFileName(path);
			entry.Thumbnail?.Dispose();
			entry.Thumbnail = LoadThumbnail(path);
			Redraw();
		}

		private static Bitmap LoadThumbnail(string path)
		{
			using var stream = File.OpenRead(path);
			return new Bitmap(stream);
		}

		// Minimal stand-in for WinForms' MessageBox.Show(..., YesNo) — an owned
		// window with two buttons, awaited via a TaskCompletionSource. Swap for a
		// real dialog service if you have one.
		private async Task<bool> ShowYesNoDialogAsync(string title, string message)
		{
			if (TopLevel.GetTopLevel(this) is not Window owner) return false;

			var tcs = new TaskCompletionSource<bool>();

			var yesButton = new Button { Content = "Yes" };
			var noButton = new Button { Content = "No" };

			var dialog = new Window
			{
				Title = title,
				Width = 380,
				Height = 170,
				CanResize = false,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new Thickness(16),
					Spacing = 12,
					Children =
					{
						new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
						new StackPanel
						{
							Orientation = Orientation.Horizontal,
							Spacing = 8,
							HorizontalAlignment = HorizontalAlignment.Right,
							Children = { yesButton, noButton }
						}
					}
				}
			};

			yesButton.Click += (_, _) => { tcs.TrySetResult(true); dialog.Close(); };
			noButton.Click += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
			dialog.Closed += (_, _) => tcs.TrySetResult(false);

			await dialog.ShowDialog(owner);
			return await tcs.Task;
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			base.OnPointerWheelChanged(e);
			// Avalonia's wheel delta is normalized (~1.0 per notch), unlike WinForms' 120 —
			// scaled here to approximate the old scroll feel; tune to taste.
			_scrollOffset = Math.Max(0, Math.Min((int)(_scrollOffset - e.Delta.Y * 40), _totalHeight - (int)Bounds.Height));
			RecalcLayout();
			Redraw();
		}

		public void Reset()
		{
			foreach (var entry in _entries)
				entry.Thumbnail?.Dispose();

			foreach (var entry in _entries)
			{
				entry.Thumbnail = null;
				entry.Filename = "No image";
				entry.Expanded = false;
				entry.Height = ElementEntry.CollapsedHeight;
			}

			_scrollOffset = 0;
			RecalcLayout();
			Redraw();
		}

		public void SetDocument(SkinDocument document)
		{
			_document = document;
		}

		public void SetThumbnail(string name, string path)
		{
			foreach (var entry in _entries)
			{
				if (entry.Name == name)
				{
					entry.Thumbnail?.Dispose();
					entry.Thumbnail = LoadThumbnail(path);
					entry.Filename = Path.GetFileName(path);
					break;
				}
			}
			Redraw();
		}
	}
}