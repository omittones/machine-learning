using System.Drawing;
using NeuralMotion.Simulator;
using Util;
using NeuralMotion.Views;

namespace NeuralMotion.Simulator
{
    public class BallArenaRenderer : IRenderer
    {
        public bool ShowPreviewFlag { get; set; }
        public bool ShowPosition { get; set; }
        public bool ShowKicks { get; set; }
        public bool ShowSpeed { get; set; }

        private Pen penBall;
        private Pen penCollision;
        private Pen penIndicators;
        private Pen penBorder;
        private Font fontText;

        private readonly BallArena arena;

        public BallArenaRenderer(BallArena arena)
        {
            this.arena = arena;

            this.ShowPosition = true;
            this.ShowSpeed = true;
            this.ShowKicks = true;

            this.penBall = new Pen(Color.Red, this.arena.BallRadius / 5);
            this.penCollision = new Pen(Color.Blue, this.penBall.Width);
            this.penIndicators = new Pen(Color.Green, 0.005f);
            this.penBorder = new Pen(Color.LightGray, 0.010f);
            this.fontText = new Font(FontFamily.GenericSansSerif, this.arena.BallRadius / 2, FontStyle.Regular, GraphicsUnit.Point);
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

        public void Render(Graphics g)
        {
            var radius = arena.BallRadius * 2;
            var halfRadius = arena.BallRadius;

            var time = $"{this.arena.SimTime} {(ShowPreviewFlag ? "PREVIEW" : "")}";
            g.DrawString(time, this.fontText, penBall.Brush, new PointF(-0.9f, -0.9f));

            var point = new PointF(0, 0);
            g.DrawLine(penBall, point.Offset(-0.1f, 0), point.Offset(0.1f, 0));
            g.DrawLine(penBall, point.Offset(0, -0.1f), point.Offset(0, 0.1f));
            g.DrawRectangle(penBorder, -1, -1, 2, 2);

            for (var index = 0; index < arena.EngineBalls.Length; index++)
            {
                var ball = arena.EngineBalls[index];
                point = arena.EngineBalls[index].Position;

                g.DrawLine(penIndicators, point, point.Offset(ball.Acceleration));

                if (arena.SimTime - ball.LastCollisionTime < 0.1)
                    g.DrawEllipse(penCollision, point.X - halfRadius, point.Y - halfRadius, radius, radius);
                else
                    g.DrawEllipse(penBall, point.X - halfRadius, point.Y - halfRadius, radius, radius);

                var text = RenderBallText(ball);
                if (text.Length > 0)
                {
                    g.DrawString(text, fontText, penBall.Brush, point.Offset(-radius, halfRadius * 1.1f));
                }

                var indicators = ball.Indicators;
                if (indicators != null)
                    foreach (var indicator in indicators)
                        indicator?.Draw(g);
            }
        }
    }
}
