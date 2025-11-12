using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using Microsoft.Owin.Hosting;
using ReverseRaffler.Api.Controllers;
using ReverseRaffler.Data;

using Telerik.WinControls;
using Telerik.WinControls.UI;


namespace ReverseRaffler
{
    public partial class frmConsole : RadForm
    {
        #region Private Objects

        IDisposable webServer;
        private frmNumberBoard numberBoard;
        private RevRaffleModelEntities dbModel;
        private int ticketsRemoved = 0;
        private List<TicketHolder> RaffleTickets { get; set; }

        #endregion

        #region Constructor(s)

        public frmConsole()
        {
            InitializeComponent();

            Shown += frmConsole_Shown;
            FormClosing += frmConsole_FormClosing;

            dbModel = new RevRaffleModelEntities();
            numberBoard = new frmNumberBoard();

            gridTicketHolders.DefaultValuesNeeded += gridTicketHolders_DefaultValuesNeeded;
            gridTicketHolders.UserAddedRow += gridTicketHolders_UserAddedRow;
            gridTicketHolders.UserDeletingRow += gridTicketHolders_UserDeletingRow;
            gridTicketHolders.CellValueChanged += gridTicketHolders_CellValueChanged;


            rsTicketNumber.KeyDown += RsTicketNumber_KeyDown;

            //Set global themes
            RadMessageBox.SetThemeName("CrystalDark");
        }

        private void RsTicketNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnMarkTicketAsNotGood_Click(null, null);
        }

        #endregion

        #region Form Events

        private void frmConsole_Load(object sender, EventArgs e)
        {

        }

        void frmConsole_Shown(object sender, EventArgs e)
        {
            BindGrid();

            // Load winner configuration from settings
            rsTotalWinningTickets.Value = Properties.Settings.Default.LastWhatTicketsAreWinners;
            rsWinnersEveryNumTickets.Value = Properties.Settings.Default.WinnersEveryNumTickets;
        }

        void frmConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();

