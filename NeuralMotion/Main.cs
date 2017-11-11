using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;
using NeuralMotion.Intelligence;
using NeuralMotion.Views;
using ConvNetSharp.Core.Training;
using System.Drawing;
using ConvNetSharp.Core;

namespace NeuralMotion
{
    public partial class Main : Form
    {
        private static readonly Random rnd = new Random();
        private readonly Settings uiSettings;
        private bool closing;
        private Task simulation;
        private readonly PlotWindow uiDiagnostics;

        public Session Session { get; private set; }
        public IEnvironment Environment { get; private set; }
        public object Controller { get; private set; }
        public IRenderer Renderer { get; private set; }

        public Main()
        {
            Ops<float>.SkipValidation = true;
            Ops<double>.SkipValidation = true;

            var ctrl = new DQNCarController();
            var env = new MountainCar();
            env.Strict = true;

            //this.Controller = new PolicyGradientsController(500, 5);
            //this.Controller = new DQNController();
            //this.Environment = new BallArena(Controller, 10, 0.06f);
            //this.Renderer = new BallArenaRenderer(this.Environment);

            this.Environment = env;
            this.Controller = ctrl;

            this.Session = Session.Create(env, ctrl);
            this.Session.RestartOnEnd = true;
            this.Session.RealTime = true;

            InitializeComponent();

            this.uiSettings = new Settings();
            this.uiSettings.RealTime = this.Session.RealTime;
            this.uiSettings.DontShowSim = true;
            this.uiSettings.ShowDiagnostics = true;
            this.uiSettings.LearningRate = ctrl.Trainer.LearningRate;
            this.uiDisplay.Renderer = env;

            //this.uiDiagnostics = PlotWindow.ValueHeatmaps(
            //    ctrl.Net,
            //    MountainCar.MinPosition, MountainCar.MaxPosition,
            //    -MountainCar.MaxVelocity * 10, MountainCar.MaxVelocity * 10,
            //    "position", "speed", "back,nothing,forward");

            this.uiDiagnostics = PlotWindow.ClassHeatmap(
                ctrl.Net,
                MountainCar.MinPosition, MountainCar.MaxPosition,
                -MountainCar.MaxVelocity * 10, MountainCar.MaxVelocity * 10,
                "position", "speed", "back,nothing,forward",
                () => new PointD(env.CarPosition, env.CarVelocity));

            this.uiDiagnostics.FormClosed += (s, e) => this.Close();

            refreshTimer.Interval = 1000 / 60;
            refreshTimer.Tick += (s, e) =>
            {
                this.Session.RealTime = uiSettings.RealTime;
                //this.Renderer.ShowKicks = uiSettings.ShowBallStatus;
                //this.Renderer.ShowPosition = uiSettings.ShowBallStatus;
                //this.Renderer.ShowSpeed = uiSettings.ShowBallStatus;

                ctrl.Trainer.LearningRate = uiSettings.LearningRate;
                //ctrl.Trainer.Epsilon = uiSettings.Epsilon;

                this.uiDiagnostics.TrackChanges = !this.uiSettings.DontShowSim;

                if (!this.uiSettings.DontShowSim)
                    this.uiDisplay.Refresh();
            };

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

            ConsoleWindow.Show();

            this.simulation = this.Session.Run();
            this.refreshTimer.Enabled = true;
            this.infoTimer.Enabled = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (uiSettings != null)
            {
                uiSettings.Show(this);
                uiSettings.SetDesktopLocation(1000, 100);
                uiSettings.FormClosed += (sender, args) =>
                {
                    if (!this.closing)
                        this.Close();
                };
            }

            if (uiDiagnostics != null)
            {
                uiDiagnostics.Show();
            }
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
                        if (uiSettings.ShowDiagnostics)
                        {
                            Console.WriteLine($"   - Replay count: {trainer.ReplayMemoryCount}");
                            Console.WriteLine($"   - QValue mean: {dqn.QValues.Mean:0.000} / {dqn.QValues.StandardDeviation:0.000}");
                            Console.WriteLine($"   - QValue range: {dqn.QValues.Min:0.000} ... {dqn.QValues.Max:0.000}");
                            Console.WriteLine($"   - Epsilon: {dqn.Trainer.Epsilon:0.0000}");
                        }
                        this.uiFitnessPlot.AddPoint(trainer.Samples, dqn.Loss.Mean, dqn.Rewards.Mean);
                        break;
                    case DQNCarController dqn:
                        trainer = dqn.Trainer;
                        Console.WriteLine($"{trainer.Samples:0000}   LOSS: {dqn.Loss.Mean:0.00000000}   REWARDS: {dqn.Rewards.ToRangeString()}");
                        if (uiSettings.ShowDiagnostics)
                        {
                            Console.WriteLine($"   - Replay count: {trainer.ReplayMemoryCount}");
                            Console.WriteLine($"   - QValue mean: {dqn.QValues.Mean:0.000} / {dqn.QValues.StandardDeviation:0.000}");
                            Console.WriteLine($"   - QValue range: {dqn.QValues.ToRangeString()}");
                            Console.WriteLine($"   - Reward range: {dqn.Rewards.ToRangeString()}");
                            Console.WriteLine($"   - Epsilon: {dqn.Trainer.Epsilon:0.0000}");
                            Console.WriteLine($"   - Good vs. Bad: {dqn.GoalReached}/{dqn.TrainingTimedout}   ({(dqn.GoalReached * 100.0 / (dqn.GoalReached + dqn.TrainingTimedout)):0.00}%)");
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

    }
}