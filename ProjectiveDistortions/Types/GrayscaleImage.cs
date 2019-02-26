using System.Drawing;
using ProjectiveDistortions.ImageProcessing;

namespace ProjectiveDistortions.Types
{
    public class GrayscaleImage
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Colors;

        public GrayscaleImage(Bitmap bmp)
        {
            Width = bmp.Width;
            Height = bmp.Height;
            Colors = BitmapProcessing.ConvertRgbToGrayscale(bmp);
        }

        public GrayscaleImage(int width, int height, byte[] colors)
        {
            Width = width;
            Height = height;
            Colors = colors;
        }
    }
}
