using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using NeuralMotion.Intelligence;
using Util;

namespace NeuralMotion.Simulator
{
    public partial class BoxArena
    {
        public Ball[] EngineBalls { get; private set; }
        public float TimeStep { get; private set; }
        public float BallRadius { get; private set; }
        public float CurrentSimulationTime { get; private set; }
        public int TotalCollisions { get; private set; }

        public bool RealTime { set; get; }
        public float SimulationDuration { get; set; }

        public float MaximumBallSpeed
        {
            get { return this.BallRadius / this.TimeStep; }
        }

        private readonly IController controller;
        private readonly CollisionDetector collisionDetector;
        private Task task;

        public BoxArena(
            IController controller,
            int noBalls = 5,
            float ballRadius = 0.06f)
        {
            noBalls = Math.Max(2, noBalls);

            this.TimeStep = 0.02f;
            this.SimulationDuration = 10;
            this.RealTime = true;
            this.BallRadius = ballRadius;

            this.controller = controller;
            this.collisionDetector = new CollisionDetector
            {
                BallRadius = this.BallRadius
            };

            //stvori lopte i dodatne parametre koje engine koristi
            this.EngineBalls = new Ball[noBalls];
            for (var index = 0; index < EngineBalls.Length; index++)
                this.EngineBalls[index] = new Ball(index);
        }

        public float RandomPosition(RandomEx generator)
        {
            return generator.NextF(-1.0f + this.BallRadius, 1.0f - this.BallRadius);
        }

        public Task RunAsync(Action<int, Ball> ballConfiguration)
        {
            if (this.task != null)
                throw new Exception("Allready started!");

            this.TotalCollisions = 0;

            this.prevAccels = new PointF[this.EngineBalls.Length];
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

            this.task = Task.Run((Action)Loop);

            return this.task;
        }

        private float lastTime = -1;
        private PointF[] prevAccels;

        private void Loop()
        {
            try
            {
                while (this.CurrentSimulationTime < this.SimulationDuration)
                {
                    var start = DateTime.UtcNow;

                    PhysicsLoop();

                    DecisionLoop();

                    this.CurrentSimulationTime += this.TimeStep;

                    if (this.RealTime)
                    {
                        var duration = DateTime.UtcNow.Subtract(start).TotalSeconds;
                        duration = this.TimeStep - duration;
                        if (duration > 0)
                            Thread.Sleep((int)(duration * 1000));
                    }
                }

                this.CurrentSimulationTime = this.SimulationDuration;
                this.task = null;
            }
            catch (Exception ex)
            {
                this.CurrentSimulationTime = SimulationDuration;
                Console.WriteLine(ex.Message);
                throw;
            }
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

                ball.Position = ball.Position.Offset(ball.Speed.Scale(this.TimeStep));
                ball.DistanceTravelled += speedLength * this.TimeStep;
            }
        }
    }
}
