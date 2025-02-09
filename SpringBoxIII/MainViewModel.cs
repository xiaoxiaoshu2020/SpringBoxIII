using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows;

namespace SpringBoxIII
{
    class MainViewModel : INotifyPropertyChanged
    {
        private System.Windows.Point _From = new(0, 0);
        private System.Windows.Point _To = new(0, 0);
        private double _Angle = 0;
        private Duration _DurationX = new TimeSpan(0, 0, 0);
        private Duration _DurationY = new TimeSpan(0, 0, 0);

        public System.Windows.Point From
        {
            get { return _From; }
            set
            {
                _From = value;
                OnPropertyChanged(nameof(From));
            }
        }
        public System.Windows.Point To
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
        public Duration DurationX
        {
            get { return _DurationX; }
            set
            {
                _DurationX = value;
                OnPropertyChanged(nameof(DurationX));
            }
        }
        public Duration DurationY
        {
            get { return _DurationY; }
            set
            {
                _DurationY = value;
                OnPropertyChanged(nameof(DurationY));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
