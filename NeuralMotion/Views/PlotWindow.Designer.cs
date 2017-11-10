﻿using OxyPlot.WindowsForms;

namespace NeuralMotion.Views
{
    partial class PlotWindow
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
            this.slowTimer = new System.Windows.Forms.Timer(this.components);
            this.fastTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // PlotWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(426, 386);
            this.Name = "PlotWindow";
            this.Text = "Plot";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer slowTimer;
        private System.Windows.Forms.Timer fastTimer;
    }
}