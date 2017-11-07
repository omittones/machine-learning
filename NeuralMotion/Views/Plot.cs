using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuralMotion.Views
{
    public static class Plot
    {
        public static Task Show(Func<PlotWindow> factory)
        {
            var task = Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(factory());
                Console.WriteLine("Display thread stopped!");
            });

            while (task.Status == TaskStatus.WaitingForActivation ||
                task.Status == TaskStatus.WaitingToRun ||
                task.Status == TaskStatus.Created)
                Thread.Sleep(0);

            return task;
        }
    }
}
