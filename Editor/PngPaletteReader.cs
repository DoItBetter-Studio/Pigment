using System;
using System.Drawing;
using System.IO;

namespace Glyphborn.Pigment.Editor
{
	public static class PngPaletteReader
	{
		public static Color[] ReadPalette(string path)
		{
			using var stream = File.OpenRead(path);
			using var reader = new BinaryReader(stream);

			reader.ReadBytes(8); // PNG signature

			Color[] palette = null;
			byte[] transparency = null;

			while (stream.Position < stream.Length)
			{
				byte[] lengthBytes = reader.ReadBytes(4);
				if (lengthBytes.Length < 4) break;

				int length = (lengthBytes[0] << 24) | (lengthBytes[1] << 16) |
							 (lengthBytes[2] << 8) | lengthBytes[3];

				byte[] typeBytes = reader.ReadBytes(4);
				string type = System.Text.Encoding.ASCII.GetString(typeBytes);
				byte[] data = reader.ReadBytes(length);
				reader.ReadBytes(4); // CRC

				if (type == "PLTE")
				{
					int colorCount = length / 3;
					palette = new Color[colorCount];
					for (int i = 0; i < colorCount; i++)
					{
						byte r = data[i * 3 + 0];
						byte g = data[i * 3 + 1];
						byte b = data[i * 3 + 2];
						palette[i] = Color.FromArgb(255, r, g, b);
					}
				}
				else if (type == "tRNS" && palette != null)
				{
					transparency = data;
					for (int i = 0; i < Math.Min(transparency.Length, palette.Length); i++)
					{
						var c = palette[i];
						palette[i] = Color.FromArgb(transparency[i], c.R, c.G, c.B);
					}
				}
				else if (type == "IDAT")
				{
					break;
				}
			}

			return palette;
		}
	}
}
