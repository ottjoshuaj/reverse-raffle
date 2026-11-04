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
using ReverseRaffler.Services;
using System.Drawing;

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
        private PayoutSchedule payoutSchedule;

        // Draws made since Begin Raffle, most recent last - used by Undo
        private readonly Stack<DrawRecord> drawHistory = new Stack<DrawRecord>();

        private class DrawRecord
        {
            public int TicketNumber;
            public int DrawNumber;
            public DrawOutcome Outcome;
        }
        private List<TicketHolder> RaffleTickets { get; set; }

        private bool RaffleIsRunning => numberBoard != null && btnEndRaffle.Enabled;

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


            numberPad.NumberSubmitted += (s, ticketNumber) => HandleTicketSelectionAction(ticketNumber);

            // Tabs are fixed - no close / scroll buttons on the tab strip
            if (pageMain.ViewElement is RadPageViewStripElement stripElement)
                stripElement.StripButtons = StripViewButtons.None;

            //Set global themes
            RadMessageBox.SetThemeName("CrystalDark");
        }

        /// <summary>
        /// While the raffle is running and the Live Draw tab is showing, number keys / Backspace / Esc / Enter
        /// go straight to the number pad no matter which control has focus.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (RaffleIsRunning && keyData == (Keys.Control | Keys.Z))
            {
                btnUndoLastDraw_Click(this, EventArgs.Empty);
                return true;
            }

            if (RaffleIsRunning && pageMain.SelectedPage == pageDraw && !txtPayoutDraws.ContainsFocus
                && numberPad.HandleKey(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Form Events

        private void frmConsole_Load(object sender, EventArgs e)
        {

        }

        void frmConsole_Shown(object sender, EventArgs e)
        {
            BindGrid();

            // Load payout schedule from settings
            txtPayoutDraws.Text = Properties.Settings.Default.PayoutDraws;
            UpdateDrawStatus();
        }

        void frmConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();

            // Let the number board close with the app
            if (numberBoard != null)
                numberBoard.AllowClose = true;

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
            if (!RaffleIsRunning)
                txtPayoutDraws.Text = Properties.Settings.Default.PayoutDraws;

            // Settings can reset the database or the drawn flags - reload the grid from the database
            BindGrid();
            UpdateDrawStatus();
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
                    $"Excel: column 1 = ticket number, columns 2-5 = names A, B, C, D.\r\n" +
                    $"CSV: header id,name1,name2,name3,name4 - no commas inside names.\r\n\r\n" +
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
            var holder4 = e.Row.Cells["colTicketHolder4"].Value?.ToString();

            dbModel.TicketHolders.Add(new TicketHolder { TicketNumber = tickNum, TicketHolder1 = holder1, TicketHolder2 = holder2, TicketHolder3 = holder3, TicketHolder4 = holder4 });
            dbModel.SaveChanges();
        }

        void gridTicketHolders_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            var tickNum = Convert.ToInt32(e.Row.Cells["colTicketNumber"].Value);
            var holder1 = e.Row.Cells["colTicketHolder1"].Value?.ToString();
            var holder2 = e.Row.Cells["colTicketHolder2"].Value?.ToString();
            var holder3 = e.Row.Cells["colTicketHolder3"].Value?.ToString();
            var holder4 = e.Row.Cells["colTicketHolder4"].Value?.ToString();

            var existingTicket = dbModel.TicketHolders.FirstOrDefault(x => x.TicketNumber == tickNum);

            if (existingTicket == null)
            {
                dbModel.TicketHolders.Add(new TicketHolder
                {
                    TicketNumber = tickNum,
                    TicketHolder1 = holder1,
                    TicketHolder2 = holder2,
                    TicketHolder3 = holder3,
                    TicketHolder4 = holder4
                });
            }
            else
            {
                existingTicket.TicketHolder1 = holder1;
                existingTicket.TicketHolder2 = holder2;
                existingTicket.TicketHolder3 = holder3;
                existingTicket.TicketHolder4 = holder4;
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
            // Fresh context every time: the board, import and settings screens write through their own
            // contexts, and a long-lived context would hand back its stale cached copies of those rows.
            dbModel?.Dispose();
            dbModel = new RevRaffleModelEntities();

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
            //Validate the payout schedule
            if (!PayoutSchedule.TryParse(txtPayoutDraws.Text, out var schedule, out var error))
            {
                RadMessageBox.Show(error, "Payout Draws", MessageBoxButtons.OK, RadMessageIcon.Error);
                return;
            }

            //Reload tickets so we start from what's in the database
            BindGrid();

            if (RaffleTickets.Count == 0)
            {
                RadMessageBox.Show("There are no tickets in the database. Import your ticket file first (File > Import Tickets).",
                    "No Tickets", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }

            if (schedule.GrandPrizeDraw > RaffleTickets.Count)
            {
                var answer = RadMessageBox.Show(
                    $"The Grand Prize is set for Draw #{schedule.GrandPrizeDraw}, but there are only {RaffleTickets.Count} tickets, " +
                    $"so it can never be reached.\r\n\r\nStart anyway?",
                    "Payout Draws", MessageBoxButtons.YesNo, RadMessageIcon.Exclamation);
                if (answer != DialogResult.Yes) return;
            }

            payoutSchedule = schedule;
            txtPayoutDraws.Text = schedule.ToString();
            Properties.Settings.Default.PayoutDraws = schedule.ToString();
            Properties.Settings.Default.Save();

            if (numberBoard == null)
                numberBoard = new frmNumberBoard();

            // Typing on the projector window still feeds the number pad (and Ctrl+Z still undoes)
            numberBoard.EntryKeyPressed += keyData =>
            {
                if (!RaffleIsRunning) return false;
                if (keyData == (Keys.Control | Keys.Z))
                {
                    btnUndoLastDraw_Click(this, EventArgs.Empty);
                    return true;
                }
                return numberPad.HandleKey(keyData);
            };

            // Show the form first so the canvas has proper dimensions
            numberBoard.Show();

            //Add Raffle Tickets
            foreach (var tickItem in RaffleTickets.OrderBy(x => x.TicketNumber))
                numberBoard.AddTicketRaffle(tickItem);

            //Enable / disable controls
            btnBeginRaffle.Enabled = false;
            txtPayoutDraws.Enabled = false;
            btnEndRaffle.Enabled = true;
            numberPad.Enabled = true;

            //Set Grid Options
            gridTicketHolders.MasterTemplate.AllowAddNewRow = false;
            gridTicketHolders.MasterTemplate.AllowEditRow = false;

            lstDrawLog.Items.Clear();
            drawHistory.Clear();
            var alreadyDrawn = (int)numberBoard.TotalLosingTickets;
            if (alreadyDrawn > 0)
                lstDrawLog.Items.Insert(0, $"Resumed raffle - {alreadyDrawn} ticket(s) were already drawn");

            pageMain.SelectedPage = pageDraw;
            lblAppStatus.Text = @"Raffle is running...";

            UpdateDrawStatus();
            UpdateHistoryControlStates();

            // Put focus back on this window so the keyboard goes to the number pad
            Activate();

            //Start API Server automatically
            StartApiServer();
        }

        private void btnEndRaffle_Click(object sender, EventArgs e)
        {
            if (numberBoard == null) return;

            if (!numberBoard.RaffleIsOver)
            {
                var answer = RadMessageBox.Show(
                    "End the raffle and close the number board?\r\n\r\nDrawn tickets are saved - click Begin Raffle to pick up where you left off.",
                    "End Raffle", MessageBoxButtons.YesNo, RadMessageIcon.Question);
                if (answer != DialogResult.Yes) return;
            }

            btnBeginRaffle.Enabled = true;
            txtPayoutDraws.Enabled = true;
            btnEndRaffle.Enabled = false;
            numberPad.Enabled = false;

            //Set Grid Options
            gridTicketHolders.MasterTemplate.AllowAddNewRow = true;
            gridTicketHolders.MasterTemplate.AllowEditRow = true;

            lblAppStatus.Text = @"Raffle has ended...";

            numberBoard.AllowClose = true;
            numberBoard.Close();
            numberBoard = null;

            UpdateDrawStatus();
            UpdateHistoryControlStates();

            //Stop API Server automatically
            StopApiServer();
        }

        void HandleTicketSelectionAction(int ticketNumber)
        {
            if (!RaffleIsRunning) return;

            if (numberBoard.RaffleIsOver)
            {
                numberPad.ShowMessage("The Grand Prize has been drawn - raffle is over", Color.Goldenrod);
                return;
            }

            //Verify ticket number is even in the list!
            if (!numberBoard.VerifyTicketNumberExists(ticketNumber))
            {
                numberPad.ShowMessage($"Ticket #{ticketNumber} doesn't exist", Color.IndianRed);
                return;
            }

            //Verify ticket has NOT been selected already
            if (numberBoard.HasTicketBeenSelectedAlready(ticketNumber))
            {
                numberPad.ShowMessage($"Ticket #{ticketNumber} was already drawn", Color.Goldenrod);
                numberBoard.ShowTicketInfoDialog(ticketNumber);
                return;
            }

            var drawNumber = (int)numberBoard.TotalLosingTickets + 1;
            var outcome = payoutSchedule.GetOutcome(drawNumber);

            numberBoard.RecordDraw(ticketNumber, drawNumber, outcome);
            drawHistory.Push(new DrawRecord { TicketNumber = ticketNumber, DrawNumber = drawNumber, Outcome = outcome });

            string logText;
            switch (outcome)
            {
                case DrawOutcome.GrandPrize:
                    logText = $"Draw #{drawNumber}   Ticket #{ticketNumber}   \u2605 GRAND PRIZE \u2605";
                    numberPad.ShowMessage($"GRAND PRIZE: Ticket #{ticketNumber}", Color.Gold);
                    numberPad.Enabled = false;
                    lblAppStatus.Text = @"Grand Prize drawn - raffle complete";
                    break;
                case DrawOutcome.Payout:
                    logText = $"Draw #{drawNumber}   Ticket #{ticketNumber}   \u2605 MINOR WINNER (payout)";
                    numberPad.ShowMessage($"Draw #{drawNumber} PAYOUT: Ticket #{ticketNumber}", Color.Gold);
                    break;
                default:
                    logText = $"Draw #{drawNumber}   Ticket #{ticketNumber}";
                    numberPad.ShowMessage($"Draw #{drawNumber}: Ticket #{ticketNumber} recorded", Color.MediumSeaGreen);
                    break;
            }

            lstDrawLog.Items.Insert(0, logText);
            lblAppTicketStatus.Text = $"Ticket #: {ticketNumber} was drawn (Draw #{drawNumber})";

            UpdateDrawStatus();
            UpdateHistoryControlStates();
        }

        private void btnUndoLastDraw_Click(object sender, EventArgs e)
        {
            if (!RaffleIsRunning || drawHistory.Count == 0) return;

            var last = drawHistory.Peek();
            var kind = last.Outcome == DrawOutcome.GrandPrize ? " (GRAND PRIZE)"
                     : last.Outcome == DrawOutcome.Payout ? " (minor winner)" : "";

            var answer = RadMessageBox.Show(
                $"Undo Draw #{last.DrawNumber} - Ticket #{last.TicketNumber}{kind}?\r\n\r\n" +
                $"The ticket goes back in play and Draw #{last.DrawNumber} will be drawn again.",
                "Undo Last Draw", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (answer != DialogResult.Yes) return;

            // A ticket may have arrived (e.g. via the API) while the confirmation was open
            if (!RaffleIsRunning || drawHistory.Count == 0 || !ReferenceEquals(drawHistory.Peek(), last))
            {
                numberPad.ShowMessage("Another draw came in - nothing was undone", Color.Goldenrod);
                UpdateHistoryControlStates();
                return;
            }

            drawHistory.Pop();
            numberBoard.UndoDraw(last.TicketNumber);

            // Remove the matching entry from the draw log (newest entries are at the top)
            var prefix = $"Draw #{last.DrawNumber}   Ticket #{last.TicketNumber}";
            for (int i = 0; i < lstDrawLog.Items.Count; i++)
            {
                if (lstDrawLog.Items[i].ToString().StartsWith(prefix))
                {
                    lstDrawLog.Items.RemoveAt(i);
                    break;
                }
            }

            numberPad.Enabled = true;
            numberPad.ShowMessage($"Undone: Draw #{last.DrawNumber} (Ticket #{last.TicketNumber})", Color.DeepSkyBlue);
            lblAppStatus.Text = @"Raffle is running...";
            lblAppTicketStatus.Text = $"Draw #{last.DrawNumber} (Ticket #{last.TicketNumber}) was undone";

            UpdateDrawStatus();
            UpdateHistoryControlStates();
            Activate();
        }

        /// <summary>
        /// Refreshes the "next draw" panel: which draw is next, whether it pays out, and the stats line.
        /// </summary>
        private void UpdateDrawStatus()
        {
            if (!RaffleIsRunning || payoutSchedule == null)
            {
                lblNextDraw.Text = "Raffle not started";
                lblNextDraw.ForeColor = Color.Gray;
                lblNextDrawType.Text = string.Empty;
                lblUpcomingPayout.Text = PayoutSchedule.TryParse(txtPayoutDraws.Text, out var preview, out _)
                    ? $"Minor winners on draws {string.Join(", ", preview.Draws.Where(d => d != preview.GrandPrizeDraw).Select(d => "#" + d))}  -  Grand Prize on draw #{preview.GrandPrizeDraw}"
                    : string.Empty;
                lblStats.Text = RaffleTickets == null ? string.Empty : $"{RaffleTickets.Count} tickets loaded";
                return;
            }

            var total = (int)numberBoard.TotalTickets;
            var drawn = (int)numberBoard.TotalLosingTickets;
            var remaining = total - drawn;
            lblStats.Text = $"Drawn: {drawn}     Remaining: {remaining}     Total tickets: {total}";

            if (numberBoard.RaffleIsOver)
            {
                lblNextDraw.Text = "Raffle complete!";
                lblNextDraw.ForeColor = Color.Gold;
                lblNextDrawType.Text = "\u2605 GRAND PRIZE AWARDED \u2605";
                lblNextDrawType.ForeColor = Color.Gold;
                lblUpcomingPayout.Text = "The Grand Prize stays on the board until you click \"Close Popup\".";
                return;
            }

            var next = drawn + 1;
            lblNextDraw.Text = $"Draw #{next} of {total}";
            lblNextDraw.ForeColor = Color.White;

            switch (payoutSchedule.GetOutcome(next))
            {
                case DrawOutcome.GrandPrize:
                    lblNextDrawType.Text = "\u2605 GRAND PRIZE DRAW \u2605";
                    lblNextDrawType.ForeColor = Color.Gold;
                    break;
                case DrawOutcome.Payout:
                    lblNextDrawType.Text = "\u2605 PAYOUT DRAW - minor winner";
                    lblNextDrawType.ForeColor = Color.Gold;
                    break;
                default:
                    lblNextDrawType.Text = "Not a winner";
                    lblNextDrawType.ForeColor = Color.Silver;
                    break;
            }

            var upcoming = payoutSchedule.NextPayoutAtOrAfter(next + 1);
            if (remaining == 1)
                lblUpcomingPayout.Text = $"Only ticket #{numberBoard.UndrawnTicketNumbers.First()} is left";
            else if (upcoming.HasValue)
                lblUpcomingPayout.Text = $"Next payout: Draw #{upcoming}" +
                    (upcoming.Value == payoutSchedule.GrandPrizeDraw ? " (Grand Prize)" : "") +
                    $"  -  {upcoming.Value - next} draw(s) after this one";
            else
                lblUpcomingPayout.Text = string.Empty;
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
            btnUndoLastDraw.Enabled = RaffleIsRunning && drawHistory.Count > 0;

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
                lblHistoryPosition.Text = "Popup history: " + numberBoard.GetHistoryPosition();
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
                getApiTicketNumber.Interval = 500;
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
