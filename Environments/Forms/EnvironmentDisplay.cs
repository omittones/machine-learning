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
        }
    }
}
