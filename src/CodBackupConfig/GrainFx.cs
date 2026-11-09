using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CodBackupConfig
{
    // deterministic film grain (smooth noise + bayer dither), seamless 96px tile
    static class GrainFx
    {
        public static readonly Bitmap Tile = BuildTile();

        static TextureBrush _brush;
        public static TextureBrush Brush =>
            _brush ?? (_brush = new TextureBrush(Tile, WrapMode.Tile));

        static Bitmap BuildTile()
        {
            const int size = 96, grid = 8;
            var rnd = new Random(1337);
            float[,] v = new float[grid, grid];
            for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                    v[x, y] = (float)rnd.NextDouble();

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
                float step = size / (float)grid;
                for (int y = 0; y < size; y++)
                {
                    int row = y * data.Stride;
                    for (int x = 0; x < size; x++)
                    {
                        // bilinear smoothstep over the wrapped grid
                        float fx = x / step, fy = y / step;
                        int gx = (int)fx % grid, gy = (int)fy % grid;
                        float tx = fx - MathF.Floor(fx), ty = fy - MathF.Floor(fy);
                        tx = tx * tx * (3f - 2f * tx);
                        ty = ty * ty * (3f - 2f * ty);
                        float n = Lerp(Lerp(v[gx, gy], v[(gx + 1) % grid, gy], tx),
                                       Lerp(v[gx, (gy + 1) % grid], v[(gx + 1) % grid, (gy + 1) % grid], tx),
                                       ty);
                        // same delta on all channels = neutral grey grain
                        float d = (n - 0.5f) * 13f + (bayer[y % 4 * 4 + x % 4] - 0.5f) * 13f;
                        int ar = Clamp(baseColor.R + d), ag = Clamp(baseColor.G + d), ab = Clamp(baseColor.B + d);
                        Marshal.WriteInt32(data.Scan0, row + x * 4,
                            (255 << 24) | (ab << 16) | (ag << 8) | ar);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static int Clamp(float c) => c < 0 ? 0 : c > 255 ? 255 : (int)(c + 0.5f);
    }
}
