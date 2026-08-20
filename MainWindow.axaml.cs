using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SteelEditor.Echo.Theme;
using SteelEditor.Pigment.Controls;
using SteelEditor.Pigment.Editor;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SteelEditor.Pigment
{
	public partial class MainWindow : Window
	{
		private Border? _titleBar;
		private Menu? _titleBarMenu;
		private Grid? _content;
		private ElementListControl? _elementList;
		private PreviewControl? _preview;
		private PaletteControl? _palette;

		private SkinDocument _document = null!;
		private string? _currentFilePath;

		public MainWindow()
		{
			Title = "Pigment";
			Width = 1100;
			Height = 700;
			MinWidth = 800;
			MinHeight = 500;
			CanResize = true;
			WindowDecorations = WindowDecorations.BorderOnly;
			ExtendClientAreaToDecorationsHint = true;
			Background = PigmentTheme.WindowBackground;

			var mainRoot = new Grid
			{
				RowDefinitions = new RowDefinitions("32, *")
			};

			InitializeTitleBar();
			InitializeUI();

			mainRoot.Children.Add(_titleBar!);
			mainRoot.Children.Add(_content!);

			Grid.SetRow(_titleBar!, 0);
			Grid.SetRow(_content!, 1);

			Content = mainRoot;

			Load();
		}

		private void InitializeTitleBar()
		{
			var platformSettings = Application.Current?.PlatformSettings;
			var accentColor = platformSettings?.GetColorValues().AccentColor1 ?? Color.Parse("#D2D2D2");
			var accentBrush = new SolidColorBrush(accentColor);

			_titleBar = new Border { Background = accentBrush };

			var chromeLayout = new Grid();
			chromeLayout.ColumnDefinitions.Add(new ColumnDefinition(28, GridUnitType.Pixel));
			chromeLayout.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
			chromeLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			var titleArea = new Border
			{
				Background = Brushes.Transparent,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};

			var titleBlock = new TextBlock
			{
				Text = "Pigment",
				Foreground = PigmentTheme.TextPrimary,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};

			titleArea.Child = titleBlock;

			titleArea.PointerPressed += (_, e) =>
			{
				if (e.GetCurrentPoint(titleArea).Properties.IsLeftButtonPressed)
					BeginMoveDrag(e);
			};

			WindowDecorationProperties.SetElementRole(titleArea, WindowDecorationsElementRole.TitleBar);

			Grid.SetColumn(titleArea, 0);
			Grid.SetColumnSpan(titleArea, 3);
			chromeLayout.Children.Add(titleArea);

			// Icon
			Icon = PigmentTheme.Icon;
			var icon = new Image
			{
				Source = PigmentTheme.WindowBitmap,
				Width = 20,
				Height = 20,
				Margin = new Thickness(4, 0, 4, 0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			Grid.SetColumn(icon, 0);
			chromeLayout.Children.Add(icon);

			InitializeMenu();
			Grid.SetColumn(_titleBarMenu!, 1);
			chromeLayout.Children.Add(_titleBarMenu!);

			// Window controls
			var controls = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			Grid.SetColumn(controls, 2);

			controls.Children.Add(MakeChromeButton("🗕", () => WindowState = WindowState.Minimized));
			controls.Children.Add(MakeChromeButton("🗖", () => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
			controls.Children.Add(MakeChromeButton("✕", Close));

			chromeLayout.Children.Add(controls);
			_titleBar.Child = chromeLayout;

			if (platformSettings != null)
			{
				platformSettings.ColorValuesChanged += (_, e) =>
				{
					var brush = new SolidColorBrush(e.AccentColor1);
					_titleBar.Background = brush;
					if (_titleBarMenu != null)
						_titleBarMenu.Background = brush;
				};
			}
		}

		private void InitializeMenu()
		{
			var platformSettings = Application.Current?.PlatformSettings;
			var accentColor = platformSettings?.GetColorValues().AccentColor1 ?? Color.Parse("#D2D2D2");
			var accentBrush = new SolidColorBrush(accentColor);

			_titleBarMenu = new Menu
			{
				Background = accentBrush,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
			};

			MenuItem fileMenu = new MenuItem { Header = "_File", FontSize = PigmentTheme.HeaderFontSize };

			MenuItem newItem = new MenuItem { Header = "New", FontSize = PigmentTheme.HeaderFontSize };
			newItem.Click += NewItem_Click;

			MenuItem openItem = new MenuItem { Header = "Open", FontSize = PigmentTheme.HeaderFontSize };
			openItem.Click += OpenItem_Click;

			MenuItem saveItem = new MenuItem { Header = "Save", HotKey = new KeyGesture(Key.S, KeyModifiers.Control), FontSize = PigmentTheme.HeaderFontSize };
			saveItem.Click += SaveItem_Click;

			MenuItem saveAsItem = new MenuItem { Header = "Save As", FontSize = PigmentTheme.HeaderFontSize };
			saveAsItem.Click += SaveAsItem_Click;

			MenuItem exportItem = new MenuItem { Header = "Export (.gbskin)", FontSize = PigmentTheme.HeaderFontSize };
			exportItem.Click += ExportItem_Click;

			MenuItem closeItem = new MenuItem { Header = "E_xit", HotKey = new KeyGesture(Key.X, KeyModifiers.Control | KeyModifiers.Shift), FontSize = PigmentTheme.HeaderFontSize };
			closeItem.Click += (_, _) => Close();

			fileMenu.Items.Add(newItem);
			fileMenu.Items.Add(openItem);
			fileMenu.Items.Add(new Separator());
			fileMenu.Items.Add(saveItem);
			fileMenu.Items.Add(saveAsItem);
			fileMenu.Items.Add(new Separator());
			fileMenu.Items.Add(exportItem);
			fileMenu.Items.Add(closeItem);

			MenuItem helpMenu = new MenuItem { Header = "_Help", FontSize = PigmentTheme.HeaderFontSize };

			MenuItem aboutItem = new MenuItem { Header = "About Pigment", FontSize = PigmentTheme.HeaderFontSize };
			aboutItem.Click += AboutItem_Click;

			helpMenu.Items.Add(aboutItem);

			_titleBarMenu.Items.Add(fileMenu);
			_titleBarMenu.Items.Add(helpMenu);
		}

		private static Button MakeChromeButton(string content, Action onClick)
		{
			var btn = new Button
			{
				Content = content,
				Width = 40,
				Background = Brushes.Transparent,
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center,
				CornerRadius = new CornerRadius(0)
			};
			btn.Click += (_, _) => onClick();
			return btn;
		}

		private void InitializeUI()
		{
			_content = new Grid { ColumnDefinitions = new ColumnDefinitions("2*, 4, 6*, 4, 2*") };

			var leftSplitter = new GridSplitter
			{
				Background = PigmentTheme.HeaderBackground,
				ResizeDirection = GridResizeDirection.Columns
			};

			var rightSplitter = new GridSplitter
			{
				Background = PigmentTheme.HeaderBackground,
				ResizeDirection = GridResizeDirection.Columns
			};

			_elementList = new ElementListControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};

			_preview = new PreviewControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};

			_palette = new PaletteControl
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};

			_content.Children.Add(_elementList);
			_content.Children.Add(leftSplitter);
			_content.Children.Add(_preview);
			_content.Children.Add(rightSplitter);
			_content.Children.Add(_palette);

			Grid.SetColumn(_elementList, 0);
			Grid.SetColumn(leftSplitter, 1);
			Grid.SetColumn(_preview, 2);
			Grid.SetColumn(rightSplitter, 3);
			Grid.SetColumn(_palette, 4);
		}

		private void Load()
		{
			_currentFilePath = null;
			_document = new SkinDocument();
			_document.OnChanged += () => _preview?.Redraw(_document);
			_document.OnPaletteDetected += colors =>
			{
				if (colors == null || colors.Length == 0) return;
				_palette?.InitFromPalette(colors);
			};

			// Wire Palette changes directly to SkinDocument and Preview
			_palette!.OnVariantSelected += colors =>
			{
				_document.ActivePalette = colors;
				_preview?.Redraw(_document);
			};

			_palette.OnPaletteChanged += () =>
			{
				_document.ActivePalette = _palette.GetActiveColors();
				_preview?.Redraw(_document);
			};

			_elementList!.SetElements(new[]
			{
				"Panel", "Window", "Tooltip", "Dialogue Box", "Portrait Frame", "Notification",
				"Button", "Button Hot", "Button Active",
				"Item Slot", "Item Slot Hot", "Item Slot Active",
				"Cursor", "Row Highlight",
				"Scrollbar Track", "Scrollbar Thumb", "Scrollbar Thumb Hot",
				"Bar Track", "Bar Fill",
				"Checkbox", "Checkbox Checked",
			}, _document);

			_preview!.Redraw(_document);
		}

		// --- Action Handlers ---

		private void NewItem_Click(object? sender, RoutedEventArgs e)
		{
			_currentFilePath = null;
			Title = "Pigment";

			_document = new SkinDocument();
			_document.OnChanged += () => _preview?.Redraw(_document);
			_document.OnPaletteDetected += colors =>
			{
				if (colors == null || colors.Length == 0) return;
				_palette?.InitFromPalette(colors);
			};

			_elementList?.Reset();
			_elementList?.SetDocument(_document);
			_palette?.Reset();
			_preview?.Redraw(_document);
		}

		private async void OpenItem_Click(object? sender, RoutedEventArgs e)
		{
			var storage = StorageProvider;
			if (storage == null) return;

			var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Open Pigment Project",
				AllowMultiple = false,
				FileTypeFilter = new[]
				{
					new FilePickerFileType("Pigment Project (*.pigment)") { Patterns = new[] { "*.pigment" } },
					FilePickerFileTypes.All
				}
			});

			if (files.Count == 0) return;

			string path = files[0].Path.LocalPath;

			try
			{
				NewItem_Click(sender, e);
				PigmentSerializer.Load(path, _document, _palette!, _elementList!);
				_document.ActivePalette = _palette!.GetActiveColors();
				_currentFilePath = path;
				Title = $"Pigment - {Path.GetFileName(path)}";
				_preview?.Redraw(_document);
			}
			catch (Exception ex)
			{
				await ShowInfoDialogAsync("Error Opening File", $"Failed to open project file:\n{ex.Message}");
			}
		}

		private async void SaveItem_Click(object? sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(_currentFilePath))
			{
				await SaveAsAsync();
			}
			else
			{
				SaveToPath(_currentFilePath);
			}
		}

		private async void SaveAsItem_Click(object? sender, RoutedEventArgs e)
		{
			await SaveAsAsync();
		}

		private async Task SaveAsAsync()
		{
			var storage = StorageProvider;
			if (storage == null) return;

			var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = "Save Pigment Project",
				DefaultExtension = "pigment",
				FileTypeChoices = new[]
				{
					new FilePickerFileType("Pigment Project (*.pigment)") { Patterns = new[] { "*.pigment" } }
				}
			});

			if (file == null) return;

			string path = file.Path.LocalPath;
			if (SaveToPath(path))
			{
				_currentFilePath = path;
				Title = $"Pigment - {Path.GetFileName(path)}";
			}
		}

		private bool SaveToPath(string path)
		{
			try
			{
				PigmentSerializer.Save(path, _document, _palette!);
				return true;
			}
			catch (Exception ex)
			{
				_ = ShowInfoDialogAsync("Error Saving File", $"Failed to save project:\n{ex.Message}");
				return false;
			}
		}

		private async void ExportItem_Click(object? sender, RoutedEventArgs e)
		{
			var storage = StorageProvider;
			if (storage == null) return;

			var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = "Export Compiled Skin",
				DefaultExtension = "gbskin",
				FileTypeChoices = new[]
				{
					new FilePickerFileType("Compiled Skin (*.gbskin)") { Patterns = new[] { "*.gbskin" } }
				}
			});

			if (file == null) return;

			string path = file.Path.LocalPath;
			try
			{
				PigmentSerializer.Export(path, _document, _palette!);
			}
			catch (Exception ex)
			{
				await ShowInfoDialogAsync("Error Exporting Skin", $"Failed to export skin:\n{ex.Message}");
			}
		}

		private async void AboutItem_Click(object? sender, RoutedEventArgs e)
		{
			await ShowInfoDialogAsync("About Pigment", "Pigment - UI Theme & Skin Editor\nVersion 1.0\n\nBuilt with Avalonia UI.");
		}

		private async Task ShowInfoDialogAsync(string title, string message)
		{
			var okButton = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right };

			var dialog = new Window
			{
				Title = title,
				Width = 340,
				Height = 150,
				CanResize = false,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new Thickness(16),
					Spacing = 12,
					Children =
					{
						new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
						okButton
					}
				}
			};

			okButton.Click += (_, _) => dialog.Close();

			await dialog.ShowDialog(this);
		}
	}
}