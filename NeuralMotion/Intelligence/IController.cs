using NeuralMotion.Simulator;

namespace NeuralMotion.Intelligence
{
    public interface IController
    {
        int InputLength { get; }
        int OutputLength { get; }

        float[] SelectInput(Ball[] allBalls, Ball ball);
        void HandleOutput(Ball ball, float[] output);
    }
}