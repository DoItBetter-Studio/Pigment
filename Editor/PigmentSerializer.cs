using Avalonia.Media;
using SteelEditor.Pigment.Controls;
using System.IO;

namespace SteelEditor.Pigment.Editor
{
	public static class PigmentSerializer
	{
		private const uint MagicPigment = 0x4D504247; // "GBPM" in Little-Endian ('G', 'B', 'P', 'M')
		private const uint MagicExport = 0x49554247; // "GBUI" in Little-Endian ('G', 'B', 'U', 'I')
		private const ushort Version = 1;

		public static void Save(string path, SkinDocument document, PaletteControl palette)
		{
			using var stream = File.Create(path);
			using var writer = new BinaryWriter(stream);

			// Magic
			writer.Write(MagicPigment);

			// Version
			writer.Write(Version);

			// Elements
			var elements = document.GetAllElements();
			writer.Write((ushort)elements.Count);

			foreach (var kvp in elements)
			{
				writer.Write(kvp.Key);
				writer.Write(kvp.Value.Path ?? string.Empty);
			}

			// Palettes
			var variants = palette.GetVariants();
			writer.Write((ushort)variants.Count);
			writer.Write((ushort)palette.GetSelectedIndex());

			foreach (var variant in variants)
			{
				writer.Write(variant.Name ?? string.Empty);
				writer.Write((ushort)(variant.Colors?.Length ?? 0));

				foreach (var color in variant.Colors ?? new Color[0])
				{
					writer.Write(color.A);
					writer.Write(color.R);
					writer.Write(color.G);
					writer.Write(color.B);
				}
			}
		}

		public static void Load(string path, SkinDocument document, PaletteControl palette, ElementListControl elementList)
		{
			using var stream = File.OpenRead(path);
			using var reader = new BinaryReader(stream);

			// Magic
			if (reader.ReadUInt32() != MagicPigment)
				throw new InvalidDataException("Not a valid .pigment file.");

			// Version
			ushort version = reader.ReadUInt16();

			// Elements
			ushort elementCount = reader.ReadUInt16();
			for (int i = 0; i < elementCount; i++)
			{
				string name = reader.ReadString();
				string imagePath = reader.ReadString();

				if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
				{
					var bmp = BitmapUtil.LoadBitmap(imagePath);
					document.SetElement(name, bmp, imagePath);
					elementList.SetThumbnail(name, imagePath);
				}
			}

			// Palettes
			ushort variantCount = reader.ReadUInt16();
			ushort selectedIndex = reader.ReadUInt16();

			var variants = new System.Collections.Generic.List<PaletteVariantData>();

			for (int i = 0; i < variantCount; i++)
			{
				string name = reader.ReadString();
				ushort colorCount = reader.ReadUInt16();
				var colors = new Color[colorCount];

				for (int j = 0; j < colorCount; j++)
				{
					byte a = reader.ReadByte();
					byte r = reader.ReadByte();
					byte g = reader.ReadByte();
					byte b = reader.ReadByte();
					colors[j] = Color.FromArgb(a, r, g, b);
				}

				variants.Add(new PaletteVariantData { Name = name, Colors = colors });
			}

			palette.LoadVariants(variants, selectedIndex);
		}

		public static void Export(string path, SkinDocument document, PaletteControl palette)
		{
			using var stream = File.Create(path);
			using var writer = new BinaryWriter(stream);

			// Magic
			writer.Write(MagicExport);

			// Palettes
			var variants = palette.GetVariants();
			writer.Write((uint)variants.Count);

			foreach (var variant in variants)
			{
				// Always write 256 entries, pad with zeros
				for (int i = 0; i < 256; i++)
				{
					if (i < (variant.Colors?.Length))
					{
						var c = variant.Colors[i];
						uint argb = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
						writer.Write(argb);
					}
					else
					{
						writer.Write((uint)0);
					}
				}
			}

			// Elements — always write in fixed order matching UISkin struct
			string[] elementOrder = new[]
			{
				"Panel", "Window", "Tooltip", "Dialogue Box", "Portrait Frame", "Notification",
				"Button", "Button Hot", "Button Active",
				"Item Slot", "Item Slot Hot", "Item Slot Active",
				"Cursor", "Row Highlight",
				"Scrollbar Track", "Scrollbar Thumb", "Scrollbar Thumb Hot",
				"Bar Track", "Bar Fill",
				"Checkbox", "Checkbox Checked",
			};

			var sourcePalette = document.SourcePalette;
			var activePalette = document.ActivePalette;

			foreach (var name in elementOrder)
			{
				var bmp = document.GetElement(name);

				if (bmp == null)
				{
					// Write empty element — 0 width, 0 height, no pixels
					writer.Write((uint)0);
					writer.Write((uint)0);
					continue;
				}

				writer.Write((uint)bmp.PixelSize.Width);
				writer.Write((uint)bmp.PixelSize.Height);

				// Read pixels (packed ARGB ints, stride-safe) and remap through palette
				int[] raw = BitmapUtil.ReadPixels(bmp);

				foreach (var pixel in raw)
				{
					var c = Color.FromUInt32(unchecked((uint)pixel));
					uint outPixel;

					if (c.A > 0 && sourcePalette != null && activePalette != null)
					{
						int index = -1;
						for (int j = 0; j < sourcePalette.Length; j++)
						{
							if (sourcePalette[j].R == c.R &&
								sourcePalette[j].G == c.G &&
								sourcePalette[j].B == c.B)
							{
								index = j;
								break;
							}
						}

						if (index >= 0 && index < activePalette.Length)
						{
							var remapped = activePalette[index];
							outPixel = (uint)((remapped.A << 24) | (remapped.R << 16) | (remapped.G << 8) | remapped.B);
						}
						else
						{
							outPixel = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
						}
					}
					else
					{
						outPixel = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
					}

					writer.Write(outPixel);
				}
			}
		}
	}

	public class PaletteVariantData
	{
		public string? Name;
		public Color[]? Colors;
	}
}