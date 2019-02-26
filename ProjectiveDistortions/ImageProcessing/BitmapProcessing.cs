using ProjectiveDistortions.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ProjectiveDistortions.ImageProcessing
{
    public static class BitmapProcessing
    {
        public static byte[] GetColorValues(this BitmapData bmpData)
        {
            int size = Math.Abs(bmpData.Stride) * bmpData.Height;
            IntPtr ptr = bmpData.Scan0;
            var colorValues = new byte[size];

            Marshal.Copy(ptr, colorValues, 0, size);
            return colorValues;
        }


        private static T WithBitmapData<T>(this Bitmap bmp, Func<BitmapData, T> bitmapDataAction)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            try
            {
                return bitmapDataAction(bitmapData);
            }
            finally
            {
                bmp.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Сжатие изображения до размера 300х400 .
        /// </summary>
        /// <param name="originalImage">Исходное изображение.</param>
        public static Bitmap ImageCompression(Bitmap originalImage)
        {
            var width = originalImage.Width;
            var height = originalImage.Height;
            if (width > height)
            {
                originalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                height = originalImage.Height;
            }
            else
            {
                width = originalImage.Width;
            }
            Bitmap newImage = originalImage;
            while (height > 850)
            {
                height = height / 2;
                width = width / 2;
                newImage = new Bitmap(newImage, new Size(width, height));
            }
            return new Bitmap(newImage, new Size(300, 400));

        }

        /// <summary>
        /// Переводит изображение в отенки серого.
        /// </summary>
        /// <param name="originalImage">Исходное изображение.</param>
        public static byte[] ConvertRgbToGrayscale(Bitmap rgbBitmap)
        {
            var width = rgbBitmap.Width;
            var height = rgbBitmap.Height;

            var stride = WithBitmapData(rgbBitmap, bmpData => bmpData.Stride);
            var colors = WithBitmapData(rgbBitmap, bmpData => GetColorValues(bmpData));
            var grayColors = new byte[width * height];
            if (rgbBitmap.PixelFormat == PixelFormat.Format32bppArgb || rgbBitmap.PixelFormat == PixelFormat.Format32bppRgb || rgbBitmap.PixelFormat == PixelFormat.Format32bppPArgb)
            {
                int len = colors.Length;
                for (int i = 0; i < len; i += 4)
                {
                    grayColors[i / 4] = (byte)(colors[i] * 0.2125 + colors[i + 1] * 0.7154 + colors[i + 2] * 0.0721);
                }
            }
            else if (rgbBitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {

                for (int i = 0; i < height; ++i)
                {
                    int rgbInd = i * stride;
                    int grayInd = i * width;
                    for (int j = 0; j < width; ++j, rgbInd += 3, ++grayInd)
                    {
                        grayColors[grayInd] = (byte)(colors[rgbInd] * 0.2125 + colors[rgbInd + 1] * 0.7154 + colors[rgbInd + 2] * 0.0721);
                    }
                }
            }
            else
            {
                throw new BadImageFormatException();
            }
            return grayColors;
        }


        public static Bitmap MakeBitmap(int width, int height, byte[] resultImage)
        {
            var newBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = newBmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var ptr = bmpData.Scan0;
            var len = bmpData.Stride * height;
            var newBytes = new byte[len];
            for (int i = 0; i < len; i += 3)
            {
                var pixel = resultImage[i / 3];
                newBytes[i] = newBytes[i + 1] = newBytes[i + 2] = pixel;
            }
            Marshal.Copy(newBytes, 0, ptr, len);
            newBmp.UnlockBits(bmpData);
            return newBmp;
        }

        /// <summary>
        /// Окрашивает фон вокруг изображения в белый цвет.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого.</param>
        /// <param name="equationsLines">Уравнения прямых, ограничивающих документ на изображении.</param>
        public static void SelectBackground(GrayscaleImage grayscale, List<EquationLine> equationsLines)
        {
            var width = grayscale.Width;
            var height = grayscale.Height;
            var pRes = Parallel.For(0, height, y =>
             {
                 for (int x = 0; x < width; x++)
                 {
                     var position1 = equationsLines[0].DeterminePosition(x, y);
                     var position2 = equationsLines[1].DeterminePosition(x, y);
                     var position3 = equationsLines[2].DeterminePosition(x, y);
                     var position4 = equationsLines[3].DeterminePosition(x, y);
                     if (position1 <= 0 || position2 >= 0 || position3 <= 0 || position4 >= 0)
                         grayscale.Colors[y * width + x] = 255;
                 }
             });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
        }

        /// <summary>
        /// Поворачивает изображение на заданный угол относительно центра изображения.
        /// </summary>
        /// <param name="grayscaleImage">Изображение в оттенках серого.</param>
        /// <param name="rotationAngle">Угол поворота изображения .</param>
        public static GrayscaleImage RotateGrayscaleImage(GrayscaleImage grayscaleImage, double rotationAngle)
        {
            var width = grayscaleImage.Width;
            var height = grayscaleImage.Height;
            double sinA = Math.Sin(rotationAngle);
            double cosA = Math.Cos(rotationAngle);

            var newColors = new byte[width * height];

            double x0 = width / 2;
            double y0 = height / 2;

            Parallel.For(0, height, newY =>
            {
                for (int newX = 0; newX < width; ++newX)
                {
                    //Вращение относительно центра изображения
                    int x = (int)(x0 + (newX - x0) * cosA - (newY - y0) * sinA);
                    int y = (int)(y0 + (newX - x0) * sinA + (newY - y0) * cosA);
                    if (x >= grayscaleImage.Width || y >= grayscaleImage.Height || x < 0 || y < 0)
                        continue;
                    newColors[newY * width + newX] = grayscaleImage.Colors[y * grayscaleImage.Width + x];
                }
            });

            return new GrayscaleImage(width, height, newColors);
        }
    }
}
