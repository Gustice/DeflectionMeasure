using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Windows.Media.Imaging;

using MeasureDeflection;
using MeasureDeflection.Utils;
using MeasureDeflection.Processor;

using MarkerScannerTest.Utils;
using System.IO;
using MarkerScannerTest.Properties;
using System.Reflection;

namespace MarkerScannerTest
{
    [TestClass]
    public class ProcessImageTest
    {
        EventSink Sink = new EventSink();

        public void TestSetup()
        {

        }

        [TestMethod]
        public void TestWithNoPreset()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            Sink.ResetPoints();
            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 300 }, Color = System.Drawing.Color.FromArgb(255, 255, 255) };

            Sink.MyLog.Clear();
            var pImg = sut.ProcessImage(img, 3);
            
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count == 1);
            Assert.IsTrue(Sink.MyLog[0].Contains("Anchor point not found. Please specify Anchor point first"));

            Assert.AreEqual(null, sut.Profile);
        }

        [TestMethod]
        public void TestWithNoTipPreset()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = new Point(300, 300),
                Diameter = 60,
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vAncor);
            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            Sink.ResetPoints();
            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 300 }, Color = Extensions.SetColor(vAncor.Fill) };

            sut.TryToSetAnchor(img, profile);
            Sink.MyLog.Clear();
            var pImg = sut.ProcessImage(img, 3);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count == 1);
            Assert.IsTrue(Sink.MyLog[0].Contains("Moving point not found. Please specify Moving point first"));
        }

        [TestMethod]
        public void TestWithCorrectPreset()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = ImageGenerator.DefaultCentre,
                Diameter = 60,
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vAncor);

            Marker vTip = new Marker()
            {
                Center = ImageGenerator.DefaultCentre + new Vector(0, ImageGenerator.DefaultRadius),
                Diameter = 100,
                Fill = Colors.Blue,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre(vAncor.Center, 0), Color = Extensions.SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre(vTip.Center, 0), Color = Extensions.SetColor(vTip.Fill) };

            Sink.MyLog.Clear();
            Sink.ResetPoints();
            sut.TryToSetAnchor(img, aProfile);
            sut.TryToSetTip(img, mtProfile);

            var pImg = sut.ProcessImage(img, 1);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.AreEqual(vAncor.Center.X, Sink.Anchor.C.X, 2);
            Assert.AreEqual(vAncor.Center.Y, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(vAncor.Diameter, Sink.Anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(vTip.Center.X, Sink.MovingTip.C.X, 2);
            Assert.AreEqual(vTip.Center.Y, Sink.MovingTip.C.Y, 2);
            Assert.AreEqual(vTip.Diameter, Sink.MovingTip.D, 2);
        }

    }
}
