using System.Collections.Generic;
using System.Linq;
using NeuralMotion.Intelligence;
using NeuralMotion.Simulator;
using Util;
using ConvNetSharp.Core;
using System.Diagnostics;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;

namespace NeuralMotion
{
    public class DQNController : IController
    {
        public DQNTrainer Trainer { get; }

        private readonly Net<double> net;
        private readonly Dictionary<long, Action> pendingAction = null;
        private readonly Dictionary<long, double> priorReward = null;
        public int InputLength => 12;
        public int OutputLength => 5;

        public DQNController()
        {
            this.pendingAction = new Dictionary<long, Action>();
            this.priorReward = new Dictionary<long, double>();

            this.net = new Net<double>();
            this.net.AddLayer(new InputLayer(1, 1, this.InputLength));
            this.net.AddLayer(new FullyConnLayer(20));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(10));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(OutputLength));
            this.net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(net, OutputLength)
            {
                Alpha = 0.1,
                ClampErrorTo = double.MaxValue,
                Epsilon = 0.1,
                Gamma = 0.9,
                LearningStepsPerIteration = 10,
                ReplayMemorySize = 1000,
                ReplaySkipCount = 0
            };
        }
        
        public void Control(Ball[] arena, Ball actor)
        {
            var inputs = SelectInput(arena, actor);
            var currentReward = Reward(actor);

            if (pendingAction.TryGetValue(actor.Id, out var action))
            {
                var reward = currentReward - priorReward[actor.Id];
                Trainer.Learn(action, inputs, reward);
            }

            priorReward[actor.Id] = currentReward;
            action = Trainer.Act(inputs);
            pendingAction[actor.Id] = action;

            HandleOutput(actor, action.Decision);
        }

        public double Reward(Ball ball)
        {
            return -ball.Energy;

            var totalKicks = ball.KicksToBorder + ball.KicksToBall + ball.KicksFromBall + 1;

            return ball.DistanceTravelled - ball.Energy - totalKicks;
        }

        public double[] SelectInput(Ball[] arena, Ball actor)
        {
            var closestBalls = arena
                .OrderBy(b => b.Position.Distance(actor.Position))
                .Take(4)
                .ToArray();

            Debug.Assert(object.ReferenceEquals(closestBalls[0], actor));

            var ballSpeed = actor.Speed;

            var polarClosest = closestBalls
                .Skip(1)
                .Select(b => b.Position.RelativeTo(actor.Position))
                .ToArray();

            var selection = new double[]
            {
                1 - actor.Position.X,
                actor.Position.X + 1,
                1 - actor.Position.Y,
                actor.Position.Y + 1,
                ballSpeed.X,
                ballSpeed.Y,
                polarClosest[0].X,
                polarClosest[0].Y,
                polarClosest[1].X,
                polarClosest[1].Y,
                polarClosest[2].X,
                polarClosest[2].Y
            };

            Debug.Assert(selection.Length == this.InputLength);

            return selection;
        }

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
                    ball.Acceleration.Y = 0.2f;
                    break;
                case 2:
                    ball.Acceleration.X = 0;
                    ball.Acceleration.Y = -0.2f;
                    break;
                case 3:
                    ball.Acceleration.X = 0.2f;
                    ball.Acceleration.Y = 0;
                    break;
                case 4:
                    ball.Acceleration.X = -0.2f;
                    ball.Acceleration.Y = 0;
                    break;
            }
        }
    }
}