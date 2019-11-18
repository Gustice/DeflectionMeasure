using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Win32;

using MeasureDeflection.Utils;
using System.Reflection;


// Known issues:
// Color Radius plausiblistaion

namespace MeasureDeflection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowView : Window, INotifyPropertyChanged
    {

        private readonly MainWindowViewModel _mvvm;

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

        public MainWindowView(MainWindowViewModel mvvm)
        {
            _mvvm = mvvm;
            DataContext = mvvm;

            InitializeComponent();

            _mvvm.LoadAvailableVideoSources();
            cbx_cams.SelectedIndex = 0;

            StartStopButton.Content = "Start";
            StartStopButton.Background = positiveButton;
        }

        // Some predefined colors
        Brush positiveButton = Brushes.LightGreen;
        Brush passiveButton = Brushes.LightGray;
        Brush negativeButton = Brushes.LightSalmon;


        #region GUI_UserEvents
        /// <summary>
        /// ComboBox is opend.
        /// This triggers scan for available image caputre devices.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cbx_cams_DropDownOpened(object sender, EventArgs e)
        {
            _mvvm.LoadAvailableVideoSources();
        }

        /// <summary>
        /// Mouse moves over Image event.
        /// Intendet to pick image colors if picker mode is active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void camStreamMouseMove(object sender, MouseEventArgs e)
        {
            _mvvm.GetPositionAndColorInPreview(Mouse.GetPosition(img_CamStream), img_CamStream);
        }

        /// <summary>
        /// Button click event for color picking. The hit button is supposed to become green.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CoulorPicker_Click(object sender, RoutedEventArgs e)
        {
            img_CamStream.MouseMove += new MouseEventHandler(camStreamMouseMove);

            Button btn = (sender as Button);
            switch (btn.Name)
            {
                case "SetAnchor":
                    SetAnchor.Background = positiveButton;
                    break;

                case "SetTip":
                    SetTip.Background = positiveButton;
                    break;

                default:
                    throw new Exception($"Click event source {btn.Name} was not considered hier");
                    break;
            }
        }

        /// <summary>
        /// Mouse click-event during color picker mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Img_CamStream_MouseDown(object sender, MouseButtonEventArgs e)
        {
            img_CamStream.MouseMove -= new MouseEventHandler(camStreamMouseMove);
            _mvvm.SetColorFromPositionInPreview(Mouse.GetPosition(img_CamStream), img_CamStream);
            SetAnchor.Background = passiveButton;
            SetTip.Background = passiveButton;
        }

        /// <summary>
        /// Starts-button was pressed.
        /// Depending on current mode either the capture should be started or stopped.
        /// Current mode is shown properly by naming the action on pressing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_mvvm.IsRunning)
            {
                // This click starts catpure mode
                _mvvm.IsRunning = true;

                 _mvvm.ImageProcessing_Command.Execute("Start");

                // Next click will stop capture mode
                StartStopButton.Content = "Stop";
                StartStopButton.Background= negativeButton;
            }
            else
            {
                // This click stops catpure mode
                _mvvm.IsRunning = false;

                _mvvm.ImageProcessing_Command.Execute("Stop");

                // Next click will start capture mode
                StartStopButton.Content = "Start";
                StartStopButton.Background = positiveButton;
            }
        }
        #endregion

    }
}