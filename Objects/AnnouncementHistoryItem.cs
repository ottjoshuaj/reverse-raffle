using System;
using System.Collections.Generic;

namespace ReverseRaffler.Objects
{
    /// <summary>
    /// Represents a winner or loser announcement in the raffle history
    /// </summary>
    public class AnnouncementHistoryItem
    {
        /// <summary>
        /// Type of announcement
        /// </summary>
        public AnnouncementType Type { get; set; }

        /// <summary>
        /// Ticket number(s) - comma-separated for multiple winners
        /// </summary>
        public string TicketNumbers { get; set; }

        /// <summary>
        /// Names on the ticket(s)
        /// </summary>
        public List<string> Names { get; set; }

        /// <summary>
        /// Timestamp when this announcement was made
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Order in which this announcement was made (for navigation)
        /// </summary>
        public int SequenceNumber { get; set; }

        public AnnouncementHistoryItem()
        {
            Names = new List<string>();
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            string typeText = Type == AnnouncementType.Winner ? "Winner" : "Loser";
            return $"{typeText} - Ticket(s) {TicketNumbers} at {Timestamp:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Type of announcement
    /// </summary>
    public enum AnnouncementType
    {
        Winner,
        Loser
    }
}
