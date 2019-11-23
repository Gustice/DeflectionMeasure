using MeasureDeflection.Utils.Interfaces;
using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;

namespace MeasureDeflection.Utils
{
    /// <summary>
    /// Saves ands laods Pictures to/from filessystem
    /// </summary>
    public class FileHandler : IFileHandler
    {
        public string DefaultPath { get; set; } = @"C:\Users";

        /// <inheritdoc />
        public BitmapImage LoadImage()
        {
            BitmapImage image = null;

            OpenFileDialog picker = new OpenFileDialog();
            picker.InitialDirectory = DefaultPath;
            picker.DefaultExt = ".jpg";
            picker.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|PNG Image|*.png";

            bool? result = picker.ShowDialog();
            if (result == true)
            {
                Uri testFramePath = new Uri(picker.FileName);
                image = new BitmapImage(testFramePath);
            }

            return image;
        }

        /// <inheritdoc />
        public void SaveImage(BitmapImage image)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image as BitmapImage));
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "Sample###";
            dlg.DefaultExt = ".jpg"; // Default file extension
            dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";

            if (dlg.ShowDialog() == true)
                using (var stream = dlg.OpenFile())
                {
                    encoder.Save(stream);
                }
        }

    }
}
