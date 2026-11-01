using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ReverseRaffler.Services
{
    /// <summary>
    /// What happens when a given draw number is reached.
    /// </summary>
    public enum DrawOutcome
    {
        /// <summary>Ticket is drawn and eliminated (no payout).</summary>
        NotAWinner,

        /// <summary>Ticket is drawn on a payout draw (e.g. Draw #1, #10, #20 ...).</summary>
        Payout,

        /// <summary>Ticket is drawn on the final payout draw. Stays on screen until the operator closes it.</summary>
        GrandPrize
    }

    /// <summary>
    /// List of draw numbers that pay out, e.g. "1,10,20,30,40,50,60,70,80,85".
    /// The highest draw number in the list is the Grand Prize.
    /// </summary>
    public class PayoutSchedule
    {
        private readonly SortedSet<int> _draws;

        private PayoutSchedule(SortedSet<int> draws)
        {
            _draws = draws;
        }

        public IReadOnlyCollection<int> Draws => _draws;

        public int GrandPrizeDraw => _draws.Count == 0 ? 0 : _draws.Max;

        /// <summary>
        /// Parses a comma/space separated list of draw numbers. Returns false with an error message if invalid.
        /// </summary>
        public static bool TryParse(string text, out PayoutSchedule schedule, out string error)
        {
            schedule = null;
            error = null;

            var draws = new SortedSet<int>();
            var parts = (text ?? string.Empty).Split(new[] { ',', ' ', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var token = part.Trim().TrimStart('#');
                if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var draw) || draw < 1)
                {
                    error = $"\"{part}\" is not a valid draw number. Use whole numbers like: 1,10,20,30,85";
                    return false;
                }

                draws.Add(draw);
            }

            if (draws.Count == 0)
            {
                error = "Enter at least one payout draw number (the last one is the Grand Prize).";
                return false;
            }

            schedule = new PayoutSchedule(draws);
            return true;
        }

        public DrawOutcome GetOutcome(int drawNumber)
        {
            if (drawNumber == GrandPrizeDraw) return DrawOutcome.GrandPrize;
            return _draws.Contains(drawNumber) ? DrawOutcome.Payout : DrawOutcome.NotAWinner;
        }

        /// <summary>
        /// The next payout draw at or after <paramref name="drawNumber"/>, or null if none remain.
        /// </summary>
        public int? NextPayoutAtOrAfter(int drawNumber)
        {
            foreach (var d in _draws)
                if (d >= drawNumber) return d;
            return null;
        }

        public override string ToString()
        {
            return string.Join(",", _draws);
        }
    }
}
