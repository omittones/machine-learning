using System;
using System.Drawing;
using System.Linq;
using Util;

namespace Environments.Bouncies
{
    public class CollisionDetector
    {
        public float ElasticFactor = 0.99f;
        public float BallRadius = 1;

        private bool[] ballIsProcessed;
        private PointF[] lastCollDetections;
        private float[] distanceTillDetection;
        private static readonly PointF zero = new PointF(0, 0);

        public int Apply(Ball[] balls, float currentTime)
        {
            var minDistanceBetweenBalls = this.BallRadius * 2;

            //ako obradimo jednu loptu, obradili smo je za sve ostale
            //pa kad obraðujemo ostale lopte možemo nju preskoèiti
            if (ballIsProcessed == null || ballIsProcessed.Length != balls.Length)
            {
                this.ballIsProcessed = new bool[balls.Length];
                this.lastCollDetections = new PointF[balls.Length];
                this.lastCollDetections.SetAll(new PointF(float.MaxValue, float.MaxValue));
                this.distanceTillDetection = new float[balls.Length];
                this.distanceTillDetection.SetAll(0);
            }

            //ovo je granica ploče, za odbijanje loptica
            var limit = 0.99f - this.BallRadius;

            var totalCount = 0;

            ballIsProcessed.SetAll(false);

            //collision detection, svako nekoliko pixela po lopti
            //kad se dogodi promijeni smijer
            for (var xFirst = 0; xFirst < balls.Length; xFirst++)
            {
                var first = balls[xFirst];
                var distanceTravelled = first.Position.Distance(lastCollDetections[xFirst]);

                //svako n pixela provjeravamo koliziju lopte sa svim ostalim
                if (distanceTravelled < this.distanceTillDetection[xFirst])
                    continue;

                ballIsProcessed[xFirst] = true;
                lastCollDetections[xFirst] = first.Position;

                //touch wall, and you collide
                if (!first.Position.X.IsInside(-limit, limit))
                {
                    first.KicksToBorder++;
                    first.Speed.X = -first.Speed.X;
                    first.Speed = first.Speed.Scale(ElasticFactor);
                    first.Acceleration = zero;

                    first.Position.X = Math.Max(first.Position.X, -limit);
                    first.Position.X = Math.Min(first.Position.X, limit);
                }
                if (!first.Position.Y.IsInside(-limit, limit))
                {
                    first.KicksToBorder++;
                    first.Speed.Y = -first.Speed.Y;
                    first.Speed = first.Speed.Scale(ElasticFactor);
                    first.Acceleration = zero;

                    first.Position.Y = Math.Max(first.Position.Y, -limit);
                    first.Position.Y = Math.Min(first.Position.Y, limit);
                }

                for (var xSecond = 0; xSecond < balls.Length; xSecond++)
                {
                    if (xSecond == xFirst)
                    {
                        first.Distances[xSecond] = float.MaxValue;
                        continue;
                    }

                    //ako je obrađena druga znači da je ne trebamo ponovo provjeravat
                    if (ballIsProcessed[xSecond])
                        continue;

                    var second = balls[xSecond];

                    //vektor od prve do druge kugle, i udaljenost kuglica
                    var bounceVector = second.Position.Offset(first.Position.Negative());
                    var ballDistance = bounceVector.Length();

                    //adjust position in case balls intersect
                    if (ballDistance <= minDistanceBetweenBalls)
                    {
                        var diff = (minDistanceBetweenBalls - ballDistance) / minDistanceBetweenBalls;
                        first.Position = first.Position.Offset(bounceVector.Scale(-diff));
                        second.Position = second.Position.Offset(bounceVector.Scale(diff));

                        //ovjde ne lockamo jer se kolizije koriste samo u ovom threadu
                        //zapamti sve kolizije, a reagiraj samo na prvu                        
                        var bounceSpeed = (first.Speed.Length() + second.Speed.Length()) / 2.0f;
                        bounceVector = bounceVector.Scale(1 / ballDistance * bounceSpeed, 1 / ballDistance * bounceSpeed);
                        bounceVector = bounceVector.Scale(ElasticFactor);

                        //samo ako je nekome od njih prva kolizija, uracunaj je u total count
                        if (first.CollisionCount == 0 || second.CollisionCount == 0)
                            totalCount++;

                        var firstSpeed = first.Speed.Length();
                        var secondSpeed = second.Speed.Length();

                        //samo ako je prva kolizija promijeni smijer
                        if (first.CollisionCount == 0)
                        {
                            first.Speed = bounceVector.Negative();
                            first.Acceleration = zero;
                            if (firstSpeed > secondSpeed)
                                first.KicksToBall++;
                            else
                                first.KicksFromBall++;
                        }
                        first.SetCollision(second, currentTime);

                        //samo ako je prva kolizija promijeni smijer
                        if (second.CollisionCount == 0)
                        {
                            second.Speed = bounceVector;
                            second.Acceleration = zero;
                            if (firstSpeed > secondSpeed)
                                second.KicksFromBall++;
                            else
                                second.KicksToBall++;
                        }
                        second.SetCollision(first, currentTime);
                    }
                    else
                    {
                        first.UnsetCollision(second);
                        second.UnsetCollision(first);
                    }

                    //recalculate ball distance
                    ballDistance = first.Position.Distance(second.Position) - minDistanceBetweenBalls;
                    first.Distances[xSecond] = ballDistance;
                    second.Distances[xFirst] = ballDistance;
                }

                //set minimum distance untill detection for this ball
                var closestBallDistance = first.Distances.Min();
                var closestWallDistance = (new[]
                {
                    first.Position.X + limit,
                    limit - first.Position.X,
                    first.Position.Y + limit,
                    limit - first.Position.Y
                }).Min();

                this.distanceTillDetection[xFirst] = Math.Min(closestWallDistance, closestBallDistance) / 2.0f;
            }

            return totalCount;
        }
    }
}