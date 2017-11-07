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
using System.Windows.Forms;

namespace NeuralMotion.Views
{
    public partial class PlotWindow : Form
    {
        private void DrawHeatmaps(int resolution, Volume<double> output)
        {
            int count;
            for (var i = 0; i < output.Depth; i++)
            {
                count = 0;
                var view = this.Controls[0].Controls[i] as PlotView;
                var hms = view.Model.Series[0] as HeatMapSeries;
                if (hms.Data.Length == 0)
                    hms.Data = new double[resolution, resolution];

                for (var y = 0; y < resolution; y += 1)
                    for (var x = 0; x < resolution; x += 1)
                    {
                        hms.Data[x, y] = output.Get(0, 0, i, count);
                        count++;
                    }

                view.InvalidatePlot(true);
            }
        }

        private void DrawClasses(ScatterSeries series, Volume<double> input, Volume<double> output)
        {
            for (var b = 0; b < input.BatchSize; b++)
            {
                var x = input.Get(0, 0, 0, b);
                var y = input.Get(0, 0, 1, b);
                var klass = output.IndexOfMax(b);
                series.Points.Add(new ScatterPoint(x, y, value: klass));
            }

            series.PlotModel.PlotView.InvalidatePlot(true);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.refreshTimer.Interval = 2000;
            this.refreshTimer.Enabled = true;
        }

        public static PlotWindow Heatmaps(
            Net<double> net,
            double minX = 0,
            double maxX = 1,
            double minY = 0,
            double maxY = 1,
            string xTitle = "X",
            string yTitle = "Y")
        {
            var window = new PlotWindow();
            window.InitializeComponent();
            window.SuspendLayout();

            var flowPanel = new FlowLayoutPanel();
            window.Controls.Add(flowPanel);
            flowPanel.Dock = DockStyle.Fill;

            var channels = net.Layers.Last().OutputDepth;
            for (var i = 0; i < channels; i++)
            {
                var model = new PlotModel()
                {
                    Axes =
                    {
                        new LinearColorAxis
                        {
                            Palette = OxyPalettes.Jet(100),
                            Position = AxisPosition.Right,
                            Minimum = -1,
                            Maximum = 2
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
                            Selectable = false,
                            X0 = minX,
                            X1 = maxX,
                            Y0 = minY,
                            Y1 = maxY,
                            XAxisKey = "x",
                            YAxisKey = "y",
                            Data = new double[,]{ },
                            Interpolate = false,
                            Title = $"Action({i})"
                        }
                    }
                };

                var plotView = new PlotView()
                {
                    Size = new Size(450, 350)
                };
                plotView.Model = model;
                flowPanel.Controls.Add(plotView);
            }
            window.ResumeLayout(true);

            window.refreshTimer.Tick += (o, s) =>
            {
                var (input, output) = net.ForwardArea((minX, minY), (maxX, maxY), 50);
                window.DrawHeatmaps(50, output);
            };

            return window;
        }

        public static PlotWindow Scatterplot(
            Net<double> net,
            double minX = 0,
            double maxX = 1,
            double minY = 0,
            double maxY = 1,
            string xTitle = "X",
            string yTitle = "Y")
        {
            var window = new PlotWindow();
            window.InitializeComponent();
            window.SuspendLayout();
            var channels = net.Layers.Last().OutputDepth;

            var series = new ScatterSeries
            {
                Selectable = false,
                XAxisKey = "x",
                YAxisKey = "y",
                MarkerType = MarkerType.Circle,
                MarkerStrokeThickness = 0,
                Background = OxyColor.Parse("#FFFFFF"),
                MarkerSize = 3,
                ColorAxisKey = "classes",
                RenderInLegend = true
            };

            var model = new PlotModel()
            {
                Axes =
                {
                    new LinearColorAxis
                    {
                        Palette = OxyPalettes.HueDistinct(channels),
                        Position = AxisPosition.Right,
                        Title = "Classes",
                        Key = "classes",
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
                    series
                }
            };

            var plotView = new PlotView();
            plotView.Dock = DockStyle.Fill;
            plotView.Model = model;
            window.Controls.Add(plotView);
            window.ResumeLayout(true);

            window.refreshTimer.Tick += (o, s) =>
            {
                var (input, output) = net.ForwardArea((minX, minY), (maxX, maxY), 50);
                window.DrawClasses(series, input, output);
            };

            return window;
        }
    }
}