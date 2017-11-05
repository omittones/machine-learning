using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;
using NeuralMotion.Intelligence;

namespace NeuralMotion
{
    public partial class Main : Form
    {
        private static readonly Random rnd = new Random();
        private readonly Settings uiSettings;
        private bool closing;
        private Task simulation;

        public BoxArena BoxArena { get; private set; }
        public IController Controller { get; private set; }

        public Main()
        {
            this.Controller = new PolicyGradientsController(500, 5);
            this.BoxArena = new BoxArena(SetupEnvironment, Controller, 10, 0.06f)
            {
                LimitSimulationDuration = 1000,
                RestartOnEnd = true,
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

            this.simulation = this.BoxArena.Run();
            this.refreshTimer.Enabled = true;
            this.infoTimer.Enabled = true;
        }

        private void SetupEnvironment(int index, Ball ball)
        {
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
        }

        private void ShowInfo(object sender, EventArgs args)
        {
            if (this.simulation.Status == TaskStatus.Running)
            {
                var pg = Controller as PolicyGradientsController;
                if (pg != null)
                {
                    var rewardRange = $"{pg.Rewards.Min:0.000} ... {pg.Rewards.Mean:0.000} ... {pg.Rewards.Max:0.000}";
                    Console.WriteLine($"{pg.Samples:0000}  REWARDS: {rewardRange}");
                    this.uiFitnessPlot.AddPoint(pg.Samples, 0, pg.Rewards.Mean);
                }
                else
                {
                    var dqn = this.Controller as DQNController;
                    if (dqn != null)
                    {
                        var trainer = dqn.Trainer;
                        var rewardRange = $"{dqn.Rewards.Min:0.000} ... {dqn.Rewards.Mean:0.000} ... {dqn.Rewards.Max:0.000}";
                        Console.WriteLine($"{trainer.Samples:0000}   LOSS: {dqn.Loss.Mean:0.00000000}   REWARDS: {rewardRange}");
                        if (uiSettings.ShowBallStatus)
                        {
                            Console.WriteLine($"   - Replay count: {trainer.ReplayMemoryCount}");
                            Console.WriteLine($"   - QValue mean: {dqn.QValues.Mean:0.000} / {dqn.QValues.StandardDeviation:0.000}");
                            Console.WriteLine($"   - QValue range: {dqn.QValues.Min:0.000} ... {dqn.QValues.Max:0.000}");
                        }
                        this.uiFitnessPlot.AddPoint(trainer.Samples, dqn.Loss.Mean, dqn.Rewards.Mean);
                    }
                }
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
                this.BoxArena.RealTime = uiSettings.RealTime;
                
                this.uiArena.ShowKicks = uiSettings.ShowBallStatus;
                this.uiArena.ShowPosition = uiSettings.ShowBallStatus;
                this.uiArena.ShowSpeed = uiSettings.ShowBallStatus;

                this.uiArena.Refresh();
            }
        }
    }
}