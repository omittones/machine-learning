using System;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume.Double;
using ConvNetSharp.Core.Training.Double;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System.Linq;
using ConvNetSharp.Core.Layers;
using System.Diagnostics;

namespace NeuralMotion.Test
{
    public class ReinforcementSoftmax : SoftmaxLayer<double>, IReinforcementLayer<double>
    {
        private Volume<double> lossVolume;
        private double[] loss;

        public ReinforcementSoftmax(int classCount) : base(classCount)
        {
        }

        public void SetLoss(double[] loss)
        {
            this.loss = loss;

            if (this.lossVolume == null ||
               !this.lossVolume.Shape.Equals(this.InputActivationGradients.Shape))
            {
                this.lossVolume = BuilderInstance<double>.Volume.SameAs(
                    this.InputActivationGradients.Storage,
                    this.InputActivationGradients.Shape);
            }

            var count = this.lossVolume.Shape.GetDimension(3);
            if (count != loss.Length)
                throw new NotSupportedException("Output vs loss does not match!");

            for (var n = 0; n < loss.Length; n++)
                for (var w = 0; w < this.lossVolume.Shape.GetDimension(0); w++)
                    for (var h = 0; h < this.lossVolume.Shape.GetDimension(1); h++)
                        for (var d = 0; d < this.lossVolume.Shape.GetDimension(2); d++)
                            this.lossVolume.Set(w, h, d, n, loss[n]);
        }

        public override void Backward(Volume<double> y, out double loss)
        {
            loss = this.loss.Sum();

            this.OutputActivation.DoMultiply(this.lossVolume, this.InputActivationGradients);
        }
    }

    public interface IReinforcementLayer<T> : ILastLayer<T>
        where T : struct, IEquatable<T>, IFormattable
    {
        void SetLoss(double[] loss);
    }

    public class ReinforcementTrainer : SgdTrainer
    {
        private readonly IReinforcementLayer<double> lossLayer;

        public ReinforcementTrainer(Net<double> net) : base(net)
        {
            this.lossLayer = net.Layers
                .OfType<IReinforcementLayer<double>>()
                .FirstOrDefault();
        }

        public void Reinforce(Volume<double> inputs, double[] loss)
        {
            var outputs = this.Forward(inputs);

            this.lossLayer.SetLoss(loss);

            this.Backward(outputs);

            var batchSize = inputs.Shape.GetDimension(3);
            var chrono = Stopwatch.StartNew();

            TrainImplem();

            this.UpdateWeightsTimeMs = chrono.Elapsed.TotalMilliseconds / batchSize;
        }
    }

    public static class Program
    {
        public static double[] Gradients(this Net<double> net)
        {
            return net
                .GetParametersAndGradients()
                .SelectMany(g => g.Gradient.ToArray())
                .ToArray();
        }

        public static double[] Parameters(this Net<double> net)
        {
            return net
                .GetParametersAndGradients()
                .SelectMany(g => g.Volume.ToArray())
                .ToArray();
        }

        public static void Main(string[] args)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(2, 2, 2));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(4));
            net.AddLayer(new ReinforcementSoftmax(4));
            const int SIZE = 1;

            var trainer = new ReinforcementTrainer(net);
            trainer.LearningRate = 0.01;
            trainer.L1Decay = 0;
            trainer.L2Decay = 0;
            trainer.Momentum = 0;
            trainer.BatchSize = SIZE;

            var rnd = new Random(DateTime.Now.Millisecond);

            var parameters = Parameters(net);
            while (true)
            {
                var inputs = BuilderInstance.Volume.SameAs(Shape.From(2, 2, 2, SIZE));
                inputs.Storage.MapInplace(i => rnd.Next(0, 2));

                var expectedClasses = new int[SIZE];
                var reshaped = inputs.ReShape(1, 1, 8, SIZE);
                for (var n = 0; n < SIZE; n++)
                {
                    var sum = 0;
                    for (var i = 0; i < 8; i++)
                        sum += (int)reshaped.Get(0, 0, i, n);
                    if (sum < 3)
                        expectedClasses[n] = 0;
                    else if (sum < 5)
                        expectedClasses[n] = 1;
                    else if (sum < 7)
                        expectedClasses[n] = 2;
                    else
                        expectedClasses[n] = 3;
                }

                var outputs = net.Forward(inputs, false);
                var predictedClasses = net.GetPrediction();
                var losses = new double[SIZE];
                for (var n = 0; n < SIZE; n++)
                    losses[n] = expectedClasses[n] != predictedClasses[n] ? 1 : 0;

                trainer.Reinforce(inputs, losses);

                var changed = Parameters(net);
                var nmChanges = parameters
                    .Zip(changed, (c, p) => c != p)
                    .Where(e => e)
                    .Count();

                var accuracy = 100.0 *
                    predictedClasses
                    .Zip(expectedClasses, (p, e) => p == e)
                    .Count(same => same) / SIZE;

                Console.WriteLine($"LOSS: {trainer.Loss:0.00} ACC:{accuracy:0.00}% NMCHANGES:{nmChanges}");
            }
        }
    }
}
