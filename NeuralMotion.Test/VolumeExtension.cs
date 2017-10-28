using ConvNetSharp.Volume;

namespace NeuralMotion.Test
{
    public static class VolumeExtension
    {
        public static int IndexOfMax(this Volume<double> output)
        {
            int a = 0;
            for (var i = 1; i < output.Shape.GetDimension(2); i++)
                if (output.Get(0, 0, i, 0) > output.Get(0, 0, a, 0))
                    a = i;
            return a;
        }
    }
}
