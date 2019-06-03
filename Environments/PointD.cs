using System.Drawing;

namespace Environments
{
    public struct PointD
    {
        public readonly double X;
        public readonly double Y;

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator PointD((double x, double y) point)
        {
            return new PointD(point.x, point.y);
        }

        public static implicit operator PointF(PointD point)
        {
            return new PointF((float)point.X, (float)point.Y);
        }
    }

    public static class PointDExtensions
    {
        public static PointF[] ToPointF(this PointD[] d)
        {
            var f = new PointF[d.Length];
            for (var i = 0; i < f.Length; i++)
                f[i] = d[i];
            return f;
        }

        public static PointF ToPoint(this (float x, float y) t)
        {
            return new PointF(t.x, t.y);
        }
    }
}