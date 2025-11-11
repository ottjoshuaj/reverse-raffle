using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ReverseRaffler.Controls
{
    /// <summary>
    /// Overlay panel that displays winner/loser announcements with graphics drawn on a canvas
    /// </summary>
    public class RaffleOverlay : Panel
    {
        #region Fields

        private OverlayType _overlayType;
        private string _ticketNumbers;
        private List<string> _names;
        private Timer _autoCloseTimer;
        private bool _isVisible;
        private float _fadeOpacity;
        private bool _showCloseButton;
        private Rectangle _closeButtonBounds;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the auto-close timeout in milliseconds (0 = no auto-close)
        /// </summary>
        public int AutoCloseTimeout { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the overlay is closed
        /// </summary>
        public event EventHandler OverlayClosed;

        #endregion

        #region Constructor

        public RaffleOverlay()
        {
            // Setup for transparency and painting
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Visible = false;
            Dock = DockStyle.Fill;
            _fadeOpacity = 0f;

            Paint += RaffleOverlay_Paint;
            Click += RaffleOverlay_Click;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a winner announcement
        /// </summary>
        public void ShowWinner(string ticketNumbers, IEnumerable<string> names, int autoCloseMs = 0, bool showCloseButton = false)
        {
            _overlayType = OverlayType.Winner;
            _ticketNumbers = ticketNumbers;
            _names = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            AutoCloseTimeout = autoCloseMs;
            _showCloseButton = showCloseButton || autoCloseMs == 0; // Show close button if no auto-close

            Show();
        }

        /// <summary>
        /// Shows a loser announcement
        /// </summary>
        public void ShowLoser(string ticketNumber, IEnumerable<string> names, int autoCloseMs = 0, bool showCloseButton = false)
        {
            _overlayType = OverlayType.Loser;
            _ticketNumbers = ticketNumber;
            _names = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            AutoCloseTimeout = autoCloseMs;
            _showCloseButton = showCloseButton || autoCloseMs == 0; // Show close button if no auto-close

            Show();
        }

        /// <summary>
        /// Shows the overlay
        /// </summary>
        public new void Show()
        {
            _isVisible = true;
            Visible = true;
            BringToFront();

            // Fade in animation
            FadeIn();

            // Setup auto-close timer
            if (AutoCloseTimeout > 0)
            {
                _autoCloseTimer?.Stop();
                _autoCloseTimer?.Dispose();

                _autoCloseTimer = new Timer();
                _autoCloseTimer.Interval = AutoCloseTimeout;
                _autoCloseTimer.Tick += (s, e) => { Close(); };
                _autoCloseTimer.Start();
            }

            Invalidate();
        }

        /// <summary>
        /// Closes the overlay
        /// </summary>
        public void Close()
        {
            _autoCloseTimer?.Stop();
            _autoCloseTimer?.Dispose();

            _isVisible = false;
            Visible = false;

            OverlayClosed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Private Methods - Animation

        private void FadeIn()
        {
            _fadeOpacity = 0f;
            var fadeTimer = new Timer { Interval = 20 };
            fadeTimer.Tick += (s, e) =>
            {
                _fadeOpacity += 0.1f;
                if (_fadeOpacity >= 1f)
                {
                    _fadeOpacity = 1f;
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                }
                Invalidate();
            };
            fadeTimer.Start();
        }

        #endregion

        #region Private Methods - Drawing

        private void RaffleOverlay_Paint(object sender, PaintEventArgs e)
        {
            if (!_isVisible) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw semi-transparent background
            using (var bgBrush = new SolidBrush(Color.FromArgb((int)(180 * _fadeOpacity), 0, 0, 0)))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Calculate announcement card bounds
            int cardWidth = Math.Min(600, Width - 100);
            int cardHeight = Math.Min(500, Height - 100);
            Rectangle cardBounds = new Rectangle(
                (Width - cardWidth) / 2,
                (Height - cardHeight) / 2,
                cardWidth,
                cardHeight
            );

            // Draw the announcement based on type
            if (_overlayType == OverlayType.Winner)
                DrawWinnerAnnouncement(g, cardBounds);
            else
                DrawLoserAnnouncement(g, cardBounds);

            // Draw close button if enabled
            if (_showCloseButton)
            {
                DrawCloseButton(g, cardBounds);
            }
        }

        private void DrawWinnerAnnouncement(Graphics g, Rectangle bounds)
        {
            // Draw card background with gradient
            using (var path = CreateRoundedRectPath(bounds, 20))
            {
                // Drop shadow
                using (var shadowPath = CreateRoundedRectPath(
                    new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width, bounds.Height), 20))
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(100 * _fadeOpacity), 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Card gradient (gold/yellow theme for winners)
                using (var brush = new LinearGradientBrush(bounds,
                    Color.FromArgb((int)(255 * _fadeOpacity), 255, 245, 200),
                    Color.FromArgb((int)(255 * _fadeOpacity), 255, 235, 150),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }

                // Border
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 200, 170, 70), 3))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw trophy icon
            int iconSize = 120;
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - iconSize) / 2,
                bounds.Y + 40,
                iconSize,
                iconSize
            );
            DrawTrophy(g, iconBounds);

            // Draw "WINNER!" text
            using (var font = new Font("Arial", 42f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 180, 120, 0)))
            {
                var text = "WINNER!";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = iconBounds.Bottom + 20;

                // Text shadow
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(80 * _fadeOpacity), 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + 2, textY + 2);
                }
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw ticket numbers
            using (var font = new Font("Arial", 20f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 80, 50, 0)))
            {
                var text = $"Ticket # {_ticketNumbers}";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + 220;
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw names (with more padding from ticket number)
            DrawNamesList(g, bounds, 280);
        }

        private void DrawLoserAnnouncement(Graphics g, Rectangle bounds)
        {
            // Draw card background with gradient (gray theme for losers)
            using (var path = CreateRoundedRectPath(bounds, 20))
            {
                // Drop shadow
                using (var shadowPath = CreateRoundedRectPath(
                    new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width, bounds.Height), 20))
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(100 * _fadeOpacity), 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Card gradient
                using (var brush = new LinearGradientBrush(bounds,
                    Color.FromArgb((int)(255 * _fadeOpacity), 240, 240, 245),
                    Color.FromArgb((int)(255 * _fadeOpacity), 220, 220, 230),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }

                // Border
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 150, 150, 160), 3))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw X icon
            int iconSize = 120;
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - iconSize) / 2,
                bounds.Y + 40,
                iconSize,
                iconSize
            );
            DrawXIcon(g, iconBounds);

            // Draw "NOT A WINNER" text
            using (var font = new Font("Arial", 32f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 100, 100, 110)))
            {
                var text = "NOT A WINNER";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = iconBounds.Bottom + 20;

                // Text shadow
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(60 * _fadeOpacity), 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + 2, textY + 2);
                }
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw ticket number
            using (var font = new Font("Arial", 20f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 80, 80, 90)))
            {
                var text = $"Ticket # {_ticketNumbers}";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + 220;
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw names
            DrawNamesList(g, bounds, 260);
        }

        private void DrawNamesList(Graphics g, Rectangle bounds, int startY)
        {
            if (_names == null || _names.Count == 0) return;

            using (var font = new Font("Arial", 14f, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 60, 60, 70)))
            {
                float y = bounds.Y + startY;
                var labels = new[] { "A", "B", "C" };
                int labelIndex = 0;
                int nameCount = 0;

                foreach (var name in _names)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // Add separator line every 3 names
                    if (nameCount > 0 && nameCount % 3 == 0)
                    {
                        y += 5;
                        using (var linePen = new Pen(Color.FromArgb((int)(100 * _fadeOpacity), 150, 150, 160), 1))
                        {
                            g.DrawLine(linePen, bounds.X + 80, y, bounds.Right - 80, y);
                        }
                        y += 10;
                        labelIndex = 0;
                    }

                    var text = $"{labels[labelIndex]}.) {name}";
                    var textSize = g.MeasureString(text, font);
                    float textX = bounds.X + (bounds.Width - textSize.Width) / 2;

                    g.DrawString(text, font, brush, textX, y);

                    y += textSize.Height + 2;
                    labelIndex = (labelIndex + 1) % 3;
                    nameCount++;

                    // Prevent overflow
                    if (y > bounds.Bottom - 40) break;
                }
            }

            // Draw "Click anywhere to close" hint
            using (var font = new Font("Arial", 11f, FontStyle.Italic))
            using (var brush = new SolidBrush(Color.FromArgb((int)(180 * _fadeOpacity), 100, 100, 110)))
            {
                var text = AutoCloseTimeout > 0 ?
                    $"Auto-closing in {AutoCloseTimeout / 1000}s (or click to close)" :
                    "Click anywhere to close";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Bottom - 30;
                g.DrawString(text, font, brush, textX, textY);
            }
        }

        private void DrawTrophy(Graphics g, Rectangle bounds)
        {
            int alpha = (int)(255 * _fadeOpacity);

            // Trophy cup (main body)
            using (var brush = new LinearGradientBrush(bounds,
                Color.FromArgb(alpha, 255, 215, 0),   // Gold
                Color.FromArgb(alpha, 218, 165, 32),  // Darker gold
                LinearGradientMode.Vertical))
            {
                Rectangle cupBounds = new Rectangle(
                    bounds.X + bounds.Width / 4,
                    bounds.Y + bounds.Height / 4,
                    bounds.Width / 2,
                    bounds.Height / 2
                );

                using (var path = new GraphicsPath())
                {
                    // Cup shape (trapezoid)
                    Point[] cupPoints = new Point[]
                    {
                        new Point(cupBounds.X + 10, cupBounds.Y),
                        new Point(cupBounds.Right - 10, cupBounds.Y),
                        new Point(cupBounds.Right, cupBounds.Bottom),
                        new Point(cupBounds.X, cupBounds.Bottom)
                    };
                    path.AddPolygon(cupPoints);
                    g.FillPath(brush, path);

                    // Outline
                    using (var pen = new Pen(Color.FromArgb(alpha, 180, 140, 0), 3))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Trophy handles
            using (var pen = new Pen(Color.FromArgb(alpha, 218, 165, 32), 8))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Left handle
                Point leftStart = new Point(bounds.X + bounds.Width / 4, bounds.Y + bounds.Height / 3);
                Point leftEnd = new Point(bounds.X + 10, bounds.Y + bounds.Height / 2);
                g.DrawArc(pen, new Rectangle(leftEnd.X, leftStart.Y, leftStart.X - leftEnd.X, 30), 90, 180);

                // Right handle
                Point rightStart = new Point(bounds.X + 3 * bounds.Width / 4, bounds.Y + bounds.Height / 3);
                Point rightEnd = new Point(bounds.Right - 10, bounds.Y + bounds.Height / 2);
                g.DrawArc(pen, new Rectangle(rightStart.X, rightStart.Y, rightEnd.X - rightStart.X, 30), 270, 180);
            }

            // Trophy base
            using (var brush = new SolidBrush(Color.FromArgb(alpha, 180, 140, 0)))
            {
                Rectangle baseBounds = new Rectangle(
                    bounds.X + bounds.Width / 3,
                    bounds.Y + 3 * bounds.Height / 4,
                    bounds.Width / 3,
                    bounds.Height / 6
                );
                g.FillRectangle(brush, baseBounds);
            }

            // Trophy stem
            using (var brush = new SolidBrush(Color.FromArgb(alpha, 200, 160, 20)))
            {
                Rectangle stemBounds = new Rectangle(
                    bounds.X + 2 * bounds.Width / 5,
                    bounds.Y + 2 * bounds.Height / 3,
                    bounds.Width / 5,
                    bounds.Height / 8
                );
                g.FillRectangle(brush, stemBounds);
            }
        }

        private void DrawXIcon(Graphics g, Rectangle bounds)
        {
            int alpha = (int)(255 * _fadeOpacity);

            // Draw circle background
            using (var brush = new LinearGradientBrush(bounds,
                Color.FromArgb(alpha, 220, 50, 50),
                Color.FromArgb(alpha, 180, 30, 30),
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(brush, bounds);
            }

            // Circle outline
            using (var pen = new Pen(Color.FromArgb(alpha, 150, 20, 20), 3))
            {
                g.DrawEllipse(pen, bounds);
            }

            // Draw X
            int padding = bounds.Width / 4;
            using (var pen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 12))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen,
                    bounds.Left + padding, bounds.Top + padding,
                    bounds.Right - padding, bounds.Bottom - padding);
                g.DrawLine(pen,
                    bounds.Right - padding, bounds.Top + padding,
                    bounds.Left + padding, bounds.Bottom - padding);
            }
        }

        private void DrawCloseButton(Graphics g, Rectangle cardBounds)
        {
            // Draw X button in top-right corner of card
            int buttonSize = 35;
            int padding = 15;
            _closeButtonBounds = new Rectangle(
                cardBounds.Right - buttonSize - padding,
                cardBounds.Top + padding,
                buttonSize,
                buttonSize
            );

            // Draw circle background
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(_closeButtonBounds);

                // Semi-transparent background
                using (var bgBrush = new SolidBrush(Color.FromArgb((int)(200 * _fadeOpacity), 100, 100, 110)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Border
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 255, 255, 255), 2))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw X
            int xPadding = 10;
            using (var pen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 255, 255, 255), 3))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen,
                    _closeButtonBounds.Left + xPadding, _closeButtonBounds.Top + xPadding,
                    _closeButtonBounds.Right - xPadding, _closeButtonBounds.Bottom - xPadding);
                g.DrawLine(pen,
                    _closeButtonBounds.Right - xPadding, _closeButtonBounds.Top + xPadding,
                    _closeButtonBounds.Left + xPadding, _closeButtonBounds.Bottom - xPadding);
            }
        }

        private GraphicsPath CreateRoundedRectPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        #endregion

        #region Event Handlers

        private void RaffleOverlay_Click(object sender, EventArgs e)
        {
            // If close button is shown and was clicked, close the overlay
            if (_showCloseButton && e is MouseEventArgs mouseArgs)
            {
                if (_closeButtonBounds.Contains(mouseArgs.Location))
                {
                    Close();
                    return;
                }
            }

            // Otherwise, clicking anywhere closes the overlay
            Close();
        }

        #endregion

        #region Enums

        private enum OverlayType
        {
            Winner,
            Loser
        }

        #endregion
    }
}
