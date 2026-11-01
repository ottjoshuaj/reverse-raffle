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
using ReverseRaffler.Services;
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

        /// <summary>
        /// The board can only be closed by the console's End Raffle button. Closing it by hand mid-raffle
        /// would leave the console pointing at a disposed window.
        /// </summary>
        public bool AllowClose { get; set; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!AllowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Telerik.WinControls.RadMessageBox.Show(this, "Use \"End Raffle\" on the console to close the number board.",
                    "Raffle Running", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Info);
                return;
            }

            base.OnFormClosing(e);
        }

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

        /// <summary>
        /// Raised for key presses on the board window so the console's number pad still works
        /// when the operator has clicked on the projector screen.
        /// </summary>
        public event Func<Keys, bool> EntryKeyPressed;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (EntryKeyPressed != null && EntryKeyPressed(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RaffleCanvas_TicketClicked(object sender, TicketClickedEventArgs e)
        {
            // Grand Prize stays on screen - don't replace it with ticket info
            if (_overlay.IsLocked) return;

            var ticket = e.Ticket;
            var names = ticket.GetDesignatedNames();
            var ticketText = ticket.TicketNumber.ToString(CultureInfo.InvariantCulture);

            if (_payoutDraws.TryGetValue(ticket.TicketNumber, out var drawNumber))
                _overlay.ShowPayoutWinner(ticketText, names, drawNumber);
            else if (ticket.MarkedAsNotWinner == true)
                _overlay.ShowLoser(ticketText, names, 0, true);
            else
                _overlay.ShowInfo(ticketText, names, 0, true);
        }

        public void AddTicketRaffle(TicketHolder ticketItem)
        {
            // Simply add ticket to the canvas - it handles all drawing
            _raffleCanvas.AddTicket(ticketItem);
        }

        // Ticket number -> draw number, for tickets that won a payout this session
        private readonly Dictionary<int, int> _payoutDraws = new Dictionary<int, int>();

        /// <summary>
        /// Records a drawn ticket: persists it as drawn, updates the board and shows the announcement.
        /// </summary>
        public void RecordDraw(int ticketNumber, int drawNumber, DrawOutcome outcome)
        {
            var ticket = _raffleCanvas.GetTicket(ticketNumber);
            if (ticket == null) return;

            // Persist as drawn so the raffle can be resumed after a restart
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

            var names = ticket.GetDesignatedNames();
            var ticketText = ticketNumber.ToString(CultureInfo.InvariantCulture);

            switch (outcome)
            {
                case DrawOutcome.GrandPrize:
                    _payoutDraws[ticketNumber] = drawNumber;
                    _raffleCanvas.MarkTicketAsGrandPrize(ticketNumber);
                    _overlay.ShowGrandPrize(ticketText, names, drawNumber);
                    AddToHistory(AnnouncementType.GrandPrize, ticketText, names, drawNumber);
                    RaffleIsOver = true;
                    break;

                case DrawOutcome.Payout:
                    _payoutDraws[ticketNumber] = drawNumber;
                    _raffleCanvas.MarkTicketAsPayoutWinner(ticketNumber);
                    int winnerCloseMs = Properties.Settings.Default.EnableAutoCloseWinningTicketWindow == CheckState.Checked ?
                        (int)(Properties.Settings.Default.AutoCloseWinningTicketWindowAfter * 1000) : 0;
                    _overlay.ShowPayoutWinner(ticketText, names, drawNumber, winnerCloseMs);
                    AddToHistory(AnnouncementType.Winner, ticketText, names, drawNumber);
                    break;

                default:
                    _raffleCanvas.MarkTicketAsLosing(ticketNumber);
                    int loserCloseMs = Properties.Settings.Default.EnableAutoCloseLosingTicketWindow == CheckState.Checked ?
                        (int)(Properties.Settings.Default.AutoCloseLosingTicketWindowAfter * 1000) : 0;
                    _overlay.ShowLoser(ticketText, names, loserCloseMs);
                    AddToHistory(AnnouncementType.Loser, ticketText, names, drawNumber);
                    break;
            }
        }

        /// <summary>
        /// Reverses a draw made with <see cref="RecordDraw"/>: the ticket goes back in play (database and board),
        /// its popup is removed from history, and the raffle re-opens if it was the Grand Prize.
        /// </summary>
        public void UndoDraw(int ticketNumber)
        {
            using (var dbModel = new RevRaffleModelEntities())
            {
                var dbTicket = dbModel.TicketHolders.FirstOrDefault(x => x.TicketNumber == ticketNumber);
                if (dbTicket != null)
                {
                    dbTicket.MarkedAsNotWinner = null;
                    dbModel.SaveChanges();
                }
            }

            _raffleCanvas.UnmarkTicket(ticketNumber);
            _payoutDraws.Remove(ticketNumber);
            RaffleIsOver = false;

            // Remove the most recent announcement for this ticket
            var ticketText = ticketNumber.ToString(CultureInfo.InvariantCulture);
            var index = _announcementHistory.FindLastIndex(h => h.TicketNumbers == ticketText);
            if (index >= 0)
            {
                _announcementHistory.RemoveAt(index);
                for (int i = 0; i < _announcementHistory.Count; i++)
                    _announcementHistory[i].SequenceNumber = i;
            }
            _currentHistoryIndex = _announcementHistory.Count - 1;

            _overlay.Close();
        }

        public bool VerifyTicketNumberExists(int ticketNumber)
        {
            return Tickets.Any(x => x.TicketNumber == ticketNumber);
        }

        public bool HasTicketBeenSelectedAlready(int ticketNumber)
        {
            var selTicket = Tickets.FirstOrDefault(x => x.TicketNumber == ticketNumber);
            return selTicket?.MarkedAsNotWinner == true;
        }

        /// <summary>
        /// Ticket numbers that have not been drawn yet
        /// </summary>
        public IEnumerable<int> UndrawnTicketNumbers =>
            Tickets.Where(x => !x.MarkedAsNotWinner.GetValueOrDefault()).Select(x => x.TicketNumber).OrderBy(x => x);

        public void ShowTicketInfoDialog(int ticketNumber)
        {
            var ticket = _raffleCanvas.GetTicket(ticketNumber);
            if (ticket == null) return;
            RaffleCanvas_TicketClicked(this, new TicketClickedEventArgs(ticket));
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
        private void AddToHistory(AnnouncementType type, string ticketNumbers, IEnumerable<string> names, int drawNumber = 0)
        {
            var item = new AnnouncementHistoryItem
            {
                Type = type,
                TicketNumbers = ticketNumbers,
                Names = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>(),
                Timestamp = DateTime.Now,
                SequenceNumber = _announcementHistory.Count,
                DrawNumber = drawNumber
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
            if (item.Type == AnnouncementType.GrandPrize)
                _overlay.ShowGrandPrize(item.TicketNumbers, item.Names, item.DrawNumber);
            else if (item.Type == AnnouncementType.Winner)
                _overlay.ShowPayoutWinner(item.TicketNumbers, item.Names, item.DrawNumber);
            else
                _overlay.ShowLoser(item.TicketNumbers, item.Names, 0);
        }

        #endregion

        #region Demo Methods

        /// <summary>
        /// Shows demo Info overlay with sample data
        /// </summary>
        public void ShowDemoInfo(string ticketNumber, IEnumerable<string> names)
        {
            _overlay.ShowInfo(ticketNumber, names, 0, true);
        }

        /// <summary>
        /// Shows demo Loser overlay with sample data
        /// </summary>
        public void ShowDemoLoser(string ticketNumber, IEnumerable<string> names)
        {
            _overlay.ShowLoser(ticketNumber, names, 0, true);
        }

        /// <summary>
        /// Shows demo Winner overlay with sample data
        /// </summary>
        public void ShowDemoWinner(string ticketNumber, IEnumerable<string> names)
        {
            _overlay.ShowWinner(ticketNumber, names, 0, true);
        }

        /// <summary>
        /// Shows demo Raffle Winner overlay with sample data and long name list for scrolling
        /// </summary>
        public void ShowDemoRaffleWinner(string ticketNumbers, IEnumerable<string> names)
        {
            _overlay.ShowRaffleWinner(ticketNumbers, names, 0, true);
        }

        #endregion
    }
}
