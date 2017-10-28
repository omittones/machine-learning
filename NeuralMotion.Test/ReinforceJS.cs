using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Volume;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuralMotion.Test
{
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
        private Net<double> net;
        private List<Experiences> exp;
        private List<double> loss;
        private int expi;
        private int sample;
        private double? r0;
        private Volume<double> s0;
        private Volume<double> s1;
        private int a0;
        private int a1;
        
        private int nmStates;
        private InputLayer inputLayer;
        private int nmActions;
        private Random rnd;
        
        public double gamma { get; set; }
        public double epsilon { get; set; }
        public double alpha { get; set; }
        public int experience_add_every { get; set; }
        public int experience_size { get; set; }
        public int learning_steps_per_iteration { get; set; }
        public double clamp_error_to { get; set; }

        public double Loss => loss.Average();

        public DQNAgent(
            Net<double> net,
            int nmActions)
        {
            this.rnd = new Random(DateTime.Now.Millisecond);
            this.loss = new List<double>();

            this.gamma = 0.75; // future reward discount factor
            this.epsilon = 0.1; // for epsilon-greedy policy
            this.alpha = 0.01; // value function learning rate
            this.experience_add_every = 25; // number of time steps before we add another experience to replay memory
            this.experience_size = 5000; // size of experience replay
            this.learning_steps_per_iteration = 10;
            this.clamp_error_to = 1.0;

            var inputLayer = net.Layers[0] as InputLayer;
            if (inputLayer == null)
                throw new NotSupportedException();
            this.inputLayer = inputLayer;

            this.nmStates =
                inputLayer.InputDepth *
                inputLayer.InputHeight *
                inputLayer.InputWidth;

            this.nmActions = nmActions;

            this.net = net;

            this.reset();
        }

        private void reset()
        {
            // nets are hardcoded for now as key (str) -> Mat
            // not proud of this. better solution is to have a whole Net object
            // on top of Mats, but for now sticking with this

            // experience
            this.exp = new List<Experiences>();

            // where to insert
            this.expi = 0;

            this.sample = 0;

            this.r0 = null;
            this.s0 = null;
            this.s1 = null;
            this.a0 = 0;
            this.a1 = 0;

            // for visualization only...
            this.loss.Clear();
            this.loss.Add(0);
        }

        public int act(double[] state)
        {
            // convert to a Mat column vector
            var s = BuilderInstance<double>.Volume.SameAs(state, Shape.From(1, 1, this.nmStates, 1));
            int a;

            // epsilon greedy policy
            if (rnd.NextDouble() < this.epsilon)
            {
                a = rnd.Next(0, this.nmActions);
            }
            else
            {
                // greedy wrt Q function
                var output = this.net.Forward(s, false);
                a = output.IndexOfMax();
            }

            // shift state memory
            this.s0 = this.s1;
            this.a0 = this.a1;
            this.s1 = s;
            this.a1 = a;

            return a;
        }

        public void learn(double r)
        {
            // perform an update on Q function
            if (this.r0.HasValue && this.alpha > 0)
            {
                // learn from this tuple to get a sense of how "surprising" it is to the agent
                this.learnFromTuple(this.s0, this.a0, this.r0.Value, this.s1, this.a1);

                // decide if we should keep this experience in the replay
                if (this.sample % this.experience_add_every == 0)
                {
                    if (this.expi == this.exp.Count)
                        this.exp.Add(null);
                    this.exp[this.expi] = Experiences.New(this.s0, this.a0, this.r0.Value, this.s1, this.a1);
                    this.expi += 1;

                    // roll over when we run out
                    if (this.expi > this.experience_size)
                        this.expi = 0;
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

            // store for next update
            this.r0 = r;
        }

        private void learnFromTuple(Volume<double> s0, int a0, double r0, Volume<double> s1, int a1)
        {
            // want: Q(s,a) = r + gamma * max_a' Q(s',a')

            // compute the target Q value (current reward + gamma * next reward)
            var qmax = r0;
            if (gamma != 0)
            {
                var a1vol = this.net.Forward(s1, false);
                var a1val = a1vol.IndexOfMax();
                qmax += this.gamma * a1vol.Get(0, 0, a1val, 0);
            }

            // now predict
            var expected = this.net.Forward(s0, true).Clone();
            var a0val = expected.Get(0, 0, a0, 0);
            var tderror = a0val - qmax;
            var clamp = this.clamp_error_to;

            // huber loss to robustify
            var clampedError = tderror;
            if (clampedError > clamp)
                clampedError = clamp;
            if (clampedError < -clamp)
                clampedError = -clamp;

            //pred.dw[a0] = tderror;
            expected.Set(0, 0, a0, 0, a0val - clampedError);

            //propagate errors
            this.net.Backward(expected); // compute gradients on net params

            // update net
            this.updateNet(this.net, this.alpha);

            loss.Add(Math.Abs(tderror));
            if (loss.Count > 100)
                loss.RemoveAt(0);
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
