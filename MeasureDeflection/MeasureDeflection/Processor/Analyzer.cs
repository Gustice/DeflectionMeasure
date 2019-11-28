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
using MeasureDeflection.Properties;

namespace MeasureDeflection.Processor
{
    class Analyzer
    {
        public System.Drawing.Bitmap PorcessedImg { get; private set; }

        public Analyzer()
        {
            PorcessedImg = new System.Drawing.Bitmap(Resources.Error);
        }

        public List<Blob> FindBlobs(TargetProfile current, BitmapSource camImage)
        {
            PorcessedImg = BitmapImage2Bitmap(camImage);
            BlobCounter blobCounter = AnalyzePicture(current, PorcessedImg);
            blobCounter.ProcessImage(PorcessedImg);
            return blobCounter.GetObjectsInformation().ToList<Blob>();
        }

        public BlobCounter AnalyzePicture(TargetProfile target, System.Drawing.Bitmap porcessedImg)
        {
            EuclideanColorFiltering filter = new EuclideanColorFiltering();
            filter.CenterColor = new RGB(target.Color);
            filter.Radius = target.FilterRadius;
            filter.ApplyInPlace(porcessedImg);

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinWidth = target.MinSize;
            blobCounter.MinHeight = target.MinSize;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
            blobCounter.MaxWidth = blobCounter.MaxHeight = target.MaxSize;

            System.Drawing.Imaging.BitmapData objectsData = porcessedImg.LockBits(new System.Drawing.Rectangle(0, 0, porcessedImg.Width, porcessedImg.Height), ImageLockMode.ReadOnly, porcessedImg.PixelFormat);
            Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
            UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));
            porcessedImg.UnlockBits(objectsData);
            return blobCounter;
        }

        public static System.Drawing.Bitmap BitmapImage2Bitmap(BitmapSource bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }
    }
}
