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
    public class TryToSetTipTest
    {
        public void TestSetup()
        {

        }

        EventSink Sink = new EventSink();

        [TestMethod]
        public void TestWithNoBlob()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            Sink.ResetPoints();
            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 300 }, Color = System.Drawing.Color.FromArgb(255, 255, 255) };

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetTip(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count == 1);
            Assert.IsTrue(Sink.MyLog[0].Contains("Please specify anchor first"));

            Assert.AreEqual(null, sut.Profile);
        }

        [TestMethod]
        public void TestWithTwoBlobAndCorrectPosition()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = new Point(300, 400),
                Diameter = 60,
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vAncor);

            Marker vTip = new Marker()
            {
                Center = new Point(900, 800),
                Diameter = 100,
                Fill = Colors.Blue,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = Extensions.SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 900, Y = 800 }, Color = Extensions.SetColor(vTip.Fill) };

            Sink.MyLog.Clear();
            Sink.ResetPoints();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Moving tip point set"));
            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(400, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(900, Sink.MovingTip.C.X, 2);
            Assert.AreEqual(800, Sink.MovingTip.C.Y, 2);
            Assert.AreEqual(100, Sink.MovingTip.D, 2);
        }

        [TestMethod]
        public void TestWithTwoBlobAndCorrectPositionAndFalsePosition()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = new Point(300, 400),
                Diameter = 60,
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vAncor);

            Marker vTip = new Marker()
            {
                Center = new Point(900, 800),
                Diameter = 100,
                Fill = Colors.Blue,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = Extensions.SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 800, Y = 700 }, Color = Extensions.SetColor(vTip.Fill) };

            Sink.MyLog.Clear();
            Sink.ResetPoints();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Moving tip point not found"));
            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(400, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(null, sut.Profile.MovingTip);
            Assert.AreEqual(null, Sink.MovingTip);
        }

        [TestMethod]
        public void TestWithTwoBlobAndCorrectPositionButWrongColor()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = new Point(300, 400),
                Diameter = 60,
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vAncor);

            Marker vTip = new Marker()
            {
                Center = new Point(900, 800),
                Diameter = 100,
                Fill = Colors.Blue,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = Extensions.SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 800, Y = 700 }, Color = Extensions.SetColor(vAncor.Fill) };

            Sink.MyLog.Clear();
            Sink.ResetPoints();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Moving tip point not found"));
            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(400, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(null, sut.Profile.MovingTip);
            Assert.AreEqual(null, Sink.MovingTip);
        }

        [TestMethod]
        public void TestWithThreeBlobAndOtherCorrectPositionButWrongColor()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = new Point(300, 400),
                Diameter = 60,
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vAncor);

            Marker vTip = new Marker()
            {
                Center = new Point(900, 800),
                Diameter = 100,
                Fill = Colors.Blue,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            Marker vTip2 = new Marker()
            {
                Center = new Point(200, 900),
                Diameter = 50,
                Fill = Colors.Red,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip2);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = Extensions.SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 200, Y = 900 }, Color = Extensions.SetColor(vTip2.Fill) };

            Sink.MyLog.Clear();
            Sink.ResetPoints();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Moving tip point set"));
            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(400, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(200, Sink.MovingTip.C.X, 2);
            Assert.AreEqual(900, Sink.MovingTip.C.Y, 2);
            Assert.AreEqual(50, Sink.MovingTip.D, 2);
        }
    }
}
