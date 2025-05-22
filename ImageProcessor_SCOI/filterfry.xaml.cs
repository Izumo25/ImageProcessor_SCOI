using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcess
{
    public partial class FurieWindow : Window
    {
        private BitmapSource _originalImage;
        private Complex[][,] _fourierTransform;
        private bool _isFourierCalculated = false;

        public FurieWindow(BitmapSource image)
        {
            InitializeComponent();
            _originalImage = image;
            OriginalImage.Source = _originalImage;
            InitializeFilterComboBox();
            UpdateFilterControls();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height < 650)
            {
                // Дополнительные действия при сильном уменьшении высоты
                FilterPreviewImage.Width = 80;
                FilterPreviewImage.Height = 40;
            }
            else
            {
                FilterPreviewImage.Width = 120;
                FilterPreviewImage.Height = 60;
            }
        }

        private void InitializeFilterComboBox()
        {
            FilterTypeComboBox.Items.Clear();
            FilterTypeComboBox.Items.Add(new ComboBoxItem { Content = "Низкочастотный" });
            FilterTypeComboBox.Items.Add(new ComboBoxItem { Content = "Высокочастотный" });
            FilterTypeComboBox.Items.Add(new ComboBoxItem { Content = "Полосовой" });
            FilterTypeComboBox.Items.Add(new ComboBoxItem { Content = "Режекторный" });
            FilterTypeComboBox.Items.Add(new ComboBoxItem { Content = "Узкополосный полосовой" });
            FilterTypeComboBox.Items.Add(new ComboBoxItem { Content = "Узкополосный режекторный" });
            FilterTypeComboBox.SelectedIndex = 0;
        }

        private void FilterTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFilterControls();
            if (_isFourierCalculated)
            {
                ShowFilterPreview();
            }
        }

        private void UpdateFilterControls()
        {
            string filterType = (FilterTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            RadiusLabel1.Visibility = Visibility.Visible;
            RadiusTextBox1.Visibility = Visibility.Visible;
            RadiusLabel2.Visibility = Visibility.Visible;
            RadiusTextBox2.Visibility = Visibility.Visible;
            CircleRadiusLabel.Visibility = Visibility.Collapsed;
            CircleRadiusTextBox.Visibility = Visibility.Collapsed;
            CircleCountLabel.Visibility = Visibility.Collapsed;
            CircleCountTextBox.Visibility = Visibility.Collapsed;
            CircleDistanceLabel.Visibility = Visibility.Collapsed;
            CircleDistanceTextBox.Visibility = Visibility.Collapsed;

            switch (filterType)
            {
                case "Низкочастотный":
                    RadiusLabel1.Text = "Радиус:";
                    RadiusLabel2.Visibility = Visibility.Collapsed;
                    RadiusTextBox2.Visibility = Visibility.Collapsed;
                    NarrowBandParams.Visibility = Visibility.Collapsed;
                    break;
                case "Высокочастотный":
                    RadiusLabel1.Text = "Радиус:";
                    RadiusLabel2.Visibility = Visibility.Collapsed;
                    RadiusTextBox2.Visibility = Visibility.Collapsed;
                    NarrowBandParams.Visibility = Visibility.Collapsed;
                    break;
                case "Полосовой":
                    RadiusLabel1.Text = "Внутренний радиус:";
                    RadiusLabel2.Text = "Внешний радиус:";
                    NarrowBandParams.Visibility = Visibility.Collapsed;
                    break;
                case "Режекторный":
                    RadiusLabel1.Text = "Внутренний радиус:";
                    RadiusLabel2.Text = "Внешний радиус:";
                    NarrowBandParams.Visibility = Visibility.Collapsed;
                    break;
                case "Узкополосный полосовой":
                case "Узкополосный режекторный":
                    RadiusLabel1.Visibility = Visibility.Collapsed;
                    RadiusTextBox1.Visibility = Visibility.Collapsed;
                    RadiusLabel2.Visibility = Visibility.Collapsed;
                    RadiusTextBox2.Visibility = Visibility.Collapsed;
                    CircleRadiusLabel.Visibility = Visibility.Visible;
                    CircleRadiusTextBox.Visibility = Visibility.Visible;
                    CircleCountLabel.Visibility = Visibility.Visible;
                    CircleCountTextBox.Visibility = Visibility.Visible;
                    NarrowBandParams.Visibility = Visibility.Visible;
                    CircleDistanceLabel.Visibility = Visibility.Visible;
                    CircleDistanceTextBox.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ShowFilterPreview()
        {
            if (!_isFourierCalculated) return;

            string filterType = (FilterTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (filterType == null) return;

            int width = _fourierTransform[0].GetLength(1);
            int height = _fourierTransform[0].GetLength(0);
            WriteableBitmap filterPreview = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            byte[] pixels = new byte[width * height * 4];

            int centerX = width / 2;
            int centerY = height / 2;

            for (int i = 3; i < pixels.Length; i += 4)
                pixels[i] = 255; // Alpha

            void SetPixelWhite(int x, int y)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    int pos = (y * width + x) * 4;
                    pixels[pos] = 255;     // B
                    pixels[pos + 1] = 255; // G
                    pixels[pos + 2] = 255; // R
                }
            }

            if (filterType == "Низкочастотный" || filterType == "Высокочастотный")
            {
                if (!double.TryParse(RadiusTextBox1.Text, out double radius)) return;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double dx = x - centerX;
                        double dy = y - centerY;
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        bool keep = filterType == "Низкочастотный" ? distance <= radius : distance > radius;
                        if (keep) SetPixelWhite(x, y);
                    }
                }
            }
            else if (filterType == "Полосовой" || filterType == "Режекторный")
            {
                if (!double.TryParse(RadiusTextBox1.Text, out double radius1) ||
                    !double.TryParse(RadiusTextBox2.Text, out double radius2)) return;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double dx = x - centerX;
                        double dy = y - centerY;
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        bool keep = filterType == "Полосовой"
                            ? (distance >= radius1 && distance <= radius2)
                            : (distance < radius1 || distance > radius2);

                        if (keep) SetPixelWhite(x, y);
                    }
                }
            }
            else if (filterType == "Узкополосный полосовой" || filterType == "Узкополосный режекторный")
            {
                if (!int.TryParse(CircleCountTextBox.Text, out int circleCount) ||
                    !double.TryParse(CircleRadiusTextBox.Text, out double circleRadius) ||
                    !double.TryParse(CircleDistanceTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out double circleDistance)) return;

                double angleStep = 2 * Math.PI / circleCount;

                for (int i = 0; i < circleCount; i++)
                {
                    double angle = i * angleStep;
                    int circleX = centerX + (int)(circleDistance * Math.Cos(angle));
                    int circleY = centerY + (int)(circleDistance * Math.Sin(angle));

                    int r = (int)Math.Ceiling(circleRadius);
                    for (int y = Math.Max(0, circleY - r); y <= Math.Min(height - 1, circleY + r); y++)
                    {
                        for (int x = Math.Max(0, circleX - r); x <= Math.Min(width - 1, circleX + r); x++)
                        {
                            double dx = x - circleX;
                            double dy = y - circleY;
                            if (dx * dx + dy * dy <= circleRadius * circleRadius)
                            {
                                SetPixelWhite(x, y);
                            }
                        }
                    }
                }

                if (filterType == "Узкополосный режекторный")
                {
                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        pixels[i] = (byte)(255 - pixels[i]);
                        pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                        pixels[i + 2] = (byte)(255 - pixels[i + 2]);
                    }
                }
            }

            filterPreview.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            FilterPreviewImage.Source = filterPreview;
        }




        private void DrawCircle(byte[] pixels, int width, int height, int centerX, int centerY, double radius, byte value)
        {
            int r = (int)radius;
            int x0 = Math.Max(0, centerX - r);
            int x1 = Math.Min(width - 1, centerX + r);
            int y0 = Math.Max(0, centerY - r);
            int y1 = Math.Min(height - 1, centerY + r);

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    double dx = x - centerX;
                    double dy = y - centerY;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int pos = (y * width + x) * 4;
                        pixels[pos] = value;     // B
                        pixels[pos + 1] = value; // G
                        pixels[pos + 2] = value; // R
                    }
                }
            }
        }

        private void ComputeDFT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_originalImage == null) return;

                var sw = Stopwatch.StartNew();
                var writableImage = new WriteableBitmap(_originalImage);
                _fourierTransform = ComputeDFT(writableImage);
                FourierTransformImage.Source = VisualizeSpectrum(_fourierTransform);
                _isFourierCalculated = true;
                ShowFilterPreview();
                sw.Stop();
                StatusText.Text = $"ДПФ вычислено за {sw.ElapsedMilliseconds} мс";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вычислении ДПФ: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isFourierCalculated)
                {
                    MessageBox.Show("Сначала вычислите Фурье-образ", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string filterType = (FilterTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (filterType == null) return;

                var sw = Stopwatch.StartNew();
                Complex[][,] filtered = null;

                if (filterType == "Низкочастотный" || filterType == "Высокочастотный")
                {
                    if (!double.TryParse(RadiusTextBox1.Text, out double radius))
                    {
                        MessageBox.Show("Некорректное значение радиуса", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    filtered = ApplyBasicFilter(_fourierTransform, filterType, radius);
                }
                else if (filterType == "Полосовой" || filterType == "Режекторный")
                {
                    if (!double.TryParse(RadiusTextBox1.Text, out double radius1) ||
                        !double.TryParse(RadiusTextBox2.Text, out double radius2))
                    {
                        MessageBox.Show("Некорректные значения радиусов", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    filtered = ApplyBandFilter(_fourierTransform, filterType, radius1, radius2);
                }
                else if (filterType == "Узкополосный полосовой" || filterType == "Узкополосный режекторный")
                {
                    if (!int.TryParse(CircleCountTextBox.Text, out int circleCount) ||
                        !double.TryParse(CircleRadiusTextBox.Text, System.Globalization.NumberStyles.Any,
                 System.Globalization.CultureInfo.InvariantCulture, out double circleRadius))
                    {
                        MessageBox.Show("Некорректные параметры фильтра", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    filtered = ApplyNarrowBandFilter(_fourierTransform, filterType, circleCount, circleRadius);
                }

                if (filtered != null)
                {
                    FourierTransformImage.Source = VisualizeSpectrum(filtered);
                    FilteredImage.Source = ComputeInverseDFT(filtered);
                    sw.Stop();
                    StatusText.Text = $"Фильтр применен за {sw.ElapsedMilliseconds} мс";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтра: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private Complex[][,] ApplyBasicFilter(Complex[][,] dft, string filterType, double radius)
        {
            int height = dft[0].GetLength(0);
            int width = dft[0].GetLength(1);
            Complex[][,] filtered = new Complex[3][,];

            int centerX = width / 2;
            int centerY = height / 2;

            for (int c = 0; c < 3; c++)
            {
                filtered[c] = new Complex[height, width];
                Array.Copy(dft[c], filtered[c], dft[c].Length);

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        double dx = x - centerX;
                        double dy = y - centerY;
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        bool keep = filterType == "Низкочастотный" ? distance <= radius : distance > radius;

                        if (!keep) filtered[c][y, x] = Complex.Zero;
                    }
                });
            }

            return filtered;
        }

        private Complex[][,] ApplyBandFilter(Complex[][,] dft, string filterType, double radius1, double radius2)
        {
            int height = dft[0].GetLength(0);
            int width = dft[0].GetLength(1);
            Complex[][,] filtered = new Complex[3][,];

            int centerX = width / 2;
            int centerY = height / 2;

            for (int c = 0; c < 3; c++)
            {
                filtered[c] = new Complex[height, width];
                Array.Copy(dft[c], filtered[c], dft[c].Length);

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        double dx = x - centerX;
                        double dy = y - centerY;
                        double distance = Math.Sqrt(dx * dx + dy * dy);

                        bool keep = filterType == "Полосовой"
                            ? (distance >= radius1 && distance <= radius2)
                            : (distance < radius1 || distance > radius2);

                        if (!keep) filtered[c][y, x] = Complex.Zero;
                    }
                });
            }

            return filtered;
        }

        private Complex[][,] ApplyNarrowBandFilter(Complex[][,] dft, string filterType, int circleCount, double circleRadius)
        {
            int height = dft[0].GetLength(0);
            int width = dft[0].GetLength(1);
            Complex[][,] filtered = new Complex[3][,];

            int centerX = width / 2;
            int centerY = height / 2;

            bool keepInMask = filterType == "Узкополосный полосовой";

            // Создаём маску
            bool[,] mask = new bool[height, width];
            double angleStep = 2 * Math.PI / circleCount;

            for (int i = 0; i < circleCount; i++)
            {
                double angle = i * angleStep;

                if (!double.TryParse(CircleDistanceTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double circleDistance))
                    return null;

                int circleX = centerX + (int)(circleDistance * Math.Cos(angle));
                int circleY = centerY + (int)(circleDistance * Math.Sin(angle));

                int r = (int)Math.Ceiling(circleRadius);

                for (int y = Math.Max(0, circleY - r); y <= Math.Min(height - 1, circleY + r); y++)
                {
                    for (int x = Math.Max(0, circleX - r); x <= Math.Min(width - 1, circleX + r); x++)
                    {
                        double dx = x - circleX;
                        double dy = y - circleY;
                        if (dx * dx + dy * dy <= circleRadius * circleRadius)
                        {
                            mask[y, x] = true;
                        }
                    }
                }
            }

            // Применяем маску
            for (int c = 0; c < 3; c++)
            {
                filtered[c] = new Complex[height, width];
                Array.Copy(dft[c], filtered[c], dft[c].Length);

                Parallel.For(0, height, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        bool keep = keepInMask ? mask[y, x] : !mask[y, x];
                        if (!keep)
                        {
                            filtered[c][y, x] = Complex.Zero;
                        }
                    }
                });
            }

            return filtered;
        }

        private Complex[][,] ComputeDFT(WriteableBitmap image)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            Complex[][,] dft = new Complex[3][,];

            // Initialize with proper centering
            for (int c = 0; c < 3; c++)
            {
                dft[c] = new Complex[height, width];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color color = GetPixelColor(image, x, y);
                        double factor = Math.Pow(-1, x + y); // Centering the spectrum
                        dft[c][y, x] = new Complex(
                            (c == 0 ? color.R : c == 1 ? color.G : color.B) * factor,
                            0);
                    }
                }
            }

            // Optimized DFT computation
            for (int c = 0; c < 3; c++)
            {
                // Row-wise
                Parallel.For(0, height, y =>
                {
                    Complex[] row = new Complex[width];
                    for (int x = 0; x < width; x++) row[x] = dft[c][y, x];
                    row = Compute1DDFT(row);
                    for (int x = 0; x < width; x++) dft[c][y, x] = row[x];
                });

                // Column-wise
                Parallel.For(0, width, x =>
                {
                    Complex[] col = new Complex[height];
                    for (int y = 0; y < height; y++) col[y] = dft[c][y, x];
                    col = Compute1DDFT(col);
                    for (int y = 0; y < height; y++) dft[c][y, x] = col[y];
                });
            }

            return dft;
        }

        private Complex[] Compute1DDFT(Complex[] input)
        {
            int N = input.Length;
            Complex[] output = new Complex[N];

            for (int k = 0; k < N; k++)
            {
                output[k] = new Complex(0, 0);
                for (int n = 0; n < N; n++)
                {
                    double angle = -2 * Math.PI * k * n / N;
                    output[k] += input[n] * new Complex(Math.Cos(angle), Math.Sin(angle));
                }
            }

            return output;
        }

        private Complex[] Compute1DIDFT(Complex[] input)
        {
            int N = input.Length;
            Complex[] output = new Complex[N];

            for (int k = 0; k < N; k++)
            {
                output[k] = new Complex(0, 0);
                for (int n = 0; n < N; n++)
                {
                    double angle = 2 * Math.PI * k * n / N;
                    output[k] += input[n] * new Complex(Math.Cos(angle), Math.Sin(angle));
                }
                output[k] = output[k] / N;
            }

            return output;
        }

        private WriteableBitmap ComputeInverseDFT(Complex[][,] dft)
        {
            int height = dft[0].GetLength(0);
            int width = dft[0].GetLength(1);
            Complex[][,] idft = new Complex[3][,];

            // Proper inverse transformation
            for (int c = 0; c < 3; c++)
            {
                idft[c] = new Complex[height, width];
                Array.Copy(dft[c], idft[c], dft[c].Length);

                // Inverse DFT row-wise
                Parallel.For(0, height, y =>
                {
                    Complex[] row = new Complex[width];
                    for (int x = 0; x < width; x++) row[x] = idft[c][y, x];
                    row = Compute1DIDFT(row);
                    for (int x = 0; x < width; x++) idft[c][y, x] = row[x];
                });

                // Inverse DFT column-wise
                Parallel.For(0, width, x =>
                {
                    Complex[] col = new Complex[height];
                    for (int y = 0; y < height; y++) col[y] = idft[c][y, x];
                    col = Compute1DIDFT(col);
                    for (int y = 0; y < height; y++) idft[c][y, x] = col[y];
                });
            }

            // Correct image restoration
            WriteableBitmap result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            byte[] pixels = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pos = (y * width + x) * 4;
                    double factor = Math.Pow(-1, x + y); // Reverse centering

                    // Normalize values
                    double r = Math.Max(0, Math.Min(255, idft[0][y, x].Real * factor));
                    double g = Math.Max(0, Math.Min(255, idft[1][y, x].Real * factor));
                    double b = Math.Max(0, Math.Min(255, idft[2][y, x].Real * factor));

                    pixels[pos] = (byte)b;
                    pixels[pos + 1] = (byte)g;
                    pixels[pos + 2] = (byte)r;
                    pixels[pos + 3] = 255;
                }
            }

            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            return result;
        }

        private WriteableBitmap VisualizeSpectrum(Complex[][,] dft)
        {
            int width = dft[0].GetLength(1);
            int height = dft[0].GetLength(0);
            WriteableBitmap spectrum = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            byte[] pixels = new byte[width * height * 4];

            // Find maximum value for normalization
            double max = 0;
            for (int c = 0; c < 3; c++)
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        max = Math.Max(max, dft[c][y, x].Magnitude);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pos = (y * width + x) * 4;
                    double val = (Math.Log(1 + (dft[0][y, x].Magnitude + dft[1][y, x].Magnitude + dft[2][y, x].Magnitude) / 3)
                                / Math.Log(1 + max)) * 255;

                    byte bval = (byte)Math.Min(255, val);
                    pixels[pos] = bval;
                    pixels[pos + 1] = bval;
                    pixels[pos + 2] = bval;
                    pixels[pos + 3] = 255;
                }
            }

            spectrum.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            return spectrum;
        }

        private Color GetPixelColor(BitmapSource bitmap, int x, int y)
        {
            if (x < 0 || y < 0 || x >= bitmap.PixelWidth || y >= bitmap.PixelHeight)
                return Colors.Black;

            var pixels = new byte[4];
            var rect = new Int32Rect(x, y, 1, 1);

            bitmap.CopyPixels(rect, pixels, 4, 0);
            return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _fourierTransform = null;
            _isFourierCalculated = false;
            OriginalImage.Source = _originalImage;
            FourierTransformImage.Source = null;
            FilteredImage.Source = null;
            FilterPreviewImage.Source = null;
            StatusText.Text = "Готово";
        }

        private void RadiusTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFourierCalculated)
            {
                ShowFilterPreview();
            }
        }
    }
}
