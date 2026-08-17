using System;
using System.Drawing;
using System.Windows.Forms;
using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class ProfileControl : UserControl, IRefreshable
    {
        private readonly User _user;
        private readonly AccountService _accountService = new AccountService();
        private readonly TransactionService _transactionService = new TransactionService();
        private readonly CategoryService _categoryService = new CategoryService();

        private static Color AppBackColor => AppTheme.AppBackColor;
        private static Color CardBackColor => AppTheme.CardBackColor;
        private static Color TextLight => AppTheme.TextLight;
        private static Color TextMuted => AppTheme.TextMuted;
        private static Color AccentColor => AppTheme.AccentColor;
        private static Color FieldBackColor => AppTheme.HoverBackColor;

        private TextBox txtFullName = new TextBox();
        private Button btnWeekly = new Button();
        private Button btnMonthly = new Button();
        private Button btnNever = new Button();
        private Label lblExplanation = new Label();
        private Panel pnlToggle = new Panel();
        private Label lblStatus = new Label();

        private Panel pnlAvatarPreview = new Panel();
        private Label lblAvatarStatus = new Label();
        private Color _selectedAvatarColor;
        private readonly Action? _onAvatarSaved;

        // onAvatarSaved: avatar rengi kaydedildiğinde MainForm'un sidebar'daki avatarı da anında
        // güncellemesi için verdiği geri çağırım (bkz. MainForm.HandleMenuClick / BuildAvatarWidget).
        public ProfileControl(User user, Action? onAvatarSaved = null)
        {
            _user = user;
            _onAvatarSaved = onAvatarSaved;
            _selectedAvatarColor = AvatarHelper.ParseColor(_user.AvatarColor, AccentColor);
            InitializeComponent();
            SetupUI();
        }

        public void RefreshData()
        {
            // Diğer ekranlardan işlem/kategori eklenmiş olabilir; Hesap Özeti kartı güncel kalsın.
            UpdateAccountSummary();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = AppBackColor;
            this.Font = new Font("Segoe UI", 9F);

            Label lblTitle = new Label { Text = "Profilim", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 30, Top = 20, AutoSize = true };
            this.Controls.Add(lblTitle);

            const int colWidth = 420;
            const int leftColX = 30;
            const int rightColX = leftColX + colWidth + 20;

            BuildAvatarCard(leftColX, 75, colWidth);
            BuildFullNameCard(leftColX, 465, colWidth);
            BuildAccountInfoCard(leftColX, 615, colWidth);

            BuildAccountSummaryCard(rightColX, 75, colWidth);
            BuildCleanupCard(rightColX, 290, colWidth);

            lblStatus.Left = leftColX; lblStatus.Top = 905; lblStatus.Width = colWidth * 2 + 20; lblStatus.Height = 25;
            lblStatus.Font = new Font("Segoe UI", 9F); lblStatus.BackColor = Color.Transparent;
            this.Controls.Add(lblStatus);

            UpdateExplanationText();
        }

        // --- Avatar kartı: önizleme + renk paleti + kaydet ---
        private void BuildAvatarCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 370);

            Label lblCardTitle = new Label { Text = "Avatar", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };

            pnlAvatarPreview.Width = 96; pnlAvatarPreview.Height = 96;
            pnlAvatarPreview.Left = (width - 96) / 2; pnlAvatarPreview.Top = 46;
            pnlAvatarPreview.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(CardBackColor);
                using var brush = new SolidBrush(_selectedAvatarColor);
                e.Graphics.FillEllipse(brush, 0, 0, pnlAvatarPreview.Width - 1, pnlAvatarPreview.Height - 1);
                string initials = AvatarHelper.GetInitials(_user);
                float fontSize = AvatarHelper.GetInitialsFontSize(initials.Length, 24F);
                using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, initials, font, pnlAvatarPreview.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            Label lblPaletteHint = new Label { Text = "Bir renk seç:", Left = 20, Top = 156, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };

            const int swatchSize = 30, swatchGap = 10, perRow = 5;
            int paletteRowWidth = perRow * swatchSize + (perRow - 1) * swatchGap;
            int paletteLeft = (width - paletteRowWidth) / 2;

            for (int i = 0; i < AvatarHelper.Palette.Length; i++)
            {
                Color swatchColor = AvatarHelper.Palette[i];
                int row = i / perRow, col = i % perRow;
                Panel swatch = new Panel
                {
                    Width = swatchSize,
                    Height = swatchSize,
                    Left = paletteLeft + col * (swatchSize + swatchGap),
                    Top = 186 + row * (swatchSize + swatchGap),
                    Cursor = Cursors.Hand,
                    Tag = swatchColor
                };
                swatch.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.Clear(CardBackColor);
                    bool isSelected = ColorsEqual(swatchColor, _selectedAvatarColor);
                    if (isSelected)
                    {
                        using var ring = new Pen(TextLight, 2.4f);
                        e.Graphics.DrawEllipse(ring, 1, 1, swatch.Width - 3, swatch.Height - 3);
                    }
                    int inset = isSelected ? 5 : 2;
                    using var brush = new SolidBrush(swatchColor);
                    e.Graphics.FillEllipse(brush, inset, inset, swatch.Width - inset * 2, swatch.Height - inset * 2);
                };
                swatch.Click += (s, e) =>
                {
                    _selectedAvatarColor = swatchColor;
                    pnlAvatarPreview.Invalidate();
                    foreach (Control c in card.Controls)
                        if (c is Panel p && p.Tag is Color) p.Invalidate();
                };
                card.Controls.Add(swatch);
            }

            int paletteRows = (AvatarHelper.Palette.Length + perRow - 1) / perRow;
            int afterPaletteTop = 186 + paletteRows * (swatchSize + swatchGap) + 14;

            Button btnSaveAvatar = new Button { Text = "Kaydet", Left = (width - 160) / 2, Top = afterPaletteTop, Width = 160, Height = 38, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSaveAvatar, AccentColor, Color.White);
            btnSaveAvatar.Click += (s, e) =>
            {
                _user.AvatarColor = AvatarHelper.ToHex(_selectedAvatarColor);
                _accountService.SetAvatarColor(_user.Id, _user.AvatarColor);
                lblAvatarStatus.ForeColor = Color.FromArgb(120, 220, 150);
                lblAvatarStatus.Text = "Avatar rengi kaydedildi.";
                _onAvatarSaved?.Invoke();
            };

            lblAvatarStatus.Left = 20; lblAvatarStatus.Top = afterPaletteTop + 46; lblAvatarStatus.Width = width - 40; lblAvatarStatus.Height = 22;
            lblAvatarStatus.Font = new Font("Segoe UI", 8.5F); lblAvatarStatus.BackColor = Color.Transparent; lblAvatarStatus.TextAlign = ContentAlignment.MiddleCenter;

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(pnlAvatarPreview);
            card.Controls.Add(lblPaletteHint);
            card.Controls.Add(btnSaveAvatar);
            card.Controls.Add(lblAvatarStatus);
        }

        private static bool ColorsEqual(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;

        // --- Ad Soyad kartı (mevcut davranış korunuyor) ---
        private void BuildFullNameCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 130);

            Label lblCardTitle = new Label { Text = "Ad Soyad", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };

            Panel pnlFullName = new Panel { Left = 20, Top = 56, Width = width - 130, Height = 42 };
            SetupSmoothContainer(pnlFullName, 8, FieldBackColor, CardBackColor);
            txtFullName.Left = 10; txtFullName.Top = 11; txtFullName.Width = pnlFullName.Width - 20;
            txtFullName.Font = new Font("Segoe UI", 10.5F); txtFullName.BorderStyle = BorderStyle.None;
            txtFullName.BackColor = FieldBackColor; txtFullName.ForeColor = TextLight;
            txtFullName.Text = _user.FullName;
            pnlFullName.Controls.Add(txtFullName);

            Button btnSaveName = new Button { Text = "Kaydet", Left = width - 100, Top = 56, Width = 80, Height = 42, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSaveName, AccentColor, Color.White);
            btnSaveName.Click += BtnSaveName_Click;

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(pnlFullName);
            card.Controls.Add(btnSaveName);
        }

        // --- Hesap Bilgileri kartı (salt okunur) ---
        private void BuildAccountInfoCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 270);
            var tr = new System.Globalization.CultureInfo("tr-TR");

            Label lblCardTitle = new Label { Text = "Hesap Bilgileri", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };

            // Etiket üstte, değer altında tam satır genişliğinde ve bol boşluklu: uzun e-postaların
            // kırpılmadan okunabilmesi ve alt harflerin (q p y g) kesilmemesi için.
            Label MakeRow(string label, string value, int rowTop)
            {
                Label l = new Label { Text = label, Left = 20, Top = rowTop, Height = 16, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
                card.Controls.Add(l);
                Label v = new Label
                {
                    Text = value,
                    Left = 20,
                    Top = rowTop + 20,
                    Width = width - 40,
                    Height = 30,
                    ForeColor = TextLight,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    AutoEllipsis = true,
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
                };
                return v;
            }

            var vUsername = MakeRow("Kullanıcı Adı:", _user.Username, 56);
            var vEmail = MakeRow("E-posta:", _user.Email, 126);
            var vSince = MakeRow("Üyelik Tarihi:", _user.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy", tr), 196);

            card.Controls.Add(lblCardTitle);
            card.Controls.Add(vUsername);
            card.Controls.Add(vEmail);
            card.Controls.Add(vSince);
        }

        // --- Hesap Özeti kartı: toplam işlem/kategori sayısı ve üyelik süresi ---
        private Label _lblTxCount = new Label();
        private Label _lblCatCount = new Label();
        private Label _lblMemberDays = new Label();

        private void BuildAccountSummaryCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 200);

            Label lblCardTitle = new Label { Text = "Hesap Özeti", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };

            Panel MakeStat(string title, Label valueLabel, int statLeft)
            {
                Panel p = new Panel { Left = statLeft, Top = 56, Width = (width - 40) / 3, Height = 120, BackColor = Color.Transparent };
                Label lblT = new Label { Text = title, Left = 0, Top = 0, Width = p.Width, Height = 32, ForeColor = TextMuted, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.TopCenter };
                valueLabel.Left = 0; valueLabel.Top = 34; valueLabel.Width = p.Width; valueLabel.Height = 44;
                valueLabel.ForeColor = AccentColor; valueLabel.BackColor = Color.Transparent;
                valueLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
                valueLabel.TextAlign = ContentAlignment.TopCenter;
                p.Controls.Add(lblT);
                p.Controls.Add(valueLabel);
                return p;
            }

            int colW = (width - 40) / 3;
            card.Controls.Add(MakeStat("İşlem", _lblTxCount, 20));
            card.Controls.Add(MakeStat("Kategori", _lblCatCount, 20 + colW));
            card.Controls.Add(MakeStat("Üyelik (gün)", _lblMemberDays, 20 + colW * 2));
            card.Controls.Add(lblCardTitle);

            UpdateAccountSummary();
        }

        private void UpdateAccountSummary()
        {
            int txCount = _transactionService.GetUserTransactions(_user.Id).Count;
            int catCount = _categoryService.GetUserCategories(_user.Id).Count;
            int memberDays = Math.Max(0, (int)(DateTime.UtcNow - _user.CreatedAt.ToUniversalTime()).TotalDays);

            _lblTxCount.Text = txCount.ToString();
            _lblCatCount.Text = catCount.ToString();
            _lblMemberDays.Text = memberDays.ToString();
        }

        // --- Temizleme Sıklığı kartı (mevcut davranış korunuyor) ---
        private void BuildCleanupCard(int left, int top, int width)
        {
            Panel card = CreateCard(left, top, width, 430);

            Label lblCleanupTitle = new Label { Text = "Temizleme Sıklığı", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = TextLight, BackColor = Color.Transparent, Left = 20, Top = 16, AutoSize = true };

            int btnW = (width - 40 - 20) / 3;
            SetupFrequencyButton(btnWeekly, DataCleanupService.Weekly, "Haftalık");
            btnWeekly.Left = 20; btnWeekly.Top = 56; btnWeekly.Width = btnW; btnWeekly.Height = 36;

            SetupFrequencyButton(btnMonthly, DataCleanupService.Monthly, "Aylık");
            btnMonthly.Left = 20 + btnW + 10; btnMonthly.Top = 56; btnMonthly.Width = btnW; btnMonthly.Height = 36;

            SetupFrequencyButton(btnNever, DataCleanupService.Never, "Hiçbir zaman");
            btnNever.Left = 20 + (btnW + 10) * 2; btnNever.Top = 56; btnNever.Width = btnW; btnNever.Height = 36;

            lblExplanation.Left = 20; lblExplanation.Top = 102; lblExplanation.Width = width - 40; lblExplanation.Height = 70;
            lblExplanation.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic); lblExplanation.ForeColor = TextMuted; lblExplanation.BackColor = Color.Transparent;

            // İki ayrı tek-satırlık AutoSize Label: sabit Height'li çok satırlı ("\n") Label'lar,
            // alt-uzantılı harflerin (g y ç ş ğ) kırpılıp bozuk görünmesine yol açıyordu (ör. "CSV'ye" -> "CSV've").
            Label lblExportToggle1 = new Label { Text = "Temizlenmeden önce", Left = 20, Top = 184, AutoSize = true, ForeColor = TextMuted, BackColor = Color.Transparent };
            Label lblExportToggle2 = new Label { Text = "her zaman CSV'ye aktarsın mı?", Left = 20, Top = 202, AutoSize = true, ForeColor = TextMuted, BackColor = Color.Transparent };

            SetupToggle();
            pnlToggle.Left = 20; pnlToggle.Top = 236; pnlToggle.Width = 52; pnlToggle.Height = 28;

            Panel divider = new Panel { Left = 20, Top = 284, Width = width - 40, Height = 1, BackColor = AppTheme.SidebarDividerColor };

            Label lblNote = new Label
            {
                Text = "Bu ayar yalnızca İşlemler ve Kategoriler verilerinizi etkiler. Tekrarlanan işlem tanımlarınız, hedefleriniz, notlarınız ve hatırlatıcılarınız asla otomatik olarak silinmez.",
                Left = 20,
                Top = 300,
                Width = width - 40,
                Height = 90,
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F)
            };

            card.Controls.Add(lblCleanupTitle);
            card.Controls.Add(btnWeekly); card.Controls.Add(btnMonthly); card.Controls.Add(btnNever);
            card.Controls.Add(lblExplanation);
            card.Controls.Add(lblExportToggle1); card.Controls.Add(lblExportToggle2); card.Controls.Add(pnlToggle);
            card.Controls.Add(divider);
            card.Controls.Add(lblNote);
        }

        private void BtnSaveName_Click(object? sender, EventArgs e)
        {
            string name = txtFullName.Text.Trim();
            _user.FullName = name;
            _accountService.SetFullName(_user.Id, name);
            pnlAvatarPreview.Invalidate();
            _onAvatarSaved?.Invoke();
            lblStatus.ForeColor = Color.FromArgb(120, 220, 150);
            lblStatus.Text = "Ad Soyad güncellendi.";
        }

        // --- Temizleme sıklığı butonları (Rapor ekranındaki periyot butonlarıyla aynı stil) ---
        private void SetupFrequencyButton(Button btn, string frequency, string text)
        {
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                bool active = _user.CleanupFrequency == frequency;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(btn.Parent?.BackColor ?? CardBackColor);

                using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                if (active)
                {
                    using var brush = new SolidBrush(isHovered ? ControlPaint.Light(AccentColor) : AccentColor);
                    e.Graphics.FillPath(brush, path);
                }
                else
                {
                    using var pen = new Pen(isHovered ? TextLight : TextMuted, 1.2f);
                    e.Graphics.DrawPath(pen, path);
                }

                Color textColor = active ? Color.White : TextMuted;
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            btn.Click += (s, e) => SelectFrequency(frequency);
        }

        private void SelectFrequency(string frequency)
        {
            if (_user.CleanupFrequency != frequency)
            {
                _user.CleanupFrequency = frequency;
                _user.CleanupPeriodStart = DateTime.Now;
                _accountService.SetCleanupFrequency(_user.Id, frequency, _user.CleanupPeriodStart);
            }

            btnWeekly.Invalidate();
            btnMonthly.Invalidate();
            btnNever.Invalidate();
            pnlToggle.Invalidate();
            UpdateExplanationText();
        }

        private void UpdateExplanationText()
        {
            lblExplanation.Text = _user.CleanupFrequency switch
            {
                DataCleanupService.Weekly => "(Haftada bir kez, işlemleriniz ve kategorileriniz otomatik olarak temizlenir.)",
                DataCleanupService.Monthly => "(Ayda bir kez, işlemleriniz ve kategorileriniz otomatik olarak temizlenir.)",
                _ => "(İşlemleriniz ve kategorileriniz asla otomatik olarak temizlenmez.)"
            };
        }

        // --- Açma/kapama anahtarı (toggle switch) ---
        private void SetupToggle()
        {
            pnlToggle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(pnlToggle.Parent?.BackColor ?? CardBackColor);

                bool interactive = _user.CleanupFrequency != DataCleanupService.Never;
                bool on = _user.CleanupExportBeforeClear;

                Color trackColor = !interactive
                    ? Color.FromArgb(60, 64, 84)
                    : (on ? AccentColor : Color.FromArgb(90, 94, 115));

                using (var path = GetRoundedRectPath(new Rectangle(0, 0, pnlToggle.Width - 1, pnlToggle.Height - 1), pnlToggle.Height / 2))
                using (var brush = new SolidBrush(trackColor))
                    e.Graphics.FillPath(brush, path);

                int knobSize = pnlToggle.Height - 6;
                int knobX = on ? pnlToggle.Width - knobSize - 3 : 3;
                Color knobColor = interactive ? Color.White : Color.FromArgb(140, 144, 160);
                using (var knobBrush = new SolidBrush(knobColor))
                    e.Graphics.FillEllipse(knobBrush, knobX, 3, knobSize, knobSize);
            };

            pnlToggle.Cursor = Cursors.Hand;
            pnlToggle.Click += (s, e) =>
            {
                if (_user.CleanupFrequency == DataCleanupService.Never) return;
                _user.CleanupExportBeforeClear = !_user.CleanupExportBeforeClear;
                _accountService.SetCleanupExportBeforeClear(_user.Id, _user.CleanupExportBeforeClear);
                pnlToggle.Invalidate();
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
