using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Interop;
using System.Windows;

using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Drawing.Drawing2D;

using MeasureDeflection.Utils;


namespace MeasureDeflection
{
    class ImageProcessor
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

        BlobProfile _anchorProfile;
        private BlobProfile AnchorProfile { get { return _anchorProfile; } set { _anchorProfile = value; } }

        BlobProfile _movingTipProfile;
        internal BlobProfile MovingTipProfile { get { return _movingTipProfile; } set { _movingTipProfile = value; } }

        SearchProfile lastAnchorTarget;
        SearchProfile lastMovingTipTarget;

        public void ResetInternals()
        {
            Operation = OperationMode.missingData;
        }

        internal ImageSource SetAnchorProperty(BitmapImage camImage, SearchProfile anchorTarget, out BlobCentre aP, out BlobCentre mtP)
        {
            AnchorProfile = new BlobProfile();
            lastAnchorTarget = anchorTarget;
            Bitmap porcessedImg = BitmapImage2Bitmap(camImage);
            BlobCounter blobCounter = AnalyzePicture(anchorTarget, porcessedImg, AnchorProfile);

            blobCounter.ProcessImage(porcessedImg);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            aP = null;
            mtP = null;
            _initFinished = false;

            if (blobs.Length >= 1)
            {
                Blob[] remains;
                Blob foundAnchor = ScanTargetBlob(anchorTarget.Centre, blobs, 0, out remains);
                if (foundAnchor != null)
                {
                    _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Note, "Anchor point set");
                    Operation = OperationMode.AnchorIsSet;
                    aP = GetBlopCentre(foundAnchor);
                    AnchorProfile.MinSize = (foundAnchor.Rectangle.Height + foundAnchor.Rectangle.Width) / 2 / 2;

                    if (remains.Length == 1)
                    {
                        _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Note, "Anchor point set - Remaining dot preset as moving tip");
                        Operation = OperationMode.MovingTipSetImplicitly;
                        mtP = GetBlopCentre(remains[0]);

                        lastMovingTipTarget = new SearchProfile();
                        lastMovingTipTarget.Centre = mtP;
                    }
                }
                else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
            }
            else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }

            return Bitmap2BitmapImage(porcessedImg);
        }

        internal ImageSource SetMovingTipProperty(BitmapImage camImage, SearchProfile movingTipTarget, out BlobCentre mtP)
        {
            mtP = null;
            Bitmap porcessedImg = BitmapImage2Bitmap(camImage);

            if ((int)Operation >= (int)OperationMode.AnchorIsSet)
            {
                MovingTipProfile = new BlobProfile();
                lastMovingTipTarget = movingTipTarget;
                BlobCounter blobCounter = AnalyzePicture(movingTipTarget, porcessedImg, MovingTipProfile);

                blobCounter.ProcessImage(porcessedImg);
                Blob[] blobs = blobCounter.GetObjectsInformation();

                if (blobs.Length >= 1)
                {
                    Blob[] remains;
                    Blob foundMovingTip = ScanTargetBlob(movingTipTarget.Centre, blobs, 0, out remains);
                    if (foundMovingTip != null)
                    {
                        _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Note, "Moving tip point set");
                        Operation = OperationMode.ExplicitSetMovingTip;
                        mtP = GetBlopCentre(foundMovingTip);
                        MovingTipProfile.MinSize = (foundMovingTip.Rectangle.Height + foundMovingTip.Rectangle.Width) / 2 / 2;
                    }
                    else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Moving tip point not found"); }
                }
                else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Moving tip point not found"); }
            }
            else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Please specify anchor first"); }
            return Bitmap2BitmapImage(porcessedImg);
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

        private BlobCounter AnalyzePicture(SearchProfile target, Bitmap porcessedImg, BlobProfile profile)
        {
            EuclideanColorFiltering filter = new EuclideanColorFiltering();
            filter.CenterColor = target.Color;
            filter.Radius = profile.FilterRadius;
            filter.ApplyInPlace(porcessedImg);

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinWidth = profile.MinSize;
            blobCounter.MinHeight = profile.MinSize;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;

            BitmapData objectsData = porcessedImg.LockBits(new Rectangle(0, 0, porcessedImg.Width, porcessedImg.Height), ImageLockMode.ReadOnly, porcessedImg.PixelFormat);
            Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));
            porcessedImg.UnlockBits(objectsData);
            return blobCounter;
        }

        public BitmapImage ProcessImage(BitmapImage camImage, AnchorTipPair targets, int tolerance, out BlobCentre aP, out BlobCentre mtP)
        {
            Bitmap porcessedImg = BitmapImage2Bitmap(camImage);
            Bitmap anchorView = (Bitmap)porcessedImg.Clone();
            Bitmap movingTipView = (Bitmap)porcessedImg.Clone();
            aP = null;
            mtP = null;

            switch (Operation)
            {
                case OperationMode.missingData:
                    _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found. Please specify Anchor point first");
                    break;

                case OperationMode.AnchorIsSet:
                    _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Moving point not found. Please specify Moving point first");
                    break;

                case OperationMode.MovingTipSetImplicitly:
                    {
                        BlobCounter blobCounter = AnalyzePicture(lastAnchorTarget, porcessedImg, AnchorProfile);
                        blobCounter.ProcessImage(porcessedImg);
                        Blob[] blobs = blobCounter.GetObjectsInformation();

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
                            else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        }
                        else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        return Bitmap2BitmapImage(porcessedImg);
                    }
                    break;

                case OperationMode.ExplicitSetMovingTip:
                    {
                        BlobCounter anchorBlobs = AnalyzePicture(lastAnchorTarget, anchorView, AnchorProfile);
                        anchorBlobs.ProcessImage(porcessedImg);
                        Blob[] blobs = anchorBlobs.GetObjectsInformation();

                        if (blobs.Length >= 1)
                        {
                            Blob[] remains;
                            Blob foundAnchor = ScanTargetBlob(lastAnchorTarget.Centre, blobs, 0, out remains);
                            if (foundAnchor != null)
                            {
                                aP = GetBlopCentre(foundAnchor);
                                lastAnchorTarget.Centre = aP;
                            }
                            else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        }
                        else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }

                        BlobCounter movingTipBlobs = AnalyzePicture(lastMovingTipTarget, movingTipView, MovingTipProfile);
                        movingTipBlobs.ProcessImage(porcessedImg);
                        blobs = movingTipBlobs.GetObjectsInformation();

                        if (blobs.Length >= 1)
                        {
                            Blob[] remains;
                            Blob foundAnchor = ScanTargetBlob(lastMovingTipTarget.Centre, blobs, 0, out remains);
                            if (foundAnchor != null)
                            {
                                mtP = GetBlopCentre(foundAnchor);
                                lastMovingTipTarget.Centre = mtP;
                            }
                            else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }
                        }
                        else { _promptNewMessage_Handler?.Invoke(UserPrompt.eNotifyType.Warning, "Anchor point not found"); }


                        //create a bitmap to hold the combined image
                        Bitmap combindedImage = new Bitmap(porcessedImg.Width, porcessedImg.Height);

                        //get a graphics object from the image so we can draw on it
                        using (Graphics g = Graphics.FromImage(combindedImage))
                        {
                            //set background color
                            g.Clear(System.Drawing.Color.Black);
                            anchorView.MakeTransparent(System.Drawing.Color.Black);
                            movingTipView.MakeTransparent(System.Drawing.Color.Black);
                            g.DrawImage(movingTipView, new System.Drawing.Rectangle(0, 0, movingTipView.Width, movingTipView.Height));
                            g.DrawImage(anchorView, new System.Drawing.Rectangle(0, 0, anchorView.Width, anchorView.Height));
                        }

                        return Bitmap2BitmapImage(combindedImage);
                    }
                    break;
            }

            return Bitmap2BitmapImage(porcessedImg);
        }

        private Blob ScanTargetBlob(BlobCentre anchorTarget, Blob[] blobs, int tolerance, out Blob[] remeins)
        {
            Blob target = null;
            remeins = new Blob[blobs.Length - 1];
            int j = 0;
            foreach (Blob blob in blobs)
            {
                Rectangle rect = blob.Rectangle;
                int x = rect.X + (rect.Width / 2);
                int y = rect.Y + (rect.Height / 2);

                if (((anchorTarget.X > (rect.X - tolerance)) && (anchorTarget.X < (rect.X + rect.Width + tolerance)))
                    && ((anchorTarget.Y > ( rect.Y - tolerance)) && (anchorTarget.Y < (rect.Y + rect.Height + tolerance))))
                {
                    target = blob;
                }
                else
                {
                    if (j < remeins.Length)
                    {
                        remeins[j++] = blob;
                    }
                }
            }
            return target;
        }


        BlobCentre _anchor = new BlobCentre();
        BlobCentre _movingTip = new BlobCentre();
        private Action<UserPrompt.eNotifyType, string> _promptNewMessage_Handler;

        public ImageProcessor(Action<UserPrompt.eNotifyType, string> promptNewMessage_Handler)
        {
            _promptNewMessage_Handler = promptNewMessage_Handler;
        }

        public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
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
            Bitmap avrg = new Bitmap(1, 1);

            Bitmap source = BitmapImage2Bitmap(bitmapSource as BitmapImage);

            // Clone a portion of the Bitmap object.

            int x = Math.Max(0, centreX - radius);
            x = Math.Min((int)(bitmapSource.Width - (2 * radius)), x);

            int y = Math.Max(0, centreY - radius);
            y = Math.Min((int)(bitmapSource.Height - (2 * radius)), y);

            int range = 2 * radius;

            Rectangle cloneRect = new Rectangle(x, y, range, range);

            System.Drawing.Imaging.PixelFormat format = source.PixelFormat;
            Bitmap clonedSection = source.Clone(cloneRect, format);

            using (Graphics g = Graphics.FromImage(avrg))
            {
                // updated: the Interpolation mode needs to be set to 
                // HighQualityBilinear or HighQualityBicubic or this method
                // doesn't work at all.  With either setting, the results are
                // slightly different from the averaging method.
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(clonedSection, new Rectangle(0, 0, 1, 1));
            }
            System.Drawing.Color pixel = avrg.GetPixel(0, 0);

            // pixel will contain average values for entire orig Bitmap
            System.Windows.Media.Color avrgCol = new System.Windows.Media.Color();
            avrgCol = System.Windows.Media.Color.FromRgb(pixel.R, pixel.G, pixel.B);
            return avrgCol;
        }


        public class BlobProfile
        {
            public short FilterRadius = 50;
            public int MinSize = 10;
        }

        public class AnchorTipPair
        {
            public SearchProfile Anchor { get; set; } = new SearchProfile();
            public SearchProfile MovingTip { get; set; } = new SearchProfile();
        }
        public class SearchProfile
        {
            public BlobCentre Centre { get; set; }
            public RGB Color { get; set; }
            public int TargetSize { get; set; }
            public int SizeTolerance { get; set; }
            public int PositionTolerance { get; set; }
        }

        public class BlobCentre
        {
            public double X { get; set; }
            public double Y { get; set; }

            public double D { get; set; }
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
