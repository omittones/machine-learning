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

        private readonly PlotWindow uiDiagnostics;
        private readonly PlotWindow uiValueWindow;
        private readonly Settings uiSettings;
        private bool closing;
        private Task simulation;

        public Session Session { get; private set; }
        public IEnvironment Environment { get; private set; }
        public object Controller { get; private set; }
        public IRenderer Renderer { get; private set; }

        public Main()
        {
            Ops<float>.SkipValidation = true;
            Ops<double>.SkipValidation = true;

            var ctrl = new DQNBallController();
            var env = new BallArena(ctrl, 10);
            this.Renderer = new BallArenaRenderer(env);

            this.Environment = env;
            this.Controller = ctrl;

            this.Session = Session.Create(env, ctrl);
            this.Session.RestartOnEnd = true;
            this.Session.RealTime = false;

            InitializeComponent();

            this.uiSettings = new Settings();
            this.uiSettings.RealTime = this.Session.RealTime;
            this.uiSettings.DontShowSim = false;
            this.uiSettings.ShowDiagnostics = true;
            this.uiSettings.LearningRate = ctrl.Trainer.LearningRate;
            this.uiDisplay.Renderer = this.Renderer;

            //this.uiDiagnostics = PlotWindow.ValueHeatmaps(
            //    ctrl.Policy,
            //    MountainCar.MinPosition, MountainCar.MaxPosition,
            //    -MountainCar.MaxVelocity * 10, MountainCar.MaxVelocity * 10,
            //    minClass: 0,
            //    maxClass: 1,
            //    xTitle: "position",
            //    yTitle: "speed",
            //    titles: "back,nothing,forward");

            //this.uiValueWindow = PlotWindow.ValueHeatmaps(
            //    ctrl.Value,
            //    MountainCar.MinPosition, MountainCar.MaxPosition,
            //    -MountainCar.MaxVelocity * 10, MountainCar.MaxVelocity * 10);

            //this.uiDiagnostics = PlotWindow.ClassHeatmap(
            //    ctrl.Net,
            //    MountainCar.MinPosition, MountainCar.MaxPosition,
            //    -MountainCar.MaxVelocity * 10, MountainCar.MaxVelocity * 10,
            //    "position", "speed", "back,nothing,forward",
            //    () => new PointD(env.CarPosition, env.CarVelocity));

            //this.uiDiagnostics.FormClosed += (s, e) => this.Close();
            //this.uiValueWindow.FormClosed += (s, e) => this.Close();

            refreshTimer.Interval = 1000 / 60;
            refreshTimer.Tick += (s, e) =>
            {
                this.Session.RealTime = uiSettings.RealTime;

                //this.Renderer.ShowKicks = uiSettings.ShowBallStatus;
                //this.Renderer.ShowPosition = uiSettings.ShowBallStatus;
                //this.Renderer.ShowSpeed = uiSettings.ShowBallStatus;
                //ctrl.PolicyTrainer.LearningRate = uiSettings.LearningRate;
                //ctrl.Trainer.Epsilon = uiSettings.Epsilon;

                if (this.uiDiagnostics != null)
                    this.uiDiagnostics.TrackChanges = !this.uiSettings.DontShowSim;
                if (this.uiValueWindow != null)
                    this.uiValueWindow.TrackChanges = !this.uiSettings.DontShowSim;

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

            if (uiValueWindow != null)
            {
                uiValueWindow.Show();
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
                    case ActorCriticCarController ac:
                        Console.WriteLine($"{ac.Epochs:0000}  REWARDS: {ac.Rewards.ToRangeString()}");
                        if (uiSettings.ShowDiagnostics)
                        {
                            Console.WriteLine($"   - Estimated rewards: {ac.PolicyTrainer.EstimatedRewards:0.000}");
                            Console.WriteLine($"   - Good vs. Bad: {ac.GoalReached}/{ac.TrainingTimedout}   ({(ac.GoalReached * 100.0 / (ac.GoalReached + ac.TrainingTimedout)):0.00}%)");
                        }
                        this.uiFitnessPlot.AddPoint(ac.Epochs, ac.ValueTrainer.Loss, ac.PolicyTrainer.EstimatedRewards);
                        break;
                    case PolicyGradientCarController pg:
                        Console.WriteLine($"{pg.Epochs:0000}  REWARDS: {pg.Rewards.ToRangeString()}");
                        if (uiSettings.ShowDiagnostics)
                        {
                            Console.WriteLine($"   - Estimated rewards: {pg.Trainer.EstimatedRewards:0.000}");
                            Console.WriteLine($"   - Good vs. Bad: {pg.GoalReached}/{pg.TrainingTimedout}   ({(pg.GoalReached * 100.0 / (pg.GoalReached + pg.TrainingTimedout)):0.00}%)");
                        }
                        this.uiFitnessPlot.AddPoint(pg.Epochs, pg.Trainer.Loss, pg.Trainer.EstimatedRewards);
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