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
        private System.Windows.Forms.Timer _reminderTimer = new System.Windows.Forms.Timer();
        private Button btnHideAmounts = new Button();
        private bool _isLoggingOut = false;

        private readonly Dictionary<string, UserControl> _screenCache = new Dictionary<string, UserControl>();
        private UserControl? _visibleControl;

        private static readonly Color SidebarColor = Color.FromArgb(24, 27, 38);
        private static readonly Color SidebarHoverColor = Color.FromArgb(45, 49, 68);
        private static readonly Color ActiveColor = Color.FromArgb(99, 102, 241);
        private static readonly Color DividerColor = Color.FromArgb(50, 54, 74);
        private static readonly Color ContentBackColor = Color.FromArgb(31, 34, 48);
        private static readonly Color LogoutColor = Color.FromArgb(230, 170, 100);
        private static readonly Color ExitColor = Color.FromArgb(230, 100, 100);

        private Panel pnlSidebar = new Panel();
        private Panel pnlContent = new Panel();

        public MainForm(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Kişisel Finans Takip Sistemi";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1300, 700);
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = ContentBackColor;

            EnableDoubleBuffering(this);

            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 240;
            pnlSidebar.BackColor = SidebarColor;
            EnableDoubleBuffering(pnlSidebar);

            Label lblLogo = new Label
            {
                Text = "Finans Takip",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Left = 20,
                Top = 25,
                AutoSize = true
            };
            lblLogo.Cursor = Cursors.Hand;
            lblLogo.Click += (s, e) =>
            {
                ClearActiveButton();
                ShowCachedContent("home", () => new HomeControl(_user));
            };
            pnlSidebar.Controls.Add(lblLogo);

            string[] menuItems =
            {
                "İşlemler",
                "Kategoriler",
                "Rapor",
                "Hedefler",
                "Notlar",
                "Hatırlatıcılar",
                "Şifre Değiştir"
            };

            int menuTop = 90;
            foreach (var item in menuItems)
            {
                Button btn = CreateSidebarButton(item, menuTop);
                pnlSidebar.Controls.Add(btn);

                Panel divider = new Panel
                {
                    Left = 20,
                    Top = menuTop + 48,
                    Width = 200,
                    Height = 1,
                    BackColor = DividerColor
                };
                pnlSidebar.Controls.Add(divider);

                menuTop += 55;
            }

            btnHideAmounts.TextAlign = ContentAlignment.MiddleLeft;
            btnHideAmounts.Left = 0;
            btnHideAmounts.Top = menuTop + 15;
            btnHideAmounts.Width = 240;
            btnHideAmounts.Height = 44;
            btnHideAmounts.FlatStyle = FlatStyle.Flat;
            btnHideAmounts.BackColor = SidebarColor;
            btnHideAmounts.ForeColor = Color.Gainsboro;
            btnHideAmounts.Font = new Font("Segoe UI", 10F);
            btnHideAmounts.Cursor = Cursors.Hand;
            btnHideAmounts.FlatAppearance.BorderSize = 0;
            btnHideAmounts.MouseEnter += (s, e) => btnHideAmounts.BackColor = SidebarHoverColor;
            btnHideAmounts.MouseLeave += (s, e) => btnHideAmounts.BackColor = SidebarColor;
            btnHideAmounts.Click += BtnHideAmounts_Click;
            UpdateHideAmountsButtonText();
            pnlSidebar.Controls.Add(btnHideAmounts);

            Button btnLogout = new Button
            {
                Text = "   Oturumu Kapat",
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 0,
                Top = menuTop + 70,
                Width = 240,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = SidebarColor,
                ForeColor = LogoutColor,
                Font = new Font("Segoe UI", 10.5F),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = SidebarHoverColor;
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = SidebarColor;
            btnLogout.Click += (s, e) =>
            {
                _isLoggingOut = true;
                RememberMeHelper.Clear();
                this.Close();
            };
            pnlSidebar.Controls.Add(btnLogout);

            Button btnExit = new Button
            {
                Text = "   Çıkış Yap",
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 0,
                Top = menuTop + 116,
                Width = 240,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = SidebarColor,
                ForeColor = ExitColor,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.MouseEnter += (s, e) => btnExit.BackColor = SidebarHoverColor;
            btnExit.MouseLeave += (s, e) => btnExit.BackColor = SidebarColor;
            btnExit.Click += (s, e) => { _isLoggingOut = true; Application.Exit(); };
            pnlSidebar.Controls.Add(btnExit);

            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = ContentBackColor;
            pnlContent.Padding = new Padding(30);
            EnableDoubleBuffering(pnlContent);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);

            ShowCachedContent("home", () => new HomeControl(_user));

            _reminderTimer.Interval = 30000;
            _reminderTimer.Tick += ReminderTimer_Tick;
            _reminderTimer.Start();

            this.FormClosing += MainForm_FormClosing;
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
                Environment.Exit(0);
            }
        }

        private void ReminderTimer_Tick(object? sender, EventArgs e)
        {
            var reminderService = new ReminderService();
            var dueReminders = reminderService.GetDueUnnotified(_user.Id);

            foreach (var reminder in dueReminders)
            {
                MessageBox.Show($"⏰ {reminder.Title}", "Hatırlatıcı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reminderService.MarkAsNotified(reminder.Id, _user.Id);
            }
        }

        private void BtnHideAmounts_Click(object? sender, EventArgs e)
        {
            _user.HideAmountsEnabled = !_user.HideAmountsEnabled;
            _accountService.SetHideAmounts(_user.Id, _user.HideAmountsEnabled);
            UpdateHideAmountsButtonText();
            RefreshAllCachedScreens();
        }

        private void UpdateHideAmountsButtonText()
        {
            btnHideAmounts.Text = _user.HideAmountsEnabled ? "   👁  Tutarları Göster" : "   🙈  Tutarları Gizle";
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

            if (!_screenCache.TryGetValue(key, out var control))
            {
                control = factory();
                control.Dock = DockStyle.Fill;
                EnableDoubleBuffering(control);
                _screenCache[key] = control;
                pnlContent.Controls.Add(control);
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
                Text = "   " + text,
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 0,
                Top = top,
                Width = 240,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = SidebarColor,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10.5F),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.MouseEnter += (s, e) => { if (btn != _activeButton) btn.BackColor = SidebarHoverColor; };
            btn.MouseLeave += (s, e) => { if (btn != _activeButton) btn.BackColor = SidebarColor; };

            btn.Click += (s, e) =>
            {
                SetActiveButton(btn);
                HandleMenuClick(text);
            };

            return btn;
        }

        private void SetActiveButton(Button btn)
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = SidebarColor;
                _activeButton.ForeColor = Color.Gainsboro;
            }

            btn.BackColor = ActiveColor;
            btn.ForeColor = Color.White;
            _activeButton = btn;
        }

        private void ClearActiveButton()
        {
            if (_activeButton != null)
            {
                _activeButton.BackColor = SidebarColor;
                _activeButton.ForeColor = Color.Gainsboro;
                _activeButton = null;
            }
        }

        private void HandleMenuClick(string menuText)
        {
            switch (menuText)
            {
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