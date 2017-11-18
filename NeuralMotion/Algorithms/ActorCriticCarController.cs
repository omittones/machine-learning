using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using NeuralMotion.Intelligence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuralMotion
{
    public class ActorCriticCarController : IController<MountainCar>
    {
        public ActorCriticTrainer PolicyTrainer { get; }
        public SgdTrainer<double> ValueTrainer { get; private set; }
        public MovingStatistics Rewards { get; }
        public Net<double> Policy { get; }
        public Net<double> Value { get; }

        private readonly double[] state;
        private readonly List<Path> paths;
        
        private Path path;
        private double reward;

        public bool Done { get; private set; }
        public int GoalReached { get; private set; }
        public int TrainingTimedout { get; private set; }
        public int Samples { get; private set; }
        public int Epochs { get; private set; }

        public ActorCriticCarController()
        {
            this.Policy = new Net<double>();
            this.Policy.AddLayer(new InputLayer(1, 1, 2));
            this.Policy.AddLayer(new FullyConnLayer(10));
            this.Policy.AddLayer(new LeakyReluLayer());
            this.Policy.AddLayer(new FullyConnLayer(3));
            this.Policy.AddLayer(new SoftmaxLayer());

            this.Value = new Net<double>();
            this.Value.AddLayer(new InputLayer(1, 1, 2));
            this.Value.AddLayer(new FullyConnLayer(10));
            this.Value.AddLayer(new LeakyReluLayer());
            this.Value.AddLayer(new FullyConnLayer(1));
            this.Value.AddLayer(new RegressionLayer());

            this.Rewards = new MovingStatistics(1000);

            this.state = new double[2];
            this.paths = new List<Path>();

            this.ValueTrainer = new SgdTrainer<double>(Value)
            {
                LearningRate = 0.1,
                L1Decay = 0.0
            };

            this.PolicyTrainer = new ActorCriticTrainer(Policy, ValueTrainer)
            {
                LearningRate = 0.001,
                L1Decay = 0.0
            };

            this.GoalReached = 0;
            this.Samples = 0;
            this.Epochs = 0;
        }

        public void Control(MountainCar environment)
        {
            if (Epochs < 100000)
            {
                PolicyTrainer.Bootstraping = true;
                ValueTrainer.LearningRate = 0.1;
            }
            else
            {
                PolicyTrainer.Bootstraping = false;
                ValueTrainer.LearningRate = 0.01;
            }

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
                lock (this.Policy)
                {
                    action = this.PolicyTrainer.Act(state);
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

            if (paths.Count >= 10)
            {
                lock (this.Value)
                    lock (this.Policy)
                    {
                        this.PolicyTrainer.Reinforce(paths.ToArray());
                        this.Epochs++;
                    }
                paths.Clear();
            }
        }
    }
}