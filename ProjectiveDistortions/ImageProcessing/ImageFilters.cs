using ProjectiveDistortions.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectiveDistortions.ImageProcessing
{
    public static class ImageFilters
    {
        public static byte[] ImageFiltering(Bitmap bmp)
        {
            var matrix = new double[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
            var grayscale = new GrayscaleImage(bmp);
            var medianImage = grayscale.Colors;
            for (int i = 0; i < 3; i++)
                medianImage = MedianFilter(grayscale, medianImage);
            var sobelImage = SobelFilter2(grayscale, medianImage, matrix);
            var binaryImage = BinarizeImage(grayscale, sobelImage);
            return binaryImage;
        }

        /// <summary>
        /// Бинаризация изображения, алгоритм Оцу.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого.</param>
        /// <param name="bytes">Обработанное изображение.</param>
        public static byte[] BinarizeImage(GrayscaleImage grayscale, byte[] bytes)
        {
            var threshold = OtsuThreshold(grayscale);
            var height = grayscale.Height;
            var width = grayscale.Width;
            var pRes = Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    bytes[y * width + x] = (byte)((bytes[y * width + x] < threshold) ? 0 : 255);
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return bytes;
        }

        /// <summary>
        /// Вычисление порогового значения.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого.</param>
        public static int OtsuThreshold(GrayscaleImage grayscale)
        {
            var bytes = grayscale.Colors;
            var width = grayscale.Width;
            var height = grayscale.Height;
            var histSize = 256;
            var hist = MakeHistogram(bytes, width, height);
            var m = 0;
            var n = 0;
            for (int t = 0; t < histSize; t++)
            {
                m += t * hist[t];
                n += hist[t];
            }
            var threshold = MakeThreshold(histSize, hist, m, n);
            return threshold;
        }

        /// <summary>
        /// Создание гистограммы цветов на изображении.
        /// </summary>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="histSize">Размер гистограммы.</param>
        /// <param name="bytes">Обработанное изображение.</param>
        private static int[] MakeHistogram(byte[] bytes, int width, int height, int histSize = 256)
        {
            var hist = new int[histSize];
            var pRes = Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = bytes[y * width + x];
                    hist[pixel]++;
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return hist;
        }


        private static int MakeThreshold(int histSize, int[] hist, int m, int n)
        {
            var maxSigma = -1.0;
            var threshold = 0;
            var alpha1 = 0;
            var beta1 = 0;
            for (int t = 0; t < histSize; t++)
            {
                alpha1 += t * hist[t];
                beta1 += hist[t];
                var w1 = (double)beta1 / n;
                var a = (double)alpha1 / beta1 - (double)(m - alpha1) / (n - beta1);
                var sigma = w1 * (1 - w1) * a * a;
                if (sigma > maxSigma)
                {
                    maxSigma = sigma;
                    threshold = t;
                }
            }
            return threshold;
        }

        /// <summary>
        /// Медианный фильтр для устранения пикселей чёрного цвета, возникших в результате коррекции изображения.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого с выделенным и выравненным документом.</param>
        public static GrayscaleImage MedianFilterMod2(GrayscaleImage grayscale)
        {
            var width = grayscale.Width;
            var height = grayscale.Height;
            var medianImage = new byte[width * height];
            var pRes = Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    if (grayscale.Colors[y * width + x] == 0)
                    {
                        var listNeighbors = new List<double>();
                        MakeListNeighbors(grayscale.Colors, listNeighbors, width, height, x, y);
                        var med = MakeMedian(listNeighbors.OrderBy(e => e).ToList());
                        medianImage[y * width + x] = (byte)med;
                    }
                    else
                        medianImage[y * width + x] = grayscale.Colors[y * width + x];

                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return new GrayscaleImage(width, height, medianImage);
        }

        /// <summary>
        /// Медианный фильтр.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого.</param>
        /// <param name="bytes">Обработанное изображение.</param>
        public static byte[] MedianFilter(GrayscaleImage grayscale, byte[] bytes)
        {
            var width = grayscale.Width;
            var height = grayscale.Height;
            var medianImage = new byte[width * height];
            var pRes = Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    var listNeighbors = new List<double>();
                    MakeListNeighbors(bytes, listNeighbors, width, height, x, y);
                    var med = MakeMedian(listNeighbors.OrderBy(e => e).ToList());
                    medianImage[y * width + x] = (byte)med;
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return medianImage;
        }


        private static double MakeMedian(List<double> listPixel)
        {
            double med;
            int lengthListPixel = listPixel.Count;
            if (lengthListPixel == 2)
                med = (listPixel[0] + listPixel[1]) / 2;
            else if (lengthListPixel % 2 == 0)
                med = (listPixel[lengthListPixel / 2 - 1] +
                    listPixel[(lengthListPixel / 2)]) / 2;
            else
                med = listPixel[(int)Math.Ceiling(lengthListPixel / 2.0)];
            return med;
        }


        private static void MakeListNeighbors(byte[] bytes, List<double> listPixel,
    int width, int height, int x, int y)
        {
            listPixel.Add(bytes[y * width + x]);
            if (x - 1 >= 0)
                listPixel.Add(bytes[y * width + x - 1]);
            if (y - 1 >= 0)
                listPixel.Add(bytes[(y - 1) * width + x]);
            if (y - 1 >= 0 && x - 1 >= 0)
                listPixel.Add(bytes[(y - 1) * width + x - 1]);
            if (x + 1 <= width - 1)
                listPixel.Add(bytes[y * width + x + 1]);
            if (y + 1 <= height - 1)
                listPixel.Add(bytes[(y + 1) * width + x]);
            if (x + 1 <= width - 1 && y + 1 <= height - 1)
                listPixel.Add(bytes[(y + 1) * width + x + 1]);
            if (x + 1 <= width - 1 && y - 1 >= 0)
                listPixel.Add(bytes[(y - 1) * width + x + 1]);
            if (x - 1 >= 0 && y + 1 <= height - 1)
                listPixel.Add(bytes[(y + 1) * width + x - 1]);
        }

        /// <summary>
        /// Фильтр Собеля для выделения границ документа.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого.</param>
        /// <param name="bytes">Обработанное изображение.</param>
        /// <param name="sx">Матрица оператора Собеля</param>
        public static byte[] SobelFilter2(GrayscaleImage grayscale, byte[] bytes, double[,] sx)
        {
            if (sx.GetLength(0) % 2 != 1 || sx.GetLength(0) != sx.GetLength(1))
                throw new Exception("The matrix is not set correctly");
            var width = grayscale.Width;
            var height = grayscale.Height;
            var sy = TransposeMatrix(sx);
            var dim = sx.GetLength(0);
            var imageFilt = new byte[bytes.Length];
            var pRes = Parallel.For(dim / 2, height - dim / 2, y =>
            {
                for (var x = dim / 2; x < width - dim / 2; x++)
                {
                    var gx = ApplyMatrix(bytes, sx, x, y, width, height);
                    var gy = ApplyMatrix(bytes, sy, x, y, width, height);
                    imageFilt[y * width + x] = (byte)Math.Sqrt(gx * gx + gy * gy);
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return imageFilt;
        }


        private static double[,] TransposeMatrix(double[,] sx)
        {
            var res = new double[sx.GetLength(0), sx.GetLength(1)];
            for (int i = 0; i < sx.GetLength(0); i++)
                for (int j = 0; j < sx.GetLength(1); j++)
                {
                    res[i, j] = sx[j, i];
                }
            return res;
        }


        private static int ApplyMatrix(byte[] bytes, double[,] matrix, int x, int y, int width, int height)
        {
            var dim = matrix.GetLength(0);
            var res = 0;
            var mid = dim / 2;
            for (int i = -mid; i <= mid; i++)
                for (int j = -mid; j <= mid; j++)
                {
                    res += (int)(bytes[(y + i) * width + x + j] * matrix[i + mid, j + mid]);
                }
            return res;
        }

    }
}
