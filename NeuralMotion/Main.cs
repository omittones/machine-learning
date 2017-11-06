using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;
using NeuralMotion.Intelligence;
using NeuralMotion.Views;

namespace NeuralMotion
{
    public partial class Main : Form
    {
        private static readonly Random rnd = new Random();
        private readonly Settings uiSettings;
        private bool closing;
        private Task simulation;

        public Session Session { get; private set; }
        public BallArena Environment { get; private set; }
        public IController Controller { get; private set; }
        public BallArenaRenderer Renderer { get; private set; }

        public Main()
        {
            //this.Controller = new PolicyGradientsController(500, 5);
            this.Controller = new DQNController();
            this.Environment = new BallArena(SetupEnvironment, Controller, 10, 0.06f);
            this.Renderer = new BallArenaRenderer(this.Environment);
            this.Session = new Session(this.Controller, this.Environment)
            {
                LimitSimulationDuration = 500,
                RestartOnEnd = true,
                RealTime = false
            };

            InitializeComponent();

            this.uiSettings = new Settings();
            this.uiDisplay.Renderer = new BallArenaRenderer(this.Environment);

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

            this.simulation = this.Session.Run();
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
                string rewardRange;
                switch (Controller)
                {
                    case PolicyGradientsController pg:
                        rewardRange = $"{pg.Rewards.Min:0.000} ... {pg.Rewards.Mean:0.000} ... {pg.Rewards.Max:0.000}";
                        Console.WriteLine($"{pg.Samples:0000}  REWARDS: {rewardRange}");
                        this.uiFitnessPlot.AddPoint(pg.Samples, 0, pg.Rewards.Mean);
                        break;
                    case DQNController dqn:
                        var trainer = dqn.Trainer;
                        rewardRange = $"{dqn.Rewards.Min:0.000} ... {dqn.Rewards.Mean:0.000} ... {dqn.Rewards.Max:0.000}";
                        Console.WriteLine($"{trainer.Samples:0000}   LOSS: {dqn.Loss.Mean:0.00000000}   REWARDS: {rewardRange}");
                        if (uiSettings.ShowBallStatus)
                        {
                            Console.WriteLine($"   - Replay count: {trainer.ReplayMemoryCount}");
                            Console.WriteLine($"   - QValue mean: {dqn.QValues.Mean:0.000} / {dqn.QValues.StandardDeviation:0.000}");
                            Console.WriteLine($"   - QValue range: {dqn.QValues.Min:0.000} ... {dqn.QValues.Max:0.000}");
                        }
                        this.uiFitnessPlot.AddPoint(trainer.Samples, dqn.Loss.Mean, dqn.Rewards.Mean);
                        break;
                    default:
                        break;
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
                this.Session.RealTime = uiSettings.RealTime;
                this.Renderer.ShowKicks = uiSettings.ShowBallStatus;
                this.Renderer.ShowPosition = uiSettings.ShowBallStatus;
                this.Renderer.ShowSpeed = uiSettings.ShowBallStatus;

                this.uiDisplay.Refresh();
            }
        }
    }
}