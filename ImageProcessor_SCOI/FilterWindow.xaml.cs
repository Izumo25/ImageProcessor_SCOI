using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public partial class SpatialWindow : Window, INotifyPropertyChanged
    {

        public class KernelRow : INotifyPropertyChanged
        {
            public int RowNumber { get; set; }
            public ObservableCollection<KernelCell> Cells { get; } = new ObservableCollection<KernelCell>();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class KernelCell : INotifyPropertyChanged
        {
            private float _value;
            public float Value
            {
                get => _value;
                set
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(FormattedValue));
                }
            }
            public int X { get; set; }
            public int Y { get; set; }
            public string FormattedValue => Value.ToString("F3", CultureInfo.InvariantCulture);

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitmapSource _originalImage;
        private WriteableBitmap _processedImage;
        private ObservableCollection<KernelRow> _kernelRows = new ObservableCollection<KernelRow>();
        private bool _isUpdatingKernel = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<KernelRow> KernelRows
        {
            get => _kernelRows;
            set
            {
                _kernelRows = value;
                OnPropertyChanged(nameof(KernelRows));
            }
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void InitializeDataGridColumns()
        {
            KernelMatrixEditor.Columns.Clear();
            KernelMatrixEditor.RowHeight = 30;

            // Добавляем столбец с номерами строк
            KernelMatrixEditor.Columns.Add(new DataGridTextColumn
            {
                Header = "Row",
                Binding = new Binding("RowNumber"),
                Width = 40,
                IsReadOnly = true,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center) }
                }
            });
        }

        private void AddDataGridColumns(int width)
        {
            // Очищаем существующие столбцы (кроме первого с номерами строк)
            while (KernelMatrixEditor.Columns.Count > 1)
            {
                KernelMatrixEditor.Columns.RemoveAt(1);
            }

            // Добавляем столбцы для каждого столбца матрицы
            for (int x = 0; x < width; x++)
            {
                KernelMatrixEditor.Columns.Add(new DataGridTextColumn
                {
                    Header = $"Col {x}",
                    Binding = new Binding($"Cells[{x}].Value"),
                    Width = 60,
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters =
                        {
                            new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                            new Setter(TextBlock.FontFamilyProperty, new FontFamily("Consolas"))
                        }
                    },
                    EditingElementStyle = new Style(typeof(TextBox))
                    {
                        Setters =
                        {
                            new Setter(TextBox.TextAlignmentProperty, TextAlignment.Center)
                        }
                    }
                });
            }
        }

        private void FilterTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = FilterTypeComboBox.SelectedIndex;

            SigmaPanel.Visibility = (selectedIndex == 1) ? Visibility.Visible : Visibility.Collapsed;

            // Если выбран "Своя матрица" (индекс 5) — разрешаем редактировать
            KernelMatrixEditor.IsReadOnly = (selectedIndex != 3);

            // Если выбран медианный фильтр (индекс 2) — прячем матрицу
            if (selectedIndex == 2)
            {
                KernelMatrixEditor.Visibility = Visibility.Collapsed;
            }
            else
            {
                KernelMatrixEditor.Visibility = Visibility.Visible;
            }

            // Show/hide kernel buttons for median filter (index 2)
            //KernelButtonsPanel.Visibility = (FilterTypeComboBox.SelectedIndex == 2) ? Visibility.Collapsed : Visibility.Visible;


            if (selectedIndex != 3 && !_isUpdatingKernel)
            {
                UpdateKernelPreview();
            }
        }


        // Удаляем метод UpdateKernelSize_Click полностью

        // Модифицируем метод KernelSize_TextChanged
        private void KernelSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && !_isUpdatingKernel)
            {
                if (int.TryParse(KernelWidthBox.Text, out int width) &&
                    int.TryParse(KernelHeightBox.Text, out int height) &&
                    width > 0 && height > 0)
                {
                    // Проверяем нечетность размеров
                    if (width % 2 == 0 || height % 2 == 0)
                    {
                        MessageBox.Show("Размеры матрицы должны быть нечетными числами");
                        return;
                    }

                    const int maxSize = 15;
                    if (width < 1 || height < 1 || width > maxSize || height > maxSize)
                    {
                        MessageBox.Show($"Размеры матрицы должны быть в диапазоне от 1 до {maxSize}");
                        return;
                    }

                    UpdateKernelSize();
                }
            }
        }

        // В конструкторе удаляем привязку к кнопке, если она была
        public SpatialWindow(BitmapSource image)
        {
            InitializeComponent();
            DataContext = this;
            _originalImage = image;
            PreviewImage.Source = _originalImage;

            // Устанавливаем значения по умолчанию для размеров ядра
            KernelWidthBox.Text = "3";  // Добавьте эти строки
            KernelHeightBox.Text = "3"; // для явного задания размеров

            // Инициализация событий
            FilterTypeComboBox.SelectionChanged += FilterTypeComboBox_SelectionChanged;
            KernelWidthBox.TextChanged += KernelSize_TextChanged;
            KernelHeightBox.TextChanged += KernelSize_TextChanged;
            SigmaSlider.ValueChanged += SigmaSlider_ValueChanged;

            SigmaPanel.Visibility = Visibility.Collapsed;

            // Инициализация ядра
            InitializeDataGridColumns();
            UpdateKernelSize(); // Это создаст начальную матрицу 3x3
        }

        private void SigmaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded && FilterTypeComboBox.SelectedIndex == 1 && !_isUpdatingKernel)
            {
                UpdateKernelPreview();
            }
        }

        private void UpdateKernelSize()
        {
            if (int.TryParse(KernelWidthBox.Text, out int width) &&
                int.TryParse(KernelHeightBox.Text, out int height) &&
                width > 0 && height > 0)
            {
                _isUpdatingKernel = true;

                // Обновляем столбцы DataGrid
                AddDataGridColumns(width);

                // Создаем новые строки ядра
                KernelRows.Clear();
                for (int y = 0; y < height; y++)
                {
                    var row = new KernelRow { RowNumber = y };
                    for (int x = 0; x < width; x++)
                    {
                        row.Cells.Add(new KernelCell { X = x, Y = y, Value = 0 });
                    }
                    KernelRows.Add(row);
                }

                // Автонастройка высоты строк
                KernelMatrixEditor.RowHeight = height > 10 ? 25 : 30;

                _isUpdatingKernel = false;
                UpdateKernelPreview();
            }
        }

        private void UpdateKernelPreview()
        {
            if (FilterTypeComboBox.SelectedIndex == 4 || _isUpdatingKernel) return;

            float[,] kernel = GenerateKernel();
            if (kernel != null)
            {
                UpdateKernelCellsFromArray(kernel);
            }
        }

        private void UpdateKernelCellsFromArray(float[,] kernel)
        {
            _isUpdatingKernel = true;

            int width = kernel.GetLength(0);
            int height = kernel.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y < KernelRows.Count && x < KernelRows[y].Cells.Count)
                    {
                        KernelRows[y].Cells[x].Value = kernel[x, y];
                    }
                }
            }

            _isUpdatingKernel = false;
        }

        private float[,] GetKernelFromCells()
        {
            if (!KernelRows.Any() || !KernelRows[0].Cells.Any()) return null;

            int width = KernelRows[0].Cells.Count;
            int height = KernelRows.Count;
            float[,] kernel = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    kernel[x, y] = KernelRows[y].Cells[x].Value;
                }
            }

            return kernel;
        }

        private float[,] GenerateKernel()
        {
            int kw = int.Parse(KernelWidthBox.Text);
            int kh = int.Parse(KernelHeightBox.Text);

            switch (FilterTypeComboBox.SelectedIndex)
            {
                case 0: return GenerateBoxKernel(kw, kh);           // Размытие
                case 1: return GenerateGaussianKernel(kw, kh, SigmaSlider.Value); // Гаусс
                case 3: return GetKernelFromCells();                 // Своя матрица
                default: return null;
            }
        }


        private float[,] GenerateBoxKernel(int kw, int kh)
        {
            float[,] kernel = new float[kw, kh];
            float value = 1.0f / (kw * kh);
            for (int i = 0; i < kw; i++)
                for (int j = 0; j < kh; j++)
                    kernel[i, j] = value;
            return kernel;
        }

        private float[,] GenerateGaussianKernel(int kw, int kh, double sigma)
        {
            float[,] kernel = new float[kw, kh];
            double sum = 0;
            int halfKw = kw / 2;
            int halfKh = kh / 2;

            for (int x = -halfKw; x <= halfKw; x++)
                for (int y = -halfKh; y <= halfKh; y++)
                {
                    double exponent = -(x * x + y * y) / (2 * sigma * sigma);
                    kernel[x + halfKw, y + halfKh] = (float)(Math.Exp(exponent) / (2 * Math.PI * sigma * sigma));
                    sum += kernel[x + halfKw, y + halfKh];
                }

            // Нормализация
            for (int i = 0; i < kw; i++)
                for (int j = 0; j < kh; j++)
                    kernel[i, j] /= (float)sum;

            return kernel;
        }

        private void KernelMatrixEditor_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit &&
                FilterTypeComboBox.SelectedIndex == 4)
            {
                ApplyCustomKernel();
            }
        }

        //private void NormalizeKernel_Click(object sender, RoutedEventArgs e)
        //{
        //    var kernel = GetKernelFromCells();
        //    if (kernel == null) return;

        //    float sum = kernel.Cast<float>().Sum();

        //    if (Math.Abs(sum) > 0.0001f)
        //    {
        //        for (int i = 0; i < kernel.GetLength(0); i++)
        //        {
        //            for (int j = 0; j < kernel.GetLength(1); j++)
        //            {
        //                kernel[i, j] /= sum;
        //            }
        //        }
        //        UpdateKernelCellsFromArray(kernel);
        //        ApplyCustomKernel();
        //    }
        //}

        //private void ResetKernel_Click(object sender, RoutedEventArgs e)
        //{
        //    UpdateKernelPreview();
        //    if (FilterTypeComboBox.SelectedIndex == 4)
        //    {
        //        ApplyCustomKernel();
        //    }
        //}

        private void ApplyCustomKernel()
        {
            var kernel = GetKernelFromCells();
            if (kernel != null)
            {
                ApplyLinearFilter(kernel);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(KernelWidthBox.Text, out int kw) || !int.TryParse(KernelHeightBox.Text, out int kh))
            {
                MessageBox.Show("Пожалуйста, введите корректные размеры матрицы");
                return;
            }

            if (kw % 2 == 0 || kh % 2 == 0)
            {
                MessageBox.Show("Размеры матрицы должны быть нечетными числами");
                return;
            }

            try
            {
                switch (FilterTypeComboBox.SelectedIndex)
                {
                    case 0: // Box Blur
                    case 1: // Gaussian Blur
                    case 3: // Custom
                        var kernel = GenerateKernel();
                        if (kernel != null)
                            ApplyLinearFilter(kernel);
                        else
                            MessageBox.Show("Ошибка при генерации ядра.");
                        break;

                    case 2: // Median Filter
                        ApplyMedianFilter(kw, kh);
                        break;

                    default:
                        MessageBox.Show("Неизвестный фильтр.");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке изображения: {ex.Message}");
            }
        }


        //private void ApplyLinearFilter(float[,] kernel)
        //{
        //    int kw = kernel.GetLength(0);
        //    int kh = kernel.GetLength(1);
        //    int padX = kw / 2;
        //    int padY = kh / 2;

        //    var source = new FormatConvertedBitmap(_originalImage, PixelFormats.Pbgra32, null, 0);
        //    _processedImage = new WriteableBitmap(source);

        //    int stride = _processedImage.BackBufferStride;
        //    int bytesPerPixel = (_processedImage.Format.BitsPerPixel + 7) / 8;
        //    int width = _processedImage.PixelWidth;
        //    int height = _processedImage.PixelHeight;

        //    byte[] pixels = new byte[stride * height];
        //    _processedImage.CopyPixels(pixels, stride, 0);

        //    // Временные массивы для хранения результатов свёртки
        //    float[] red = new float[width * height];
        //    float[] green = new float[width * height];
        //    float[] blue = new float[width * height];

        //    // Применяем свёртку
        //    for (int y = padY; y < height - padY; y++)
        //    {
        //        for (int x = padX; x < width - padX; x++)
        //        {
        //            float r = 0, g = 0, b = 0;

        //            for (int ky = 0; ky < kh; ky++)
        //            {
        //                for (int kx = 0; kx < kw; kx++)
        //                {
        //                    int px = x + kx - padX;
        //                    int py = y + ky - padY;
        //                    int idx = py * stride + px * bytesPerPixel;

        //                    float weight = kernel[kx, ky];
        //                    b += pixels[idx] * weight;
        //                    g += pixels[idx + 1] * weight;
        //                    r += pixels[idx + 2] * weight;
        //                }
        //            }

        //            int pos = y * width + x;
        //            blue[pos] = b;
        //            green[pos] = g;
        //            red[pos] = r;
        //        }
        //    }

        //    // Находим абсолютные значения и максимальное значение
        //    float maxMagnitude = 0;
        //    for (int i = 0; i < red.Length; i++)
        //    {
        //        red[i] = Math.Abs(red[i]);
        //        green[i] = Math.Abs(green[i]);
        //        blue[i] = Math.Abs(blue[i]);

        //        float currentMax = Math.Max(red[i], Math.Max(green[i], blue[i]));
        //        if (currentMax > maxMagnitude)
        //            maxMagnitude = currentMax;
        //    }

        //    // Нормализуем и преобразуем в байты
        //    byte[] result = new byte[pixels.Length];
        //    float scale = maxMagnitude > 0 ? 255f / maxMagnitude : 0;

        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            int srcPos = y * width + x;
        //            int dstPos = y * stride + x * bytesPerPixel;

        //            result[dstPos] = (byte)(blue[srcPos] * scale);      // B
        //            result[dstPos + 1] = (byte)(green[srcPos] * scale); // G
        //            result[dstPos + 2] = (byte)(red[srcPos] * scale);   // R
        //            result[dstPos + 3] = 255;                           // A (непрозрачность)
        //        }
        //    }

        //    _processedImage.WritePixels(
        //        new Int32Rect(0, 0, width, height),
        //        result,
        //        stride,
        //        0);

        //    PreviewImage.Source = _processedImage;
        //}

        private void ApplyLinearFilter(float[,] kernel)
        {
            int kw = kernel.GetLength(0);
            int kh = kernel.GetLength(1);
            int padX = kw / 2;
            int padY = kh / 2;

            var source = new FormatConvertedBitmap(_originalImage, PixelFormats.Pbgra32, null, 0);
            _processedImage = new WriteableBitmap(source);

            int stride = _processedImage.BackBufferStride;
            int bytesPerPixel = (_processedImage.Format.BitsPerPixel + 7) / 8;
            int width = _processedImage.PixelWidth;
            int height = _processedImage.PixelHeight;

            byte[] pixels = new byte[stride * height];
            _processedImage.CopyPixels(pixels, stride, 0);

            // Временные массивы для хранения результатов свёртки
            float[] red = new float[width * height];
            float[] green = new float[width * height];
            float[] blue = new float[width * height];

            // Применяем свёртку
            for (int y = padY; y < height - padY; y++)
            {
                for (int x = padX; x < width - padX; x++)
                {
                    float r = 0, g = 0, b = 0;

                    for (int ky = 0; ky < kh; ky++)
                    {
                        for (int kx = 0; kx < kw; kx++)
                        {
                            int px = x + kx - padX;
                            int py = y + ky - padY;
                            int idx = py * stride + px * bytesPerPixel;

                            float weight = kernel[kx, ky];
                            b += pixels[idx] * weight;
                            g += pixels[idx + 1] * weight;
                            r += pixels[idx + 2] * weight;
                        }
                    }

                    int pos = y * width + x;
                    blue[pos] = b;
                    green[pos] = g;
                    red[pos] = r;
                }
            }

            // Находим абсолютные значения и максимальное значение
            float maxMagnitude = 0;
            for (int i = 0; i < red.Length; i++)
            {
                red[i] = Math.Abs(red[i]);
                green[i] = Math.Abs(green[i]);
                blue[i] = Math.Abs(blue[i]);

                float currentMax = Math.Max(red[i], Math.Max(green[i], blue[i]));
                if (currentMax > maxMagnitude)
                    maxMagnitude = currentMax;
            }

            // Нормализуем и преобразуем в байты
            byte[] result = new byte[pixels.Length];
            float scale = maxMagnitude > 0 ? 255f / maxMagnitude : 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcPos = y * width + x;
                    int dstPos = y * stride + x * bytesPerPixel;

                    result[dstPos] = (byte)(blue[srcPos] * scale);      // B
                    result[dstPos + 1] = (byte)(green[srcPos] * scale); // G
                    result[dstPos + 2] = (byte)(red[srcPos] * scale);  // R
                    result[dstPos + 3] = 255;                           // A (непрозрачность)
                }
            }

            _processedImage.WritePixels(
                new Int32Rect(0, 0, width, height),
                result,
                stride,
                0);

            PreviewImage.Source = _processedImage;
        }


        private void ApplyMedianFilter(int kernelWidth, int kernelHeight)
        {
            if (_originalImage.Format != PixelFormats.Pbgra32)
            {
                _originalImage = new FormatConvertedBitmap(_originalImage, PixelFormats.Pbgra32, null, 0);
            }

            int padX = kernelWidth / 2;
            int padY = kernelHeight / 2;

            _processedImage = new WriteableBitmap(_originalImage);
            int width = _processedImage.PixelWidth;
            int height = _processedImage.PixelHeight;

            int bytesPerPixel = 4;
            int stride = width * bytesPerPixel;

            byte[] sourcePixels = new byte[height * stride];
            byte[] resultPixels = new byte[height * stride];

            _processedImage.CopyPixels(sourcePixels, stride, 0);

            byte[] windowR = new byte[kernelWidth * kernelHeight];
            byte[] windowG = new byte[kernelWidth * kernelHeight];
            byte[] windowB = new byte[kernelWidth * kernelHeight];

            for (int y = padY; y < height - padY; y++)
            {
                for (int x = padX; x < width - padX; x++)
                {
                    int count = 0;

                    for (int ky = -padY; ky <= padY; ky++)
                    {
                        for (int kx = -padX; kx <= padX; kx++)
                        {
                            int sampleX = x + kx;
                            int sampleY = y + ky;
                            int index = (sampleY * width + sampleX) * bytesPerPixel;

                            windowB[count] = sourcePixels[index];
                            windowG[count] = sourcePixels[index + 1];
                            windowR[count] = sourcePixels[index + 2];
                            count++;
                        }
                    }

                    Array.Sort(windowR, 0, count);
                    Array.Sort(windowG, 0, count);
                    Array.Sort(windowB, 0, count);
                    int median = count / 2;

                    int targetIndex = (y * width + x) * bytesPerPixel;
                    resultPixels[targetIndex] = windowB[median];
                    resultPixels[targetIndex + 1] = windowG[median];
                    resultPixels[targetIndex + 2] = windowR[median];
                    resultPixels[targetIndex + 3] = 255; // полностью непрозрачный
                }
            }

            _processedImage.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, stride, 0);
            PreviewImage.Source = _processedImage;
        }

    }
}