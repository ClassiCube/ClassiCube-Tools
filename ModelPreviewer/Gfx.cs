using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL;
using BmpPixelFormat = System.Drawing.Imaging.PixelFormat;
using GlPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace ModelPreviewer {
	public static class Gfx {
		
		public static int CreateTexture(string path) {
			using (FileStream fs = File.OpenRead(path))
				using (Bitmap bmp = ReadBmp(fs))
			{
				Rectangle rec = new Rectangle(0, 0, bmp.Width, bmp.Height);
				BitmapData data = bmp.LockBits(rec, ImageLockMode.ReadOnly, bmp.PixelFormat);
				int texId = CreateTexture(data.Width, data.Height, data.Scan0);
				bmp.UnlockBits(data);
				return texId;
			}
		}
		
		public static Bitmap ReadBmp(Stream src) {
			Bitmap bmp = new Bitmap(src);
			BmpPixelFormat format = bmp.PixelFormat;
			
			if (!(format == BmpPixelFormat.Format32bppRgb || format == BmpPixelFormat.Format32bppArgb)) {
				Bitmap resampled = new Bitmap(bmp.Width, bmp.Height);
				using (Graphics g = Graphics.FromImage(resampled)) {
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
				}
				
				bmp.Dispose();
				resampled = bmp;
			}
			return bmp;
		}

		public static bool AlphaTest {
			set { ToggleCap(EnableCap.AlphaTest, value); }
		}
		
		public static bool Texturing {
			set { ToggleCap(EnableCap.Texture2D, value); }
		}
		
		public unsafe static int CreateTexture(int width, int height, IntPtr scan0) {
			int texId = 0;
			GL.GenTextures(1, &texId);
			GL.BindTexture(TextureTarget.Texture2D, texId);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
			              GlPixelFormat.Bgra, PixelType.UnsignedByte, scan0);
			return texId;
		}
		
		public static void BindTexture(int texture) {
			GL.BindTexture(TextureTarget.Texture2D, texture);
		}
		
		public unsafe static void DeleteTexture(ref int texId) {
			if (texId <= 0) return;
			int id = texId;
			GL.DeleteTextures(1, &id);
			texId = -1;
		}
		
		static void ToggleCap(EnableCap cap, bool value) {
			if(value) GL.Enable(cap);
			else GL.Disable(cap);
		}
	}
}