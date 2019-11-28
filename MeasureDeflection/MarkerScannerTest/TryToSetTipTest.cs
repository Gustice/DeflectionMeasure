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
        BlobCentre _anchor;
        BlobCentre _movingTip;

        public void TestSetup()
        {

        }

        [TestMethod]
        public void TestWithNoBlob()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name, this.GetType().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            var img = generator.RenderImage();
            TestImageHelper.SaveBitmap(test.FileName_Image, img);

            _anchor = _movingTip = new BlobCentre();
            var sut = new MarkerScanner(PromptNewMessage_Handler, OnAnchorSetEvent, OnMovingTipSetEvent);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 300 }, Color = System.Drawing.Color.FromArgb(255, 255, 255) };

            MyLog.Clear();
            var pImg = sut.TryToSetTip(img, profile);
            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(MyLog.Count == 1);
            Assert.IsTrue(MyLog[0].Contains("Please specify anchor first"));

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

            var sut = new MarkerScanner(PromptNewMessage_Handler, OnAnchorSetEvent, OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 900, Y = 800 }, Color = SetColor(vTip.Fill) };

            MyLog.Clear();
            _anchor = _movingTip = new BlobCentre();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(MyLog[MyLog.Count - 1].Contains("Moving tip point set"));
            Assert.AreEqual(300, _anchor.C.X, 2);
            Assert.AreEqual(400, _anchor.C.Y, 2);
            Assert.AreEqual(60, _anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(900, _movingTip.C.X, 2);
            Assert.AreEqual(800, _movingTip.C.Y, 2);
            Assert.AreEqual(100, _movingTip.D, 2);
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

            var sut = new MarkerScanner(PromptNewMessage_Handler, OnAnchorSetEvent, OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 800, Y = 700 }, Color = SetColor(vTip.Fill) };

            MyLog.Clear();
            _anchor = _movingTip = new BlobCentre();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(MyLog[MyLog.Count - 1].Contains("Moving tip point not found"));
            Assert.AreEqual(300, _anchor.C.X, 2);
            Assert.AreEqual(400, _anchor.C.Y, 2);
            Assert.AreEqual(60, _anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(null, sut.Profile.MovingTip);
            Assert.AreEqual(0, _movingTip.C.X, 2);
            Assert.AreEqual(0, _movingTip.C.Y, 2);
            Assert.AreEqual(0, _movingTip.D, 2);
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

            var sut = new MarkerScanner(PromptNewMessage_Handler, OnAnchorSetEvent, OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 800, Y = 700 }, Color = SetColor(vAncor.Fill) };

            MyLog.Clear();
            _anchor = _movingTip = new BlobCentre();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(MyLog[MyLog.Count - 1].Contains("Moving tip point not found"));
            Assert.AreEqual(300, _anchor.C.X, 2);
            Assert.AreEqual(400, _anchor.C.Y, 2);
            Assert.AreEqual(60, _anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(null, sut.Profile.MovingTip);
            Assert.AreEqual(0, _movingTip.C.X, 2);
            Assert.AreEqual(0, _movingTip.C.Y, 2);
            Assert.AreEqual(0, _movingTip.D, 2);
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

            var sut = new MarkerScanner(PromptNewMessage_Handler, OnAnchorSetEvent, OnMovingTipSetEvent);
            var aProfile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 400 }, Color = SetColor(vAncor.Fill) };
            var mtProfile = new TargetProfile() { Centre = new BlobCentre() { X = 200, Y = 900 }, Color = SetColor(vTip2.Fill) };

            MyLog.Clear();
            _anchor = _movingTip = new BlobCentre();
            sut.TryToSetAnchor(img, aProfile);
            var pImg = sut.TryToSetTip(img, mtProfile);

            TestImageHelper.SaveBitmap(test.FileName_Processed, pImg as BitmapSource);

            Assert.IsTrue(MyLog[MyLog.Count - 1].Contains("Moving tip point set"));
            Assert.AreEqual(300, _anchor.C.X, 2);
            Assert.AreEqual(400, _anchor.C.Y, 2);
            Assert.AreEqual(60, _anchor.D, 2);

            Assert.AreEqual(aProfile.Centre.X, sut.Profile.Anchor.Initial.Centre.X);
            Assert.AreEqual(aProfile.Centre.Y, sut.Profile.Anchor.Initial.Centre.Y);
            Assert.AreEqual(aProfile.Centre.D, sut.Profile.Anchor.Initial.Centre.D);

            Assert.AreEqual(200, _movingTip.C.X, 2);
            Assert.AreEqual(900, _movingTip.C.Y, 2);
            Assert.AreEqual(50, _movingTip.D, 2);
        }

        void OnAnchorSetEvent(BlobCentre anchor)
        {
            _anchor = anchor;
        }

        void OnMovingTipSetEvent(BlobCentre movingTip)
        {
            _movingTip = movingTip;
        }

        System.Drawing.Color SetColor(Color color)
        {
            System.Drawing.Color c = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            return c;
        }


        List<string> MyLog = new List<string>();

        /// <summary>
        /// Promt handler for user notifications
        /// </summary>
        /// <param name="type">Notification urgency</param>
        /// <param name="message">Message</param>
        public void PromptNewMessage_Handler(UserPrompt.eNotifyType type, string message)
        {
            var prompt = $"'{type,8}': {message}";
            MyLog.Add(prompt);
        }

    }
}
