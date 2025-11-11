using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.UI;

using ReverseRaffler.Controls;
using ReverseRaffler.Data;
using ReverseRaffler.Objects;
using Timer = System.Threading.Timer;

namespace ReverseRaffler
{
    public partial class frmNumberBoard : RadForm
    {
        #region Fields

        private RaffleCanvas _raffleCanvas;
        private RaffleOverlay _overlay;
        private List<AnnouncementHistoryItem> _announcementHistory;
        private int _currentHistoryIndex;

        #endregion

        #region Properties

        public bool RaffleIsOver { get; private set; }

        public long TotalTickets
        {
            get { return _raffleCanvas?.Tickets?.Count ?? 0; }
        }

        public long TotalLosingTickets
        {
            get { return _raffleCanvas?.TotalLosingTickets ?? 0; }
        }

        public long TotalTicketsNotDrawn
        {
            get { return _raffleCanvas?.TotalTicketsNotDrawn ?? 0; }
        }

        public long AvailableWinners { get; set; }

        List<TicketHolder> Tickets
        {
            get { return _raffleCanvas?.Tickets ?? new List<TicketHolder>(); }
        }

        #endregion

        public frmNumberBoard()
        {
            InitializeComponent();

            RaffleIsOver = false;
            _announcementHistory = new List<AnnouncementHistoryItem>();
            _currentHistoryIndex = -1;

            // Initialize the new canvas-based system
            InitializeRaffleCanvas();
        }

        private void InitializeRaffleCanvas()
        {
            // Create and configure the raffle canvas
            _raffleCanvas = new RaffleCanvas
            {
                Dock = DockStyle.Fill,
                TicketWidth = Properties.Settings.Default.ticketWidth > 0 ?
                    Convert.ToInt32(Properties.Settings.Default.ticketWidth) : 120,
                TicketHeight = Properties.Settings.Default.ticketHeightt > 0 ?
                    Convert.ToInt32(Properties.Settings.Default.ticketHeightt) : 60
            };

            // Handle ticket click events
            _raffleCanvas.TicketClicked += RaffleCanvas_TicketClicked;

            // Replace the old FlowLayoutPanel with the new canvas
            panelTickerHolder.Controls.Clear();
            panelTickerHolder.Controls.Add(_raffleCanvas);

            // Create overlay for winner/loser announcements
            _overlay = new RaffleOverlay();
            panelTickerHolder.Controls.Add(_overlay);
            _overlay.BringToFront();
        }

        private void RaffleCanvas_TicketClicked(object sender, TicketClickedEventArgs e)
        {
            // Show ticket details in overlay when clicked
            var ticket = e.Ticket;
            var names = new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 };

            // Show appropriate overlay based on ticket status, with close button
            if (ticket.MarkedAsNotWinner == true)
            {
                // Ticket marked as loser
                _overlay.ShowLoser(ticket.TicketNumber.ToString(), names, 0, true);
            }
            else if (ticket.MarkedAsNotWinner == false)
            {
                // Ticket marked as winner
                _overlay.ShowWinner(ticket.TicketNumber.ToString(), names, 0, true);
            }
            else
            {
                // Ticket not yet drawn (neither winner nor loser)
                _overlay.ShowInfo(ticket.TicketNumber.ToString(), names, 0, true);
            }
        }

        public void AddTicketRaffle(TicketHolder ticketItem)
        {
            // Simply add ticket to the canvas - it handles all drawing
            _raffleCanvas.AddTicket(ticketItem);
        }

