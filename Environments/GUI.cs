using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Environments
{
    public static class GUI
    {
        public static Task ShowForm(Func<Form> factory)
        {
            var task = Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(factory());
            });

            while (task.Status == TaskStatus.WaitingForActivation ||
                task.Status == TaskStatus.WaitingToRun ||
                task.Status == TaskStatus.Created)
                Thread.Sleep(0);

            return task;
        }
    }
}
