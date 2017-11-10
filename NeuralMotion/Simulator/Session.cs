using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using NeuralMotion.Intelligence;

namespace NeuralMotion.Simulator
{
    public abstract class Session
    {
        public static Session Create<TEnvironment>(TEnvironment environment, IController<TEnvironment> controller)
            where TEnvironment : IEnvironment
        {
            return new Session<TEnvironment>(environment, controller);
        }

        public float? LimitSimulationDuration { get; set; }
        public bool RealTime { set; get; }
        public bool RestartOnEnd { set; get; }

        private readonly IEnvironment environment;
        private CancellationTokenSource token;
        private Task task;

        public Session(IEnvironment environment)
        {
            this.LimitSimulationDuration = 10;
            this.RealTime = true;
            this.RestartOnEnd = false;
            this.environment = environment;
        }

        public Task Reset()
        {
            if (this.task == null)
                throw new Exception("Not running!");
            this.token.Cancel();
            this.task.Wait();

            return this.Run();
        }

        public void Stop()
        {
            if (this.task == null)
                throw new Exception("Not running!");
            this.token.Cancel();
            this.task.Wait();
        }

        public Task Run()
        {
            if (this.task != null && (
                task.Status == TaskStatus.Running ||
                task.Status == TaskStatus.WaitingForActivation ||
                task.Status == TaskStatus.WaitingToRun))
                throw new Exception("Already started!");

            if (task != null)
            {
                task.Dispose();
                token.Dispose();
            }

            this.Initialize();

            this.token = new CancellationTokenSource();
            this.task = Task.Run(() => Loop(token.Token), token.Token);

            return this.task;
        }

        private void Initialize()
        {
            this.environment.Reset();

            if (this.environment.SimTime != 0)
                throw new NotSupportedException("Environment did not properly reset!");
        }

        protected abstract bool Step();

        private void Loop(CancellationToken token)
        {
            try
            {
                while (this.RestartOnEnd)
                {
                    while (!this.LimitSimulationDuration.HasValue ||
                            this.environment.SimTime < this.LimitSimulationDuration)
                    {
                        if (token.IsCancellationRequested)
                            return;

                        var localStart = DateTime.UtcNow;
                        var simStart = this.environment.SimTime;

                        var done = this.Step();

                        if (this.RealTime)
                        {
                            var localDuration = DateTime.UtcNow.Subtract(localStart).TotalSeconds;
                            var simDuration = this.environment.SimTime - simStart;
                            if (simDuration > localDuration)
                                Thread.Sleep((int)((simDuration - localDuration) * 1000));
                        }

                        if (done) break;
                    }

                    Initialize();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }

    public class Session<TEnvironment> : Session
        where TEnvironment : IEnvironment
    {
        private readonly TEnvironment environment;
        private readonly IController<TEnvironment> controller;

        public Session(
            TEnvironment environment,
            IController<TEnvironment> controller) : base(environment)
        {
            this.environment = environment;
            this.controller = controller;
        }

        protected override bool Step()
        {
            this.controller.Control(this.environment);

            return this.controller.Done;
        }
    }
}
