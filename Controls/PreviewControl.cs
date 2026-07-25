using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SteelEditor.Pigment.Editor;
using System;

namespace SteelEditor.Pigment.Controls
{
	public class PreviewControl : Control
	{
		private const int FB_WIDTH = 640;
		private const int FB_HEIGHT = 360;

		private readonly uint[] _framebuffer = new uint[FB_WIDTH * FB_HEIGHT];
		private readonly WriteableBitmap _surface;

		public PreviewControl()
		{
			_surface = new WriteableBitmap(
				new PixelSize(FB_WIDTH, FB_HEIGHT),
				new Vector(96, 96),
				PixelFormat.Bgra8888,
				AlphaFormat.Unpremul);

			SizeChanged += (_, _) => InvalidateVisual();
		}

		public void Redraw(SkinDocument document)
		{
			Array.Fill(_framebuffer, 0xFF1A1A1A);

			void Draw(string name, int x, int y, int w, int h)
			{
				var bmp = document.GetElement(name);
				if (bmp == null) return;
				var pixels = BitmapToPixels(bmp, document);
				DrawNineSlice(pixels, bmp.PixelSize.Width, bmp.PixelSize.Height, x, y, w, h, 16, 16, 16, 16);
			}

			// Containers
			Draw("Window", 160, 60, 320, 200);
			Draw("Panel", 8, 8, 140, 80);
			Draw("Dialogue Box", 120, 280, 400, 70);
			Draw("Portrait Frame", 124, 284, 60, 62);
			Draw("Notification", 220, 8, 200, 48);
			Draw("Tooltip", 480, 8, 150, 60);

			// Buttons
			Draw("Button", 220, 220, 80, 24);
			Draw("Button Hot", 310, 220, 80, 24);
			Draw("Button Active", 400, 220, 80, 24);

			// Item Slots
			Draw("Item Slot", 500, 60, 40, 40);
			Draw("Item Slot Hot", 500, 106, 40, 40);
			Draw("Item Slot Active", 500, 152, 40, 40);

			// Scrollbar
			Draw("Scrollbar Track", 476, 60, 16, 160);
			Draw("Scrollbar Thumb", 476, 90, 16, 48);
			Draw("Scrollbar Thumb Hot", 476, 144, 16, 48);

			// Bars
			Draw("Bar Track", 8, 100, 120, 16);
			Draw("Bar Fill", 8, 100, 80, 16);

			// Misc
			Draw("Cursor", 168, 68, 16, 16);
			Draw("Row Highlight", 164, 90, 140, 18);
			Draw("Checkbox", 8, 130, 16, 16);
			Draw("Checkbox Checked", 8, 152, 16, 16);

			BitmapUtil.WritePixels(_surface, Array.ConvertAll(_framebuffer, x => unchecked((int)x)));

			InvalidateVisual();
		}

		private void BlitTiledRegion(uint[] pixels, int imgW, int imgH,
			int dstX, int dstY, int dstW, int dstH,
			int srcX, int srcY, int srcW, int srcH)
		{
			for (int dy = 0; dy < dstH; dy++)
			{
				for (int dx = 0; dx < dstW; dx++)
				{
					int tx = dx % srcW;
					int ty = dy % srcH;
					int srcPxX = srcX + tx;
					int srcPxY = srcY + ty;

					if (srcPxX >= imgW || srcPxY >= imgH) continue;

					int dstPxX = dstX + dx;
					int dstPxY = dstY + dy;

					if (dstPxX < 0 || dstPxX >= FB_WIDTH || dstPxY < 0 || dstPxY >= FB_HEIGHT) continue;

					uint pixel = pixels[srcPxY * imgW + srcPxX];
					if ((pixel >> 24) == 0) continue;

					_framebuffer[dstPxY * FB_WIDTH + dstPxX] = pixel;
				}
			}
		}

