using Glyphborn.Pigment.Editor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Glyphborn.Pigment.Controls
{
	public class PreviewControl : Control
	{
		private const int FB_WIDTH = 640;
		private const int FB_HEIGHT = 360;

		private readonly uint[] _framebuffer = new uint[FB_WIDTH * FB_HEIGHT];
		private Bitmap _surface;

		public PreviewControl()
		{
			DoubleBuffered = true;
			BackColor = Color.Black;
			_surface = new Bitmap(FB_WIDTH, FB_HEIGHT, PixelFormat.Format32bppArgb);
		}

		public void Redraw(SkinDocument document)
		{
			Array.Fill(_framebuffer, 0xFF1A1A1A);

			void Draw(string name, int x, int y, int w, int h)
			{
				var bmp = document.GetElement(name);
				if (bmp == null) return;
				var pixels = BitmapToPixels(bmp);
				DrawNineSlice(pixels, bmp.Width, bmp.Height, x, y, w, h, 16, 16, 16, 16);
			}

			// Containers
			Draw("Window", 160, 60, 320, 200);
			Draw("Panel", 8, 8, 140, 80);
			Draw("Dialogue Box", 120, 280, 400, 70);
			Draw("Portrait Frame", 124, 284, 60, 62);
			Draw("Notification", 220, 8, 200, 28);
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

			var data = _surface.LockBits(
				new Rectangle(0, 0, FB_WIDTH, FB_HEIGHT),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			Marshal.Copy(
				Array.ConvertAll(_framebuffer, x => (int)x),
				0, data.Scan0, _framebuffer.Length);

			_surface.UnlockBits(data);
			Invalidate();
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

		private uint[] BitmapToPixels(Bitmap bmp)
		{
			var pixels = new uint[bmp.Width * bmp.Height];
			var data = bmp.LockBits(
				new Rectangle(0, 0, bmp.Width, bmp.Height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);

			Marshal.Copy(data.Scan0, (int[])(object)pixels, 0, pixels.Length);
			bmp.UnlockBits(data);
			return pixels;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			g.Clear(Color.Black);

			float scaleX = (float)Width / FB_WIDTH;
			float scaleY = (float)Height / FB_HEIGHT;
			float scale = Math.Min(scaleX, scaleY);

			int renderW = (int)(FB_WIDTH * scale);
			int renderH = (int)(FB_HEIGHT * scale);
			int offsetX = (Width - renderW) / 2;
			int offsetY = (Height - renderH) / 2;

			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			g.DrawImage(_surface, offsetX, offsetY, renderW, renderH);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Invalidate();
		}
	}
}