using ProjectiveDistortions.ImageProcessing;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using ProjectiveDistortions.Types;

namespace ProjectiveDistortions
{
    public static class EliminationOfDistortions
    {
        public static Bitmap RecognizeDocumentInImage(Bitmap originalBmp)
        {
            var grayscale = new GrayscaleImage(originalBmp);
            var compressionBmp = BitmapProcessing.ImageCompression(originalBmp);
            var processedImage = ImageFilters.ImageFiltering(compressionBmp);
            var resultSearch = SearchSingularPoints.SerchSPForImage(originalBmp, compressionBmp, processedImage);

            var spPoints = resultSearch.Item1;
            var equations = resultSearch.Item2;
            BitmapProcessing.SelectBackground(grayscale, equations);

            var widthAndHeight = MakeWidthAndHeightDocument(spPoints);
            var documentWidth = widthAndHeight.Item1;
            var documentHeight = widthAndHeight.Item2;

            var anglePoints = MakeAnglePoints(documentWidth, documentHeight, spPoints);
            var H = GetMatrixHomography(spPoints, anglePoints);
            var inverseH = H.Inverse();

            var correctImage = ImageCorrection(inverseH, grayscale);
            var correctSpPoints = TransformPoints(inverseH, spPoints);
            var correctEquations = MakeEquationsLines(correctSpPoints);
            var angle = correctEquations[2].AngleDeviationOX();
            correctImage = BitmapProcessing.RotateGrayscaleImage(correctImage, angle);
            var document = ImageCutter.CutDocument(correctImage);

            return BitmapProcessing.MakeBitmap(document.Width, document.Height, document.Colors);
            //return BitmapProcessing.MakeBitmap(correctImage.Width, correctImage.Height, correctImage.Colors);

        }


        /// <summary>
        /// Преобразование особых точек.
        /// </summary>
        /// <param name="inverseH">Обратная матрица гомографии</param>
        /// <param name="spPoints">Список особых точек</param>
        private static List<Point> TransformPoints(Matrix<double> inverseH, List<Point> spPoints)
        {
            var result = new List<Point>();
            foreach (var point in spPoints)
            {
                var newX = (inverseH[0, 0] * point.X + inverseH[0, 1] * point.Y + inverseH[0, 2]) /
                           (inverseH[2, 0] * point.X + inverseH[2, 1] * point.Y + inverseH[2, 2]);
                var newY = (inverseH[1, 0] * point.X + inverseH[1, 1] * point.Y + inverseH[1, 2]) /
                           (inverseH[2, 0] * point.X + inverseH[2, 1] * point.Y + inverseH[2, 2]);
                result.Add(new Point((int)newX, (int)newY));
            }

            return result;
        }


