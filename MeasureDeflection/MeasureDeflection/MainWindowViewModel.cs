using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using AForge.Video;
using AForge.Video.DirectShow;

using MeasureDeflection.Utils;
using System.Reflection;
using MeasureDeflection.Utils.Interfaces;
using System.Collections.ObjectModel;
using MeasureDeflection.Processor;



// @todo:   Add Download-Link to AForgenet
//          http://www.aforgenet.com/framework/downloads.html

// @todo:   NUnit-TestFramework einarbeitn
//          FakeItEasy
//          PrimsUnity anschauen
//          Resharper

namespace MeasureDeflection
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Preferences

        // Default Folder for loading and saving images
        const string defaultPath = @"D:\Arbeitsordner\CS\DeflectionMeasure\Files";
        const int colorPickerRadius = 4;
        const double startToleranceFactor = 3.0;
        const int skipFramesInContiunousMode = 15; // Results in roughly 2 Frames per Second
        const int skipFramesInOnShotMode = 15; // Results in roughly 2 Frames per Second
        private readonly IFileHandler _fHandler;
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Automated PropertyChanged Methode:
        /// Calling Member is determined automatically by CallerMemberName-Property
        /// </summary>
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
        /// Constructor
        /// </summary>
        public MainWindowViewModel(IFileHandler fHandler)
        {
            Processor = new MarkerScanner(PromptNewMessage_Handler, OnAnchorSetEvent, OnMovingTipSetEvent);

            Assembly myAssembly = Assembly.GetExecutingAssembly();
            // Loading included picture by reflection               Namespace ...     Subdir ... ImageName
            Stream myStream = myAssembly.GetManifestResourceStream("MeasureDeflection.Pictures.Logo.png");
            CamImage = MarkerScanner.Bitmap2BitmapImage(new System.Drawing.Bitmap(myStream));

            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _lastSelectedVideoSourceIdx = 0;

            PickerRadius = colorPickerRadius;
            TargetToleranceFactor = startToleranceFactor;
            referenceDireectionVector = new Vector();

            ImageProcessing_Command = new RelayCommand(ImageProcessingAction);
            ColorPicker_Command = new RelayCommand(ColorPickerAction);
            LoadSaveImage_Command = new RelayCommand(LoadSaveImageAction);
            SetAngleReference_Command = new RelayCommand(SetAngleReference);
            AngleList_Command = new RelayCommand(AngleListAction);
            Log_Command = new RelayCommand(SaveLogAction);
            _fHandler = fHandler;
            _fHandler.DefaultPath = defaultPath;
        }

        #region GUI_Properties

        private int _slectedVideoSourceIdx;
        /// <summary> Selected index in list of available video sources </summary>
        public int SlectedVideoSourceIdx
        {
            get { return _slectedVideoSourceIdx; }
            set { _slectedVideoSourceIdx = value; }
        }

        private object _selectedVideoSource;
        /// <summary> Selected video source from list</summary>
        public object SelectedVideoSource
        {
            get { return _selectedVideoSource; }
            set { _selectedVideoSource = value; }
        }

        private BitmapImage _camImage;
        /// <summary> Camera-Image </summary>
        public BitmapImage CamImage
        {
            get { return _camImage; }
            set { _camImage = value; OnPropertyChanged(); }
        }

        private ImageSource _processedImage;
        /// <summary> Processed-Image </summary>
        public ImageSource ProcessedImage
        {
            get { return _processedImage; }
            set { _processedImage = value; OnPropertyChanged(); }
        }

        private string _angleOutput;
        /// <summary> Prompted Angle </summary>
        public string AngleOutput
        {
            get { return _angleOutput; }
            set { _angleOutput = value; OnPropertyChanged(); }
        }

        /// <summary> Formatted List of saved DotPositions </summary>
        public ObservableCollection<DotSamples> DotPosition { get; set; } = new ObservableCollection<DotSamples>();

        private string _anchorPixelPosition;
        /// <summary> Formated indication of anchor pixel position </summary>
        public string AnchorPixelPosition
        {
            get { return _anchorPixelPosition; }
            set { _anchorPixelPosition = value; OnPropertyChanged(); }
        }

        private string _movingTipPixelPosition;
        /// <summary> Formated indication of moving tip pixel position </summary>
        public string MovingTipPixelPosition
        {
            get { return _movingTipPixelPosition; }
            set { _movingTipPixelPosition = value; OnPropertyChanged(); }
        }

        private double _targetTolerance;
        /// <summary> Tolerance factor applied on size in order to define tolerated dot movement </summary>
        public double TargetToleranceFactor
        {
            get { return _targetTolerance; }
            set { _targetTolerance = value; OnPropertyChanged(); }
        }

        /// <summary> Information for user </summary>
        public UserPrompt Prompt { get; set; } = new UserPrompt();

        /// <summary> Picked color of anchor </summary>
        public SmartColor AnchorColor { get; set; } = new SmartColor();

        /// <summary> Picked color of moving tip </summary>
        public SmartColor MovingTipColor { get; set; } = new SmartColor();

        private int _pickerRadius;
        /// <summary> Color picker radius. To calculate avarage color for given area. </summary>
        public int PickerRadius
        {
            get { return _pickerRadius; }
            set { _pickerRadius = value; OnPropertyChanged(); }
        }

        private FilterInfoCollection _videoCaptureDevices;
        /// <summary> Collection of available video devices </summary>
        public FilterInfoCollection VideoCaptureDevices
        {
            get { return _videoCaptureDevices; }
            set { _videoCaptureDevices = value; OnPropertyChanged(); }
        }

        private bool _tipSelectionActive;
        /// <summary> Property to show wether moving tip was explicitely set </summary>
        public bool TipSelectionActive
        {
            get { return _tipSelectionActive; }
            set { _tipSelectionActive = value; OnPropertyChanged(); }
        }
        #endregion

        /// <summary> Image Processor of captured or loaded images </summary>
        MarkerScanner Processor;

        VideoCaptureDevice _captureDevice;
        /// <summary> Source to capture images </summary>
        public VideoCaptureDevice CaptureDevice
        {
            get => _captureDevice;
            set
            {
                if (_lastSelectedVideoSourceIdx != SlectedVideoSourceIdx)
                    Processor.ResetInternals();

                _lastSelectedVideoSourceIdx = SlectedVideoSourceIdx;
                _captureDevice = value;
            }
        }

        #region FlowControl

        bool _isRunning = false;
        public bool IsRunning { get => _isRunning; set => _isRunning = value; }

        PickerMode ColorCaptureMode = PickerMode.off;
        Vector currentDirectionVector;
        Vector referenceDireectionVector;
        BlobCentre _anchorPoint = new BlobCentre();
        BlobCentre _movingTipPoint = new BlobCentre();
        int _sampleIteration = 0;
        int currentTolerance = 0;
        int _lastSelectedVideoSourceIdx;

        int skipImage = 0;

        double _currentAngle = 0;
        /// <summary> Current angle relative to an reference angle </summary>
        public double CurrentAngle
        {
            get { return _currentAngle; }
            set { _currentAngle = value; AngleOutput = _currentAngle.ToString("F2"); }
        }

        #endregion

        #region GUI_Commands

        public ICommand ImageProcessing_Command { get; set; }
        public ICommand ColorPicker_Command { get; set; }
        public ICommand LoadSaveImage_Command { get; set; }
        public ICommand SetAngleReference_Command { get; set; }
        public ICommand AngleList_Command { get; set; }
        public ICommand Log_Command { get; set; }


        /// <summary>
        /// This triggers scan for available image caputre devices.
        /// </summary>
        public void LoadAvailableVideoSources()
        {
            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }

        /// <summary>
        /// Imaging and processing related command
        /// </summary>
        private void ImageProcessingAction(object obj)
        {
            string action = (string)obj;

            switch (action)
            {
                case "Start": 
                    StartSelectedVideoSource(new NewFrameEventHandler(CaptureDevice_NewFrame));
                    break;

                case "Stop": 
                    CaptureDevice.SignalToStop(); 
                    break;

                case "Sample":
                    if (IsRunning == false) // @todo IsRunning is not used
                        StartSelectedVideoSource(new NewFrameEventHandler(CaptureDevice_NewFrameOnce));
                    break;

                default: throw new Exception($"The command {action} is unknown");
            }
        }

        /// <summary>
        /// Start Video Capturing with selected source
        /// </summary>
        private void StartSelectedVideoSource(NewFrameEventHandler imageCallback)
        {
            CaptureDevice = new VideoCaptureDevice((string)SelectedVideoSource);
            CaptureDevice.NewFrame += new NewFrameEventHandler(imageCallback);
            CaptureDevice.Start();
        }

        /// <summary> Mode code for action keyword </summary>
        Dictionary<string, PickerMode> PickerModeDict = new Dictionary<string, PickerMode>
        {
            {"Anchor", PickerMode.getAnchor},
            {"MovingTip", PickerMode.getTip},
        };

        /// <summary>
        /// Set referencepoint properties (color and start position)
        /// </summary>
        private void ColorPickerAction(object obj)
        {
            string action = (string)obj;
            if (!PickerModeDict.ContainsKey(action))
                throw new Exception($"The command '{action}' is unknown");

            ColorCaptureMode = PickerModeDict[action];
        }

        /// <summary>
        /// Saves currecnt image ore loads image from filesystem to analyse it rather than a freshly captured image
        /// </summary>
        private void LoadSaveImageAction(object obj)
        {
            string action = (string)obj;
            switch (action)
            {
                case "PreloadImage":
                    var newImage = _fHandler.LoadImage();
                    if (newImage == null)
                        return;

                    CamImage = newImage;
                    ProcessImage(currentTolerance * 3);
                    break;

                case "SaveImage":
                    _fHandler.SaveImage(CamImage);
                    break;

                default: throw new Exception($"The command {action} is unknown");
            }
        }

        /// <summary>
        /// Sets zero-positiom reference to current position
        /// </summary>
        private void SetAngleReference(object obj)
        {
            CurrentAngle = 0;
            referenceDireectionVector = currentDirectionVector;
        }

        /// <summary>
        /// Command related to DotList
        /// </summary>
        private void AngleListAction(object obj)
        {
            string action = (string)obj;

            switch (action)
            {
                case "SaveList": DotPosition.Add(new DotSamples(DotPosition.Count, _anchorPoint.X, _anchorPoint.Y, _movingTipPoint.X, _movingTipPoint.Y, CurrentAngle)); break;

                case "CopyList":
                    string outTable = "Sample\tAnchor-X\ttAnchor-Y\tTip-X\tTip-Y\tAngle" + Environment.NewLine;
                    foreach (var item in DotPosition)
                        outTable += item.ToString() + Environment.NewLine;
                    Clipboard.SetText(outTable);
                    break;

                case "ClearList": DotPosition.Clear(); break;

                default: throw new Exception("This command is unknown");
            }
        }

        /// <summary>
        /// Command related to DotList
        /// </summary>
        private void SaveLogAction(object obj)
        {
            DotPosition.Add(new DotSamples(DotPosition.Count, _anchorPoint.X, _anchorPoint.Y, _movingTipPoint.X, _movingTipPoint.Y, CurrentAngle));
        }
        #endregion

        /// <summary>
        /// Event intended to be triggered if capture device provides a new image
        /// </summary>
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
        private void CaputerImage(NewFrameEventArgs eventArgs)
        {
            System.Drawing.Bitmap frame = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
            CamImage = MarkerScanner.Bitmap2BitmapImage(frame);

            ProcessImage(currentTolerance);
        }

        void OnAnchorSetEvent(BlobCentre anchor)
        {
            _anchorPoint = anchor;
        }

        void OnMovingTipSetEvent(BlobCentre movingTip)
        {
            _movingTipPoint = movingTip;
        }


        /// <summary>
        /// Gets pixel position in preview image and sets anchor and moving acording to the local color
        /// </summary>
        /// <returns></returns>
        public Point GetPositionAndColorInPreview(Point hoverPos, Image imageFrame)
        {
            Point pixelPos = new Point();

            // Only if picker is currently active
            if ((int)ColorCaptureMode > (int)PickerMode.off)
            {
                BitmapSource bitmapSource = CamImage as BitmapSource;
                pixelPos = ImageHelper.GetXYPosInFrame(imageFrame, hoverPos);

                Color acvrgColor = MarkerScanner.GetAvarageColor(CamImage as BitmapSource, (int)pixelPos.X, (int)pixelPos.Y, PickerRadius);

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
            }

            return pixelPos;
        }

        /// <summary>
        /// Colorpicker function.
        /// The current position in the image is used to set the selected color.
        /// </summary>
        public void SetColorFromPositionInPreview(Point clickPos, Image imageFrame)
        {
            if (ColorCaptureMode != PickerMode.off)
            {

                Point pixelPos = GetPositionAndColorInPreview(clickPos, imageFrame);
                switch (ColorCaptureMode)
                {
                    case PickerMode.getAnchor:
                        {
                            var profile = SetSearchProfile(pixelPos, AnchorColor);
                            TipSelectionActive = true;

                            var img = Processor.TryToSetAnchor(CamImage, profile);
                            img.Freeze();
                            ProcessedImage = img;
                        }
                        break;

                    case PickerMode.getTip:
                        {
                            var profile = SetSearchProfile(pixelPos, MovingTipColor);

                            var img = Processor.TryToSetTip(CamImage, profile);
                            img.Freeze();
                            ProcessedImage = img;
                        }
                        break;
                }

                ProcessImage(TargetToleranceFactor);
                ColorCaptureMode = PickerMode.off;
            }
        }

        /// <summary>
        /// Generates search profile wit target location and color
        /// </summary>
        private TargetProfile SetSearchProfile(Point pixelPos, SmartColor color)
        {
            var point = new TargetProfile();
            point.Centre = new BlobCentre { X = (int)pixelPos.X, Y = (int)pixelPos.Y };
            point.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            return point;
        }

        /// <summary>
        /// Processes Image. 
        /// Scans for anchor and moving tip on assumed locations, calculates deflection and visualizes the result
        /// </summary>
        void ProcessImage(double toleranceFactor)
        {
            if (Processor.InitFinisched)
            {
                var image = Processor.ProcessImage(CamImage, toleranceFactor);

                ImageSource imgb = ((ImageSource)image);
                imgb.Freeze();
                ProcessedImage = imgb;

                AnchorPixelPosition = $"###.## / ###.##";
                MovingTipPixelPosition = $"###.## / ###.##";
                double angle = -361;

                Point aP = new Point(); 
                Point mP = new Point();
                if (_anchorPoint != null)
                {
                    aP = _anchorPoint.C;
                    AnchorPixelPosition = $"{aP.X:F2} / {aP.Y:F2}";
                }
                if (_movingTipPoint != null)
                {
                    mP = _movingTipPoint.C;
                    MovingTipPixelPosition = $"{mP.X:F2} / {mP.Y:F2}";
                }

                if ((aP != null) && (mP != null))
                {
                    Vector aV = new Vector(aP.X, aP.Y);
                    Vector mtV = new Vector(mP.X, mP.Y);
                    Vector dir = mtV - aV;

                    angle = Vector.AngleBetween(dir, referenceDireectionVector);
                    currentDirectionVector = dir;
                }
                CurrentAngle = angle;
            }
        }


        List<string> MyLog = new List<string>();
        
        /// <summary>
        /// Promt handler for user notifications
        /// </summary>
        /// <param name="type">Notification urgency</param>
        /// <param name="message">Message</param>
        public void PromptNewMessage_Handler(UserPrompt.eNotifyType type, string message)
        {
            Prompt.Caption = type.ToString();
            Prompt.PromptMessage = message;

            var prompt = $"'{type,8}': {message}";
            MyLog.Add(prompt);
            Debug.WriteLine(prompt);
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
