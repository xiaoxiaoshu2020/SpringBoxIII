﻿using System;
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
        private Duration _Duration = new TimeSpan(0, 0, 0, 0, 0);
        private System.Windows.Point _point;

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
        public Duration Duration
        {
            get { return _Duration; }
            set
            {
                _Duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }
        public System.Windows.Point point
        {
            get { return _point; }
            set
            {
                _point = value;
                OnPropertyChanged(nameof(point));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
