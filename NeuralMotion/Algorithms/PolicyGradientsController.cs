using NeuralMotion.Simulator;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Core.Layers;

namespace NeuralMotion
{
    public class PolicyGradientsController : BallController
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
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(50));
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

        public override void ControlBall(Ball[] arena, Ball actor)
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
    }
}