using System;
using ConvNetSharp.Volume.Double;
using ConvNetSharp.Core;
using ConvNetSharp.Volume;
using System.Collections.Generic;
using ConvNetSharp.Core.Training;
using System.Linq;

namespace NeuralMotion.Test
{
    //Experience a st,at,rt,st + 1st,at,rt,st + 1 transition in the environment.
    //Forward st + 1 to evaluate the(fixed) target y = maxafθ(st + 1). This quantity is interpreted to be maxaQ(st+1).
    //Forward fθ(st)fθ(st) and apply a simple L2 regression loss on the dimension atat of the output, to be yy. This gradient will have a very simple form where the predicted value is simply subtracted from yy.The other output dimensions have zero gradient.
    //Backpropagate the gradient and perform a parameter update.
    public class BatchedDQNTrainer
    {
        TrainerBase<double> trainer;
        Net<double> net;
        private List<Experience> experience;

        public double Gamma { get; private set; }
        public double L2 { get; private set; }
        public double Loss { get; private set; }

        public BatchedDQNTrainer(
            Net<double> net,
            TrainerBase<double> trainer)
        {
            this.L2 = 0;
            this.Gamma = 0;
            this.experience = new List<Experience>();
            this.net = net;
            this.trainer = trainer;
        }

        public int[] Forwards(Volume<double> actorStates, bool learn = false)
        {
            var actorCount = actorStates.Shape.GetDimension(3);
            var actions = new int[actorCount];
            var actionRewards = net.Forward(actorStates);
            if (actorStates.Shape.GetDimension(0) != 1 ||
                actorStates.Shape.GetDimension(1) != 1 ||
                actorStates.Shape.GetDimension(3) != actorCount)
                throw new NotSupportedException();

            var actionCount = actionRewards.Shape.GetDimension(2);
            for (var n = 0; n < actorCount; n++)
                for (var c = 1; c < actionCount; c++)
                    if (actionRewards.Get(0, 0, c, n) > actionRewards.Get(0, 0, actions[n], n))
                        actions[n] = c;

            if (learn)
            {
                if (actorCount != trainer.BatchSize)
                    throw new NotSupportedException();

                if (experience.Count != 0)
                    experience[experience.Count - 1].NextState = actorStates.Clone();

                experience.Add(new Experience
                {
                    PrevState = actorStates.Clone(),
                    Action = actions,
                    Rewards = actionRewards.Clone(),
                    NextState = null,
                    Done = false
                });
            }

            return actions;
        }

        public void Backwards(double[][] rewards, Volume<double> nextActorState)
        {
            if (experience.Count != 0)
                experience[experience.Count - 1].NextState = nextActorState.Clone();
            else
                throw new NotSupportedException("At least one Actions call must be made!");

            for (var x = 0; x < rewards.Length; x++)
            {
                var xlast = experience.Count - 1 - x;
                if (experience[xlast].Done)
                    throw new NotSupportedException("Too much rewards!");

                Volume<double> nextReward = null;
                if (xlast + 1 < experience.Count)
                    nextReward = experience[xlast + 1].Rewards;
                else
                    nextReward = net.Forward(nextActorState);
                nextReward = nextReward.Max();

                var oldReward = experience[xlast].Rewards;
                oldReward = oldReward.Max();

                for (var n = 0; n < nextActorState.Shape.GetDimension(3); n++)
                {
                    var oldAction = experience[xlast].Action[n];
                    var newReward = rewards[rewards.Length - x - 1][n];

                    var adjustedReward = (L2 * oldReward.Get(0, 0, 0, n)) + ((1.0 - L2) * (newReward + Gamma * nextReward.Get(0, 0, 0, n)));
                    experience[xlast].Rewards.Set(0, 0, oldAction, n, adjustedReward);
                }

                experience[xlast].Done = true;
            }
            if (experience.Any(xp => !xp.Done))
                throw new NotSupportedException("Too little rewards");

            foreach (var exp in experience)
            {
                trainer.Train(exp.PrevState, exp.Rewards);
            }

            this.Loss = trainer.Loss;

            experience.Clear();
        }
    }
}
