using System.Windows;

namespace ImageProcessor_SCOI
{
    public static class CurveInterpolator
    {
        public static Func<byte, byte> CreateCurve(List<Point> points)
        {
            if (points == null || points.Count < 2)
                return x => x; // Линейная функция по умолчанию

            points = points.OrderBy(p => p.X).ToList();

            return x =>
            {
                if (x <= points[0].X) return (byte)points[0].Y;
                if (x >= points[^1].X) return (byte)points[^1].Y;

                for (int i = 0; i < points.Count - 1; i++)
                {
                    if (x >= points[i].X && x <= points[i + 1].X)
                    {
                        double t = (x - points[i].X) / (points[i + 1].X - points[i].X);
                        return (byte)(points[i].Y + t * (points[i + 1].Y - points[i].Y)); // Линейная интерполяция
                    }
                }

                return (byte)x;
            };
        }
    }
}