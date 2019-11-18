using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MeasureDeflection.Utils.Interfaces
{
    /// <summary>
    /// Loading and saving pictures
    /// </summary>
    public interface IFileHandler
    {
        /// <summary>
        /// Default path for File dialogue
        /// </summary>
        string DefaultPath { get; set; }

        /// <summary>
        /// Loads image from filesystem to analyse it
        /// </summary>
        /// <returns>Loaded image</returns>
        BitmapImage LoadImage();

        /// <summary>
        /// Saves current taken image
        /// </summary>
        /// <param name="image">Image to be saved</param>
        void SaveImage(BitmapImage image);

    }
}
