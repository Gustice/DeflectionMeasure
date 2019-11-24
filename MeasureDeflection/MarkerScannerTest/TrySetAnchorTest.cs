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
        bool PicFolderPresent = false;
        string Path2Exe;
        string Path2Proj;
        string Path2Image;

        private string myVar;

        public string MyProperty
        {
            get { return myVar; }
            set { myVar = value; }
        }



        public TrySetAnchorTest()
        {
            Path2Exe = AppDomain.CurrentDomain.BaseDirectory;
            Path2Proj = Directory.GetParent(Directory.GetParent(Path2Exe).ToString()).ToString();
            Path2Image = Path.Combine(Path2Proj, "GeneratedTestPics");

            if (Directory.Exists(Path.Combine(Path2Proj, Path2Image)))
                PicFolderPresent = true;
        }


        [TestMethod]
        public void TestMethod1()
        {
            var test = new TestDescription(MethodBase.GetCurrentMethod().Name);
            ImageGenerator generator = new ImageGenerator(test.Name);
            Marker vAncor = new Marker()
            {
                Center = new Point(300, 300),
                Diameter = 20,
                Fill = Colors.Yellow,
                Border = Colors.Gray
            };

            generator.AddAnchorToImage(vAncor);
            var img = generator.RenderImage();

            string fileName = Path.Combine(Path2Image, test.FileName_Image);
            
            var encoder = new PngBitmapEncoder();
            var frame = BitmapFrame.Create(img.Source as BitmapSource);
            encoder.Frames.Add(frame);

            using (Stream stm = File.Create(fileName))  {
                encoder.Save(stm);
            }

            var sut = new MarkerScanner(PromptNewMessage_Handler);
            var profile = new TargetProfile() { Centre = new BlobCentre() { X = 300, Y = 300}, Color = SetColor(vAncor.Fill) };
            var pImg = sut.TryToSetAnchor(img.Source as BitmapImage, profile);

            var encoder2 = new PngBitmapEncoder();
            var frame2 = BitmapFrame.Create(pImg as BitmapSource);
            encoder.Frames.Add(frame2);

            string fileNameProcessed = Path.Combine(Path2Image, test.FileName_Processed);
            using (Stream stm = File.Create(fileNameProcessed))
            {
                encoder.Save(stm);
            }
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

    class TestDescription
    {
        public const string FileExtension = "png";

        public string Name { get; }

        public string FileName_Image => $"{Name}.{FileExtension}";
        public string FileName_Processed => $"{Name}_Processed.{FileExtension}";

        public TestDescription(string name)
        {
            Name = name;
        }
    }


}
