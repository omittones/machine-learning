using System.Diagnostics;
using System.Linq;
using NeuralMotion.Simulator;
using Util;

namespace NeuralMotion.Intelligence
{
    public class BallController : IController
    {
        public int InputLength => 12;
        public int OutputLength => 5;

        public float[] SelectInput(Ball[] population, Ball current)
        {
            var closestBalls = population
                .OrderBy(b => b.Position.Distance(current.Position))
                .Take(4)
                .ToArray();

            Debug.Assert(object.ReferenceEquals(closestBalls[0], current));

            var ballSpeed = current.Speed.FromCartesianToPolar();

            var polarClosest = closestBalls.Skip(1)
                .Select(b => b.Position.RelativeTo(current.Position).FromCartesianToPolar())
                .ToArray();

            var selection = new[]
            {
                1 - current.Position.X,
                current.Position.X + 1,
                1 - current.Position.Y,
                current.Position.Y + 1,
                ballSpeed.Angle,
                polarClosest[0].Angle,
                polarClosest[1].Angle,
                polarClosest[2].Angle,
                ballSpeed.Radius,
                polarClosest[0].Radius,
                polarClosest[1].Radius,
                polarClosest[2].Radius
            };

            Debug.Assert(selection.Length == this.InputLength);

            return selection;
        }

        public void HandleOutput(Ball ball, float[] output)
        {
            var x = 0;
            for (var i = 1; i < output.Length; i++)
                if (output[i] > output[x])
                    x = i;
            switch (x)
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
    }
}