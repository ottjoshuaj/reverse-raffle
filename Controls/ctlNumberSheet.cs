using System;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

using ReverseRaffler.Data;

namespace ReverseRaffler.Controls
{
    public partial class ctlNumberSheet : UserControl
    {
        #region Properties

        public TicketHolder Ticket { get; private set; }

        #endregion

        #region Constructor

        public ctlNumberSheet(TicketHolder ticket)
        {
            Ticket = ticket;

            InitializeComponent();

            //Fix the label for ticket not a winner
            var notWinnerPosition = PointToScreen(lblTicketNotWinner.Location);
            notWinnerPosition = lblNumberDisplay.PointToClient(notWinnerPosition);
            lblTicketNotWinner.Parent = lblNumberDisplay;
            lblTicketNotWinner.Location = notWinnerPosition;
            lblTicketNotWinner.BackColor = Color.Transparent;
            lblTicketNotWinner.Visible = false;

            lblNumberDisplay.Text = ticket.TicketNumber.ToString(CultureInfo.InvariantCulture);

            if (ticket.MarkedAsNotWinner.GetValueOrDefault())
                lblTicketNotWinner.Visible = true;

            //Hook up events
            lblNumberDisplay.Click += lblNumberDisplay_Click;
            lblTicketNotWinner.Click += lblTicketNotWinner_Click;
        }

        #endregion

        #region Control Events

        private void ctlNumberSheet_Load(object sender, EventArgs e)
        {

        }

        void lblTicketNotWinner_Click(object sender, EventArgs e)
        {
            ShowTicketInfo();
        }

        void lblNumberDisplay_Click(object sender, EventArgs e)
        {
            ShowTicketInfo();
        }

        #endregion

        #region Public Methods

        public void MarkTicketAsNotWinner()
        {
            Ticket.MarkedAsNotWinner = true;
            lblTicketNotWinner.Visible = true;

            using (var dbModel = new RevRaffleModelEntities())
            {
                var ticket = dbModel.TicketHolders.FirstOrDefault(x => x.TicketNumber == Ticket.TicketNumber);
                if (ticket == null) return;

                ticket.MarkedAsNotWinner = true;
                dbModel.TicketHolders.AddOrUpdate(ticket);
                dbModel.SaveChanges();
            }
        }

        #endregion

        #region Internal Methods

        void ShowTicketInfo()
        {
            var newFrmTicketDisplay = new frmTicketDetails(Ticket.TicketNumber,
                new[] {Ticket.TicketHolder1, Ticket.TicketHolder2, Ticket.TicketHolder3});
            var result = newFrmTicketDisplay.ShowDialog(this);
            newFrmTicketDisplay.Dispose();
        }

        #endregion
    }
}
