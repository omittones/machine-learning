using System.Collections.Generic;
using System.Drawing;

namespace NeuralMotion.Simulator
{
    public class Ball
    {
        public long Id { get; }

        public Ball(long id)
        {
            this.Id = id;
        }

        public int KicksToBorder = 0;
        public int KicksToBall = 0;
        public int KicksFromBall = 0;

        public PointF StartingPosition = new PointF(0, 0);

        public float[] Distances = null;

        public PointF Position = new PointF(0, 0);
        public PointF Speed = new PointF(0, 0);
        public PointF Acceleration = new PointF(0, 0);
        
        public float LastCollisionTime { get; private set; }
        public float DistanceTravelled;
        public float Energy;

        private readonly HashSet<long> collisions = new HashSet<long>();
        
        public int CollisionCount => collisions.Count;

        public IIndicator[] Indicators { get; set; }

        public void UnsetCollision(Ball collidedBall)
        {
            collisions.Remove(collidedBall.Id);
        }

        public void SetCollision(Ball collidedBall, float currentTime)
        {
            if (collidedBall.Id == this.Id)
                return;

            if (!collisions.Contains(collidedBall.Id))
                collisions.Add(collidedBall.Id);

            this.LastCollisionTime = currentTime;
        }

        public void Reset()
        {
            this.KicksFromBall = 0;
            this.KicksToBall = 0;
            this.KicksToBorder = 0;
            this.DistanceTravelled = 0;
            this.LastCollisionTime = -1;
            this.Energy = 0;
            
            this.collisions.Clear();
        }
    }
}