		private void DrawNineSlice(uint[] pixels, int imgW, int imgH,
			int dstX, int dstY, int dstW, int dstH,
			int sliceLeft, int sliceTop, int sliceRight, int sliceBottom)
		{
			int centerSrcW = imgW - sliceLeft - sliceRight;
			int centerSrcH = imgH - sliceTop - sliceBottom;
			int centerDstW = dstW - sliceLeft - sliceRight;
			int centerDstH = dstH - sliceTop - sliceBottom;

			// Corners
			BlitTiledRegion(pixels, imgW, imgH, dstX, dstY, sliceLeft, sliceTop, 0, 0, sliceLeft, sliceTop);
			BlitTiledRegion(pixels, imgW, imgH, dstX + dstW - sliceRight, dstY, sliceRight, sliceTop, imgW - sliceRight, 0, sliceRight, sliceTop);
			BlitTiledRegion(pixels, imgW, imgH, dstX, dstY + dstH - sliceBottom, sliceLeft, sliceBottom, 0, imgH - sliceBottom, sliceLeft, sliceBottom);
			BlitTiledRegion(pixels, imgW, imgH, dstX + dstW - sliceRight, dstY + dstH - sliceBottom, sliceRight, sliceBottom, imgW - sliceRight, imgH - sliceBottom, sliceRight, sliceBottom);

			// Edges
			BlitTiledRegion(pixels, imgW, imgH, dstX + sliceLeft, dstY, centerDstW, sliceTop, sliceLeft, 0, centerSrcW, sliceTop);
			BlitTiledRegion(pixels, imgW, imgH, dstX + sliceLeft, dstY + dstH - sliceBottom, centerDstW, sliceBottom, sliceLeft, imgH - sliceBottom, centerSrcW, sliceBottom);
			BlitTiledRegion(pixels, imgW, imgH, dstX, dstY + sliceTop, sliceLeft, centerDstH, 0, sliceTop, sliceLeft, centerSrcH);
			BlitTiledRegion(pixels, imgW, imgH, dstX + dstW - sliceRight, dstY + sliceTop, sliceRight, centerDstH, imgW - sliceRight, sliceTop, sliceRight, centerSrcH);

			// Center
			BlitTiledRegion(pixels, imgW, imgH, dstX + sliceLeft, dstY + sliceTop, centerDstW, centerDstH, sliceLeft, sliceTop, centerSrcW, centerSrcH);
		}

		private uint[] BitmapToPixels(WriteableBitmap bmp, SkinDocument document)
		{
			int[] raw = BitmapUtil.ReadPixels(bmp);
			var pixels = new uint[raw.Length];

			var source = document.SourcePalette;
			var active = document.ActivePalette;

			for (int i = 0; i < raw.Length; i++)
			{
				var c = Color.FromUInt32(unchecked((uint)raw[i]));

				if (c.A > 0 && source != null && active != null)
				{
					// Find matching index in source palette
					int index = -1;
					for (int j = 0; j < source.Length; j++)
					{
						if (source[j].R == c.R && source[j].G == c.G && source[j].B == c.B)
						{
							index = j;
							break;
						}
					}

					if (index >= 0 && index < active.Length)
					{
						var remapped = active[index];
						pixels[i] = (uint)((remapped.A << 24) | (remapped.R << 16) | (remapped.G << 8) | remapped.B);
						continue;
					}
				}

				pixels[i] = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
			}

			return pixels;
		}

		public override void Render(DrawingContext context)
		{
			context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

			double scaleX = Bounds.Width / FB_WIDTH;
			double scaleY = Bounds.Height / FB_HEIGHT;
			double scale = Math.Min(scaleX, scaleY);

			double renderW = FB_WIDTH * scale;
			double renderH = FB_HEIGHT * scale;
			double offsetX = (Bounds.Width - renderW) / 2;
			double offsetY = (Bounds.Height - renderH) / 2;

			var destRect = new Rect(offsetX, offsetY, renderW, renderH);
			var srcRect = new Rect(0, 0, FB_WIDTH, FB_HEIGHT);

			using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
			{
				context.DrawImage(_surface, srcRect, destRect);
			}
		}
	}
}
