using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MeasureDeflection.Utils
{
    /// <summary>
    /// Color Class with bindable color fields
    /// </summary>
    public class SmartColor : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Automated PropertyChanged Methode:
        /// Calling Member is determined automatically by CallerMemberName-Property
        /// </summary>
        /// <param name="propertyName"></param>
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            System.ComponentModel.PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private byte _r;
        /// <summary>
        /// Red 
        /// </summary>
        public byte R
        {
            get { return _r; }
            set
            {
                _r = value;
                OnPropertyChanged();
                PickedColor = new SolidColorBrush(Color.FromRgb(R, G, B));
            }
        }

        private byte _g;
        /// <summary>
        /// Green
        /// </summary>
        public byte G
        {
            get { return _g; }
            set
            {
                _g = value; OnPropertyChanged();
                PickedColor = new SolidColorBrush(Color.FromRgb(R, G, B));
            }
        }

        private byte _b;
        /// <summary>
        /// Blue
        /// </summary>
        public byte B
        {
            get { return _b; }
            set
            {
                _b = value; OnPropertyChanged();
                PickedColor = new SolidColorBrush(Color.FromRgb(R, G, B));
            }
        }

        /// <summary>
        /// Set color from Color object
        /// </summary>
        /// <param name="newColor"></param>
        public void SetColor(Color newColor)
        {
            R = newColor.R;
            G = newColor.G;
            B = newColor.B;
        }

        private Brush _pickedColor;
        /// <summary>
        /// Brush color
        /// </summary>
        public Brush PickedColor
        {
            get { return _pickedColor; }
            set { _pickedColor = value; OnPropertyChanged(); }
        }
    }
}
