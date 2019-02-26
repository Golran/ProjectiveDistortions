using System;
using System.Drawing;

namespace ProjectiveDistortions.Types
{
    public class EquationLine
    {
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public EquationLine(Point point1, Point point2, TypeEquation typeEquation)
        {
            if (typeEquation == TypeEquation.NormVector)
            {
                A = point1.X;
                B = point1.Y;
                C = (-point2.X) * point1.X + (-point2.Y) * point1.Y;
            }
            else if (typeEquation == TypeEquation.TwoPoint)
            {
                var dX = point2.X - point1.X;
                var dY = point2.Y - point1.Y;
                A = dY;
                B = -dX;
                C = dY * point1.X + (-dX) * point1.Y;
            }
        }

        public double AngleDeviationOX()
        {
            var a2 = A;
            var b2 = B;
            return Math.Acos(b2 / Math.Sqrt(a2 * a2 + b2 * b2));
        }

        public double DeterminePosition(int x, int y)
        {
            return A * x + B * y + C;
        }
    }
}
