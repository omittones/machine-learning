using System;
using System.Windows.Forms;

namespace NeuralMotion
{
    public partial class Settings : Form
    {
        public new Main Owner { get; private set; }

        public bool WriteStatus => uiWriteLog.Checked;
        public bool DontShowSim => uiDontShowSim.Checked;
        public bool ShowBallStatus => uiShowBallStatusText.Checked;
        
        public Settings()
        {
            InitializeComponent();

            this.uiToggleSpeed.Click += OnToggleSpeed;
            this.uiWriteLog.Checked = true;
            this.uiToggleSpeed.Text = "Fast";
            this.uiDecreaseLearningRate.Click += DecreaseLearningRate;
            this.uiIncreaseLearningRate.Click += IncreaseLearningRate;

        }

        private void NotifyAboutLR()
        {
            Console.WriteLine($"Adjusting LR to {Owner.Controller.Trainer.Alpha:0.0000000}");
        }

        private void IncreaseLearningRate(object sender, EventArgs e)
        {
            Owner.Controller.Trainer.Alpha *= 1.5;
            NotifyAboutLR();
        }

        private void DecreaseLearningRate(object sender, EventArgs e)
        {
            Owner.Controller.Trainer.Alpha /= 1.5;
            if (Owner.Controller.Trainer.Alpha < 0.000001)
                Owner.Controller.Trainer.Alpha = 0.000001;
            NotifyAboutLR();
        }
                
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Owner.BoxArena.RealTime)
                uiToggleSpeed.Text = "Fast";
            else
                uiToggleSpeed.Text = "Slow";

            base.OnPaint(e);
        }

        private void OnToggleSpeed(object sender, EventArgs e)
        {
            if (Owner.BoxArena.RealTime)
                Owner.BoxArena.RealTime = false;
            else
                Owner.BoxArena.RealTime = true;
            Refresh();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Owner = (Main) base.Owner;
        }
    }
}
