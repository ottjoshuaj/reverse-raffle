using System;
using System.Drawing;
using System.Windows.Forms;

namespace ReverseRaffler.Controls
{
    /// <summary>
    /// On-screen number pad for entering drawn ticket numbers.
    /// Works with mouse/touch clicks and with the keyboard (number row, numpad, Backspace, Esc/Delete, Enter)
    /// via <see cref="HandleKey"/>, which the hosting form calls from ProcessCmdKey.
    /// </summary>
    public class NumberPad : UserControl
    {
        private const int MaxDigits = 4;

        private static readonly Color PadBack = Color.FromArgb(30, 32, 38);
        private static readonly Color KeyBack = Color.FromArgb(52, 56, 66);
        private static readonly Color KeyHover = Color.FromArgb(70, 76, 90);
        private static readonly Color KeyFore = Color.White;
        private static readonly Color EnterBack = Color.FromArgb(0, 140, 90);
        private static readonly Color EnterHover = Color.FromArgb(0, 170, 110);
        private static readonly Color DisplayBack = Color.FromArgb(14, 15, 18);

        private readonly Label _display;
        private readonly Label _message;
        private readonly Button _enterButton;
        private readonly Timer _flashTimer;
        private string _value = string.Empty;

        /// <summary>Raised when the operator presses Enter with a number typed in.</summary>
        public event EventHandler<int> NumberSubmitted;

        public NumberPad()
        {
            DoubleBuffered = true;
            BackColor = PadBack;
            Padding = new Padding(12);
            MinimumSize = new Size(300, 460);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                BackColor = PadBack,
                Margin = Padding.Empty
            };
            for (int c = 0; c < 3; c++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));   // display
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // message
            for (int r = 0; r < 4; r++)
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20f)); // digit rows
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));     // enter

            _display = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 44f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = DisplayBack,
                Margin = new Padding(4)
            };
            layout.Controls.Add(_display, 0, 0);
            layout.SetColumnSpan(_display, 3);

            _message = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.Silver,
                BackColor = PadBack,
                Margin = new Padding(4, 0, 4, 0)
            };
            layout.Controls.Add(_message, 0, 1);
            layout.SetColumnSpan(_message, 3);

            string[,] keys =
            {
                { "7", "8", "9" },
                { "4", "5", "6" },
                { "1", "2", "3" },
                { "⌫", "0", "CLR" }
            };

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var key = keys[r, c];
                    var btn = CreateKey(key, KeyBack, KeyHover, key.Length == 1 && char.IsDigit(key[0]) ? 26f : 18f);
                    btn.Click += (s, e) => OnPadKey(key);
                    layout.Controls.Add(btn, c, r + 2);
                }
            }

            _enterButton = CreateKey("ENTER", EnterBack, EnterHover, 22f);
            _enterButton.Click += (s, e) => Submit();
            layout.Controls.Add(_enterButton, 0, 6);
            layout.SetColumnSpan(_enterButton, 3);

            Controls.Add(layout);

            _flashTimer = new Timer { Interval = 4000 };
            _flashTimer.Tick += (s, e) => { _flashTimer.Stop(); ShowIdleMessage(); };

            RefreshDisplay();
            ShowIdleMessage();
        }

        /// <summary>Text shown under the display when there is nothing else to say.</summary>
        public string IdleMessage { get; set; } = "Type a ticket number, then ENTER";

        /// <summary>The digits currently typed in.</summary>
        public string Value => _value;

        private Button CreateKey(string text, Color back, Color hover, float fontSize)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = KeyFore,
                Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
                TabStop = false,      // keep keyboard focus off the keys so Enter/Space never "re-click" one
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = hover;
            return btn;
        }

        private void OnPadKey(string key)
        {
            if (!Enabled) return;

            switch (key)
            {
                case "CLR": Clear(); break;
                case "⌫": Backspace(); break;
                default: AppendDigit(key[0]); break;
            }
        }

        /// <summary>
        /// Handles a keyboard key. Returns true if the key was consumed.
        /// </summary>
        public bool HandleKey(Keys keyData)
        {
            if (!Enabled) return false;

            // Ignore Ctrl/Alt combos so shortcuts still work
            if ((keyData & (Keys.Control | Keys.Alt)) != 0) return false;

            var key = keyData & Keys.KeyCode;

            if (key >= Keys.D0 && key <= Keys.D9 && (keyData & Keys.Shift) == 0)
            {
                AppendDigit((char)('0' + (key - Keys.D0)));
                return true;
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                AppendDigit((char)('0' + (key - Keys.NumPad0)));
                return true;
            }

            switch (key)
            {
                case Keys.Back:
                    Backspace();
                    return true;
                case Keys.Escape:
                case Keys.Delete:
                    Clear();
                    return true;
                case Keys.Enter:
                    Submit();
                    return true;
            }

            return false;
        }

        public void AppendDigit(char digit)
        {
            if (_value.Length >= MaxDigits) return;
            if (_value.Length == 0 && digit == '0') return; // no leading zeros / ticket #0

            _value += digit;
            RefreshDisplay();
        }

        public void Backspace()
        {
            if (_value.Length == 0) return;
            _value = _value.Substring(0, _value.Length - 1);
            RefreshDisplay();
        }

        public void Clear()
        {
            _value = string.Empty;
            RefreshDisplay();
        }

        public void Submit()
        {
            if (!Enabled) return;

            if (!int.TryParse(_value, out var number) || number <= 0)
            {
                ShowMessage("Type a ticket number first", Color.Goldenrod);
                return;
            }

            Clear();
            NumberSubmitted?.Invoke(this, number);
        }

        /// <summary>Shows a status message under the display for a few seconds.</summary>
        public void ShowMessage(string text, Color color)
        {
            _message.Text = text;
            _message.ForeColor = color;
            _flashTimer.Stop();
            _flashTimer.Start();
        }

        public void ShowIdleMessage()
        {
            _message.Text = Enabled ? IdleMessage : "Begin the raffle to enter tickets";
            _message.ForeColor = Color.Silver;
        }

        private void RefreshDisplay()
        {
            _display.Text = _value.Length == 0 ? "–" : _value;
            _display.ForeColor = _value.Length == 0 ? Color.FromArgb(90, 95, 105) : Color.White;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled) Clear();
            _enterButton.BackColor = Enabled ? EnterBack : KeyBack;
            ShowIdleMessage();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _flashTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
