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
        public static void Evolve(Net<double> net, Volume<double> inputs, int[] expectedClasses)
        {
            var outputs = expectedClasses.ToSoftmaxVolume();
            var fitness = new SoftmaxLossFitness(net, inputs, outputs);
            var evolver = new Evolver(fitness.ProtoChromosome, 100, fitness);
            while (true)
            {
                if (evolver.CurrentChampGenome != null)
                    net.FromChromosome(evolver.CurrentChampGenome);
                GetPunishment(net, inputs, expectedClasses);

                //trainer.Train(inputs, outputs);
                evolver.PerformSingleStep();
                Console.WriteLine("--------------- evolving ---------------");

                net.FromChromosome(evolver.CurrentChampGenome);
                GetPunishment(net, inputs, expectedClasses);
                var predicedClasses = net.GetPrediction();

                var accuracy = expectedClasses
                    .Zip(predicedClasses, (e, p) => e == p ? 1.0 : 00).Sum() * 100.0 / expectedClasses.Length;

                Console.WriteLine($"LOSS: {evolver.BestFitness:0.00} ACC:{accuracy:0.00}%");
                //Console.WriteLine();
                //Console.ReadKey();
            }
        }

        public static void Main(string[] args)
        {
            const int SIZE = 1000;

            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 12));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(1));
            net.AddLayer(new RegressionLayer());
            //net.AddLayer(new SoftmaxLayer(4));
            //net.AddLayer(new ReinforcementSoftmaxLayer(4));

            var rnd = new Random(DateTime.Now.Millisecond);

            var inputs = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 8 + 4, SIZE));

            var count = inputs.Shape.GetDimension(3);
            inputs.Storage.MapInplace(i => rnd.Next(0, 2));
            for (var n = 0; n < count; n++)
                for (var w = 8; w < 12; w++)
                    inputs.Set(0, 0, w, n, 0);

            int[] expectedClasses = BuildClasses(inputs);

            var outputs = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 1, count));
            for (var n = 0; n < count; n++)
            {
                var predicted = rnd.Next(0, 4);
                var reward = predicted == expectedClasses[n] ? 1 : 0;
                inputs.Set(0, 0, 8 + predicted, n, 1);
                outputs.Set(0, 0, 0, n, reward);
            }

            var trainer = new SgdTrainer(net);
            trainer.LearningRate = 0.1;
            trainer.L1Decay = 0.01;
            trainer.L2Decay = 0;
            trainer.Momentum = 0.2;
            trainer.BatchSize = count;

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                        break;

                    if (key.Key == ConsoleKey.UpArrow)
                        trainer.LearningRate *= 1.1;
                    if (key.Key == ConsoleKey.DownArrow)
                        trainer.LearningRate /= 1.1;

                    if (trainer.LearningRate > 0.9)
                        trainer.LearningRate = 0.9;

                    Console.WriteLine($"LR: {trainer.LearningRate:0.000000}");
                }

                trainer.Train(inputs, outputs);
                Console.WriteLine($"LOSS: {trainer.Loss:0.00000000}");
            }

            inputs.Storage.MapInplace(i => rnd.Next(0, 2));
            for (var n = 0; n < count; n++)
                for (var w = 8; w < 12; w++)
                    inputs.Set(0, 0, w, n, 0);
            expectedClasses = BuildClasses(inputs);

            var guesses = new Guess[count];
            for (var n = 0; n < guesses.Length; n++)
                guesses[n].Reward = double.MinValue;

            for (var klass = 0; klass < 4; klass++)
            {
                for (var n = 0; n < count; n++)
                {
                    for (var w = 8; w < 12; w++)
                        inputs.Set(0, 0, w, n, 0);
                    inputs.Set(0, 0, klass + 8, n, 1);
                }

                var predictedReward = net.Forward(inputs, false);
                for (var n = 0; n < guesses.Length; n++)
                {
                    var reward = predictedReward.Get(0, 0, 0, n);
                    if (guesses[n].Reward < reward)
                    {
                        guesses[n].Reward = reward;
                        guesses[n].Klass = klass;
                    }
                }
            }

            var accuracy = expectedClasses
                .Zip(guesses, (e, g) => e == g.Klass ? 1.0 : 0)
                .Sum() / expectedClasses.Length;
            accuracy *= 100.0;
            Console.WriteLine($"ACCURACY: {accuracy:0.00}%");
        }

        public struct Guess
        {
            public int Klass;
            public double Reward;
        }

        private static void ReinforcementTesting(Net<double> net, Volume<double> inputs, int[] expectedClasses)
        {
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

        //public string WriteSet(Volume<double> calculated, Volume<double> outputs)
        //{
        //    var probabilities = calculated.ToArray();
        //    var batchSize = calculated.Shape.GetDimension(3);
        //    var classCount = probabilities.Length / batchSize;
        //    for (var n = 0; n < batchSize; n++)
        //    {
        //        Console.Write(probabilities.Skip(n * classCount).Take(classCount).ToArray().ToString("0.000"));
        //        Console.Write(" -> ");
        //        Console.WriteLine($"{predictedClasses[n]} vs. {expectedClasses[n]} (loss {punishment[n]})");
        //    }
        //}

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
