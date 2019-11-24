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

namespace MarkerScannerTest
{
    [TestClass]
    public class TrySetAnchorTest
    {
        bool PicFolderPresent = false;
        string Path2Exe;
        string Path2Proj;
        string Path2Image;


        public TrySetAnchorTest()
        {
            Path2Exe = AppDomain.CurrentDomain.BaseDirectory;
            Path2Proj = Directory.GetParent(Directory.GetParent(Path2Exe).ToString()).ToString();
            Path2Image = Path.Combine(Path2Proj, "GeneratedPics");
        }


        [TestMethod]
        public void TestMethod1()
        {
            ImageGenerator generator = new ImageGenerator("TestImage");
            Marker vAncor = new Marker()
            {
                C = new Point(300, 300),
                D = 10,
                Fill = Colors.Yellow,
                Border = Colors.Gray
            };

            //generator.AddAnchorToImage(vAncor);
            //var img = generator.RenderImage();
            var img = generator.TestImage;

            if (Directory.Exists(Path.Combine(Path2Proj, Path2Image)))
                PicFolderPresent = true;

            string fileName = Path.Combine(Path2Image, "empty.png");
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img.Source as BitmapSource));
            using (Stream stm = File.Create(fileName))  {
                encoder.Save(stm);
            }



            MarkerScanner sut = new MarkerScanner(PromptNewMessage_Handler);
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
