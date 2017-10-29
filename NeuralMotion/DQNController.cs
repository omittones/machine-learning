using System.Collections.Generic;
using System.Linq;
using NeuralMotion.Intelligence;
using NeuralMotion.Simulator;
using Util;
using ConvNetSharp.Core;
using System.Diagnostics;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using Accord.Statistics.Moving;

namespace NeuralMotion
{
    public class DQNController : IController
    {
        public DQNTrainer Trainer { get; }
        public MovingNormalStatistics Loss { get; }
        public MovingNormalStatistics Reward { get; }
        
        private readonly Net<double> net;
        private readonly Dictionary<long, Action> pendingAction = null;
        private readonly Dictionary<long, double> priorReward = null;

        public DQNController()
        {
            this.pendingAction = new Dictionary<long, Action>();
            this.priorReward = new Dictionary<long, double>();
            this.Loss = new MovingNormalStatistics(1000);
            this.Reward = new MovingNormalStatistics(1000);

            this.net = new Net<double>();
            this.net.AddLayer(new InputLayer(1, 1, this.InputLength));
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(20));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(OutputLength));
            this.net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(net, OutputLength)
            {
                Alpha = 0.001,
                ClampErrorTo = 1000.0,
                Epsilon = 0.1,
                Gamma = 0.1,
                LearningStepsPerIteration = 100,
                ReplayMemorySize = 1000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaySkipCount = 0
            };
        }

        public void Control(Ball[] arena, Ball actor)
        {
            var inputs = SelectInput(arena, actor);
            var currentReward = GetReward(actor);

            if (pendingAction.TryGetValue(actor.Id, out var action))
            {
                //var reward = currentReward - priorReward[actor.Id];
                var reward = currentReward;
                Trainer.Learn(action, inputs, reward);
                Loss.Push(Trainer.Loss);
                Reward.Push(reward);
            }

            priorReward[actor.Id] = currentReward;

            action = Trainer.Act(inputs);
            pendingAction[actor.Id] = action;

            HandleOutput(actor, action.Decision);
        }

        public double GetReward(Ball ball)
        {
            return (1 / (ball.Position.Distance(0, 0) + 1)) * 10.0;

            var totalKicks = ball.KicksToBorder + ball.KicksToBall + ball.KicksFromBall;
            return (ball.DistanceTravelled - ball.Energy - totalKicks * 5);
        }

        public int InputLength => 8;
        public double[] SelectInput(Ball[] arena, Ball actor)
        {
            var closestBalls = arena
                .OrderBy(b => b.Position.Distance(actor.Position))
                .Take(4)
                .ToArray();
            Debug.Assert(object.ReferenceEquals(closestBalls[0], actor));

            //var polarClosest = closestBalls
            //    .Skip(1)
            //    .Select(b => b.Position.RelativeTo(actor.Position))
            //    .ToArray();

            var selection = new double[]
            {
                actor.Position.X,
                actor.Position.Y,
                1 - actor.Position.X,
                actor.Position.X + 1,
                1 - actor.Position.Y,
                actor.Position.Y + 1,
                actor.Speed.X,
                actor.Speed.Y,
                //polarClosest[0].X,
                //polarClosest[0].Y,
                //polarClosest[1].X,
                //polarClosest[1].Y,
                //polarClosest[2].X,
                //polarClosest[2].Y
            };

            Debug.Assert(selection.Length == this.InputLength);

            return selection;
        }

        public int OutputLength => 5;
        public void HandleOutput(Ball ball, int action)
        {
            Debug.Assert(action >= 0 && action < this.OutputLength);
            switch (action)
            {
                case 0:
                    ball.Acceleration.X = 0;
                    ball.Acceleration.Y = 0;
                    break;
                case 1:
                    ball.Acceleration.X = 0;
                    ball.Acceleration.Y = 0.8f;
                    break;
                case 2:
                    ball.Acceleration.X = 0;
                    ball.Acceleration.Y = -0.8f;
                    break;
                case 3:
                    ball.Acceleration.X = 0.8f;
                    ball.Acceleration.Y = 0;
                    break;
                case 4:
                    ball.Acceleration.X = -0.8f;
                    ball.Acceleration.Y = 0;
                    break;
            }
        }
    }
}