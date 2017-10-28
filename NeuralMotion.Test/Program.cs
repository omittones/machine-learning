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
using System.Windows.Forms;
using System.Threading.Tasks;
using NeuralMotion.Evolution.Annealing;
using NeuralMotion.Evolution.Custom;

namespace NeuralMotion.Test
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 2));
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(5));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new SigmoidLayer());
            net.AddLayer(new RegressionLayer());

            var task = Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main(net));

                Console.WriteLine("Display thread stopped!");
            });

            var rnd = new Random(DateTime.Now.Millisecond);

            ShowAccuracy(rnd, net);

            RunQLearning(net, task, rnd);
            //RunGenetic(net, task, rnd);

            task.Wait();
        }

        private static void RunQLearning(Net<double> net, Task task, Random rnd)
        {
            var loss = new List<double>();
            var inputs = new double[] { 0, 0 };
            var qLearner = new DQNTrainer(net, 2)
            {
                Alpha = 0.01,
                Epsilon = 0.1,
                ReplaySkipCount = 1,
                ReplayMemorySize = 1000,
                Gamma = 0,
                LearningStepsPerIteration = 100,
                ClampErrorTo = double.MaxValue
            };

            while (task.Status == TaskStatus.Running)
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
                            qLearner.Alpha *= 1.5;
                        if (key.Key == ConsoleKey.DownArrow)
                            qLearner.Alpha /= 1.5;

                        if (qLearner.Alpha > 0.9)
                            qLearner.Alpha = 0.9;

                        Console.WriteLine($"LR: {qLearner.Alpha:0.000000}");
                        Thread.Sleep(500);
                    }
                }

                lock (net)
                {
                    inputs = inputs.Select(v => rnd.NextDouble()).ToArray();
                    for (var i = 0; i < 1; i++)
                    {
                        var expectedActions = GetClasses(inputs);

                        var predictedAction = qLearner.Act(inputs.ToArray());

                        var reward = expectedActions[0] == predictedAction.Decision ? 1.0 : 0.0;

                        qLearner.Learn(predictedAction, inputs.ToArray(), reward);
                    }
                }

                loss.Add(qLearner.Loss);
                if (loss.Count > 1000)
                    loss.RemoveAt(0);

                if (qLearner.Samples % 100 == 0)
                {
                    var accuracy = GetAccuracy(rnd, net);
                    Console.WriteLine($"{qLearner.Samples:0000} ACC: {accuracy:0.00}   LOSS: {loss.Average():0.00000000}");
                }
            }
        }

        public static void RunGenetic(Net<double> net, Task task, Random rnd)
        {
            var inputs = BuilderInstance<double>.Volume.SameAs(Shape.From(1, 1, 2, 1000));
            inputs.MapInplace(v => rnd.NextDouble());
            var expectedClasses = GetClasses(inputs);

            var outputs = expectedClasses.ToSoftmaxVolume();
            var fitness = new SoftmaxLossFitness(net, inputs, outputs);
            var evolver = new ShrinkingRadiusSearch(100, (ArrayChromosome)fitness.ProtoChromosome, fitness, 10);
            //var evolver = new Evolver(fitness.ProtoChromosome, 100, fitness, mutation: new NumberMutation());
            //var evolver = new SimulatedAnnealing((ArrayChromosome)fitness.ProtoChromosome, fitness,
            //    step: 1,
            //    alpha: 0.9999);

            while (task.Status == TaskStatus.Running)
            {
                lock (net)
                {
                    if (evolver.CurrentChampGenome != null)
                        net.FromChromosome(evolver.CurrentChampGenome);

                    evolver.PerformSingleStep();

                    net.FromChromosome(evolver.CurrentChampGenome);

                    var accuracy = GetAccuracy(rnd, net);

                    Console.WriteLine($"{evolver.CurrentGeneration * evolver.Size:0000}  LOSS: {evolver.BestFitness:0.00} ACC:{accuracy:0.00}%");
                    foreach (var line in evolver.StatusReport())
                        Console.WriteLine(line);
                }
            }
        }

        private static double GetAccuracy(Random rnd, Net<double> net)
        {
            lock (net)
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
        }

        private static void ShowAccuracy(Random rnd, Net<double> net)
        {
            var accuracy = GetAccuracy(rnd, net);

            Console.WriteLine($"ACCURACY: {accuracy:0.00}%");
        }
        
        private static int[] GetClasses(Volume<double> inputs)
        {
            var count = inputs.Shape.GetDimension(3);
            var output = new int[count];
            for (var n = 0; n < count; n++)
            {
                var x = inputs.Get(0, 0, 0, n);
                var y = inputs.Get(0, 0, 1, n);

                if (x < 0.2 || x > 0.8)
                    output[n] = 1;
                if (y < 0.2 || y > 0.8)
                    output[n] = 1;
            }

            return output;
        }
    }
}
