using System.Linq;

namespace gym
{
    internal class DiscreteSpace : Space<int>
    {
        private int[] values;

        public DiscreteSpace(int[] values)
        {
            this.values = values;
        }

        public override bool contains(int item)
        {
            return values.Contains(item);
        }

        public override int sample()
        {
            var x = rnd.Next(0, values.Length);
            return values[x];
        }
    }
}