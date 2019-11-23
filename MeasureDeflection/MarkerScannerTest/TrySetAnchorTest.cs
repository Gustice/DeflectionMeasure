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

namespace MarkerScannerTest
{
    [TestClass]
    public class TrySetAnchorTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            ImageGenerator generator = new ImageGenerator();
            Marker vAncor = new Marker()
            {
                C = new Point(300, 300),
                D = 10,
                Fill = Colors.Yellow,
                Border = Colors.Gray
            };

            generator.AddAnchorToImage(vAncor);
            var img = generator.RenderImage();


            FileStream stream = new FileStream("empty.jpg", FileMode.Create);
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img as BitmapImage));
            encoder.Save(stream);

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
