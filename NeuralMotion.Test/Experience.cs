using ConvNetSharp.Volume;

namespace NeuralMotion.Test
{
    public class Experience
    {
        public Volume<double> PrevState;
        public int[] Action;
        public Volume<double> Rewards;
        public Volume<double> NextState;
        public bool Done;
    }
}
