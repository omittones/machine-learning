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

namespace NeuralMotion.Test
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            //var input = B.SameAs(new double[] { 1, 2, 3, 1, 1, 0, 0, 1, 0 }, Shape.From(1, 1, 3, 3));
            //var mu = MuPolicy(input);
            //Console.WriteLine("Mu:");
            //Console.WriteLine(mu.ToString("0.00"));

            //var log_mu = LogMuPolicy(mu);
            //Console.WriteLine("Log Mu:");
            //Console.WriteLine(log_mu.ToString("0.00"));

            ////var alt_log_mu = AltLogMuPolicy(mu);
            ////Console.WriteLine(alt_log_mu.ToString("0.00"));
            ////return;

            //var grad_log_mu = GradientLogMuPolicy(input, mu, 1, 1);
            //Console.WriteLine("Grad Log Mu (1, 1):");
            //Console.WriteLine(grad_log_mu.ToString());

            //input = B.SameAs(new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }, Shape.From(1, 1, 3, 3));
            //mu = MuPolicy(input);
            //var grad = PolicyGradient(input, mu,
            //new int[][] {
            //    new[] { 0, 0 },
            //    new[] { 1 }
            //}, new double[] { 0.1, 1.0 });

            //Console.WriteLine("Policy Grad:");
            //Console.WriteLine(grad.ToString());

            //return;

            //var net = new Net<double>();
            //net.AddLayer(new InputLayer(1, 1, 2));
            //net.AddLayer(new FullyConnLayer(20));
            //net.AddLayer(new LeakyReluLayer());
            //net.AddLayer(new FullyConnLayer(10));
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

            var rnd = new Random(DateTime.Now.Millisecond);

            RunPolicyGradients(rnd);
            //RunQLearning(rnd);
            //RunStochastic(rnd);
        }

        private static Task PlotOutput(Net<double> net, bool showClasses = false)
        {
            var task = Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main(net)
                {
                    ShowClasses = showClasses
                });

                Console.WriteLine("Display thread stopped!");
            });

            while (task.Status == TaskStatus.WaitingForActivation ||
                task.Status == TaskStatus.WaitingToRun ||
                task.Status == TaskStatus.Created)
                Thread.Sleep(0);

            return task;
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
            net.AddLayer(new ReinforcementLayer());

            ShowAccuracy(rnd, net);

            var task = PlotOutput(net);

            var rewards = new MovingStatistics(100);
            var means = new MovingStatistics(100);
            var pgTrainer = new ReinforcementTrainer(net, rnd)
            {
                BatchSize = 1000,
                Momentum = 0,
                L2Decay = 0,
                L1Decay = 0,
                LearningRate = 0.1
            };
            const int rolloutSteps = 10;

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

                inputs.MapInplace(v => rnd.NextDouble());
                var expected = GetClassesForCenter(inputs);
                //var expected = GetClassesForBorders(inputs);
                //var expected = Enumerable.Repeat(0, inputs.BatchSize).ToArray();

                var pathRewards = new double[pgTrainer.BatchSize / rolloutSteps];
                var pathActions = new int[pgTrainer.BatchSize / rolloutSteps][];
                lock (net)
                {
                    var predicted = pgTrainer.Act(inputs);
                    for (var i = 0; i < inputs.BatchSize; i++)
                    {
                        var xPath = i / rolloutSteps;
                        var xAction = i - xPath * rolloutSteps;
                        if (pathActions[xPath] == null)
                            pathActions[xPath] = new int[rolloutSteps];
                        pathActions[xPath][xAction] = predicted[i];

                        if (expected[i] != predicted[i])
                            pathRewards[xPath] += -1.0 / rolloutSteps;
                        else
                            pathRewards[xPath] += 1.0 / rolloutSteps;
                    }

                    pgTrainer.Reinforce(inputs, pathActions, pathRewards);

                    epoch++;
                }

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
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(2));
            net.AddLayer(new RegressionLayer());

            ShowAccuracy(rnd, net);

            var task = PlotOutput(net);

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
                        var expectedActions = GetClassesForCenter(inputs);

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

            var task = PlotOutput(net);

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
                        expectedValidation = GetClassesForCenter(validation);
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
                var expectedValidation = GetClassesForBorders(validation);

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
                var expectedValidation = GetClassesForBorders(validation);

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
    }
}
