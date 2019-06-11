using System;
using System.Diagnostics;
using System.Drawing;
using Util;

namespace Environments.Bouncies
{
    public partial class Environment : IEnvironment
    {
        private static Random rnd = new Random();

        public float TimeStep { get; private set; }
        public Ball[] Objects { get; private set; }
        public float ElapsedTime { get; private set; }
        public int TotalCollisions { get; private set; }
        public float BallRadius { get => this.collisions.BallRadius; set => this.collisions.BallRadius = value; }
        public float FrictionFactor { get => this.collisions.ElasticFactor; set => this.collisions.ElasticFactor = value; }

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
            this.collisions = new CollisionDetector();

            noBalls = Math.Max(1, noBalls);

            this.TimeStep = 0.02f;
            this.BallRadius = ballRadius;
            this.FrictionFactor = frictionFactor;

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
            const int nmObservations = 6;

            var observation = new float[this.Objects.Length * nmObservations];
            for (var i = 0; i < this.Objects.Length; i++)
            {
                var ball = this.Objects[i];
                var x = i * nmObservations;
                observation[x++] = ball.Position.X;
                observation[x++] = ball.Position.Y;
                observation[x++] = ball.Speed.X;
                observation[x++] = ball.Speed.Y;
                observation[x++] = ball.Acceleration.X;
                observation[x++] = ball.Acceleration.Y;
            }

            return observation;
        }

        protected virtual void ApplyActions(float[] actions)
        {
            if (actions == null)
                return;

            Debug.Assert(actions.Length == this.Objects.Length * 5);
            for (var xBall = 0; xBall < this.Objects.Length; xBall++)
            {
                var selectedAction = 0;
                for (var xAction = 1; xAction < 5; xAction++)
                    if (actions[xBall * 5 + selectedAction] < actions[xBall * 5 + xAction])
                        selectedAction = xAction;

                var ball = this.Objects[xBall];
                switch (selectedAction)
                {
                    case 0:
                        ball.Acceleration.X = 0;
                        ball.Acceleration.Y = 0;
                        break;
                    case 1:
                        ball.Acceleration.X = 0;
                        ball.Acceleration.Y = 1f;
                        break;
                    case 2:
                        ball.Acceleration.X = 0;
                        ball.Acceleration.Y = -1f;
                        break;
                    case 3:
                        ball.Acceleration.X = 1f;
                        ball.Acceleration.Y = 0;
                        break;
                    case 4:
                        ball.Acceleration.X = -1f;
                        ball.Acceleration.Y = 0;
                        break;
                }
            }
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