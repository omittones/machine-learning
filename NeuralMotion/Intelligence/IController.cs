using NeuralMotion.Simulator;

namespace NeuralMotion.Intelligence
{
    public interface IController<TEnvironment>
    {
        bool Done { get; }

        void Control(TEnvironment environment);
    }
}