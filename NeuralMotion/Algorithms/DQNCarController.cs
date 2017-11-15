using System.Collections.Generic;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using NeuralMotion.Intelligence;
using System;
using System.Threading;
using System.Linq;

namespace NeuralMotion
{
    public class DQNCarController : IController<MountainCar>
    {
        public DQNTrainer Trainer { get; }
        public MovingStatistics Loss { get; }
        public MovingStatistics QValues { get; }
        public MovingStatistics Rewards { get; }
        public Net<double> Net { get; }

        public bool Done { get; private set; }
        public int GoalReached { get; private set; }
        public int TrainingTimedout { get; private set; }

        private readonly double[] state;
        private readonly Dictionary<long, Action> pendingAction = null;

        public DQNCarController()
        {
            this.pendingAction = new Dictionary<long, Action>();
            this.Loss = new MovingStatistics(1000);
            this.QValues = new MovingStatistics(1000);
            this.Rewards = new MovingStatistics(1000);

            this.state = new double[2];
            this.Net = new Net<double>();
            this.Net.AddLayer(new InputLayer(1, 1, 2));
            this.Net.AddLayer(new FullyConnLayer(10));
            this.Net.AddLayer(new LeakyReluLayer());
            this.Net.AddLayer(new FullyConnLayer(3));
            this.Net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(Net, 3)
            {
                LearningRate = 0.001,
                L1Decay = 0.0,
                Epsilon = 1.0,
                Gamma = 0.99,
                ReplayMemorySize = 1000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.AverageReward,
                ReplaysPerIteration = 1000,
                ReplaySkipCount = 0,
                ClampErrorTo = 100,
                MaxQValue = DQNTrainer.TheoreticalMaxQValue(0.99, 1),
                MinQValue = DQNTrainer.TheoreticalMinQValue(0.99, 0),
                FreezeInterval = 200
            };

            this.GoalReached = 0;
            this.TrainingTimedout = 0;
        }

        private int step = 0;
        private double reward = double.MinValue;
        private Decision? decision = null;
        public void Control(MountainCar environment)
        {
            const double epsDelta = 0.0009;
            const double minEpsilon = 0.1;
            const double maxEpsilon = 1.0;

            if (step % 10 == 0)
            {
                state[0] = environment.CarPosition;
                state[1] = environment.CarVelocity * 10;

                lock (this.Net)
                {
                    if (decision.HasValue)
                    {
                        this.Trainer.Learn(
                            decision.Value,
                            state,
                            reward);

                        this.QValues.Push(this.Trainer.QValue);
                        this.Loss.Push(this.Trainer.Loss);
                    }

                    decision = this.Trainer.Act(state);
                }

                environment.Action = decision.Value.Action;

                var decrease = Math.Pow(Math.E, -epsDelta * step);
                var range = maxEpsilon - minEpsilon;
                this.Trainer.Epsilon = minEpsilon + range * decrease;

                reward = double.MinValue;
            }

            environment.Step();
            this.Rewards.Push(environment.Reward);
            if (environment.Reward > reward)
                reward = environment.Reward;

            this.Done = environment.Done ||
                    environment.SimTime > 10;
            if (this.Done && environment.Done)
                this.GoalReached++;
            else if (this.Done)
                this.TrainingTimedout++;

            step++;
        }
    }
}