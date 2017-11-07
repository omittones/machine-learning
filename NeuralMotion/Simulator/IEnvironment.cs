namespace NeuralMotion.Simulator
{
    public interface IEnvironment
    {
        float SimTime { get; }

        bool Done { get; }

        void Reset();

        void Step();
    }
}
