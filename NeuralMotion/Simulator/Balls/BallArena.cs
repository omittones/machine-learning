using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using NeuralMotion.Intelligence;
using Util;

namespace NeuralMotion.Simulator
{
    public partial class BallArena : IEnvironment
    {
        public Ball[] EngineBalls { get; private set; }
        public float TimeStep { get; private set; }
        public float BallRadius { get; private set; }
        public float CurrentSimulationTime { get; private set; }
        public int TotalCollisions { get; private set; }

        public float MaximumBallSpeed
        {
            get { return this.BallRadius / this.TimeStep; }
        }

        private readonly Action<int, Ball> ballConfiguration;
        private readonly CollisionDetector collisionDetector;
        private readonly IController controller;

        public BallArena(
            Action<int, Ball> ballConfiguration,
            IController controller,
            int noBalls = 5,
            float ballRadius = 0.06f)
        {
            noBalls = Math.Max(1, noBalls);

            this.TimeStep = 0.02f;
            this.BallRadius = ballRadius;
            this.ballConfiguration = ballConfiguration;
            this.controller = controller;
            this.collisionDetector = new CollisionDetector
            {
                BallRadius = this.BallRadius
            };

            //stvori lopte i dodatne parametre koje engine koristi
            this.EngineBalls = new Ball[noBalls];
            for (var index = 0; index < EngineBalls.Length; index++)
                this.EngineBalls[index] = new Ball(index);
            this.prevAccels = new PointF[this.EngineBalls.Length];
        }

        public float RandomPosition(RandomEx generator)
        {
            return generator.NextF(-1.0f + this.BallRadius, 1.0f - this.BallRadius);
        }

        public void Reset()
        {
            this.TotalCollisions = 0;
            for (var index = 0; index < EngineBalls.Length; index++)
            {
                prevAccels[index] = PointF.Empty;

                EngineBalls[index].Reset();

                //stvori udaljenosti od ostalih loptica
                //zapamti pocetni polozaj
                EngineBalls[index].Distances = new float[EngineBalls.Length];
                EngineBalls[index].Speed = new PointF(0, 0);
                EngineBalls[index].Acceleration = new PointF(0, 0);

                ballConfiguration(index, EngineBalls[index]);

                EngineBalls[index].StartingPosition = EngineBalls[index].Position;
            }
            this.CurrentSimulationTime = 0;
        }

        private float lastTime = -1;
        private PointF[] prevAccels;

        public void Step()
        {
            PhysicsLoop();

            DecisionLoop();

            this.CurrentSimulationTime += this.TimeStep;
        }

        private void DecisionLoop()
        {
            if (this.lastTime > this.CurrentSimulationTime ||
                this.CurrentSimulationTime - lastTime >= 0.1)
            {
                lastTime = this.CurrentSimulationTime;
                foreach (var ball in this.EngineBalls)
                    this.controller.Control(this.EngineBalls, ball);
            }
        }

        private void PhysicsLoop()
        {
            var detections = this.collisionDetector.Detect(this.EngineBalls, this.CurrentSimulationTime);
            this.TotalCollisions += detections;

            //pokreni svaku loptu
            //zapamti jesu li u stanju kolizije
            for (var i = 0; i < this.EngineBalls.Length; i++)
            {
                var ball = this.EngineBalls[i];
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
                ball.Speed = ball.Speed.Scale(0.999f);

                ball.Position = ball.Position.Offset(ball.Speed.Scale(this.TimeStep));
                ball.DistanceTravelled += speedLength * this.TimeStep;
            }
        }
    }
}