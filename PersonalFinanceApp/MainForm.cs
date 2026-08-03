using PersonalFinanceApp.Models;
using PersonalFinanceApp.Helpers;

namespace PersonalFinanceApp
{
    public partial class MainForm : Form
    {
        private readonly User _user;
        private Button? _activeButton; 
        private bool _isLoggingOut = false;
        private System.Windows.Forms.Timer _reminderTimer = new System.Windows.Forms.Timer();

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
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = "Kişisel Finans Takip Sistemi";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1300, 700);
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = ContentBackColor;

            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 240;
            pnlSidebar.BackColor = SidebarColor;

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
                ShowContent(new HomeControl(_user));
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

            Button btnLogout = new Button
            {
                Text = "   Oturumu Kapat",
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 0,
                Top = menuTop + 20,
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
                Top = menuTop + 66,
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
            btnExit.Click += (s, e) => { _isLoggingOut = true; Application.Exit(); }; // uygulamayı tamamen kapatır
            pnlSidebar.Controls.Add(btnExit);

            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = ContentBackColor;
            pnlContent.Padding = new Padding(30);

            ShowContent(new HomeControl(_user));

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
            this.FormClosing += MainForm_FormClosing;
            _reminderTimer.Interval = 30000; // her 30 saniyede bir kontrol ediyoruz
            _reminderTimer.Tick += ReminderTimer_Tick;
            _reminderTimer.Start();

            this.FormClosing += (s, e) => _reminderTimer.Stop();
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

            // Fare üzerine gelince, sadece aktif (seçili) buton değilse renk değiştir
            btn.MouseEnter += (s, e) => { if (btn != _activeButton) btn.BackColor = SidebarHoverColor; };
            btn.MouseLeave += (s, e) => { if (btn != _activeButton) btn.BackColor = SidebarColor; };

            btn.Click += (s, e) =>
            {
                SetActiveButton(btn);
                HandleMenuClick(text);
            };

            return btn;
        }

        // Hangi menü öğesinin şu an seçili/aktif olduğunu görsel olarak vurguluyoruz
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
        private void ShowContent(Control control)
        {
            pnlContent.Controls.Clear();
            control.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(control);
        }

        private void ShowWelcomeContent()
        {
            pnlContent.Controls.Clear();

            Label lblWelcome = new Label
            {
                Text = $"Hoş geldin, {_user.Username}",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Left = 20,
                Top = 20
            };

            Label lblSubtitle = new Label
            {
                Text = "Sol menüden bir işlem seçerek başlayabilirsin.",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(170, 173, 190),
                AutoSize = true,
                Left = 20,
                Top = 90
            };

            pnlContent.Controls.Add(lblWelcome);
            pnlContent.Controls.Add(lblSubtitle);
        }

        private void HandleMenuClick(string menuText)
        {
            switch (menuText)
            {
                case "İşlemler":
                    ShowContent(new TransactionControl(_user));
                    break;
                case "Rapor":
                    ShowContent(new ReportControl(_user));
                    break;
                case "Kategoriler":
                    ShowContent(new CategoryControl(_user));
                    break;
                case "Hedefler":
                    ShowContent(new SavingsGoalControl(_user));
                    break;
                case "Notlar":
                    ShowContent(new NoteControl(_user));
                    break;
                case "Hatırlatıcılar":
                    ShowContent(new ReminderControl(_user));
                    break;
                case "Şifre Değiştir":
                    ShowContent(new PasswordChangeControl(_user));
                    break;
            }
        }

        private void ReminderTimer_Tick(object? sender, EventArgs e)
        {
            var reminderService = new PersonalFinanceApp.Services.ReminderService();
            var dueReminders = reminderService.GetDueUnnotified(_user.Id);

            foreach (var reminder in dueReminders)
            {
                MessageBox.Show($"⏰ {reminder.Title}", "Hatırlatıcı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                reminderService.MarkAsNotified(reminder.Id, _user.Id);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Eğer kapatma işlemi bizim "Oturumu Kapat" ya da "Çıkış Yap" butonlarımızdan geldiyse, tekrar sormuyoruz
            if (_isLoggingOut)
            {
                return;
            }

            var result = MessageBox.Show(
                "Uygulamadan çıkmak istediğinize emin misiniz?",
                "Çıkış Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}