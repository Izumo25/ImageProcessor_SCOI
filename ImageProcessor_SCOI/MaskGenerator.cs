using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public static class MaskGenerator
    {
        public static WriteableBitmap CreateCircleMask(int width, int height, int radius)
        {
            var mask = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            byte[] pixels = new byte[width * height * 4];

            int centerX = width / 2;
            int centerY = height / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * 4;
                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));

                    if (distance <= radius)
                    {
                        pixels[index] = 255; // B
                        pixels[index + 1] = 255; // G
                        pixels[index + 2] = 255; // R
                        pixels[index + 3] = 255; // A
                    }
                }
            }

            mask.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            return mask;
        }
    }
}