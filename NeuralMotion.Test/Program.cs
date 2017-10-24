using System;
using ConvNetSharp.Core.Training.Double;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume.Double;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System.Linq;
using NeuralMotion.Evolution.GeneticSharp;

namespace NeuralMotion.Test
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(2, 2, 2));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(4));
            //net.AddLayer(new SoftmaxLayer(4));
            net.AddLayer(new ReinforcementSoftmaxLayer(4));

            var rnd = new Random(DateTime.Now.Millisecond);

            var inputs = BuilderInstance.Volume.SameAs(Shape.From(2, 2, 2, 2));
            inputs.Storage.MapInplace(i => rnd.Next(0, 2));

            int[] expectedClasses = BuildClasses(inputs);
            var outputs = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 4, expectedClasses.Length));
            for (var i = 0; i < expectedClasses.Length; i++)
                outputs.Set(0, 0, expectedClasses[i], i, 1);

            var fitness = new SoftmaxLossFitness(net, inputs, outputs);
            var evolver = new Evolver(fitness.ProtoChromosome, 100, fitness);
            while (true)
            {
                var punishment = GetPunishment(net, inputs, expectedClasses);

                //trainer.Train(inputs, outputs);
                evolver.PerformSingleStep();
                Console.WriteLine("--------------- evolving ---------------");

                GetPunishment(net, inputs, expectedClasses);

                var accuracy = punishment.Where(p => p < 1).Count() * 100.0 / expectedClasses.Length;

                Console.WriteLine($"LOSS: {evolver.BestFitness:0.00} ACC:{accuracy:0.00}%");
                Console.WriteLine();
                Console.ReadKey();
            }

            return;

            var trainer = new ReinforcementTrainer(net);
            //var trainer = new SgdTrainer(net);
            trainer.LearningRate = 0.1;
            trainer.L1Decay = 0;
            trainer.L2Decay = 0;
            trainer.Momentum = 0;
            trainer.BatchSize = inputs.Shape.GetDimension(3);

            while (true)
            {
                if (false)
                {
                    var firstOutput = net.Forward(inputs, false).ToArray();
                    Console.WriteLine(firstOutput.ToString("0.000"));
                    Console.WriteLine("-------- rewarding ----------------");
                    var loss = new double[] { -1 };
                    for (var epoch = 0; epoch < 10; epoch++)
                    {
                        trainer.Reinforce(inputs, loss);
                        var secondOutput = net.Forward(inputs, false).ToArray();
                        Console.WriteLine(secondOutput.ToString("0.000"));
                    }
                    Console.WriteLine("-------- punishing ----------------");
                    loss = new double[] { 1 };
                    for (var epoch = 0; epoch < 10; epoch++)
                    {
                        trainer.Reinforce(inputs, loss);
                        var secondOutput = net.Forward(inputs, false).ToArray();
                        Console.WriteLine(secondOutput.ToString("0.000"));
                    }
                    break;
                }
                else
                {
                    var punishment = GetPunishment(net, inputs, expectedClasses);

                    //trainer.Train(inputs, outputs);
                    trainer.Reinforce(inputs, punishment);
                    Console.WriteLine("-------- reinforcing ----------------");

                    GetPunishment(net, inputs, expectedClasses);

                    var accuracy = punishment.Where(p => p < 1).Count() * 100.0 / expectedClasses.Length;

                    Console.WriteLine($"LOSS: {trainer.Loss:0.00} ACC:{accuracy:0.00}%");
                    Console.WriteLine();
                    Console.ReadKey();
                }
            }
        }

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

        private static double[] GetPunishment(INet<double> net, Volume<double> inputs, int[] expectedClasses)
        {
            var size = inputs.Shape.GetDimension(3);
            var calculated = net.Forward(inputs, false);
            var predictedClasses = net.GetPrediction();
            var punishment = new double[size];

            double negatives = 0; double positives = 0;
            for (var n = 0; n < size; n++)
            {
                if (expectedClasses[n] != predictedClasses[n])
                {
                    positives += 1;
                    punishment[n] = 1;
                }
                else
                {
                    negatives += 1;
                    punishment[n] = -1;
                }
            }
            for (var n = 0; n < size; n++)
            {
                punishment[n] = punishment[n] < 0 ? punishment[n] / negatives : punishment[n] / positives;
            }

            var probabilities = calculated.ToArray();
            var batchSize = calculated.Shape.GetDimension(3);
            var classCount = probabilities.Length / batchSize;
            for (var n = 0; n < batchSize; n++)
            {
                Console.Write(probabilities.Skip(n * classCount).Take(classCount).ToArray().ToString("0.000"));
                Console.Write(" -> ");
                Console.WriteLine($"{predictedClasses[n]} vs. {expectedClasses[n]} (loss {punishment[n]})");
            }

            return punishment;
        }

        private static int[] BuildClasses(Volume<double> inputs)
        {
            var count = inputs.Shape.GetDimension(3);
            var expectedClasses = new int[count];
            var reshaped = inputs.ReShape(1, 1, -1, count);
            for (var n = 0; n < count; n++)
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

            return expectedClasses;
        }
    }
}
