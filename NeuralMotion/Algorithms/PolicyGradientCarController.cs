﻿using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using NeuralMotion.Intelligence;
using System.Collections.Generic;

namespace NeuralMotion
{
    public class PolicyGradientCarController : IController<MountainCar>
    {
        public VanillaPolicyGradientTrainer Trainer { get; }
        public MovingStatistics Rewards { get; }
        public Net<double> Net { get; }

        private readonly double[] state;
        private readonly List<Path> paths;
        private Path path;
        private double reward;

        public bool Done { get; private set; }
        public int GoalReached { get; private set; }
        public int TrainingTimedout { get; private set; }
        public int Samples { get; private set; }
        public int Epochs { get; private set; }

        public PolicyGradientCarController()
        {
            this.Net = new Net<double>();
            this.Net.AddLayer(new InputLayer(1, 1, 2));
            this.Net.AddLayer(new FullyConnLayer(10));
            this.Net.AddLayer(new LeakyReluLayer(0.3));
            this.Net.AddLayer(new FullyConnLayer(3));
            this.Net.AddLayer(new SoftmaxLayer(3));

            this.Rewards = new MovingStatistics(1000);

            this.state = new double[2];
            this.paths = new List<Path>();

            this.Trainer = new VanillaPolicyGradientTrainer(Net)
            {
                LearningRate = 0.1
            };

            this.GoalReached = 0;
            this.Samples = 0;
            this.Epochs = 0;
        }

        public void Control(MountainCar environment)
        {
            if (this.path == null)
            {
                this.path = new Path();
                this.reward = double.MinValue;
            }

            if (environment.SimFrame % 10 == 0)
            {
                this.Samples++;

                state[0] = environment.CarPosition;
                state[1] = environment.CarVelocity * 10;

                ActionInput action;
                lock (this.Net)
                {
                    action = this.Trainer.Act(state);
                    path.Add(action);
                }

                environment.Action = action.Action;
            }

            environment.Step();
            
            if (environment.Reward > this.reward)
                this.reward = environment.Reward;

            this.Done = environment.Done ||
                    environment.SimTime > 10;
            if (this.Done && environment.Done)
                this.GoalReached++;
            else if (this.Done)
                this.TrainingTimedout++;

            if (this.Done)
            {
                this.Rewards.Push(reward);
                this.path.SetReward(reward);
                this.paths.Add(this.path);
                this.path = new Path();
                this.reward = double.MinValue;
            }

            if (paths.Count >= 100)
            {
                lock (this.Net)
                {
                    this.Trainer.Reinforce(paths.ToArray());
                    this.Epochs++;
                }
                paths.Clear();
            }
        }
    }
}