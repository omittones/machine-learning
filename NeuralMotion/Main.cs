using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;
using System.Linq;

namespace NeuralMotion
{
    public partial class Main : Form
    {
        private static readonly Random rnd = new Random();
        private readonly Settings uiSettings;
        private readonly DQNController controller;
        private bool closing;
        private Task simulation;

        public BoxArena BoxArena { get; private set; }

        public Main()
        {
            this.controller = new DQNController();
            this.BoxArena = new BoxArena(controller, 4, 0.06f)
            {
                SimulationDuration = 20000,
                RealTime = true
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

                ball.Position = new System.Drawing.PointF
                {
                    X = (float)rnd.NextDouble() * 2 - 1.0f,
                    Y = (float)rnd.NextDouble() * 2 - 1.0f
                };
            });

            this.refreshTimer.Enabled = true;
            this.infoTimer.Enabled = true;
        }

        private double TotalRewards()
        {
            return BoxArena.EngineBalls.Sum(b => controller.Reward(b));
        }

        private double oldRewards = 0;
        private void ShowInfo(object sender, EventArgs args)
        {
            var trainer = controller.Trainer;

            if (this.simulation.Status == TaskStatus.Running)
            {
                var newRewards = TotalRewards();
                var dRewards = newRewards - oldRewards;
                oldRewards = newRewards;

                Console.WriteLine($"{trainer.Samples:0000}   LOSS: {trainer.Loss:0.00000000}   REWARDS:{dRewards:0.0000}");
                this.uiFitnessPlot.AddPoint(trainer.Samples, trainer.Loss * 50, dRewards);
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


            //Console.WriteLine("{0:000} epoch, fitness:{1:000.0000} ({2})", this.evolver.CurrentGeneration, this.evolver.BestFitness, hash);

            //if (this.uiSettings.WriteStatus)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("Max possible speed: {0}", this.BoxArena.MaximumBallSpeed);
            //    Console.WriteLine("        Gene count: {0}", this.evolver.CurrentChampGenome.Length);

            //    var lines = this.evolver.StatusReport().Select(l =>
            //    {
            //        var totalLength = Math.Max(0, 18 - l.IndexOf(':')) + l.Length;
            //        return l.PadLeft(totalLength, ' ');
            //    }).ToArray();
            //    foreach (var line in lines)
            //        Console.WriteLine(line);

            //    Console.WriteLine();
            //    Console.WriteLine("Best ball");
            //    Console.WriteLine("      Brain: {0}", hash);
            //    Console.WriteLine("      Kicks To Ball: {0}", bestBall.KicksToBall);
            //    Console.WriteLine("      Kicks From Ball: {0}", bestBall.KicksFromBall);
            //    Console.WriteLine("      Kicks To Border: {0}", bestBall.KicksToBorder);
            //    Console.WriteLine("      Distance Travelled: {0}", bestBall.DistanceTravelled);
            //    Console.WriteLine("      Speed: {0}", bestBall.Speed.Distance(0, 0));
            //    Console.WriteLine("Total Collisions: " + this.BoxArena.TotalCollisions);
            //    Console.WriteLine("Best Fitness: {0}", this.evolver.BestFitness);
            //    Console.WriteLine();
            //    Console.WriteLine();
            //}

            //if (!this.uiSettings.DontPreviewBest)
            //{
            //    this.uiArena.ShowPreviewFlag = true;

            //    if (!this.BoxArena.RealTime)
            //        this.BoxArena.RealTime = true;

            //    this.fitnessFunction.Evaluate(this.evolver.CurrentChampGenome);

            //    if (this.BoxArena.RealTime)
            //        this.BoxArena.RealTime = false;

            //    this.uiArena.ShowPreviewFlag = false;
            //}
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