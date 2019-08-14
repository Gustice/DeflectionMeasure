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
    /// User Prompts
    /// Combination of caption and detailed description optionally with predefined color sceme
    /// </summary>
    public class UserPrompt : INotifyPropertyChanged
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

        /// <summary>
        /// Delegate prototype as handle reference
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        public delegate void PromptNewMessage(eNotifyType type, string message);

        /// <summary>
        /// Assosiated colors for notification types
        /// </summary>
        public Dictionary<eNotifyType, Brush> PromptColors = new Dictionary<eNotifyType, Brush> {
            {eNotifyType.Neutral, Brushes.Black},
            {eNotifyType.Success, Brushes.DarkGreen},
            {eNotifyType.Note, Brushes.DarkBlue},
            {eNotifyType.Warning, Brushes.DarkOrange},
            {eNotifyType.Error, Brushes.DarkRed},
        };

        /// <summary>
        /// Constructor. 
        /// </summary>
        public UserPrompt()
        {
            PromptColor = PromptColors[eNotifyType.Neutral];
        }

        private string _caption;
        /// <summary>
        /// Caption of status message
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { _caption = value; OnPropertyChanged(); }
        }

        private string _promptMessage;
        /// <summary>
        /// Prompted status message
        /// </summary>
        public string PromptMessage
        {
            get { return _promptMessage; }
            set { _promptMessage = value; OnPropertyChanged(); }
        }

        private Brush _promptColor;
        /// <summary>
        /// Color of status message
        /// </summary>
        public Brush PromptColor
        {
            get { return _promptColor; }
            set { _promptColor = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Notification types
        /// </summary>
        public enum eNotifyType
        {
            Neutral,
            Success,
            Note,
            Warning,
            Error
        }
    }
}
