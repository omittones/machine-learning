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
using ConvNetSharp.Core.Layers;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using NeuralMotion.Views;
using Util;

namespace NeuralMotion.Test
{
    public static partial class Program
    {
        public static VolumeBuilder<double> build => BuilderInstance<double>.Volume;
        public static Random rnd = new Random(DateTime.Now.Millisecond);
        public static Func<Volume<double>, int[]> getClasses = GetClassesForXor;

        public static void Main(string[] args)
        {
            Ops<double>.SkipValidation = true;

            ConsoleWindow.Show();

            //ShowSet();
            RunPolicyGradients(rnd);
            //RunQLearning(rnd);
            //RunStochastic(rnd);
        }

        private static Task PlotNet(Net<double> net)
        {
            return Plot.Show(() => PlotWindow.ValueHeatmaps(
                net,
                minX: 0,
                maxX: 1,
                minY: 0,
                maxY: 1));
        }

        private static void RunPolicyGradients(Random rnd)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 2));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new SoftmaxLayer());

            ShowAccuracy(rnd, net);

            var task = PlotNet(net);

            var rewards = new MovingStatistics(100);
            var means = new MovingStatistics(100);
            var pgTrainer = new ReinforcementTrainer(net)
            {
                LearningRate = 5,
                Momentum = 0,
                L2Decay = 0,
                L1Decay = 0,
                RewardDiscountGamma = 0,
                ApplyBaselineAndNormalizeReturns = true
            };
            
            var inputs = BuilderInstance<double>.Volume.SameAs(Shape.From(1, 1, 2, 1));
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

                const int rolloutSteps = 1;
                var paths = new ConvNetSharp.Core.Training.Path[1000];

                for (var b = 0; b < paths.Length; b++)
                {
                    var path = new ConvNetSharp.Core.Training.Path();
                    var reward = 0.0;
                    for (var i = 0; i < rolloutSteps; i++)
                    {
                        inputs.MapInplace(v => rnd.NextDouble());
                        var expected = getClasses(inputs);

                        ActionGradient action;
                        lock (net)
                        {
                            action = pgTrainer.Act(inputs);
                        }

                        path.Add(action);
                        if (action.Action == expected[0])
                            reward += 1.0;
                        else
                            reward -= 1.0;

                    }
                    path.SetReward(reward / rolloutSteps, 0);
                    paths[b] = path;
                }
               

                lock (net)
                {
                    pgTrainer.Reinforce(paths);
                }

                epoch++;

                rewards.Push(pgTrainer.EstimatedRewards);
                means.Push(rewards.Mean);
                var speed = (means.Max - means.Min) / means.Count;

                Console.WriteLine($"{epoch:0000} REWARD: {rewards.Mean:0.000000000} SPEED:{speed:0.0000000} rew/epoch");
            }
        }

        private static void RunQLearning(Random rnd)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 2));
            net.AddLayer(new FullyConnLayer(30));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new RegressionLayer());

            ShowAccuracy(rnd, net);

            var task = PlotNet(net);

            var loss = new MovingStatistics(1000);
            var qvalues = new MovingStatistics(1000);
            var inputs = new double[] { 0, 0 };
            var qLearner = new DQNTrainer(net, 2)
            {
                LearningRate = 0.01,
                Epsilon = 0.1,
                ReplaySkipCount = 1,
                ReplayMemorySize = 1000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaysPerIteration = 10,
                Gamma = 0,
                ClampErrorTo = 1,
                L1Decay = 0,
                FreezeInterval = 0
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
                            qLearner.LearningRate *= 1.5;
                        if (key.Key == ConsoleKey.DownArrow)
                            qLearner.LearningRate /= 1.5;

                        if (qLearner.LearningRate > 0.9)
                            qLearner.LearningRate = 0.9;

                        Console.WriteLine($"LR: {qLearner.LearningRate:0.000000}");
                        Thread.Sleep(500);
                    }
                }

                lock (net)
                {
                    inputs = inputs.Select(v => rnd.NextDouble()).ToArray();
                    for (var i = 0; i < 1; i++)
                    {
                        var expectedActions = getClasses(inputs);

                        var predictedAction = qLearner.Act(inputs.ToArray());

                        var reward = expectedActions[0] == predictedAction.Action ? 1.0 : -1.0;

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

        public static void RunStochastic(Random rnd)
        {
            var net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, 2));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new SoftmaxLayer());

            ShowAccuracy(rnd, net);

            var task = PlotNet(net);

            var trainer = new CrossEntropyMethod(net)
            {
                Alpha = 0.1,
                InitialMean = 0,
                InitialStdDev = 0.2,
                SamplesCreatedOnEachIteration = 100,
                SamplesLeftAfterEvaluation = 50
            };

            int epoch = 0;
            int batchSize = 100;

            Volume<double> validation = null;
            int[] expectedValidation = null;

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
                        trainer.ClearCache();

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
                    if (expectedValidation == null || expectedValidation.Length != batchSize)
                    {
                        validation = BuilderInstance.Volume.SameAs(Shape.From(1, 1, 2, batchSize));
                        validation.Storage.MapInplace(v => rnd.NextDouble());
                        expectedValidation = getClasses(validation);
                    }

                    trainer.Train(sample =>
                    {
                        for (var i = 0; i < sample.Length; i++)
                            sample[i].DoMultiply(parameters[i].Volume, 1.0);
                        var output = net.Forward(validation);
                        var batchLoss = 0.0;
                        for (var n = 0; n < batchSize; n++)
                        {
                            var loss = output.Get(0, 0, expectedValidation[n], n);
                            if (loss == 0)
                                loss = 0.0000000001;
                            batchLoss += Math.Log(loss) / batchSize;
                        }
                        return batchLoss;
                    });

                    for (var i = 0; i < trainer.BestSample.Parameters.Length; i++)
                        trainer.BestSample.Parameters[i].DoMultiply(parameters[i].Volume, 1.0);

                    Console.WriteLine($"{epoch:0000} STDEVS:{trainer.AvgStdDevs:0.000} RETURNS: {trainer.BestSample?.Returns:0.0000}");
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
                var expectedValidation = getClasses(validation);

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
                var expectedValidation = getClasses(validation);

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

        private static int[] GetClassesForXor(Volume<double> inputs)
        {
            var count = inputs.BatchSize;
            var output = new int[count];
            for (var n = 0; n < count; n++)
            {
                var x = inputs.Get(0, 0, 0, n);
                var y = inputs.Get(0, 0, 1, n);
                output[n] = (x > 0.5) ^ (y > 0.5) ? 0 : 1;
            }

            return output;
        }

        private static int[] GetClassesForBorders(Volume<double> inputs)
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

        private static int[] GetPredefinedClasses(Volume<double> inputs, int klass)
        {
            var output = new int[inputs.BatchSize];
            for (var n = 0; n < inputs.BatchSize; n++)
                output[n] = klass;
            return output;
        }

        private static int[] GetClassesForCenter(Volume<double> inputs)
        {
            var output = new int[inputs.BatchSize];
            for (var n = 0; n < inputs.BatchSize; n++)
            {
                const double radius = 0.3;
                var x = inputs.Get(0, 0, 0, n);
                var y = inputs.Get(0, 0, 1, n);
                x = x - 0.5;
                y = y - 0.5;
                if (x * x + y * y < radius * radius)
                    output[n] = 1;
                else
                    output[n] = 0;
            }

            return output;
        }

        private static int[] GetClassesForSpiral(Volume<double> inputs)
        {
            var sx = new List<double>();
            var sy = new List<double>();
            for (var angle = 0.0; angle < Math.PI * 3; angle += 0.1)
            {
                var radius = 0.5 / (Math.PI * 3) * angle;
                sx.Add(0.5 + Math.Sin(angle) * radius);
                sy.Add(0.5 + Math.Cos(angle) * radius);
            }

            var output = new int[inputs.BatchSize];
            for (var n = 0; n < inputs.BatchSize; n++)
            {
                var ix = inputs.Get(0, 0, 0, n);
                var iy = inputs.Get(0, 0, 1, n);
                double minDist = double.MaxValue;
                for (var i = 0; i < sx.Count; i++)
                {
                    var d1 = ix - sx[i];
                    var d2 = iy - sy[i];
                    var dist = Math.Sqrt(d1 * d1 + d2 * d2);
                    if (minDist > dist)
                        minDist = dist;
                }
                output[n] = minDist < 0.05 ? 1 : 0;
            }

            return output;
        }
    }
}
