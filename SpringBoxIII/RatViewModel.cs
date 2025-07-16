using System.ComponentModel;
using System.Windows;

namespace SpringBoxIII
{
    class RatViewModel : INotifyPropertyChanged
    {
        private Point _From = new(0, 0);
        private Point _To = new(0, 0);
        private double _Angle = 0;
        private Duration _Duration = new TimeSpan(0, 0, 0, 0, 0);
        private Point _ImgPosition = new(0, 0);

        public Point From
        {
            get { return _From; }
            set
            {
                _From = value;
                OnPropertyChanged(nameof(From));
            }
        }
        public Point To
        {
            get { return _To; }
            set
            {
                _To = value;
                OnPropertyChanged(nameof(To));
            }
        }
        public double Angle
        {
            get { return _Angle; }
            set
            {
                _Angle = value;
                OnPropertyChanged(nameof(Angle));
            }
        }
        public Duration Duration
        {
            get { return _Duration; }
            set
            {
                _Duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }
        public Point ImgPosition
        {
            get { return _ImgPosition; }
            set
            {
                _ImgPosition = value;
                OnPropertyChanged(nameof(ImgPosition));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
