using System;
using System.Drawing;
using Util;

namespace Environments.Bouncies
{
    public partial class Environment : IEnvironment
    {
        private static Random rnd = new Random();

        public float TimeStep;
        public float FrictionFactor;
        public float BallRadius;
        public Ball[] Objects { get; private set; }
        public float ElapsedTime { get; private set; }
        public int TotalCollisions { get; private set; }

        public float MaximumBallSpeed
        {
            get { return this.BallRadius / this.TimeStep; }
        }

        private readonly CollisionDetector collisions;
        
        public Environment(
            int noBalls = 5,
            float ballRadius = 0.06f,
            float frictionFactor = 0.999f)
        {
            noBalls = Math.Max(1, noBalls);

            this.TimeStep = 0.02f;
            this.BallRadius = ballRadius;
            this.FrictionFactor = frictionFactor;
            this.collisions = new CollisionDetector
            {
                BallRadius = this.BallRadius,
                ElasticFactor = this.FrictionFactor
            };

            //stvori lopte i dodatne parametre koje engine koristi
            this.Objects = new Ball[noBalls];
            for (var index = 0; index < Objects.Length; index++)
                this.Objects[index] = new Ball(index);
            this.prevAccels = new PointF[this.Objects.Length];
        }

        private void InitBall(int index, Ball ball)
        {
            ball.Speed = new PointF
            {
                X = (float)rnd.NextDouble() * 2 - 1.0f,
                Y = (float)rnd.NextDouble() * 2 - 1.0f
            };
            ball.Speed = ball.Speed.Scale(0.5f);
            ball.Position = new PointF
            {
                X = (float)rnd.NextDouble() * 2 - 1.0f,
                Y = (float)rnd.NextDouble() * 2 - 1.0f
            };
        }

        public void Reset()
        {
            this.TotalCollisions = 0;
            for (var index = 0; index < Objects.Length; index++)
            {
                prevAccels[index] = PointF.Empty;

                Objects[index].Reset();
                Objects[index].Distances = new float[Objects.Length];
                Objects[index].Speed = new PointF(0, 0);
                Objects[index].Acceleration = new PointF(0, 0);

                InitBall(index, Objects[index]);

                Objects[index].StartingPosition = Objects[index].Position;
            }
            this.ElapsedTime = 0;
        }

        private PointF[] prevAccels;

        public State Step(float[] actions)
        {
            ApplyActions(actions);

            MoveBalls();

            this.ElapsedTime += this.TimeStep;

            return new State
            {
                Done = false,
                Observation = CollectObservation(),
                Info = null,
                Reward = 0
            };
        }

        protected virtual float[] CollectObservation()
        {
            return null;
            //var output = new BallSenses[this.EngineBalls.Length];
            //for (var i = 0; i < this.EngineBalls.Length; i++)
            //{
            //    output[i] = new BallSenses();
            //}
            //return output;
        }

        protected virtual void ApplyActions(float[] actions)
        {
            //Debug.Assert(actions.Length == this.EngineBalls.Length);
            //for (var i = 0; i < this.EngineBalls.Length; i++)
            //{
            //    var ball = this.EngineBalls[i];
            //    var action = actions[i];
            //    switch (action)
            //    {
            //        case Action.None:
            //            ball.Acceleration.X = 0;
            //            ball.Acceleration.Y = 0;
            //            break;
            //        case Action.Down:
            //            ball.Acceleration.X = 0;
            //            ball.Acceleration.Y = 0.5f;
            //            break;
            //        case Action.Up:
            //            ball.Acceleration.X = 0;
            //            ball.Acceleration.Y = -0.5f;
            //            break;
            //        case Action.Right:
            //            ball.Acceleration.X = 0.5f;
            //            ball.Acceleration.Y = 0;
            //            break;
            //        case Action.Left:
            //            ball.Acceleration.X = -0.5f;
            //            ball.Acceleration.Y = 0;
            //            break;
            //    }
            //}
        }

        private void MoveBalls()
        {
            this.collisions.BallRadius = this.BallRadius;
            this.collisions.ElasticFactor = this.FrictionFactor;

            var detections = this.collisions.Apply(this.Objects, this.ElapsedTime);

            this.TotalCollisions += detections;

            //pokreni svaku loptu
            //zapamti jesu li u stanju kolizije
            for (var i = 0; i < this.Objects.Length; i++)
            {
                var ball = this.Objects[i];
                ball.Energy += ball.Acceleration.Length() * this.TimeStep;
                if (ball.Acceleration != prevAccels[i])
                    ball.Energy += prevAccels[i].Length() * this.TimeStep;
                prevAccels[i] = ball.Acceleration;

                //ograničenje brzine, simuliram light speed
                var speed = ball.Speed.Offset(ball.Acceleration.Scale(this.TimeStep));
                var speedLength = speed.Length();
                if (speedLength < this.MaximumBallSpeed)
                    ball.Speed = speed;

                //friction
                ball.Speed = ball.Speed.Scale(this.FrictionFactor);

                ball.Position = ball.Position.Offset(ball.Speed.Scale(this.TimeStep));
                ball.DistanceTravelled += speedLength * this.TimeStep;
            }
        }
    }
}