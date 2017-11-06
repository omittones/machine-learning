//http://incompleteideas.net/sutton/MountainCar/MountainCar1.cp
//permalink: https://perma.cc/6Z2N-PFWC

using gym;
using NeuralMotion.Views;
using System;
using System.Drawing;
using System.Linq;

public class MountainCar : IRenderer
{
    private Random np_random = new Random();

    private double min_position;
    private double max_position;
    private double max_speed;
    private double goal_position;
    double[] state;
    protected Space<int> action_space = null;
    protected Space<(double x, double y)> observation_space = null;

    public MountainCar()
    {
        this.min_position = -1.2;
        this.max_position = 0.6;
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

        this.state = new[] { position, velocity };

        return new StepReturnType
        {
            reward = reward,
            done = done,
            info = new { },
            observation = this.state
        };
    }

    public object Reset()
    {
        this.state = new[] { this.np_random.NextDouble() * 0.2 - 0.6, 0 };
        return state;
    }

    public double height(double xs)
    {
        return Math.Sin(3 * xs) * .45 + .55;
    }

    public void Render(Graphics graphics)
    {
        graphics.Clear(Color.White);
        var from = new PointD(min_position, height(min_position));
        for (var x = min_position; x < max_position; x += 0.1)
        {
            var to = new PointD(x, height(x));
            graphics.DrawLine(Pens.Black, from, to);
            from = to;
        }

        var position = this.state[0];
        var carwidth = 0.4f;
        var carheight = 0.2f;
        var clearance = 0.1f;
        (var l, var r, var t, var b) = (-carwidth / 2, carwidth / 2, carheight, 0);

        var car = new[] { new PointD(l, b), new PointD(l, t), new PointD(r, t), new PointD(r, b) }.ToPointF();

        var tscreen = graphics.Transform;
        graphics.RotateTransform((float)Math.Cos(3 * position));
        var tcar = graphics.Transform;

        graphics.TranslateTransform(0, clearance);
        graphics.FillPolygon(Brushes.LightGray, car);

        var circle = carheight / 2.5f;
        graphics.TranslateTransform(carwidth / 4, clearance);
        graphics.FillEllipse(Brushes.DarkGray, circle, circle, 2 * circle, 2 * circle);
        graphics.Transform = tcar;

        graphics.TranslateTransform(-carwidth / 4, clearance);
        graphics.FillEllipse(Brushes.DarkGray, circle, circle, 2 * circle, 2 * circle);

        graphics.Transform = tscreen;
        var flagx = (float)(this.goal_position - this.min_position);
        var flagy1 = (float)this.height(this.goal_position);
        var flagy2 = flagy1 + 0.2f;
        graphics.DrawLine(Pens.Black, flagx, flagy1, flagx, flagy2);
        graphics.FillPolygon(
            Brushes.Gray,
            new[] {
                (flagx, flagy2).ToPoint(),
                (flagx, flagy2 - 10).ToPoint(),
                (flagx + 25, flagy2 - 5).ToPoint()
                });
    }
}
