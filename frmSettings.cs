using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using Microsoft.Owin.Hosting;
using ReverseRaffler.Api;
using System.Data.Entity.Migrations;
using ReverseRaffler.Data;

namespace ReverseRaffler
{
    public partial class frmSettings : RadForm
    {
        private frmNumberBoard numberBoard;

        public frmSettings(frmNumberBoard board)
        {
            InitializeComponent();
            numberBoard = board;
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Load winner configuration
            rsTotalWinningTickets.Value = Properties.Settings.Default.LastWhatTicketsAreWinners;
            rsWinnersEveryNumTickets.Value = Properties.Settings.Default.WinnersEveryNumTickets;

            // Load auto-close settings
            chkEnableAutoCloseLosingTicketAlert.Checked = Properties.Settings.Default.EnableAutoCloseLosingTicketWindow == CheckState.Checked;
            rsAutoCloseLosingTicket.Value = Properties.Settings.Default.AutoCloseLosingTicketWindowAfter;

            radCheckBox1.Checked = Properties.Settings.Default.EnableAutoCloseWinningTicketWindow == CheckState.Checked;
            radSpinEditor2.Value = Properties.Settings.Default.AutoCloseWinningTicketWindowAfter;

            // Load display settings
            rsTicketWidth.Value = Properties.Settings.Default.ticketWidth > 0 ? Properties.Settings.Default.ticketWidth : 120;
            radSpinEditor1.Value = Properties.Settings.Default.ticketHeightt > 0 ? Properties.Settings.Default.ticketHeightt : 60;
        }

        private void btnSaveSettingsOptions_Click(object sender, EventArgs e)
        {
            try
            {
                // Save winner configuration
                Properties.Settings.Default.LastWhatTicketsAreWinners = Convert.ToInt64(rsTotalWinningTickets.Value);
                Properties.Settings.Default.WinnersEveryNumTickets = Convert.ToInt64(rsWinnersEveryNumTickets.Value);

                // Save auto-close settings
                Properties.Settings.Default.EnableAutoCloseLosingTicketWindow = chkEnableAutoCloseLosingTicketAlert.Checked ? CheckState.Checked : CheckState.Unchecked;
                Properties.Settings.Default.AutoCloseLosingTicketWindowAfter = rsAutoCloseLosingTicket.Value;

                Properties.Settings.Default.EnableAutoCloseWinningTicketWindow = radCheckBox1.Checked ? CheckState.Checked : CheckState.Unchecked;
                Properties.Settings.Default.AutoCloseWinningTicketWindowAfter = radSpinEditor2.Value;

                // Save display settings
                Properties.Settings.Default.ticketWidth = Convert.ToInt32(rsTicketWidth.Value);
                Properties.Settings.Default.ticketHeightt = Convert.ToInt32(radSpinEditor1.Value);

                Properties.Settings.Default.Save();

                RadMessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, RadMessageIcon.Info);
            }
            catch (Exception ex)
            {
                RadMessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        #region Database Operations

        private void btnImportFile2DB_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select CSV File to Import"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var importForm = new frmImport(openFileDialog.FileName);
                importForm.ShowDialog(this);
            }
        }

