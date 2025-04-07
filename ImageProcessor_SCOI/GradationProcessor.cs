using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public static class GradationProcessor
    {
        public static WriteableBitmap ApplyCurve(BitmapSource image, Func<byte, byte> curve)
        {
            if (image == null || curve == null) return null;

            WriteableBitmap result = new WriteableBitmap(image);
            byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
            image.CopyPixels(pixels, image.PixelWidth * 4, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = curve(pixels[i]);     // B
                pixels[i + 1] = curve(pixels[i + 1]); // G
                pixels[i + 2] = curve(pixels[i + 2]); // R
            }

            result.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight), pixels, image.PixelWidth * 4, 0);
            return result;
        }

        public static int[] CalculateHistogram(BitmapSource image)
        {
            int[] histogram = new int[256];
            byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
            image.CopyPixels(pixels, image.PixelWidth * 4, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte brightness = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
                histogram[brightness]++;
            }

            return histogram;
        }
    }
}