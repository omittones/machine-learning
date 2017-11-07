using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using NeuralMotion.Intelligence;

namespace NeuralMotion.Simulator
{
    public class Session
    {
        public float? LimitSimulationDuration { get; set; }
        public bool RealTime { set; get; }
        public bool RestartOnEnd { set; get; }

        private readonly IController controller;
        private readonly IEnvironment environment;
        private CancellationTokenSource token;
        private Task task;

        public Session(
            IController controller,
            IEnvironment environment)
        {
            this.LimitSimulationDuration = 10;
            this.RealTime = true;
            this.RestartOnEnd = false;
            this.controller = controller;
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

            if (this.environment.CurrentSimulationTime != 0)
                throw new NotSupportedException("Environment did not properly reset!");
        }

        private void Loop(CancellationToken token)
        {
            try
            {
                while (this.RestartOnEnd)
                {
                    while (!this.LimitSimulationDuration.HasValue ||
                            this.environment.CurrentSimulationTime < this.LimitSimulationDuration)
                    {
                        if (token.IsCancellationRequested)
                            return;

                        var localStart = DateTime.UtcNow;
                        var simStart = this.environment.CurrentSimulationTime;

                        this.environment.Step();

                        if (this.RealTime)
                        {
                            var localDuration = DateTime.UtcNow.Subtract(localStart).TotalSeconds;
                            var simDuration = this.environment.CurrentSimulationTime - simStart;
                            if (simDuration > localDuration)
                                Thread.Sleep((int)((simDuration - localDuration) * 1000));
                        }
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
}
