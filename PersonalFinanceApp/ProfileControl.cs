using System;
using System.Collections.Generic;
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

        private Panel pnlScroll = new Panel();
        private readonly List<AccordionSection> _sections = new List<AccordionSection>();
        private const int SectionWidth = 760;

        private TextBox txtFullName = new TextBox();
        private Label lblNameStatus = new Label();

        private Button btnWeekly = new Button();
        private Button btnMonthly = new Button();
        private Button btnNever = new Button();
        private Label lblExplanation = new Label();
        private Panel pnlToggle = new Panel();

        private Panel pnlAvatarPreview = new Panel();
        private Label lblAvatarStatus = new Label();
        private Color _selectedAvatarColor;
        private readonly Action? _onAvatarSaved;

        private Label _lblTxCount = new Label();
        private Label _lblCatCount = new Label();
        private Label _lblMemberDays = new Label();

        // onAvatarSaved: avatar rengi ya da isim kaydedildiğinde MainForm'un sidebar'daki avatarı da
        // anında güncellemesi için verdiği geri çağırım (bkz. MainForm.BuildAvatarWidget).
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
            // Diğer ekranlardan işlem/kategori eklenmiş olabilir; Hesap Özeti bölümü güncel kalsın.
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

            AddAccordionSection("Avatar", 340, body => BuildAvatarBody(body, SectionWidth));
            AddAccordionSection("Ad Soyad", 100, body => BuildFullNameBody(body, SectionWidth));
            AddAccordionSection("Hesap Bilgileri", 220, body => BuildAccountInfoBody(body, SectionWidth));
            AddAccordionSection("Hesap Özeti", 150, body => BuildAccountSummaryBody(body, SectionWidth));
            AddAccordionSection("Temizleme Sıklığı", 360, body => BuildCleanupBody(body, SectionWidth));

            RelayoutSections();
            UpdateExplanationText();
        }

        private void ResizeScrollArea()
        {
            pnlScroll.Width = Math.Max(200, this.ClientSize.Width - 60);
            pnlScroll.Height = Math.Max(200, this.ClientSize.Height - 90);
        }

        // --- Avatar bölümü: önizleme + renk paleti + kaydet ---
        private void BuildAvatarBody(Panel body, int width)
        {
            pnlAvatarPreview.Width = 96; pnlAvatarPreview.Height = 96;
            pnlAvatarPreview.Left = (width - 96) / 2; pnlAvatarPreview.Top = 10;
            pnlAvatarPreview.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(CardBackColor);
                using var brush = new SolidBrush(_selectedAvatarColor);
                e.Graphics.FillEllipse(brush, 0, 0, pnlAvatarPreview.Width - 1, pnlAvatarPreview.Height - 1);
                string initials = AvatarHelper.GetInitials(_user);
                float fontSize = AvatarHelper.GetInitialsFontSize(initials.Length, 21F);
                using var font = new Font(AvatarHelper.InitialsFontFamily, fontSize, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, initials, font, pnlAvatarPreview.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            Label lblPaletteHint = new Label { Text = "Bir renk seç:", Left = 24, Top = 120, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true };

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
                    Top = 150 + row * (swatchSize + swatchGap),
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
                    foreach (Control c in body.Controls)
                        if (c is Panel p && p.Tag is Color) p.Invalidate();
                };
                body.Controls.Add(swatch);
            }

            int paletteRows = (AvatarHelper.Palette.Length + perRow - 1) / perRow;
            int afterPaletteTop = 150 + paletteRows * (swatchSize + swatchGap) + 14;

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

            lblAvatarStatus.Left = 24; lblAvatarStatus.Top = afterPaletteTop + 46; lblAvatarStatus.Width = width - 48; lblAvatarStatus.Height = 22;
            lblAvatarStatus.Font = new Font("Segoe UI", 8.5F); lblAvatarStatus.BackColor = Color.Transparent; lblAvatarStatus.TextAlign = ContentAlignment.MiddleCenter;

            body.Controls.Add(pnlAvatarPreview);
            body.Controls.Add(lblPaletteHint);
            body.Controls.Add(btnSaveAvatar);
            body.Controls.Add(lblAvatarStatus);
        }

        private static bool ColorsEqual(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;

        // --- Ad Soyad bölümü ---
        private void BuildFullNameBody(Panel body, int width)
        {
            Panel pnlFullName = new Panel { Left = 24, Top = 10, Width = width - 24 - 100 - 20 - 24, Height = 42 };
            SetupSmoothContainer(pnlFullName, 8, FieldBackColor, CardBackColor);
            txtFullName.Name = "FullName";
            txtFullName.Left = 10; txtFullName.Top = 11; txtFullName.Width = pnlFullName.Width - 20;
            txtFullName.Font = new Font("Segoe UI", 10.5F); txtFullName.BorderStyle = BorderStyle.None;
            txtFullName.BackColor = FieldBackColor; txtFullName.ForeColor = TextLight;
            txtFullName.Text = _user.FullName;
            pnlFullName.Controls.Add(txtFullName);

            Button btnSaveName = new Button { Text = "Kaydet", Left = width - 24 - 100, Top = 10, Width = 100, Height = 42, Cursor = Cursors.Hand };
            SetupRoundedButton(btnSaveName, AccentColor, Color.White);
            btnSaveName.Click += BtnSaveName_Click;

            lblNameStatus.Left = 24; lblNameStatus.Top = 60; lblNameStatus.Width = width - 48; lblNameStatus.Height = 22;
            lblNameStatus.Font = new Font("Segoe UI", 9F); lblNameStatus.BackColor = Color.Transparent;

            body.Controls.Add(pnlFullName);
            body.Controls.Add(btnSaveName);
            body.Controls.Add(lblNameStatus);
        }

        // --- Hesap Bilgileri bölümü (salt okunur) ---
        private void BuildAccountInfoBody(Panel body, int width)
        {
            var tr = new System.Globalization.CultureInfo("tr-TR");

            // Etiket üstte, değer altında tam satır genişliğinde ve bol boşluklu: uzun e-postaların
            // kırpılmadan okunabilmesi ve alt harflerin (q p y g) kesilmemesi için.
            Label MakeRow(string label, string value, int rowTop)
            {
                Label l = new Label { Text = label, Left = 24, Top = rowTop, Height = 16, ForeColor = TextMuted, BackColor = Color.Transparent, AutoSize = true, Font = new Font("Segoe UI", 8.5F) };
                body.Controls.Add(l);
                Label v = new Label
                {
                    Text = value,
                    Left = 24,
                    Top = rowTop + 20,
                    Width = width - 48,
                    Height = 30,
                    ForeColor = TextLight,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    AutoEllipsis = true,
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
                };
                return v;
            }

            var vUsername = MakeRow("Kullanıcı Adı:", _user.Username, 10);
            var vEmail = MakeRow("E-posta:", _user.Email, 80);
            var vSince = MakeRow("Üyelik Tarihi:", _user.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy", tr), 150);

            body.Controls.Add(vUsername);
            body.Controls.Add(vEmail);
            body.Controls.Add(vSince);
        }

        // --- Hesap Özeti bölümü: toplam işlem/kategori sayısı ve üyelik süresi ---
        private void BuildAccountSummaryBody(Panel body, int width)
        {
            Panel MakeStat(string title, Label valueLabel, int statLeft, int colW)
            {
                Panel p = new Panel { Left = statLeft, Top = 10, Width = colW, Height = 120, BackColor = Color.Transparent };
                Label lblT = new Label { Text = title, Left = 0, Top = 0, Width = p.Width, Height = 32, ForeColor = TextMuted, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8.5F), TextAlign = ContentAlignment.TopCenter };
                valueLabel.Left = 0; valueLabel.Top = 34; valueLabel.Width = p.Width; valueLabel.Height = 44;
                valueLabel.ForeColor = AccentColor; valueLabel.BackColor = Color.Transparent;
                valueLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
                valueLabel.TextAlign = ContentAlignment.TopCenter;
                p.Controls.Add(lblT);
                p.Controls.Add(valueLabel);
                return p;
            }

            int colW = (width - 48) / 3;
            body.Controls.Add(MakeStat("İşlem", _lblTxCount, 24, colW));
            body.Controls.Add(MakeStat("Kategori", _lblCatCount, 24 + colW, colW));
            body.Controls.Add(MakeStat("Üyelik (gün)", _lblMemberDays, 24 + colW * 2, colW));

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

        // --- Temizleme Sıklığı bölümü ---
        private void BuildCleanupBody(Panel body, int width)
        {
            int btnW = (width - 48 - 20) / 3;
            SetupFrequencyButton(btnWeekly, DataCleanupService.Weekly, "Haftalık");
            btnWeekly.Left = 24; btnWeekly.Top = 10; btnWeekly.Width = btnW; btnWeekly.Height = 36;

            SetupFrequencyButton(btnMonthly, DataCleanupService.Monthly, "Aylık");
            btnMonthly.Left = 24 + btnW + 10; btnMonthly.Top = 10; btnMonthly.Width = btnW; btnMonthly.Height = 36;

            SetupFrequencyButton(btnNever, DataCleanupService.Never, "Hiçbir zaman");
            btnNever.Left = 24 + (btnW + 10) * 2; btnNever.Top = 10; btnNever.Width = btnW; btnNever.Height = 36;

            lblExplanation.Left = 24; lblExplanation.Top = 56; lblExplanation.Width = width - 48; lblExplanation.Height = 70;
            lblExplanation.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic); lblExplanation.ForeColor = TextMuted; lblExplanation.BackColor = Color.Transparent;

            Label lblExportToggle1 = new Label { Text = "Temizlenmeden önce", Left = 24, Top = 136, AutoSize = true, ForeColor = TextMuted, BackColor = Color.Transparent };
            Label lblExportToggle2 = new Label { Text = "her zaman CSV'ye aktarsın mı?", Left = 24, Top = 154, AutoSize = true, ForeColor = TextMuted, BackColor = Color.Transparent };

            SetupToggle();
            pnlToggle.Left = 24; pnlToggle.Top = 184; pnlToggle.Width = 52; pnlToggle.Height = 28;

            Panel divider = new Panel { Left = 24, Top = 232, Width = width - 48, Height = 1, BackColor = AppTheme.SidebarDividerColor };

            Label lblNote = new Label
            {
                Text = "Bu ayar yalnızca gelir ve gider işlemlerinizi (ve bunlara ait kategorileri) etkiler. Tekrarlanan işlem tanımlarınız, yatırımlarınız, hedefleriniz, notlarınız ve hatırlatıcılarınız asla otomatik olarak silinmez.",
                Left = 24,
                Top = 248,
                Width = width - 48,
                Height = 90,
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F)
            };

            body.Controls.Add(btnWeekly); body.Controls.Add(btnMonthly); body.Controls.Add(btnNever);
            body.Controls.Add(lblExplanation);
            body.Controls.Add(lblExportToggle1); body.Controls.Add(lblExportToggle2); body.Controls.Add(pnlToggle);
            body.Controls.Add(divider);
            body.Controls.Add(lblNote);
        }

        private void BtnSaveName_Click(object? sender, EventArgs e)
        {
            string name = txtFullName.Text.Trim();
            _user.FullName = name;
            _accountService.SetFullName(_user.Id, name);
            pnlAvatarPreview.Invalidate();
            _onAvatarSaved?.Invoke();
            lblNameStatus.ForeColor = Color.FromArgb(120, 220, 150);
            lblNameStatus.Text = "Ad Soyad güncellendi.";
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
                DataCleanupService.Weekly => $"(Haftada bir kez, gelir ve gider işlemleriniz otomatik olarak temizlenir. Sonraki temizlenme tarihi: {_user.CleanupPeriodStart.AddDays(7):dd.MM.yyyy})",
                DataCleanupService.Monthly => $"(Ayda bir kez, gelir ve gider işlemleriniz otomatik olarak temizlenir. Sonraki temizlenme tarihi: {_user.CleanupPeriodStart.AddMonths(1):dd.MM.yyyy})",
                _ => "(Gelir ve gider işlemleriniz asla otomatik olarak temizlenmez.)"
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

        // --- AKORDİYON (AÇ/KAPA BÖLÜM) ALTYAPISI ---
        // Profilim ve Ayarlar ekranlarında, karmaşık 2 kolonlu kart düzeni yerine tek kolonda alt alta,
        // her biri başlık + ok simgesiyle açılıp kapanan bölümler kullanılıyor. Sayfa sığmazsa dikey
        // olarak kayar (pnlScroll.AutoScroll).
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
