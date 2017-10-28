﻿using NeuralMotion.Views;

namespace NeuralMotion
{
    partial class Main
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
            this.bwEngine = new System.ComponentModel.BackgroundWorker();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.bwBrains = new System.ComponentModel.BackgroundWorker();
            this.uiArena = new NeuralMotion.Views.BallDisplay();
            this.uiFitnessPlot = new NeuralMotion.Views.FitnessPlot();
            this.SuspendLayout();
            // 
            // refreshTimer
            // 
            this.refreshTimer.Interval = 10;
            this.refreshTimer.Tick += new System.EventHandler(this.OnRefreshTimer);
            // 
            // uiArena
            // 
            this.uiArena.Arena = null;
            this.uiArena.BackColor = System.Drawing.Color.Black;
            this.uiArena.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiArena.Location = new System.Drawing.Point(0, 0);
            this.uiArena.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.uiArena.Name = "uiArena";
            this.uiArena.ShowKicks = true;
            this.uiArena.ShowPosition = true;
            this.uiArena.ShowPreviewFlag = false;
            this.uiArena.ShowSpeed = true;
            this.uiArena.Size = new System.Drawing.Size(621, 377);
            this.uiArena.TabIndex = 0;
            // 
            // uiFitnessPlot
            // 
            this.uiFitnessPlot.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.uiFitnessPlot.Location = new System.Drawing.Point(0, 377);
            this.uiFitnessPlot.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.uiFitnessPlot.Name = "uiFitnessPlot";
            this.uiFitnessPlot.PanCursor = System.Windows.Forms.Cursors.Hand;
            this.uiFitnessPlot.Size = new System.Drawing.Size(621, 178);
            this.uiFitnessPlot.TabIndex = 2;
            this.uiFitnessPlot.Text = "Fitness";
            this.uiFitnessPlot.ZoomHorizontalCursor = System.Windows.Forms.Cursors.SizeWE;
            this.uiFitnessPlot.ZoomRectangleCursor = System.Windows.Forms.Cursors.SizeNWSE;
            this.uiFitnessPlot.ZoomVerticalCursor = System.Windows.Forms.Cursors.SizeNS;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(621, 555);
            this.Controls.Add(this.uiArena);
            this.Controls.Add(this.uiFitnessPlot);
            this.DoubleBuffered = true;
            this.Location = new System.Drawing.Point(20, 50);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Main";
            this.ResumeLayout(false);

        }

        #endregion

        private System.ComponentModel.BackgroundWorker bwEngine;
        private System.Windows.Forms.Timer refreshTimer;
        private System.ComponentModel.BackgroundWorker bwBrains;
        private BallDisplay uiArena;
        private FitnessPlot uiFitnessPlot;
    }
}

