using System;
using System.Drawing;
using System.Windows.Forms;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class SettingsControl : UserControl
    {
        private readonly User _user;
        private readonly Action _onThemeToggle;
        private readonly Action _onHideAmountsToggle;
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;

        private Panel pnlThemeToggle = new Panel();
        private Panel pnlHideToggle = new Panel();
        private Label lblDataStatus = new Label();
        private Label lblSessionStatus = new Label();

        public SettingsControl(User user, Action onThemeToggle, Action onHideAmountsToggle)
        {
            _user = user;
            _onThemeToggle = onThemeToggle;
            _onHideAmountsToggle = onHideAmountsToggle;
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label { Text = "Ayarlar", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 30, Top = 20, AutoSize = true };
            this.Controls.Add(lblTitle);

            const int colWidth = 420;
            const int leftColX = 30;
            const int rightColX = leftColX + colWidth + 20;

            BuildPreferencesCard(leftColX, 75, colWidth);
            BuildDataSessionCard(rightColX, 75, colWidth);
        }

        // Çok satırlı Label'ların satır aralığı/kırpılma sorunlarından kaçınmak için, her satırı
        // ayrı bir tek-satırlık Label olarak oluşturan yardımcı. AutoSize=true kullanıyoruz çünkü
        // sabit bir Height (ör. 18px), italik font + Türkçe alt-uzantılı harflerle (ç ğ ş g y)
        // birlikte harflerin alt kısmını kırpıp bozuk görünmesine yol açıyordu (g->a, y->v gibi).
        private Label MakeHintLine(string text, int left, int top, int width)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                AutoSize = true,
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };
        }

        // Kart içinde bölüm başlıklarını ayıran ince çizgi.
        private Panel MakeSectionDivider(int left, int top, int width)
            => new Panel { Left = left, Top = top, Width = width, Height = 1, BackColor = AppTheme.SidebarDividerColor };

        // --- Tek uzun kart: Görünüm + Gizlilik + Hakkında (küçük ayrı kutucuklar yerine,
        // içeriğin taşmadan bol boşlukla sığdığı tek dikdörtgen kart) ---
        private void BuildPreferencesCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 500);

            Label lblSection1 = new Label { Text = "Görünüm", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };
            Label lblRow1 = new Label { Text = "Koyu Tema", Left = 20, Top = 54, Width = width - 100, Height = 24, ForeColor = TextLight, BackColor = Color.Transparent };
            Label lblHint1a = MakeHintLine("Kenar çubuğundaki güneş simgesiyle", 20, 90, width - 40);
            Label lblHint1b = MakeHintLine("aynı ayardır.", 20, 108, width - 40);
            SetupToggle(pnlThemeToggle, () => AppTheme.IsDark, () => _onThemeToggle());
            pnlThemeToggle.Left = width - 72; pnlThemeToggle.Top = 52; pnlThemeToggle.Width = 52; pnlThemeToggle.Height = 28;

            Panel divider1 = MakeSectionDivider(20, 142, width - 40);

            Label lblSection2 = new Label { Text = "Gizlilik", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 164, AutoSize = true };
            Label lblRow2 = new Label { Text = "Tutarları Gizle", Left = 20, Top = 202, Width = width - 100, Height = 24, ForeColor = TextLight, BackColor = Color.Transparent };
            Label lblHint2a = MakeHintLine("Açıkken tüm ekranlardaki tutarlar", 20, 238, width - 40);
            Label lblHint2b = MakeHintLine("•••••• olarak gösterilir.", 20, 256, width - 40);
            SetupToggle(pnlHideToggle, () => _user.HideAmountsEnabled, () => _onHideAmountsToggle());
            pnlHideToggle.Left = width - 72; pnlHideToggle.Top = 200; pnlHideToggle.Width = 52; pnlHideToggle.Height = 28;

            Panel divider2 = MakeSectionDivider(20, 290, width - 40);

            Label lblSection3 = new Label { Text = "Hakkında", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 312, AutoSize = true };
            Label lblAppName = new Label { Text = "Kişisel Finans Takip Sistemi", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 350, AutoSize = true };
            Label lblVersion = new Label { Text = "Sürüm 1.0", Font = new Font("Segoe UI", 9F), ForeColor = TextMuted, BackColor = Color.Transparent, Left = 20, Top = 378, AutoSize = true };
            Label lblDesc1 = MakeHintLine("Gelir, gider ve hedeflerinizi tek", 20, 406, width - 40);
            Label lblDesc2 = MakeHintLine("yerden takip edin; kasa/cüzdan", 20, 424, width - 40);
            Label lblDesc3 = MakeHintLine("bakiyenizi yönetin, raporlarınızı", 20, 442, width - 40);
            Label lblDesc4 = MakeHintLine("görüntüleyin.", 20, 460, width - 40);

            card.Controls.Add(lblSection1);
            card.Controls.Add(lblRow1);
            card.Controls.Add(pnlThemeToggle);
            card.Controls.Add(lblHint1a);
            card.Controls.Add(lblHint1b);
            card.Controls.Add(divider1);
            card.Controls.Add(lblSection2);
            card.Controls.Add(lblRow2);
            card.Controls.Add(pnlHideToggle);
            card.Controls.Add(lblHint2a);
            card.Controls.Add(lblHint2b);
            card.Controls.Add(divider2);
            card.Controls.Add(lblSection3);
            card.Controls.Add(lblAppName);
            card.Controls.Add(lblVersion);
            card.Controls.Add(lblDesc1);
            card.Controls.Add(lblDesc2);
            card.Controls.Add(lblDesc3);
            card.Controls.Add(lblDesc4);
        }

        // --- Tek uzun kart: Veri Yönetimi + Oturum ---
        private void BuildDataSessionCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 440);

            Label lblSection1 = new Label { Text = "Veri Yönetimi", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };
            Label lblHint1 = MakeHintLine("İşlemler ve Kategoriler ekranlarındaki", 20, 54, width - 40);
            Label lblHint2 = MakeHintLine("dışa aktarma kısayolları.", 20, 72, width - 40);

            Button btnExportTx = new Button { Text = "📄 Tüm İşlemleri CSV'ye Aktar", Left = 20, Top = 100, Width = width - 40, Height = 44, Cursor = Cursors.Hand };
            SetupOutlinedButton(btnExportTx, TextMuted, TextLight);
            btnExportTx.Click += (s, e) => ExportTransactions();

            Button btnExportCat = new Button { Text = "📄 Tüm Kategorileri CSV'ye Aktar", Left = 20, Top = 154, Width = width - 40, Height = 44, Cursor = Cursors.Hand };
            SetupOutlinedButton(btnExportCat, TextMuted, TextLight);
            btnExportCat.Click += (s, e) => ExportCategories();

            lblDataStatus.Left = 20; lblDataStatus.Top = 206; lblDataStatus.Width = width - 40; lblDataStatus.Height = 24;
            lblDataStatus.Font = new Font("Segoe UI", 9F); lblDataStatus.BackColor = Color.Transparent;

            Panel divider = MakeSectionDivider(20, 246, width - 40);

            bool remembered = RememberMeHelper.Load() != null;

            Label lblSection2 = new Label { Text = "Oturum", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 268, AutoSize = true };
            Label lblRow = new Label
            {
                Text = remembered ? "\"Beni hatırla\" bu cihazda etkin." : "\"Beni hatırla\" bu cihazda kayıtlı değil.",
                Left = 20,
                Top = 306,
                Width = width - 40,
                Height = 24,
                ForeColor = TextLight,
                BackColor = Color.Transparent
            };

            Button btnClear = new Button { Text = "Kayıtlı Oturum Bilgisini Temizle", Left = 20, Top = 338, Width = width - 40, Height = 44, Cursor = Cursors.Hand, Enabled = remembered };
            SetupOutlinedButton(btnClear, remembered ? AppTheme.DangerColor : TextMuted, remembered ? AppTheme.DangerColor : TextMuted);
            btnClear.Click += (s, e) =>
            {
                RememberMeHelper.Clear();
                btnClear.Enabled = false;
                btnClear.Invalidate();
                lblRow.Text = "\"Beni hatırla\" bu cihazda kayıtlı değil.";
                lblSessionStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblSessionStatus.Text = "Kayıtlı oturum bilgisi temizlendi.";
            };

            lblSessionStatus.Left = 20; lblSessionStatus.Top = 390; lblSessionStatus.Width = width - 40; lblSessionStatus.Height = 24;
            lblSessionStatus.Font = new Font("Segoe UI", 9F); lblSessionStatus.BackColor = Color.Transparent;

            card.Controls.Add(lblSection1);
            card.Controls.Add(lblHint1);
            card.Controls.Add(lblHint2);
            card.Controls.Add(btnExportTx);
            card.Controls.Add(btnExportCat);
            card.Controls.Add(lblDataStatus);
            card.Controls.Add(divider);
            card.Controls.Add(lblSection2);
            card.Controls.Add(lblRow);
            card.Controls.Add(btnClear);
            card.Controls.Add(lblSessionStatus);
        }

        private void ExportTransactions()
        {
            using var dialog = new SaveFileDialog { Filter = "CSV Dosyası (*.csv)|*.csv", FileName = $"islemler_{DateTime.Today:yyyy_MM_dd}.csv" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var tr = new System.Globalization.CultureInfo("tr-TR");
                var transactions = _transactionService.GetUserTransactions(_user.Id);
                using (var writer = new System.IO.StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Tarih;Tip;Kategori;Tutar;Açıklama");
                    foreach (var t in transactions)
                    {
                        string aciklama = (t.Description ?? "").Replace(";", ",");
                        writer.WriteLine($"{t.TransactionDate:dd.MM.yyyy};{TypeToTr(t.Type)};{t.CategoryName};{t.Amount.ToString("0.00", tr)};{aciklama}");
                    }
                }
                lblDataStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblDataStatus.Text = "İşlemler CSV olarak dışa aktarıldı.";
            }
            catch (Exception ex)
            {
                lblDataStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblDataStatus.Text = $"Dışa aktarma başarısız: {ex.Message}";
            }
        }

        private void ExportCategories()
        {
            using var dialog = new SaveFileDialog { Filter = "CSV Dosyası (*.csv)|*.csv", FileName = $"kategoriler_{DateTime.Today:yyyy_MM_dd}.csv" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var tr = new System.Globalization.CultureInfo("tr-TR");
                var categories = _categoryService.GetUserCategories(_user.Id);
                var totals = _transactionService.GetCategoryTotals(_user.Id);
                using (var writer = new System.IO.StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Ad;Tip;Toplam Tutar");
                    foreach (var c in categories)
                    {
                        decimal total = totals.TryGetValue(c.Id, out decimal t) ? t : 0;
                        writer.WriteLine($"{c.Name};{TypeToTr(c.Type)};{total.ToString("0.00", tr)}");
                    }
                }
                lblDataStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblDataStatus.Text = "Kategoriler CSV olarak dışa aktarıldı.";
            }
            catch (Exception ex)
            {
                lblDataStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblDataStatus.Text = $"Dışa aktarma başarısız: {ex.Message}";
            }
        }

        private static string TypeToTr(string type) => type switch { "income" => "Gelir", "goal" => "Hedef", _ => "Gider" };

        // --- Açma/kapama anahtarı (toggle switch) — ProfileControl'deki desenin genel hâli ---
        private void SetupToggle(Panel toggle, Func<bool> getOn, Action onClick)
        {
            toggle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(toggle.Parent?.BackColor ?? CardBackColor);

                bool on = getOn();
                Color trackColor = on ? AccentColor : Color.FromArgb(90, 94, 115);

                using (var path = GetRoundedRectPath(new Rectangle(0, 0, toggle.Width - 1, toggle.Height - 1), toggle.Height / 2))
                using (var brush = new SolidBrush(trackColor))
                    e.Graphics.FillPath(brush, path);

                int knobSize = toggle.Height - 6;
                int knobX = on ? toggle.Width - knobSize - 3 : 3;
                using (var knobBrush = new SolidBrush(Color.White))
                    e.Graphics.FillEllipse(knobBrush, knobX, 3, knobSize, knobSize);
            };

            toggle.Cursor = Cursors.Hand;
            toggle.Click += (s, e) =>
            {
                onClick();
                toggle.Invalidate();
            };
        }

        protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } }

        // --- GÖRSEL YARDIMCI METOTLAR ---
        private Panel CreateCard(int left, int top, int width, int height)
        {
            Panel card = new Panel { Left = left, Top = top, Width = width, Height = height };
            SetupSmoothContainer(card, 14, CardBackColor);
            this.Controls.Add(card);
            return card;
        }

        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor)
        {
            pnl.BackColor = AppBackColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnl.Parent?.BackColor ?? AppBackColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
            };
            pnl.SizeChanged += (s, e) => pnl.Invalidate();
        }

        private void SetupOutlinedButton(Button btn, Color borderColor, Color textColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? AppBackColor);

                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using (var pen = new Pen(isHovered && btn.Enabled ? TextLight : borderColor, 1.2f))
                    e.Graphics.DrawPath(pen, path);

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
    }
}
