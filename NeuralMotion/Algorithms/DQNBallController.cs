using System.Collections.Generic;
using NeuralMotion.Simulator;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;

namespace NeuralMotion
{
    public class DQNBallController : BallController
    {
        public DQNTrainer Trainer { get; }
        public MovingStatistics Loss { get; }
        public MovingStatistics QValues { get; }
        public MovingStatistics Rewards { get; }

        private readonly Net<double> net;
        private readonly Dictionary<long, Decision> pendingAction = null;

        public DQNBallController()
        {
            this.pendingAction = new Dictionary<long, Decision>();
            this.Loss = new MovingStatistics(1000);
            this.QValues = new MovingStatistics(1000);
            this.Rewards = new MovingStatistics(1000);

            this.net = new Net<double>();
            this.net.AddLayer(new InputLayer(1, 1, this.InputLength));
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer(0.3));
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer(0.3));
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer(0.3));
            this.net.AddLayer(new FullyConnLayer(OutputLength));
            this.net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(net, OutputLength)
            {
                LearningRate = 0.1,
                ClampErrorTo = 1,
                Epsilon = 0.1,
                Gamma = 0.5,
                ReplaysPerIteration = 10,
                ReplayMemorySize = 100000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaySkipCount = 0
            };
        }

        public override void ControlBall(Ball[] arena, Ball actor)
        {
            var inputs = SelectInput(arena, actor);

            if (actor.Id == 0)
            {
                if (pendingAction.TryGetValue(actor.Id, out var action))
                {
                    var reward = GetReward(arena, actor);

                    Trainer.Learn(action, inputs, reward);

                    Loss.Push(Trainer.Loss);
                    QValues.Push(Trainer.QValue);
                    Rewards.Push(reward);
                }

                action = Trainer.Act(inputs);
                pendingAction[actor.Id] = action;

                HandleOutput(actor, action.Action);
            }
            else
            {
                var action = Trainer.Act(inputs);
                HandleOutput(actor, action.Action);
            }
        }
    }
}