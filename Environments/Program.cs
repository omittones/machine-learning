using System;
using System.Threading;
using System.Threading.Tasks;

namespace Environments
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool stop = false;
            var env = new Bouncies.Environment(frictionFactor: 1);

            var gui = GUI.ShowForm(() =>
            {
                var form = new Forms.EnvironmentDisplay();
                var renderer = new Bouncies.Renderer(env);
                form.Renderer = renderer;
                return form;
            });

            var engine = Task.Run(() =>
            {
                DateTime realStart = DateTime.UtcNow;
                float simStart = 0;

                env.Reset();

                while (!stop)
                {
                    env.Step(null);

                    if (env.ElapsedTime > 1000)
                    {
                        var pauseMs = (env.ElapsedTime - simStart) - (DateTime.UtcNow - realStart).TotalSeconds;
                        pauseMs *= 1000;
                        if (pauseMs > 0)
                            Thread.Sleep((int)pauseMs);
                    }
                    else
                    {
                        realStart = DateTime.UtcNow;
                        simStart = env.ElapsedTime;
                    }
                }
            });

            Task.WaitAll(gui);
            stop = true;
            Task.WaitAll(engine);

            if (gui.Exception != null ||
                engine.Exception != null)
            {
                Console.WriteLine(gui.Exception ?? engine.Exception);
            }
        }
    }
}
