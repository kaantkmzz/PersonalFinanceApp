using PersonalFinanceApp.Helpers;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp
{
    public partial class MainForm : Form
    {
        private readonly User _user;
        private readonly AccountService _accountService = new AccountService();
        private Button? _activeButton;
        private string? _activeMenuLabel;
        private System.Windows.Forms.Timer _reminderTimer = new System.Windows.Forms.Timer();
        private bool _isLoggingOut = false;

        public bool ExitRequested { get; private set; }

        private readonly Dictionary<string, UserControl> _screenCache = new Dictionary<string, UserControl>();
        private UserControl? _visibleControl;
        private string _activeContentKey = "home";
        private Func<UserControl> _activeContentFactory;

        private static Color SidebarColor => AppTheme.SidebarColor;
        private static Color SidebarHoverColor => AppTheme.SidebarHoverColor;
        private static Color ActiveColor => AppTheme.SidebarActiveColor;
        private static Color DividerColor => AppTheme.SidebarDividerColor;
        private static Color ContentBackColor => AppTheme.AppBackColor;
        private static Color SidebarTextColor => AppTheme.SidebarTextColor;
        private static Color SidebarIconColor => AppTheme.SidebarIconColor;
        private static Color LogoutColor => AppTheme.LogoutColor;
        private static Color ExitColor => AppTheme.ExitColor;

        private Panel pnlSidebar = new Panel();
        private Panel pnlContent = new Panel();
        private Button? _profileMenuButton;
        private Button? _eyeButton;

        public MainForm(User user, bool startOnProfile = false)
        {
            _user = user;
            if (startOnProfile)
            {
                _activeContentKey = "profile";
                _activeContentFactory = () => new ProfileControl(_user, onAvatarSaved: BuildSidebar);
                _activeMenuLabel = "Profilim";
            }
            else
            {
                _activeContentKey = "home";
                _activeContentFactory = () => new HomeControl(_user, onNavigate: NavigateTo);
                _activeMenuLabel = "Ana Sayfa";
            }
            InitializeComponent();
            SetupUI();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: Bütün formu arka planda hazırlayıp tek seferde çizer.
                return cp;
            }
        }

        private void SetupUI()
        {
            this.Text = "Kişisel Finans Takip Sistemi";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1300, 700);
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = ContentBackColor;

            EnableDoubleBuffering(this);

            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = ContentBackColor;
            pnlContent.Padding = new Padding(30);
            EnableDoubleBuffering(pnlContent);

            this.Controls.Add(pnlContent);

            BuildSidebar();

            ShowCachedContent(_activeContentKey, _activeContentFactory);

            _reminderTimer.Interval = 30000;
            _reminderTimer.Tick += ReminderTimer_Tick;
            _reminderTimer.Start();

            this.FormClosing += MainForm_FormClosing;
        }

        // Kenar çubuğunu kurar; tema değiştirildiğinde de (verileri koruyarak) yeniden çağrılır.
        private void BuildSidebar()
        {
            pnlSidebar.Controls.Clear();
            this.Controls.Remove(pnlSidebar);
            pnlSidebar.Dispose();
            pnlSidebar = new Panel();

            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 240;
            pnlSidebar.BackColor = SidebarColor;
            EnableDoubleBuffering(pnlSidebar);

            _activeButton = null;

            Label lblLogo = new Label
            {
                Text = "Finans Takip",
                ForeColor = SidebarTextColor,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Left = 20,
                Top = 25,
                AutoSize = true
            };
            pnlSidebar.Controls.Add(lblLogo);

            string[] menuItems =
            {
                "Ana Sayfa",
                "İşlemler",
                "Kategoriler",
                "Rapor",
                "Hedefler",
                "Varlıklarım",
                "Notlar",
                "Hatırlatıcılar",
                "Profilim",
                "Ayarlar"
            };

            int menuTop = 80;
            foreach (var item in menuItems)
            {
                Button btn = CreateSidebarButton(item, menuTop);
                pnlSidebar.Controls.Add(btn);

                if (item == "Profilim") _profileMenuButton = btn;

                if (item == _activeMenuLabel)
                {
                    SetActiveButton(btn);
                }

                Panel divider = new Panel
                {
                    Left = 24,
                    Top = menuTop + 46,
                    Width = 192,
                    Height = 1,
                    BackColor = DividerColor
                };
                pnlSidebar.Controls.Add(divider);

                menuTop += 55;
            }

            BuildBottomBar();

            this.Controls.Add(pnlSidebar);
        }

        // Kullanıcının baş harflerini gösteren tıklanabilir dairesel avatar; tıklanınca doğrudan
        // Profilim ekranına gider. Alt çubuktaki diğer ikon butonlarla aynı sırada yer alır.
        private Panel BuildAvatarWidget(int left, int top, int size)
        {
            Color avatarColor = AvatarHelper.ParseColor(_user.AvatarColor, AppTheme.AccentColor);
            string initials = AvatarHelper.GetInitials(_user);

            Panel avatar = new Panel { Left = left, Top = top, Width = size, Height = size, Cursor = Cursors.Hand };

            bool isHovered = false;
            avatar.MouseEnter += (s, e) => { isHovered = true; avatar.Invalidate(); };
            avatar.MouseLeave += (s, e) => { isHovered = false; avatar.Invalidate(); };

            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(SidebarColor);
                Color fill = isHovered ? ControlPaint.Light(avatarColor, 0.15f) : avatarColor;
                using (var brush = new SolidBrush(fill))
                    e.Graphics.FillEllipse(brush, 0, 0, avatar.Width - 1, avatar.Height - 1);
                float fontSize = AvatarHelper.GetInitialsFontSize(initials.Length, size * 0.26F);
                using (var font = new Font(AvatarHelper.InitialsFontFamily, fontSize, FontStyle.Bold))
                    TextRenderer.DrawText(e.Graphics, initials, font, avatar.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            avatar.Click += (s, e) =>
            {
                _activeMenuLabel = "Profilim";
                if (_profileMenuButton != null) SetActiveButton(_profileMenuButton);
                ShowCachedContent("profile", () => new ProfileControl(_user, onAvatarSaved: BuildSidebar));
            };

            return avatar;
        }

        // Kenar çubuğunun en altındaki satır: solda kullanıcı avatarı (diğerlerinden büyük),
        // yanında üç küçük ikon buton (Kapat, Tutarları Göster/Gizle, Tema) — hepsi aynı hizada.
        private void BuildBottomBar()
        {
            const int rowHeight = 76;
            const int avatarSize = 52;
            const int itemSize = 40;
            const int margin = 15;
            const int gap = 13;

            Panel pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = rowHeight,
                BackColor = SidebarColor
            };

            int avatarTop = (rowHeight - avatarSize) / 2;
            int itemTop = (rowHeight - itemSize) / 2;
            int left = margin;

            Panel avatar = BuildAvatarWidget(left, avatarTop, avatarSize);
            left += avatarSize + gap;

            Button btnPower = CreateIconButton(left, itemTop, itemSize, DrawPowerIcon);
            btnPower.Click += (s, e) => ShowPowerMenu(btnPower);
            left += itemSize + gap;

            Button btnEye = CreateIconButton(left, itemTop, itemSize, (g, r) => DrawEyeIcon(g, r, _user.HideAmountsEnabled));
            btnEye.Click += (s, e) =>
            {
                _user.HideAmountsEnabled = !_user.HideAmountsEnabled;
                _accountService.SetHideAmounts(_user.Id, _user.HideAmountsEnabled);
                RefreshAllCachedScreens();
                btnEye.Invalidate();
            };
            _eyeButton = btnEye;
            left += itemSize + gap;

            Button btnTheme = CreateIconButton(left, itemTop, itemSize, DrawThemeIcon);
            btnTheme.Click += (s, e) =>
            {
                AppTheme.Toggle();
                RebuildForThemeChange();
            };

            pnlBottomBar.Controls.Add(avatar);
            pnlBottomBar.Controls.Add(btnPower);
            pnlBottomBar.Controls.Add(btnEye);
            pnlBottomBar.Controls.Add(btnTheme);

            pnlSidebar.Controls.Add(pnlBottomBar);
        }

        private Button CreateIconButton(int left, int top, int size, Action<Graphics, Rectangle> drawIcon)
        {
            Button btn = new Button
            {
                Left = left,
                Top = top,
                Width = size,
                Height = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = SidebarColor,
                Cursor = Cursors.Hand,
                Text = string.Empty
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = SidebarHoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = SidebarColor;
            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                drawIcon(e.Graphics, btn.ClientRectangle);
            };
            return btn;
        }

        private static void DrawPowerIcon(Graphics g, Rectangle r)
        {
            using var pen = new Pen(ExitColor, 2.2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            int pad = r.Width / 4;
            var circleRect = new Rectangle(r.Left + pad, r.Top + pad, r.Width - pad * 2, r.Height - pad * 2);
            g.DrawArc(pen, circleRect, -55, 290);
            int cx = r.Left + r.Width / 2;
            g.DrawLine(pen, cx, r.Top + pad - 2, cx, r.Top + r.Height / 2);
        }

        private static void DrawEyeIcon(Graphics g, Rectangle r, bool hidden)
        {
            using var pen = new Pen(SidebarIconColor, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            int w = (int)(r.Width * 0.6);
            int h = (int)(r.Height * 0.34);
            var eyeRect = new Rectangle(r.Left + (r.Width - w) / 2, r.Top + (r.Height - h) / 2, w, h);
            g.DrawEllipse(pen, eyeRect);
            int pupilSize = h / 2;
            var pupilRect = new Rectangle(eyeRect.Left + (w - pupilSize) / 2, eyeRect.Top + (h - pupilSize) / 2, pupilSize, pupilSize);
            using var brush = new SolidBrush(SidebarIconColor);
            g.FillEllipse(brush, pupilRect);

            if (hidden)
            {
                g.DrawLine(pen, r.Left + r.Width / 2 - w / 2 - 2, r.Top + r.Height / 2 + h / 2 + 2, r.Left + r.Width / 2 + w / 2 + 2, r.Top + r.Height / 2 - h / 2 - 2);
            }
        }

        private static void DrawThemeIcon(Graphics g, Rectangle r)
        {
            using var pen = new Pen(SidebarIconColor, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            using var brush = new SolidBrush(SidebarIconColor);
            int cx = r.Left + r.Width / 2;
            int cy = r.Top + r.Height / 2;

            // Her iki modda da güneş ikonu gösterilir: light modda dolu (aktif), dark modda içi boş.
            int radius = r.Width / 6;
            if (AppTheme.IsDark)
            {
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
            }
            else
            {
                g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            int rayInner = radius + 4;
            int rayOuter = radius + 8;
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4;
                int x1 = cx + (int)(rayInner * Math.Cos(angle));
                int y1 = cy + (int)(rayInner * Math.Sin(angle));
                int x2 = cx + (int)(rayOuter * Math.Cos(angle));
                int y2 = cy + (int)(rayOuter * Math.Sin(angle));
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void ShowPowerMenu(Button anchor)
        {
            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                BackColor = AppTheme.CardBackColor,
                Renderer = new ToolStripProfessionalRenderer(new FlyoutColorTable())
            };

            var logoutItem = new ToolStripMenuItem("Oturumu Kapat")
            {
                ForeColor = LogoutColor,
                Font = new Font("Segoe UI", 10F)
            };
            logoutItem.Click += (s, e) =>
            {
                _isLoggingOut = true;
                RememberMeHelper.Clear();
                this.Close();
            };

            var exitItem = new ToolStripMenuItem("Çıkış Yap")
            {
                ForeColor = ExitColor,
                Font = new Font("Segoe UI", 10F)
            };
            exitItem.Click += (s, e) =>
            {
                _isLoggingOut = true;
                ExitRequested = true;
                this.Close();
            };

            menu.Items.Add(exitItem);
            menu.Items.Add(logoutItem);

            // Kapsül köşelerini yumuşatır: boyut kesinleşince Region'ı yuvarlatılmış dikdörtgene kırpar.
            menu.Paint += (s, e) =>
            {
                using var path = GetRoundedRectPath(new Rectangle(0, 0, menu.Width - 1, menu.Height - 1), 10);
                menu.Region = new Region(path);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(AppTheme.CardBorderColor, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            menu.Show(anchor, new Point(0, 0), ToolStripDropDownDirection.AboveRight);
        }

        private class FlyoutColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => AppTheme.CardBackColor;
            public override Color MenuBorder => AppTheme.CardBorderColor;
            public override Color MenuItemBorder => AppTheme.SidebarHoverColor;
            public override Color MenuItemSelected => AppTheme.SidebarHoverColor;
            public override Color MenuItemSelectedGradientBegin => AppTheme.SidebarHoverColor;
            public override Color MenuItemSelectedGradientEnd => AppTheme.SidebarHoverColor;
            public override Color MenuItemPressedGradientBegin => AppTheme.SidebarHoverColor;
            public override Color MenuItemPressedGradientEnd => AppTheme.SidebarHoverColor;
            public override Color ImageMarginGradientBegin => AppTheme.CardBackColor;
            public override Color ImageMarginGradientMiddle => AppTheme.CardBackColor;
            public override Color ImageMarginGradientEnd => AppTheme.CardBackColor;
        }

        // Ayarlar ekranından tema değişikliği istendiğinde çağrılır (bkz. HandleMenuClick "Ayarlar").
        private void OnThemeToggleRequested()
        {
            AppTheme.Toggle();
            RebuildForThemeChange();
        }

        // Ayarlar ekranından "Tutarları Gizle" değiştirildiğinde çağrılır.
        private void OnHideAmountsToggleRequested()
        {
            _user.HideAmountsEnabled = !_user.HideAmountsEnabled;
            _accountService.SetHideAmounts(_user.Id, _user.HideAmountsEnabled);
            RefreshAllCachedScreens();
            _eyeButton?.Invalidate();
        }

        // Tema değişince tüm önbelleklenmiş ekranları atıp kenar çubuğunu ve o an açık ekranı yeniden kurar.
        private void RebuildForThemeChange()
        {
            this.SuspendLayout();

            // Rapor ekranı o an açıksa ve bir kırılım (drill-down) görünümü inceleniyorsa, ekran
            // sıfırdan kurulacağı için bu durumu yakalayıp yeni örneğe geri uyguluyoruz.
            string? reportDrillDownType = null;
            bool reportAssetMode = false;
            if (_activeContentKey == "report" && _screenCache.TryGetValue("report", out var activeReportControl) && activeReportControl is ReportControl activeReport)
            {
                reportDrillDownType = activeReport.DrillDownType;
                reportAssetMode = activeReport.IsAssetReportMode;
            }

            // Aktif ekranda kullanıcının henüz kaydetmediği bir form girişi olabilir (ör. İşlemler'de
            // yarım kalmış bir kayıt). Tema değişince ekran yeniden kurulacağı için bu veriyi
            // kaybetmemek adına, Name'i atanmış denetimlerin değerlerini önce yakalıyoruz.
            Dictionary<string, object>? activeFormState = _visibleControl != null ? CaptureFormState(_visibleControl) : null;

            foreach (var control in _screenCache.Values)
            {
                pnlContent.Controls.Remove(control);
                control.Dispose();
            }
            _screenCache.Clear();
            _visibleControl = null;

            this.BackColor = ContentBackColor;
            pnlContent.BackColor = ContentBackColor;

            BuildSidebar();
            ShowCachedContent(_activeContentKey, _activeContentFactory);

            if (activeFormState != null && activeFormState.Count > 0 && _visibleControl != null)
            {
                RestoreFormState(_visibleControl, activeFormState);
            }

            if (_screenCache.TryGetValue("report", out var newReportControl) && newReportControl is ReportControl newReport)
            {
                if (reportAssetMode)
                    newReport.RestoreAssetReportMode(true);
                else if (reportDrillDownType != null)
                    newReport.RestoreDrillDownType(reportDrillDownType);
            }

            this.ResumeLayout(true);
        }

        // Bir ekranın Name'i atanmış TextBox/ComboBox/NumericUpDown/CheckBox denetimlerinin o anki
        // değerlerini isimleriyle eşleştirerek toplar (bkz. RebuildForThemeChange).
        private static Dictionary<string, object> CaptureFormState(Control root)
        {
            var state = new Dictionary<string, object>();
            CaptureFormStateRecursive(root, state);
            return state;
        }

        private static void CaptureFormStateRecursive(Control root, Dictionary<string, object> state)
        {
            foreach (Control c in root.Controls)
            {
                if (!string.IsNullOrEmpty(c.Name))
                {
                    switch (c)
                    {
                        case TextBox tb: state[c.Name] = tb.Text; break;
                        case ComboBox cb: state[c.Name] = cb.Text; break;
                        case NumericUpDown nud: state[c.Name] = nud.Value; break;
                        case CheckBox chk: state[c.Name] = chk.Checked; break;
                    }
                }
                CaptureFormStateRecursive(c, state);
            }
        }

        // CaptureFormState ile toplanan değerleri, yeniden kurulan ekrandaki aynı isimli
        // denetimlere geri yazar. Denetim ağacı henüz farklı bir düzende olabileceğinden
        // (ör. bir ComboBox'ın seçenekleri başka bir alana bağlıysa) başarısız tekil atamalar
        // sessizce yok sayılır; kritik olan metin alanlarının (tutar, açıklama, başlık vb.) kaybolmamasıdır.
        private static void RestoreFormState(Control root, Dictionary<string, object> state)
        {
            foreach (Control c in root.Controls)
            {
                if (!string.IsNullOrEmpty(c.Name) && state.TryGetValue(c.Name, out var value))
                {
                    try
                    {
                        switch (c)
                        {
                            case TextBox tb: tb.Text = (string)value; break;
                            case ComboBox cb: cb.Text = (string)value; break;
                            case NumericUpDown nud: nud.Value = (decimal)value; break;
                            case CheckBox chk: chk.Checked = (bool)value; break;
                        }
                    }
                    catch
                    {
                    }
                }
                RestoreFormState(c, state);
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

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _reminderTimer.Stop();

            if (_isLoggingOut)
            {
                return;
            }

            var result = MessageBox.Show(
                "Pencereyi kapatırsanız oturumunuz sonlanır ve uygulamadan tamamen çıkılır. Devam etmek istiyor musunuz?",
                "Çıkış Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                _reminderTimer.Start();
            }
            else
            {
                ExitRequested = true;
                _isLoggingOut = true;
            }
        }

        private void ReminderTimer_Tick(object? sender, EventArgs e)
        {
            var reminderService = new ReminderService();
            var dueReminders = reminderService.GetDueUnnotified(_user.Id);

            foreach (var reminder in dueReminders)
            {

                if (reminder.IsCompleted)
                {
                    continue;
                }

                MessageBox.Show($"⏰ {reminder.Title}", "Hatırlatıcı", MessageBoxButtons.OK);
                reminderService.MarkAsNotified(reminder.Id, _user.Id);
            }
        }

        // Tüm ekranları yeniden inşa etmek yerine, sadece verilerini (görünür olsun olmasın) tazeliyoruz
        private void RefreshAllCachedScreens()
        {
            foreach (var control in _screenCache.Values)
            {
                if (control is IRefreshable refreshable)
                {
                    refreshable.RefreshData();
                }
            }
        }

        // Bir ekranı ilk kez ziyaret ediyorsak oluşturup önbelleğe alıyoruz; sonraki ziyaretlerde sadece gösteriyoruz
        private void ShowCachedContent(string key, Func<UserControl> factory)
        {
            this.SuspendLayout();
            pnlContent.SuspendLayout();

            _activeContentKey = key;
            _activeContentFactory = factory;

            if (!_screenCache.TryGetValue(key, out var control))
            {
                control = factory();
                control.Dock = DockStyle.Fill;
                EnableDoubleBuffering(control);
                _screenCache[key] = control;
                pnlContent.Controls.Add(control);
            }
            else if (control is IRefreshable refreshable)
            {
                // Önbellekten geliyorsa (yani sayfa daha önce açılmışsa), göstermeden önce verisini tazeliyoruz
                refreshable.RefreshData();
            }

            foreach (Control c in pnlContent.Controls)
            {
                c.Visible = (c == control);
            }
            control.BringToFront();
            _visibleControl = control;

            pnlContent.ResumeLayout(true);
            this.ResumeLayout(true);
        }

        private Button CreateSidebarButton(string text, int top)
        {
            Button btn = new Button
            {
                Text = text,
                Left = 12,
                Top = top,
                Width = 216,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = SidebarTextColor,
                Font = new Font("Segoe UI", 10.5F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;

            bool isHovered = false;
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(SidebarColor);

                bool isActive = btn == _activeButton;
                Color bg = isActive ? ActiveColor : (isHovered ? SidebarHoverColor : SidebarColor);
                if (bg != SidebarColor)
                {
                    using var path = GetRoundedRectPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 10);
                    using var brush = new SolidBrush(bg);
                    e.Graphics.FillPath(brush, path);
                }

                Color fg = isActive ? AppTheme.SidebarTextActiveColor : SidebarTextColor;
                var textRect = new Rectangle(18, 0, btn.Width - 18, btn.Height);
                TextRenderer.DrawText(e.Graphics, text, btn.Font, textRect, fg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };

            btn.Click += (s, e) =>
            {
                _activeMenuLabel = text;
                SetActiveButton(btn);
                HandleMenuClick(text);
            };

            return btn;
        }

        // Ana Sayfa'daki mini-widget'lar gibi ekran içi kısayolların sidebar navigasyonunu tetiklemesi
        // için: CreateSidebarButton'ın Click handler'ıyla birebir aynı üçlü (aktif etiket, aktif buton
        // vurgusu, içerik değişimi), tek bir yerde.
        private void NavigateTo(string menuText)
        {
            _activeMenuLabel = menuText;
            var btn = pnlSidebar.Controls.OfType<Button>().FirstOrDefault(b => b.Text == menuText);
            if (btn != null) SetActiveButton(btn);
            HandleMenuClick(menuText);
        }

        private void SetActiveButton(Button btn)
        {
            var previous = _activeButton;
            _activeButton = btn;
            previous?.Invalidate();
            btn.Invalidate();
        }

        private void ClearActiveButton()
        {
            var previous = _activeButton;
            _activeButton = null;
            previous?.Invalidate();
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

        private void HandleMenuClick(string menuText)
        {
            switch (menuText)
            {
                case "Ana Sayfa":
                    ShowCachedContent("home", () => new HomeControl(_user, onNavigate: NavigateTo));
                    break;
                case "İşlemler":
                    ShowCachedContent("transactions", () => new TransactionControl(_user));
                    break;
                case "Kategoriler":
                    ShowCachedContent("categories", () => new CategoryControl(_user));
                    break;
                case "Rapor":
                    ShowCachedContent("report", () => new ReportControl(_user));
                    break;
                case "Hedefler":
                    ShowCachedContent("goals", () => new SavingsGoalControl(_user));
                    break;
                case "Varlıklarım":
                    ShowCachedContent("assets", () => new AssetControl(_user));
                    break;
                case "Notlar":
                    ShowCachedContent("notes", () => new NoteControl(_user));
                    break;
                case "Hatırlatıcılar":
                    ShowCachedContent("reminders", () => new ReminderControl(_user));
                    break;
                case "Profilim":
                    ShowCachedContent("profile", () => new ProfileControl(_user, onAvatarSaved: BuildSidebar));
                    break;
                case "Ayarlar":
                    ShowCachedContent("settings", () => new SettingsControl(_user, OnThemeToggleRequested, OnHideAmountsToggleRequested));
                    break;
                default:
                    MessageBox.Show("Bu özellik yakında eklenecek.", "Bilgi");
                    break;
            }
        }
    }
}
