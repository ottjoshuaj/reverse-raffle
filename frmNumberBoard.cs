using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        #region Properties

        public bool RaffleIsOver { get; private set; }

        public long TotalTickets
        {
            get { return panelTickerHolder.Controls.Count; }
        }

        public long TotalLosingTickets
        {
            get
            {
                return panelTickerHolder.Controls.Cast<ctlNumberSheet>().Count(ticketControl => 
                    ticketControl.Ticket.MarkedAsNotWinner.GetValueOrDefault());
            }
        }

        public long TotalTicketsNotDrawn
        {
            get { return (TotalTickets - TotalLosingTickets); }
        }

        public long AvailableWinners { get; set; }

        List<TicketHolder> Tickets
        {
            get
            {
                return (from ctlNumberSheet ticketCtl in panelTickerHolder.Controls select ticketCtl.Ticket).ToList();
            }
        }

        #endregion

        public frmNumberBoard()
        {
            InitializeComponent();

            RaffleIsOver = false;
        }

        public void AddTicketRaffle(TicketHolder ticketItem)
        {
            var newTicketCtl = new ctlNumberSheet(ticketItem);
            newTicketCtl.Name = ticketItem.TicketNumber.ToString(CultureInfo.InvariantCulture);

            if (Properties.Settings.Default.ticketWidth > 0)
                newTicketCtl.Width = Convert.ToInt32(Properties.Settings.Default.ticketWidth); 

            if (Properties.Settings.Default.ticketHeightt > 0)
                newTicketCtl.Height = Convert.ToInt32(Properties.Settings.Default.ticketHeightt);

            //Add ticket control to panel
            panelTickerHolder.Controls.Add(newTicketCtl);
        }

        public void MarkTicketAsLosing(int ticketNumber, bool skipDisplayScreen = false)
        {
            var ticketControl = panelTickerHolder.Controls.Find(ticketNumber.ToString(CultureInfo.InvariantCulture), false)
                .FirstOrDefault() as ctlNumberSheet;

            if (ticketControl == null) return;
            
            //Mark Ticket Control as not winner
            ticketControl.MarkTicketAsNotWinner();

            if (TotalTicketsNotDrawn == AvailableWinners)
            {
                //Just a normal losing ticket.  Just show the loser display for 5 seconds
                var frmLosingTicket = new frmTicketNotWinner(ticketNumber,
                    new[] { ticketControl.Ticket.TicketHolder1, ticketControl.Ticket.TicketHolder2, ticketControl.Ticket.TicketHolder3 });
                frmLosingTicket.ShowDialog(this);
                frmLosingTicket.BringToFront();
                frmLosingTicket.Close();

                //Now show the winning screen
                var winningTickets = string.Empty;
                var winners = new List<string>();

                foreach (var ticket in Tickets.Where(x => !x.MarkedAsNotWinner.GetValueOrDefault()))
                {
                    winningTickets = string.IsNullOrWhiteSpace(winningTickets) ?
                        ticket.TicketNumber.ToString(CultureInfo.InvariantCulture) :
                        string.Format("{0}, {1}", winningTickets, ticket.TicketNumber);

                    if (!winners.Contains(ticket.TicketHolder1))
                        winners.Add(ticket.TicketHolder1);
                    if (!winners.Contains(ticket.TicketHolder2))
                        winners.Add(ticket.TicketHolder2);
                    if (!winners.Contains(ticket.TicketHolder3))
                        winners.Add(ticket.TicketHolder3);
                }

                //Raffle is over
                var winnerForm = new frmTicketWinner(winningTickets, winners);
                winnerForm.ShowDialog(this);
                winnerForm.BringToFront();

                //Make sure this control knows the raffle is over
                RaffleIsOver = true;
            }
            else
            {
                if (skipDisplayScreen) return;

                //Just a normal losing ticket.  Just show the loser display for 5 seconds
                var frmLosingTicket = new frmTicketNotWinner(ticketNumber,
                    new[] { ticketControl.Ticket.TicketHolder1, ticketControl.Ticket.TicketHolder2, ticketControl.Ticket.TicketHolder3 });
                frmLosingTicket.ShowDialog(this);
                frmLosingTicket.BringToFront();
            }
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
            var ticketControl = panelTickerHolder.Controls.Find(ticketNumber.ToString(CultureInfo.InvariantCulture), false)
                .FirstOrDefault() as ctlNumberSheet;

            var frmTicketInfoDialog = new frmTicketNotWinner(ticketNumber,
                new[] { ticketControl.Ticket.TicketHolder1, ticketControl.Ticket.TicketHolder2, ticketControl.Ticket.TicketHolder3 });
            frmTicketInfoDialog.ShowDialog(this);
            frmTicketInfoDialog.BringToFront();
            frmTicketInfoDialog.Close();
        }

        public void ShowWinnerScreen(int ticketNumber)
        {
            var winners = new List<string>();

            var selTicket = Tickets.FirstOrDefault(x => x.TicketNumber == ticketNumber);
            if (selTicket == null) return;

            if (!winners.Contains(selTicket.TicketHolder1))
                winners.Add(selTicket.TicketHolder1);
            if (!winners.Contains(selTicket.TicketHolder2))
                winners.Add(selTicket.TicketHolder2);
            if (!winners.Contains(selTicket.TicketHolder3))
                winners.Add(selTicket.TicketHolder3);

            //Raffle is over
            var winnerForm = new frmTicketWinner(selTicket.TicketNumber.ToString(CultureInfo.InvariantCulture), winners);
            winnerForm.ShowDialog(this);
            winnerForm.BringToFront();
        }

        public void ClearRaffleScreen()
        {
            foreach (var ctl in panelTickerHolder.Controls.Cast<UserControl>().Where(ctl => ctl != null))
                ctl.Dispose();

            panelTickerHolder.Controls.Clear();
        }
    }
}
