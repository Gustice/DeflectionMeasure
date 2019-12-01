using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;

using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

using MeasureDeflection.Utils;
using MeasureDeflection.Processor;

namespace MeasureDeflection
{
    /// <summary>
    /// Processor for captured Images.
    /// Scans for marks and returns found positions
    /// </summary>
    public class MarkerScanner
    {
        private bool _initFinished;
        /// <summary>
        /// Uper layer indication for finieshed initialization
        /// </summary>
        public bool InitFinisched
        {
            get { return _initFinished; }
        }

        OperationMode _operation = OperationMode.missingData;
        /// <summary>
        /// Current operation mode of image processor
        /// </summary>
        private OperationMode Operation
        {
            get { return _operation; }
            set
            {
                _operation = value;

                _initFinished = false;
                if ((int)_operation >= (int)OperationMode.MovingTipIsSet)
                    _initFinished = true;
            }
        }

        public AnchorTipPair Profile { get; private set; }

        /// <summary>
        /// Reset internal to get a clean object to start a new image processing approach
        /// </summary>
        public void ResetInternals()
        {
            Operation = OperationMode.missingData;
        }

        /// <summary>
        /// Helper for analyzing pictures
        /// </summary>
        Analyzer PicAnalyzer = new Analyzer();

        private BlobCentre anchorPoint;
        /// <summary>
        /// Public anchor point.
        /// Can also be monitored by event.
        /// </summary>
        public BlobCentre AnchorPoint
        {
            get => anchorPoint; 
            private set
            {
                anchorPoint = value;
                _anchroSetEvent?.Invoke(anchorPoint);
            }
        }

        private BlobCentre tipPoint;
        /// <summary>
        /// Public tip point.
        /// Can also be monitored by event.
        /// </summary>
        public BlobCentre TipPoint
        {
            get => tipPoint;
            private set
            {
                tipPoint = value;
                _MovingTipSetEvent?.Invoke(tipPoint);
            }
        }

        /// <summary>
        /// Defines Anchor in picutre. 
        /// Color profile is applied and found marks a tested agains give position.
        /// Matched mark is set as anchor point. If only one mark remains it is set as tip point.
        /// Note: Processing is optimized for round marks only.
        /// </summary>
        /// <param name="camImage"></param>
        /// <param name="anchorTarget"></param>
        /// <returns></returns>
        public ImageSource TryToSetAnchor(BitmapSource camImage, TargetProfile anchorTarget)
        {
            _initFinished = false;
            AnchorPoint = TipPoint = null;

            var result = SearchMarkers("Anchor", camImage, anchorTarget);
            if (result.Target == null)
                return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);

            Operation = OperationMode.AnchorIsSet;
            PromptMessage(UserPrompt.eNotifyType.Note, $"Anchor point set");

            AnchorPoint = anchorTarget.Centre = GetBlopCentre(result.Target);

            var target = SetMarkerProperties(anchorTarget, result.Target);
            Profile = new AnchorTipPair(target);

            if (result.Remaining.Count == 1)
            {
                PromptMessage(UserPrompt.eNotifyType.Note, "Anchor point set - Remaining dot preset as moving tip");
                Operation = OperationMode.MovingTipIsSet;

                TargetProfile tipTarget = (TargetProfile)anchorTarget.Clone();
                TipPoint = tipTarget.Centre = GetBlopCentre(result.Remaining[0]);

                Profile.AddTipProfile(tipTarget);
            }


            return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
        }

        /// <summary>
        /// Defines moving Tip in picutre. 
        /// Color profile is applied and found marks a tested agains give position.
        /// Matched mark is set as tip point.
        /// Note: Processing is optimized for round marks only.
        /// </summary>
        /// <param name="camImage"></param>
        /// <param name="anchorTarget"></param>
        /// <returns></returns>
        public ImageSource TryToSetTip(BitmapSource camImage, TargetProfile movingTipTarget)
        {
            TipPoint = null;

            if ((int)Operation < (int)OperationMode.AnchorIsSet)
            {
                PromptMessage(UserPrompt.eNotifyType.Warning, "Please specify anchor first");
                return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
            }

            var result = SearchMarkers("Moving tip", camImage, movingTipTarget);
            if (result.Target == null)
                return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);