        public void MarkTicketAsLosing(int ticketNumber, bool skipDisplayScreen = false)
        {
            var ticket = _raffleCanvas.GetTicket(ticketNumber);
            if (ticket == null) return;

            // Mark ticket as losing in the database
            using (var dbModel = new RevRaffleModelEntities())
            {
                var dbTicket = dbModel.TicketHolders.FirstOrDefault(x => x.TicketNumber == ticketNumber);
                if (dbTicket != null)
                {
                    dbTicket.MarkedAsNotWinner = true;
                    dbModel.TicketHolders.AddOrUpdate(dbTicket);
                    dbModel.SaveChanges();
                }
            }

            // Mark ticket visually on canvas (this will trigger a repaint with red X)
            _raffleCanvas.MarkTicketAsLosing(ticketNumber);

            if (TotalTicketsNotDrawn == AvailableWinners)
            {
                // Show loser overlay first (if not skipped)
                if (!skipDisplayScreen)
                {
                    int autoCloseMs = Properties.Settings.Default.EnableAutoCloseLosingTicketWindow == CheckState.Checked ?
                        (int)(Properties.Settings.Default.AutoCloseLosingTicketWindowAfter * 1000) : 0;

                    var names = new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 };
                    _overlay.ShowLoser(ticketNumber.ToString(CultureInfo.InvariantCulture), names, autoCloseMs);

                    // Track in history
                    AddToHistory(AnnouncementType.Loser, ticketNumber.ToString(CultureInfo.InvariantCulture), names);

                    // Wait for overlay to close before showing winner
                    var closeWaiter = new System.Windows.Forms.Timer { Interval = autoCloseMs > 0 ? autoCloseMs : 100 };
                    closeWaiter.Tick += (s, e) =>
                    {
                        closeWaiter.Stop();
                        closeWaiter.Dispose();
                        ShowFinalWinners();
                    };
                    closeWaiter.Start();
                }
                else
                {
                    ShowFinalWinners();
                }
            }
            else
            {
                if (skipDisplayScreen) return;

                // Show loser overlay
                int autoCloseMs = Properties.Settings.Default.EnableAutoCloseLosingTicketWindow == CheckState.Checked ?
                    (int)(Properties.Settings.Default.AutoCloseLosingTicketWindowAfter * 1000) : 0;

                var names = new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 };
                _overlay.ShowLoser(ticketNumber.ToString(CultureInfo.InvariantCulture), names, autoCloseMs);

