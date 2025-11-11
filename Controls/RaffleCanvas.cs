using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ReverseRaffler.Data;

namespace ReverseRaffler.Controls
{
    /// <summary>
    /// Custom canvas control that draws raffle tickets using GDI+ instead of creating individual controls.
    /// Provides much better performance and allows for modern visual effects.
    /// </summary>
    public class RaffleCanvas : Panel
    {
        #region Fields

        private List<TicketHolder> _tickets;
        private Dictionary<int, Rectangle> _ticketBounds;
        private int _ticketWidth;
        private int _ticketHeight;
        private int _spacing;
        private int _columnsPerRow;
        private TicketHolder _hoveredTicket;

        #endregion

        #region Properties

        /// <summary>
        /// Gets all tickets currently on the canvas
        /// </summary>
        public List<TicketHolder> Tickets => _tickets;

        /// <summary>
        /// Gets the number of tickets marked as losing
        /// </summary>
        public int TotalLosingTickets => _tickets.Count(t => t.MarkedAsNotWinner.GetValueOrDefault());

        /// <summary>
        /// Gets the number of tickets not yet drawn
        /// </summary>
        public int TotalTicketsNotDrawn => _tickets.Count - TotalLosingTickets;

        /// <summary>
        /// Gets or sets the width of each ticket
        /// </summary>
        public int TicketWidth
        {
            get => _ticketWidth;
            set
            {
                _ticketWidth = value;
                RecalculateLayout();
            }
        }

