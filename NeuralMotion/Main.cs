using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;
using NeuralMotion.Intelligence;
using NeuralMotion.Views;
using ConvNetSharp.Core.Training;

namespace NeuralMotion
{
    public partial class Main : Form
    {
        private static readonly Random rnd = new Random();
        private readonly Settings uiSettings;
        private bool closing;
        private Task simulation;
        private readonly PlotWindow uiRewards;

        public Session Session { get; private set; }
        public IEnvironment Environment { get; private set; }
        public object Controller { get; private set; }
        public IRenderer Renderer { get; private set; }

        public Main()
        {
            var env = new MountainCar();
            var ctrl = new DQNCarController();

            //this.Controller = new PolicyGradientsController(500, 5);
            //this.Controller = new DQNController();
            //this.Environment = new BallArena(Controller, 10, 0.06f);
            //this.Renderer = new BallArenaRenderer(this.Environment);

            this.Environment = env;
            this.Controller = ctrl;

            this.Session = Session.Create(env, ctrl);
            this.Session.LimitSimulationDuration = 10;
            this.Session.RestartOnEnd = true;
            this.Session.RealTime = true;

            InitializeComponent();

            this.uiSettings = new Settings();
            this.uiSettings.RealTime = this.Session.RealTime;
            this.uiDisplay.Renderer = env;

            this.uiRewards = new PlotWindow(ctrl.Net,
                MountainCar.MinPosition, MountainCar.MaxPosition,
                -MountainCar.MaxSpeed, MountainCar.MaxSpeed,
                "position", "speed");
            
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

            uiRewards.Show(this);

            ConsoleWindow.Show();

            Console.WriteLine("Loaded...");

            this.simulation = this.Session.Run();
            this.refreshTimer.Enabled = true;
            this.infoTimer.Enabled = true;
        }
        
        private void ShowInfo(object sender, EventArgs args)
        {
            if (this.simulation.Status == TaskStatus.Running)
            {
                string rewardRange;
                DQNTrainer trainer;
                switch (Controller)
                {
                    case PolicyGradientsController pg:
                        rewardRange = $"{pg.Rewards.Min:0.000} ... {pg.Rewards.Mean:0.000} ... {pg.Rewards.Max:0.000}";
                        Console.WriteLine($"{pg.Samples:0000}  REWARDS: {rewardRange}");
                        this.uiFitnessPlot.AddPoint(pg.Samples, 0, pg.Rewards.Mean);
                        break;
                    case DQNBallController dqn:
                        trainer = dqn.Trainer;
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
                    case DQNCarController dqn:
                        trainer = dqn.Trainer;
                        rewardRange = $"{dqn.Rewards.Min:0.000000} ... {dqn.Rewards.Mean:0.000000} ... {dqn.Rewards.Max:0.000000}";
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
            this.Session.RealTime = uiSettings.RealTime;
            //this.Renderer.ShowKicks = uiSettings.ShowBallStatus;
            //this.Renderer.ShowPosition = uiSettings.ShowBallStatus;
            //this.Renderer.ShowSpeed = uiSettings.ShowBallStatus;

            if (!this.uiSettings.DontShowSim)
                this.uiDisplay.Refresh();
        }
    }
}