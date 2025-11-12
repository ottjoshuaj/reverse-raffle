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

        // Particle animation fields
        private List<Particle> _particles;
        private Timer _particleTimer;
        private Random _random;

        // Name scrolling fields
        private float _namesScrollOffset;
        private Timer _namesScrollTimer;
        private float _totalNamesHeight;
        private bool _scrollPaused;
        private int _scrollPauseCounter;

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

            // Initialize particle system
            _particles = new List<Particle>();
            _random = new Random();

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
            _showCloseButton = true; // Always show close button for winner overlay

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
            _showCloseButton = true; // Always show close button for loser overlay

            Show();
        }

        /// <summary>
        /// Shows generic ticket info (for tickets that are neither winners nor losers)
        /// </summary>
        public void ShowInfo(string ticketNumber, IEnumerable<string> names, int autoCloseMs = 0, bool showCloseButton = false)
        {
            _overlayType = OverlayType.Info;
            _ticketNumbers = ticketNumber;
            _names = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            AutoCloseTimeout = autoCloseMs;
            _showCloseButton = showCloseButton || autoCloseMs == 0; // Show close button if no auto-close

            Show();
        }

        /// <summary>
        /// Shows the final raffle winner announcement with animated confetti/stars
        /// </summary>
        public void ShowRaffleWinner(string ticketNumbers, IEnumerable<string> names, int autoCloseMs = 0, bool showCloseButton = false)
        {
            _overlayType = OverlayType.RaffleWinner;
            _ticketNumbers = ticketNumbers;
            _names = names?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
            AutoCloseTimeout = autoCloseMs;
            _showCloseButton = true; // Always show close button for raffle winner overlay

            // Initialize particles for animation
            InitializeParticles();

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

            // Initialize name scrolling
            InitializeNameScrolling();

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

            _particleTimer?.Stop();
            _particleTimer?.Dispose();
            _particleTimer = null;

            _namesScrollTimer?.Stop();
            _namesScrollTimer?.Dispose();
            _namesScrollTimer = null;

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

        private void InitializeNameScrolling()
        {
            _namesScrollOffset = 0f;
            _scrollPaused = false;
            _scrollPauseCounter = 0;

            float scale = CalculateScaleFactor();

            // Calculate total height of names to determine if scrolling is needed
            _totalNamesHeight = CalculateTotalNamesHeight(scale);

            // Start scrolling timer if names exceed available space
            _namesScrollTimer?.Stop();
            _namesScrollTimer?.Dispose();

            if (_totalNamesHeight > 0) // Will check in DrawNamesList if scrolling is needed
            {
                _namesScrollTimer = new Timer { Interval = 50 }; // 20 FPS
                _namesScrollTimer.Tick += (s, e) =>
                {
                    if (_scrollPaused)
                    {
                        _scrollPauseCounter++;
                        if (_scrollPauseCounter > 40) // 2 seconds pause at 50ms intervals
                        {
                            _scrollPaused = false;
                            _scrollPauseCounter = 0;
                            _namesScrollOffset = 0f; // Reset to start
                        }
                    }
                    else
                    {
                        float scrollScale = CalculateScaleFactor();
                        _namesScrollOffset += 1.5f * scrollScale; // Scroll speed scaled
                    }
                    Invalidate();
                };
                _namesScrollTimer.Start();
            }
        }

        private float CalculateTotalNamesHeight(float scale)
        {
            if (_names == null || _names.Count == 0) return 0;

            float totalHeight = 0;
            float namesFontSize = 24f * scale;
            using (var font = new Font("Arial", namesFontSize, FontStyle.Regular))
            using (var g = CreateGraphics())
            {
                int nameCount = 0;
                foreach (var name in _names)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // Add separator height every 3 names (scaled)
                    if (nameCount > 0 && nameCount % 3 == 0)
                    {
                        totalHeight += 15 * scale; // 5 + 10 from separator, scaled
                    }

                    var textSize = g.MeasureString("A.) " + name, font);
                    totalHeight += textSize.Height + (2 * scale);
                    nameCount++;
                }
            }

            return totalHeight;
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

            // Calculate scale factor based on screen size (baseline: 1920x1080)
            float scaleFactor = CalculateScaleFactor();

            // Calculate announcement card bounds (scaled) - increased base size for better visibility
            int baseCardWidth = 900;
            int baseCardHeight = 750;
            int cardWidth = Math.Min((int)(baseCardWidth * scaleFactor), Width - (int)(100 * scaleFactor));
            int cardHeight = Math.Min((int)(baseCardHeight * scaleFactor), Height - (int)(100 * scaleFactor));
            Rectangle cardBounds = new Rectangle(
                (Width - cardWidth) / 2,
                (Height - cardHeight) / 2,
                cardWidth,
                cardHeight
            );

            // Draw the announcement based on type
            if (_overlayType == OverlayType.Winner)
                DrawWinnerAnnouncement(g, cardBounds, scaleFactor);
            else if (_overlayType == OverlayType.Loser)
                DrawLoserAnnouncement(g, cardBounds, scaleFactor);
            else if (_overlayType == OverlayType.RaffleWinner)
                DrawRaffleWinnerAnnouncement(g, cardBounds, scaleFactor);
            else
                DrawInfoAnnouncement(g, cardBounds, scaleFactor);

            // Draw close button if enabled
            if (_showCloseButton)
            {
                DrawCloseButton(g, cardBounds, scaleFactor);
            }
        }

        /// <summary>
        /// Calculates scale factor based on screen size (baseline: 1920x1080)
        /// </summary>
        private float CalculateScaleFactor()
        {
            // Use minimum dimension to ensure overlay fits
            float minDimension = Math.Min(Width, Height);
            float baselineDimension = 1080f; // Baseline height

            // Calculate scale factor (minimum 0.5x, maximum 3.0x for very large displays)
            float scale = minDimension / baselineDimension;
            return Math.Max(0.5f, Math.Min(3.0f, scale));
        }

        private void DrawWinnerAnnouncement(Graphics g, Rectangle bounds, float scale)
        {
            // Draw card background with gradient
            int cornerRadius = (int)(20 * scale);
            using (var path = CreateRoundedRectPath(bounds, cornerRadius))
            {
                // Drop shadow
                int shadowOffset = (int)(5 * scale);
                using (var shadowPath = CreateRoundedRectPath(
                    new Rectangle(bounds.X + shadowOffset, bounds.Y + shadowOffset, bounds.Width, bounds.Height), cornerRadius))
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
                int borderWidth = Math.Max(1, (int)(3 * scale));
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 200, 170, 70), borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw trophy icon (scaled)
            int iconSize = (int)(120 * scale);
            int iconTopMargin = (int)(40 * scale);
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - iconSize) / 2,
                bounds.Y + iconTopMargin,
                iconSize,
                iconSize
            );
            DrawTrophy(g, iconBounds, scale);

            // Draw "WINNER!" text (scaled)
            float titleFontSize = 42f * scale;
            using (var font = new Font("Arial", titleFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 180, 120, 0)))
            {
                var text = "WINNER!";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = iconBounds.Bottom + (20 * scale);

                // Text shadow (scaled)
                int shadowOffset = Math.Max(1, (int)(2 * scale));
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(80 * _fadeOpacity), 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + shadowOffset, textY + shadowOffset);
                }
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw ticket numbers (scaled)
            float ticketFontSize = 20f * scale;
            using (var font = new Font("Arial", ticketFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 80, 50, 0)))
            {
                var text = $"Ticket # {_ticketNumbers}";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + (270 * scale);
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw names (scaled)
            DrawNamesList(g, bounds, (int)(330 * scale), scale);
        }

        private void DrawRaffleWinnerAnnouncement(Graphics g, Rectangle bounds, float scale)
        {
            // Draw particles BEHIND the card (scaled)
            DrawParticles(g, bounds, scale);

            // Draw card background with gradient (same gold theme as regular winner)
            int cornerRadius = (int)(20 * scale);
            using (var path = CreateRoundedRectPath(bounds, cornerRadius))
            {
                // Drop shadow
                int shadowOffset = (int)(5 * scale);
                using (var shadowPath = CreateRoundedRectPath(
                    new Rectangle(bounds.X + shadowOffset, bounds.Y + shadowOffset, bounds.Width, bounds.Height), cornerRadius))
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(100 * _fadeOpacity), 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Card gradient (gold/yellow theme)
                using (var brush = new LinearGradientBrush(bounds,
                    Color.FromArgb((int)(255 * _fadeOpacity), 255, 245, 200),
                    Color.FromArgb((int)(255 * _fadeOpacity), 255, 235, 150),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }

                // Border
                int borderWidth = Math.Max(1, (int)(3 * scale));
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 200, 170, 70), borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw trophy icon (scaled)
            int iconSize = (int)(120 * scale);
            int iconTopMargin = (int)(40 * scale);
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - iconSize) / 2,
                bounds.Y + iconTopMargin,
                iconSize,
                iconSize
            );
            DrawTrophy(g, iconBounds, scale);

            // Draw "Raffle Winner(s)" text (scaled)
            float titleFontSize = 36f * scale;
            using (var font = new Font("Arial", titleFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 180, 120, 0)))
            {
                var text = "Raffle Winner(s)";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = iconBounds.Bottom + (20 * scale);

                // Text shadow (scaled)
                int shadowOffset = Math.Max(1, (int)(2 * scale));
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(80 * _fadeOpacity), 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + shadowOffset, textY + shadowOffset);
                }
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw ticket numbers (scaled)
            float ticketFontSize = 20f * scale;
            using (var font = new Font("Arial", ticketFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 80, 50, 0)))
            {
                var text = $"Ticket # {_ticketNumbers}";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + (270 * scale);
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw names (scaled)
            DrawNamesList(g, bounds, (int)(330 * scale), scale);
        }

        private void DrawLoserAnnouncement(Graphics g, Rectangle bounds, float scale)
        {
            // Draw card background with gradient (gray theme for losers)
            int cornerRadius = (int)(20 * scale);
            using (var path = CreateRoundedRectPath(bounds, cornerRadius))
            {
                // Drop shadow
                int shadowOffset = (int)(5 * scale);
                using (var shadowPath = CreateRoundedRectPath(
                    new Rectangle(bounds.X + shadowOffset, bounds.Y + shadowOffset, bounds.Width, bounds.Height), cornerRadius))
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
                int borderWidth = Math.Max(1, (int)(3 * scale));
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 150, 150, 160), borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw X icon (scaled)
            int iconSize = (int)(120 * scale);
            int iconTopMargin = (int)(40 * scale);
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - iconSize) / 2,
                bounds.Y + iconTopMargin,
                iconSize,
                iconSize
            );
            DrawXIcon(g, iconBounds, scale);

            // Draw "NOT A WINNER" text (scaled)
            float titleFontSize = 32f * scale;
            using (var font = new Font("Arial", titleFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 100, 100, 110)))
            {
                var text = "NOT A WINNER";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = iconBounds.Bottom + (20 * scale);

                // Text shadow (scaled)
                int shadowOffset = Math.Max(1, (int)(2 * scale));
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(60 * _fadeOpacity), 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + shadowOffset, textY + shadowOffset);
                }
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw ticket number (scaled)
            float ticketFontSize = 20f * scale;
            using (var font = new Font("Arial", ticketFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 80, 80, 90)))
            {
                var text = $"Ticket # {_ticketNumbers}";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + (270 * scale);
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw names (scaled)
            DrawNamesList(g, bounds, (int)(330 * scale), scale);
        }

        private void DrawInfoAnnouncement(Graphics g, Rectangle bounds, float scale)
        {
            // Draw card background with gradient (blue theme for info)
            int cornerRadius = (int)(20 * scale);
            using (var path = CreateRoundedRectPath(bounds, cornerRadius))
            {
                // Drop shadow
                int shadowOffset = (int)(5 * scale);
                using (var shadowPath = CreateRoundedRectPath(
                    new Rectangle(bounds.X + shadowOffset, bounds.Y + shadowOffset, bounds.Width, bounds.Height), cornerRadius))
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(100 * _fadeOpacity), 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Card gradient (soft blue)
                using (var brush = new LinearGradientBrush(bounds,
                    Color.FromArgb((int)(255 * _fadeOpacity), 230, 240, 255),
                    Color.FromArgb((int)(255 * _fadeOpacity), 210, 225, 245),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }

                // Border
                int borderWidth = Math.Max(1, (int)(3 * scale));
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 100, 150, 200), borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw info icon (scaled)
            int iconSize = (int)(100 * scale);
            int iconTopMargin = (int)(50 * scale);
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - iconSize) / 2,
                bounds.Y + iconTopMargin,
                iconSize,
                iconSize
            );
            DrawInfoIcon(g, iconBounds, scale);

            // Draw "Ticket Information" text (scaled)
            float titleFontSize = 28f * scale;
            using (var font = new Font("Arial", titleFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 60, 100, 150)))
            {
                var text = "Ticket Information";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = iconBounds.Bottom + (20 * scale);

                // Text shadow (scaled)
                int shadowOffset = Math.Max(1, (int)(2 * scale));
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(60 * _fadeOpacity), 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + shadowOffset, textY + shadowOffset);
                }
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw ticket number (scaled)
            float ticketFontSize = 24f * scale;
            using (var font = new Font("Arial", ticketFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 40, 70, 120)))
            {
                var text = $"Ticket # {_ticketNumbers}";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + (240 * scale);
                g.DrawString(text, font, brush, textX, textY);
            }

            // Draw names (scaled)
            DrawNamesList(g, bounds, (int)(290 * scale), scale);
        }

        private void DrawNamesList(Graphics g, Rectangle bounds, int startY, float scale)
        {
            if (_names == null || _names.Count == 0) return;

            // Define the clipping region for names (leaves room for bottom hint text, scaled)
            int bottomMargin = (int)(40 * scale);
            int availableHeight = bounds.Bottom - (bounds.Y + startY) - bottomMargin;
            Rectangle namesRegion = new Rectangle(
                bounds.X,
                bounds.Y + startY,
                bounds.Width,
                availableHeight
            );

            // Check if scrolling is needed
            bool needsScrolling = _totalNamesHeight > availableHeight;

            // Set clipping region to prevent overflow
            var previousClip = g.Clip;
            g.SetClip(namesRegion);

            float namesFontSize = 24f * scale;
            using (var font = new Font("Arial", namesFontSize, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _fadeOpacity), 60, 60, 70)))
            {
                float y = bounds.Y + startY - (needsScrolling ? _namesScrollOffset : 0);
                var labels = new[] { "A", "B", "C" };
                int labelIndex = 0;
                int nameCount = 0;

                foreach (var name in _names)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // Add separator line every 3 names (scaled)
                    if (nameCount > 0 && nameCount % 3 == 0)
                    {
                        y += (5 * scale);
                        int lineWidth = Math.Max(1, (int)(1 * scale));
                        using (var linePen = new Pen(Color.FromArgb((int)(100 * _fadeOpacity), 150, 150, 160), lineWidth))
                        {
                            int lineMargin = (int)(80 * scale);
                            g.DrawLine(linePen, bounds.X + lineMargin, y, bounds.Right - lineMargin, y);
                        }
                        y += (10 * scale);
                        labelIndex = 0;
                    }

                    var text = $"{labels[labelIndex]}.) {name}";
                    var textSize = g.MeasureString(text, font);
                    float textX = bounds.X + (bounds.Width - textSize.Width) / 2;

                    g.DrawString(text, font, brush, textX, y);

                    y += textSize.Height + (2 * scale);
                    labelIndex = (labelIndex + 1) % 3;
                    nameCount++;
                }

                // Check if we've scrolled past the end (pause and restart)
                if (needsScrolling && !_scrollPaused && _namesScrollOffset > _totalNamesHeight + availableHeight)
                {
                    _scrollPaused = true;
                    _scrollPauseCounter = 0;
                }
            }

            // Restore previous clipping region
            g.Clip = previousClip;

            // Draw "Click anywhere to close" hint (scaled)
            float hintFontSize = 11f * scale;
            using (var font = new Font("Arial", hintFontSize, FontStyle.Italic))
            using (var brush = new SolidBrush(Color.FromArgb((int)(180 * _fadeOpacity), 100, 100, 110)))
            {
                var text = AutoCloseTimeout > 0 ?
                    $"Auto-closing in {AutoCloseTimeout / 1000}s (or click to close)" :
                    "Click anywhere to close";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Bottom - (30 * scale);
                g.DrawString(text, font, brush, textX, textY);
            }
        }

        private void DrawTrophy(Graphics g, Rectangle bounds, float scale)
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
                    // Cup shape (trapezoid) - scaled inset
                    int inset = (int)(10 * scale);
                    Point[] cupPoints = new Point[]
                    {
                        new Point(cupBounds.X + inset, cupBounds.Y),
                        new Point(cupBounds.Right - inset, cupBounds.Y),
                        new Point(cupBounds.Right, cupBounds.Bottom),
                        new Point(cupBounds.X, cupBounds.Bottom)
                    };
                    path.AddPolygon(cupPoints);
                    g.FillPath(brush, path);

                    // Outline (scaled)
                    int outlineWidth = Math.Max(1, (int)(3 * scale));
                    using (var pen = new Pen(Color.FromArgb(alpha, 180, 140, 0), outlineWidth))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Trophy handles (scaled)
            int handleWidth = Math.Max(2, (int)(8 * scale));
            using (var pen = new Pen(Color.FromArgb(alpha, 218, 165, 32), handleWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Left handle
                int handleMargin = (int)(10 * scale);
                int handleHeight = (int)(30 * scale);
                Point leftStart = new Point(bounds.X + bounds.Width / 4, bounds.Y + bounds.Height / 3);
                Point leftEnd = new Point(bounds.X + handleMargin, bounds.Y + bounds.Height / 2);
                g.DrawArc(pen, new Rectangle(leftEnd.X, leftStart.Y, leftStart.X - leftEnd.X, handleHeight), 90, 180);

                // Right handle
                Point rightStart = new Point(bounds.X + 3 * bounds.Width / 4, bounds.Y + bounds.Height / 3);
                Point rightEnd = new Point(bounds.Right - handleMargin, bounds.Y + bounds.Height / 2);
                g.DrawArc(pen, new Rectangle(rightStart.X, rightStart.Y, rightEnd.X - rightStart.X, handleHeight), 270, 180);
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

        private void DrawXIcon(Graphics g, Rectangle bounds, float scale)
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

            // Circle outline (scaled)
            int outlineWidth = Math.Max(1, (int)(3 * scale));
            using (var pen = new Pen(Color.FromArgb(alpha, 150, 20, 20), outlineWidth))
            {
                g.DrawEllipse(pen, bounds);
            }

            // Draw X (scaled)
            int padding = bounds.Width / 4;
            int xWidth = Math.Max(2, (int)(12 * scale));
            using (var pen = new Pen(Color.FromArgb(alpha, 255, 255, 255), xWidth))
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

        private void DrawInfoIcon(Graphics g, Rectangle bounds, float scale)
        {
            int alpha = (int)(255 * _fadeOpacity);

            // Draw circle background
            using (var brush = new LinearGradientBrush(bounds,
                Color.FromArgb(alpha, 70, 130, 220),
                Color.FromArgb(alpha, 50, 100, 180),
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(brush, bounds);
            }

            // Circle outline (scaled)
            int outlineWidth = Math.Max(1, (int)(3 * scale));
            using (var pen = new Pen(Color.FromArgb(alpha, 40, 80, 150), outlineWidth))
            {
                g.DrawEllipse(pen, bounds);
            }

            // Draw "i" letter (scaled)
            float iFontSize = 60f * scale;
            using (var font = new Font("Arial", iFontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
            {
                var text = "i";
                var textSize = g.MeasureString(text, font);
                float textX = bounds.X + (bounds.Width - textSize.Width) / 2;
                float textY = bounds.Y + (bounds.Height - textSize.Height) / 2;
                g.DrawString(text, font, brush, textX, textY);
            }
        }

        private void DrawCloseButton(Graphics g, Rectangle cardBounds, float scale)
        {
            // Draw X button in top-right corner of card (scaled)
            int buttonSize = (int)(35 * scale);
            int padding = (int)(15 * scale);
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

                // Border (scaled)
                int borderWidth = Math.Max(1, (int)(2 * scale));
                using (var borderPen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 255, 255, 255), borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw X (scaled)
            int xPadding = (int)(10 * scale);
            int xWidth = Math.Max(1, (int)(3 * scale));
            using (var pen = new Pen(Color.FromArgb((int)(255 * _fadeOpacity), 255, 255, 255), xWidth))
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

        #region Particle System

        /// <summary>
        /// Represents a confetti/star particle for animation
        /// </summary>
        private class Particle
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float VelocityX { get; set; }
            public float VelocityY { get; set; }
            public float Rotation { get; set; }
            public float RotationSpeed { get; set; }
            public ParticleType Type { get; set; }
            public Color Color { get; set; }
            public float Size { get; set; }
        }

        private enum ParticleType
        {
            Star,
            Confetti
        }

        private void InitializeParticles()
        {
            _particles.Clear();

            // Calculate scale factor
            float scale = CalculateScaleFactor();

            // Create 60 particles spread across the top (mix of stars and confetti, scaled)
            for (int i = 0; i < 60; i++)
            {
                var particle = new Particle
                {
                    X = (float)(_random.NextDouble() * Width), // Random position across entire width
                    Y = (float)(_random.NextDouble() * -200 * scale), // Start above the visible area
                    VelocityX = (float)(_random.NextDouble() * 4 * scale - 2 * scale), // scaled -2 to 2 (gentle drift)
                    VelocityY = (float)(_random.NextDouble() * 3 * scale + 2 * scale), // scaled 2 to 5 (downward)
                    Rotation = (float)(_random.NextDouble() * 360),
                    RotationSpeed = (float)(_random.NextDouble() * 10 - 5),
                    Type = _random.Next(2) == 0 ? ParticleType.Star : ParticleType.Confetti,
                    Size = (float)(_random.NextDouble() * 8 * scale + 6 * scale), // scaled 6 to 14
                    Color = GetRandomParticleColor()
                };

                _particles.Add(particle);
            }

            // Start particle animation timer
            _particleTimer?.Stop();
            _particleTimer?.Dispose();

            _particleTimer = new Timer { Interval = 30 }; // ~33 FPS
            _particleTimer.Tick += (s, e) =>
            {
                UpdateParticles();
                Invalidate();
            };
            _particleTimer.Start();
        }

        private Color GetRandomParticleColor()
        {
            var colors = new[]
            {
                Color.FromArgb(255, 215, 0),   // Gold
                Color.FromArgb(255, 140, 0),   // Dark orange
                Color.FromArgb(255, 69, 0),    // Red-orange
                Color.FromArgb(255, 255, 0),   // Yellow
                Color.FromArgb(255, 200, 50),  // Light gold
                Color.FromArgb(200, 150, 255), // Light purple
                Color.FromArgb(150, 200, 255)  // Light blue
            };

            return colors[_random.Next(colors.Length)];
        }

        private void UpdateParticles()
        {
            // Calculate scale for gravity and respawn
            float scale = CalculateScaleFactor();

            foreach (var particle in _particles)
            {
                // Update position
                particle.X += particle.VelocityX;
                particle.Y += particle.VelocityY;

                // Apply gravity (scaled)
                particle.VelocityY += 0.3f * scale;

                // Update rotation
                particle.Rotation += particle.RotationSpeed;

                // Dampen horizontal velocity slightly
                particle.VelocityX *= 0.99f;

                // Respawn particle at top if it falls off the bottom (continuous confetti)
                if (particle.Y > Height + (50 * scale))
                {
                    particle.X = (float)(_random.NextDouble() * Width); // Random position across width
                    particle.Y = (float)(_random.NextDouble() * -100 * scale); // Start above visible area
                    particle.VelocityX = (float)(_random.NextDouble() * 4 * scale - 2 * scale); // scaled -2 to 2
                    particle.VelocityY = (float)(_random.NextDouble() * 3 * scale + 2 * scale); // scaled 2 to 5 (downward)
                    particle.Rotation = (float)(_random.NextDouble() * 360);
                    particle.Color = GetRandomParticleColor();
                }
            }
        }

        private void DrawParticles(Graphics g, Rectangle cardBounds, float scale)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var particle in _particles)
            {
                // Skip particles that are outside the visible area (scaled margin)
                if (particle.Y > Height + (50 * scale)) continue;

                int alpha = (int)(255 * _fadeOpacity);
                using (var brush = new SolidBrush(Color.FromArgb(alpha, particle.Color)))
                {
                    g.TranslateTransform(particle.X, particle.Y);
                    g.RotateTransform(particle.Rotation);

                    if (particle.Type == ParticleType.Star)
                    {
                        // Draw 5-pointed star (size already scaled in particle)
                        DrawStar(g, brush, particle.Size);
                    }
                    else
                    {
                        // Draw confetti rectangle (size already scaled in particle)
                        float w = particle.Size;
                        float h = particle.Size * 2;
                        g.FillRectangle(brush, -w / 2, -h / 2, w, h);
                    }

                    g.ResetTransform();
                }
            }
        }

        private void DrawStar(Graphics g, Brush brush, float size)
        {
            // Create 5-pointed star shape
            PointF[] points = new PointF[10];
            float outerRadius = size;
            float innerRadius = size * 0.4f;

            for (int i = 0; i < 10; i++)
            {
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                float angle = (float)(i * Math.PI / 5 - Math.PI / 2);
                points[i] = new PointF(
                    (float)(radius * Math.Cos(angle)),
                    (float)(radius * Math.Sin(angle))
                );
            }

            g.FillPolygon(brush, points);
        }

        #endregion

        #region Enums

        private enum OverlayType
        {
            Winner,
            Loser,
            Info,
            RaffleWinner
        }

        #endregion
    }
}
