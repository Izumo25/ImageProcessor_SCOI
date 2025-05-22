using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public static class BinarizationMethods
    {
        public static WriteableBitmap ApplyBinarization(BitmapSource source, string method, double sensitivity = -0.2)
        {
            if (source == null) return null;

            double dpiX = source.DpiX;
            double dpiY = source.DpiY;

            // Конвертируем в градации серого
            var grayPixels = ConvertToGrayscale(source);
            byte[] resultPixels = new byte[grayPixels.Length];
            int width = source.PixelWidth;
            int height = source.PixelHeight;

            switch (method)
            {
                case "Гаврилов":
                    ApplyGavrilov(grayPixels, resultPixels, width, height);
                    break;
                case "Отсу":
                    ApplyOtsu(grayPixels, resultPixels, width, height);
                    break;
                case "Ниблек":
                    ApplyNiblack(grayPixels, resultPixels, width, height, 15, sensitivity);
                    break;
                case "Сауволы":
                    ApplySauvola(grayPixels, resultPixels, width, height, 15, sensitivity);
                    break;
                case "Брэдли-Рота":
                    ApplyBradleyRoth(grayPixels, resultPixels, width, height, 15, sensitivity);
                    break;
                case "Вульф":
                    ApplyWolf(grayPixels, resultPixels, width, height, 15, 0.5);
                    break;
                default:
                    throw new ArgumentException("Неизвестный метод бинаризации");
            }

            return CreateBinaryBitmap(resultPixels, width, height, dpiX, dpiY);
        }

        private static byte[] ConvertToGrayscale(BitmapSource source)
        {
            byte[] pixels = new byte[source.PixelWidth * source.PixelHeight * 4];
            source.CopyPixels(pixels, source.PixelWidth * 4, 0);

            byte[] grayPixels = new byte[source.PixelWidth * source.PixelHeight];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                grayPixels[i / 4] = (byte)(0.2125 * pixels[i + 2] + 0.7154 * pixels[i + 1] + 0.0721 * pixels[i]);
            }
            return grayPixels;
        }

        private static WriteableBitmap CreateBinaryBitmap(byte[] pixels, int width, int height, double dpiX, double dpiY)
        {
            var bitmap = new WriteableBitmap(
                width,
                height,
                dpiX,  // Используем переданные DPI
                dpiY,
                PixelFormats.Gray8,
                null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
            return bitmap;
        }

        //private static WriteableBitmap CreateBinaryBitmap(byte[] pixels, int width, int height)
        //{
        //    var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
        //    bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
        //    return bitmap;
        //}

        private static void ApplyGavrilov(byte[] grayPixels, byte[] resultPixels, int width, int height)
        {
            double threshold = CalculateAverageThreshold(grayPixels);

            for (int i = 0; i < grayPixels.Length; i++)
            {
                resultPixels[i] = grayPixels[i] <= threshold ? (byte)0 : (byte)255;
            }
        }

        private static double CalculateAverageThreshold(byte[] pixels)
        {
            double sum = 0;
            foreach (byte pixel in pixels) sum += pixel;
            return sum / pixels.Length;
        }

        private static void ApplyOtsu(byte[] grayPixels, byte[] resultPixels, int width, int height)
        {
            int[] histogram = new int[256];
            foreach (byte pixel in grayPixels) histogram[pixel]++;

            double sum = 0;
            for (int i = 0; i < 256; i++) sum += i * histogram[i];

            double sumB = 0;
            int wB = 0;
            double maxVariance = 0;
            byte threshold = 0;

            for (int i = 0; i < 256; i++)
            {
                wB += histogram[i];
                if (wB == 0) continue;

                int wF = grayPixels.Length - wB;
                if (wF == 0) break;

                sumB += i * histogram[i];
                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;
                double variance = wB * wF * (mB - mF) * (mB - mF);

                if (variance > maxVariance)
                {
                    maxVariance = variance;
                    threshold = (byte)i;
                }
            }

            for (int i = 0; i < grayPixels.Length; i++)
            {
                resultPixels[i] = grayPixels[i] <= threshold ? (byte)0 : (byte)255;
            }
        }

        private static void ApplyNiblack(byte[] grayPixels, byte[] resultPixels, int width, int height, int windowSize, double k)
        {
            int halfSize = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int x1 = Math.Max(0, x - halfSize);
                    int y1 = Math.Max(0, y - halfSize);
                    int x2 = Math.Min(width - 1, x + halfSize);
                    int y2 = Math.Min(height - 1, y + halfSize);

                    double mean = 0, stdDev = 0;
                    int count = 0;

                    for (int ny = y1; ny <= y2; ny++)
                    {
                        for (int nx = x1; nx <= x2; nx++)
                        {
                            byte pixel = grayPixels[ny * width + nx];
                            mean += pixel;
                            stdDev += pixel * pixel;
                            count++;
                        }
                    }

                    mean /= count;
                    stdDev = Math.Sqrt(stdDev / count - mean * mean);
                    double threshold = mean + k * stdDev;
                    resultPixels[y * width + x] = grayPixels[y * width + x] <= threshold ? (byte)0 : (byte)255;
                }
            }
        }

        private static void ApplySauvola(byte[] grayPixels, byte[] resultPixels, int width, int height, int windowSize, double k)
        {
            const int R = 128;
            int halfSize = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int x1 = Math.Max(0, x - halfSize);
                    int y1 = Math.Max(0, y - halfSize);
                    int x2 = Math.Min(width - 1, x + halfSize);
                    int y2 = Math.Min(height - 1, y + halfSize);

                    double mean = 0, stdDev = 0;
                    int count = 0;

                    for (int ny = y1; ny <= y2; ny++)
                    {
                        for (int nx = x1; nx <= x2; nx++)
                        {
                            byte pixel = grayPixels[ny * width + nx];
                            mean += pixel;
                            stdDev += pixel * pixel;
                            count++;
                        }
                    }

                    mean /= count;
                    stdDev = Math.Sqrt(stdDev / count - mean * mean);
                    double threshold = mean * (1 + k * (stdDev / R - 1));
                    resultPixels[y * width + x] = grayPixels[y * width + x] <= threshold ? (byte)0 : (byte)255;
                }
            }
        }

        private static void ApplyWolf(byte[] grayPixels, byte[] resultPixels, int width, int height, int windowSize, double a)
        {
            // 1. Находим минимальную яркость по всему изображению
            byte m = FindMinIntensity(grayPixels);

            // 2. Находим максимальное стандартное отклонение по всем окнам
            double R = FindMaxStdDev(grayPixels, width, height, windowSize);

            int halfSize = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 3. Вычисляем окрестность
                    int x1 = Math.Max(0, x - halfSize);
                    int y1 = Math.Max(0, y - halfSize);
                    int x2 = Math.Min(width - 1, x + halfSize);
                    int y2 = Math.Min(height - 1, y + halfSize);

                    // 4. Вычисляем среднее и СКО для окна
                    double mean, stdDev;
                    CalculateWindowStats(grayPixels, width, x1, y1, x2, y2, out mean, out stdDev);

                    // 5. Формула Кристиана Вульфа
                    double threshold = (1 - a) * mean + a * m + a * (stdDev / R) * (mean - m);

                    // 6. Бинаризация
                    resultPixels[y * width + x] = grayPixels[y * width + x] <= threshold ? (byte)0 : (byte)255;
                }
            }
        }

        private static byte FindMinIntensity(byte[] pixels)
        {
            byte min = 255;
            foreach (byte p in pixels)
            {
                if (p < min) min = p;
            }
            return min;
        }

        private static double FindMaxStdDev(byte[] pixels, int width, int height, int windowSize)
        {
            double maxStdDev = 0;
            int halfSize = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int x1 = Math.Max(0, x - halfSize);
                    int y1 = Math.Max(0, y - halfSize);
                    int x2 = Math.Min(width - 1, x + halfSize);
                    int y2 = Math.Min(height - 1, y + halfSize);

                    double mean, stdDev;
                    CalculateWindowStats(pixels, width, x1, y1, x2, y2, out mean, out stdDev);

                    if (stdDev > maxStdDev)
                    {
                        maxStdDev = stdDev;
                    }
                }
            }

            return maxStdDev;
        }

        private static void CalculateWindowStats(byte[] pixels, int width,
                                              int x1, int y1, int x2, int y2,
                                              out double mean, out double stdDev)
        {
            double sum = 0;
            double sumSq = 0;
            int count = 0;

            for (int y = y1; y <= y2; y++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    byte val = pixels[y * width + x];
                    sum += val;
                    sumSq += val * val;
                    count++;
                }
            }

            mean = sum / count;
            stdDev = Math.Sqrt((sumSq / count) - (mean * mean));
        }

        private static void ApplyBradleyRoth(byte[] grayPixels, byte[] resultPixels, int width, int height, int windowSize, double k)
        {
            long[,] integralImage = CalculateIntegralImage(grayPixels, width, height);
            int halfSize = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int x1 = Math.Max(0, x - halfSize);
                    int y1 = Math.Max(0, y - halfSize);
                    int x2 = Math.Min(width - 1, x + halfSize);
                    int y2 = Math.Min(height - 1, y + halfSize);

                    long sum = integralImage[y2, x2]
                             - (y1 > 0 ? integralImage[y1 - 1, x2] : 0)
                             - (x1 > 0 ? integralImage[y2, x1 - 1] : 0)
                             + (y1 > 0 && x1 > 0 ? integralImage[y1 - 1, x1 - 1] : 0);

                    int count = (x2 - x1 + 1) * (y2 - y1 + 1);
                    resultPixels[y * width + x] = grayPixels[y * width + x] * count < sum * (1 - k) ? (byte)0 : (byte)255;
                }
            }
        }

        private static long[,] CalculateIntegralImage(byte[] pixels, int width, int height)
        {
            long[,] integral = new long[height, width];
            for (int y = 0; y < height; y++)
            {
                long rowSum = 0;
                for (int x = 0; x < width; x++)
                {
                    rowSum += pixels[y * width + x];
                    integral[y, x] = (y > 0 ? integral[y - 1, x] : 0) + rowSum;
                }
            }
            return integral;
        }
    }
}
