using System.Linq;
using NeuralMotion.Intelligence;
using NeuralMotion.Simulator;
using Util;
using ConvNetSharp.Core;
using System.Diagnostics;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Core.Layers;

namespace NeuralMotion
{
    public class PolicyGradientsController : IController
    {
        public ReinforcementTrainer Trainer { get; }
        public MovingStatistics Rewards { get; }
        public int Samples { get; private set; }

        private readonly Net<double> net;
        private readonly RolloutCollection rollouts;
        
        public PolicyGradientsController(
            int nmPaths, 
            int nmActions)
        {
            this.Rewards = new MovingStatistics(1000);
            this.rollouts = new RolloutCollection(nmPaths, nmActions);

            this.net = new Net<double>();
            this.net.AddLayer(new InputLayer(1, 1, this.InputLength));
            this.net.AddLayer(new FullyConnLayer(20));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(10));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(OutputLength));
            this.net.AddLayer(new ReinforcementLayer());

            this.Trainer = new ReinforcementTrainer(net)
            {
                LearningRate = 0.1,
                ApplyBaselineAndNormalizeReturns = true,
                RewardDiscountGamma = 0.5,
                BatchSize = nmPaths * nmActions
            };
        }

        public void Control(Ball[] arena, Ball actor)
        {
            var inputs = SelectInput(arena, actor);

            var action = Trainer.Act(inputs);

            HandleOutput(actor, action[0]);

            this.rollouts.AppendToPath(actor.Id, inputs, action[0], () =>
            {
                var reward = GetReward(arena, actor);
                Rewards.Push(reward);
                return reward;
            });

            if (this.rollouts.Full)
            {
                this.rollouts.Apply(Trainer);
                this.Samples++;
            }
        }

        public double GetReward(Ball[] arena, Ball actor)
        {
            double reward;
            var totalKicks = actor.KicksToBorder + actor.KicksToBall;
            if (totalKicks > 0)
                reward = -totalKicks * 6;
            else
                reward = actor.DistanceTravelled * 2;

            actor.Reset();

            return reward;
        }

        public int InputLength => 12;
        public double[] SelectInput(Ball[] arena, Ball actor)
        {
            var neighbours = arena
                .Where(b => b.Id != actor.Id)
                .OrderBy(b => b.Position.Distance(actor.Position))
                .Select(b => new double[] {
                    b.Position.X - actor.Position.X,
                    b.Position.Y - actor.Position.Y,
                    //b.Speed.X,
                    //b.Speed.Y
                })
                .Take(6)
                .SelectMany(b => b)
                .ToArray();

            var selection = new double[]
            {
                //actor.Position.X,
                //actor.Position.Y,
                //1 - actor.Position.X,
                //actor.Position.X + 1,
                //1 - actor.Position.Y,
                //actor.Position.Y + 1,
                //actor.Speed.X,
                //actor.Speed.Y
            };

            selection = selection
                .Concat(neighbours)
                .ToArray();

            if (actor.Id == 0)
            {
                var indicators = new IIndicator[6];
                var pos = actor.Position;
                for (var i = 0; i < 6; i++)
                {
                    var next = pos.Offset((float)neighbours[i * 2], (float)neighbours[i * 2 + 1]);
                    indicators[i] = new Line(pos, next);
                }
                actor.Indicators = indicators;
            }

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