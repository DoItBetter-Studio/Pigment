using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Glyphborn.Pigment.Controls
{
	public class PaletteControl : Control
	{
		private class PaletteVariant
		{
			public string Name;
			public Color[] Colors;
			public bool Editing = false;

			public PaletteVariant(string name, int size)
			{
				Name = name;
				Colors = new Color[size];
				for (int i = 0; i < size; i++)
					Colors[i] = Color.Black;
			}

			public PaletteVariant(string name, Color[] colors)
			{
				Name = name;
				Colors = (Color[])colors.Clone();
			}
		}

		private readonly System.Collections.Generic.List<PaletteVariant> _variants = new();
		private int _selectedVariant = -1;
		private int _hoveredVariant = -1;
		private int _hoveredColor = -1;
		private int _scrollOffset = 0;
		private int _totalListHeight = 0;

		private TextBox _editBox;

		private const int ListWidth = 140;
		private const int VariantHeight = 28;
		private const int ColorSize = 24;
		private const int ColorPadding = 4;
		private const int ColorsPerRow = 4;
		private const int AddButtonHeight = 28;

		private static readonly Color ColorBackground = Color.FromArgb(37, 37, 38);
		private static readonly Color ColorHeader = Color.FromArgb(45, 45, 48);
		private static readonly Color ColorHover = Color.FromArgb(62, 62, 64);
		private static readonly Color ColorSelected = Color.FromArgb(0, 122, 204);
		private static readonly Color ColorText = Color.White;
		private static readonly Color ColorSubtext = Color.FromArgb(140, 140, 140);
		private static readonly Color ColorBorder = Color.FromArgb(20, 20, 20);
		private static readonly Font FontVariant = new Font("Segoe UI", 9f);
		private static readonly Font FontSmall = new Font("Segoe UI", 8f);

		public event Action OnPaletteChanged;

		public PaletteControl()
		{
			DoubleBuffered = true;
			BackColor = ColorBackground;

			_editBox = new TextBox
			{
				Visible = false,
				BorderStyle = BorderStyle.None,
				BackColor = Color.FromArgb(62, 62, 64),
				ForeColor = Color.White,
				Font = FontVariant,
			};

			_editBox.KeyDown += OnEditBoxKeyDown;
			_editBox.LostFocus += OnEditBoxLostFocus;
			Controls.Add(_editBox);
		}

		public void InitFromPalette(Color[] colors)
		{
			_variants.Clear();
			_variants.Add(new PaletteVariant("Default", colors));
			_selectedVariant = 0;
			RecalcLayout();
			Invalidate();
		}

		public Color[] GetActiveColors()
		{
			if (_selectedVariant < 0 || _selectedVariant >= _variants.Count)
				return null;
			return _variants[_selectedVariant].Colors;
		}

		private void RecalcLayout()
		{
			_totalListHeight = _variants.Count * VariantHeight + AddButtonHeight;
		}

		private Rectangle GetListRect() => new Rectangle(0, 0, ListWidth, Height);
		private Rectangle GetAddButton() => new Rectangle(0, _totalListHeight - AddButtonHeight - _scrollOffset, ListWidth, AddButtonHeight);
		private Rectangle GetVariantRect(int i) => new Rectangle(0, i * VariantHeight - _scrollOffset, ListWidth, VariantHeight);
		private Rectangle GetColorGridRect() => new Rectangle(ListWidth + 8, 8, Width - ListWidth - 16, Height - 16);

		private Rectangle GetColorRect(int index)
		{
			var grid = GetColorGridRect();
			int col = index % ColorsPerRow;
			int row = index / ColorsPerRow;
			return new Rectangle(
				grid.X + col * (ColorSize + ColorPadding),
				grid.Y + row * (ColorSize + ColorPadding),
				ColorSize,
				ColorSize);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.Clear(ColorBackground);

			// Divider
			g.DrawLine(new Pen(ColorBorder), ListWidth, 0, ListWidth, Height);

			// Variant list
			for (int i = 0; i < _variants.Count; i++)
			{
				var variant = _variants[i];
				var rect = GetVariantRect(i);

				if (rect.Bottom < 0 || rect.Top > Height) continue;

				bool selected = _selectedVariant == i;
				bool hovered = _hoveredVariant == i;

				g.FillRectangle(new SolidBrush(
					selected ? ColorSelected :
					hovered ? ColorHover : ColorHeader), rect);

				g.DrawLine(new Pen(ColorBorder), 0, rect.Bottom - 1, ListWidth, rect.Bottom - 1);

				if (!variant.Editing)
				{
					g.DrawString(variant.Name, FontVariant, new SolidBrush(ColorText),
						new PointF(8, rect.Y + 7));
				}

				// Small color preview swatches in variant row
				int swatchX = ListWidth - (Math.Min(variant.Colors.Length, 4) * 10) - 6;
				for (int s = 0; s < Math.Min(variant.Colors.Length, 4); s++)
				{
					g.FillRectangle(new SolidBrush(variant.Colors[s]),
						swatchX + s * 10, rect.Y + 8, 8, 12);
				}
			}

			// Add button
			var addRect = GetAddButton();
			bool addHovered = _hoveredVariant == -2;
			g.FillRectangle(new SolidBrush(addHovered ? ColorHover : ColorHeader), addRect);
			var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
			g.DrawString("+ Add Palette", FontSmall, new SolidBrush(ColorSubtext), addRect, sf);
			g.DrawLine(new Pen(ColorBorder), 0, addRect.Bottom - 1, ListWidth, addRect.Bottom - 1);

			// Color grid
			if (_selectedVariant >= 0 && _selectedVariant < _variants.Count)
			{
				var colors = _variants[_selectedVariant].Colors;
				for (int i = 0; i < colors.Length; i++)
				{
					var rect = GetColorRect(i);

					if (colors[i].A == 0)
					{
						// Checkerboard for transparent
						int checkSize = 4;
						for (int cy = 0; cy < rect.Height; cy += checkSize)
						{
							for (int cx = 0; cx < rect.Width; cx += checkSize)
							{
								bool even = ((cx / checkSize) + (cy / checkSize)) % 2 == 0;
								var checkColor = even ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
								g.FillRectangle(new SolidBrush(checkColor),
									rect.X + cx, rect.Y + cy,
									Math.Min(checkSize, rect.Width - cx),
									Math.Min(checkSize, rect.Height - cy));
							}
						}
					}
					else
					{
						g.FillRectangle(new SolidBrush(colors[i]), rect);
					}

					bool colorHovered = _hoveredColor == i;
					g.DrawRectangle(new Pen(colorHovered ? Color.White : ColorBorder), rect);
				}

				// Color count label
				g.DrawString($"{colors.Length} color{(colors.Length != 1 ? "s" : "")}",
					FontSmall, new SolidBrush(ColorSubtext),
					new PointF(ListWidth + 8, Height - 20));
			}
			else
			{
				var grid = GetColorGridRect();
				var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
				g.DrawString("No palette selected", FontSmall, new SolidBrush(ColorSubtext), grid, sf2);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			int prevVariant = _hoveredVariant;
			int prevColor = _hoveredColor;
			_hoveredVariant = -1;
			_hoveredColor = -1;

			if (e.X < ListWidth)
			{
				for (int i = 0; i < _variants.Count; i++)
				{
					if (GetVariantRect(i).Contains(e.Location))
					{
						_hoveredVariant = i;
						break;
					}
				}
				if (_hoveredVariant == -1 && GetAddButton().Contains(e.Location))
					_hoveredVariant = -2;
			}
			else if (_selectedVariant >= 0)
			{
				var colors = _variants[_selectedVariant].Colors;
				for (int i = 0; i < colors.Length; i++)
				{
					if (GetColorRect(i).Contains(e.Location))
					{
						_hoveredColor = i;
						break;
					}
				}
			}

			if (_hoveredVariant != prevVariant || _hoveredColor != prevColor)
				Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_hoveredVariant = -1;
			_hoveredColor = -1;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			// Add button
			if (GetAddButton().Contains(e.Location))
			{
				int count = _variants.Count;
				var newVariant = _selectedVariant >= 0
					? new PaletteVariant($"Palette {count}", _variants[_selectedVariant].Colors)
					: new PaletteVariant($"Palette {count}", 4);

				_variants.Add(newVariant);
				_selectedVariant = _variants.Count - 1;
				RecalcLayout();
				Invalidate();
				OnPaletteChanged?.Invoke();
				return;
			}

			// Variant list
			if (e.X < ListWidth)
			{
				for (int i = 0; i < _variants.Count; i++)
				{
					var rect = GetVariantRect(i);
					if (rect.Contains(e.Location))
					{
						if (e.Clicks == 2)
						{
							// Double click — edit name
							StartEditing(i, rect);
						}
						else
						{
							_selectedVariant = i;
							Invalidate();
							OnPaletteChanged?.Invoke();
						}
						return;
					}
				}
				return;
			}

			// Color grid
			if (_selectedVariant >= 0)
			{
				var colors = _variants[_selectedVariant].Colors;
				for (int i = 0; i < colors.Length; i++)
				{
					if (GetColorRect(i).Contains(e.Location))
					{
						using var dialog = new ColorDialog
						{
							Color = colors[i],
							FullOpen = true,
						};

						if (dialog.ShowDialog() == DialogResult.OK)
						{
							colors[i] = dialog.Color;
							Invalidate();
							OnPaletteChanged?.Invoke();
						}
						return;
					}
				}

				// Click empty space in grid — add color if under 16
				var grid = GetColorGridRect();
				if (grid.Contains(e.Location) && colors.Length < 16)
				{
					var newColors = new Color[colors.Length + 1];
					Array.Copy(colors, newColors, colors.Length);
					newColors[colors.Length] = Color.Black;
					_variants[_selectedVariant].Colors = newColors;
					Invalidate();
					OnPaletteChanged?.Invoke();
				}
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (e.X < ListWidth)
			{
				_scrollOffset = Math.Max(0, Math.Min(_scrollOffset - e.Delta, _totalListHeight - Height));
				RecalcLayout();
				Invalidate();
			}
		}

		private void StartEditing(int index, Rectangle rect)
		{
			_variants[index].Editing = true;
			_editBox.Text = _variants[index].Name;
			_editBox.Bounds = new Rectangle(8, rect.Y + 4, ListWidth - 16, VariantHeight - 8);
			_editBox.Tag = index;
			_editBox.Visible = true;
			_editBox.Focus();
			_editBox.SelectAll();
			Invalidate();
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
			_editBox.Visible = false;
			Invalidate();
		}

		private void OnEditBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Escape)
				CommitEditing();
		}

		private void OnEditBoxLostFocus(object sender, EventArgs e)
		{
			CommitEditing();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Invalidate();
		}
	}
}