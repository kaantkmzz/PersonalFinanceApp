using System;
using System.Collections.Generic;
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
        private readonly AuthService _authService = new AuthService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color FieldBackColor => AppTheme.HoverBackColor;

        private Panel pnlScroll = new Panel();
        private readonly List<AccordionSection> _sections = new List<AccordionSection>();
        private const int SectionWidth = 760;

        private Panel pnlThemeToggle = new Panel();
        private Panel pnlHideToggle = new Panel();
        private Label lblDataStatus = new Label();
        private Label lblSessionStatus = new Label();

        private TextBox txtCurrentPassword = new TextBox();
        private TextBox txtNewPassword = new TextBox();
        private TextBox txtConfirmPassword = new TextBox();
        private CheckBox chkShowPasswords = new CheckBox();
        private Label lblPasswordStatus = new Label();

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

            pnlScroll.Left = 30;
            pnlScroll.Top = 70;
            pnlScroll.AutoScroll = true;
            pnlScroll.HorizontalScroll.Enabled = false;
            pnlScroll.HorizontalScroll.Visible = false;
            pnlScroll.BackColor = AppBackColor;
            EnableDoubleBuffering(pnlScroll);
            this.Controls.Add(pnlScroll);
            DarkTitleBarHelper.SetScrollBarDarkMode(pnlScroll, AppTheme.IsDark);
            this.Resize += (s, e) => ResizeScrollArea();
            ResizeScrollArea();

            AddAccordionSection("Görünüm", 100, body => BuildAppearanceBody(body, SectionWidth));
            AddAccordionSection("Gizlilik", 100, body => BuildPrivacyBody(body, SectionWidth));
            AddAccordionSection("Hakkında", 150, body => BuildAboutBody(body, SectionWidth));
            AddAccordionSection("Veri Yönetimi", 200, body => BuildDataBody(body, SectionWidth));
            AddAccordionSection("Oturum", 140, body => BuildSessionBody(body, SectionWidth));
            AddAccordionSection("Şifre Değiştir", 380, body => BuildPasswordBody(body, SectionWidth));

            RelayoutSections();
        }

        private void ResizeScrollArea()
        {
            pnlScroll.Width = Math.Max(200, this.ClientSize.Width - 60);
            pnlScroll.Height = Math.Max(200, this.ClientSize.Height - 90);
        }

        // --- Görünüm: tema aç/kapa ---
        private void BuildAppearanceBody(Panel body, int width)
        {
            Label lblRow = new Label { Text = "Koyu Tema", Left = 24, Top = 12, Width = width - 120, Height = 24, ForeColor = TextLight, BackColor = Color.Transparent };
            Label lblHint1 = MakeHintLine("Kenar çubuğundaki güneş simgesiyle", 24, 46, width - 48);
            Label lblHint2 = MakeHintLine("aynı ayardır.", 24, 64, width - 48);

            SetupToggle(pnlThemeToggle, () => AppTheme.IsDark, () => _onThemeToggle());
            pnlThemeToggle.Left = width - 76; pnlThemeToggle.Top = 10; pnlThemeToggle.Width = 52; pnlThemeToggle.Height = 28;

            body.Controls.Add(lblRow);
            body.Controls.Add(lblHint1);
            body.Controls.Add(lblHint2);
            body.Controls.Add(pnlThemeToggle);
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

        // --- Gizlilik: tutarları gizle ---
        private void BuildPrivacyBody(Panel body, int width)
        {
            Label lblRow = new Label { Text = "Tutarları Gizle", Left = 24, Top = 12, Width = width - 120, Height = 24, ForeColor = TextLight, BackColor = Color.Transparent };
            Label lblHint1 = MakeHintLine("Açıkken tüm ekranlardaki tutarlar", 24, 46, width - 48);
            Label lblHint2 = MakeHintLine("•••••• olarak gösterilir.", 24, 64, width - 48);

            SetupToggle(pnlHideToggle, () => _user.HideAmountsEnabled, () => _onHideAmountsToggle());
            pnlHideToggle.Left = width - 76; pnlHideToggle.Top = 10; pnlHideToggle.Width = 52; pnlHideToggle.Height = 28;

            body.Controls.Add(lblRow);
            body.Controls.Add(lblHint1);
            body.Controls.Add(lblHint2);
            body.Controls.Add(pnlHideToggle);
        }

        // --- Hakkında ---
        private void BuildAboutBody(Panel body, int width)
        {
            Label lblAppName = new Label { Text = "Kişisel Finans Takip Sistemi", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 24, Top = 10, AutoSize = true };
            Label lblVersion = new Label { Text = "Sürüm 1.0", Font = new Font("Segoe UI", 9F), ForeColor = TextMuted, BackColor = Color.Transparent, Left = 24, Top = 34, AutoSize = true };
            Label lblDesc1 = MakeHintLine("Gelir, gider ve hedeflerinizi tek", 24, 62, width - 48);
            Label lblDesc2 = MakeHintLine("yerden takip edin; kasa/cüzdan", 24, 80, width - 48);
            Label lblDesc3 = MakeHintLine("bakiyenizi yönetin, raporlarınızı", 24, 98, width - 48);
            Label lblDesc4 = MakeHintLine("görüntüleyin.", 24, 116, width - 48);

            body.Controls.Add(lblAppName);
            body.Controls.Add(lblVersion);
            body.Controls.Add(lblDesc1);
            body.Controls.Add(lblDesc2);
            body.Controls.Add(lblDesc3);
            body.Controls.Add(lblDesc4);
        }

        // --- Veri yönetimi: hızlı CSV dışa aktarma ---
        private void BuildDataBody(Panel body, int width)
        {
            Label lblHint1 = MakeHintLine("İşlemler ve Kategoriler ekranlarındaki", 24, 10, width - 48);
            Label lblHint2 = MakeHintLine("dışa aktarma kısayolları.", 24, 28, width - 48);

            Button btnExportTx = new Button { Text = "📄 Tüm İşlemleri CSV'ye Aktar", Left = 24, Top = 56, Width = width - 48, Height = 44, Cursor = Cursors.Hand };
            SetupOutlinedButton(btnExportTx, TextMuted, TextLight);
            btnExportTx.Click += (s, e) => ExportTransactions();

            Button btnExportCat = new Button { Text = "📄 Tüm Kategorileri CSV'ye Aktar", Left = 24, Top = 110, Width = width - 48, Height = 44, Cursor = Cursors.Hand };
            SetupOutlinedButton(btnExportCat, TextMuted, TextLight);
            btnExportCat.Click += (s, e) => ExportCategories();

            lblDataStatus.Left = 24; lblDataStatus.Top = 162; lblDataStatus.Width = width - 48; lblDataStatus.Height = 24;
            lblDataStatus.Font = new Font("Segoe UI", 9F); lblDataStatus.BackColor = Color.Transparent;

            body.Controls.Add(lblHint1);
            body.Controls.Add(lblHint2);
            body.Controls.Add(btnExportTx);
            body.Controls.Add(btnExportCat);
            body.Controls.Add(lblDataStatus);
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

        // --- Oturum: Beni Hatırla durumu ---
        private void BuildSessionBody(Panel body, int width)
        {
            bool remembered = RememberMeHelper.Load() != null;

            Label lblRow = new Label
            {
                Text = remembered ? "\"Beni hatırla\" bu cihazda etkin." : "\"Beni hatırla\" bu cihazda kayıtlı değil.",
                Left = 24,
                Top = 10,
                Width = width - 48,
                Height = 24,
                ForeColor = TextLight,
                BackColor = Color.Transparent
            };

            Button btnClear = new Button { Text = "Kayıtlı Oturum Bilgisini Temizle", Left = 24, Top = 42, Width = width - 48, Height = 44, Cursor = Cursors.Hand, Enabled = remembered };
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

            lblSessionStatus.Left = 24; lblSessionStatus.Top = 94; lblSessionStatus.Width = width - 48; lblSessionStatus.Height = 24;
            lblSessionStatus.Font = new Font("Segoe UI", 9F); lblSessionStatus.BackColor = Color.Transparent;

            body.Controls.Add(lblRow);
            body.Controls.Add(btnClear);
            body.Controls.Add(lblSessionStatus);
        }

        // --- Şifre Değiştir (eskiden sidebar'daki ayrı ekrandı; artık Ayarlar'da bir bölüm) ---
        private void BuildPasswordBody(Panel body, int width)
        {
            int fieldWidth = width - 48;

            Label lblCurrent = new Label { Text = "Mevcut Şifre:", Left = 24, Top = 10, ForeColor = TextMuted, BackColor = CardBackColor, AutoSize = true };
            Panel pnlCurrent = new Panel { Left = 24, Top = 36, Width = fieldWidth, Height = 40 };
            SetupSmoothContainer(pnlCurrent, 8, FieldBackColor, CardBackColor);
            txtCurrentPassword.Left = 10; txtCurrentPassword.Top = 10; txtCurrentPassword.Width = fieldWidth - 20;
            txtCurrentPassword.Font = new Font("Segoe UI", 10.5F); txtCurrentPassword.BorderStyle = BorderStyle.None;
            txtCurrentPassword.BackColor = FieldBackColor; txtCurrentPassword.ForeColor = TextLight; txtCurrentPassword.UseSystemPasswordChar = true;
            pnlCurrent.Controls.Add(txtCurrentPassword);

            Label lblNew = new Label { Text = "Yeni Şifre (en az 6 karakter):", Left = 24, Top = 92, ForeColor = TextMuted, BackColor = CardBackColor, AutoSize = true };
            Panel pnlNew = new Panel { Left = 24, Top = 118, Width = fieldWidth, Height = 40 };
            SetupSmoothContainer(pnlNew, 8, FieldBackColor, CardBackColor);
            txtNewPassword.Left = 10; txtNewPassword.Top = 10; txtNewPassword.Width = fieldWidth - 20;
            txtNewPassword.Font = new Font("Segoe UI", 10.5F); txtNewPassword.BorderStyle = BorderStyle.None;
            txtNewPassword.BackColor = FieldBackColor; txtNewPassword.ForeColor = TextLight; txtNewPassword.UseSystemPasswordChar = true;
            pnlNew.Controls.Add(txtNewPassword);

            Label lblConfirm = new Label { Text = "Yeni Şifre (tekrar):", Left = 24, Top = 174, ForeColor = TextMuted, BackColor = CardBackColor, AutoSize = true };
            Panel pnlConfirm = new Panel { Left = 24, Top = 200, Width = fieldWidth, Height = 40 };
            SetupSmoothContainer(pnlConfirm, 8, FieldBackColor, CardBackColor);
            txtConfirmPassword.Left = 10; txtConfirmPassword.Top = 10; txtConfirmPassword.Width = fieldWidth - 20;
            txtConfirmPassword.Font = new Font("Segoe UI", 10.5F); txtConfirmPassword.BorderStyle = BorderStyle.None;
            txtConfirmPassword.BackColor = FieldBackColor; txtConfirmPassword.ForeColor = TextLight; txtConfirmPassword.UseSystemPasswordChar = true;
            pnlConfirm.Controls.Add(txtConfirmPassword);

            chkShowPasswords.Text = "Şifreleri göster";
            chkShowPasswords.Left = 24; chkShowPasswords.Top = 252; chkShowPasswords.AutoSize = true;
            chkShowPasswords.ForeColor = TextMuted; chkShowPasswords.BackColor = CardBackColor;
            chkShowPasswords.CheckedChanged += (s, e) =>
            {
                bool show = chkShowPasswords.Checked;
                txtCurrentPassword.UseSystemPasswordChar = !show;
                txtNewPassword.UseSystemPasswordChar = !show;
                txtConfirmPassword.UseSystemPasswordChar = !show;
            };

            Button btnSavePassword = new Button { Text = "💾 Şifreyi Güncelle", Left = 24, Top = 284, Width = 240, Height = 38, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSavePassword, AccentColor, Color.White);
            btnSavePassword.Click += BtnSavePassword_Click;

            lblPasswordStatus.Left = 24; lblPasswordStatus.Top = 330; lblPasswordStatus.Width = fieldWidth; lblPasswordStatus.Height = 24;
            lblPasswordStatus.Font = new Font("Segoe UI", 9F); lblPasswordStatus.BackColor = Color.Transparent;

            body.Controls.Add(lblCurrent); body.Controls.Add(pnlCurrent);
            body.Controls.Add(lblNew); body.Controls.Add(pnlNew);
            body.Controls.Add(lblConfirm); body.Controls.Add(pnlConfirm);
            body.Controls.Add(chkShowPasswords);
            body.Controls.Add(btnSavePassword);
            body.Controls.Add(lblPasswordStatus);
        }

        private void BtnSavePassword_Click(object? sender, EventArgs e)
        {
            string current = txtCurrentPassword.Text;
            string newPass = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            if (newPass != confirm)
            {
                lblPasswordStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblPasswordStatus.Text = "Yeni şifreler birbiriyle eşleşmiyor.";
                return;
            }

            if (_authService.ChangePassword(_user.Id, current, newPass, out string errorMessage))
            {
                lblPasswordStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblPasswordStatus.Text = "Şifreniz başarıyla güncellendi.";
                txtCurrentPassword.Clear(); txtNewPassword.Clear(); txtConfirmPassword.Clear();
            }
            else
            {
                lblPasswordStatus.ForeColor = Color.FromArgb(255, 140, 140);
                lblPasswordStatus.Text = errorMessage;
            }
        }

        // --- Açma/kapama anahtarı (toggle switch) ---
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

        // --- AKORDİYON (AÇ/KAPA BÖLÜM) ALTYAPISI ---
        // Profilim ekranındaki desenin aynısı: bkz. ProfileControl.cs için ayrıntılı açıklama.
        private class AccordionSection
        {
            public Panel Card = new Panel();
            public Panel Body = new Panel();
            public Panel Chevron = new Panel();
            public bool Expanded;
            public int BodyHeight;
            public const int HeaderHeight = 56;
            public const int GapAfterHeader = 24;
            public const int BottomPadding = 16;
        }

        private void AddAccordionSection(string title, int bodyHeight, Action<Panel> buildBody)
        {
            var section = new AccordionSection { BodyHeight = bodyHeight, Expanded = false };
            int width = SectionWidth;

            section.Card.Width = width;
            section.Card.Height = AccordionSection.HeaderHeight;
            SetupSmoothContainer(section.Card, 14, CardBackColor);
            section.Card.Cursor = Cursors.Hand;
            EnableDoubleBuffering(section.Card);
            EnableDoubleBuffering(section.Body);

            Label lblSectionTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TextLight,
                BackColor = Color.Transparent,
                Left = 24,
                Top = 0,
                Width = width - 80,
                Height = AccordionSection.HeaderHeight,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            section.Chevron.Width = 22;
            section.Chevron.Height = 22;
            section.Chevron.Left = width - 46;
            section.Chevron.Top = (AccordionSection.HeaderHeight - 22) / 2;
            section.Chevron.BackColor = Color.Transparent;
            section.Chevron.Cursor = Cursors.Hand;
            section.Chevron.Paint += (s, e) => DrawChevron(e.Graphics, section.Chevron.ClientRectangle, section.Expanded);

            Panel divider = new Panel { Left = 24, Top = AccordionSection.HeaderHeight, Width = width - 48, Height = 1, BackColor = AppTheme.SidebarDividerColor, Visible = false };

            section.Body.Left = 0;
            section.Body.Top = AccordionSection.HeaderHeight + AccordionSection.GapAfterHeader;
            section.Body.Width = width;
            section.Body.Height = bodyHeight;
            section.Body.BackColor = CardBackColor;
            section.Body.Visible = false;
            buildBody(section.Body);

            void Toggle()
            {
                section.Expanded = !section.Expanded;
                section.Body.Visible = section.Expanded;
                divider.Visible = section.Expanded;
                section.Card.Height = section.Expanded
                    ? AccordionSection.HeaderHeight + AccordionSection.GapAfterHeader + bodyHeight + AccordionSection.BottomPadding
                    : AccordionSection.HeaderHeight;
                section.Chevron.Invalidate();
                RelayoutSections();
            }

            section.Card.Click += (s, e) => Toggle();
            lblSectionTitle.Click += (s, e) => Toggle();
            section.Chevron.Click += (s, e) => Toggle();

            section.Card.Controls.Add(lblSectionTitle);
            section.Card.Controls.Add(section.Chevron);
            section.Card.Controls.Add(divider);
            section.Card.Controls.Add(section.Body);

            pnlScroll.Controls.Add(section.Card);
            _sections.Add(section);
        }

        private static void DrawChevron(Graphics g, Rectangle r, bool expanded)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(TextMuted, 2.2f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };
            int cx = r.Left + r.Width / 2, cy = r.Top + r.Height / 2;
            if (expanded)
            {
                g.DrawLines(pen, new PointF[] { new PointF(cx - 6, cy - 2), new PointF(cx, cy + 5), new PointF(cx + 6, cy - 2) });
            }
            else
            {
                g.DrawLines(pen, new PointF[] { new PointF(cx - 3, cy - 7), new PointF(cx + 4, cy), new PointF(cx - 3, cy + 7) });
            }
        }

        private void RelayoutSections()
        {
            int y = 8;
            foreach (var section in _sections)
            {
                section.Card.Top = y;
                y += section.Card.Height + 16;
            }

            // Genişliği hep 0 olarak sabitliyoruz ki içerik hiçbir zaman yatay kaydırma
            // gerektirmesin (sığdığı halde ara sıra yatay kaydırma çubuğu çıkıyordu); dikey
            // kaydırma için gereken yüksekliği ise burada kendimiz belirliyoruz.
            pnlScroll.AutoScrollMinSize = new Size(0, y);
        }

        // MainForm'daki EnableDoubleBuffering ile aynı: kaydırılabilir alan çift arabellekli
        // olmayınca (özellikle özel çizimli kartlarla) kaydırma sırasında eski karelerden kalma
        // izler ("hayalet" koyu kutular) görülebiliyordu.
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                control,
                new object[] { true });
        }

        // --- GÖRSEL YARDIMCI METOTLAR ---
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor) => SetupSmoothContainer(pnl, radius, bgColor, AppBackColor);
        private void SetupSmoothContainer(Panel pnl, int radius, Color bgColor, Color clearColor)
        {
            pnl.BackColor = clearColor;
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(clearColor);
                using var path = GetRoundedRectPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), radius);
                using var brush = new SolidBrush(bgColor);
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
                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using var brush = new SolidBrush(bgColor);
                e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
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
