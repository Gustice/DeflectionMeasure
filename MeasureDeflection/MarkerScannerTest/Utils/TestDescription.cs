using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkerScannerTest.Utils
{
    class TestDescription
    {
        static int uniquID = 0;
        public const string FileExtension = "png";

        int ID;
        public string Name { get; }
        public string RelativePath { get; private set; }

        public string FileName_Image { get; }
        public string FileName_Processed { get; }

        
        public TestDescription(string name, string relPath)
        {
            ID = uniquID++;
            Name = name;
            RelativePath = relPath;

            FileName_Image = Path.Combine(relPath, $"{ID:000}{Name}.{FileExtension}");
            FileName_Processed = Path.Combine(relPath, $"{ID:000}{Name}_Processed.{FileExtension}");
        }
    }
}
