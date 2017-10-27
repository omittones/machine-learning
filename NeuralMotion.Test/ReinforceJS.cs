using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume;
using System;
using System.Collections.Generic;

namespace NeuralMotion.Test
{
    public interface IEnv
    {
        int getNumStates();
        int getMaxNumActions();
    }

    public class Experiences
    {
        public double r0;
        public Volume<double> s0;
        public Volume<double> s1;
        public int a0;
        public int a1;

        internal static Experiences New(Volume<double> s0, int a0, double r0, Volume<double> s1, int a1)
        {
            return new Experiences
            {
                s0 = s0.Clone(),
                s1 = s1.Clone(),
                a0 = a0,
                a1 = a1,
                r0 = r0
            };
        }
    }

    public static class VolumeExtension
    {
        public static int IndexOfMax(this Volume<double> output)
        {
            int a = 0;
            for (var i = 1; i < output.Shape.GetDimension(2); i++)
                if (output.Get(0, 0, i, 0) > output.Get(0, 0, a, 0))
                    a = i;
            return a;
        }
    }

    public class DQNAgent
    {
        private readonly IEnv env;
        private Net<double> net;
        private List<Experiences> exp;
        private int expi;
        private int sample;
        private double? r0;
        private Volume<double> s0;
        private Volume<double> s1;
        private int a0;
        private int a1;
        private double tderror;
        private int ns;
        private int na;
        private Random rnd;

        public double gamma { get; set; }
        public double epsilon { get; set; }
        public double alpha { get; set; }
        public int experience_add_every { get; set; }
        public int experience_size { get; set; }
        public int learning_steps_per_iteration { get; set; }
        public double tderror_clamp { get; set; }

        public DQNAgent(IEnv env)
        {
            this.rnd = new Random(DateTime.Now.Millisecond);

            this.gamma = 0.75; // future reward discount factor
            this.epsilon = 0.1; // for epsilon-greedy policy
            this.alpha = 0.01; // value function learning rate
            this.experience_add_every = 25; // number of time steps before we add another experience to replay memory
            this.experience_size = 5000; // size of experience replay
            this.learning_steps_per_iteration = 10;
            this.tderror_clamp = 1.0;

            this.env = env;
        }

        private void reset()
        {
            this.ns = this.env.getNumStates();
            this.na = this.env.getMaxNumActions();

            // nets are hardcoded for now as key (str) -> Mat
            // not proud of this. better solution is to have a whole Net object
            // on top of Mats, but for now sticking with this
            this.net = new Net<double>();
            net.AddLayer(new InputLayer(1, 1, this.env.getNumStates()));
            net.AddLayer(new FullyConnLayer(20));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(10));
            net.AddLayer(new LeakyReluLayer());
            net.AddLayer(new FullyConnLayer(this.env.getMaxNumActions()));
            net.AddLayer(new RegressionLayer());

            this.exp = new List<Experiences>(); // experience
            this.expi = 0; // where to insert

            this.sample = 0;

            this.r0 = null;
            this.s0 = null;
            this.s1 = null;
            this.a0 = 0;
            this.a1 = 0;

            this.tderror = 0; // for visualization only...
        }

        private Volume<double> forwardQ(Volume<double> state, bool needsBackprop)
        {
            var output = this.net.Forward(state, needsBackprop);
            return output;
        }

        public int act(double[] state)
        {
            // convert to a Mat column vector
            var s = BuilderInstance<double>.Volume.SameAs(state, Shape.From(1, 1, this.ns, 1));

            int a;

            // epsilon greedy policy
            if (rnd.NextDouble() < this.epsilon)
            {
                a = rnd.Next(0, this.na);
            }
            else
            {
                // greedy wrt Q function
                var output = this.forwardQ(s, false);
                a = output.IndexOfMax();
            }

            // shift state memory
            this.s0 = this.s1;
            this.a0 = this.a1;
            this.s1 = s;
            this.a1 = a;

            return a;
        }

        public void learn(double r1)
        {
            // perform an update on Q function
            if (this.r0.HasValue && this.alpha > 0)
            {
                // learn from this tuple to get a sense of how "surprising" it is to the agent
                var tderror = this.learnFromTuple(this.s0, this.a0, this.r0.Value, this.s1, this.a1);
                this.tderror = tderror; // a measure of surprise

                // decide if we should keep this experience in the replay
                if (this.sample % this.experience_add_every == 0)
                {
                    if (this.expi == this.exp.Count)
                        this.exp.Add(null);
                    this.exp[this.expi] = Experiences.New(this.s0, this.a0, this.r0.Value, this.s1, this.a1);
                    this.expi += 1;
                    if (this.expi > this.experience_size) { this.expi = 0; } // roll over when we run out
                }
                this.sample += 1;

                // sample some additional experience from replay memory and learn from it
                for (var k = 0; k < this.learning_steps_per_iteration; k++)
                {
                    var ri = this.rnd.Next(0, this.exp.Count); // todo: priority sweeps?
                    var e = this.exp[ri];
                    this.learnFromTuple(e.s0, e.a0, e.r0, e.s1, e.a1);
                }
            }
            this.r0 = r1; // store for next update
        }

        public double learnFromTuple(Volume<double> s0, int a0, double r0, Volume<double> s1, int a1)
        {
            // want: Q(s,a) = r + gamma * max_a' Q(s',a')

            // compute the target Q value
            var tmat = this.forwardQ(s1, false);
            var a = tmat.IndexOfMax();
            var qmax = r0 + this.gamma * tmat.Get(0, 0, a, 0);

            // now predict
            var pred = this.forwardQ(s0, true);
            var old = pred.Get(0, 0, a0, 0);
            var tderror = old - qmax;
            var clamp = this.tderror_clamp;
            if (Math.Abs(tderror) > clamp)
            {
                // huber loss to robustify
                if (tderror > clamp) tderror = clamp;
                if (tderror < -clamp) tderror = -clamp;
            }

            //pred.dw[a0] = tderror;
            pred.Set(0, 0, a0, 0, old - tderror);

            //propagate errors
            this.net.Backward(pred); // compute gradients on net params

            // update net
            this.updateNet(this.net, this.alpha);

            return tderror;
        }

        private void updateNet(INet<double> net, double alpha)
        {
            foreach (var pandg in net.GetParametersAndGradients())
            {
                pandg.Gradient.DoMultiply(pandg.Gradient, -alpha);
                pandg.Volume.DoAdd(pandg.Gradient, pandg.Volume);
                pandg.Gradient.Clear();
            }
        }
    }
}
