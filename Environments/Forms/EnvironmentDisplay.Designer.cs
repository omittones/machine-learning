namespace Environments.Forms
{
    partial class EnvironmentDisplay
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.envRender = new Environments.Forms.Render();
            this.refreshPanel = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // envRender
            // 
            this.envRender.BackColor = System.Drawing.Color.Black;
            this.envRender.Dock = System.Windows.Forms.DockStyle.Fill;
            this.envRender.Location = new System.Drawing.Point(0, 0);
            this.envRender.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.envRender.Name = "envRender";
            this.envRender.Renderer = null;
            this.envRender.Size = new System.Drawing.Size(628, 559);
            this.envRender.TabIndex = 0;
            // 
            // refreshPanel
            // 
            this.refreshPanel.Enabled = true;
            this.refreshPanel.Interval = 16;
            // 
            // EnvironmentDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(628, 559);
            this.Controls.Add(this.envRender);
            this.Name = "EnvironmentDisplay";
            this.Text = "EnvironmentDisplay";
            this.ResumeLayout(false);

        }

        #endregion

        private Render envRender;
        private System.Windows.Forms.Timer refreshPanel;
    }
}