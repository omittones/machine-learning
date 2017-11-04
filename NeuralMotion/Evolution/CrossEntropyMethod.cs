using Accord.Statistics;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuralMotion.Evolution
{
    public class CrossEntropyMethod
    {
        private Random rng = new Random();
        private double[] means;
        private double[] stdDevs;
        private List<Sample> samples;
        private readonly Net<double> net;

        public int SamplesCreatedOnEachIteration { get; set; }
        public int SamplesLeftAfterEvaluation { get; set; }
        public double Alpha { get; set; }
        public double InitialMean { get; set; }
        public double InitialStdDev { get; set; }
        public Sample BestSample { get; private set; }
        public double AvgStdDevs => stdDevs.Average();

        public CrossEntropyMethod(Net<double> net)
        {
            this.net = net;
            this.samples = new List<Sample>();
            this.BestSample = null;
        }

        private double RandomGaussian(double mean = 0.0, double stdev = 1.0)
        {
            double u1, u2, w;
            u1 = u2 = w = 0;
            do
            {
                u1 = 2 * rng.NextDouble() - 1;
                u2 = 2 * rng.NextDouble() - 1;
                w = u1 * u1 + u2 * u2;
            } while (w >= 1);

            w = Math.Sqrt((-2.0 * Math.Log(w)) / w);
            return mean + (u2 * w) * stdev;
        }

        public void Reset()
        {
            this.samples.Clear();
            this.means = null;
            this.stdDevs = null;
            this.BestSample = null;
        }

        public void Train(Func<Volume<double>[], double> returnsFunction)
        {
            var png = net.GetParametersAndGradients();
            var volumes = png.Select(p => p.Volume).ToArray();
            var total = TotalLength(volumes);

            if (this.means == null || this.means.Length != total)
            {
                this.means = Enumerable.Repeat(InitialMean, total).ToArray();
                this.stdDevs = Enumerable.Repeat(InitialStdDev, means.Length).ToArray();
            }

            int count;
            for (var sample = 0; sample < SamplesCreatedOnEachIteration; sample++)
            {
                count = 0;
                var cloned = Clone(volumes);

                foreach (var volume in cloned)
                {
                    var flat = volume.ReShape(1, 1, 1, -1);
                    for (var i = 0; i < flat.Shape.TotalLength; i++)
                    {
                        flat.Set(0, 0, 0, i, RandomGaussian(means[count], stdDevs[count]));
                        count++;
                    }
                }

                samples.Add(new Sample
                {
                    Parameters = cloned,
                    Returns = null
                });
            }

            for (var sample = 0; sample < samples.Count; sample++)
                if (samples[sample].Returns == null)
                    samples[sample].Returns = returnsFunction(samples[sample].Parameters);
            
            samples = samples
                .OrderByDescending(s => s.Returns)
                .Take(SamplesLeftAfterEvaluation)
                .ToList();

            BestSample = samples[0];

            var values = new double[samples.Count];
            count = 0;
            for (var v = 0; v < volumes.Length; v++)
            {
                for (var p = 0; p < volumes[v].Shape.TotalLength; p++)
                {
                    for (var s = 0; s < samples.Count; s++)
                    {
                        var parameters = samples[s].Parameters[v].ReShape(1, 1, 1, -1);
                        values[s] = parameters.Get(0, 0, 0, p);
                    }

                    var mean = values.Mean();
                    var stdDev = values.StandardDeviation(mean);
                    means[count] = means[count] - Alpha * (means[count] - mean);
                    stdDevs[count] = stdDevs[count] - Alpha * (stdDevs[count] - stdDev);
                    count++;
                }
            }
        }

        private Volume<double>[] Clone(IEnumerable<Volume<double>> volumes)
        {
            return volumes.Select(v => v.Clone()).ToArray();
        }

        private int TotalLength(IEnumerable<Volume<double>> volumes)
        {
            return (int)volumes.Select(v => v.Shape.TotalLength).Sum();
        }

        public void ClearCache()
        {
            samples.ForEach(s => s.Returns = null);
        }
    }

    public class Sample
    {
        public double? Returns;
        public Volume<double>[] Parameters;

        public override string ToString()
        {
            if (Returns.HasValue)
                return "returns: " + Returns.Value.ToString("0.000");
            else
                return "returns: pending";
        }
    }
}
