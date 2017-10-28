using ConvNetSharp.Volume;

namespace NeuralMotion.Test
{
    public class Experiences
    {
        public double r0;
        public Volume<double> s0;
        public Volume<double> s1;
        public int a0;
        public int a1;

        internal static Experiences New(Volume<double> s0, int a0, double r0, Volume<double> s1, int a1)
        {
            return new Experiences
            {
                s0 = s0.Clone(),
                s1 = s1.Clone(),
                a0 = a0,
                a1 = a1,
                r0 = r0
            };
        }
    }
}
