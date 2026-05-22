using Glyphborn.Pigment.Colors;
using Glyphborn.Pigment.Controls;
using Glyphborn.Pigment.Editor;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Glyphborn.Pigment
{
	public partial class Pigment : Form
	{
		private ElementListControl _elementList;
		private PreviewControl _preview;
		private PaletteControl _palette;
		private SplitContainer _outerSplit;
		private SplitContainer _innerSplit;

		private SkinDocument _document;

		public Pigment()
		{
			InitializeComponent();
			SetupForm();
			SetupMenu();
		}

		private void Pigment_Load(object sender, EventArgs e)
		{
			SetupLayout();
		}

		private void SetupForm()
		{
			Text = "Pigment — Glyphborn Skin Editor";
			BackColor = Color.FromArgb(30, 30, 30);
			ForeColor = Color.White;
		}

		private void SetupMenu()
		{
			var menuStrip = new MenuStrip
			{
				RenderMode = ToolStripRenderMode.Professional,
				Renderer = new ToolStripProfessionalRenderer(new MenuStripColor()),
				BackColor = Color.FromArgb(45, 45, 48),
				ForeColor = Color.FromArgb(220, 220, 220),
			};

			var fileMenu = new ToolStripMenuItem("File");
			fileMenu.DropDownItems.Add("New", null, OnNew);
			fileMenu.DropDownItems.Add("Open...", null, OnOpen);
			fileMenu.DropDownItems.Add(new ToolStripSeparator());
			fileMenu.DropDownItems.Add("Save", null, OnSave);
			fileMenu.DropDownItems.Add("Save As...", null, OnSaveAs);
			fileMenu.DropDownItems.Add(new ToolStripSeparator());
			fileMenu.DropDownItems.Add("Export .gbskin", null, OnExport);
			fileMenu.DropDownItems.Add(new ToolStripSeparator());
			fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

			var editMenu = new ToolStripMenuItem("Edit");
			editMenu.DropDownItems.Add("Undo", null, OnUndo);
			editMenu.DropDownItems.Add("Redo", null, OnRedo);

			var viewMenu = new ToolStripMenuItem("View");
			viewMenu.DropDownItems.Add("Reset Layout", null, OnResetLayout);

			var helpMenu = new ToolStripMenuItem("Help");
			helpMenu.DropDownItems.Add("About Pigment", null, OnAbout);

			menuStrip.Items.Add(fileMenu);
			menuStrip.Items.Add(editMenu);
			menuStrip.Items.Add(viewMenu);
			menuStrip.Items.Add(helpMenu);

			ApplyDarkThemeToMenu(menuStrip.Items);

			MainMenuStrip = menuStrip;
			Controls.Add(menuStrip);
		}

		private void ApplyDarkThemeToMenu(ToolStripItemCollection items)
		{
			foreach (ToolStripItem item in items)
			{
				item.ForeColor = Color.White;

				if (item is ToolStripMenuItem menuItem)
				{
					ApplyDarkThemeToMenu(menuItem.DropDownItems);
				}
			}
		}

		private void SetupLayout()
		{
			_outerSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical,
				BackColor = Color.FromArgb(30, 30, 30),
			};

			_innerSplit = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical,
				BackColor = Color.FromArgb(30, 30, 30),
			};

			_elementList = new ElementListControl
			{
				Dock = DockStyle.Fill,
			};

			_preview = new PreviewControl
			{
				Dock = DockStyle.Fill,
			};

			_palette = new PaletteControl
			{
				Dock = DockStyle.Fill,
			};

			_palette.OnPaletteChanged += () => _preview.Redraw(_document);

			_outerSplit.Panel1.Controls.Add(_elementList);
			_innerSplit.Panel1.Controls.Add(_preview);
			_innerSplit.Panel2.Controls.Add(_palette);
			_outerSplit.Panel2.Controls.Add(_innerSplit);

			Controls.Add(_outerSplit);
			_outerSplit.BringToFront();
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			_outerSplit.SplitterDistance = (int)(_outerSplit.Width * 0.18);
			_innerSplit.SplitterDistance = (int)(_innerSplit.Width * 0.75);

			_document = new SkinDocument();
			_document.OnChanged += () => _preview.Redraw(_document);
			_document.OnPaletteDetected += colors => _palette.InitFromPalette(colors);

			_elementList.SetElements(new[]
			{
				"Panel", "Window", "Tooltip", "Dialogue Box", "Portrait Frame", "Notification",
				"Button", "Button Hot", "Button Active",
				"Item Slot", "Item Slot Hot", "Item Slot Active",
				"Cursor", "Row Highlight",
				"Scrollbar Track", "Scrollbar Thumb", "Scrollbar Thumb Hot",
				"Bar Track", "Bar Fill",
				"Checkbox", "Checkbox Checked",
			}, _document);

			_preview.Redraw(_document);
		}

		private void OnNew(object sender, System.EventArgs e) { }
		private void OnOpen(object sender, System.EventArgs e) { }
		private void OnSave(object sender, System.EventArgs e) { }
		private void OnSaveAs(object sender, System.EventArgs e) { }
		private void OnExport(object sender, System.EventArgs e) { }
		private void OnUndo(object sender, System.EventArgs e) { }
		private void OnRedo(object sender, System.EventArgs e) { }
		private void OnResetLayout(object sender, System.EventArgs e) { }
		private void OnAbout(object sender, System.EventArgs e) { }
	}
}