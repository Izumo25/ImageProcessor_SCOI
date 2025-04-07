using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageProcessor_SCOI
{
    public partial class GradationWindow : Window
    {
        private BitmapSource _originalImage;
        private List<Point> _curvePoints = new List<Point>();
        private bool _isDragging = false;
        private int _selectedPointIndex = -1;

        public bool IsApplied { get; private set; } = false;
        public BitmapSource ProcessedImage { get; private set; }

        public GradationWindow(BitmapSource image)
        {
            InitializeComponent();
            _originalImage = image;
            PreviewImage.Source = _originalImage;
            InitializeCurveCanvas();
            DrawDefaultCurve();
            UpdateHistogram(_originalImage);
        }

        private void InitializeCurveCanvas()
        {
            // Оси X и Y
            XAxis.Y1 = XAxis.Y2 = CurveCanvas.ActualHeight - 1;
            XAxis.X2 = CurveCanvas.ActualWidth - 1;
            YAxis.X1 = YAxis.X2 = 0;
            YAxis.Y2 = CurveCanvas.ActualHeight - 1;
        }

        private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Обновляем оси и кривую при изменении размера
            XAxis.Y1 = XAxis.Y2 = CurveCanvas.ActualHeight - 1;
            XAxis.X2 = CurveCanvas.ActualWidth - 1;
            YAxis.Y2 = CurveCanvas.ActualHeight - 1;
            UpdateCurveLine();
        }

        private void HistogramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (PreviewImage.Source != null)
                UpdateHistogram((BitmapSource)PreviewImage.Source);
        }

        private void DrawDefaultCurve()
        {
            _curvePoints.Clear();
            // Добавляем точки от (0,255) до (255,0) - теперь правильно для отображения
            //_curvePoints.Add(new Point(0, 255));
            //_curvePoints.Add(new Point(255, 0));
            _curvePoints.Add(new Point(0, 0));     // Левый нижний угол (Y=0)
            _curvePoints.Add(new Point(255, 255)); // Правый верхний угол (Y=255)
            UpdateCurveLine();
        }

        private void UpdateCurveLine()
        {
            if (_curvePoints.Count < 2) return;

            // Сглаживание кривой
            var smoothedPoints = SmoothCurve(_curvePoints, 0.5); // 0.5 - коэффициент сглаживания

            PointCollection points = new PointCollection();
            foreach (var point in _curvePoints)
            {
                // Инвертируем Y для отображения
                points.Add(new Point(
                    point.X,
                    CurveCanvas.ActualHeight - point.Y * (CurveCanvas.ActualHeight / 255)
                ));
            }
            CurveLine.Points = points;
        }

        private List<Point> SmoothCurve(List<Point> points, double tension)
        {
            if (points.Count < 3) return points;

            var result = new List<Point>();
            for (int i = 1; i < points.Count - 1; i++)
            {
                // Простое сглаживание (можно заменить на Catmull-Rom или другие сплайны)
                double x = points[i].X;
                double y = (points[i - 1].Y + points[i].Y + points[i + 1].Y) / 3;
                result.Add(new Point(x, y));
            }
            return result;
        }


        private void UpdateHistogram(BitmapSource image)
        {
            HistogramCanvas.Children.Clear();
            int[] histogram = GradationProcessor.CalculateHistogram(image);
            int maxCount = histogram.Max();
            double width = HistogramCanvas.ActualWidth / 256.0;
            double heightScale = HistogramCanvas.ActualHeight / (double)maxCount;

            for (int i = 0; i < 256; i++)
            {
                Rectangle rect = new Rectangle
                {
                    Width = width,
                    Height = histogram[i] * heightScale,
                    Fill = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Canvas.SetLeft(rect, i * width);
                Canvas.SetBottom(rect, 0);
                HistogramCanvas.Children.Add(rect);
            }
        }

        private void CurveCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(CurveCanvas);
            double x = Math.Clamp(mousePos.X, 0, 255);
            // Инвертируем Y:
            double y = Math.Clamp(255 - (mousePos.Y * 255 / CurveCanvas.ActualHeight), 0, 255);

            // Проверяем, был ли клик рядом с существующей точкой
            for (int i = 0; i < _curvePoints.Count; i++)
            {
                if (Math.Abs(_curvePoints[i].X - x) < 10 && Math.Abs(_curvePoints[i].Y - y) < 10)
                {
                    _selectedPointIndex = i;
                    _isDragging = true;
                    return;
                }
            }

            // Добавляем новую точку
            _curvePoints.Add(new Point(x, y));
            _curvePoints = _curvePoints.OrderBy(p => p.X).ToList();
            _selectedPointIndex = _curvePoints.FindIndex(p => p.X == x && p.Y == y);
            _isDragging = true;
            UpdateCurveLine();
        }

        private readonly Dictionary<string, Func<byte, byte>> _transformFunctions = new()
            {
                {"Тождественная", x => x},
                {"Логарифм", x => (byte)(255 * Math.Log(1 + x) / Math.Log(256))},
                {"Корень n-степени", x => (byte)(255 * Math.Pow(x/255.0, 1/2.5))}, // 2.5 - пример степени
                {"n-ная степень", x => (byte)(255 * Math.Pow(x/255.0, 2.5))},
                {"Обратный логарифм", x => (byte)(255 * (1 - Math.Log(1 + 255 - x) / Math.Log(256)))}
            };

        private void TransformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransformComboBox.SelectedItem is not ComboBoxItem item) return;

            string transformName = item.Content.ToString();
            if (_transformFunctions.TryGetValue(transformName, out var transform))
            {
                // Создаем дискретные точки для отображения
                _curvePoints = Enumerable.Range(0, 256)
                    .Where(x => x % 10 == 0) // Берем каждую 10-ю точку
                    .Select(x => new Point(x, transform((byte)x)))
                    .ToList();

                UpdateCurveLine();
                ApplyCurveToImage();
            }
        }

        private void CurveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point mousePos = e.GetPosition(CurveCanvas);
            double x = Math.Clamp(mousePos.X, 0, 255);
            double y = Math.Clamp(255 - mousePos.Y, 0, 255); // Инвертируем Y

            _curvePoints[_selectedPointIndex] = new Point(x, y);
            UpdateCurveLine();
        }

        private void CurveCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ApplyCurveToImage();
        }

        private void ApplyCurveToImage()
        {
            if (_originalImage == null || _curvePoints.Count < 2) return;

            Func<byte, byte> curve = CurveInterpolator.CreateCurve(_curvePoints);
            WriteableBitmap processedImage = GradationProcessor.ApplyCurve(_originalImage, curve);
            PreviewImage.Source = processedImage;
            ProcessedImage = processedImage;
            UpdateHistogram(processedImage);
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ApplyCurveToImage();
            IsApplied = true;
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JPEG Image|*.jpg|PNG Image|*.png|BMP Image|*.bmp",
                Title = "Сохранить изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = dialog.FileName.EndsWith(".png") ? new PngBitmapEncoder() :
                                      dialog.FileName.EndsWith(".bmp") ? new BmpBitmapEncoder() :
                                      new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapSource)PreviewImage.Source));

                using (var stream = dialog.OpenFile())
                {
                    encoder.Save(stream);
                }
                IsApplied = true;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            DrawDefaultCurve();
            PreviewImage.Source = _originalImage;
            ProcessedImage = _originalImage;
            UpdateHistogram(_originalImage);
        }
    }
}