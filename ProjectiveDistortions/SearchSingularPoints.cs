using ProjectiveDistortions.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectiveDistortions
{
    public static class SearchSingularPoints
    {
        private const double epsilon = 10;

        /// <summary>
        /// Поиск особых точек (угловые точки документа) на изображении.
        /// </summary>
        /// <param name="originalImage">Изначальное изображение поступающее на вход</param>
        /// <param name="compressionImage">Сжатое изображение</param>
        /// <param name="processedImage">Обработанное фильтрами сжатое изображение</param>
        public static Tuple<List<Point>, List<EquationLine>> SerchSPForImage(Bitmap originalImage, Bitmap compressionImage, byte[] processedImage)
        {
            var countCompression = (double)originalImage.Width / compressionImage.Width;
            var straightLines = HoughTransform(compressionImage, processedImage, countCompression);
            var equationList = straightLines
                .Select(line => MakeEquationLine(line))
                .ToList();
            var width = originalImage.Width;
            var height = originalImage.Height;
            var result = new List<Point>();
            for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 4; j++)
                {
                    var resSearch = SearchIntersection(equationList[i], equationList[j]);
                    if (resSearch.Item2 && (resSearch.Item1.X > -width / 7 && resSearch.Item1.X < width + width / 7) &&
                        (resSearch.Item1.Y > -height / 9 && resSearch.Item1.Y < height + height / 9))
                        result.Add(resSearch.Item1);
                }
            result = SortPoints(result);
            equationList = SortEquations(equationList);
            return Tuple.Create(result, equationList);
        }

        /// <summary>
        /// Переход от параметризации к уравнению прямой.
        /// </summary>
        /// <param name="straightLine">Параметризация прямой: расстояние до неё и угол отклонения перпендикуляра к ней</param>
        private static EquationLine MakeEquationLine(StraightLine straightLine)
        {
            var x1 = (int)(straightLine.Distance * Math.Cos(straightLine.Angle));
            var y1 = (int)(straightLine.Distance * Math.Sin(straightLine.Angle));
            var normVector = new Point(x1, y1);
            var x2 = (int)(x1 + Math.Cos(straightLine.Angle + Math.PI / 2) * 20);
            var y2 = (int)(y1 + Math.Sin(straightLine.Angle + Math.PI / 2) * 20);
            var point2 = new Point(x2, y2);
            return new EquationLine(normVector, point2, TypeEquation.NormVector);
        }

        /// <summary>
        /// Поиск точки пересечения 2 прямых(Особой точки).
        /// </summary>
        /// <param name="equation1">Уравнение 1ой прямой</param>
        /// <param name="equation2">Уравнение 2ой прямой</param>
        private static Tuple<Point, bool> SearchIntersection(EquationLine equation1, EquationLine equation2)
        {
            var a1 = equation1.A;
            var a2 = equation2.A;
            var b1 = equation1.B;
            var b2 = equation2.B;
            var c1 = -equation1.C;
            var c2 = -equation2.C;

            if (Math.Abs(a1 * b2 - b1 * a2) < epsilon)
                return Tuple.Create(new Point(0, 0), false);

            var x = (c1 - c2 * b1 / b2) / (a1 - a2 * b1 / b2);
            var y = (c2 - x * a2) / b2;
            return Tuple.Create(new Point((int)x, (int)y), true);
        }

        /// <summary>
        /// Сортировка особых точек в порядке обхода документа по часовой стрелке, начиная с левой верхней точки.
        /// </summary>
        /// <param name="points">Список особых точек</param>
        private static List<Point> SortPoints(List<Point> points)
        {
            if (points.Count != 4)
                throw new ArgumentException();
            points = points.OrderBy(point => Math.Sqrt(point.X * point.X + point.Y * point.Y)).ToList();
            var point2 = points[1];
            var point3 = points[2];
            if (point2.X < point3.X || point2.Y > point3.Y)
            {
                var point = point2;
                point2 = point3;
                point3 = point;
            }
            points[1] = point2;
            points[2] = points[3];
            points[3] = point3;
            return points;
        }

        /// <summary>
        /// Сортировка уравнений прямых ограничивающих документ в порядке: верхняя и нижняя граница, левая и правая граница.
        /// </summary>
        /// <param name="equationsLines">Cписок уравнений прямых</param>
        private static List<EquationLine> SortEquations(List<EquationLine> equationsLines)
        {
            var sort1 = equationsLines.OrderBy(eq => eq.AngleDeviationOX()).ToList();
            var part1 = sort1.Take(2).OrderBy(eq => Math.Abs(eq.C)).ToList();
            var part2 = sort1.Skip(2).OrderBy(eq => Math.Abs(eq.C)).ToList();
            return part1.Concat(part2).ToList();

        }

        /// <summary>
        /// Преобразование Хафа.
        /// </summary>
        /// <param name="CompressionImage">Сжатое изображение</param>
        /// <param name="processedImage">Изображение обработанное фильтрами</param>
        /// <param name="countCompression">Коэффициент сжатия изображения</param>
        public static List<StraightLine> HoughTransform(Bitmap CompressionImage, byte[] processedImage, double countCompression)
        {
            var width = CompressionImage.Width;
            var height = CompressionImage.Height;
            var diagonal = (int)Math.Ceiling(Math.Sqrt(width * width + height * height));
            var houghSpace = new HoughSpace(diagonal);
            var result = new List<StraightLine>();

            houghSpace.FillHoughSpace(CompressionImage, processedImage);

            for (int i = 0; i < 4; i++)
            {
                var maxLine = GetMaxStraightLine(houghSpace);
                houghSpace.ClearPartHoughSpace(maxLine);
                maxLine.Distance = (int)(maxLine.Distance * countCompression) + 2;
                result.Add(maxLine);
            }
            return result;
        }

        /// <summary>
        /// Поиск самой длинной линии на изображении.
        /// </summary>
        /// <param name="houghSpace">Пространство Хафа(фазовое пространство параметров: длина перпендикуляра до прямой и угол отклонения перпендикуляра)</param>
        private static StraightLine GetMaxStraightLine(HoughSpace houghSpace)
        {
            var max = new StraightLine(0, 0, 0);
            var midDistance = houghSpace.DiagonalImage;
            var midStep = houghSpace.Size.Width / 2;
            var pRes = Parallel.For(0, houghSpace.Accumulator.GetLength(0), angle =>
            {
                for (int distance = 0; distance < houghSpace.Accumulator.GetLength(1); distance++)
                {
                    var vote = houghSpace.Accumulator[angle, distance];
                    if (vote > max.Vote && distance - midDistance > 0)
                        max = new StraightLine(distance - midDistance, (angle - midStep) * HoughSpace.stepForAngle + HoughSpace.shiftForAngle, vote);
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return max;
        }
    }
}
