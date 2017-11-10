namespace NeuralMotion
{
    partial class Settings
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
            this.uiToggleSpeed = new System.Windows.Forms.Button();
            this.uiWriteLog = new System.Windows.Forms.CheckBox();
            this.uiDontShowSim = new System.Windows.Forms.CheckBox();
            this.uiShowDiagnostics = new System.Windows.Forms.CheckBox();
            this.uiIncreaseLearningRate = new System.Windows.Forms.Button();
            this.uiDecreaseLearningRate = new System.Windows.Forms.Button();
            this.uiToggleExploration = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // uiToggleSpeed
            // 
            this.uiToggleSpeed.Location = new System.Drawing.Point(18, 16);
            this.uiToggleSpeed.Margin = new System.Windows.Forms.Padding(4);
            this.uiToggleSpeed.Name = "uiToggleSpeed";
            this.uiToggleSpeed.Size = new System.Drawing.Size(100, 28);
            this.uiToggleSpeed.TabIndex = 0;
            this.uiToggleSpeed.Text = "Slow";
            this.uiToggleSpeed.UseVisualStyleBackColor = true;
            // 
            // uiWriteLog
            // 
            this.uiWriteLog.AutoSize = true;
            this.uiWriteLog.Location = new System.Drawing.Point(18, 53);
            this.uiWriteLog.Margin = new System.Windows.Forms.Padding(4);
            this.uiWriteLog.Name = "uiWriteLog";
            this.uiWriteLog.Size = new System.Drawing.Size(105, 21);
            this.uiWriteLog.TabIndex = 1;
            this.uiWriteLog.Text = "Write status";
            this.uiWriteLog.UseVisualStyleBackColor = true;
            // 
            // uiDontShowSim
            // 
            this.uiDontShowSim.AutoSize = true;
            this.uiDontShowSim.Location = new System.Drawing.Point(18, 75);
            this.uiDontShowSim.Margin = new System.Windows.Forms.Padding(2);
            this.uiDontShowSim.Name = "uiDontShowSim";
            this.uiDontShowSim.Size = new System.Drawing.Size(124, 21);
            this.uiDontShowSim.TabIndex = 4;
            this.uiDontShowSim.Text = "Don\'t show sim";
            this.uiDontShowSim.UseVisualStyleBackColor = true;
            // 
            // uiShowDiagnostics
            // 
            this.uiShowDiagnostics.AutoSize = true;
            this.uiShowDiagnostics.Location = new System.Drawing.Point(18, 98);
            this.uiShowDiagnostics.Name = "uiShowDiagnostics";
            this.uiShowDiagnostics.Size = new System.Drawing.Size(139, 21);
            this.uiShowDiagnostics.TabIndex = 5;
            this.uiShowDiagnostics.Text = "Show diagnostics";
            this.uiShowDiagnostics.UseVisualStyleBackColor = true;
            // 
            // uiIncreaseLearningRate
            // 
            this.uiIncreaseLearningRate.Location = new System.Drawing.Point(179, 131);
            this.uiIncreaseLearningRate.Name = "uiIncreaseLearningRate";
            this.uiIncreaseLearningRate.Size = new System.Drawing.Size(75, 29);
            this.uiIncreaseLearningRate.TabIndex = 6;
            this.uiIncreaseLearningRate.Text = "Inc LR";
            this.uiIncreaseLearningRate.UseVisualStyleBackColor = true;
            // 
            // uiDecreaseLearningRate
            // 
            this.uiDecreaseLearningRate.Location = new System.Drawing.Point(98, 131);
            this.uiDecreaseLearningRate.Name = "uiDecreaseLearningRate";
            this.uiDecreaseLearningRate.Size = new System.Drawing.Size(75, 29);
            this.uiDecreaseLearningRate.TabIndex = 7;
            this.uiDecreaseLearningRate.Text = "Dec LR";
            this.uiDecreaseLearningRate.UseVisualStyleBackColor = true;
            // 
            // uiToggleExploration
            // 
            this.uiToggleExploration.Location = new System.Drawing.Point(98, 166);
            this.uiToggleExploration.Name = "uiToggleExploration";
            this.uiToggleExploration.Size = new System.Drawing.Size(156, 30);
            this.uiToggleExploration.TabIndex = 8;
            this.uiToggleExploration.Text = "Exploration == On";
            this.uiToggleExploration.UseVisualStyleBackColor = true;
            this.uiToggleExploration.Click += new System.EventHandler(this.ToggleExploration);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 239);
            this.Controls.Add(this.uiToggleExploration);
            this.Controls.Add(this.uiDecreaseLearningRate);
            this.Controls.Add(this.uiIncreaseLearningRate);
            this.Controls.Add(this.uiShowDiagnostics);
            this.Controls.Add(this.uiDontShowSim);
            this.Controls.Add(this.uiWriteLog);
            this.Controls.Add(this.uiToggleSpeed);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Location = new System.Drawing.Point(550, 50);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button uiToggleSpeed;
        private System.Windows.Forms.CheckBox uiWriteLog;
        private System.Windows.Forms.CheckBox uiDontShowSim;
        private System.Windows.Forms.CheckBox uiShowDiagnostics;
        private System.Windows.Forms.Button uiIncreaseLearningRate;
        private System.Windows.Forms.Button uiDecreaseLearningRate;
        private System.Windows.Forms.Button uiToggleExploration;
    }
}