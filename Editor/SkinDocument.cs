using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace SteelEditor.Pigment.Editor
{
	public class SkinDocument
	{

		public class ElementData
		{
			public WriteableBitmap? Bitmap;
			public string? Path;
		}

		private readonly Dictionary<string, ElementData> _elementData = new();

		private readonly Dictionary<string, WriteableBitmap?> _elements = new();

		public Color[]? ActivePalette { get; set; } = null;
		public Color[]? SourcePalette { get; private set; } = null;
		public Dictionary<string, ElementData> GetAllElements() => _elementData;


		public event Action? OnChanged;
		public event Action<Color[]?>? OnPaletteDetected;

		public PaletteSetResult SetElement(string name, WriteableBitmap? bitmap, string? path = null)
		{
			if (_elementData.TryGetValue(name, out var existing))
				existing?.Bitmap?.Dispose();

			if (bitmap == null)
			{
				_elementData[name] = new ElementData { Bitmap = null, Path = null };
				_elements[name] = null;
				OnChanged?.Invoke();
				return PaletteSetResult.Ok;
			}

			if (path != null && TryReadPalette(path, out var palette))
			{
				// TryReadPalette returns true when a usable palette was read (palette != null && length <= 16).
				// If the palette is null here something unexpected happened — treat that as a null-palette result.
				if (palette == null) return PaletteSetResult.NullPalette;

				if (ActivePalette == null)
				{
					ActivePalette = palette;
					SourcePalette = palette.Clone() as Color[];
					OnPaletteDetected?.Invoke(palette);
				}
				else if (!PalettesMatch(ActivePalette, palette))
				{
					_elementData[name] = new ElementData { Bitmap = bitmap, Path = path };
					_elements[name] = bitmap;
					OnChanged?.Invoke();
					return PaletteSetResult.PaletteMismatch;
				}
			}

			_elementData[name] = new ElementData { Bitmap = bitmap, Path = path };
			_elements[name] = bitmap;
			OnChanged?.Invoke();
			return PaletteSetResult.Ok;
		}

		public WriteableBitmap? GetElement(string name)
		{
			_elements.TryGetValue(name, out var bmp);
			return bmp;
		}

		public bool Has(string name) => _elements.ContainsKey(name) && _elements[name] != null;

		public bool HasPalette => ActivePalette != null;

		private bool PalettesMatch(Color[]? a, Color[]? b)
		{
			if (a == null || b == null) return false;
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i].A != b[i].A || a[i].R != b[i].R || a[i].G != b[i].G || a[i].B != b[i].B) return false;
			return true;
		}

		public bool TryReadPalette(string path, out Color[]? palette)
		{
			palette = PngPaletteReader.ReadPalette(path);
			// Each palette should contain at most 16 colors (colors-per-palette limit).
			return palette != null && palette.Length <= 16;
		}
	}

	public enum PaletteSetResult
	{
		Ok,
		PaletteMismatch,
		NullPalette,
	}
}
