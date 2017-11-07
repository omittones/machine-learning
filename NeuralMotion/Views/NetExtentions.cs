using ConvNetSharp.Core;
using ConvNetSharp.Volume;

namespace NeuralMotion.Views
{
    public static class NetExtentions
    {
        public static (Volume<double>, Volume<double>) ForwardArea(this Net<double> net, PointD min, PointD max, int resolution)
        {
            var spanX = (max.X - min.X) / resolution;
            var spanY = (max.Y - min.Y) / resolution;

            int count = 0;
            var input = BuilderInstance<double>.Volume.SameAs(Shape.From(1, 1, 2, resolution * resolution));
            for (var y = min.Y; y <= max.Y; y += spanY)
                for (var x = min.X; x <= max.X; x += spanX)
                {
                    if (count == input.BatchSize)
                        break;

                    input.Set(0, 0, 0, count, x);
                    input.Set(0, 0, 1, count, y);
                    count++;
                }

            lock (net)
            {
                return (input, net.Forward(input));
            }
        }
    }
}
