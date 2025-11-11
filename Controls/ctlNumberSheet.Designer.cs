using System.Drawing;

namespace ReverseRaffler.Controls
{
    partial class ctlNumberSheet
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ctlNumberSheet));
            this.panelNumberSheet = new System.Windows.Forms.Panel();
            this.lblNumberDisplay = new System.Windows.Forms.Label();
            this.lblTicketNotWinner = new System.Windows.Forms.Label();
            this.panelNumberSheet.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelNumberSheet
            // 
            this.panelNumberSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelNumberSheet.BackColor = System.Drawing.Color.White;
            this.panelNumberSheet.Controls.Add(this.lblNumberDisplay);
            this.panelNumberSheet.Controls.Add(this.lblTicketNotWinner);
            this.panelNumberSheet.Location = new System.Drawing.Point(3, 3);
            this.panelNumberSheet.Name = "panelNumberSheet";
            this.panelNumberSheet.Size = new System.Drawing.Size(60, 36);
            this.panelNumberSheet.TabIndex = 0;
            // 
            // lblNumberDisplay
            // 
            this.lblNumberDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNumberDisplay.BackColor = System.Drawing.Color.Transparent;
            this.lblNumberDisplay.Font = new System.Drawing.Font("Arial", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNumberDisplay.ForeColor = System.Drawing.Color.Black;
            this.lblNumberDisplay.Image = ((System.Drawing.Image)(resources.GetObject("lblNumberDisplay.Image")));
            this.lblNumberDisplay.Location = new System.Drawing.Point(-3, -12);
            this.lblNumberDisplay.Name = "lblNumberDisplay";
            this.lblNumberDisplay.Size = new System.Drawing.Size(66, 62);
            this.lblNumberDisplay.TabIndex = 0;
            this.lblNumberDisplay.Text = "300";
            this.lblNumberDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTicketNotWinner
            // 
            this.lblTicketNotWinner.BackColor = System.Drawing.Color.Transparent;
            this.lblTicketNotWinner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTicketNotWinner.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTicketNotWinner.ForeColor = System.Drawing.Color.Red;
            this.lblTicketNotWinner.Location = new System.Drawing.Point(0, 0);
            this.lblTicketNotWinner.Name = "lblTicketNotWinner";
            this.lblTicketNotWinner.Size = new System.Drawing.Size(60, 36);
            this.lblTicketNotWinner.TabIndex = 1;
            this.lblTicketNotWinner.Text = "X";
            this.lblTicketNotWinner.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ctlNumberSheet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.panelNumberSheet);
            this.DoubleBuffered = true;
            this.Name = "ctlNumberSheet";
            this.Size = new System.Drawing.Size(66, 42);
            this.Load += new System.EventHandler(this.ctlNumberSheet_Load);
            this.panelNumberSheet.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelNumberSheet;
        private System.Windows.Forms.Label lblNumberDisplay;
        private System.Windows.Forms.Label lblTicketNotWinner;
    }
}
