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
            set {
                _operation = value;

                _initFinished = false;
                if ((int)_operation >= (int)OperationMode.MovingTipSetImplicitly)
                    _initFinished = true;
            }
        }

        TargetProfile lastAnchorTarget;
        TargetProfile lastMovingTipTarget;
        AnchorTipPair Profile { get; set; }


        /// <summary>
        /// Reset internal to get a clean object to start a new image processing approach
        /// </summary>
        public void ResetInternals()
        {
            Operation = OperationMode.missingData;
        }

        Analyzer PicAnalyzer = new Analyzer();

        public BlobCentre AnchorPoint { get; private set; }
        public BlobCentre TipPoint { get; private set; }


        // Note this Function is optimized for round blobs
        public ImageSource TryToSetAnchor(BitmapSource camImage, TargetProfile anchorTarget)
        {
            _initFinished = false;
            AnchorPoint = TipPoint = null;
            Blob[] blobs;

            blobs = TryToFindBlob(camImage, anchorTarget);
            if (blobs.Length == 0)
            {
                PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found");
                return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
            }

            Blob[] remains;
            Blob foundAnchor = ScanTargetBlob(anchorTarget.Centre, blobs, 0, out remains);
            if (foundAnchor != null)
            {
                Operation = OperationMode.AnchorIsSet;
                PromptMessage(UserPrompt.eNotifyType.Note, "Anchor point set");

                AnchorPoint = GetBlopCentre(foundAnchor);
                anchorTarget.MinSize = (int)((foundAnchor.Rectangle.Height + foundAnchor.Rectangle.Width) / 2 * 0.7);
                Profile = new AnchorTipPair(anchorTarget);

                if (remains.Length == 1)
                {
                    PromptMessage(UserPrompt.eNotifyType.Note, "Anchor point set - Remaining dot preset as moving tip");
                    Operation = OperationMode.MovingTipSetImplicitly;

                    TargetProfile tipTarget = (TargetProfile)anchorTarget.Clone();
                    tipTarget.Centre = GetBlopCentre(remains[0]);

                    Profile.AddTipProfile(tipTarget);
                }
            }
            else
                PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found");

            return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
        }

        private Blob[] TryToFindBlob(BitmapSource camImage, TargetProfile target)
        {
            Blob[] blobs;
            target.MinSize = Math.Min(camImage.PixelWidth, camImage.PixelHeight) / 10;
            do
            {
                blobs = PicAnalyzer.FindBlobs(target, camImage);
                if (blobs.Length == 0)
                {
                    PromptMessage(UserPrompt.eNotifyType.Note, $"Unable to find reference point with size {target.MinSize}. Reduzing size constraint");
                    target.MinSize = target.MinSize / 2;
                }

            } while (blobs.Length > 0 && target.MinSize > 5);
            return blobs;
        }

        public ImageSource TryToSetTip(BitmapImage camImage, TargetProfile movingTipTarget)
        {
            TipPoint = null;

            if ((int)Operation >= (int)OperationMode.AnchorIsSet)
            {
                PromptMessage(UserPrompt.eNotifyType.Warning, "Please specify anchor first");
                return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
            }

            Profile.AddTipProfile(movingTipTarget);
            Blob[] blobs = PicAnalyzer.FindBlobs(Profile.MovingTip.Current, camImage);

            if (blobs.Length == 0)
            {
                PromptMessage(UserPrompt.eNotifyType.Warning, "Moving tip point not found");
                return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
            }

            Blob[] remains;
            Blob foundMovingTip = ScanTargetBlob(movingTipTarget.Centre, blobs, 0, out remains);
            if (foundMovingTip != null)
            {
                PromptMessage(UserPrompt.eNotifyType.Note, "Moving tip point set");
                Operation = OperationMode.ExplicitSetMovingTip;

                movingTipTarget.MinSize = (int)((foundMovingTip.Rectangle.Height + foundMovingTip.Rectangle.Width) / 2 * 0.7);

                TargetProfile tipTarget = (TargetProfile)movingTipTarget.Clone();
                tipTarget.Centre = GetBlopCentre(foundMovingTip);
                Profile.AddTipProfile(tipTarget);
            }
            else { PromptMessage(UserPrompt.eNotifyType.Warning, "Moving tip point not found"); }


            return Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
        }

        private static BlobCentre GetBlopCentre(Blob blob)
        {
            BlobCentre aP = new BlobCentre() {
                X = blob.CenterOfGravity.X,
                Y = blob.CenterOfGravity.Y,
                D = (blob.Rectangle.Width + blob.Rectangle.Height) / 2
            };

            return aP;
        }





        public RenderTargetBitmap ProcessImage(BitmapImage camImage, double tolerancefactor, out BlobCentre aP, out BlobCentre mtP)
        {
            aP = null;
            mtP = null;
            int tolerance = (int)(Profile.Anchor.Initial.Centre.D * tolerancefactor);

            BitmapImage filterImage = null;

            switch (Operation)
            {
                case OperationMode.missingData:
                    PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found. Please specify Anchor point first");
                    break;

                case OperationMode.AnchorIsSet:
                    PromptMessage(UserPrompt.eNotifyType.Warning, "Moving point not found. Please specify Moving point first");
                    break;

                case OperationMode.MovingTipSetImplicitly:
                    {
                        Blob[] blobs = PicAnalyzer.FindBlobs(Profile.Anchor.Last, camImage);

                        if (blobs.Length >= 1)
                        {
                            Blob[] remains;
                            Blob foundAnchor = ScanTargetBlob(lastAnchorTarget.Centre, blobs, tolerance, out remains);
                            if (foundAnchor != null)
                            {
                                aP = GetBlopCentre(foundAnchor);
                                lastAnchorTarget.Centre = aP;

                                if (remains.Length >= 1)
                                {
                                    Blob foundMovingTip = ScanTargetBlob(lastMovingTipTarget.Centre, remains, tolerance,  out remains);
                                    if (foundMovingTip != null)
                                    {
                                        mtP = GetBlopCentre(foundMovingTip);
                                        lastMovingTipTarget.Centre = mtP;
                                    }
                                }
                            }
                            else { PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        }
                        else { PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        filterImage = Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);
                    }
                    break;

                case OperationMode.ExplicitSetMovingTip:
                    {
                        Blob[] blobs = PicAnalyzer.FindBlobs(Profile.Anchor.Last, camImage);
                        var anchorView = (System.Drawing.Bitmap)PicAnalyzer.PorcessedImg.Clone();


                        if (blobs.Length >= 1)
                        {
                            Blob[] remains;
                            Blob foundAnchor = ScanTargetBlob(lastAnchorTarget.Centre, blobs, 0, out remains);
                            if (foundAnchor != null)
                            {
                                aP = GetBlopCentre(foundAnchor);
                                lastAnchorTarget.Centre = aP;
                            }
                            else { PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        }
                        else { PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }

                        blobs = PicAnalyzer.FindBlobs(Profile.Anchor.Last, camImage);
                        var movingTipView = (System.Drawing.Bitmap)PicAnalyzer.PorcessedImg.Clone();

                        if (blobs.Length >= 1)
                        {
                            Blob[] remains;
                            Blob foundAnchor = ScanTargetBlob(lastMovingTipTarget.Centre, blobs, 0, out remains);
                            if (foundAnchor != null)
                            {
                                mtP = GetBlopCentre(foundAnchor);
                                lastMovingTipTarget.Centre = mtP;
                            }
                            else { PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        }
                        else { PromptMessage(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }


                        //create a bitmap to hold the combined image
                        System.Drawing.Bitmap combindedImage = new System.Drawing.Bitmap(anchorView.Width, anchorView.Height);

                        //get a graphics object from the image so we can draw on it
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
                    }
                    break;
            }

            if(filterImage == null)
            {
                filterImage = Bitmap2BitmapImage(PicAnalyzer.PorcessedImg);

                DrawingVisual temp = new DrawingVisual();
                using (DrawingContext dc = temp.RenderOpen())
                {
                    dc.DrawImage(filterImage, new Rect(0, 0, filterImage.PixelWidth, filterImage.PixelHeight));
                }
                RenderTargetBitmap rtbb = new RenderTargetBitmap(filterImage.PixelWidth, filterImage.PixelHeight, 96, 96, PixelFormats.Pbgra32);
                rtbb.Render(temp);

                return rtbb;
            }


            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                Pen stroke = new Pen(Brushes.White, 3);
                dc.DrawImage(filterImage, new Rect(0, 0, filterImage.PixelWidth, filterImage.PixelHeight));

                if ((aP != null) && (mtP != null))
                {
                    Point anchor = new Point(aP.X, aP.Y);

                    dc.DrawEllipse(Brushes.Yellow, stroke, anchor, 10, 10);
                    if (mtP != null)
                    {
                        Point movingTip = new Point(mtP.X, mtP.Y);

                        dc.DrawEllipse(Brushes.White, stroke, movingTip, 10, 10);
                        dc.DrawLine(stroke, anchor, movingTip);
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

        private Blob ScanTargetBlob(BlobCentre anchorTarget, Blob[] blobs, int tolerance, out Blob[] remeins)
        {
            Blob target = null;

            List<Blob> rest = new List<Blob>();
            
            foreach (Blob blob in blobs)
            {
                System.Drawing.Rectangle rect = blob.Rectangle;
                int x = rect.X + (rect.Width / 2);
                int y = rect.Y + (rect.Height / 2);

                if (((anchorTarget.X > (rect.X - tolerance)) && (anchorTarget.X < (rect.X + rect.Width + tolerance)))
                    && ((anchorTarget.Y > ( rect.Y - tolerance)) && (anchorTarget.Y < (rect.Y + rect.Height + tolerance))))
                    target = blob;
                else
                    rest.Add(blob);
            }

            remeins = rest.ToArray();
            return target;
        }


        BlobCentre _anchor = new BlobCentre();
        BlobCentre _movingTip = new BlobCentre();
        private Action<UserPrompt.eNotifyType, string> _promptNewMessage_Handler;

        public MarkerScanner(Action<UserPrompt.eNotifyType, string> promptNewMessage_Handler)
        {
            _promptNewMessage_Handler = promptNewMessage_Handler;
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
            MovingTipSetImplicitly,
            ExplicitSetMovingTip,
        }
    }



}
