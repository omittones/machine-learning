using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers;
using ConvNetSharp.Volume;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Linq;
using System.Windows.Forms;

namespace NeuralMotion.Test
{
    public partial class Main : Form
    {
        private Net<double> net;
        private Volume<double> input;
        private PlotModel plotModel;

        public void RefreshSeries(object sender, EventArgs args)
        {
            var hms = plotModel.Series[0] as HeatMapSeries;
            int resolution = 50;
            int count = 0;
            if (hms.Data.Length == 0)
            {
                hms.Data = new double[resolution, resolution];
                input = BuilderInstance<double>.Volume.SameAs(Shape.From(1, 1, 2, resolution * resolution));
                count = 0;
                for (var y = 0; y < resolution; y += 1)
                    for (var x = 0; x < resolution; x += 1)
                    {
                        input.Set(0, 0, 0, count, x / (double)resolution);
                        input.Set(0, 0, 1, count, y / (double)resolution);
                        count++;
                    }
            }

            lock (net)
            {
                var isReg = net.Layers.Last() is RegressionLayer<double>;

                count = 0;
                var output = net.Forward(input);
                for (var y = 0; y < resolution; y += 1)
                    for (var x = 0; x < resolution; x += 1)
                    {
                        var isa = output.Get(0, 0, 0, count);
                        var isb = output.Get(0, 0, 1, count);
                        if (isReg)
                            hms.Data[x, y] = isa - isb;
                        else
                            hms.Data[x, y] = isa;

                        //var sum = isa + isb;
                        //sum = isa / sum;
                        //hms.Data[x, y] = isb;

                        count++;
                    }
            }

            plotView.InvalidatePlot(true);
        }

        public Main(Net<double> net)
        {
            InitializeComponent();

            this.net = net;
            this.plotModel = new PlotModel()
            {
                Axes =
                {
                    new LinearColorAxis
                    {
                         Palette = OxyPalettes.Jet(100)
                    }
                },
                PlotType = PlotType.XY,
                Series =
                {
                    new HeatMapSeries
                    {
                        RenderMethod = HeatMapRenderMethod.Bitmap,
                        Selectable = false,
                        X0 = 0,
                        X1 = 1,
                        Y0 = 0,
                        Y1 = 1,
                        Data = new double[,]{ },
                        Interpolate = false
                    }
                }
            };

            this.plotView.Model = plotModel;
            this.plotView.Controller = null;

            this.refreshTimer.Interval = 100;
            this.refreshTimer.Tick += this.RefreshSeries;
            this.refreshTimer.Enabled = true;
        }
    }
}
