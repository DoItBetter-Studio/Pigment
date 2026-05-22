using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace Glyphborn.Pigment.Editor
{
	public class SkinDocument
	{
		private readonly Dictionary<string, Bitmap> _elements = new();
		public Color[] ActivePalette { get; private set; } = null;


		public event Action OnChanged;
		public event Action<Color[]> OnPaletteDetected;

		public PaletteSetResult SetElement(string name, Bitmap bitmap, string path = null)
		{
			if (_elements.TryGetValue(name, out var existing))
				existing?.Dispose();

			if (bitmap == null)
			{
				_elements[name] = null;
				OnChanged?.Invoke();
				return PaletteSetResult.Ok;
			}

			if (path != null && TryReadPalette(path, out var palette))
			{
				if (ActivePalette == null)
				{
					ActivePalette = palette;
					OnPaletteDetected?.Invoke(palette);
				}
				else if (!PalettesMatch(ActivePalette, palette))
				{
					_elements[name] = bitmap;
					OnChanged?.Invoke();
					return PaletteSetResult.PaletteMismatch;
				}
			}

			_elements[name] = bitmap;
			OnChanged?.Invoke();
			return PaletteSetResult.Ok;
		}

		public Bitmap GetElement(string name)
		{
			_elements.TryGetValue(name, out var bmp);
			return bmp;
		}

		public bool Has(string name) => _elements.ContainsKey(name) && _elements[name] != null;

		public bool HasPalette => ActivePalette != null;

		private bool PalettesMatch(Color[] a, Color[] b)
		{
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i]) return false;
			return true;
		}

		public bool TryReadPalette(string path, out Color[] palette)
		{
			palette = PngPaletteReader.ReadPalette(path);
			return palette != null && palette.Length <= 16;
		}
	}

	public enum PaletteSetResult
	{
		Ok,
		PaletteMismatch,
	}
}
