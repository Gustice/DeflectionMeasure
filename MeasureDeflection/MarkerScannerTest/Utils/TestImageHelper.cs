using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MarkerScannerTest.Utils
{
    

    class TestImageHelper
    {

        static string Path2Exe;
        static string Path2Proj;
        static string Path2Image;

        static bool PicFolderPresent = false;
        static bool FirstInstanceDone = false;

        TestImageHelper()
        {
            Path2Exe = AppDomain.CurrentDomain.BaseDirectory;
            Path2Proj = Directory.GetParent(Directory.GetParent(Path2Exe).ToString()).ToString();
            Path2Image = Path.Combine(Path2Proj, "GeneratedTestPics");

            if (Directory.Exists(Path.Combine(Path2Proj, Path2Image)))
                PicFolderPresent = true;

            FirstInstanceDone = true;
        }

        public static void SaveBitmap(string fName, BitmapSource img)
        {
            if (FirstInstanceDone == false)
                _ = new TestImageHelper();

            if (PicFolderPresent == false)
                return;

            string path = Path.Combine(Path2Image, fName);
            string dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var encoder = new PngBitmapEncoder();
            var frame = BitmapFrame.Create(img);
            encoder.Frames.Add(frame);

            using (Stream stm = File.Create(path)) {
                encoder.Save(stm);
            }
        }
    }
}
