namespace Environments
{
    public struct State
    {
        public float[] Observation;
        public float Reward;
        public bool Done;
        public object Info;
    }

    public interface IEnvironment
    {
        float SimTime { get; }

        State Step(float[] action);

        void Reset();
    }
}
