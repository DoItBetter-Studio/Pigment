using Glyphborn.Pigment.Editor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Glyphborn.Pigment.Controls
{
	public class ElementListControl : Control
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

			public Rectangle HeaderRect(int width) => new Rectangle(0, Y, width, HeaderHeight);
			public Rectangle PickerRect(int width) => new Rectangle((width - PickerSize) / 2, Y + HeaderHeight + 6, PickerSize, PickerSize);
			public Rectangle FilenameRect(int width) => new Rectangle(0, Y + HeaderHeight + PickerSize + 8, width, FilenameHeight);
		}

		private readonly List<ElementEntry> _entries = new();
		private int _scrollOffset = 0;
		private int _hoveredIndex = -1;
		private int _totalHeight = 0;

		private static readonly Color ColorBackground = Color.FromArgb(37, 37, 38);
		private static readonly Color ColorHeader = Color.FromArgb(45, 45, 48);
		private static readonly Color ColorHeaderHover = Color.FromArgb(62, 62, 64);
		private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
		private static readonly Color ColorPicker = Color.FromArgb(20, 20, 20);
		private static readonly Color ColorText = Color.White;
		private static readonly Color ColorSubtext = Color.FromArgb(140, 140, 140);
		private static readonly Font FontHeader = new Font("Segoe UI", 9f);
		private static readonly Font FontFilename = new Font("Segoe UI", 8f);

		private SkinDocument _document;

		public ElementListControl()
		{
			DoubleBuffered = true;
			BackColor = ColorBackground;
		}

		public void SetElements(string[] names, SkinDocument document)
		{
			_document = document;
			_entries.Clear();
			foreach (var name in names)
				_entries.Add(new ElementEntry(name));
			RecalcLayout();
			Invalidate();
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

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.Clear(ColorBackground);

			for (int i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];

				if (entry.Y + entry.Height < 0 || entry.Y > Height)
					continue;

				// Header
				var headerRect = entry.HeaderRect(Width);
				bool hovered = _hoveredIndex == i;
				g.FillRectangle(new SolidBrush(hovered ? ColorHeaderHover : ColorHeader), headerRect);

				// Arrow
				g.DrawString(entry.Expanded ? "▼" : "▶", FontHeader, new SolidBrush(ColorSubtext), new PointF(6, entry.Y + 7));

				// Element name
				g.DrawString(entry.Name, FontHeader, new SolidBrush(ColorText), new PointF(24, entry.Y + 7));

				// Bottom border
				g.DrawLine(new Pen(Color.FromArgb(20, 20, 20)), 0, entry.Y + ElementEntry.HeaderHeight - 1, Width, entry.Y + ElementEntry.HeaderHeight - 1);

				if (entry.Expanded)
				{
					// Picker background
					var pickerRect = entry.PickerRect(Width);
					g.FillRectangle(new SolidBrush(ColorPicker), pickerRect);
					g.DrawRectangle(new Pen(ColorAccent), pickerRect);

					if (entry.Thumbnail != null)
					{
						g.DrawImage(entry.Thumbnail, pickerRect);

						var clearRect = new RectangleF(
													pickerRect.Right - 36,
													pickerRect.Bottom - 16,
													34,
													14);

						g.FillRectangle(new SolidBrush(Color.FromArgb(180, 20, 20, 20)), clearRect);
						g.DrawString("Clear", FontFilename, new SolidBrush(Color.FromArgb(200, 80, 80)), clearRect.Location);
					}
					else
					{
						// Cross placeholder
						using var pen = new Pen(Color.FromArgb(60, 60, 60), 1);
						g.DrawLine(pen, pickerRect.Left, pickerRect.Top, pickerRect.Right, pickerRect.Bottom);
						g.DrawLine(pen, pickerRect.Right, pickerRect.Top, pickerRect.Left, pickerRect.Bottom);
					}

					// Filename
					var filenameRect = entry.FilenameRect(Width);
					var sf = new StringFormat { Alignment = StringAlignment.Center };
					g.DrawString(entry.Filename, FontFilename, new SolidBrush(entry.Thumbnail != null ? ColorText : ColorSubtext), filenameRect, sf);
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			int prev = _hoveredIndex;
			_hoveredIndex = -1;

			for (int i = 0; i < _entries.Count; i++)
			{
				if (_entries[i].HeaderRect(Width).Contains(e.Location))
				{
					_hoveredIndex = i;
					break;
				}
			}

			if (_hoveredIndex != prev)
				Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_hoveredIndex = -1;
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			for (int i = 0; i < _entries.Count; i++)
			{
				var entry = _entries[i];

				// Header click — toggle expand
				if (entry.HeaderRect(Width).Contains(e.Location))
				{
					entry.Expanded = !entry.Expanded;
					entry.Height = entry.Expanded ? ElementEntry.ExpandedHeight : ElementEntry.CollapsedHeight;
					RecalcLayout();
					Invalidate();
					return;
				}

				if (entry.Expanded && entry.Thumbnail != null)
				{
					var pickerRect = entry.PickerRect(Width);
					var clearRect = new Rectangle(
						pickerRect.Right - 36,
						pickerRect.Bottom - 16,
						34,
						14);

					if (clearRect.Contains(e.Location))
					{
						entry.Thumbnail?.Dispose();
						entry.Thumbnail = null;
						entry.Filename = "No image";
						_document.SetElement(entry.Name, null);
						Invalidate();
						return;
					}
				}

				// Picker click — open file dialog
				if (entry.Expanded && entry.PickerRect(Width).Contains(e.Location))
				{
					using var dialog = new OpenFileDialog
					{
						Title = $"Select image for {entry.Name}",
						Filter = "Image Files|*.png;*.bmp|All Files|*.*",
					};

					if (dialog.ShowDialog() == DialogResult.OK)
					{
						using var raw = new Bitmap(dialog.FileName);
						var bmp = new Bitmap(raw);
						var result = _document.SetElement(entry.Name, bmp, dialog.FileName);

						if (result == PaletteSetResult.PaletteMismatch)
						{
							var response = MessageBox.Show(
								$"The image assigned to '{entry.Name}' has a different palette than the established skin palette.\n\nContinue anyway?",
								"Palette Mismatch",
								MessageBoxButtons.YesNo,
								MessageBoxIcon.Warning);

							if (response == DialogResult.No)
							{
								bmp.Dispose();
								_document.SetElement(entry.Name, null);
								return;
							}
						}

						entry.Filename = System.IO.Path.GetFileName(dialog.FileName);
						entry.Thumbnail = new Bitmap(dialog.FileName);
						Invalidate();
					}
					return;
				}
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			_scrollOffset = Math.Max(0, Math.Min(_scrollOffset - e.Delta, _totalHeight - Height));
			RecalcLayout();
			Invalidate();
		}

		protected override void OnResize(EventArgs e)
		{
			RecalcLayout();
			Invalidate();
		}
	}
}