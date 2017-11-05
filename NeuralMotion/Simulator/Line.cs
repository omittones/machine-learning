using System.Drawing;

namespace NeuralMotion.Simulator
{
    public class Line : IIndicator
    {
        private readonly double x1;
        private readonly double y1;
        private readonly double x2;
        private readonly double y2;

        public Line(PointF p1, PointF p2)
        {
            this.x1 = p1.X;
            this.y1 = p1.Y;
            this.x2 = p2.X;
            this.y2 = p2.Y;
        }

        public Line(double x1, double y1, double x2, double y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }

        public void Draw(Graphics graphics)
        {
            var purple = Pens.Purple.Clone() as Pen;
            purple.Width = 0.01f;
            graphics.DrawLine(purple, (float)x1, (float)y1, (float)x2, (float)y2);
        }
    }
}