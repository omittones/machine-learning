using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers;
using ConvNetSharp.Volume;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace NeuralMotion.Views
{
    public partial class PlotWindow : Form
    {
        public bool TrackChanges { get; set; }

        public PlotWindow()
        {
            TrackChanges = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.slowTimer.Interval = 2000;
            this.slowTimer.Enabled = true;
            this.fastTimer.Interval = 1000 / 60;
            this.fastTimer.Enabled = true;
        }

        private void DrawHeatmaps(
            int resolution,
            Volume<double> output,
            double? minClass = null,
            double? maxClass = null)
        {
            double min = double.MaxValue, max = double.MinValue;
            int count;
            for (var channel = 0; channel < output.Depth; channel++)
            {
                count = 0;
                var view = this.Controls[0].Controls[channel] as PlotView;
                var hms = view.Model.Series[0] as HeatMapSeries;
                if (hms.Data.Length < resolution * resolution)
                    hms.Data = new double[resolution, resolution];

                for (var y = 0; y < resolution; y += 1)
                    for (var x = 0; x < resolution; x += 1)
                    {
                        if (count == output.BatchSize)
                            break;
                        hms.Data[x, y] = output.Get(0, 0, channel, count);
                        count++;
                    }

                if (minClass.HasValue)
                    min = minClass.Value;
                else
                    min = Math.Min(min, hms.Data.Min2D());

                if (maxClass.HasValue)
                    max = maxClass.Value;
                else
                    max = Math.Max(max, hms.Data.Max2D());
            }

            for (var channel = 0; channel < output.Depth; channel++)
            {
                var view = this.Controls[0].Controls[channel] as PlotView;
                var colors = view.Model.Axes.OfType<LinearColorAxis>().First();
                colors.Minimum = min;
                colors.Maximum = max;
                view.InvalidatePlot(true);
            }
        }

        private void DrawClasses(int resolution, Volume<double> output)
        {
            var view = this.Controls[0] as PlotView;
            var hms = view.Model.Series.OfType<HeatMapSeries>().First();
            if (hms.Data.Length < resolution * resolution)
                hms.Data = new double[resolution, resolution];

            int count = 0;
            for (var y = 0; y < resolution; y += 1)
                for (var x = 0; x < resolution; x += 1)
                {
                    if (count == output.BatchSize)
                        break;
                    hms.Data[x, y] = output.IndexOfMax(count);
                    count++;
                }

            hms.Invalidate();
            view.InvalidatePlot(true);
        }

        public static PlotWindow ValueHeatmaps(
            Net<double> net,
            double minX = 0,
            double maxX = 1,
            double minY = 0,
            double maxY = 1,
            double? minClass = null,
            double? maxClass = null,
            string xTitle = "X",
            string yTitle = "Y",
            string titles = null)
        {
            var window = new PlotWindow();
            window.InitializeComponent();
            window.SuspendLayout();

            var flowPanel = new FlowLayoutPanel();
            window.Controls.Add(flowPanel);
            flowPanel.Dock = DockStyle.Fill;

            var channels = net.Layers.Last().OutputDepth;
            var size = new Size(50, 400);
            for (var i = 0; i < channels; i++)
            {
                var model = new PlotModel()
                {
                    Axes =
                    {
                        new LinearColorAxis
                        {
                            Palette = OxyPalettes.Jet(100),
                            Position = AxisPosition.Right
                        },
                        new LinearAxis
                        {
                                Title = xTitle,
                                Position = AxisPosition.Bottom,
                                Key = "x"
                        },
                        new LinearAxis
                        {
                            Title = yTitle,
                            Position = AxisPosition.Left,
                            Key = "y"
                        }
                    },
                    PlotType = PlotType.XY,
                    Series =
                    {
                        new HeatMapSeries
                        {
                            RenderMethod = HeatMapRenderMethod.Bitmap,
                            X0 = minX,
                            X1 = maxX,
                            Y0 = minY,
                            Y1 = maxY,
                            XAxisKey = "x",
                            YAxisKey = "y",
                            Data = new double[2,2] { { 0, 0 }, { 0, 0 } },
                            Interpolate = false,
                            Title = titles != null ? titles.Split(',')[i] :  $"Action({i})"
                        }
                    }
                };
                
                var plotView = new PlotView()
                {
                    Size = new Size(450, 350)
                };

                plotView.Model = model;
                flowPanel.Controls.Add(plotView);
                size.Width += plotView.Size.Width;
            }
            window.Size = size;
            window.ResumeLayout(true);

            window.slowTimer.Tick += (o, s) =>
            {
                if (!window.TrackChanges)
                    return;

                Volume<double> output;
                lock (net)
                {
                    (_, output) = net.ForwardArea((minX, minY), (maxX, maxY), 50);
                }

                window.DrawHeatmaps(50,
                    output,
                    minClass,
                    maxClass);
            };

            return window;
        }

        public static PlotWindow ClassHeatmap(
            Net<double> net,
            double minX = 0,
            double maxX = 1,
            double minY = 0,
            double maxY = 1,
            string xTitle = "X",
            string yTitle = "Y",
            string labels = null,
            Func<PointF> tracker = null)
        {
            var window = new PlotWindow();
            window.InitializeComponent();
            window.SuspendLayout();

            var nmClasses = net.Layers.Last().OutputDepth;
            var categories = new CategoryColorAxis
            {
                Palette = OxyPalettes.HueDistinct(nmClasses),
                Position = AxisPosition.Right,
                Title = "Classes",
                Key = "classes",
                Minimum = 0,
                Maximum = nmClasses - 1
            };

            if (labels != null)
            {
                var names = labels.Split(',');
                categories.Labels.AddRange(names);
            }

            var model = new PlotModel()
            {
                Axes =
                {
                    categories,

                    new LinearColorAxis
                    {
                         Palette = OxyPalettes.BlackWhiteRed(2),
                         Minimum = 0,
                         Maximum = 1,
                         Key = "circle"
                    },

                    new LinearAxis
                    {
                        Title = xTitle,
                        Position = AxisPosition.Bottom,
                        Minimum = minX,
                        Maximum = maxX,
                        Key = "x"
                    },
                    new LinearAxis
                    {
                        Title = yTitle,
                        Position = AxisPosition.Left,
                        Minimum = minY,
                        Maximum = maxY,
                        Key = "y"
                    }
                },
                PlotType = PlotType.XY,
                Series =
                {
                    new HeatMapSeries
                    {
                        Selectable = false,
                        XAxisKey = "x",
                        YAxisKey = "y",
                        X0 = minX,
                        X1 = maxX,
                        Y0 = minY,
                        Y1 = maxY,
                        ColorAxisKey = "classes",
                        TrackerKey = "temp",
                        Interpolate = false,
                        RenderMethod = HeatMapRenderMethod.Bitmap,
                        Data = new double[2,2] { { 0, 0 }, { 0, 0 } }
                    },
                    new ScatterSeries
                    {
                         MarkerType = MarkerType.Circle,
                         MarkerSize = 3,
                         XAxisKey = "x",
                         YAxisKey = "y",
                         ColorAxisKey = "circle",
                         Points =
                         {
                             new ScatterPoint(0, 0, value:0)
                         }
                    }
                }
            };

            var plotView = new PlotView();
            plotView.Dock = DockStyle.Fill;
            plotView.Model = model;
            window.Controls.Add(plotView);
            window.ResumeLayout(true);

            var scatter = model.Series.OfType<ScatterSeries>().First();
            scatter.IsVisible = false;

            if (tracker != null)
            {
                scatter.IsVisible = true;

                int count = 0;
                window.fastTimer.Tick += (o, s) =>
                {
                    if (!window.TrackChanges)
                        return;

                    var point = tracker();
                    scatter.Points[0] = new ScatterPoint(point.X, point.Y, value: 0);

                    if (count > 50)
                    {
                        Volume<double> output;
                        lock (net)
                        {
                            (_, output) = net.ForwardArea((minX, minY), (maxX, maxY), 50);
                        }
                        window.DrawClasses(50, output);
                        count = 0;
                    }
                    else
                    {
                        plotView.InvalidatePlot(true);
                    }

                    count++;
                };
            }
            else
            {
                window.slowTimer.Tick += (o, s) =>
                {
                    if (!window.TrackChanges)
                        return;

                    Volume<double> output;
                    lock (net)
                    {
                        (_, output) = net.ForwardArea((minX, minY), (maxX, maxY), 50);
                    }
                    window.DrawClasses(50, output);
                };
            }

            return window;
        }
    }
}