namespace ReverseRaffler
{
    partial class frmNumberBoard
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmNumberBoard));
            this.panelTickerHolder = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTickerHolder
            //
            this.panelTickerHolder.BackColor = System.Drawing.Color.White;
            this.panelTickerHolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTickerHolder.Location = new System.Drawing.Point(0, 0);
            this.panelTickerHolder.Name = "panelTickerHolder";
            this.panelTickerHolder.Size = new System.Drawing.Size(774, 602);
            this.panelTickerHolder.TabIndex = 0;
            // 
            // frmNumberBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(774, 602);
            this.Controls.Add(this.panelTickerHolder);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmNumberBoard";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Raffle Tickets";
            this.ThemeName = "Office2010Black";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTickerHolder;



    }
}