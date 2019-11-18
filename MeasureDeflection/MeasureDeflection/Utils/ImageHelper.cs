using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace MeasureDeflection.Utils
{
    public class ImageHelper
    {

        /// <summary>
        /// Get pixel x and y position of mouse cursor relativ to an certain image
        /// </summary>
        /// <param name="imageFrame"></param>
        /// <param name="hoverPos"></param>
        /// <returns>Point in Picture</returns>
        public static Point GetXYPosInFrame(Image imageFrame, Point hoverPos)
        {
            BitmapSource bitmapSource = imageFrame.Source as BitmapSource;
            double x, y;

            x = PixelPosition(hoverPos.X, bitmapSource.PixelWidth, imageFrame.ActualWidth);
            y = PixelPosition(hoverPos.Y, bitmapSource.PixelHeight, imageFrame.ActualHeight);

            Point pos = new Point(x, y);
            return pos;
        }

        /// <summary>
        /// Get relative Position on each direction
        /// </summary>
        private static double PixelPosition(double relPos, int pixels, double range)
        {
            relPos = relPos * pixels / range;
            
            relPos = Math.Min(relPos, pixels - 1);
            relPos = Math.Max(relPos, 0);

            return relPos;
        }
    }
}
