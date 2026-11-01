using System.Collections.Generic;

namespace ReverseRaffler.Data
{
    /// <summary>
    /// Helpers for the (up to) four names on a ticket: A, B, C and D.
    /// Kept separate from the EF-generated TicketHolder.cs so regenerating the model doesn't wipe it.
    /// </summary>
    public partial class TicketHolder
    {
        public static readonly string[] NameLetters = { "A", "B", "C", "D" };

        /// <summary>
        /// Non-empty names on the ticket, prefixed with their designation, e.g. "A - John Smith".
        /// </summary>
        public List<string> GetDesignatedNames()
        {
            var raw = new[] { TicketHolder1, TicketHolder2, TicketHolder3, TicketHolder4 };
            var result = new List<string>();

            for (int i = 0; i < raw.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(raw[i])) continue;
                result.Add($"{NameLetters[i]} - {raw[i].Trim()}");
            }

            return result;
        }
    }
}
