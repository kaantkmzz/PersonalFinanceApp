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

        public MainForm(User user, bool startOnProfile = false)
        {
            _user = user;
            if (startOnProfile)
            {
                _activeContentKey = "profile";
                _activeContentFactory = () => new ProfileControl(_user);
                _activeMenuLabel = "Profil";
            }
            else
            {
                _activeContentKey = "home";
                _activeContentFactory = () => new HomeControl(_user);
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
                "Notlar",
                "Hatırlatıcılar",
                "Profil",
                "Şifre Değiştir"
            };

            int menuTop = 90;
            foreach (var item in menuItems)
            {
                Button btn = CreateSidebarButton(item, menuTop);
                pnlSidebar.Controls.Add(btn);

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

        // Kenar çubuğunun en altındaki üç ikon buton: Kapat, Tutarları Göster/Gizle, Tema
        private void BuildBottomBar()
        {
            const int rowHeight = 92;
            const int buttonSize = 60;

            Panel pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = rowHeight,
                BackColor = SidebarColor
            };

            Button btnPower = CreateIconButton(20, (rowHeight - buttonSize) / 2, buttonSize, DrawPowerIcon);
            btnPower.Click += (s, e) => ShowPowerMenu(btnPower);

            Button btnEye = CreateIconButton(90, (rowHeight - buttonSize) / 2, buttonSize, (g, r) => DrawEyeIcon(g, r, _user.HideAmountsEnabled));
            btnEye.Click += (s, e) =>
            {
                _user.HideAmountsEnabled = !_user.HideAmountsEnabled;
                _accountService.SetHideAmounts(_user.Id, _user.HideAmountsEnabled);
                RefreshAllCachedScreens();
                btnEye.Invalidate();
            };

            Button btnTheme = CreateIconButton(160, (rowHeight - buttonSize) / 2, buttonSize, DrawThemeIcon);
            btnTheme.Click += (s, e) =>
            {
                AppTheme.Toggle();
                RebuildForThemeChange();
            };

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

        // Tema değişince tüm önbelleklenmiş ekranları atıp kenar çubuğunu ve o an açık ekranı yeniden kurar.
        private void RebuildForThemeChange()
        {
            this.SuspendLayout();

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

            this.ResumeLayout(true);
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
                    ShowCachedContent("home", () => new HomeControl(_user));
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
                case "Notlar":
                    ShowCachedContent("notes", () => new NoteControl(_user));
                    break;
                case "Hatırlatıcılar":
                    ShowCachedContent("reminders", () => new ReminderControl(_user));
                    break;
                case "Profil":
                    ShowCachedContent("profile", () => new ProfileControl(_user));
                    break;
                case "Şifre Değiştir":
                    ShowCachedContent("password", () => new PasswordChangeControl(_user));
                    break;
                default:
                    MessageBox.Show("Bu özellik yakında eklenecek.", "Bilgi");
                    break;
            }
        }
    }
}
