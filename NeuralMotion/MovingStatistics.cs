using Accord.Statistics.Moving;
using System.Linq;

namespace NeuralMotion
{
    public class MovingStatistics : IMoving<double>, IMovingStatistics
    {
        private readonly MovingRangeStatistics ranges;
        private readonly MovingNormalStatistics normals;

        public MovingStatistics(int window)
        {
            this.ranges = new MovingRangeStatistics(window);
            this.normals = new MovingNormalStatistics(window);
        }

        public double Max => ranges.Max;
        public double Min => ranges.Min;
        public double Mean => normals.Mean;
        public double Variance => normals.Variance;
        public double StandardDeviation => normals.StandardDeviation;
        public int Window => normals.Window;
        public int Count => normals.Count;

        public void Clear()
        {
            normals.Clear();
            ranges.Clear();
        }

        public void Push(double value)
        {
            normals.Push(value);
            ranges.Push(value);
        }
    }
}