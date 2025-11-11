using System.Collections.Generic;

using Telerik.WinControls.UI;

namespace ReverseRaffler
{
    public partial class frmTicketDetails : RadForm
    {
        public frmTicketDetails(int ticketNumber, IEnumerable<string> namesonticket)
        {
            InitializeComponent();

            lblTicketDisplay.Text = string.Format("Ticket # {0}", ticketNumber);

            var nameList = string.Empty;
            var cnt = 0;
            foreach (var name in namesonticket)
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
        }

        string FormatNameLine(int count, string name)
        {
            var strAlphaBet = new[]
            {
                "A", "B", "C"
            };

            return string.Format("<p>{0}.) {1}</p>", strAlphaBet[count], name);
        }
    }
}
