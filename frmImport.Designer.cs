namespace ReverseRaffler
{
    partial class frmImport
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
            this.progressImportStatus = new Telerik.WinControls.UI.RadProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.progressImportStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // progressImportStatus
            // 
            this.progressImportStatus.Location = new System.Drawing.Point(12, 22);
            this.progressImportStatus.Name = "progressImportStatus";
            this.progressImportStatus.ShowItemToolTips = false;
            this.progressImportStatus.ShowProgressIndicators = true;
            this.progressImportStatus.Size = new System.Drawing.Size(386, 32);
            this.progressImportStatus.TabIndex = 0;
            this.progressImportStatus.Text = "Importing File...";
            this.progressImportStatus.ThemeName = "CrystalDark";
            // 
            // frmImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(410, 77);
            this.Controls.Add(this.progressImportStatus);
            this.Name = "frmImport";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import Raffle File";
            this.ThemeName = "CrystalDark";
            this.Load += new System.EventHandler(this.frmImport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.progressImportStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.WinControls.UI.RadProgressBar progressImportStatus;
    }
}