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


// Known issues:
// Color Radius plausiblistaion

namespace MeasureDeflection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Preferences

        // Default Folder for loading and saving images
        const string defaultPath = @"D:\Arbeitsordner\CS\DeflectionMeasure\Files";
        const int colorPickerRadius = 4;
        const double startToleranceFactor = 3.0;
        const int skipFramesInContiunousMode = 15; // Results in roughly 2 Frames per Second
        const int skipFramesInOnShotMode = 15; // Results in roughly 2 Frames per Second
        #endregion

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


        #region GUI_Interface
        private BitmapImage _camImage;
        /// <summary>
        /// Camera-Image
        /// </summary>
        public BitmapImage CamImage
        {
            get { return _camImage; }
            set { _camImage = value; OnPropertyChanged(); }
        }

        private ImageSource _processedImage;
        /// <summary>
        /// Processed-Image
        /// </summary>
        public ImageSource ProcessedImage
        {
            get { return _processedImage; }
            set { _processedImage = value; OnPropertyChanged(); }
        }

        private Brush _startColor;
        /// <summary>
        /// Color of Start/Stop button
        /// </summary>
        public Brush StartButtonColor
        {
            get { return _startColor; }
            set { _startColor = value; OnPropertyChanged(); }
        }

        private string _startText;
        /// <summary>
        /// Text of Start/Stop button
        /// </summary>
        public string StartText
        {
            get { return _startText; }
            set { _startText = value; OnPropertyChanged(); }
        }

        private string _angleOutput;
        /// <summary>
        /// Prompted Angle
        /// </summary>
        public string AngleOutput
        {
            get { return _angleOutput; }
            set { _angleOutput = value; OnPropertyChanged(); }
        }


        private string _dotPositions;
        /// <summary>
        /// Formatted List of saved DotPositions
        /// </summary>
        public string DotPositions
        {
            get { return _dotPositions; }
            set { _dotPositions = value; OnPropertyChanged(); }
        }

        private string _anchorPixelPosition;
        /// <summary>
        /// Formated indication of anchor pixel position
        /// </summary>
        public string AnchorPixelPosition
        {
            get { return _anchorPixelPosition; }
            set { _anchorPixelPosition = value; OnPropertyChanged(); }
        }

        private string _movingTipPixelPosition;
        /// <summary>
        /// Formated indication of moving tip pixel position
        /// </summary>
        public string MovingTipPixelPosition
        {
            get { return _movingTipPixelPosition; }
            set { _movingTipPixelPosition = value; OnPropertyChanged(); }
        }

        private double _targetTolerance;
        /// <summary>
        /// Tolerance factor applied on size in order to define tolerated dot movement
        /// </summary>
        public double TargetToleranceFactor
        {
            get { return _targetTolerance; }
            set { _targetTolerance = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Information for user
        /// </summary>
        public UserPrompt Prompt { get; set; } = new UserPrompt();

        /// <summary>
        /// Picked color of anchor
        /// </summary>
        public SmartColor AnchorColor { get; set; } = new SmartColor();

        /// <summary>
        /// Picked color of moving tip
        /// </summary>
        public SmartColor MovingTipColor { get; set; } = new SmartColor();

        private int _pickerRadius;
        /// <summary>
        /// Color picker radius. To calculate avarage color for given area.
        /// </summary>
        public int PickerRadius
        {
            get { return _pickerRadius; }
            set { _pickerRadius = value; OnPropertyChanged(); }
        }

        private Brush _anchorColorPickerBack;
        /// <summary>
        /// Color of button to pick anchor color.
        /// Indicates active or passive state
        /// </summary>
        public Brush AnchorColorPickerBack
        {
            get { return _anchorColorPickerBack; }
            set { _anchorColorPickerBack = value; OnPropertyChanged(); }
        }

        private Brush _tipColorPickerBack;
        /// <summary>
        /// Color of button to pick moving tip color.
        /// Indicates active or passive state
        /// </summary>
        public Brush TipColorPickerBack
        {
            get { return _tipColorPickerBack; }
            set { _tipColorPickerBack = value; OnPropertyChanged(); }
        }
        
        private FilterInfoCollection _videoCaptureDevices;
        /// <summary>
        /// Collection of available video devices
        /// </summary>
        public FilterInfoCollection VideoCaptureDevices {
            get { return _videoCaptureDevices; }
            set { _videoCaptureDevices = value; OnPropertyChanged(); }
        }

        private bool _tipSelectionActive;
        /// <summary>
        /// Property to show wether moving tip was explicitely set
        /// </summary>
        public bool TipSelectionActive
        {
            get { return _tipSelectionActive; }
            set { _tipSelectionActive = value; OnPropertyChanged(); }
        }

        // Some predefined colors
        Brush positiveButton = Brushes.LightGreen;
        Brush passiveButton = Brushes.LightGray;
        Brush negativeButton = Brushes.LightSalmon;
        #endregion

        /// <summary>
        /// Image Processor of captured or loaded images
        /// </summary>
        ImageProcessor Processor;

        VideoCaptureDevice _captureDevice;
        /// <summary>
        /// Source to capture images
        /// </summary>
        public VideoCaptureDevice CaptureDevice
        {
            get => _captureDevice;
            set
            {
                if (_lastSelectedVideoSourceIdx != cbx_cams.SelectedIndex)
                    Processor.ResetInternals();

                _lastSelectedVideoSourceIdx = cbx_cams.SelectedIndex;
                _captureDevice = value;
            }
        }

        /// <summary>
        /// Main Window setup
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Uri testFramePath = new Uri(@"D:\Arbeitsordner\CS\DeflectionMeasure\MeasureDeflection\MeasureDeflection\Files\Idle.jpg");
            CamImage = new BitmapImage(testFramePath);

            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            cbx_cams.SelectedIndex = _lastSelectedVideoSourceIdx = 0;

            Processor = new ImageProcessor(PromptNewMessage_Handler);
            StartButtonColor = positiveButton;
            StartText = "Start";
            PickerRadius = colorPickerRadius;
            TargetToleranceFactor = startToleranceFactor;
            referenceDireectionVector = new Vector();

            DataContext = this;
        }

        #region FlowControl

        bool isRunning = false;
        PickerMode ColorCaptureMode = PickerMode.off;
        ImageProcessor.AnchorTipPair _targets = new ImageProcessor.AnchorTipPair();
        Vector currentDirectionVector;
        Vector referenceDireectionVector;
        ImageProcessor.BlobCentre anchorPoint = new ImageProcessor.BlobCentre();
        ImageProcessor.BlobCentre movingTipPoint = new ImageProcessor.BlobCentre();
        int _sampleIteration = 0;
        int currentTolerance = 0;
        int _lastSelectedVideoSourceIdx;

        int skipImage = 0;

        double _currentAngle = 0;
        /// <summary>
        /// Current angle relative to an reference angle 
        /// </summary>
        public double CurrentAngle
        {
            get { return _currentAngle; }
            set { _currentAngle = value; AngleOutput = _currentAngle.ToString("F2"); }
        }

        #endregion

        #region GUI_UserEvents
        /// <summary>
        /// ComboBox is opend.
        /// This triggers scan for available image caputre devices.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cbx_cams_DropDownOpened(object sender, EventArgs e)
        {
            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }

        /// <summary>
        /// Start/Stop-button was pressed.
        /// Depending on current mode either the capture should be started or stopped.
        /// Current mode is shown properly by naming the action on pressing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                // This click starts catpure mode
                isRunning = true;
                CaptureDevice = new VideoCaptureDevice((string)cbx_cams.SelectedValue);
                CaptureDevice.NewFrame += new NewFrameEventHandler(CaptureDevice_NewFrame);

                CaptureDevice.Start();

                // Next click will stop capture mode
                StartText = "Stop";
                StartButtonColor = negativeButton;
            }
            else
            {
                // This click stops catpure mode
                isRunning = false;
                CaptureDevice.SignalToStop();
                
                // Next click will start capture mode
                StartText = "Start";
                StartButtonColor = positiveButton;
            }
        }

        /// <summary>
        /// Triggers on capture event.
        /// This is only possible if capture mode is not already active
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_OneShot_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning == false)
            {
                CaptureDevice = new VideoCaptureDevice((string)cbx_cams.SelectedValue);
                CaptureDevice.NewFrame += new NewFrameEventHandler(CaptureDevice_NewFrameOnce);
                CaptureDevice.Start();
            }
        }

        /// <summary>
        /// Loads image from filesystem to analyse it rather than a freshly captured image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_PreloadImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog picker = new OpenFileDialog();
            picker.InitialDirectory = defaultPath;
            picker.DefaultExt = ".jpg";
            picker.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|PNG Image|*.png";

            bool? result = picker.ShowDialog();
            if (result == true)
            {
                Uri testFramePath = new Uri(picker.FileName);
                CamImage = new BitmapImage(testFramePath);
            }
            ProcessImage(currentTolerance*3);
        }

        /// <summary>
        /// Saves current taken image for later use on the filesystem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SaveImageClick(object sender, RoutedEventArgs e)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img_CamStream.Source as BitmapImage));
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "Sample###";
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";

            if (dlg.ShowDialog() == true)
            {
                using (var stream = dlg.OpenFile())
                {
                    encoder.Save(stream);
                }
            }
        }

        /// <summary>
        /// Set anchor properties (color and start position)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Anchor_Click(object sender, RoutedEventArgs e)
        {
            ColorCaptureMode = PickerMode.getAnchor;
            img_CamStream.MouseMove += new MouseEventHandler(camStreamMouseMove);
            AnchorColorPickerBack = positiveButton;
        }

        /// <summary>
        /// Set moving tip properties (color and start position)
        /// Intendet to be optionally
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_MovingTip_Click(object sender, RoutedEventArgs e)
        {
            ColorCaptureMode = PickerMode.getTip;
            img_CamStream.MouseMove += new MouseEventHandler(camStreamMouseMove);
            TipColorPickerBack = positiveButton;
        }

        /// <summary>
        /// Mouse moves over Image event.
        /// Intendet to pick image colors if picker mode is active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void camStreamMouseMove(object sender, MouseEventArgs e)
        {
            // Only if picker is currently active
            if ((int)ColorCaptureMode > (int)PickerMode.off)
            {
                GetPositionAndColorInPreview();
            }
        }

        /// <summary>
        /// Mouse click-event during color picker mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Img_CamStream_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ColorCaptureMode != PickerMode.off)
            {
                img_CamStream.MouseMove -= new MouseEventHandler(camStreamMouseMove);

                Point clickPos = Mouse.GetPosition(img_CamStream);
                Point pixelPos = GetPositionAndColorInPreview();
                switch (ColorCaptureMode)
                {
                    case PickerMode.getAnchor:
                        {
                            _targets.Anchor.Centre = new ImageProcessor.BlobCentre { X = (int)pixelPos.X, Y = (int)pixelPos.Y };
                            _targets.Anchor.Color = new AForge.Imaging.RGB(AnchorColor.R, AnchorColor.G, AnchorColor.B);
                            TipSelectionActive = true;

                            ImageProcessor.BlobCentre anchor = new ImageProcessor.BlobCentre();
                            ImageProcessor.BlobCentre movingTip = new ImageProcessor.BlobCentre();

                            ImageSource img = Processor.SetAnchorProperty(CamImage, _targets.Anchor, out anchor, out movingTip);
                            img.Freeze();
                            ProcessedImage = img;
                            currentTolerance = (int)(anchor.D * TargetToleranceFactor);
                        }
                        break;

                    case PickerMode.getTip:
                        {
                            _targets.MovingTip.Centre = new ImageProcessor.BlobCentre { X = (int)pixelPos.X, Y = (int)pixelPos.Y };
                            _targets.MovingTip.Color = new AForge.Imaging.RGB(MovingTipColor.R, MovingTipColor.G, MovingTipColor.B);

                            ImageProcessor.BlobCentre movingTip = new ImageProcessor.BlobCentre();
                            ImageSource img = Processor.SetMovingTipProperty(CamImage, _targets.MovingTip, out movingTip);
                            img.Freeze();
                            ProcessedImage = img;
                        }
                        break;
                }
                AnchorColorPickerBack = passiveButton;
                TipColorPickerBack = passiveButton;

                ProcessImage(currentTolerance);

                ColorCaptureMode = PickerMode.off;
            }
        }

        /// <summary>
        /// Sets zero-positiom reference to current position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SetReference(object sender, RoutedEventArgs e)
        {
            CurrentAngle = 0;
            referenceDireectionVector = currentDirectionVector;
        }

        /// <summary>
        /// Saves current angle and positions to formatted list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SaveCurrentPostion(object sender, RoutedEventArgs e)
        {
            if (_sampleIteration <= 0)
                DotPositions += $"Num\tAnchor x\t/y\tMoving x\t/y\tAngle\n";

            DotPositions += $"{++_sampleIteration}\t{anchorPoint.X:F2}\t{anchorPoint.Y:F2}\t{movingTipPoint.X:F2}\t{movingTipPoint.Y:F2}\t{CurrentAngle:F3}\n";
        }

        /// <summary>
        /// Copies formatted list to clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CopyListToClipBoard(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(DotPositions);
        }

        /// <summary>
        /// Clears formatted list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bnt_ClearList(object sender, RoutedEventArgs e)
        {
            DotPositions = "";
            _sampleIteration = 0;
        }

        #endregion

        /// <summary>
        /// Event intended to be triggered if capture device provides a new image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void CaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (skipImage-- <= 0)
            {
                skipImage = skipFramesInContiunousMode;
                CaputerImage(eventArgs);
            }
        }

        /// <summary>
        /// Event intended to be triggered once if capture device provides a new image and stop caputre device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void CaptureDevice_NewFrameOnce(object sender, NewFrameEventArgs eventArgs)
        {
            if (skipImage-- <= 0)
            {
                skipImage = skipFramesInOnShotMode;
                CaputerImage(eventArgs);

                CaptureDevice.SignalToStop();
            }
        }

        /// <summary>
        /// Displays captured image and initiates processing
        /// </summary>
        /// <param name="eventArgs"></param>
        private void CaputerImage(NewFrameEventArgs eventArgs)
        {
            System.Drawing.Bitmap frame = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
            CamImage = ImageProcessor.Bitmap2BitmapImage(frame);

            ProcessImage(currentTolerance);
        }

        /// <summary>
        /// Gets pixel position in preview image and sets anchor and moving acording to the local color
        /// </summary>
        /// <returns></returns>
        private Point GetPositionAndColorInPreview()
        {
            BitmapSource bitmapSource = img_CamStream.Source as BitmapSource;
            Image imageFrame = img_CamStream;
            Point hoverPos = Mouse.GetPosition(img_CamStream);
            Point pixelPos = GetXYPosInFrame(imageFrame, hoverPos);

            Color acvrgColor = ImageProcessor.GetAvarageColor(img_CamStream.Source as BitmapSource, (int)pixelPos.X, (int)pixelPos.Y, PickerRadius);

            switch (ColorCaptureMode)
            {
                case PickerMode.getAnchor:
                    AnchorColor.SetColor(acvrgColor);
                    MovingTipColor.SetColor(acvrgColor);
                    break;

                case PickerMode.getTip:
                    MovingTipColor.SetColor(acvrgColor);
                    break;
            }

            return pixelPos;
        }

        /// <summary>
        /// Get pixel x and y position of mouse cursor relativ to an certain image
        /// </summary>
        /// <param name="imageFrame"></param>
        /// <param name="hoverPos"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static Point GetXYPosInFrame(Image imageFrame, Point hoverPos)
        {
            BitmapSource bitmapSource = imageFrame.Source as BitmapSource;
            double x, y;

            x = hoverPos.X;
            x *= bitmapSource.PixelWidth / imageFrame.ActualWidth;
            if ((int)x > bitmapSource.PixelWidth - 1)
                x = bitmapSource.PixelWidth - 1;
            else if (x < 0)
                x = 0;

            y = hoverPos.Y;
            y *= bitmapSource.PixelHeight / imageFrame.ActualHeight;
            if ((int)y > bitmapSource.PixelHeight - 1)
                y = bitmapSource.PixelHeight - 1;
            else if (y < 0)
                y = 0;

            Point pos = new Point(x, y);
            return pos;
        }

        /// <summary>
        /// Processes Image. 
        /// Scans for anchor and moving tip on assumed locations, calculates deflection and visualizes the result
        /// </summary>
        void ProcessImage(int tolerance)
        {
            if (Processor.InitFinisched)
            {
                ImageProcessor.BlobCentre aP, mTP;
                BitmapImage anchroImg = Processor.ProcessImage(CamImage, _targets, tolerance, out aP, out mTP);

                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    Pen stroke = new Pen(Brushes.White, 2);

                    dc.DrawImage(anchroImg, new Rect(0, 0, anchroImg.PixelWidth, anchroImg.PixelHeight));

                    AnchorPixelPosition = $"###.## / ###.##";
                    MovingTipPixelPosition = $"###.## / ###.##";

                    double angle = -361;

                    if ((aP != null) && (mTP != null))
                    {
                        Point anchor = new Point(aP.X, aP.Y);
                        AnchorPixelPosition = $"{anchor.X:F2} / {anchor.Y:F2}";
                        anchorPoint = aP;

                        dc.DrawEllipse(Brushes.Yellow, stroke, anchor, 10, 10);
                        if (mTP != null)
                        {
                            Point movingTip = new Point(mTP.X, mTP.Y);
                            MovingTipPixelPosition = $"{movingTip.X:F2} / {movingTip.Y:F2}";
                            movingTipPoint = mTP;

                            dc.DrawEllipse(Brushes.White, stroke, movingTip, 10, 10);
                            dc.DrawLine(stroke, anchor, movingTip);
                            dc.DrawRectangle(Brushes.Transparent, stroke,
                                new Rect(new Point(aP.X - currentTolerance / 2, aP.Y - currentTolerance / 2),
                                new Size(currentTolerance, currentTolerance)));
                            dc.DrawRectangle(Brushes.Transparent, stroke,
                                new Rect(new Point(mTP.X - currentTolerance / 2, mTP.Y - currentTolerance / 2),
                                new Size(currentTolerance, currentTolerance)));

                            Vector aV = new Vector { X = anchor.X, Y = anchor.Y };
                            Vector mtV = new Vector { X = movingTip.X, Y = movingTip.Y };
                            Vector dir = mtV - aV;

                            angle = Vector.AngleBetween(dir, referenceDireectionVector);
                            currentDirectionVector = dir;
                        }
                    }
                    CurrentAngle = angle;
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(anchroImg.PixelWidth, anchroImg.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);
                ImageSource img = ((ImageSource)rtb);
                img.Freeze();
                ProcessedImage = img;
            }
        }

        /// <summary>
        /// Promt handler for user notifications
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        public void PromptNewMessage_Handler(UserPrompt.eNotifyType type, string message)
        {
            Prompt.Caption = type.ToString();
            Prompt.PromptMessage = message;
            Debug.WriteLine($"'{type,8}': {message}");
        }

        #region SubClasses

        /// <summary>
        /// Picker modes
        /// </summary>
        enum PickerMode
        {
            off,
            getAnchor,
            getTip
        }
        #endregion
    }
}
