using System;
using System.Drawing;
using System.Windows.Forms;
using NeuralMotion.Simulator;
using Util;

namespace NeuralMotion.Views
{
    public partial class BallDisplay : UserControl
    {
        public bool ShowPreviewFlag { get; set; }
        public bool ShowPosition { get; set; }
        public bool ShowKicks { get; set; }
        public bool ShowSpeed { get; set; }

        private readonly Pen penRed = new Pen(Color.Red);
        private readonly Pen penBlue = new Pen(Color.Blue);
        private readonly Pen penGreen = new Pen(Color.Green);
        private Font fontText;
        
        public BoxArena Arena { get; set; }

        public BallDisplay()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw |
                          ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true);

            this.ShowPosition = true;
            this.ShowSpeed = true;
            this.ShowKicks = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (this.Arena != null)
            {
                this.penRed.Width = this.Arena.BallRadius / 5;
                this.penBlue.Width = this.penRed.Width;
                this.penGreen.Width = 0.005f;
                this.fontText = new Font(FontFamily.GenericSansSerif, this.Arena.BallRadius / 2, FontStyle.Regular, GraphicsUnit.Point);
            }
        }

        protected string RenderBallText(Ball ball)
        {
            string text = string.Empty;
            if (ShowPosition)
            {
                text += $"position {ball.Position.ToFormattedString()}\n";
            }
            if (ShowSpeed)
            {
                text += $"speed {ball.Speed.ToFormattedString()}\n";
                text += $"distance {ball.DistanceTravelled:0.00}\n";
                text += $"energy {ball.Energy:0.00}\n";
            }
            if (ShowKicks)
            {
                var totalKicks = ball.KicksToBall + ball.KicksToBorder;
                text += $"kicks {totalKicks}\n";
            }
            return text.Trim();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            if (this.Arena != null)
            {
                var smallerClientSize = Math.Min(this.ClientSize.Width, this.ClientSize.Height);
                e.Graphics.ScaleTransform(smallerClientSize, smallerClientSize);
                e.Graphics.ScaleTransform(0.5f, 0.5f);
                e.Graphics.TranslateTransform(1.0f, 1.0f);

                var radius = Arena.BallRadius * 2;
                var halfRadius = Arena.BallRadius;

                var time = $"{this.Arena.CurrentSimulationTime} {(ShowPreviewFlag ? "PREVIEW" : "")}";
                e.Graphics.DrawString(time, this.fontText, penRed.Brush, new PointF(-0.9f, -0.9f));

                var point = new PointF(0, 0);
                e.Graphics.DrawLine(penRed, point.Offset(-0.1f, 0), point.Offset(0.1f, 0));
                e.Graphics.DrawLine(penRed, point.Offset(0, -0.1f), point.Offset(0, 0.1f));

                for (var index = 0; index < Arena.EngineBalls.Length; index++)
                {
                    var ball = Arena.EngineBalls[index];
                    point = Arena.EngineBalls[index].Position;

                    e.Graphics.DrawLine(penGreen, point, point.Offset(ball.Acceleration));

                    if (Arena.CurrentSimulationTime - ball.LastCollisionTime < 0.1)
                        e.Graphics.DrawEllipse(penBlue, point.X - halfRadius, point.Y - halfRadius, radius, radius);
                    else
                        e.Graphics.DrawEllipse(penRed, point.X - halfRadius, point.Y - halfRadius, radius, radius);

                    var text = RenderBallText(ball);
                    if (text.Length > 0)
                    {
                        e.Graphics.DrawString(text, fontText, penRed.Brush, point.Offset(-radius, halfRadius * 1.1f));
                    }
                }
            }
        }
    }
}
