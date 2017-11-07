//http://incompleteideas.net/sutton/MountainCar/MountainCar1.cp
//permalink: https://perma.cc/6Z2N-PFWC

using System;
using System.Drawing;
using System.Linq;
using NeuralMotion.Simulator;
using NeuralMotion.Views;

public class MountainCar : IRenderer, IEnvironment
{
    private Random np_random = new Random();

    public const double MinPosition = -1.2;
    public const double MaxPosition = 0.7;
    public const double MaxSpeed = 0.07;
    public const double GoalPosition = 0.5;

    public float SimTime { get; private set; }
    public double CarPosition { get; private set; }
    public double CarVelocity { get; private set; }
    public double Reward { get; private set; }
    public bool Done { get; private set; }
    public int Action { get; set; }
        
    public MountainCar()
    {
        var low = (x: MinPosition, y: -MaxSpeed);
        var high = (x: MaxPosition, y: MaxSpeed);

        this.Reset();
    }

    public long[] Seed(int? seed = null)
    {
        var s = seed.GetValueOrDefault();
        this.np_random = new Random(s);
        return new long[] { s };
    }

    public static double Clip(double value, double min, double max)
    {
        if (value > max)
            return max;
        if (value < min)
            return min;
        return value;
    }

    public void Step()
    {
        if (Action < 0 || Action > 2)
            throw new Exception("action invalid");

        var position = this.CarPosition;
        var velocity = this.CarVelocity;
        if (position >= GoalPosition)
        {
            this.Done = true;
            this.Reward = 1.0;
            velocity = 0;
        }
        else
        {
            this.Done = false;
            this.Reward = 0.0;
            velocity += (this.Action - 1) * 0.001 + Math.Cos(3 * position) * (-0.0025);
            velocity = Clip(velocity, -MaxSpeed, MaxSpeed);
        }

        //var dist = GoalPosition - position;
        //this.Reward = 1.0 / (dist * dist + 0.000001);

        position += velocity;
        if (position < MinPosition || position > MaxPosition)
        {
            position = Clip(position, MinPosition, MaxPosition);
            velocity = -velocity * 0.1f;
        }

        this.SimTime += 0.02f;
        this.CarPosition = position;
        this.CarVelocity = velocity;
    }

    public void Reset()
    {
        this.CarPosition = this.np_random.NextDouble() * 0.2 - 0.6;
        this.CarVelocity = 0;
        this.SimTime = 0;
    }

    public double height(double xs)
    {
        return Math.Sin(3 * xs) * .45 + .55;
    }

    public void Render(Graphics graphics)
    {
        var blackPen = Pens.Black.Clone() as Pen;

        graphics.Clear(Color.White);
        blackPen.Width = 0.005f;
        graphics.DrawRectangle(blackPen, -1, -1, 2, 1.99f);
        
        var fontText = new Font(FontFamily.GenericSansSerif, 0.05f, FontStyle.Regular, GraphicsUnit.Point);
        graphics.DrawString(this.SimTime.ToString("0.0"), fontText, Brushes.Black, -0.8f, -0.8f);

        var scale = 2.1f / (float)(MaxPosition - MinPosition);
        graphics.ScaleTransform(scale, -scale);
        graphics.TranslateTransform(0.3f, -0.8f);
        var tscreen = graphics.Transform;

        blackPen.Width = 0.008f;
        var from = new PointD(MinPosition, height(MinPosition));
        for (var pos = MinPosition; pos < MaxPosition; pos += 0.1)
        {
            var to = new PointD(pos, height(pos));
            graphics.DrawLine(blackPen, from, to);
            from = to;
        }

        var position = (float)this.CarPosition;

        var carWidth = 0.18f;
        var carHeight = 0.09f;
        var clearance = 0.04f;
        graphics.TranslateTransform(position, (float)height(position));
        graphics.RotateTransform((float)(Math.Cos(3 * position) * 60));
        graphics.TranslateTransform(0, clearance);
        var tcar = graphics.Transform;

        (var l, var r, var t, var b) = (-carWidth / 2, carWidth / 2, carHeight, 0);
        var car = new[] { new PointD(l, b), new PointD(l, t), new PointD(r, t), new PointD(r, b) }.ToPointF();
        graphics.FillPolygon(Brushes.LightGray, car);
        if (this.Action == 0)
            graphics.DrawString("<", fontText, Brushes.Black, l - 0.05f, b);
        else if (this.Action == 2)
            graphics.DrawString(">", fontText, Brushes.Black, r, b);

        var circle = carHeight / 2.5f;
        graphics.TranslateTransform(carWidth / 4, 0);
        graphics.FillEllipse(Brushes.DarkGray, -circle, -circle, 2 * circle, 2 * circle);
        graphics.Transform = tcar;

        graphics.TranslateTransform(-carWidth / 4, 0);
        graphics.FillEllipse(Brushes.DarkGray, -circle, -circle, 2 * circle, 2 * circle);

        graphics.Transform = tscreen;
        var flagx = (float)GoalPosition;
        var flagy1 = (float)this.height(GoalPosition);
        var flagy2 = flagy1 + 0.1f;
        graphics.DrawLine(blackPen, flagx, flagy1, flagx, flagy2);
        graphics.FillPolygon(
            Brushes.Yellow,
            new[] {
                (flagx, flagy2).ToPoint(),
                (flagx, flagy2 - 0.05f).ToPoint(),
                (flagx + 0.07f, flagy2 - 0.025f).ToPoint()
                });
    }    
}
