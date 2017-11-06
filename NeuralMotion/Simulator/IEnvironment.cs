namespace NeuralMotion.Simulator
{
    public interface IEnvironment
    {
        float CurrentSimulationTime { get; }

        void Reset();

        void Step();
    }
}
