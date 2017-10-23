using System;
using ConvNetSharp.Core.Training.Double;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume.Double;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System.Linq;

namespace NeuralMotion.Test
{
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
            net.AddLayer(new ReinforcementSoftmaxLayer(4));
            const int SIZE = 100;

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
                int[] expectedClasses = BuildClasses(SIZE, inputs);

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

        private static int[] BuildClasses(int SIZE, Volume<double> inputs)
        {
            var expectedClasses = new int[SIZE];
            var reshaped = inputs.ReShape(1, 1, -1, SIZE);
            for (var n = 0; n < SIZE; n++)
            {
                expectedClasses[n] = 0;
                continue;

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

            return expectedClasses;
        }
    }
}