            Operation = OperationMode.MovingTipIsSet;
            PromptMessage(UserPrompt.eNotifyType.Note, "Moving tip point set");

            TipPoint = movingTipTarget.Centre = GetBlopCentre(result.Target);

            var target = SetMarkerProperties(movingTipTarget, result.Target);
            Profile.AddTipProfile(target);

            return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
        }

        /// <summary>
        /// Processes Images after Anchor and Tip point are set.
        /// Old positions are taken as reference positions. 
        /// New position of marker is only accepted if new marker is found within tolerance in reference to last known position.
        /// </summary>
        /// <param name="camImage"></param>
        /// <param name="tolerancefactor"></param>
        /// <returns>Processed image with annotations</returns>
        public RenderTargetBitmap ProcessImage(BitmapSource camImage, double tolerancefactor)
        {
            int tolerance = 0;
            BitmapImage filterImage = null;

            switch (Operation)
            {
                case OperationMode.missingData:
                    PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found. Please specify Anchor point first");
                    return RenderEmptyImageFromLast();

                case OperationMode.AnchorIsSet:
                    PromptMessage(UserPrompt.eNotifyType.Warning, "Moving point not found. Please specify Moving point first");
                    return RenderEmptyImageFromLast();

                case OperationMode.MovingTipIsSet:
                    {
                        tolerance = (int)(Profile.Anchor.Initial.Centre.D * tolerancefactor);
                        var anchorView = EvalPart(camImage, Profile.Anchor, tolerance, "Anchor");
                        AnchorPoint = Profile.Anchor.Current.Centre;

                        var movingTipView = EvalPart(camImage, Profile.MovingTip, tolerance, "Tip");
                        TipPoint = Profile.MovingTip.Current.Centre;

                        filterImage = CombineImages(anchorView, movingTipView);
                    }
                    break;

                default:
                    throw new Exception($"Case {Operation} was not implemented");
            }

            return GenerateAnnotatedImage(filterImage, tolerance);
        }


        private System.Drawing.Bitmap EvalPart(BitmapSource camImage, DynamicProfile mark,  int tolerance, string targetName)
        {
            var blobs = PicAnalyzer.FindBlobs(mark.Last, camImage);
            var view = (System.Drawing.Bitmap)PicAnalyzer.PorcessedImg.Clone();

            if (blobs.Count >= 1)
            {
                var found = SpareOutTargetBlobFromList(mark.Last.Centre, blobs, tolerance);
                if (found != null)
                {
                    mark.Current.Centre = GetBlopCentre(found);
                    mark.SaveCurrentToOld();
                }
                else
                    PromptMessage(UserPrompt.eNotifyType.Warning, $"{targetName} point not lost");
            }
            else
                PromptMessage(UserPrompt.eNotifyType.Warning, $"{targetName} point not lost");

            return view;
        }


        private BitmapImage CombineImages(System.Drawing.Bitmap anchorView, System.Drawing.Bitmap movingTipView)
        {
            BitmapImage filterImage;
            System.Drawing.Bitmap combindedImage = new System.Drawing.Bitmap(anchorView.Width, anchorView.Height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(combindedImage))
            {
                //set background color
                g.Clear(System.Drawing.Color.Black);
                anchorView.MakeTransparent(System.Drawing.Color.Black);
                movingTipView.MakeTransparent(System.Drawing.Color.Black);
                g.DrawImage(movingTipView, new System.Drawing.Rectangle(0, 0, movingTipView.Width, movingTipView.Height));
                g.DrawImage(anchorView, new System.Drawing.Rectangle(0, 0, anchorView.Width, anchorView.Height));
            }

            filterImage = Bitmap2BitmapImage(combindedImage);
            return filterImage;
        }

        private RenderTargetBitmap RenderEmptyImageFromLast()
        {
            var filterImage = Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);

            DrawingVisual temp = new DrawingVisual();
            using (DrawingContext dc = temp.RenderOpen()) {
                dc.DrawImage(filterImage, new Rect(0, 0, filterImage.PixelWidth, filterImage.PixelHeight));
            }
            RenderTargetBitmap rtbb = new RenderTargetBitmap(filterImage.PixelWidth, filterImage.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            rtbb.Render(temp);

            return rtbb;
        }

        private RenderTargetBitmap GenerateAnnotatedImage(BitmapImage filterImage, int tolerance)
        {
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                Pen stroke = new Pen(Brushes.White, 3);
                dc.DrawImage(filterImage, new Rect(0, 0, filterImage.PixelWidth, filterImage.PixelHeight));

                if (AnchorPoint != null)
                {
                    dc.DrawEllipse(Brushes.Yellow, stroke, AnchorPoint.C, 10, 10);
                    if (TipPoint != null)
                    {
                        dc.DrawEllipse(Brushes.White, stroke, TipPoint.C, 10, 10);

                        var aP = AnchorPoint.C;
                        var mtP = TipPoint.C;
                        dc.DrawLine(stroke, AnchorPoint.C, TipPoint.C);

                        dc.DrawRectangle(Brushes.Transparent, stroke,
                            new Rect(new Point(aP.X - tolerance / 2, aP.Y - tolerance / 2),
                            new Size(tolerance, tolerance)));
                        dc.DrawRectangle(Brushes.Transparent, stroke,
                            new Rect(new Point(mtP.X - tolerance / 2, mtP.Y - tolerance / 2),
                            new Size(tolerance, tolerance)));
                    }
                }
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(filterImage.PixelWidth, filterImage.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            return rtb;
        }



        private MarkerScan SearchMarkers(string mName, BitmapSource camImage, TargetProfile anchorTarget)
        {
            var result = new MarkerScan();

            var blobs = TryToFindBlob(camImage, anchorTarget);
            if (blobs.Count == 0)
            {
                PromptMessage(UserPrompt.eNotifyType.Warning, $"No suitable marker for {mName} point not found");
                return result;
            }

            result.Target = SpareOutTargetBlobFromList(anchorTarget.Centre, blobs, 0);
            if (result.Target == null)
            {
                PromptMessage(UserPrompt.eNotifyType.Warning, $"{mName} point not found");
                return result;
            }
            result.Remaining = blobs;

            return result;
        }


        private TargetProfile SetMarkerProperties(TargetProfile target, Blob found)
        {
            var size = (found.Rectangle.Width + found.Rectangle.Height)/2;
            target.Centre = new BlobCentre(found.CenterOfGravity.X, found.CenterOfGravity.Y, size);
            target.MinSize = (int)((found.Rectangle.Height + found.Rectangle.Width) / 2 * 0.7);

            return target;
        }

        private List<Blob> TryToFindBlob(BitmapSource camImage, TargetProfile target)
        {
            List<Blob> blobs;
            target.MinSize = Math.Min(camImage.PixelWidth, camImage.PixelHeight) / 10;
            target.MaxSize = Math.Min(camImage.PixelWidth, camImage.PixelHeight) / 3;
            do
            {
                blobs = PicAnalyzer.FindBlobs(target, camImage);
                if (blobs.Count == 0)
                {
                    PromptMessage(UserPrompt.eNotifyType.Note, $"Unable to find reference point with size {target.MinSize}. Reduzing size constraint");
                    target.MinSize = target.MinSize / 2;
                }

            } while (blobs.Count == 0 && target.MinSize > 5);
            return blobs;
        }

        private static BlobCentre GetBlopCentre(Blob blob)
        {
            return new BlobCentre(blob.CenterOfGravity.X, blob.CenterOfGravity.Y, (blob.Rectangle.Width + blob.Rectangle.Height) / 2);
        }




        private Blob SpareOutTargetBlobFromList(BlobCentre anchorTarget, List<Blob> blobs, int tolerance)
        {
            Blob target = null;

            foreach (Blob blob in blobs)
            {
                System.Drawing.Rectangle rect = blob.Rectangle;
                int x = rect.X + (rect.Width / 2);
                int y = rect.Y + (rect.Height / 2);

                if (((anchorTarget.X > (rect.X - tolerance)) && (anchorTarget.X < (rect.X + rect.Width + tolerance)))
                    && ((anchorTarget.Y > (rect.Y - tolerance)) && (anchorTarget.Y < (rect.Y + rect.Height + tolerance))))
                {
                    target = blob;
                    blobs.Remove(blob);
                    return target;
                }
            }
            return target;
        }

        private Action<UserPrompt.eNotifyType, string> _promptNewMessage_Handler;

        public delegate void MarkerSetEvent(BlobCentre centre);

        private MarkerSetEvent _anchroSetEvent;
        private MarkerSetEvent _MovingTipSetEvent;

        public MarkerScanner(Action<UserPrompt.eNotifyType, string> promptNewMessage_Handler, MarkerSetEvent anchroSet, MarkerSetEvent movingTipSet)
        {
            _promptNewMessage_Handler = promptNewMessage_Handler;

            _anchroSetEvent = anchroSet;
            _MovingTipSetEvent = movingTipSet;
        }


        public static BitmapImage Bitmap2BitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        public static System.Windows.Media.Color GetAvarageColor(BitmapSource bitmapSource, int centreX, int centreY, int radius)
        {
            System.Drawing.Bitmap avrg = new System.Drawing.Bitmap(1, 1);

            System.Drawing.Bitmap source = Analyzer.BitmapImage2Bitmap(bitmapSource as BitmapImage);

            // Clone a portion of the Bitmap object.

            int x = Math.Max(0, centreX - radius);
            x = Math.Min((int)(bitmapSource.Width - (2 * radius)), x);

            int y = Math.Max(0, centreY - radius);
            y = Math.Min((int)(bitmapSource.Height - (2 * radius)), y);

            int range = 2 * radius;

            System.Drawing.Rectangle cloneRect = new System.Drawing.Rectangle(x, y, range, range);

            System.Drawing.Imaging.PixelFormat format = source.PixelFormat;
            System.Drawing.Bitmap clonedSection = source.Clone(cloneRect, format);

            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(avrg))
            {
                // updated: the Interpolation mode needs to be set to 
                // HighQualityBilinear or HighQualityBicubic or this method
                // doesn't work at all.  With either setting, the results are
                // slightly different from the averaging method.
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(clonedSection, new System.Drawing.Rectangle(0, 0, 1, 1));
            }
            System.Drawing.Color pixel = avrg.GetPixel(0, 0);

            // pixel will contain average values for entire orig Bitmap
            System.Windows.Media.Color avrgCol = new System.Windows.Media.Color();
            avrgCol = System.Windows.Media.Color.FromRgb(pixel.R, pixel.G, pixel.B);
            return avrgCol;
        }

        void PromptMessage(UserPrompt.eNotifyType severity, string message)
        {
            _promptNewMessage_Handler?.Invoke(severity, message);
        }


        public class MarkerScan
        {
            public Blob Target;
            public List<Blob> Remaining;
        }

        public class AnchorTipPair
        {
            public DynamicProfile Anchor { get; }
            public DynamicProfile MovingTip { get; internal set; }

            public AnchorTipPair(TargetProfile profile)
            {
                Anchor = new DynamicProfile(profile);
            }

            public void AddTipProfile(TargetProfile profile)
            {
                MovingTip = new DynamicProfile(profile);
            }
        }

        enum OperationMode
        {
            missingData,
            AnchorIsSet,
            MovingTipIsSet,
        }
    }



}