                // Track in history
                AddToHistory(AnnouncementType.Loser, ticketNumber.ToString(CultureInfo.InvariantCulture), names);
            }
        }

        private void ShowFinalWinners()
        {
            // Collect all winning tickets
            var winningTickets = string.Empty;
            var winners = new List<string>();

            foreach (var ticket in Tickets.Where(x => !x.MarkedAsNotWinner.GetValueOrDefault()))
            {
                winningTickets = string.IsNullOrWhiteSpace(winningTickets) ?
                    ticket.TicketNumber.ToString(CultureInfo.InvariantCulture) :
                    string.Format("{0}, {1}", winningTickets, ticket.TicketNumber);

                if (!string.IsNullOrWhiteSpace(ticket.TicketHolder1) && !winners.Contains(ticket.TicketHolder1))
                    winners.Add(ticket.TicketHolder1);
                if (!string.IsNullOrWhiteSpace(ticket.TicketHolder2) && !winners.Contains(ticket.TicketHolder2))
                    winners.Add(ticket.TicketHolder2);
                if (!string.IsNullOrWhiteSpace(ticket.TicketHolder3) && !winners.Contains(ticket.TicketHolder3))
                    winners.Add(ticket.TicketHolder3);
            }

            // Show winner overlay
            int autoCloseMs = Properties.Settings.Default.EnableAutoCloseWinningTicketWindow == CheckState.Checked ?
                (int)(Properties.Settings.Default.AutoCloseWinningTicketWindowAfter * 1000) : 0;

            _overlay.ShowWinner(winningTickets, winners, autoCloseMs);

            // Track in history
            AddToHistory(AnnouncementType.Winner, winningTickets, winners);

            // Mark raffle as over
            RaffleIsOver = true;
        }


        public bool VerifyTicketNumberExists(int ticketNumber)
        {
            return Tickets.Any(x => x.TicketNumber == ticketNumber);
        }

        public bool HasTicketBeenSelectedAlready(int ticketNumber)
        {
            var selTicket = Tickets.FirstOrDefault(x => x.TicketNumber == ticketNumber);
            if (selTicket.MarkedAsNotWinner == true)
                return true;

            return false;
        }

        public void ShowTicketInfoDialog(int ticketNumber)
        {
            var ticket = _raffleCanvas.GetTicket(ticketNumber);
            if (ticket == null) return;

            var names = new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 };

            // Show ticket info using overlay (with close button since it's manual)
            if (ticket.MarkedAsNotWinner.GetValueOrDefault())
            {
                _overlay.ShowLoser(ticketNumber.ToString(), names, 0, true);
            }
            else
            {
                _overlay.ShowWinner(ticketNumber.ToString(), names, 0, true);
            }
        }

        public void ShowWinnerScreen(int ticketNumber)
        {
            var selTicket = Tickets.FirstOrDefault(x => x.TicketNumber == ticketNumber);
            if (selTicket == null) return;

            var winners = new List<string>();
            if (!string.IsNullOrWhiteSpace(selTicket.TicketHolder1))
                winners.Add(selTicket.TicketHolder1);
            if (!string.IsNullOrWhiteSpace(selTicket.TicketHolder2))
                winners.Add(selTicket.TicketHolder2);
            if (!string.IsNullOrWhiteSpace(selTicket.TicketHolder3))
                winners.Add(selTicket.TicketHolder3);

            // Show winner overlay with auto-close
            int autoCloseMs = Properties.Settings.Default.EnableAutoCloseWinningTicketWindow == CheckState.Checked ?
                (int)(Properties.Settings.Default.AutoCloseWinningTicketWindowAfter * 1000) : 0;

            var ticketNum = selTicket.TicketNumber.ToString(CultureInfo.InvariantCulture);
            _overlay.ShowWinner(ticketNum, winners, autoCloseMs);

            // Track in history
            AddToHistory(AnnouncementType.Winner, ticketNum, winners);
        }

        public void ClearRaffleScreen()
        {
            _raffleCanvas?.Clear();
            _overlay?.Close();
            _announcementHistory.Clear();
            _currentHistoryIndex = -1;
        }

        #region Announcement History Navigation

        /// <summary>
        /// Adds an announcement to history
        /// </summary>
        private void AddToHistory(AnnouncementType type, string ticketNumbers, IEnumerable<string> names)
        {
            var item = new AnnouncementHistoryItem
            {
                Type = type,
                TicketNumbers = ticketNumbers,
                Names = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>(),
                Timestamp = DateTime.Now,
                SequenceNumber = _announcementHistory.Count
            };

            _announcementHistory.Add(item);
            _currentHistoryIndex = _announcementHistory.Count - 1;
        }

        /// <summary>
        /// Shows the most recent announcement
        /// </summary>
        public void ShowMostRecentAnnouncement()
        {
            if (_announcementHistory.Count == 0)
            {
                Telerik.WinControls.RadMessageBox.Show("No announcements to show yet.", "No History",
                    System.Windows.Forms.MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Info);
                return;
            }

            _currentHistoryIndex = _announcementHistory.Count - 1;
            ShowCurrentHistoryItem();
        }

        /// <summary>
        /// Shows the previous announcement in history
        /// </summary>
        public void ShowPreviousAnnouncement()
        {
            if (_announcementHistory.Count == 0) return;

            if (_currentHistoryIndex > 0)
            {
                _currentHistoryIndex--;
                ShowCurrentHistoryItem();
            }
            else
            {
                Telerik.WinControls.RadMessageBox.Show("Already at the first announcement.", "History",
                    System.Windows.Forms.MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Info);
            }
        }

        /// <summary>
        /// Shows the next announcement in history
        /// </summary>
        public void ShowNextAnnouncement()
        {
            if (_announcementHistory.Count == 0) return;

            if (_currentHistoryIndex < _announcementHistory.Count - 1)
            {
                _currentHistoryIndex++;
                ShowCurrentHistoryItem();
            }
            else
            {
                Telerik.WinControls.RadMessageBox.Show("Already at the most recent announcement.", "History",
                    System.Windows.Forms.MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Info);
            }
        }

        /// <summary>
        /// Closes the current overlay
        /// </summary>
        public void CloseCurrentOverlay()
        {
            _overlay?.Close();
        }

        /// <summary>
        /// Gets whether there are any announcements in history
        /// </summary>
        public bool HasHistory => _announcementHistory.Count > 0;

        /// <summary>
        /// Gets the current history position (for UI display)
        /// </summary>
        public string GetHistoryPosition()
        {
            if (_announcementHistory.Count == 0)
                return "No history";

            return $"{_currentHistoryIndex + 1} of {_announcementHistory.Count}";
        }

        /// <summary>
        /// Shows the announcement at the current history index
        /// </summary>
        private void ShowCurrentHistoryItem()
        {
            if (_currentHistoryIndex < 0 || _currentHistoryIndex >= _announcementHistory.Count)
                return;

            var item = _announcementHistory[_currentHistoryIndex];

            // Don't auto-close when viewing history
            if (item.Type == AnnouncementType.Winner)
            {
                _overlay.ShowWinner(item.TicketNumbers, item.Names, 0);
            }
            else
            {
                _overlay.ShowLoser(item.TicketNumbers, item.Names, 0);
            }
        }

        #endregion
    }
}
