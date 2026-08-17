using System.Windows.Forms.DataVisualization.Charting;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class HomeControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly Action<string>? _onNavigate;
        private readonly AccountService _accountService = new AccountService();
        private readonly AssetService _assetService = new AssetService();
        private readonly ReminderService _reminderService = new ReminderService();
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly ReportService _reportService = new ReportService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color IncomeColor => AppTheme.IncomeColor;
        private static Color ExpenseColor => AppTheme.ExpenseColor;

        private const int CardLeft1 = 20;
        private const int CardLeft2 = 360;
        private const int CardLeft3 = 700;
        private const int CardWidth = 320;
        private const int AllCardsRight = CardLeft3 + CardWidth; // 1020

        private Label lblWalletAmount = new Label();
        private Label lblSafeAmount = new Label();
        private Label lblInvestAmount = new Label();
        private Label lblStatus = new Label();
        private Panel pnlNotifications = new Panel();

        private const int MiniRowTop = 630;
        private const int MiniRowHeight = 210;

        public HomeControl(User user, Action<string>? onNavigate = null)
        {
            _user = user;
            _onNavigate = onNavigate;
            InitializeComponent();
            SetupUI();
            RefreshBalances();
            _ = LoadNotificationsAsync();
            LoadReminderWidget();
            LoadMiniReportWidget();
            LoadRecentTransactionsWidget();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlWallet = new Panel { Left = CardLeft1, Top = 30, Width = CardWidth, Height = 240 };
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

            Panel pnlSafe = new Panel { Left = CardLeft2, Top = 30, Width = CardWidth, Height = 240 };
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

            Panel pnlInvest = new Panel { Left = CardLeft3, Top = 30, Width = CardWidth, Height = 240 };
            SetupSmoothContainer(pnlInvest, 16, CardBackColor);
            Label lblInvestIcon = new Label { Text = "📈", Font = new Font("Segoe UI Emoji", 32F), ForeColor = TextLight, Left = 20, Top = 15, AutoSize = true, BackColor = Color.Transparent };
            Label lblInvestTitle = new Label { Text = "Varlıklarım", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = TextLight, Left = 20, Top = 130, AutoSize = true, BackColor = Color.Transparent };
            lblInvestAmount.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblInvestAmount.ForeColor = Color.FromArgb(120, 220, 210);
            lblInvestAmount.Left = 20;
            lblInvestAmount.Top = 165;
            lblInvestAmount.AutoSize = true;
            lblInvestAmount.BackColor = Color.Transparent;
            EnableDoubleBuffering(lblInvestAmount);
            pnlInvest.Controls.Add(lblInvestIcon);
            pnlInvest.Controls.Add(lblInvestTitle);
            pnlInvest.Controls.Add(lblInvestAmount);

            // Butonlar Cüzdan kutucuğunun altına, sol hizalı (üç kutu eklenince sağa hizalama anlamını yitirdi)
            const int buttonsTop = 30 + 240 + 16;
            const int btnWidth = 190;
            const int btnGap = 14;

            Button btnTransfer = new Button
            {
                Text = "Transfer Et",
                Left = CardLeft1,
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
                Left = CardLeft1 + btnWidth + btnGap,
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

            lblStatus.Left = CardLeft1;
            lblStatus.Top = buttonsTop + 55;
            lblStatus.Width = 500;
            lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F);

            // Varlıklarım'da pozisyonu olan kullanıcılar için kâr/zarar bildirim kartı; pozisyon yoksa
            // (bkz. LoadNotificationsAsync) tamamen gizlenir, boş kart gösterilmez.
            pnlNotifications.Left = CardLeft1;
            pnlNotifications.Top = lblStatus.Top + 40;
            pnlNotifications.Width = AllCardsRight - CardLeft1;
            pnlNotifications.Visible = false;
            SetupSmoothContainer(pnlNotifications, 16, CardBackColor);

            this.Controls.Add(pnlWallet);
            this.Controls.Add(pnlSafe);
            this.Controls.Add(pnlInvest);
            this.Controls.Add(btnTransfer);
            this.Controls.Add(btnHistory);
            this.Controls.Add(lblStatus);
            this.Controls.Add(pnlNotifications);

            SetupMiniWidgets();
        }

        // Ana Sayfa'yı çeşitlendiren üç küçük, tıklanabilir önizleme: yaklaşan hatırlatıcılar,
        // bu ayın rapor özeti ve son işlemler — her biri kendi tam ekranına götürür.
        private Panel pnlReminderWidget = new Panel();
        private Panel pnlMiniReportWidget = new Panel();
        private Panel pnlRecentTxWidget = new Panel();

        private void SetupMiniWidgets()
        {
            pnlReminderWidget = CreateWidgetCard(CardLeft1, "⏰ Yaklaşan Hatırlatıcılar", "Hatırlatıcılar");
            pnlMiniReportWidget = CreateWidgetCard(CardLeft2, "📊 Bu Ayın Özeti", "Rapor");
            pnlRecentTxWidget = CreateWidgetCard(CardLeft3, "🧾 Son İşlemler", "İşlemler");

            this.Controls.Add(pnlReminderWidget);
            this.Controls.Add(pnlMiniReportWidget);
            this.Controls.Add(pnlRecentTxWidget);
        }

        private Panel CreateWidgetCard(int left, string titleText, string navigateTarget)
        {
            Panel card = new Panel { Left = left, Top = MiniRowTop, Width = CardWidth, Height = MiniRowHeight };
            SetupSmoothContainer(card, 16, CardBackColor);

            Label lblTitle = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 18,
                Top = 14,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            MakeClickable(card, () => _onNavigate?.Invoke(navigateTarget));
            return card;
        }

        // Panel ve içindeki her denetim (WinForms'ta Click yukarı aktarılmaz) için tıklanabilir
        // el imleci ve navigasyon davranışı ekler.
        private void MakeClickable(Control root, Action onClick)
        {
            root.Cursor = Cursors.Hand;
            root.Click += (s, e) => onClick();
            foreach (Control child in root.Controls)
            {
                MakeClickable(child, onClick);
            }
        }

        private void LoadReminderWidget()
        {
            var upcoming = _reminderService.GetUserReminders(_user.Id)
                .Where(r => !r.IsCompleted && r.ReminderDate >= DateTime.Now)
                .OrderBy(r => r.ReminderDate)
                .Take(3)
                .ToList();

            Action goToReminders = () => _onNavigate?.Invoke("Hatırlatıcılar");

            int top = 48;
            if (upcoming.Count == 0)
            {
                AddWidgetLine(pnlReminderWidget, "Yaklaşan hatırlatıcı yok.", top, TextMuted, 18, goToReminders);
                return;
            }

            foreach (var r in upcoming)
            {
                string line = $"{r.Title}  —  {r.ReminderDate:dd.MM.yyyy}";
                AddWidgetLine(pnlReminderWidget, line, top, TextLight, 18, goToReminders);
                top += 30;
            }
        }

        private void LoadRecentTransactionsWidget()
        {
            var recent = _transactionService.GetUserTransactions(_user.Id).Take(3).ToList();
            var tr = new System.Globalization.CultureInfo("tr-TR");

            int top = 48;
            if (recent.Count == 0)
            {
                AddWidgetLine(pnlRecentTxWidget, "Henüz işlem yok.", top, TextMuted, 18, () => _onNavigate?.Invoke("İşlemler"));
                return;
            }

            foreach (var t in recent)
            {
                Color amountColor = t.Type == "income" ? IncomeColor : ExpenseColor;
                string amountText = _user.HideAmountsEnabled ? "••••••" : t.Amount.ToString("#,##0", tr) + " ₺";

                Label lblCategory = new Label
                {
                    Text = t.CategoryName,
                    ForeColor = TextLight,
                    Left = 18,
                    Top = top,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5F)
                };
                Label lblAmount = new Label
                {
                    Text = amountText,
                    ForeColor = amountColor,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Left = CardWidth - 150,
                    Top = top,
                    Width = 130,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };
                pnlRecentTxWidget.Controls.Add(lblCategory);
                pnlRecentTxWidget.Controls.Add(lblAmount);
                MakeClickable(lblCategory, () => _onNavigate?.Invoke("İşlemler"));
                MakeClickable(lblAmount, () => _onNavigate?.Invoke("İşlemler"));
                top += 30;
            }
        }

        private void LoadMiniReportWidget()
        {
            DateTime start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime end = start.AddMonths(1);
            var report = _reportService.GenerateReport(_user.Id, start, end);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            Chart miniChart = new Chart { Left = 18, Top = 44, Width = 100, Height = 100, BackColor = CardBackColor };
            ChartArea area = new ChartArea("mini") { BackColor = CardBackColor };
            area.Position = new ElementPosition(0, 0, 100, 100);
            area.InnerPlotPosition = new ElementPosition(0, 0, 100, 100);
            miniChart.ChartAreas.Add(area);

            Series series = new Series { ChartType = SeriesChartType.Doughnut, ChartArea = "mini" };
            series["DoughnutRadius"] = "55";
            series["PieLabelStyle"] = "Disabled";

            if (report.TotalIncome > 0 || report.TotalExpense > 0)
            {
                int i1 = series.Points.AddY((double)report.TotalIncome);
                series.Points[i1].Color = IncomeColor;
                int i2 = series.Points.AddY((double)report.TotalExpense);
                series.Points[i2].Color = ExpenseColor;
            }
            else
            {
                int i1 = series.Points.AddY(1);
                series.Points[i1].Color = TextMuted;
            }
            miniChart.Series.Add(series);
            pnlMiniReportWidget.Controls.Add(miniChart);

            string incomeText = _user.HideAmountsEnabled ? "••••••" : report.TotalIncome.ToString("#,##0", tr) + " ₺";
            string expenseText = _user.HideAmountsEnabled ? "••••••" : report.TotalExpense.ToString("#,##0", tr) + " ₺";

            Action goToReport = () => _onNavigate?.Invoke("Rapor");
            AddWidgetLine(pnlMiniReportWidget, $"Gelir: {incomeText}", 44, IncomeColor, 130, goToReport);
            AddWidgetLine(pnlMiniReportWidget, $"Gider: {expenseText}", 74, ExpenseColor, 130, goToReport);

            MakeClickable(miniChart, goToReport);
        }

        // parent'ın kartı zaten tıklanabilir (bkz. CreateWidgetCard) ama bu satır SetupUI bittikten
        // SONRA eklendiği için o ilk MakeClickable taramasına dahil değil — burada ayrıca sarmalıyoruz.
        private void AddWidgetLine(Panel parent, string text, int top, Color color, int left, Action onClick)
        {
            Label lbl = new Label
            {
                Text = text,
                ForeColor = color,
                Left = left,
                Top = top,
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F),
                MaximumSize = new Size(CardWidth - left - 16, 0)
            };
            parent.Controls.Add(lbl);
            MakeClickable(lbl, onClick);
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

        public void RefreshData()
        {
            RefreshBalances();
            _ = LoadNotificationsAsync();
        }

        // Varlıklarım'daki pozisyonların anlık kâr/zararını "bildirim" tarzında listeler; en çok
        // hareket edenler (yüzde olarak) üstte. Pozisyon yoksa kart tamamen gizlenir.
        private async Task LoadNotificationsAsync()
        {
            var holdings = await _assetService.GetHoldingsWithLivePricesAsync(_user.Id);
            if (this.IsDisposed) return;

            var withChange = holdings.Where(h => h.ProfitLossPercent.HasValue).ToList();

            pnlNotifications.Controls.Clear();

            if (withChange.Count == 0)
            {
                pnlNotifications.Visible = false;
                return;
            }

            pnlNotifications.Visible = true;

            Label lblHeader = new Label
            {
                Text = "🔔 Varlık Bildirimleri",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TextLight,
                Left = 20,
                Top = 16,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlNotifications.Controls.Add(lblHeader);

            var ordered = withChange.OrderByDescending(h => Math.Abs(h.ProfitLossPercent!.Value)).Take(5).ToList();
            var tr = new System.Globalization.CultureInfo("tr-TR");

            int rowTop = 56;
            foreach (var h in ordered)
            {
                bool isUp = h.ProfitLossPercent!.Value >= 0;
                Color changeColor = isUp ? IncomeColor : ExpenseColor;
                string arrow = isUp ? "▲" : "▼";

                Label lblSymbol = new Label
                {
                    Text = $"{h.Symbol} — {h.Name}",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = TextLight,
                    Left = 20,
                    Top = rowTop,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };

                string changeText = _user.HideAmountsEnabled
                    ? "••••••"
                    : $"{arrow} %{Math.Abs(h.ProfitLossPercent!.Value).ToString("0.0", tr)}   {h.ProfitLossTry!.Value.ToString("+#,##0;-#,##0", tr)} ₺";

                Label lblChange = new Label
                {
                    Text = changeText,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = changeColor,
                    Left = pnlNotifications.Width - 220,
                    Top = rowTop,
                    Width = 200,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleRight,
                    BackColor = Color.Transparent
                };

                pnlNotifications.Controls.Add(lblSymbol);
                pnlNotifications.Controls.Add(lblChange);
                rowTop += 32;
            }

            pnlNotifications.Height = rowTop + 16;
        }

        private void RefreshBalances()
        {
            var (wallet, safe) = _accountService.GetBalances(_user.Id);
            decimal invest = _accountService.GetInvestBalance(_user.Id);

            AnimateCardValue(lblWalletAmount, wallet);
            AnimateCardValue(lblSafeAmount, safe);
            AnimateCardValue(lblInvestAmount, invest);
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
                if (dialog.ShowDialog() != DialogResult.OK) return;

                bool success;
                string errorMessage;

                switch ((dialog.From, dialog.To))
                {
                    case (TransferAccount.Wallet, TransferAccount.Safe):
                        success = _accountService.TransferToSafe(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    case (TransferAccount.Safe, TransferAccount.Wallet):
                        success = _accountService.TransferToWallet(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    case (TransferAccount.Wallet, TransferAccount.Invest):
                        success = _accountService.TransferWalletToInvest(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    case (TransferAccount.Safe, TransferAccount.Invest):
                        success = _accountService.TransferSafeToInvest(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    case (TransferAccount.Invest, TransferAccount.Wallet):
                        success = _accountService.TransferInvestToWallet(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    case (TransferAccount.Invest, TransferAccount.Safe):
                        success = _accountService.TransferInvestToSafe(_user.Id, dialog.Amount, out errorMessage);
                        break;
                    default:
                        success = false;
                        errorMessage = "Geçersiz transfer yönü.";
                        break;
                }

                if (success)
                {
                    lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
                    lblStatus.Text = "Transfer başarılı.";
                    RefreshBalances();
                    _ = LoadNotificationsAsync();
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(255, 140, 140);
                    lblStatus.Text = errorMessage;
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
