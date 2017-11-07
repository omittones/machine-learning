using System.Collections.Generic;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using NeuralMotion.Intelligence;

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
            this.Net.AddLayer(new FullyConnLayer(50));
            this.Net.AddLayer(new LeakyReluLayer());
            this.Net.AddLayer(new FullyConnLayer(50));
            this.Net.AddLayer(new LeakyReluLayer());
            this.Net.AddLayer(new FullyConnLayer(3));
            this.Net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(Net, 3)
            {
                LearningRate = 0.1,
                L1Decay = 0.0,
                ClampErrorTo = double.MaxValue,
                Epsilon = 0,
                Gamma = 0.9,
                ReplaysPerIteration = 100,
                ReplayMemorySize = 10000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaySkipCount = 0
            };
        }

        private double last = 0;
        public void Control(MountainCar environment)
        {
            if (environment.SimTime - last > 1.0 ||
                environment.SimTime < last)
            {
                last = environment.SimTime;

                lock (this.Net)
                {
                    input[0] = environment.CarPosition;
                    input[1] = environment.CarVelocity;

                    var action = this.Trainer.Act(input);

                    environment.Action = action.Decision;

                    environment.Step();

                    this.Trainer.Learn(
                        action,
                        new[] { environment.CarPosition, environment.CarVelocity },
                        environment.Reward);

                    this.QValues.Push(this.Trainer.QValue);
                    this.Rewards.Push(environment.Reward);
                    this.Loss.Push(this.Trainer.Loss);
                }
            }
            else
            {
                environment.Step();
            }
        }
    }
}