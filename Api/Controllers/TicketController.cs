using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ReverseRaffler.Api.Controllers
{
    public class TicketController : ApiController
    {
        public static int currentTicketNumber = 0;
        public static Action<string> LogActivity;

        public bool Get(int number)
        {
            TicketController.currentTicketNumber = number;

            // Log the API call
            LogActivity?.Invoke($"API Request: Ticket #{number} submitted");

            return true;
        }
    }
}
