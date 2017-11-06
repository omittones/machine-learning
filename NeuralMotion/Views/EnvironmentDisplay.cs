using System;
using System.Windows.Forms;

namespace NeuralMotion.Views
{
    public partial class EnvironmentDisplay : UserControl
    {
        public IRenderer Renderer { get; set; }

        public EnvironmentDisplay()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw |
                          ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            var graphics = e.Graphics;
            var smallerClientSize = Math.Min(this.ClientSize.Width, this.ClientSize.Height);
            var center = (this.ClientSize.Width - smallerClientSize) / 2.0f;
            graphics.TranslateTransform(center, 0);
            graphics.ScaleTransform(smallerClientSize, smallerClientSize);
            graphics.ScaleTransform(0.5f, 0.5f);
            graphics.TranslateTransform(1.0f, 1.0f);

            if (Renderer != null)
            {
                Renderer.Render(graphics);
            }
        }
    }
}
