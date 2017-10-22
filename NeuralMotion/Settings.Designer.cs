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
            this.uiDontPreviewBest = new System.Windows.Forms.CheckBox();
            this.uiEnableNetExpansion = new System.Windows.Forms.CheckBox();
            this.uiDontShowSim = new System.Windows.Forms.CheckBox();
            this.uiShowBallStatusText = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // uiToggleSpeed
            // 
            this.uiToggleSpeed.Location = new System.Drawing.Point(18, 16);
            this.uiToggleSpeed.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.uiWriteLog.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.uiWriteLog.Name = "uiWriteLog";
            this.uiWriteLog.Size = new System.Drawing.Size(105, 21);
            this.uiWriteLog.TabIndex = 1;
            this.uiWriteLog.Text = "Write status";
            this.uiWriteLog.UseVisualStyleBackColor = true;
            // 
            // uiDontPreviewBest
            // 
            this.uiDontPreviewBest.AutoSize = true;
            this.uiDontPreviewBest.Location = new System.Drawing.Point(18, 77);
            this.uiDontPreviewBest.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.uiDontPreviewBest.Name = "uiDontPreviewBest";
            this.uiDontPreviewBest.Size = new System.Drawing.Size(146, 21);
            this.uiDontPreviewBest.TabIndex = 2;
            this.uiDontPreviewBest.Text = "Don\'t preview best";
            this.uiDontPreviewBest.UseVisualStyleBackColor = true;
            // 
            // uiEnableNetExpansion
            // 
            this.uiEnableNetExpansion.AutoSize = true;
            this.uiEnableNetExpansion.Location = new System.Drawing.Point(18, 99);
            this.uiEnableNetExpansion.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.uiEnableNetExpansion.Name = "uiEnableNetExpansion";
            this.uiEnableNetExpansion.Size = new System.Drawing.Size(166, 21);
            this.uiEnableNetExpansion.TabIndex = 3;
            this.uiEnableNetExpansion.Text = "Enable net expansion";
            this.uiEnableNetExpansion.UseVisualStyleBackColor = true;
            // 
            // uiDontShowSim
            // 
            this.uiDontShowSim.AutoSize = true;
            this.uiDontShowSim.Location = new System.Drawing.Point(18, 121);
            this.uiDontShowSim.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.uiDontShowSim.Name = "uiDontShowSim";
            this.uiDontShowSim.Size = new System.Drawing.Size(124, 21);
            this.uiDontShowSim.TabIndex = 4;
            this.uiDontShowSim.Text = "Don\'t show sim";
            this.uiDontShowSim.UseVisualStyleBackColor = true;
            // 
            // showBallStatusText
            // 
            this.uiShowBallStatusText.AutoSize = true;
            this.uiShowBallStatusText.Location = new System.Drawing.Point(18, 144);
            this.uiShowBallStatusText.Name = "showBallStatusText";
            this.uiShowBallStatusText.Size = new System.Drawing.Size(132, 21);
            this.uiShowBallStatusText.TabIndex = 5;
            this.uiShowBallStatusText.Text = "Show ball status";
            this.uiShowBallStatusText.UseVisualStyleBackColor = true;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(266, 241);
            this.Controls.Add(this.uiShowBallStatusText);
            this.Controls.Add(this.uiDontShowSim);
            this.Controls.Add(this.uiEnableNetExpansion);
            this.Controls.Add(this.uiDontPreviewBest);
            this.Controls.Add(this.uiWriteLog);
            this.Controls.Add(this.uiToggleSpeed);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Location = new System.Drawing.Point(550, 50);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button uiToggleSpeed;
        private System.Windows.Forms.CheckBox uiWriteLog;
        private System.Windows.Forms.CheckBox uiDontPreviewBest;
        private System.Windows.Forms.CheckBox uiEnableNetExpansion;
        private System.Windows.Forms.CheckBox uiDontShowSim;
        private System.Windows.Forms.CheckBox uiShowBallStatusText;
    }
}