﻿using System.Collections.Generic;
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
        public MovingStatistics Loss { get; }
        public MovingStatistics QValues { get; }
        public MovingStatistics Rewards { get; }

        private readonly Net<double> net;
        private readonly Dictionary<long, Action> pendingAction = null;

        public DQNController()
        {
            this.pendingAction = new Dictionary<long, Action>();
            this.Loss = new MovingStatistics(1000);
            this.QValues = new MovingStatistics(1000);
            this.Rewards = new MovingStatistics(1000);

            this.net = new Net<double>();
            this.net.AddLayer(new InputLayer(1, 1, this.InputLength));
            this.net.AddLayer(new FullyConnLayer(300));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(100));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(50));
            this.net.AddLayer(new LeakyReluLayer());
            this.net.AddLayer(new FullyConnLayer(OutputLength));
            this.net.AddLayer(new RegressionLayer());

            this.Trainer = new DQNTrainer(net, OutputLength)
            {
                Alpha = 0.01,
                ClampErrorTo = 1000.0,
                Epsilon = 0.5,
                Gamma = 0.5,
                ReplaysPerIteration = 10,
                ReplayMemorySize = 100000,
                ReplayMemoryDiscardStrategy = ExperienceDiscardStrategy.First,
                ReplaySkipCount = 0
            };
        }

        public void Control(Ball[] arena, Ball actor)
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

                HandleOutput(actor, action.Decision);
            }
            else
            {
                var action = Trainer.Act(inputs);
                HandleOutput(actor, action.Decision);
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

        public int InputLength => 32;
        public double[] SelectInput(Ball[] arena, Ball actor)
        {
            var neighbours = arena
                .Where(b => b.Id != actor.Id)
                .OrderBy(b => b.Position.Distance(actor.Position))
                .Select(b => new double[] {
                    b.Position.X,
                    b.Position.Y,
                    b.Speed.X,
                    b.Speed.Y
                })
                .Take(6)
                .SelectMany(b => b)
                .ToArray();

            var selection = new double[]
            {
                actor.Position.X,
                actor.Position.Y,
                1 - actor.Position.X,
                actor.Position.X + 1,
                1 - actor.Position.Y,
                actor.Position.Y + 1,
                actor.Speed.X,
                actor.Speed.Y
            };

            selection = selection
                .Concat(neighbours)
                .ToArray();

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