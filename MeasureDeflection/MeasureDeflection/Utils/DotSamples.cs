using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeasureDeflection.Utils
{
    public class DotSamples
    {
        public int Sample { get; }
        public double AnchorX { get; }
        public double AnchorY { get; }
        public double TipX { get; }
        public double TipY { get; }

        public double Angle { get; }

        public DotSamples(int num, double aX, double aY, double tX, double tY, double ang)
        {
            Sample = num;
            AnchorX = aX;
            AnchorY = aY;
            TipX = tX;
            TipX = tY;
            Angle = ang;
        }

        public override string ToString()
        {
            return $"{Sample}\t{AnchorX:F2}\t{AnchorY:F2}\t{TipX:F2}\t{TipY:F2}\t{Angle:F3}";
        }
    }
}
