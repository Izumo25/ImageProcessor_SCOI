using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public static class PixelOperations
    {
        public static WriteableBitmap ApplyBlend(
      WriteableBitmap baseImage,
      WriteableBitmap overlay,
      string blendMode,
      double opacity,
      bool applyOpacity
      ) // Параметр по умолчанию для обратной совместимости
        {
            if (baseImage == null || overlay == null)
                return baseImage;

            // Приводим изображения к одинаковому размеру
            var resizedOverlay = ResizeImage(overlay, baseImage.PixelWidth, baseImage.PixelHeight);

            int width = baseImage.PixelWidth;
            int height = baseImage.PixelHeight;

            var result = new WriteableBitmap(baseImage);
            byte[] basePixels = new byte[width * height * 4];
            byte[] overlayPixels = new byte[width * height * 4];

            baseImage.CopyPixels(basePixels, width * 4, 0);
            resizedOverlay.CopyPixels(overlayPixels, width * 4, 0);

            //double effectiveOpacity = opacity;
            double effectiveOpacity = applyOpacity ? opacity : 1.0;

            for (int i = 0; i < basePixels.Length; i += 4)
            {
                double bB = basePixels[i];
                double bG = basePixels[i + 1];
                double bR = basePixels[i + 2];
                double bA = basePixels[i + 3];

                double oB = overlayPixels[i];
                double oG = overlayPixels[i + 1];
                double oR = overlayPixels[i + 2];
                double oA = overlayPixels[i + 3] * effectiveOpacity;

                double rR = 0, rG = 0, rB = 0, rA = 0;

                switch (blendMode)
                {
                    case "Normal":
                        rR = oR * oA / 255 + bR * (1 - oA / 255);
                        rG = oG * oA / 255 + bG * (1 - oA / 255);
                        rB = oB * oA / 255 + bB * (1 - oA / 255);
                        rA = oA + bA * (1 - oA / 255);
                        break;
                    case "Add":
                        rR = Math.Min(bR + oR * oA / 255, 255);
                        rG = Math.Min(bG + oG * oA / 255, 255);
                        rB = Math.Min(bB + oB * oA / 255, 255);
                        rA = Math.Min(oA + bA, 255);
                        break;
                    case "Multiply":
                        rR = bR * oR / 255 * oA / 255 + bR * (1 - oA / 255);
                        rG = bG * oG / 255 * oA / 255 + bG * (1 - oA / 255);
                        rB = bB * oB / 255 * oA / 255 + bB * (1 - oA / 255);
                        rA = Math.Min(oA + bA, 255);
                        break;
                    case "Average":
                        rR = (bR + oR) / 2 * oA / 255 + bR * (1 - oA / 255);
                        rG = (bG + oG) / 2 * oA / 255 + bG * (1 - oA / 255);
                        rB = (bB + oB) / 2 * oA / 255 + bB * (1 - oA / 255);
                        rA = Math.Min(oA + bA, 255);
                        break;
                    case "Max":
                        rR = Math.Max(bR, oR) * oA / 255 + bR * (1 - oA / 255);
                        rG = Math.Max(bG, oG) * oA / 255 + bG * (1 - oA / 255);
                        rB = Math.Max(bB, oB) * oA / 255 + bB * (1 - oA / 255);
                        rA = Math.Min(oA + bA, 255);
                        break;
                    case "Min":
                        rR = Math.Min(bR, oR) * oA / 255 + bR * (1 - oA / 255);
                        rG = Math.Min(bG, oG) * oA / 255 + bG * (1 - oA / 255);
                        rB = Math.Min(bB, oB) * oA / 255 + bB * (1 - oA / 255);
                        rA = Math.Min(oA + bA, 255);
                        break;
                }

                basePixels[i] = (byte)Math.Clamp(rB, 0, 255);
                basePixels[i + 1] = (byte)Math.Clamp(rG, 0, 255);
                basePixels[i + 2] = (byte)Math.Clamp(rR, 0, 255);
                basePixels[i + 3] = (byte)Math.Clamp(rA, 0, 255);
            }

            result.WritePixels(new Int32Rect(0, 0, width, height), basePixels, width * 4, 0);
            return result;
        }

        public static WriteableBitmap ApplyChannelMask(WriteableBitmap image, bool red, bool green, bool blue)
        {
            if (image == null) return null;

            var result = new WriteableBitmap(image);
            byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
            image.CopyPixels(pixels, image.PixelWidth * 4, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (!blue) pixels[i] = 0;     // B
                if (!green) pixels[i + 1] = 0; // G
                if (!red) pixels[i + 2] = 0;   // R
                                               // Альфа-канал (i+3) не трогаем
            }

            result.WritePixels(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight),
                             pixels, image.PixelWidth * 4, 0);
            return result;
        }

        private static WriteableBitmap ResizeImage(WriteableBitmap source, int width, int height)
        {
            var scaledBitmap = new TransformedBitmap(
                source,
                new ScaleTransform(
                    (double)width / source.PixelWidth,
                    (double)height / source.PixelHeight)
            );

            var result = new WriteableBitmap(scaledBitmap);
            result.Freeze();
            return result;
        }
    }
}
