﻿using System;
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
        public System.Drawing.Color Color { get; set; }
        public int TargetSize { get; set; }
        public int SizeTolerance { get; set; }
        public int PositionTolerance { get; set; }

        public short FilterRadius { get; set; } = 50; // @todo inject from Higher level
        public int MinSize { get; set; }
        public int MaxSize { get; set; }

        public object Clone()
        {
            var clone = new TargetProfile();
            clone.Centre = (BlobCentre)Centre.Clone();
            clone.Color = Color;
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
        public Point C { get; set; } = new Point();

        /// <summary> X pixel position </summary>
        public double X { get; set; }

        /// <summary> Y pixel position </summary>
        public double Y { get; set; }

        /// <summary> Pixel diameter </summary>
        public double D { get; set; }

        public BlobCentre()
        {
            X = Y = D = 0;
            C = new Point();
        }

        public BlobCentre(double x, double y, double d)
        {
            X = x;
            Y = y;
            D = d;
            C = new Point(x, y);
        }
        public BlobCentre(Point p, double d)
        {
            X = p.X;
            Y = p.Y;
            D = d;
            C = p;
        }

        public object Clone()
        {
            var clone = new BlobCentre(X,Y,D);
            clone.C = new Point(C.X, C.Y);

            return clone;
        }
    }
}
