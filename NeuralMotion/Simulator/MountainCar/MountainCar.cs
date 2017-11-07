//http://incompleteideas.net/sutton/MountainCar/MountainCar1.cp
//permalink: https://perma.cc/6Z2N-PFWC

using gym;
using NeuralMotion.Simulator;
using NeuralMotion.Views;
using System;
using System.Drawing;
using System.Linq;

public class MountainCar : IRenderer, IEnvironment
{
    private Random np_random = new Random();

    private double min_position;
    private double max_position;
    private double max_speed;
    private double goal_position;
    double[] state;
    protected Space<int> action_space = null;
    protected Space<(double x, double y)> observation_space = null;

    public float CurrentSimulationTime { get; private set; }

    public MountainCar()
    {
        this.min_position = -1.2;
        this.max_position = 0.7;
        this.max_speed = 0.07;
        this.goal_position = 0.5;

        var low = (x: this.min_position, y: -this.max_speed);
        var high = (x: this.max_position, y: this.max_speed);
        this.action_space = Space.Discrete(3);
        this.observation_space = Space.Box(low, high);

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
        this.Step(1);
    }

    public StepReturnType Step(int action)
    {
        if (!this.action_space.contains(action))
            throw new Exception("action invalid");

        var position = this.state[0];
        var velocity = this.state[1];

        velocity += (action * 2 - 1) * 0.001 + Math.Cos(3 * position) * (-0.0025);
        velocity = Clip(velocity, -this.max_speed, this.max_speed);
        position += velocity;
        position = Clip(position, this.min_position, this.max_position);
        if (position == this.min_position && velocity < 0)
            velocity = 0;

        var done = position >= this.goal_position;
        var reward = -1.0;

        this.CurrentSimulationTime += 0.1f;

        this.state = new[] { position, velocity };
        
        return new StepReturnType
        {
            reward = reward,
            done = done,
            info = new { },
            observation = this.state
        };
    }

    public void Reset()
    {
        this.state = new[] { this.np_random.NextDouble() * 0.2 - 0.6, 0 };
        this.CurrentSimulationTime = 0;
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
        graphics.DrawString(this.CurrentSimulationTime.ToString("0.0"), fontText, Brushes.Black, -0.8f, -0.8f);

        var scale = 2.1f / (float)(max_position - min_position);
        graphics.ScaleTransform(scale, -scale);
        graphics.TranslateTransform(0.3f, -0.8f);
        var tscreen = graphics.Transform;

        blackPen.Width = 0.008f;
        var from = new PointD(min_position, height(min_position));
        for (var pos = min_position; pos < max_position; pos += 0.1)
        {
            var to = new PointD(pos, height(pos));
            graphics.DrawLine(blackPen, from, to);
            from = to;
        }

        var position = (float)this.state[0];

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

        var circle = carHeight / 2.5f;
        graphics.TranslateTransform(carWidth / 4, 0);
        graphics.FillEllipse(Brushes.DarkGray, -circle, -circle, 2 * circle, 2 * circle);
        graphics.Transform = tcar;

        graphics.TranslateTransform(-carWidth / 4, 0);
        graphics.FillEllipse(Brushes.DarkGray, -circle, -circle, 2 * circle, 2 * circle);

        graphics.Transform = tscreen;
        var flagx = (float)this.goal_position;
        var flagy1 = (float)this.height(this.goal_position);
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