            // Stop API server if running
            StopApiServer();
        }


        #endregion

        #region Menu Events

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            var newFrmAbout = new frmAbout();
            newFrmAbout.ShowDialog(this);
            newFrmAbout.Dispose();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new frmSettings(numberBoard);
            settingsForm.ShowDialog(this);

            // Reload settings after dialog closes
            rsTotalWinningTickets.Value = Properties.Settings.Default.LastWhatTicketsAreWinners;
            rsWinnersEveryNumTickets.Value = Properties.Settings.Default.WinnersEveryNumTickets;
        }

        private void mnuImportCSV_Click(object sender, EventArgs e)
        {
            var rosterFile = new OpenFileDialog
            {
                Multiselect = false,
                Title = @"Select An Import File (Will overwrite database)",
                Filter = @"All Supported Files|*.csv;*.xls;*.xlsx|CSV Files (*.csv)|*.csv|Excel 97-2003 (*.xls)|*.xls|Excel 2007+ (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
            };

            if (rosterFile.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var newFrmImporter = new frmImport(rosterFile.FileName);
                var result = newFrmImporter.ShowDialog(this);
                newFrmImporter.Dispose();

                //Bind grid with changes
                ClearGrid();
                BindGrid();

                RadMessageBox.Show("Your file has been imported.", "Import Success", MessageBoxButtons.OK, RadMessageIcon.Info);
            }
            catch (Exception ex)
            {
                RadMessageBox.Show($"There was an error opening your import file.\r\n\r\n" +
                    $"For CSV files: Make sure it is comma delimited and that no NAMES have commas in them.\r\n" +
                    $"Format: ticketnumber,name,name,name (max of 3 names)\r\n\r\n" +
                    $"Ticket number is required but names are optional.\r\n\r\n" +
                    $"Error: {ex.Message}",
                    "ERROR ON IMPORT", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region Grid Events

        void gridTicketHolders_DefaultValuesNeeded(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells["colTicketNumber"].Value = GetNextTicketNumber();
        }

        int GetNextTicketNumber()
        {
            if (RaffleTickets == null || RaffleTickets.Count == 0)
                return 1;

            return RaffleTickets.Max(x => x.TicketNumber) + 1;
        }

        void gridTicketHolders_UserAddedRow(object sender, GridViewRowEventArgs e)
        {
            var tickNum = Convert.ToInt32(e.Row.Cells["colTicketNumber"].Value);
            var holder1 = e.Row.Cells["colTicketHolder1"].Value?.ToString();
            var holder2 = e.Row.Cells["colTicketHolder2"].Value?.ToString();
            var holder3 = e.Row.Cells["colTicketHolder3"].Value?.ToString();

            dbModel.TicketHolders.Add(new TicketHolder { TicketNumber = tickNum, TicketHolder1 = holder1, TicketHolder2 = holder2, TicketHolder3 = holder3 });
            dbModel.SaveChanges();
        }

        void gridTicketHolders_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            var tickNum = Convert.ToInt32(e.Row.Cells["colTicketNumber"].Value);
            var holder1 = e.Row.Cells["colTicketHolder1"].Value?.ToString();
            var holder2 = e.Row.Cells["colTicketHolder2"].Value?.ToString();
            var holder3 = e.Row.Cells["colTicketHolder3"].Value?.ToString();

            var existingTicket = dbModel.TicketHolders.FirstOrDefault(x => x.TicketNumber == tickNum);

            if (existingTicket == null)
            {
                dbModel.TicketHolders.Add(new TicketHolder
                {
                    TicketNumber = tickNum,
                    TicketHolder1 = holder1,
                    TicketHolder2 = holder2,
                    TicketHolder3 = holder3
                });
            }
            else
            {
                existingTicket.TicketHolder1 = holder1;
                existingTicket.TicketHolder2 = holder2;
                existingTicket.TicketHolder3 = holder3;
            }

            dbModel.SaveChanges();
        }

        void gridTicketHolders_UserDeletingRow(object sender, GridViewRowCancelEventArgs e)
        {
            try
            {
                var ticketNumber = Convert.ToInt32(e.Rows[0].Cells["colTicketNumber"].Value);
                var ticket = dbModel.TicketHolders.FirstOrDefault(x => x.TicketNumber == ticketNumber);
                if (ticket != null)
                {
                    dbModel.TicketHolders.Remove(ticket);
                    dbModel.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                RadMessageBox.Show($"Error deleting ticket: {ex.Message}", "Error", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        void CheckDataForTicketLayout(int numOfTickets)
        {
            for (int i = 1; i < (numOfTickets + 1); i++)
            {
                if (dbModel.TicketHolders.Any(x => x.TicketNumber == i)) continue;

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

        void BindGrid()
        {
            RaffleTickets = dbModel.TicketHolders.ToList();

            gridTicketHolders.DataSource = RaffleTickets;
            gridTicketHolders.Refresh();
        }

        void ClearGrid()
        {
            gridTicketHolders.Rows.Clear();
        }

        #endregion

        #region Raffle Manager

        private void btnBeginRaffle_Click(object sender, EventArgs e)
        {
            if (numberBoard == null)
                numberBoard = new frmNumberBoard();

            //Set available winners
            numberBoard.AvailableWinners = Convert.ToInt32(rsTotalWinningTickets.Value);

            // Show the form first so the canvas has proper dimensions
            numberBoard.Show();

            //Add Raffle Tickets
            foreach (var tickItem in RaffleTickets.OrderBy(x => x.TicketNumber))
            {
                if(tickItem.MarkedAsNotWinner == true)
                {
                    ticketsRemoved++;
                }

                numberBoard.AddTicketRaffle(tickItem);
            }

            //Enable Buttons
            btnBeginRaffle.Enabled = false;
            rsTotalWinningTickets.Enabled = false;
            rsWinnersEveryNumTickets.Enabled = false;
            btnEndRaffle.Enabled = true;
            btnMarkTicketAsNotGood.Enabled = true;
            rsTicketNumber.Enabled = true;

            //Set Grid Options
            gridTicketHolders.MasterTemplate.AllowAddNewRow = false;
            gridTicketHolders.MasterTemplate.AllowEditRow = false;

            //Set Stats on Stat Group
            lblTicketsNotDrawn.Text = numberBoard.TotalTicketsNotDrawn.ToString(CultureInfo.InvariantCulture);
            lblTicketsThatLost.Text = numberBoard.TotalLosingTickets.ToString(CultureInfo.InvariantCulture);
            lblTotalTicketsInRaffle.Text = numberBoard.TotalTickets.ToString(CultureInfo.InvariantCulture);

            //Set App Status
            lblAppStatus.Text = @"Raffle is running...";

            //Update history control states
            UpdateHistoryControlStates();

            //Start API Server automatically
            StartApiServer();
        }

        private void btnEndRaffle_Click(object sender, EventArgs e)
        {
            if (numberBoard != null)
            {
                btnBeginRaffle.Enabled = true;
                rsTotalWinningTickets.Enabled = true;
                rsWinnersEveryNumTickets.Enabled = true;
                btnEndRaffle.Enabled = false;
                btnMarkTicketAsNotGood.Enabled = false;
                rsTicketNumber.Enabled = false;

                //Set Grid Options
                gridTicketHolders.MasterTemplate.AllowAddNewRow = true;
                gridTicketHolders.MasterTemplate.AllowEditRow = true;

                //Set Stats on Stat Group
                lblTicketsNotDrawn.Text = @"0";
                lblTicketsThatLost.Text = @"0";
                lblTotalTicketsInRaffle.Text = @"0";

                //Set App Status
                lblAppStatus.Text = @"Raffle has ended...";

                numberBoard.Close();
                numberBoard = null;

                ticketsRemoved = 0;

                //Update history control states
                UpdateHistoryControlStates();

                //Stop API Server automatically
                StopApiServer();
            }
        }

        private void btnMarkTicketAsNotGood_Click(object sender, EventArgs e)
        {
            HandleTicketSelectionAction((int)rsTicketNumber.Value);
        }


        void HandleTicketSelectionAction(int ticketNumber)
        {
            //Verify ticket number is even in the list!
            if (!numberBoard.VerifyTicketNumberExists(ticketNumber)) return;

            //Verify ticket has NOT been selected already
            if (numberBoard.HasTicketBeenSelectedAlready(ticketNumber))
            {
                //Lets just show the ticket again for INFO only reasons
                numberBoard.ShowTicketInfoDialog(ticketNumber);
                return;
            }

            ticketsRemoved++;

            //Clear typed number
            rsTicketNumber.SpinElement.TextBoxControl.Text = string.Empty;

            //Handle winner every (x) ticket count
            if (rsWinnersEveryNumTickets.Value == ticketsRemoved && rsWinnersEveryNumTickets.Value > 0)
            {
                //Mark ticket as losing, but DO NOT show the loser screen
                numberBoard.MarkTicketAsLosing(ticketNumber, true);

                //Show as a winner screen since their ticket has won
                numberBoard.ShowWinnerScreen(ticketNumber);

                //Show winners
                ticketsRemoved = 0;
            }
            else
                numberBoard.MarkTicketAsLosing(ticketNumber);


            //Set Stats on Stat Group
            lblTicketsNotDrawn.Text = numberBoard.TotalTicketsNotDrawn.ToString(CultureInfo.InvariantCulture);
            lblTicketsThatLost.Text = numberBoard.TotalLosingTickets.ToString(CultureInfo.InvariantCulture);
            lblTotalTicketsInRaffle.Text = numberBoard.TotalTickets.ToString(CultureInfo.InvariantCulture);

            //Set app status
            lblAppTicketStatus.Text = $"Ticket #: {ticketNumber} was drawn..";

            //Update history controls
            UpdateHistoryControlStates();
        }

        private void btnShowRecentAnnouncement_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.HasHistory) return;

            numberBoard.ShowMostRecentAnnouncement();
            UpdateHistoryControlStates();
        }

        private void btnCloseOverlay_Click(object sender, EventArgs e)
        {
            if (numberBoard == null) return;

            numberBoard.CloseCurrentOverlay();
        }

        private void btnPreviousAnnouncement_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.HasHistory) return;

            numberBoard.ShowPreviousAnnouncement();
            UpdateHistoryControlStates();
        }

        private void btnNextAnnouncement_Click(object sender, EventArgs e)
        {
            if (numberBoard == null || !numberBoard.HasHistory) return;

            numberBoard.ShowNextAnnouncement();
            UpdateHistoryControlStates();
        }

        private void UpdateHistoryControlStates()
        {
            if (numberBoard == null || !numberBoard.HasHistory)
            {
                btnShowRecentAnnouncement.Enabled = false;
                btnCloseOverlay.Enabled = false;
                btnPreviousAnnouncement.Enabled = false;
                btnNextAnnouncement.Enabled = false;
                lblHistoryPosition.Text = "";
            }
            else
            {
                btnShowRecentAnnouncement.Enabled = true;
                btnCloseOverlay.Enabled = true;
                btnPreviousAnnouncement.Enabled = true;
                btnNextAnnouncement.Enabled = true;
                lblHistoryPosition.Text = numberBoard.GetHistoryPosition();
            }
        }

        #endregion

        #region API Server

        Timer getApiTicketNumber;
        private const int API_PORT = 9150;

        private void StartApiServer()
        {
            try
            {
                string baseAddress = $"http://+:{API_PORT}/";
                webServer = WebApp.Start<ApiConfig>(url: baseAddress);

                Api.Controllers.TicketController.LogActivity = LogApiActivity;

                var localIPs = GetLocalIPAddresses();

                LogApiActivity($"API Server Started on port {API_PORT}");
                foreach (var ip in localIPs)
                {
                    LogApiActivity($"  Available at: http://{ip}:{API_PORT}");
                }

                lblApiStatus.Text = $"API: Running on port {API_PORT}";

                getApiTicketNumber = new Timer();
                getApiTicketNumber.Interval = 2000;
                getApiTicketNumber.Tick += GetApiTicketNumber_Tick;
                getApiTicketNumber.Start();
            }
            catch (Exception ex)
            {
                LogApiActivity($"ERROR: Failed to start API server - {ex.Message}");
                lblApiStatus.Text = "API: Failed to start";
                RadMessageBox.Show($"Failed to start API server on port {API_PORT}.\r\n\r\n" +
                    $"Error: {ex.Message}\r\n\r\n" +
                    $"SOLUTION 1: Run this application as Administrator\r\n\r\n" +
                    $"SOLUTION 2: Run this command once as Administrator:\r\n" +
                    $"netsh http add urlacl url=http://+:{API_PORT}/ user=Everyone",
                    "API Error - Admin Rights Required", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void StopApiServer()
        {
            if (webServer != null)
            {
                LogApiActivity("API Server Stopped");
                lblApiStatus.Text = "API: Stopped";

                Api.Controllers.TicketController.LogActivity = null;

                webServer?.Dispose();
                webServer = null;
                getApiTicketNumber?.Stop();
                getApiTicketNumber?.Dispose();
                getApiTicketNumber = null;
            }
        }

        private void LogApiActivity(string message)
        {
            if (txtApiLogs.InvokeRequired)
            {
                txtApiLogs.Invoke(new Action(() => LogApiActivity(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            txtApiLogs.AppendText($"[{timestamp}] {message}\r\n");
        }

        private List<string> GetLocalIPAddresses()
        {
            var ipAddresses = new List<string>();

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddresses.Add(ip.ToString());
                    }
                }
            }
            catch
            {
                ipAddresses.Add("127.0.0.1");
            }

            return ipAddresses;
        }

        private void GetApiTicketNumber_Tick(object sender, EventArgs e)
        {
            var ticketNumber = TicketController.currentTicketNumber;
            if(ticketNumber > 0)
            {
                TicketController.currentTicketNumber = 0;
                HandleTicketSelectionAction(ticketNumber);
            }
        }

        #endregion
    }
}
