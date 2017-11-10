using System.Collections.Generic;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using NeuralMotion.Intelligence;
using System;

namespace NeuralMotion
{
    public class DQNCarController : IController<MountainCar>
    {
        public DQNTrainer Trainer { get; }
        public MovingStatistics Loss { get; }
        public MovingStatistics QValues { get; }
        public MovingStatistics Rewards { get; }
        public Net<double> Net { get; }

        private readonly double[] input;
        private readonly Dictionary<long, Action> pendingAction = null;

        public DQNCarController()
        {
            this.pendingAction = new Dictionary<long, Action>();
            this.Loss = new MovingStatistics(1000);
            this.QValues = new MovingStatistics(1000);
            this.Rewards = new MovingStatistics(1000);

            this.input = new double[2];
            this.Net = new Net<double>();
            this.Net.AddLayer(new InputLayer(1, 1, 2));
            this.Net.AddLayer(new FullyConnLayer(30));
            this.Net.AddLayer(new LeakyReluLayer());
            this.Net.AddLayer(new FullyConnLayer(20));
            this.Net.AddLayer(new LeakyReluLayer());
            this.Net.AddLayer(new FullyConnLayer(3));
            this.Net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(Net, 3)
            {
                LearningRate = 0.00001,
                L1Decay = 0.1,
                ClampErrorTo = 5,
                Epsilon = 1.0,
                Gamma = 0.9,
                ReplaysPerIteration = 1000,
                ReplayMemorySize = 1000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.AverageReward,
                ReplaySkipCount = 0
            };
        }

        private double last = 0;

        public bool Done { get; private set; }

        private int step = 0;
        public void Control(MountainCar environment)
        {
            const double epsDelta = 0.001;
            const double minEpsilon = 0.1;
            const double maxEpsilon = 1.0;

            if (environment.SimTime - last > 0.1 ||
                environment.SimTime < last)
            {
                last = environment.SimTime;

                lock (this.Net)
                {
                    input[0] = environment.CarPosition;
                    input[1] = environment.CarVelocity;

                    var decision = this.Trainer.Act(input);

                    environment.Action = decision.Action;

                    environment.Step();

                    var decrease = Math.Pow(Math.E, -epsDelta * step);
                    var range = maxEpsilon - minEpsilon;
                    this.Trainer.Epsilon = minEpsilon + range * decrease;
                    step++;

                    this.Trainer.Learn(
                        decision,
                        new[] { environment.CarPosition, environment.CarVelocity },
                        environment.Reward);

                    this.QValues.Push(this.Trainer.QValue);
                    this.Rewards.Push(environment.Reward);
                    this.Loss.Push(this.Trainer.Loss);

                    this.Done = environment.Done;
                }
            }
            else
            {
                environment.Step();
            }
        }
    }
}