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

        private readonly double[] input;
        private readonly Net<double> net;
        private readonly Dictionary<long, Action> pendingAction = null;

        public DQNCarController()
        {
            this.pendingAction = new Dictionary<long, Action>();
            this.Loss = new MovingStatistics(1000);
            this.QValues = new MovingStatistics(1000);
            this.Rewards = new MovingStatistics(1000);

            this.input = new double[2];
            this.net = new Net<double>();
            this.net.AddLayer(new InputLayer(1, 1, 2));
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(3));
            this.net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(net, 3)
            {
                LearningRate = 0.1,
                L1Decay = 0.01,
                ClampErrorTo = 1,
                Epsilon = 0.1,
                Gamma = 0.5,
                ReplaysPerIteration = 10,
                ReplayMemorySize = 100,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaySkipCount = 0
            };
        }

        public void Control(MountainCar environment)
        {
            input[0] = environment.CarPosition;
            input[1] = environment.CarVelocity;

            var action = this.Trainer.Act(input);

            environment.Action = action.Decision;
            environment.Step();

            this.Trainer.Learn(action,
                new[] { environment.CarPosition, environment.CarVelocity },
                environment.Reward);
        }
    }
}