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
        public const int DefaultWidth = 1280;
        public const int DefaultHeight = 1024;

        BitmapSource Image;
        DrawingVisual Visual;
        DrawingContext Context;

        public ImageGenerator(string description)
        {
            Image = new BitmapImage();
            FormattedText text = new FormattedText(description,
                    new CultureInfo("de-de"),
                    FlowDirection.LeftToRight,
                    new Typeface("Cambria"),
                    12,
                    Brushes.Gray, 
                    96 );
            text.TextAlignment = TextAlignment.Right;
            
            Visual = new DrawingVisual();
            
            Context = Visual.RenderOpen();
            Context.DrawRectangle(Brushes.White, new Pen(Brushes.Black,3), new Rect(0, 0, DefaultWidth, DefaultHeight));

            Context.DrawText(text, new Point(DefaultWidth-2, DefaultHeight-2-text.Height));
        }

        public void AddAnchorToImage(Marker anchor)
        {
                var fill = new SolidColorBrush(anchor.Fill);
                var border = new SolidColorBrush(anchor.Border);
                Pen stroke = new Pen(border, 1);
                
                Context.DrawEllipse(fill, stroke, anchor.Center, anchor.Diameter/2, anchor.Diameter / 2);
        }


        public BitmapSource RenderImage()
        {
            Context.Close();
            RenderTargetBitmap bmp = new RenderTargetBitmap(DefaultWidth, DefaultHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(Visual);

            //Image.Source = bmp;
            //Image.Source.Freeze();
            Image = bmp;
            return Image;
        }





    }



    public class Marker
    {
        public Point Center { get; set; }
        public double Diameter { get; set; }
        public Color Fill { get; set; }
        public Color Border { get; set; }

    }
}
