using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows;

using MeasureDeflection.Utils;
using MeasureDeflection.Processor;

namespace MarkerScannerTest.Utils
{
    public class ImageGenerator
    {
        const int DefaultWidth = 1280;
        const int DefaultHeight = 1024;

        DrawingVisual Visual;
        DrawingContext Context;

        public ImageGenerator()
        {
            DrawingVisual visual = new DrawingVisual();
            Context = visual.RenderOpen();

            var background = Brushes.White;
            Context.DrawRectangle(background, new Pen(), new Rect(0, 0, DefaultWidth, DefaultHeight));
        }

        ~ImageGenerator()
        {
            Context.Close();
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

            BitmapSource image = ((BitmapSource)bitmap);
            image.Freeze();
            
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
