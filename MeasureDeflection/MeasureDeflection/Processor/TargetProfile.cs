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


namespace MeasureDeflection.Processor
{
    public class DynamicProfile
    {
        private TargetProfile current = new TargetProfile();
        private TargetProfile last;

        public DynamicProfile(TargetProfile profile)
        {
            Initial = (TargetProfile)profile.Clone();
            Current = profile;
            Last = (TargetProfile)profile.Clone();
        }

        public TargetProfile Initial { get; }

        public TargetProfile Last { 
            get 
            {
                return last; 
            } 
            private set => last = value; 
        }

        public TargetProfile Current
        {
            get => current;
            set
            {
                Last = current;
                current = value;
            }
        }

        public void SaveCurrentToOld()
        {
            Last = (TargetProfile)Current.Clone();
        }
    }
    

    public class TargetProfile : ICloneable
    {
        public BlobCentre Centre { get; set; }
        public RGB Color { get; set; }
        public int TargetSize { get; set; }
        public int SizeTolerance { get; set; }
        public int PositionTolerance { get; set; }

        public short FilterRadius { get; set; } = 50;
        public int MinSize { get; set; };



        public object Clone()
        {
            var clone = new TargetProfile();
            clone.Centre = (BlobCentre)Centre.Clone();
            clone.Color = new RGB(Color.Red, Color.Green, Color.Blue);
            clone.TargetSize = TargetSize;
            clone.SizeTolerance = SizeTolerance;
            clone.PositionTolerance = PositionTolerance;
            clone.FilterRadius = FilterRadius;
            clone.MinSize = MinSize;

            return clone; 
        }
    }

    public class BlobCentre : ICloneable
    {
        /// <summary> Center Point </summary>
        Point C { get; set; } = new Point();

        /// <summary> X position </summary>
        public double X { get; set; }

        /// <summary> Y position </summary>
        public double Y { get; set; }

        /// <summary> Diameter </summary>
        public double D { get; set; }

        public object Clone()
        {
            var clone = new BlobCentre();
            clone.C = new Point(C.X, C.Y);
            clone.X = X;
            clone.Y = Y;
            clone.D = D;

            return clone;
        }
    }
}
