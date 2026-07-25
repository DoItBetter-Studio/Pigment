using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using SteelEditor.Pigment.Editor; // Uses your existing PaletteVariantData

namespace SteelEditor.Pigment.Controls
{
	public class PaletteControl : Control
	{
		private class PaletteVariant
		{
			public string Name;
			public Color[] ColorArray;
			public bool Editing = false;

			public PaletteVariant(string name, int size)
			{
				Name = name;
				ColorArray = new Color[size];
				for (int i = 0; i < size; i++)
					ColorArray[i] = Colors.Black;
			}

			public PaletteVariant(string name, Color[] colors)
			{
				Name = name;
				ColorArray = (Color[])colors.Clone();
			}
		}

		private readonly List<PaletteVariant> _variants = new();
		private int _selectedVariant = -1;
		private int _hoveredVariant = -1;
		private int _hoveredColor = -1;
		private int _editingColorIndex = -1;
		private int _scrollOffset = 0;
		private int _totalListHeight = 0;

		private readonly TextBox _editBox;
		private Rect _editBoxBounds;

		private readonly Popup _colorPopup;
		private readonly ColorView _colorPicker;
		private bool _isUpdatingPickerColor;

		private const int ListWidth = 140;
		private const int VariantHeight = 28;
		private const int ColorSize = 24;
		private const int ColorPadding = 4;
		private const int ColorsPerRow = 4;
		private const int AddButtonHeight = 28;

		private static readonly IBrush BrushBackground = new SolidColorBrush(Color.FromRgb(37, 37, 38));
		private static readonly IBrush BrushHeader = new SolidColorBrush(Color.FromRgb(45, 45, 48));
		private static readonly IBrush BrushHover = new SolidColorBrush(Color.FromRgb(62, 62, 64));
		private static readonly IBrush BrushSelected = new SolidColorBrush(Color.FromRgb(0, 122, 204));
		private static readonly IBrush BrushText = Brushes.White;
		private static readonly IBrush BrushSubtext = new SolidColorBrush(Color.FromRgb(140, 140, 140));
		private static readonly IPen PenBorder = new Pen(new SolidColorBrush(Color.FromRgb(20, 20, 20)));
		private static readonly IPen PenHoverColor = new Pen(Brushes.White);
		private const double FontVariantSize = 13;
		private const double FontSmallSize = 11;

		public event Action? OnPaletteChanged;
		public event Action<Color[]>? OnVariantSelected;

