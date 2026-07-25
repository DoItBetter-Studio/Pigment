using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SteelEditor.Pigment.Editor
{
	/// <summary>
	/// Helpers for loading arbitrary image files into pixel-accessible WriteableBitmaps
	/// and reading/writing their raw pixel data as packed ARGB ints (matching the
	/// (A&lt;&lt;24)|(R&lt;&lt;16)|(G&lt;&lt;8)|B convention used throughout Pigment).
	///
	/// All bitmaps produced here use PixelFormat.Bgra8888, which — on little-endian
	/// platforms — has the same in-memory byte order as GDI+'s Format32bppArgb, so a
	/// raw int32 read of a pixel equals (A&lt;&lt;24)|(R&lt;&lt;16)|(G&lt;&lt;8)|B, exactly like the
	/// original WinForms code assumed.
	/// </summary>
	public static class BitmapUtil
	{
		/// <summary>
		/// Decodes an image file (png/bmp/jpg/etc.) into a new pixel-accessible
		/// WriteableBitmap. The source file is fully decoded and closed before
		/// returning — the returned bitmap does not keep the file open.
		/// </summary>
		public static WriteableBitmap LoadBitmap(string path)
		{
			using var stream = File.OpenRead(path);
			using var decoded = new Bitmap(stream);

			var size = decoded.PixelSize;
			var result = new WriteableBitmap(size, decoded.Dpi, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

			using (var fb = result.Lock())
			{
				decoded.CopyPixels(new PixelRect(size), fb.Address, fb.RowBytes * size.Height, fb.RowBytes);
			}

			return result;
		}

		/// <summary>
		/// Reads a bitmap's pixels into a tightly-packed row-major int[] (no stride
		/// padding), where each int is (A&lt;&lt;24)|(R&lt;&lt;16)|(G&lt;&lt;8)|B — the same layout
		/// System.Drawing.Color.FromArgb(int) / Avalonia.Media.Color.FromUInt32(uint) expect.
		/// </summary>
		public static int[] ReadPixels(WriteableBitmap bmp)
		{
			var size = bmp.PixelSize;
			int width = size.Width;
			int height = size.Height;
			var pixels = new int[width * height];

			using var fb = bmp.Lock();
			int tightRowBytes = width * 4;

			if (fb.RowBytes == tightRowBytes)
			{
				Marshal.Copy(fb.Address, pixels, 0, pixels.Length);
			}
			else
			{
				for (int y = 0; y < height; y++)
				{
					IntPtr rowPtr = fb.Address + y * fb.RowBytes;
					Marshal.Copy(rowPtr, pixels, y * width, width);
				}
			}

			return pixels;
		}

		/// <summary>
		/// Writes a tightly-packed row-major int[] of ARGB pixels (see ReadPixels) back
		/// into a bitmap of matching size.
		/// </summary>
		public static void WritePixels(WriteableBitmap bmp, int[] pixels)
		{
			var size = bmp.PixelSize;
			int width = size.Width;
			int height = size.Height;

			using var fb = bmp.Lock();
			int tightRowBytes = width * 4;

			if (fb.RowBytes == tightRowBytes)
			{
				Marshal.Copy(pixels, 0, fb.Address, pixels.Length);
			}
			else
			{
				for (int y = 0; y < height; y++)
				{
					IntPtr rowPtr = fb.Address + y * fb.RowBytes;
					Marshal.Copy(pixels, y * width, rowPtr, width);
				}
			}
		}
	}
}
