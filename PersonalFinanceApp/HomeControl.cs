using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class HomeControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly AccountService _accountService = new AccountService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color AccentColor => AppTheme.AccentColor;

        private Label lblWelcome = new Label();
        private Label lblWalletAmount = new Label();
        private Label lblSafeAmount = new Label();
        private Label lblStatus = new Label();

        public HomeControl(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
            RefreshBalances();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            lblWelcome.Text = $"Hoş geldin, {DisplayName}";
            lblWelcome.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblWelcome.ForeColor = TextLight;
            lblWelcome.AutoSize = true;
            lblWelcome.Left = 20;
            lblWelcome.Top = 20;

            Panel pnlWallet = new Panel { Left = 20, Top = 100, Width = 320, Height = 240 };
            SetupSmoothContainer(pnlWallet, 16, CardBackColor);
            Label lblWalletIcon = new Label { Text = "💳", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true, BackColor = Color.Transparent };
            Label lblWalletTitle = new Label { Text = "Cüzdan", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true, BackColor = Color.Transparent };
            lblWalletAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblWalletAmount.ForeColor = Color.FromArgb(120, 220, 150);
            lblWalletAmount.Left = 20;
            lblWalletAmount.Top = 165;
            lblWalletAmount.AutoSize = true;
            lblWalletAmount.BackColor = Color.Transparent;
            EnableDoubleBuffering(lblWalletAmount);
            pnlWallet.Controls.Add(lblWalletIcon);
            pnlWallet.Controls.Add(lblWalletTitle);
            pnlWallet.Controls.Add(lblWalletAmount);

            Panel pnlSafe = new Panel { Left = 360, Top = 100, Width = 320, Height = 240 };
            SetupSmoothContainer(pnlSafe, 16, CardBackColor);
            Label lblSafeIcon = new Label { Text = "🏦", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true, BackColor = Color.Transparent };
            Label lblSafeTitle = new Label { Text = "Kasa", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true, BackColor = Color.Transparent };
            lblSafeAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblSafeAmount.ForeColor = Color.FromArgb(120, 180, 255);
            lblSafeAmount.Left = 20;
            lblSafeAmount.Top = 165;
            lblSafeAmount.AutoSize = true;
            lblSafeAmount.BackColor = Color.Transparent;
            EnableDoubleBuffering(lblSafeAmount);
            pnlSafe.Controls.Add(lblSafeIcon);
            pnlSafe.Controls.Add(lblSafeTitle);
            pnlSafe.Controls.Add(lblSafeAmount);

            // Butonlar, Kasa kutucuğunun sağ kenarıyla hizalı, kutucukların hemen altında (sağ alt)
            const int cardsRight = 360 + 320; // pnlSafe.Left + pnlSafe.Width
            const int buttonsTop = 100 + 240 + 16; // kutucukların altı + küçük boşluk
            const int btnWidth = 190;
            const int btnGap = 14;

            Button btnTransfer = new Button
            {
                Text = "Transfer Et",
                Left = cardsRight - (btnWidth * 2 + btnGap),
                Top = buttonsTop,
                Width = btnWidth,
                Height = 42,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F)
            };
            SetupRoundedButton(btnTransfer, AccentColor, Color.White);
            btnTransfer.Click += BtnTransfer_Click;

            Button btnHistory = new Button
            {
                Text = "Transfer Geçmişi",
                Left = cardsRight - btnWidth,
                Top = buttonsTop,
                Width = btnWidth,
                Height = 42,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F)
            };
            SetupRoundedButton(btnHistory, CardBackColor, TextLight);
            btnHistory.Click += (s, e) =>
            {
                using (var dialog = new TransferHistoryDialog(_user.Id))
                {
                    dialog.ShowDialog();
                }
            };

            lblStatus.Left = 20;
            lblStatus.Top = buttonsTop + 55;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            this.Controls.Add(lblWelcome);
            this.Controls.Add(pnlWallet);
            this.Controls.Add(pnlSafe);
            this.Controls.Add(btnTransfer);
            this.Controls.Add(btnHistory);
            this.Controls.Add(lblStatus);
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupRoundedButton(Button btn, Color bgColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);
                using (var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8))
                using (var brush = new SolidBrush(bgColor))
                    e.Graphics.FillPath(brush, path);
                if (bgColor == CardBackColor)
                {
                    using var pen = new Pen(Color.FromArgb(90, 94, 115), 1);
                    using var path2 = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                    e.Graphics.DrawPath(pen, path2);
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = Math.Max(radius * 2, 1);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private string DisplayName => string.IsNullOrWhiteSpace(_user.FullName) ? _user.Username : _user.FullName;

        public void RefreshData()
        {
            lblWelcome.Text = $"Hoş geldin, {DisplayName}";
            RefreshBalances();
        }

        private void RefreshBalances()
        {
            var (wallet, safe) = _accountService.GetBalances(_user.Id);

            AnimateCardValue(lblWalletAmount, wallet);
            AnimateCardValue(lblSafeAmount, safe);
        }

        // Kart tutarını 0'dan gerçek değerine sayarak (count-up) belirtir; tutarlar gizliyse animasyonsuz gösterir.
        private void AnimateCardValue(Label label, decimal targetValue, string suffix = " ₺")
        {
            if (_user.HideAmountsEnabled)
            {
                label.Text = "••••••";
                return;
            }

            var tr = new System.Globalization.CultureInfo("tr-TR");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            const int durationMs = 800;

            timer.Tick += (s, e) =>
            {
                if (label.IsDisposed) { timer.Stop(); timer.Dispose(); return; }

                double t = sw.Elapsed.TotalMilliseconds / durationMs;
                bool finished = t >= 1.0;
                if (finished) t = 1.0;

                double eased = 1 - Math.Pow(1 - t, 3);
                decimal shown = finished ? targetValue : Math.Round(targetValue * (decimal)eased);
                label.Text = shown.ToString("#,##0", tr) + suffix;

                if (finished)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void BtnTransfer_Click(object? sender, EventArgs e)
        {
            using (var dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    bool success;
                    string errorMessage;

                    if (dialog.Direction == TransferDirection.WalletToSafe)
                        success = _accountService.TransferToSafe(_user.Id, dialog.Amount, out errorMessage);
                    else
                        success = _accountService.TransferToWallet(_user.Id, dialog.Amount, out errorMessage);

                    if (success)
                    {
                        lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                        lblStatus.Text = "Transfer başarılı.";
                        RefreshBalances();
                    }
                    else
                    {
                        lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                        lblStatus.Text = errorMessage;
                    }
                }
            }
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
        }
    }
}