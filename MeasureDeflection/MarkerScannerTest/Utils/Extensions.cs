using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MarkerScannerTest.Utils
{
    public class Extensions
    {
        public static System.Drawing.Color SetColor(Color color)
        {
            System.Drawing.Color c = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            return c;
        }
    }
}
