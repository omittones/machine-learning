using System;
using ConvNetSharp.Core.Training.Double;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume.Double;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System.Linq;
using NeuralMotion.Evolution.GeneticSharp;
using System.Collections.Generic;
using System.Threading;

namespace NeuralMotion.Test
{
    //Implement from: https://github.com/karpathy/reinforcejs/blob/master/lib/rl.js
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 2));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new RegressionLayer());

            var rnd = new Random(DateTime.Now.Millisecond);

            var inputs = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 2, 1));
            var count = inputs.Shape.GetDimension(3);

            var trainer = new SgdTrainer(net);
            trainer.LearningRate = 0.1;
            trainer.L1Decay = 0;
            trainer.L2Decay = 0;
            trainer.Momentum = 0;
            trainer.BatchSize = count;

            //var qLearner = new BatchedDQNTrainer(net, trainer);
            var qLearner = new DQNAgent(net, 2)
            {
                alpha = 0.1,
                epsilon = 0.1,
                experience_add_every = 1,
                experience_size = 100,
                gamma = 0,
                learning_steps_per_iteration = 10,
                clamp_error_to = double.MaxValue
            };

            var loss = new List<double>();

            ShowAccuracy(rnd, net);

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                        break;

                    if (key.Key == ConsoleKey.A)
                    {
                        ShowAccuracy(rnd, net);
                        Thread.Sleep(500);
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.UpArrow)
                            trainer.LearningRate *= 1.5;
                        if (key.Key == ConsoleKey.DownArrow)
                            trainer.LearningRate /= 1.5;

                        if (trainer.LearningRate > 0.9)
                            trainer.LearningRate = 0.9;

                        Console.WriteLine($"LR: {trainer.LearningRate:0.000000}");
                        Thread.Sleep(500);
                    }
                }

                inputs.Storage.MapInplace(v => rnd.NextDouble());
                for (var i = 0; i < 1; i++)
                {
                    var expectedActions = GetClasses(inputs);
                    var predictedActions = new[] { qLearner.act(inputs.ToArray()) };

                    var rewards = new double[1][];
                    rewards[0] = new double[expectedActions.Length];
                    for (var l = 0; l < expectedActions.Length; l++)
                        rewards[0][l] = expectedActions[l] == predictedActions[l] ? 1 : 0;

                    qLearner.learn(rewards[0][0]);
                }

                var accuracy = GetAccuracy(rnd, net);

                loss.Add(qLearner.Loss);
                if (loss.Count > 1000)
                    loss.RemoveAt(0);

                Console.WriteLine($"ACC: {accuracy:0.00}   LOSS: {loss.Average():0.00000000}");
            }
        }

        private static double GetAccuracy(Random rnd, Net<double> net)
        {
            var validation = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 2, 1000));
            validation.Storage.MapInplace(v => rnd.NextDouble());
            var expectedValidation = GetClasses(validation);

            var output = net.Forward(validation);
            var predictedValidation = new int[validation.Shape.GetDimension(3)];
            for (var n = 0; n < predictedValidation.Length; n++)
                predictedValidation[n] = output.Get(0, 0, 0, n) > output.Get(0, 0, 1, n) ? 0 : 1;

            var accuracy = expectedValidation
                .Zip(predictedValidation, (e, p) => e == p ? 1.0 : 0)
                .Sum() / expectedValidation.Length;
            accuracy *= 100.0;

            return accuracy;
        }

        private static void ShowAccuracy(Random rnd, Net<double> net)
        {
            var accuracy = GetAccuracy(rnd, net);

            Console.WriteLine($"ACCURACY: {accuracy:0.00}%");
        }

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


        private static void ReinforcementTesting(
            Net<double> net, 
            Volume<double> inputs, 
            int[] expectedClasses)
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

        private static int[] GetClasses(Volume<double> inputs)
        {
            var count = inputs.Shape.GetDimension(3);
            var output = new int[count];
            for (var n = 0; n < count; n++)
            {
                var x = inputs.Get(0, 0, 0, n);
                var y = inputs.Get(0, 0, 0, n);

                if (x < 0.1 || x > 0.9)
                    output[n] = 1;
                if (y < 0.1 || y > 0.9)
                    output[n] = 1;
            }

            return output;
        }
    }
}
