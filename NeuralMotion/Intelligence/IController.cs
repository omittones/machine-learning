using NeuralMotion.Simulator;

namespace NeuralMotion.Intelligence
{
    public interface IController<TEnvironment>
    {
        void Control(TEnvironment environment);
    }
}