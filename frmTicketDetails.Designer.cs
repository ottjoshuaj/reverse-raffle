using System.Drawing;
using Telerik.WinControls.UI;

namespace ReverseRaffler
{
    partial class frmTicketDetails
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
            this.lblTicketDisplay = new System.Windows.Forms.Label();
            this.lblNamesOnTicket = new Telerik.WinControls.UI.RadLabel();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.lblNamesOnTicket)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTicketDisplay
            // 
            this.lblTicketDisplay.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTicketDisplay.Font = new System.Drawing.Font("Arial", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTicketDisplay.Location = new System.Drawing.Point(0, 0);
            this.lblTicketDisplay.Name = "lblTicketDisplay";
            this.lblTicketDisplay.Size = new System.Drawing.Size(709, 75);
            this.lblTicketDisplay.TabIndex = 2;
            this.lblTicketDisplay.Text = "Ticket # 300";
            this.lblTicketDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNamesOnTicket
            // 
            this.lblNamesOnTicket.AutoSize = false;
            this.lblNamesOnTicket.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNamesOnTicket.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNamesOnTicket.Location = new System.Drawing.Point(0, 107);
            this.lblNamesOnTicket.Name = "lblNamesOnTicket";
            this.lblNamesOnTicket.Padding = new System.Windows.Forms.Padding(0, 30, 0, 0);
            this.lblNamesOnTicket.Size = new System.Drawing.Size(709, 345);
            this.lblNamesOnTicket.TabIndex = 3;
            this.lblNamesOnTicket.Text = "<html><p>NAMES ON TICKET</p></html>";
            this.lblNamesOnTicket.TextAlignment = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(709, 32);
            this.label1.TabIndex = 1;
            this.label1.Text = "Ticket Holders:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // frmTicketDetails
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(709, 452);
            this.Controls.Add(this.lblNamesOnTicket);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblTicketDisplay);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "frmTicketDetails";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "";
            this.ThemeName = "Office2010Black";
            ((System.ComponentModel.ISupportInitialize)(this.lblNamesOnTicket)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblTicketDisplay;
        private RadLabel lblNamesOnTicket;
        private System.Windows.Forms.Label label1;
    }
}