        private void btnResetDatabase_Click(object sender, EventArgs e)
        {
            var result = RadMessageBox.Show(
                $"This will delete ALL existing tickets and create {rsResetDBWithTicketCount.Value} test tickets.\r\n\r\nAre you sure?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                RadMessageIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var dbModel = new RevRaffleModelEntities())
                    {
                        // Delete all existing tickets
                        foreach (var item in dbModel.TicketHolders.ToList())
                        {
                            dbModel.TicketHolders.Remove(item);
                        }
                        dbModel.SaveChanges();

                        // Create test tickets
                        int ticketCount = Convert.ToInt32(rsResetDBWithTicketCount.Value);
                        for (int i = 1; i <= ticketCount; i++)
                        {
                            dbModel.TicketHolders.Add(new TicketHolder
                            {
                                TicketNumber = i,
                                TicketHolder1 = "sample name",
                                TicketHolder2 = "sample name",
                                TicketHolder3 = "sample name"
                            });
                        }
                        dbModel.SaveChanges();
                    }

                    RadMessageBox.Show($"Database reset complete. Created {rsResetDBWithTicketCount.Value} test tickets.",
                        "Success", MessageBoxButtons.OK, RadMessageIcon.Info);
                }
                catch (Exception ex)
                {
                    RadMessageBox.Show($"Error resetting database: {ex.Message}",
                        "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            }
        }

        private void btnResetFlag_Click(object sender, EventArgs e)
        {
            var result = RadMessageBox.Show(
                "This will reset the 'MarkedAsNotWinner' flag for ALL tickets.\r\n\r\nAre you sure?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                RadMessageIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var dbModel = new RevRaffleModelEntities())
                    {
                        foreach (var ticket in dbModel.TicketHolders.ToList())
                        {
                            ticket.MarkedAsNotWinner = null;
                            dbModel.TicketHolders.AddOrUpdate(ticket);
                        }
                        dbModel.SaveChanges();
                    }

                    RadMessageBox.Show("Flags reset successfully. All tickets are now unmarked.",
                        "Success", MessageBoxButtons.OK, RadMessageIcon.Info);
                }
                catch (Exception ex)
                {
                    RadMessageBox.Show($"Error resetting flags: {ex.Message}",
                        "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            }
        }

        #endregion

        #region Demo Overlay Buttons

        private void btnDemoInfo_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.Visible)
            {
                RadMessageBox.Show("Please start the raffle first (click 'Begin Raffle' button)",
                    "Number Board Required", MessageBoxButtons.OK, RadMessageIcon.Info);
                return;
            }

            var sampleNames = new[] { "John Smith", "ABC Company", "XYZ Corporation" };
            numberBoard.ShowDemoInfo("42", sampleNames);
        }

        private void btnDemoLoser_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.Visible)
            {
                RadMessageBox.Show("Please start the raffle first (click 'Begin Raffle' button)",
                    "Number Board Required", MessageBoxButtons.OK, RadMessageIcon.Info);
                return;
            }

            var sampleNames = new[] { "Jane Doe", "Main Street Business", "Downtown LLC" };
            numberBoard.ShowDemoLoser("87", sampleNames);
        }

        private void btnDemoWinner_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.Visible)
            {
                RadMessageBox.Show("Please start the raffle first (click 'Begin Raffle' button)",
                    "Number Board Required", MessageBoxButtons.OK, RadMessageIcon.Info);
                return;
            }

            var sampleNames = new[] { "Bob Johnson", "Local Shop", "Community Center" };
            numberBoard.ShowDemoWinner("123", sampleNames);
        }

        private void btnDemoRaffleWinner_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.Visible)
            {
                RadMessageBox.Show("Please start the raffle first (click 'Begin Raffle' button)",
                    "Number Board Required", MessageBoxButtons.OK, RadMessageIcon.Info);
                return;
            }

            // Create long list of names for scrolling demo
            var sampleNames = new List<string>();
            var names = new[]
            {
                "John Smith", "ABC Company", "XYZ Corporation",
                "Jane Doe", "Main Street Business", "Downtown LLC",
                "Bob Johnson", "Local Shop", "Community Center",
                "Alice Williams", "Tech Solutions Inc", "Global Enterprises",
                "Charlie Brown", "Hardware Store", "Retail Group",
                "Diana Prince", "Wonder Consulting", "Justice League LLC",
                "Frank Castle", "Security Services", "Vigilante Corp",
                "Grace Hopper", "Computing Innovations", "Navy Research Lab",
                "Henry Ford", "Motor Company", "Assembly Line Industries",
                "Irene Adler", "Private Investigations", "Holmes & Watson Ltd",
                "Jack Ryan", "CIA Operations", "Analyst Division",
                "Karen Starr", "Power Industries", "Kryptonian Tech",
                "Larry Page", "Search Engines Inc", "Alphabet Corporation",
                "Marie Curie", "Radioactive Research", "Nobel Prize Foundation",
                "Neil Armstrong", "Apollo Missions", "NASA Space Center",
                "Olivia Pope", "Crisis Management", "OPA Consulting",
                "Peter Parker", "Daily Bugle", "Freelance Photography",
                "Quinn Fabray", "McKinley High", "Cheerios Squad",
                "Rachel Green", "Ralph Lauren", "Bloomingdales",
                "Steve Rogers", "Shield Operations", "Avengers Initiative",
                "Tony Stark", "Stark Industries", "Iron Man Tech",
                "Uma Thurman", "Kill Bill Productions", "Bride Enterprises",
                "Victor Von Doom", "Latveria Inc", "Doom Industries",
                "Wade Wilson", "Mercenary Services", "Deadpool Corp",
                "Xavier Institute", "School for Gifted", "Mutant Education",
                "Yoda Master", "Jedi Council", "Force Training Academy"
            };

            sampleNames.AddRange(names);

            numberBoard.ShowDemoRaffleWinner("5, 12, 18, 42, 87, 123", sampleNames);
        }

        #endregion

        private void frmSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Nothing to clean up
        }
    }
}
