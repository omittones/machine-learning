using System.Windows.Forms;

namespace Environments.Forms
{
    public partial class EnvironmentDisplay : Form
    {
        public IRenderer Renderer
        {
            get => this.envRender?.Renderer;
            set => this.envRender.Renderer = value;
        }

        public EnvironmentDisplay()
        {
            InitializeComponent();

            this.refreshPanel.Interval = 1000 / 120;
            this.refreshPanel.Tick += RefreshPanel;
        }

        private void RefreshPanel(object sender, System.EventArgs e)
        {
            this.envRender?.Refresh();
        }
    }
}
