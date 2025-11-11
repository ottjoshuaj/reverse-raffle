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
using Timer = System.Threading.Timer;

namespace ReverseRaffler
{
    public partial class frmNumberBoard : RadForm
    {
        #region Fields

        private RaffleCanvas _raffleCanvas;
        private RaffleOverlay _overlay;

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
            // Show ticket details when clicked
            ShowTicketInfoDialog(e.Ticket.TicketNumber);
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

                    _overlay.ShowLoser(
                        ticketNumber.ToString(CultureInfo.InvariantCulture),
                        new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 },
                        autoCloseMs
                    );

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

                _overlay.ShowLoser(
                    ticketNumber.ToString(CultureInfo.InvariantCulture),
                    new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 },
                    autoCloseMs
                );
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

            var frmTicketInfoDialog = new frmTicketDetails(ticketNumber,
                new[] { ticket.TicketHolder1, ticket.TicketHolder2, ticket.TicketHolder3 });
            frmTicketInfoDialog.ShowDialog(this);
            frmTicketInfoDialog.BringToFront();
            frmTicketInfoDialog.Close();
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

            _overlay.ShowWinner(
                selTicket.TicketNumber.ToString(CultureInfo.InvariantCulture),
                winners,
                autoCloseMs
            );
        }

        public void ClearRaffleScreen()
        {
            _raffleCanvas?.Clear();
            _overlay?.Close();
        }
    }
}
