using System.Linq;
using NeuralMotion.Intelligence;
using System.Diagnostics;
using Environments.Bouncies;
using Environments;
using Util;

namespace NeuralMotion
{
    public abstract class BallController : IController<Environment>
    {
        public bool Done => false;

        public int InputLength => 30;

        public double[] SelectInput(Ball[] arena, Ball actor)
        {
            var neighbours = arena
                .Where(b => b.Id != actor.Id)
                .OrderBy(b => b.Position.Distance(actor.Position))
                .Select(b => new
                {
                    pos = b.Position.Offset(actor.Position.Negative()),
                    speed = b.Speed
                })
                .Take(6)
                .ToArray();

            var selection = new double[]
            {
                1 - actor.Position.X,
                actor.Position.X + 1,
                1 - actor.Position.Y,
                actor.Position.Y + 1,
                actor.Speed.X,
                actor.Speed.Y
            };

            selection = selection
                .Concat(neighbours
                .SelectMany(n => new double[] {
                    n.pos.X,
                    n.pos.Y,
                    n.speed.X,
                    n.speed.Y
                })).ToArray();

            if (actor.Id == 0)
            {
                var indicators = new IIndicator[neighbours.Length];
                var pos = actor.Position;
                for (var i = 0; i < neighbours.Length; i++)
                {
                    var next = pos.Offset(neighbours[i].pos.X, neighbours[i].pos.Y);
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
                    ball.Acceleration.Y = 0.5f;
                    break;
                case 2:
                    ball.Acceleration.X = 0;
                    ball.Acceleration.Y = -0.5f;
                    break;
                case 3:
                    ball.Acceleration.X = 0.5f;
                    ball.Acceleration.Y = 0;
                    break;
                case 4:
                    ball.Acceleration.X = -0.5f;
                    ball.Acceleration.Y = 0;
                    break;
            }
        }

        public virtual double GetReward(Ball[] arena, Ball actor)
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

        private float lastTime = -1;
        public void Control(Environment environment)
        {
            var now = environment.SimTime;
            if (this.lastTime > now || now - lastTime >= 0.1)
            {
                lastTime = now;
                foreach (var ball in environment.EngineBalls)
                    ControlBall(environment.EngineBalls, ball);
            }

            var state = environment.Step(null);
        }

        public abstract void ControlBall(Ball[] arena, Ball actor);
    }
}