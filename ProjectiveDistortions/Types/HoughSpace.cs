using System;
using System.Drawing;
using System.Threading.Tasks;

namespace ProjectiveDistortions.Types
{
    public class HoughSpace
    {
        /// <summary>
        /// Размер пространства.
        /// </summary>
        public Size Size { get; set; }
        /// <summary>
        /// Длина диагонали изображения.
        /// </summary>
        public int DiagonalImage { get; set; }
        /// <summary>
        /// Смещение угла поиска.
        /// </summary>
        public const double shiftForAngle = Math.PI / 4;
        /// <summary>
        /// Угол поиска.
        /// </summary>
        public const double angle = Math.PI;
        /// <summary>
        /// Шаг для угла поиска.
        /// </summary>
        public const double stepForAngle = (2 * Math.PI) / 1440;
        /// <summary>
        /// Таблица cos и sin для каждого данного пространства.
        /// </summary>
        private readonly double[,] tableCosAndSin;
        /// <summary>
        /// Массив в котором ведется голосование за прямые.
        /// </summary>
        public int[,] Accumulator;
        public HoughSpace(int diagonalImage)
        {
            DiagonalImage = diagonalImage;
            Size = new Size((int)(angle / stepForAngle) + 1, diagonalImage * 2 + 1);
            Accumulator = new int[Size.Width, Size.Height];
            tableCosAndSin = MakeTable();
        }

        /// <summary>
        /// Создание таблицы cos и sin.
        /// </summary>
        private double[,] MakeTable()
        {
            var table = new double[2, Size.Width];
            var midSteps = Size.Width / 2;
            for (int i = 0; i < 2; i++)
                for (int j = -midSteps; j < midSteps; j++)
                {
                    if (i == 0)
                        table[i, j + midSteps] = Math.Cos(j * stepForAngle + shiftForAngle);
                    else
                        table[i, j + midSteps] = Math.Sin(j * stepForAngle + shiftForAngle);
                }
            return table;
        }

        /// <summary>
        /// Заполнение пространства Хафа.
        /// </summary>
        /// <param name="bmp">Сжатое изображение</param>
        /// <param name="processedImage">Обработанное фильтрами сжатое изображение</param>
        public void FillHoughSpace(Bitmap bmp, byte[] processedImage)
        {
            var width = bmp.Width;
            var height = bmp.Height;
            var pRes = Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    if (processedImage[y * width + x] != 255) continue;
                    for (int i = 0; i < Size.Width; i++)
                    {
                        var distance = (int)Math.Ceiling(x * tableCosAndSin[0, i] + y * tableCosAndSin[1, i]);
                        Accumulator[i, distance + DiagonalImage]++;
                    }
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
        }

        /// <summary>
        /// Оичистка пространства Хафа от уже найденной прямой.
        /// </summary>
        /// <param name="line">Прямая найденная ранее</param>
        public void ClearPartHoughSpace(StraightLine line)
        {
            var angleIndex = (int)((line.Angle - shiftForAngle) / stepForAngle) + Size.Width / 2;
            var distanceIndex = line.Distance + DiagonalImage;
            for (int i = -Size.Width / 6; i < Size.Width / 6; i++)
                for (int j = -Size.Height / 12; j < Size.Height / 12; j++)
                {
                    if (angleIndex + i >= 0 && angleIndex + i < Size.Width && distanceIndex + j >= 0 && distanceIndex + j < Size.Height)
                        Accumulator[angleIndex + i, distanceIndex + j] = 0;
                }
        }

    }
}
