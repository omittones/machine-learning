using NeuralMotion.Simulator;

namespace NeuralMotion.Intelligence
{
    public interface IController
    {
        void Control(Ball[] arena, Ball actor);
    }
}