using System.ComponentModel; // Необходим для INotifyPropertyChanged
using System.Runtime.CompilerServices; // Для CallerMemberName
using System.Windows.Media.Imaging;

namespace ImageProcessor_SCOI
{
    public class ImageLayer : INotifyPropertyChanged // Реализуем интерфейс
    {
        private BitmapImage _image;
        private string _blendMode = "Normal";
        private double _opacity = 1.0;

        // Событие, требующееся для INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        // Метод для вызова события (ваш OnPropertyChanged)
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //=========

        private bool _isTransparencyEnabled = false;

        private bool _redChannelEnabled = true;
        private bool _greenChannelEnabled = true;
        private bool _blueChannelEnabled = true;

        public bool IsTransparencyEnabled
        {
            get => _isTransparencyEnabled;
            set
            {
                _isTransparencyEnabled = value;
                OnPropertyChanged();
            }
        }
        public bool RedChannelEnabled
        {
            get => _redChannelEnabled;
            set { _redChannelEnabled = value; OnPropertyChanged(); }
        }

        public bool GreenChannelEnabled
        {
            get => _greenChannelEnabled;
            set { _greenChannelEnabled = value; OnPropertyChanged(); }
        }

        public bool BlueChannelEnabled
        {
            get => _blueChannelEnabled;
            set { _blueChannelEnabled = value; OnPropertyChanged(); }
        }

        //========

        public BitmapImage Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged(); // Уведомляем об изменении
            }
        }

        public string BlendMode
        {
            get => _blendMode;
            set
            {
                _blendMode = value;
                OnPropertyChanged(); // Уведомляем об изменении
            }
        }

        public double Opacity
        {
            get => _opacity;
            set
            {
                //_opacity = Math.Clamp(value, 0, 1);
                _opacity = value;
                OnPropertyChanged(); // Критично для работы Slider'а!
            }
        }

        public List<string> BlendModes { get; } = new List<string>
        {
            "Normal",
            "Add",
            "Multiply",
            "Average",
            "Max",
            "Min"
        };


    }
}




//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using System.Windows.Media.Imaging;

//namespace ImageProcessor_SCOI
//{
//    public class ImageLayer : INotifyPropertyChanged
//    {
//        private BitmapImage _image;
//        private string _blendMode = "Normal";
//        private double _opacity = 1.0;

//        public BitmapImage Image
//        {
//            get => _image;
//            set { _image = value; OnPropertyChanged(); }
//        }

//        public string BlendMode
//        {
//            get => _blendMode;
//            set { _blendMode = value; OnPropertyChanged(); }
//        }

//        public double Opacity
//        {
//            get => _opacity;
//            set { _opacity = value; OnPropertyChanged(); }
//        }

//        public List<string> BlendModes { get; } = new List<string>
//        {
//            "Normal",
//            "Add",
//            "Multiply",
//            "Average",
//            "Max",
//            "Min"
//        };

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }
//}

