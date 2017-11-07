using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers;
using ConvNetSharp.Volume;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuralMotion.Views
{
    public partial class PlotWindow : Form
    {
        public bool ShowClasses { get; set; }

        private Net<double> net;
        private Volume<double> input;

        private (Volume<double>, Volume<double>) ForwardNet(PointD min, PointD max, int resolution)
        {
            var spanX = (max.X - min.X) / resolution;
            var spanY = (max.Y - min.Y) / resolution;

            int count = 0;
            input = BuilderInstance<double>.Volume.SameAs(Shape.From(1, 1, 2, resolution * resolution));
            for (var y = min.Y; y <= max.Y; y += spanY)
                for (var x = min.X; x <= max.X; x += spanX)
                {
                    if (count == input.BatchSize)
                        break;

                    input.Set(0, 0, 0, count, x);
                    input.Set(0, 0, 1, count, y);
                    count++;
                }

            lock (net)
            {
                return (input, net.Forward(input));
            }
        }

        private void DrawHeatmaps(int resolution, Volume<double> output)
        {
            int count;
            for (var i = 0; i < output.Depth; i++)
            {
                count = 0;
                for (var y = 0; y < resolution; y += 1)
                    for (var x = 0; x < resolution; x += 1)
                    {
                        var view = flowPanel.Controls[i] as OxyPlotView;
                        var hms = view.Model.Series[0] as HeatMapSeries;
                        if (hms.Data.Length == 0)
                            hms.Data = new double[resolution, resolution];
                        hms.Data[x, y] = output.Get(0, 0, i, count);
                        view.InvalidatePlot(true);
                        count++;
                    }
            }
        }

        private void DrawClasses(ScatterSeries series, Volume<double> input, Volume<double> output)
        {

        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.refreshTimer.Interval = 2000;
            this.refreshTimer.Enabled = true;
        }

        public static Task Show(Func<PlotWindow> factory)
        {
            var task = Task.Run(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(factory());
                Console.WriteLine("Display thread stopped!");
            });

            while (task.Status == TaskStatus.WaitingForActivation ||
                task.Status == TaskStatus.WaitingToRun ||
                task.Status == TaskStatus.Created)
                Thread.Sleep(0);

            return task;
        }

        private void AddPlot(PlotModel model)
        {
            var plotView = new OxyPlotView()
            {
                Size = new Size(500, 400)
            };
            plotView.Model = model;
            this.flowPanel.Controls.Add(plotView);
        }

        public static void OutputHeatmaps(
            Net<double> net,
            double minX, double maxX, double minY, double maxY,
            string xTitle = "X",
            string yTitle = "Y")
        {
            PlotWindow.Show(() =>
            {
                var window = new PlotWindow();
                window.InitializeComponent();
                window.net = net;
                var channels = window.net.Layers.Last().OutputDepth;
                window.flowPanel.SuspendLayout();
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

                    window.AddPlot(model);
                }
                window.flowPanel.ResumeLayout(true);

                window.refreshTimer.Tick += (o, s) =>
                {
                    var (input, output) = window.ForwardNet((minX, minY), (maxX, maxY), 50);
                    window.DrawHeatmaps(50, output);
                };

                return window;
            });
        }

        public static void ClassScatterplot(
            Net<double> net,
            double minX, double maxX, double minY, double maxY,
            string xTitle = "X",
            string yTitle = "Y")
        {
            PlotWindow.Show(() =>
            {
                var window = new PlotWindow();
                window.InitializeComponent();
                window.net = net;
                var channels = window.net.Layers.Last().OutputDepth;
                window.flowPanel.SuspendLayout();

                var series = new ScatterSeries
                {
                    Selectable = false,
                    XAxisKey = "x",
                    YAxisKey = "y",
                    ColorAxisKey = "classes",
                    Title = "Classes",
                };

                var model = new PlotModel()
                {
                    Axes =
                    {
                        new CategoryColorAxis
                        {
                            Key = "classes",
                            ActualLabels =
                            {
                            }
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
                        series
                    }
                };

                window.AddPlot(model);
                window.flowPanel.ResumeLayout(true);

                window.refreshTimer.Tick += (o, s) =>
                {
                    var (input, output) = window.ForwardNet((minX, minY), (maxX, maxY), 50);
                    window.DrawClasses(series, input, output);
                };

                return window;
            });
        }
    }
}
