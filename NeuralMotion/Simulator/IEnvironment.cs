namespace NeuralMotion.Simulator
{
    public interface IEnvironment
    {
        float SimTime { get; }

        void Reset();

        void Step();
    }
}
