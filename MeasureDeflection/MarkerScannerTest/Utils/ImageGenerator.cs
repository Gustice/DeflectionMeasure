using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;

using MeasureDeflection.Utils;
using MeasureDeflection.Processor;
using System.Globalization;

namespace MarkerScannerTest.Utils
{
    public class ImageGenerator
    {
        const int DefaultWidth = 1280;
        const int DefaultHeight = 1024;


        public Image TestImage { get; private set; }
        DrawingVisual Visual;
        DrawingContext Context;

        public ImageGenerator(string description)
        {
            TestImage = new Image();
            FormattedText text = new FormattedText(description,
                    new CultureInfo("de-de"),
                    FlowDirection.LeftToRight,
                    new Typeface("Cambria"),
                    12,
                    Brushes.Gray, 
                    VisualTreeHelper.GetDpi(TestImage).PixelsPerDip);
            
            Visual = new DrawingVisual();
            
            Context = Visual.RenderOpen();

            var background = Brushes.Yellow;
            Context.DrawRectangle(background, new Pen(Brushes.Red,3), new Rect(0, 0, DefaultWidth, DefaultHeight));
            Context.DrawText(text, new Point(2, 2));

            Context.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap(DefaultWidth, DefaultHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(Visual);
            TestImage.Source = bmp;
        }

        public void AddAnchorToImage(Marker anchor)
        {
                var fill = new SolidColorBrush(anchor.Fill);
                var border = new SolidColorBrush(anchor.Border);
                Pen stroke = new Pen(border, 3);
                
                Context.DrawEllipse(fill, stroke, anchor.C, anchor.D/2, anchor.D / 2);
        }


        public BitmapSource RenderImage()
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(DefaultWidth, DefaultHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(Visual);
            
            BitmapSource image = bitmap;
            image.Freeze();
            Context.Close();
            return image;

        }





    }



    public class Marker
    {
        public Point C { get; set; }
        public double D { get; set; }
        public Color Fill { get; set; }
        public Color Border { get; set; }

    }
}
