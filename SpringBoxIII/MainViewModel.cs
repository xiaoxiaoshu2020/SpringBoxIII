using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpringBoxIII
{
    class MainViewModel : INotifyPropertyChanged
    {
        private Point _From = new(0,0);
        private Point _To = new(0, 0);

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

        public  event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
