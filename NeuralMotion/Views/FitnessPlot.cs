using System.Linq;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;

namespace NeuralMotion.Views
{
    public class FitnessPlot : PlotView
    {
        private LineSeries plotLoss;
        private LineSeries plotReward;

        private void EnsureInitialized()
        {

            if (this.Model == null)
            {
                this.plotLoss = new LineSeries
                {
                    BrokenLineColor = OxyColors.Blue,
                    BrokenLineThickness = 1,
                    StrokeThickness = 1,
                    YAxisKey = "loss-y"
                };
                this.plotReward = new LineSeries
                {
                    BrokenLineColor = OxyColors.Red,
                    BrokenLineThickness = 1,
                    StrokeThickness = 1,
                    YAxisKey = "reward-y"
                };

                this.Model = new PlotModel()
                {
                    Axes =
                    {
                        new LogarithmicAxis
                        {
                            Position = AxisPosition.Left,
                            Key = "loss-y",
                            Title = "loss"
                        },
                        new LinearAxis
                        {
                            Position = AxisPosition.Right,
                            Key = "reward-y",
                            Title = "reward"
                        },
                        new LinearAxis
                        {
                            Position = AxisPosition.Bottom
                        }
                    },
                    Series =
                    {
                        plotLoss,
                        plotReward
                    }
                };
            }
        }

        public void AddPoint(int sample, double loss, double reward)
        {
            EnsureInitialized();

            if (this.plotLoss.Points.Count > 0 &&
                this.plotReward.Points.Count > 0)
            {
                var lastLoss = this.plotLoss.Points.Last().Y;
                var lastReward = this.plotReward.Points.Last().Y;
                if (lastLoss == loss && lastReward == reward)
                    return;
            }

            lock (this.plotLoss)
            {
                this.plotLoss.Points.Add(new DataPoint(sample, loss));
                this.plotReward.Points.Add(new DataPoint(sample, reward));
                this.InvalidatePlot(true);
            }
        }
    }
}
