using System;
using System.Windows.Forms;

namespace NeuralMotion
{
    public partial class Settings : Form
    {
        public bool WriteStatus
        {
            get => uiWriteLog.Checked;
            set => uiWriteLog.Checked = value;
        }
            
        public bool DontShowSim
        {
            get => uiDontShowSim.Checked;
            set => uiDontShowSim.Checked = value;
        }

        public bool ShowDiagnostics
        {
            get => uiShowDiagnostics.Checked;
            set => uiShowDiagnostics.Checked = value;
        }

        public bool RealTime { get; set; }
        public double LearningRate { get; set; }
        public double Epsilon { get; set; }

        public Settings()
        {
            InitializeComponent();

            this.uiToggleSpeed.Click += OnToggleSpeed;
            this.uiWriteLog.Checked = true;
            this.uiToggleSpeed.Text = "Fast";
            this.uiDecreaseLearningRate.Click += DecreaseLearningRate;
            this.uiIncreaseLearningRate.Click += IncreaseLearningRate;

            this.RealTime = true;
            this.LearningRate = 0.001;
            this.Epsilon = 0.1;
        }

        private void NotifyAboutLR()
        {
            Console.WriteLine($"Adjusting LR to {this.LearningRate:0.0000000}");
        }

        private void IncreaseLearningRate(object sender, EventArgs e)
        {
            this.LearningRate *= 1.5;
            NotifyAboutLR();
        }

        private void DecreaseLearningRate(object sender, EventArgs e)
        {
            this.LearningRate /= 1.5;
            if (this.LearningRate < 0.000001)
                this.LearningRate = 0.000001;
            NotifyAboutLR();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.RealTime)
                uiToggleSpeed.Text = "Fast";
            else
                uiToggleSpeed.Text = "Slow";

            if (this.Epsilon == 0)
                uiToggleExploration.Text = "Exploration == Off";
            else
                uiToggleExploration.Text = "Exploration == On";

            base.OnPaint(e);
        }

        private void OnToggleSpeed(object sender, EventArgs e)
        {
            if (this.RealTime)
                this.RealTime = false;
            else
                this.RealTime = true;
            Refresh();
        }

        double oldExp;
        private void ToggleExploration(object sender, EventArgs e)
        {
            if (this.Epsilon == 0)
            {
                this.Epsilon = oldExp;
            }
            else
            {
                oldExp = this.Epsilon;
                this.Epsilon = 0;
            }
        }
    }
}
