using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CodBackupConfig
{
    // matte film grain: the solid ui.bg base with ~5% neutral noise per
    // pixel plus ~10% bayer 4x4 dither layered on top, baked into a 256px
    // tile. every pixel is independent (no grid, no smoothing steps), so
    // the tile repeats seamlessly and reads as near-solid matte grain
    static class GrainFx
    {
        public static readonly Bitmap Tile = BuildTile();

        static TextureBrush _brush;
        public static TextureBrush Brush =>
            _brush ?? (_brush = new TextureBrush(Tile, WrapMode.Tile));

        static Bitmap BuildTile()
        {
            const int size = 256;

            // standard 4x4 bayer matrix, normalized to [0,1]
            float[] bayer =
            {
                0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f,
                12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f,
                3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f,
                15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f,
            };

            Color baseColor = Ui.Bg;
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, size, size);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < size; y++)
                {
                    int row = y * data.Stride;
                    for (int x = 0; x < size; x++)
                    {
                        // ~5% per-pixel noise: -2.5 .. +2.5, the same delta on
                        // all three channels = neutral grey grain
                        float n = (Hash(x, y) - 0.5f) * 5f;
                        // ~10% ordered dither over the grain: -1.5 .. +1.5
                        float d = n + (bayer[(y % 4) * 4 + (x % 4)] - 0.5f) * 3f;
                        int ar = Clamp(baseColor.R + d), ag = Clamp(baseColor.G + d), ab = Clamp(baseColor.B + d);
                        // bgra byte order for format32bppargb
                        Marshal.WriteInt32(data.Scan0, row + x * 4,
                            (255 << 24) | (ar << 16) | (ag << 8) | ab);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        // deterministic integer hash in [0,1); stable across runs and archs
        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + 142635);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h / 4294967296f;
            }
        }

        static int Clamp(float c) => c < 0 ? 0 : c > 255 ? 255 : (int)(c + 0.5f);
    }
}