        /// <summary>
        /// Gets or sets the height of each ticket
        /// </summary>
        public int TicketHeight
        {
            get => _ticketHeight;
            set
            {
                _ticketHeight = value;
                RecalculateLayout();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a ticket is clicked
        /// </summary>
        public event EventHandler<TicketClickedEventArgs> TicketClicked;

        #endregion

        #region Constructor

        public RaffleCanvas()
        {
            _tickets = new List<TicketHolder>();
            _ticketBounds = new Dictionary<int, Rectangle>();
            _ticketWidth = 120;
            _ticketHeight = 60;
            _spacing = 10;

            // Enable double buffering for smooth rendering
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // Set background
            BackColor = Color.FromArgb(240, 240, 245);
            AutoScroll = true;

            // Wire up events
            Paint += RaffleCanvas_Paint;
            MouseClick += RaffleCanvas_MouseClick;
            MouseMove += RaffleCanvas_MouseMove;
            Resize += RaffleCanvas_Resize;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a ticket to the canvas
        /// </summary>
        public void AddTicket(TicketHolder ticket)
        {
            _tickets.Add(ticket);

            // Auto-size tickets based on total count and canvas dimensions
            CalculateOptimalTicketSize();

            RecalculateLayout();
        }

        /// <summary>
        /// Clears all tickets from the canvas
        /// </summary>
        public void Clear()
        {
            _tickets.Clear();
            _ticketBounds.Clear();
            _hoveredTicket = null;
            Invalidate();
        }

        /// <summary>
        /// Marks a ticket as losing and triggers a repaint
        /// </summary>
        public void MarkTicketAsLosing(int ticketNumber)
        {
            var ticket = _tickets.FirstOrDefault(t => t.TicketNumber == ticketNumber);
            if (ticket != null)
            {
                ticket.MarkedAsNotWinner = true;
                Invalidate(); // Trigger repaint
            }
        }

        /// <summary>
        /// Gets a ticket by its number
        /// </summary>
        public TicketHolder GetTicket(int ticketNumber)
        {
            return _tickets.FirstOrDefault(t => t.TicketNumber == ticketNumber);
        }

        #endregion

        #region Private Methods - Layout

        private void CalculateOptimalTicketSize()
        {
            if (_tickets.Count == 0 || Width <= 0 || Height <= 0) return;

            // Target aspect ratio (width:height) - raffle tickets are typically 2:1
            const double targetAspectRatio = 2.0;

            // Available dimensions (accounting for spacing)
            int availableWidth = Width - (_spacing * 2);
            int availableHeight = Height - (_spacing * 2);

            // Try different column counts to find the best fit
            int bestColumns = 1;
            int bestTicketWidth = 120;
            int bestTicketHeight = 60;
            double bestUtilization = 0;

            for (int columns = 1; columns <= Math.Min(_tickets.Count, 20); columns++)
            {
                // Calculate rows needed
                int rows = (int)Math.Ceiling((double)_tickets.Count / columns);

                // Calculate ticket dimensions for this layout
                int ticketWidth = (availableWidth - ((columns - 1) * _spacing)) / columns;
                int ticketHeight = (availableHeight - ((rows - 1) * _spacing)) / rows;

                // Apply aspect ratio constraint (maintain 2:1 ratio)
                int constrainedHeight = (int)(ticketWidth / targetAspectRatio);
                if (constrainedHeight < ticketHeight)
                {
                    ticketHeight = constrainedHeight;
                }
                else
                {
                    ticketWidth = (int)(ticketHeight * targetAspectRatio);
                }

                // Ensure minimum size for readability
                if (ticketWidth < 80 || ticketHeight < 40)
                    continue;

                // Calculate utilization (how much of the canvas is used)
                int totalWidth = (columns * ticketWidth) + ((columns - 1) * _spacing);
                int totalHeight = (rows * ticketHeight) + ((rows - 1) * _spacing);
                double utilization = ((double)totalWidth * totalHeight) / (availableWidth * availableHeight);

                // Prefer layouts with better utilization
                if (utilization > bestUtilization)
                {
                    bestUtilization = utilization;
                    bestColumns = columns;
                    bestTicketWidth = ticketWidth;
                    bestTicketHeight = ticketHeight;
                }
            }

            // Apply the best size found
            _ticketWidth = bestTicketWidth;
            _ticketHeight = bestTicketHeight;
        }

        private void RecalculateLayout()
        {
            _ticketBounds.Clear();

            if (_tickets.Count == 0) return;

            // Calculate how many tickets fit per row
            int availableWidth = Width - _spacing;
            _columnsPerRow = Math.Max(1, (availableWidth + _spacing) / (_ticketWidth + _spacing));

            int x = _spacing;
            int y = _spacing;
            int column = 0;

            foreach (var ticket in _tickets)
            {
                var bounds = new Rectangle(x, y, _ticketWidth, _ticketHeight);
                _ticketBounds[ticket.TicketNumber] = bounds;

                column++;
                if (column >= _columnsPerRow)
                {
                    // Move to next row
                    x = _spacing;
                    y += _ticketHeight + _spacing;
                    column = 0;
                }
                else
                {
                    x += _ticketWidth + _spacing;
                }
            }

            // Set AutoScrollMinSize to enable scrolling
            int totalRows = (int)Math.Ceiling((double)_tickets.Count / _columnsPerRow);
            AutoScrollMinSize = new Size(0, totalRows * (_ticketHeight + _spacing) + _spacing);

            Invalidate();
        }

        #endregion

        #region Private Methods - Drawing

        private void RaffleCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Adjust for scrolling
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            // Draw each ticket
            foreach (var ticket in _tickets)
            {
                if (_ticketBounds.ContainsKey(ticket.TicketNumber))
                {
                    Rectangle bounds = _ticketBounds[ticket.TicketNumber];
                    bool isHovered = _hoveredTicket != null && _hoveredTicket.TicketNumber == ticket.TicketNumber;
                    bool isLosing = ticket.MarkedAsNotWinner.GetValueOrDefault();

                    DrawRaffleTicket(g, bounds, ticket.TicketNumber, isLosing, isHovered);
                }
            }
        }

        private void DrawRaffleTicket(Graphics g, Rectangle bounds, int ticketNumber, bool isLosing, bool isHovered)
        {
            // Create raffle ticket appearance with perforated edge effect
            using (GraphicsPath ticketPath = CreateTicketPath(bounds))
            {
                // Draw shadow for depth
                if (!isLosing)
                {
                    using (var shadowPath = CreateTicketPath(new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width, bounds.Height)))
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                // Main ticket background with gradient
                Color startColor = isLosing ? Color.FromArgb(200, 200, 200) : (isHovered ? Color.FromArgb(255, 250, 230) : Color.FromArgb(255, 255, 245));
                Color endColor = isLosing ? Color.FromArgb(150, 150, 150) : (isHovered ? Color.FromArgb(255, 245, 210) : Color.FromArgb(250, 250, 235));

                using (var brush = new LinearGradientBrush(bounds, startColor, endColor, LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, ticketPath);
                }

                // Border
                Color borderColor = isLosing ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 140, 70);
                using (var borderPen = new Pen(borderColor, 2))
                {
                    g.DrawPath(borderPen, ticketPath);
                }

                // Draw perforated edge line (tear-off stub effect)
                DrawPerforatedLine(g, new Point(bounds.Left, bounds.Top + bounds.Height / 4),
                                      new Point(bounds.Right, bounds.Top + bounds.Height / 4),
                                      isLosing ? Color.FromArgb(120, 120, 120) : Color.FromArgb(200, 160, 90));

                // Calculate font sizes based on ticket dimensions for better scaling
                float labelFontSize = Math.Max(6f, bounds.Height * 0.12f);
                float numberFontSize = Math.Max(12f, bounds.Height * 0.35f);
                if (isHovered) numberFontSize *= 1.1f;

                // Draw "TICKET" label at top
                using (var labelFont = new Font("Arial", labelFontSize, FontStyle.Bold))
                using (var labelBrush = new SolidBrush(isLosing ? Color.FromArgb(100, 100, 100) : Color.FromArgb(150, 120, 60)))
                {
                    var labelSize = g.MeasureString("TICKET", labelFont);
                    var labelX = bounds.X + (bounds.Width - labelSize.Width) / 2;
                    var labelY = bounds.Y + Math.Max(3, bounds.Height * 0.08f);
                    g.DrawString("TICKET", labelFont, labelBrush, labelX, labelY);
                }

                // Draw ticket number (with more padding below the "TICKET" label)
                string ticketText = ticketNumber.ToString();
                using (var font = new Font("Arial", numberFontSize, FontStyle.Bold))
                using (var textBrush = new SolidBrush(isLosing ? Color.FromArgb(80, 80, 80) : Color.FromArgb(60, 40, 20)))
                {
                    var textSize = g.MeasureString(ticketText, font);
                    var textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                    var textY = bounds.Y + (bounds.Height - textSize.Height) / 2 + Math.Max(8, bounds.Height * 0.1f);
                    g.DrawString(ticketText, font, textBrush, textX, textY);
                }

                // Draw red X over losing tickets
                if (isLosing)
                {
                    DrawRedX(g, bounds);
                }
            }
        }

        private GraphicsPath CreateTicketPath(Rectangle bounds)
        {
            GraphicsPath path = new GraphicsPath();
            int cornerRadius = 4;

            // Create rounded rectangle path
            path.AddArc(bounds.X, bounds.Y, cornerRadius, cornerRadius, 180, 90);
            path.AddArc(bounds.Right - cornerRadius, bounds.Y, cornerRadius, cornerRadius, 270, 90);
            path.AddArc(bounds.Right - cornerRadius, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void DrawPerforatedLine(Graphics g, Point start, Point end, Color color)
        {
            using (var pen = new Pen(color, 1))
            {
                pen.DashStyle = DashStyle.Dot;
                g.DrawLine(pen, start, end);
            }
        }

        private void DrawRedX(Graphics g, Rectangle bounds)
        {
            // Draw semi-transparent overlay
            using (var overlayBrush = new SolidBrush(Color.FromArgb(100, 255, 0, 0)))
            using (var ticketPath = CreateTicketPath(bounds))
            {
                g.FillPath(overlayBrush, ticketPath);
            }

            // Draw bold red X
            int padding = 10;
            using (var pen = new Pen(Color.FromArgb(220, 0, 0), 5))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Draw X
                g.DrawLine(pen,
                    bounds.Left + padding, bounds.Top + padding,
                    bounds.Right - padding, bounds.Bottom - padding);
                g.DrawLine(pen,
                    bounds.Right - padding, bounds.Top + padding,
                    bounds.Left + padding, bounds.Bottom - padding);
            }
        }

        #endregion

        #region Private Methods - Mouse Interaction

        private void RaffleCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Adjust for scrolling
            Point scrolledPoint = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);

            // Find ticket under mouse
            TicketHolder newHovered = null;
            foreach (var kvp in _ticketBounds)
            {
                if (kvp.Value.Contains(scrolledPoint))
                {
                    newHovered = _tickets.FirstOrDefault(t => t.TicketNumber == kvp.Key);
                    break;
                }
            }

            // Update hover state
            if (newHovered != _hoveredTicket)
            {
                _hoveredTicket = newHovered;
                Cursor = newHovered != null ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        private void RaffleCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            // Adjust for scrolling
            Point scrolledPoint = new Point(e.X - AutoScrollPosition.X, e.Y - AutoScrollPosition.Y);

            // Find clicked ticket
            foreach (var kvp in _ticketBounds)
            {
                if (kvp.Value.Contains(scrolledPoint))
                {
                    var ticket = _tickets.FirstOrDefault(t => t.TicketNumber == kvp.Key);
                    if (ticket != null)
                    {
                        TicketClicked?.Invoke(this, new TicketClickedEventArgs(ticket));
                    }
                    break;
                }
            }
        }

        private void RaffleCanvas_Resize(object sender, EventArgs e)
        {
            // Recalculate optimal ticket size when canvas is resized
            CalculateOptimalTicketSize();
            RecalculateLayout();
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Event arguments for ticket click events
    /// </summary>
    public class TicketClickedEventArgs : EventArgs
    {
        public TicketHolder Ticket { get; }

        public TicketClickedEventArgs(TicketHolder ticket)
        {
            Ticket = ticket;
        }
    }

    #endregion
}
