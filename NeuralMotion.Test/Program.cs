using System;
using ConvNetSharp.Core.Training.Double;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume.Double;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using ConvNetSharp.Core.Training;
using NeuralMotion.Evolution;

namespace NeuralMotion.Test
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            //var net = new Net<double>();
            //net.AddLayer(new InputLayer(1, 1, 2));
            //net.AddLayer(new FullyConnLayer(3));
            //net.AddLayer(new LeakyReluLayer());
            //net.AddLayer(new FullyConnLayer(3));
            //net.AddLayer(new LeakyReluLayer());
            //net.AddLayer(new FullyConnLayer(2));
            //net.AddLayer(new ReinforcementLayer());

            //var net = new Net<double>();
            //net.AddLayer(new InputLayer(1, 1, 2));
            //net.AddLayer(new FullyConnLayer(20));
            //net.AddLayer(new LeakyReluLayer());
            //net.AddLayer(new FullyConnLayer(10));
            //net.AddLayer(new LeakyReluLayer());
            //net.AddLayer(new FullyConnLayer(2));
            //net.AddLayer(new RegressionLayer());

            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 2));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new SoftmaxLayer());

            var task = Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main(net));

                Console.WriteLine("Display thread stopped!");
            });

            var rnd = new Random(DateTime.Now.Millisecond);

            ShowAccuracy(rnd, net);

            //RunPolicyGradients(net, task, rnd);
            //RunQLearning(net, task, rnd);
            RunStochastic(net, task, rnd);

            task.Wait();
        }

        private static void RunPolicyGradients(Net<double> net, Task task, Random rnd)
        {
            var loss = new MovingStatistics(1000);
            var pgTrainer = new ReinforcementTrainer(net, rnd)
            {
                BatchSize = 1000,
                Momentum = 0,
                L2Decay = 0,
                L1Decay = 0,
                LearningRate = 0.1
            };

            var inputs = BuilderInstance<double>.Volume.SameAs(Shape.From(1, 1, 2, pgTrainer.BatchSize));
            int epoch = 0;
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
                            pgTrainer.LearningRate *= 1.5;
                        if (key.Key == ConsoleKey.DownArrow)
                            pgTrainer.LearningRate /= 1.5;

                        if (pgTrainer.LearningRate > 0.9)
                            pgTrainer.LearningRate = 0.9;

                        Console.WriteLine($"LR: {pgTrainer.LearningRate:0.000000}");
                        Thread.Sleep(500);
                    }
                }

                var rewards = new double[pgTrainer.BatchSize];
                var averages = new double[pgTrainer.BatchSize / 20];
                lock (net)
                {
                    inputs.MapInplace(v => rnd.NextDouble());

                    var expected = GetClasses(inputs);
                    var predicted = pgTrainer.Act(inputs);
                    for (var i = 0; i < rewards.Length; i++)
                    {
                        if (expected[i] != predicted[i])
                            averages[i / 20] += -1.0;
                        else
                            averages[i / 20] += 1.0;
                    }

                    var mean = averages.Average();
                    averages = averages.Select(a => a - mean).ToArray();
                    var stdev = averages.Select(a => a * a).Average();
                    averages = averages.Select(a => a / stdev).ToArray();

                    for (var i = 0; i < rewards.Length; i++)
                        rewards[i] = averages[i / 20];

                    pgTrainer.Reinforce(inputs, predicted, rewards);

                    epoch++;
                }

                loss.Push(pgTrainer.Loss);

                Console.WriteLine($"{epoch:0000} LOSS: {loss.Mean:0.000000000}");
            }
        }

        private static void RunQLearning(Net<double> net, Task task, Random rnd)
        {
            var loss = new MovingStatistics(1000);
            var qvalues = new MovingStatistics(1000);
            var inputs = new double[] { 0, 0 };
            var qLearner = new DQNTrainer(net, 2)
            {
                Alpha = 0.001,
                Epsilon = 0.1,
                ReplaySkipCount = 1,
                ReplayMemorySize = 10000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaysPerIteration = 100,
                Gamma = 0,
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

                        var reward = expectedActions[0] == predictedAction.Decision ? 1.0 : -1.0;

                        qLearner.Learn(predictedAction, inputs.ToArray(), reward);
                    }
                }

                loss.Push(qLearner.Loss);
                qvalues.Push(qLearner.QValue);
                if (qLearner.Samples % 100 == 0)
                {
                    Console.WriteLine($"{qLearner.Samples:0000} LOSS: {loss.Mean:0.000000000} QV:{qvalues.Min:0.000}...{qvalues.Max:0.000}");
                }
            }
        }

        public static void RunStochastic(Net<double> net, Task task, Random rnd)
        {
            var trainer = new CrossEntropyMethod(net)
            {
                Alpha = 0.01,
                InitialMean = 0,
                InitialStdDev = 2,
                SamplesCreatedOnEachIteration = 100,
                SamplesLeftAfterEvaluation = 50
            };

            int epoch = 0;
            int batchSize = 10;
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
                    else if (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow)
                    {
                        if (key.Key == ConsoleKey.UpArrow)
                            trainer.Alpha *= 1.5;
                        if (key.Key == ConsoleKey.DownArrow)
                            trainer.Alpha /= 1.5;

                        if (trainer.Alpha > 1.0)
                            trainer.Alpha = 1.0;

                        Console.WriteLine($"LR: {trainer.Alpha:0.000000}");
                        Thread.Sleep(100);
                    }
                    else if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow)
                    {
                        if (key.Key == ConsoleKey.RightArrow)
                            batchSize *= 2;
                        if (key.Key == ConsoleKey.LeftArrow)
                            batchSize /= 2;

                        if (batchSize < 1)
                            batchSize = 1;

                        Console.WriteLine($"BS: {batchSize}");
                        Thread.Sleep(100);
                    }
                }

                var parameters = net.GetParametersAndGradients();

                lock (net)
                {
                    trainer.Train(sample =>
                    {
                        for (var i = 0; i < sample.Length; i++)
                            sample[i].DoMultiply(parameters[i].Volume, 1.0);
                        return GetLoss(rnd, net, batchSize);
                    });

                    for (var i = 0; i < trainer.BestSample.Parameters.Length; i++)
                        trainer.BestSample.Parameters[i].DoMultiply(parameters[i].Volume, 1.0);

                    Console.WriteLine($"{epoch:0000} RETURNS: {trainer.BestSample?.Returns:0.0000}");
                }

                epoch++;
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

        private static double GetLoss(Random rnd, Net<double> net, int batchSize)
        {
            lock (net)
            {
                var validation = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 2, batchSize));
                validation.Storage.MapInplace(v => rnd.NextDouble());
                var expectedValidation = GetClasses(validation);

                var output = net.Forward(validation);

                var batchLoss = 0.0;
                for (var n = 0; n < batchSize; n++)
                {
                    var loss = output.Get(0, 0, expectedValidation[n], n);
                    batchLoss += Math.Log(loss) / batchSize;
                }

                return batchLoss;
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

                output[n] = 0;
                if (x < 0.2 || x > 0.8)
                    output[n] = 1;
                if (y < 0.2 || y > 0.8)
                    output[n] = 1;
            }

            return output;
        }
    }
}
