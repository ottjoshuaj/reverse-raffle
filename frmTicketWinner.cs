using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Telerik.WinControls.UI;

namespace ReverseRaffler
{
    public partial class frmTicketWinner : RadForm
    {
        public frmTicketWinner(string ticketNumber, IEnumerable<string> namesonticket)
        {
            InitializeComponent();

            lblTicketDisplay.Text = string.Format("Ticket # {0}", ticketNumber);

            var nameList = string.Empty;
            var cnt = 0;
            foreach(var name in namesonticket)
            {
                if (cnt == 3)
                {
                    nameList = nameList + "<p>--------------------------------------</p>";
                    cnt = 0;
                }

                nameList = nameList + FormatNameLine(cnt, name);
                cnt++;
            }

            lblNamesOnTicket.Text = string.Format(@"<html>{0}</html>", nameList);


            //Setup a timer that will auto close the form
            if (Properties.Settings.Default.EnableAutoCloseWinningTicketWindow == CheckState.Checked)
            {
                var displayTimer = new Timer();
                displayTimer.Tick += displayTimer_Tick;
                displayTimer.Interval = (Convert.ToInt32(Properties.Settings.Default.AutoCloseWinningTicketWindowAfter) * 1000);
                displayTimer.Enabled = true;
                displayTimer.Start();
            }
        }

        string FormatNameLine(int count, string name)
        {
            var strAlphaBet = new[]
            {
                "A", "B", "C"
            };

            return string.Format("<p>{0}.) {1}</p>", strAlphaBet[count], name);
        }

        void displayTimer_Tick(object sender, EventArgs e)
        {
            var displayTimer = sender as Timer;
            if (displayTimer == null) return;

            displayTimer.Enabled = false;
            displayTimer.Tick -= null;
            displayTimer.Stop();

            displayTimer.Dispose();

            //Close this form
            Hide();
            Close();
        }
    }
}