		public PaletteControl()
		{
			ClipToBounds = true;

			// Inline TextBox for double-click renaming
			_editBox = new TextBox
			{
				IsVisible = false,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				BorderThickness = new Thickness(1),
				BorderBrush = BrushSelected,
				Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
				Foreground = Brushes.White,
				FontSize = FontVariantSize,
				Padding = new Thickness(2, 0),
				MinHeight = 0,
				VerticalContentAlignment = VerticalAlignment.Center
			};
			_editBox.KeyDown += OnEditBoxKeyDown;
			_editBox.LostFocus += OnEditBoxLostFocus;

			// Add child controls to visual tree
			VisualChildren.Add(_editBox);
			LogicalChildren.Add(_editBox);

			// Color picker popup setup
			_colorPicker = new ColorView
			{
				IsAlphaVisible = true,
				IsColorSpectrumVisible = true,
				IsColorPaletteVisible = true,
				IsColorComponentsVisible = false
			};
			_colorPicker.ColorChanged += OnColorPickerColorChanged;

			_colorPopup = new Popup
			{
				PlacementTarget = this,
				Placement = PlacementMode.BottomEdgeAlignedLeft, // Fixed: changed PlacementMode to Placement
				IsLightDismissEnabled = true,
				Child = new Border
				{
					Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
					Padding = new Thickness(8),
					CornerRadius = new CornerRadius(4),
					Child = _colorPicker
				}
			};

			LogicalChildren.Add(_colorPopup);

			SizeChanged += (_, _) =>
			{
				RecalcLayout();
				InvalidateVisual();
			};
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_editBox.IsVisible)
			{
				_editBox.Measure(_editBoxBounds.Size);
			}
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_editBox.IsVisible)
			{
				_editBox.Arrange(_editBoxBounds);
			}
			// Return finalSize directly — do NOT call base.ArrangeOverride(finalSize)
			return finalSize;
		}

		public void InitFromPalette(Color[] colors)
		{
			_variants.Clear();
			_variants.Add(new PaletteVariant("Default", colors));
			_selectedVariant = 0;
			RecalcLayout();
			InvalidateVisual();
		}

		public Color[]? GetActiveColors()
		{
			if (_selectedVariant < 0 || _selectedVariant >= _variants.Count)
				return null;
			return _variants[_selectedVariant].ColorArray;
		}

		private void RecalcLayout()
		{
			_totalListHeight = _variants.Count * VariantHeight + AddButtonHeight;
		}

		private Rect GetAddButton() => new Rect(0, _totalListHeight - AddButtonHeight - _scrollOffset, ListWidth, AddButtonHeight);
		private Rect GetVariantRect(int i) => new Rect(0, i * VariantHeight - _scrollOffset, ListWidth, VariantHeight);
		private Rect GetColorGridRect() => new Rect(ListWidth + 8, 8, Math.Max(0, Bounds.Width - ListWidth - 16), Math.Max(0, Bounds.Height - 16));

		private Rect GetColorRect(int index)
		{
			var grid = GetColorGridRect();
			int col = index % ColorsPerRow;
			int row = index / ColorsPerRow;
			return new Rect(
				grid.X + col * (ColorSize + ColorPadding),
				grid.Y + row * (ColorSize + ColorPadding),
				ColorSize,
				ColorSize);
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

		public override void Render(DrawingContext context)
		{
			base.Render(context);

			context.FillRectangle(BrushBackground, new Rect(Bounds.Size));

			// Divider line between list and grid
			context.DrawLine(PenBorder, new Point(ListWidth, 0), new Point(ListWidth, Bounds.Height));

			// Variant list
			for (int i = 0; i < _variants.Count; i++)
			{
				var variant = _variants[i];
				var rect = GetVariantRect(i);

				if (rect.Bottom < 0 || rect.Top > Bounds.Height) continue;

				bool selected = _selectedVariant == i;
				bool hovered = _hoveredVariant == i;

				context.FillRectangle(selected ? BrushSelected : hovered ? BrushHover : BrushHeader, rect);
				context.DrawLine(PenBorder, new Point(0, rect.Bottom - 1), new Point(ListWidth, rect.Bottom - 1));

				if (!variant.Editing)
					DrawText(context, variant.Name, new Point(8, rect.Y + 6), BrushText, FontVariantSize);

				// Small color preview swatches in variant row
				int swatchCount = Math.Min(variant.ColorArray.Length, 4);
				int swatchX = ListWidth - (swatchCount * 10) - 6;
				for (int s = 0; s < swatchCount; s++)
				{
					context.FillRectangle(new SolidColorBrush(variant.ColorArray[s]),
						new Rect(swatchX + s * 10, rect.Y + 8, 8, 12));
				}
			}

			// Add button
			var addRect = GetAddButton();
			bool addHovered = _hoveredVariant == -2;
			context.FillRectangle(addHovered ? BrushHover : BrushHeader, addRect);
			DrawText(context, "+ Add Palette", new Point(addRect.Center.X, addRect.Center.Y - 6), BrushSubtext, FontSmallSize, TextAlignment.Center);
			context.DrawLine(PenBorder, new Point(0, addRect.Bottom - 1), new Point(ListWidth, addRect.Bottom - 1));

			// Color grid
			if (_selectedVariant >= 0 && _selectedVariant < _variants.Count)
			{
				var colors = _variants[_selectedVariant].ColorArray;
				for (int i = 0; i < colors.Length; i++)
				{
					var rect = GetColorRect(i);

					if (colors[i].A == 0)
					{
						// Checkerboard for transparent swatches
						const int checkSize = 4;
						for (double cy = 0; cy < rect.Height; cy += checkSize)
						{
							for (double cx = 0; cx < rect.Width; cx += checkSize)
							{
								bool even = (((int)(cx / checkSize)) + ((int)(cy / checkSize))) % 2 == 0;
								var checkColor = even ? Color.FromRgb(180, 180, 180) : Color.FromRgb(100, 100, 100);
								context.FillRectangle(new SolidColorBrush(checkColor),
									new Rect(rect.X + cx, rect.Y + cy,
										Math.Min(checkSize, rect.Width - cx),
										Math.Min(checkSize, rect.Height - cy)));
							}
						}
					}
					else
					{
						context.FillRectangle(new SolidColorBrush(colors[i]), rect);
					}

					bool colorHovered = _hoveredColor == i;
					context.DrawRectangle(colorHovered ? PenHoverColor : PenBorder, rect);
				}

				// Color count label
				DrawText(context, $"{colors.Length} color{(colors.Length != 1 ? "s" : "")}",
					new Point(ListWidth + 8, Bounds.Height - 20), BrushSubtext, FontSmallSize);
			}
			else
			{
				var grid = GetColorGridRect();
				DrawText(context, "No palette selected", new Point(grid.Center.X, grid.Center.Y), BrushSubtext, FontSmallSize, TextAlignment.Center);
			}
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);
			var pos = e.GetPosition(this);
			int prevVariant = _hoveredVariant;
			int prevColor = _hoveredColor;
			_hoveredVariant = -1;
			_hoveredColor = -1;

			if (pos.X < ListWidth)
			{
				for (int i = 0; i < _variants.Count; i++)
				{
					if (GetVariantRect(i).Contains(pos))
					{
						_hoveredVariant = i;
						break;
					}
				}
				if (_hoveredVariant == -1 && GetAddButton().Contains(pos))
					_hoveredVariant = -2;
			}
			else if (_selectedVariant >= 0)
			{
				var colors = _variants[_selectedVariant].ColorArray;
				for (int i = 0; i < colors.Length; i++)
				{
					if (GetColorRect(i).Contains(pos))
					{
						_hoveredColor = i;
						break;
					}
				}
			}

			if (_hoveredVariant != prevVariant || _hoveredColor != prevColor)
				InvalidateVisual();
		}

		protected override void OnPointerExited(PointerEventArgs e)
		{
			base.OnPointerExited(e);
			_hoveredVariant = -1;
			_hoveredColor = -1;
			InvalidateVisual();
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if (_colorPopup.IsOpen)
			{
				_colorPopup.IsOpen = false;
				e.Handled = true;
				return;
			}

			var point = e.GetCurrentPoint(this);
			var pos = point.Position;

			// Right click context menu on variant items
			if (point.Properties.IsRightButtonPressed && pos.X < ListWidth)
			{
				for (int i = 0; i < _variants.Count; i++)
				{
					if (GetVariantRect(i).Contains(pos))
					{
						if (_variants.Count == 1)
						{
							ShowInfoDialog("Pigment", "Cannot remove the last palette variant.");
							return;
						}

						int index = i;
						var menu = new ContextMenu();
						var removeItem = new MenuItem { Header = "Remove Palette" };
						removeItem.Click += (_, _) =>
						{
							_variants.RemoveAt(index);

							if (_selectedVariant >= _variants.Count)
								_selectedVariant = _variants.Count - 1;

							RecalcLayout();
							InvalidateVisual();
							OnVariantSelected?.Invoke(_variants[_selectedVariant].ColorArray);
							OnPaletteChanged?.Invoke();
						};
						menu.Items.Add(removeItem);
						menu.Open(this);
						return;
					}
				}
				return;
			}

			if (!point.Properties.IsLeftButtonPressed)
				return;

			// Add palette button
			if (GetAddButton().Contains(pos))
			{
				int count = _variants.Count;
				var newVariant = _selectedVariant >= 0
					? new PaletteVariant($"Palette {count}", _variants[_selectedVariant].ColorArray)
					: new PaletteVariant($"Palette {count}", 4);

				_variants.Add(newVariant);
				_selectedVariant = _variants.Count - 1;
				RecalcLayout();
				InvalidateVisual();
				OnPaletteChanged?.Invoke();
				return;
			}

			// Variant list click / double click
			if (pos.X < ListWidth)
			{
				for (int i = 0; i < _variants.Count; i++)
				{
					var rect = GetVariantRect(i);
					if (rect.Contains(pos))
					{
						if (e.ClickCount == 2)
						{
							StartEditing(i, rect);
						}
						else
						{
							_selectedVariant = i;
							InvalidateVisual();
							OnVariantSelected?.Invoke(_variants[i].ColorArray);
							OnPaletteChanged?.Invoke();
						}
						return;
					}
				}
				return;
			}

			// Color grid swatches
			if (_selectedVariant >= 0)
			{
				var colors = _variants[_selectedVariant].ColorArray;
				for (int i = 0; i < colors.Length; i++)
				{
					var rect = GetColorRect(i);
					if (rect.Contains(pos))
					{
						OpenColorPicker(i, rect);
						return;
					}
				}

				// Click empty grid space — add a color slot
				var grid = GetColorGridRect();
				if (grid.Contains(pos) && colors.Length < 16)
				{
					var newColors = new Color[colors.Length + 1];
					Array.Copy(colors, newColors, colors.Length);
					newColors[colors.Length] = Colors.Black;
					_variants[_selectedVariant].ColorArray = newColors;
					InvalidateVisual();
					OnPaletteChanged?.Invoke();
				}
			}
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			base.OnPointerWheelChanged(e);
			var pos = e.GetPosition(this);
			if (pos.X < ListWidth)
			{
				int maxScroll = Math.Max(0, _totalListHeight - (int)Bounds.Height);
				_scrollOffset = Math.Clamp((int)(_scrollOffset - e.Delta.Y * 30), 0, maxScroll);
				RecalcLayout();
				InvalidateVisual();
			}
		}

		private void OpenColorPicker(int colorIndex, Rect swatchRect)
		{
			if (_selectedVariant < 0 || _selectedVariant >= _variants.Count) return;

			_editingColorIndex = colorIndex;
			var currentColor = _variants[_selectedVariant].ColorArray[colorIndex];

			_isUpdatingPickerColor = true;
			_colorPicker.Color = currentColor;
			_isUpdatingPickerColor = false;

			_colorPopup.HorizontalOffset = swatchRect.X;
			_colorPopup.VerticalOffset = swatchRect.Y + swatchRect.Height;
			_colorPopup.IsOpen = true;
		}

		private void OnColorPickerColorChanged(object? sender, ColorChangedEventArgs e)
		{
			if (_isUpdatingPickerColor) return;

			if (_selectedVariant >= 0 && _selectedVariant < _variants.Count)
			{
				var colors = _variants[_selectedVariant].ColorArray;
				if (_editingColorIndex >= 0 && _editingColorIndex < colors.Length)
				{
					colors[_editingColorIndex] = e.NewColor;
					InvalidateVisual();
					OnPaletteChanged?.Invoke();
				}
			}
		}

		private void StartEditing(int index, Rect rect)
		{
			_variants[index].Editing = true;
			_editBox.Text = _variants[index].Name;

			// Calculate exact width so it stays strictly within the label area
			int swatchCount = Math.Min(_variants[index].ColorArray.Length, 4);
			int swatchAreaWidth = (swatchCount * 10) + 12;
			double maxLabelWidth = Math.Max(40, ListWidth - swatchAreaWidth - 12);

			_editBoxBounds = new Rect(4, rect.Y + 3, maxLabelWidth, VariantHeight - 6);
			_editBox.Width = maxLabelWidth;
			_editBox.Height = VariantHeight - 6;
			_editBox.Tag = index;
			_editBox.IsVisible = true;

			InvalidateMeasure();
			InvalidateArrange();

			_editBox.Focus();
			_editBox.SelectAll();
			InvalidateVisual();
		}

		private void CommitEditing()
		{
			if (_editBox.Tag is int index && index < _variants.Count)
			{
				_variants[index].Name = string.IsNullOrWhiteSpace(_editBox.Text)
					? $"Palette {index}"
					: _editBox.Text;
				_variants[index].Editing = false;
			}
			_editBox.IsVisible = false;
			InvalidateMeasure();
			InvalidateArrange();
			InvalidateVisual();
		}

		private void OnEditBoxKeyDown(object? sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Escape)
				CommitEditing();
		}

		private void OnEditBoxLostFocus(object? sender, EventArgs e)
		{
			CommitEditing();
		}

		private void ShowInfoDialog(string title, string message)
		{
			if (TopLevel.GetTopLevel(this) is not Window owner) return;

			var okButton = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right };

			var dialog = new Window
			{
				Title = title,
				Width = 320,
				Height = 140,
				CanResize = false,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new Thickness(16),
					Spacing = 12,
					Children =
					{
						new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
						okButton,
					}
				}
			};

			okButton.Click += (_, _) => dialog.Close();

			_ = dialog.ShowDialog(owner);
		}

		public List<PaletteVariantData> GetVariants()
		{
			var result = new List<PaletteVariantData>();
			foreach (var v in _variants)
				result.Add(new PaletteVariantData { Name = v.Name, Colors = (Color[])v.ColorArray.Clone() });
			return result;
		}

		public int GetSelectedIndex() => _selectedVariant;

		public void LoadVariants(List<PaletteVariantData> variants, int selectedIndex)
		{
			_variants.Clear();
			foreach (var v in variants)
				_variants.Add(new PaletteVariant(v.Name, v.Colors));

			_selectedVariant = selectedIndex >= 0 && selectedIndex < _variants.Count ? selectedIndex : 0;
			RecalcLayout();
			InvalidateVisual();
		}

		public void Reset()
		{
			_variants.Clear();
			_selectedVariant = -1;
			_hoveredVariant = -1;
			_hoveredColor = -1;
			_scrollOffset = 0;
			RecalcLayout();
			InvalidateVisual();
		}
	}
}