        /// <summary>
        /// Устранение проективных искажений на изображении.
        /// </summary>
        /// <param name="inverseH">Обратная матрица гомографии</param>
        /// <param name="grayscale">Изображение в оттенках серого с выделенными границами</param>
        private static GrayscaleImage ImageCorrection(Matrix<double> inverseH, GrayscaleImage grayscale)
        {
            var width = grayscale.Width;
            var height = grayscale.Height;
            var result = new byte[width * height];
            var pRes = Parallel.For(0, grayscale.Height, y =>
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    var newX = (inverseH[0, 0] * x + inverseH[0, 1] * y + inverseH[0, 2]) /
                    (inverseH[2, 0] * x + inverseH[2, 1] * y + inverseH[2, 2]);
                    var newY = (inverseH[1, 0] * x + inverseH[1, 1] * y + inverseH[1, 2]) /
                    (inverseH[2, 0] * x + inverseH[2, 1] * y + inverseH[2, 2]);
                    if (newX >= 0 && newX < width - 1 && newY >= 0 && newY < height - 1)
                    {
                        result[(int)newY * width + (int)newX] = grayscale.Colors[y * grayscale.Width + x];
                    }
                }
            });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return new GrayscaleImage(width, height, result);
        }

        /// <summary>
        /// Нахождение уравнение прямых, ограничивающих документ на исправленном изображении. 
        /// </summary>
        /// <param name="points">Список особых точек на исправленном изображении</param>
        private static List<EquationLine> MakeEquationsLines(List<Point> points)
        {
            var result = new List<EquationLine>();
            for (int i = 0; i < 3; i++)
            {
                var eqLine = new EquationLine(points[i], points[i + 1], TypeEquation.TwoPoint);
                result.Add(eqLine);
            }
            var eqLine4 = new EquationLine(points[3], points[0], TypeEquation.TwoPoint);
            result.Add(eqLine4);
            return result;
        }

        /// <summary>
        /// Создание оценочной матрицы гомографии через SVD разложение.
        /// </summary>
        /// <param name="resultSearch">Список особых точек на исходном изображении</param>
        /// <param name="anglePoints">Список особых точек после отображения</param>
        private static Matrix<double> GetMatrixHomography(List<Point> resultSearch, List<Point> anglePoints)
        {
            var matrixT = MakeMatrixT(resultSearch, anglePoints);
            var T = Matrix<double>.Build.DenseOfArray(matrixT);
            var V = T.Svd().VT.Transpose();
            var result = V.Column(V.ColumnCount - 1);
            var matrixGomograp = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    matrixGomograp[i, j] = result[i * 3 + j];
            return Matrix<double>.Build.DenseOfArray(matrixGomograp);
        }

        /// <summary>
        /// Вычисление длин сторон документа.
        /// </summary>
        /// <param name="anglePoints">Список особых точек после отображения</param>
        private static List<double> MakeLengthSides(List<Point> anglePoints)
        {
            var result = new List<double>();
            for (int i = 0; i < anglePoints.Count - 1; i++)
            {
                var side = new Point(anglePoints[i + 1].X - anglePoints[i].X, anglePoints[i + 1].Y - anglePoints[i].Y);
                var lenghtSide = Math.Sqrt(side.X * side.X + side.Y * side.Y);
                result.Add(lenghtSide);
            }
            var side4 = new Point(anglePoints[0].X - anglePoints[3].X, anglePoints[0].Y - anglePoints[3].Y);
            var lenghtSide4 = Math.Sqrt(side4.X * side4.X + side4.Y * side4.Y);
            result.Add(lenghtSide4);
            return result;
        }

        /// <summary>
        /// Вычисление длины и ширины документа.
        /// </summary>
        /// <param name="anglePoints">Список особых точек после отображения</param>
        private static Tuple<int, int> MakeWidthAndHeightDocument(List<Point> anglePoints)
        {
            var listLengthSides = MakeLengthSides(anglePoints);
            var width = ((int)(listLengthSides[0] + listLengthSides[2]) / 2) + 4;
            var height = ((int)(listLengthSides[1] + listLengthSides[3]) / 2) + 4;
            var rem = width % 10;
            width = width + rem;
            if (height / width < Math.Sqrt(2) - 0.4 && height / width > Math.Sqrt(2) + 0.4)
                throw new Exception();
            return Tuple.Create(width, height);
        }

        /// <summary>
        /// Вычисление особых точек после отображения.
        /// </summary>
        /// <param name="spPoints">Список особых точек изображения</param>
        /// <param name="width">Ширина документа</param>
        /// <param name="height">Высота документа</param>
        private static List<Point> MakeAnglePoints(int width, int height, List<Point> spPoints)
        {
            var x3 = (spPoints[2].X + spPoints[3].X) / 2;
            var y3 = spPoints[3].Y;
            var point3 = new Point(x3, y3);
            var point2 = new Point(x3, y3 - height);
            var point4 = new Point(x3 - width, y3);
            var point1 = new Point(x3 - width, y3 - height);
            return new List<Point> { point1, point2, point3, point4 };
        }

        /// <summary>
        /// Создает матрицу прехода Т.
        /// </summary>
        /// <param name="listPoint1">Список особых точек изображения</param>
        /// <param name="listPoint2">Список особых точек после отображения</param>
        private static double[,] MakeMatrixT(List<Point> listPoint1, List<Point> listPoint2)
        {
            return new double[,]
            {
                {listPoint1[0].X,listPoint1[0].Y,1,0,0,0,-listPoint1[0].X*listPoint2[0].X,-listPoint1[0].X*listPoint2[0].Y,-listPoint1[0].X },
                {0,0,0,listPoint2[0].X,listPoint2[0].Y,1,-listPoint1[0].Y*listPoint2[0].X,-listPoint1[0].Y*listPoint2[0].Y, - listPoint1[0].Y},
                {listPoint1[1].X,listPoint1[1].Y,1,0,0,0,-listPoint1[1].X*listPoint2[1].X,-listPoint1[1].X*listPoint2[1].Y,-listPoint1[1].X },
                {0,0,0,listPoint1[1].X,listPoint1[1].Y,1,-listPoint1[1].Y*listPoint2[1].X,-listPoint1[1].Y*listPoint2[1].Y, - listPoint1[1].Y},
                {listPoint1[2].X,listPoint1[2].Y,1,0,0,0,-listPoint1[2].X*listPoint2[2].X,-listPoint1[2].X*listPoint2[2].Y,-listPoint1[2].X },
                {0,0,0,listPoint1[2].X,listPoint1[2].Y,1,-listPoint1[2].Y*listPoint2[2].X,-listPoint1[2].Y*listPoint2[2].Y, - listPoint1[2].Y},
                {listPoint1[3].X,listPoint1[3].Y,1,0,0,0,-listPoint1[3].X*listPoint2[3].X,-listPoint1[3].X*listPoint2[3].Y,-listPoint1[3].X },
                {0,0,0,listPoint1[3].X,listPoint1[3].Y,1,-listPoint1[3].Y*listPoint2[3].X,-listPoint1[3].Y*listPoint2[3].Y, - listPoint1[3].Y},
            };
        }
    }
}
