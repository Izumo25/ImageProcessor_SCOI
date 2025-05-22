using ImageProcess;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            Layers.CollectionChanged += Layers_CollectionChanged; // Подписка на изменение коллекции
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

                    Process_Click(null, null);
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

                // Проверяем, что выбран реальный метод (не первый пустой элемент)
                if (MethodComboBox.SelectedItem is ComboBoxItem selectedItem &&
                    selectedItem.Content.ToString() != "Выберите метод бинаризации" &&
                    PreviewImage.Source is BitmapSource currentImage)
                {
                    var originalImage = Layers[0].Image;
                    if (originalImage == null) return;

                    // Проверка бинаризации
                    bool applyBinarization = MethodComboBox.SelectedItem != null &&
                                          ((ComboBoxItem)MethodComboBox.SelectedItem).Content.ToString() != "Выберите метод";

                    if (applyBinarization)
                    {
                        // Применяем бинаризацию к оригиналу
                        string method = ((ComboBoxItem)MethodComboBox.SelectedItem).Content.ToString();
                        double sensitivity = SensitivitySlider.Value;
                        var binarizedImage = BinarizationMethods.ApplyBinarization(originalImage, method, sensitivity);

                        // Масштабируем для отображения
                        PreviewImage.Source = ScaleImageToFit(binarizedImage, PreviewImage.ActualWidth, PreviewImage.ActualHeight);
                        return;
                    }
                }



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

        private BitmapSource ScaleImageToFit(BitmapSource source, double maxWidth, double maxHeight)
        {
            if (source == null) return null;

            double scaleX = maxWidth / source.PixelWidth;
            double scaleY = maxHeight / source.PixelHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Если изображение меньше панели - не масштабируем
            if (scale >= 1.0) return source;

            var scaledBitmap = new TransformedBitmap(
                source,
                new ScaleTransform(scale, scale));

            return scaledBitmap;
        }

        private static BitmapImage ConvertToBitmapImage(BitmapSource source)
        {
            var bitmapImage = new BitmapImage();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = new System.IO.MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }

            return bitmapImage;
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

        private void OpenFilters_Click(object sender, RoutedEventArgs e)
        {
            if (Layers.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одно изображение");
                return;
            }

            if (PreviewImage.Source is BitmapSource image)
            {
                // Блокируем главное окно
                Mouse.OverrideCursor = Cursors.Wait;
                this.IsEnabled = false;

                try
                {
                    var SpatialWindow = new SpatialWindow(image)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    // Восстанавливаем состояние после закрытия
                    SpatialWindow.Closed += (s, args) =>
                    {
                        Mouse.OverrideCursor = null;
                        this.IsEnabled = true;
                        this.Activate();
                    };

                    SpatialWindow.ShowDialog();
                }
                catch
                {
                    // На случай ошибки при открытии окна
                    Mouse.OverrideCursor = null;
                    this.IsEnabled = true;
                }
            }
        }




        private void FurieWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (Layers.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одно изображение");
                return;
            }

            if (PreviewImage.Source is BitmapSource image)
            {
                // Блокируем главное окно
                Mouse.OverrideCursor = Cursors.Wait;
                this.IsEnabled = false;

                try
                {
                    var window = new FurieWindow(image)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    // Восстанавливаем состояние после закрытия
                    window.Closed += (s, args) =>
                    {
                        Mouse.OverrideCursor = null;
                        this.IsEnabled = true;
                        this.Activate();
                    };

                    window.ShowDialog();
                }
                catch
                {
                    // На случай ошибки при открытии окна
                    Mouse.OverrideCursor = null;
                    this.IsEnabled = true;
                }
            }
        }


        // Обработчик изменения коллекции слоев
        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            FiltersButton.IsEnabled = Layers.Count > 0;
        }

        private void OpenGradationWindow_Click(object sender, RoutedEventArgs e)
        {
            if (Layers.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одно изображение");
                return;
            }

            if (PreviewImage.Source == null) return;
            var window = new GradationWindow((BitmapSource)PreviewImage.Source);
            window.ShowDialog();
        }
    }
}
