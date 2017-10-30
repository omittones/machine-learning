using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;

namespace NeuralMotion
{
    public partial class Main : Form
    {
        private static readonly Random rnd = new Random();
        private readonly Settings uiSettings;
        private bool closing;
        private Task simulation;

        public BoxArena BoxArena { get; private set; }
        public DQNController Controller { get; private set; }

        public Main()
        {
            this.Controller = new DQNController();
            this.BoxArena = new BoxArena(Controller, 10, 0.06f)
            {
                LimitSimulationDuration = null,
                RealTime = false
            };

            InitializeComponent();

            this.uiSettings = new Settings();
            this.uiArena.Arena = this.BoxArena;

            refreshTimer.Interval = 1000 / 60;
            infoTimer.Interval = 1000;
            infoTimer.Tick += ShowInfo;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.closing = true;
            base.OnClosing(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.SetClientSizeCore(550, 700);

            uiSettings.Show(this);
            uiSettings.SetDesktopLocation(1000, 100);
            uiSettings.FormClosed += (sender, args) =>
            {
                if (!this.closing)
                    this.Close();
            };

            ConsoleWindow.Show();

            Console.WriteLine("Loaded...");

            this.simulation = this.BoxArena.RunAsync((index, ball) =>
            {
                Console.WriteLine($"Created ball {ball.Id}...");

                ball.Speed = new System.Drawing.PointF
                {
                    X = (float)rnd.NextDouble() * 2 - 1.0f,
                    Y = (float)rnd.NextDouble() * 2 - 1.0f
                };
                ball.Speed = ball.Speed.Scale(0.5f);

                ball.Position = new System.Drawing.PointF
                {
                    X = (float)rnd.NextDouble() * 2 - 1.0f,
                    Y = (float)rnd.NextDouble() * 2 - 1.0f
                };
            });

            this.refreshTimer.Enabled = true;
            this.infoTimer.Enabled = true;
        }

        private void ShowInfo(object sender, EventArgs args)
        {
            var trainer = Controller.Trainer;

            if (this.simulation.Status == TaskStatus.Running)
            {
                var rewardRange = $"{Controller.Rewards.Min:0.000} ... {Controller.Rewards.Mean:0.000} ... {Controller.Rewards.Max:0.000}";
                Console.WriteLine($"{trainer.Samples:0000}   LOSS: {Controller.Loss.Mean:0.00000000}   REWARDS: {rewardRange}");
                if (uiSettings.ShowBallStatus)
                {
                    Console.WriteLine($"   - Replay count: {trainer.ReplayMemoryCount}");
                    Console.WriteLine($"   - QValue mean: {Controller.QValues.Mean:0.000} / {Controller.QValues.StandardDeviation:0.000}");
                    Console.WriteLine($"   - QValue range: {Controller.QValues.Min:0.000} ... {Controller.QValues.Max:0.000}");
                }

                this.uiFitnessPlot.AddPoint(trainer.Samples, Controller.Loss.Mean, Controller.Rewards.Mean);
            }
            else
            {
                if (this.simulation.IsFaulted)
                {
                    Console.WriteLine($"Simulation stopped! " + this.simulation.Exception?.Message);
                }
                else
                {
                    Console.WriteLine($"Simulation stopped!");
                }
            }
        }

        private void OnRefreshTimer(object sender, EventArgs e)
        {
            if (!this.uiSettings.DontShowSim)
            {
                this.uiArena.ShowKicks = uiSettings.ShowBallStatus;
                this.uiArena.ShowPosition = uiSettings.ShowBallStatus;
                this.uiArena.ShowSpeed = uiSettings.ShowBallStatus;

                this.uiArena.Refresh();
            }
        }
    }
}