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
    public class TrySetAnchorTest
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
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count > 2);
            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 2].Contains("Unable to find reference point with size"));
            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("No suitable marker for Anchor point not found"));

            Assert.AreEqual(null, sut.Profile);
        }

        [TestMethod]
        public void TestWithOneBlobAndCorrectPosition()
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

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count >= 1);
            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Anchor point set"));

            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(300, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(profile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(profile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(profile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(null, sut.Profile.MovingTip);
        }

        [TestMethod]
        public void TestWithOneBlobAndFalsePosition()
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
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 900, Y = 900}, Color = Extensions.SetColor(vAncor.Fill) };

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count >= 1);
            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Anchor point not found"));

            Assert.AreEqual(null, sut.Profile);
        }

        [TestMethod]
        public void TestWithOneBlobAndCorrectPositionButWrongColor()
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
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 300 }, Color = Extensions.SetColor(Colors.Blue) };

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog.Count >= 1);
            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("No suitable marker for Anchor point not found"));

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
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            Sink.ResetPoints();
            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = Extensions.SetColor(vAncor.Fill) };

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Anchor point set - Remaining dot preset as moving tip"));
            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(400, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(profile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(profile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(profile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(900, Sink.MovingTip.C.X, 2);
            Assert.AreEqual(800, Sink.MovingTip.C.Y, 2);
            Assert.AreEqual(100, Sink.MovingTip.D, 2);
        }

        [TestMethod]
        public void TestWithTwoBlobAndOtherCorrectPosition()
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
                Fill = Colors.Green,
                Border = Colors.Gray
            };
            generator.AddMarkerToImage(vTip);

            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            Sink.ResetPoints();
            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 900, Y = 800 }, Color = Extensions.SetColor(vAncor.Fill) };

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Anchor point set - Remaining dot preset as moving tip"));
            Assert.AreEqual(900, Sink.Anchor.C.X, 2);
            Assert.AreEqual(800, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(100, Sink.Anchor.D, 2);

            Assert.AreEqual(profile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(profile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(profile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(300, Sink.MovingTip.C.X, 2);
            Assert.AreEqual(400, Sink.MovingTip.C.Y, 2);
            Assert.AreEqual(60, Sink.MovingTip.D, 2);
        }

        [TestMethod]
        public void TestWithTwoDifferentBlobsAndCorrectPosition()
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

            Sink.ResetPoints();
            var sut = new MarkerScanner(Sink.PromptNewMessage_Handler, Sink.OnAnchorSetEvent, Sink.OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = Extensions.SetColor(vAncor.Fill) };

            Sink.MyLog.Clear();
            var pImg = sut.TryToSetAnchor(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(Sink.MyLog[Sink.MyLog.Count - 1].Contains("Anchor point set"));
            Assert.AreEqual(300, Sink.Anchor.C.X, 2);
            Assert.AreEqual(400, Sink.Anchor.C.Y, 2);
            Assert.AreEqual(60, Sink.Anchor.D, 2);

            Assert.AreEqual(profile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(profile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(profile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(null, sut.Profile.MovingTip);
        }
    }
}
