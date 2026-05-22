using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace Glyphborn.Pigment.Controls
{
	public class ElementEntry : UserControl
	{
		private Panel _header;
		private Label _arrow;
		private Label _elementName;
		private Panel _picker;
		private Label _filename;

		private bool _expanded = false;

		private const int HeaderHeight = 28;
		private const int PickerSize = 120;
		private const int FilenameHeight = 20;
		private const int CollapsedHeight = HeaderHeight;
		private const int ExpandedHeight = HeaderHeight + PickerSize + FilenameHeight + 12;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string ElementName
		{
			get => _elementName.Text;
			set => _elementName.Text = value;
		}

		public ElementEntry(string name)
		{
			SetupControl();
			ElementName = name;
		}

		private void SetupControl()
		{
			BackColor = Color.FromArgb(37, 37, 38);
			ForeColor = Color.White;
			Dock = DockStyle.Top;
			Height = CollapsedHeight;
			Margin = new Padding(0, 0, 0, 1);

			// Header
			_header = new Panel
			{
				Dock = DockStyle.Top,
				Height = HeaderHeight,
				BackColor = Color.FromArgb(45, 45, 48),
				Cursor = Cursors.Hand,
			};

			_arrow = new Label
			{
				Text = "▶",
				ForeColor = Color.FromArgb(180, 180, 180),
				BackColor = Color.Transparent,
				Location = new Point(6, 6),
				Size = new Size(16, 16),
				Font = new Font("Segoe UI", 8f),
			};

			_elementName = new Label
			{
				ForeColor = Color.White,
				BackColor = Color.Transparent,
				Location = new Point(26, 6),
				Size = new Size(160, 16),
				Font = new Font("Segoe UI", 9f),
			};

			_header.Controls.Add(_arrow);
			_header.Controls.Add(_elementName);
			_header.Click += OnHeaderClick;
			_arrow.Click += OnHeaderClick;
			_elementName.Click += OnHeaderClick;

			_header.MouseEnter += (s, e) => _header.BackColor = Color.FromArgb(62, 62, 64);
			_header.MouseLeave += (s, e) => _header.BackColor = Color.FromArgb(45, 45, 48);

			// Picker
			_picker = new Panel
			{
				BackColor = Color.FromArgb(30, 30, 30),
				Size = new Size(PickerSize, PickerSize),
				Location = new Point(0, HeaderHeight + 6),
				Visible = false,
				Cursor = Cursors.Hand,
				BorderStyle = BorderStyle.FixedSingle,
			};

			_picker.Paint += OnPickerPaint;
			_picker.Click += OnPickerClick;

			// Filename
			_filename = new Label
			{
				Text = "No image",
				ForeColor = Color.FromArgb(140, 140, 140),
				BackColor = Color.Transparent,
				TextAlign = ContentAlignment.MiddleCenter,
				Location = new Point(0, HeaderHeight + PickerSize + 8),
				Size = new Size(Width, FilenameHeight),
				Font = new Font("Segoe UI", 8f),
				Visible = false,
			};

			Controls.Add(_header);
			Controls.Add(_picker);
			Controls.Add(_filename);

			Resize += (s, e) =>
			{
				_picker.Left = (Width - PickerSize) / 2;
				_filename.Width = Width;
			};
		}

		private void OnHeaderClick(object sender, EventArgs e)
		{
			_expanded = !_expanded;
			_arrow.Text = _expanded ? "▼" : "▶";
			_picker.Visible = _expanded;
			_filename.Visible = _expanded;
			Height = _expanded ? ExpandedHeight : CollapsedHeight;
		}

		private void OnPickerPaint(object sender, PaintEventArgs e)
		{
			// Placeholder cross pattern when no image assigned
			var g = e.Graphics;
			using var pen = new Pen(Color.FromArgb(80, 80, 80), 1);
			g.DrawLine(pen, 0, 0, _picker.Width, _picker.Height);
			g.DrawLine(pen, _picker.Width, 0, 0, _picker.Height);
		}

		private void OnPickerClick(object sender, EventArgs e)
		{
			using var dialog = new OpenFileDialog
			{
				Title = $"Select image for {ElementName}",
				Filter = "Image Files|*.png;*.bmp;*.jpg|All Files|*.*",
			};

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				_filename.Text = System.IO.Path.GetFileName(dialog.FileName);
				_filename.ForeColor = Color.FromArgb(200, 200, 200);
				// TODO: load and display thumbnail in picker
				_picker.Tag = dialog.FileName;
				_picker.Invalidate();
			}
		}
	}
}