using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<ImageLayer> Layers { get; } = new ObservableCollection<ImageLayer>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.bmp" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    Layers.Add(new ImageLayer
                    {
                        Image = bitmap,
                        Opacity = 1.0
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            if (Layers.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одно изображение");
                return;
            }

            try
            {
                // 1. Создаем прозрачный холст размером с первое изображение
                var firstLayer = Layers[0];
                var result = new WriteableBitmap(
                    firstLayer.Image.PixelWidth,
                    firstLayer.Image.PixelHeight,
                    firstLayer.Image.DpiX,
                    firstLayer.Image.DpiY,
                    PixelFormats.Pbgra32,
                    null
                );

                // 2. Инициализируем прозрачный холст
                byte[] transparentPixels = new byte[firstLayer.Image.PixelWidth * firstLayer.Image.PixelHeight * 4];
                result.WritePixels(
                    new Int32Rect(0, 0, firstLayer.Image.PixelWidth, firstLayer.Image.PixelHeight),
                    transparentPixels,
                    firstLayer.Image.PixelWidth * 4,
                    0
                );

                // 3. Обрабатываем все слои
                for (int i = 0; i < Layers.Count; i++)
                {
                    var layer = Layers[i];
                    if (layer.Image == null) continue;

                    // 4. Создаем копию изображения с применением масок каналов
                    var overlay = PixelOperations.ApplyChannelMask(
                        new WriteableBitmap(layer.Image),
                        layer.RedChannelEnabled,
                        layer.GreenChannelEnabled,
                        layer.BlueChannelEnabled);

                    // 5. Для первого слоя с отключенной прозрачностью - просто копируем
                    if (i == 0 && !layer.IsTransparencyEnabled)
                    {
                        byte[] layerPixels = new byte[overlay.PixelWidth * overlay.PixelHeight * 4];
                        overlay.CopyPixels(layerPixels, overlay.PixelWidth * 4, 0);
                        result.WritePixels(
                            new Int32Rect(0, 0, overlay.PixelWidth, overlay.PixelHeight),
                            layerPixels,
                            overlay.PixelWidth * 4,
                            0
                        );
                    }
                    else
                    {
                        // 6. Применяем blending с учетом прозрачности
                        result = PixelOperations.ApplyBlend(
                            result,
                            overlay,
                            layer.BlendMode,
                            layer.Opacity,
                            layer.IsTransparencyEnabled
                        );
                    }
                }

                PreviewImage.Source = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        // Вспомогательный метод для применения прозрачности к пикселям
        private void ApplyOpacityToPixels(byte[] pixels, double opacity)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 3] = (byte)(pixels[i + 3] * opacity); // Альфа-канал
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ImageLayer layer })
            {
                int index = Layers.IndexOf(layer);
                if (index > 0) Layers.Move(index, index - 1);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ImageLayer layer })
            {
                int index = Layers.IndexOf(layer);
                if (index < Layers.Count - 1) Layers.Move(index, index + 1);
            }
        }

        private void RemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ImageLayer layer })
            {
                Layers.Remove(layer);
            }
        }

        //private void ApplyCurve_Click(object sender, RoutedEventArgs e)
        //{
        //    // Проверяем, есть ли изображение для обработки
        //    if (PreviewImage.Source == null || !(PreviewImage.Source is BitmapSource originalImage))
        //    {
        //        MessageBox.Show("Сначала загрузите изображение", "Ошибка",
        //                       MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }

        //    try
        //    {
        //        // 1. Получаем функцию преобразования из редактора кривых
        //        Func<byte, byte> curveFunction = _curveEditor.GetCurveFunction();

        //        // 2. Создаем WriteableBitmap из исходного изображения
        //        WriteableBitmap sourceBitmap = new WriteableBitmap(originalImage);

        //        // 3. Применяем кривую к изображению
        //        WriteableBitmap processedBitmap = GradationFunction.ApplyCurve(sourceBitmap, curveFunction);

        //        // 4. Обновляем превью
        //        PreviewImage.Source = processedBitmap;

        //        // 5. Обновляем гистограмму (если Canvas есть в разметке)
        //        if (HistogramCanvas != null)
        //        {
        //            HistogramBuilder.DrawHistogram(processedBitmap, HistogramCanvas);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка при применении кривой: {ex.Message}", "Ошибка",
        //                       MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}


    }
}


