using PersonalFinanceApp.Models;

namespace PersonalFinanceApp
{
    public partial class MainForm : Form
    {
        private readonly User _user;

        private static readonly Color SidebarColor = Color.FromArgb(33, 37, 51);
        private static readonly Color SidebarHoverColor = Color.FromArgb(52, 58, 79);
        private static readonly Color DividerColor = Color.FromArgb(55, 60, 80);
        private static readonly Color ContentBackColor = Color.FromArgb(230, 232, 242);
        private static readonly Color LogoutColor = Color.FromArgb(230, 100, 100);

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
            this.MinimumSize = new Size(1000, 650);
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
            pnlSidebar.Controls.Add(lblLogo);

            string[] menuItems =
            {
                "Gelir / Gider İşlemleri",
                "Kategoriler",
                "Aylık Rapor",
                "Tasarruf Hedefleri",
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

            // Çıkış Yap artık en son menü öğesinin hemen altında
            Button btnLogout = new Button
            {
                Text = "   Çıkış Yap",
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 0,
                Top = menuTop + 20,
                Width = 240,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = SidebarColor,
                ForeColor = LogoutColor,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = SidebarHoverColor;
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = SidebarColor;
            btnLogout.Click += (s, e) => this.Close();
            pnlSidebar.Controls.Add(btnLogout);

            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = ContentBackColor;

            ShowWelcomeContent();

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
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

            btn.MouseEnter += (s, e) => btn.BackColor = SidebarHoverColor;
            btn.MouseLeave += (s, e) => btn.BackColor = SidebarColor;
            btn.Click += (s, e) => HandleMenuClick(text);

            return btn;
        }

        // Seçilen modülü, sağdaki içerik alanına gömer (yeni pencere açmak yerine)
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
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Left = 50,
                Top = 50
            };

            Label lblSubtitle = new Label
            {
                Text = "Sol menüden bir işlem seçerek başlayabilirsin.",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(90, 90, 100),
                AutoSize = true,
                Left = 50,
                Top = 120
            };

            pnlContent.Controls.Add(lblWelcome);
            pnlContent.Controls.Add(lblSubtitle);
        }

        private void HandleMenuClick(string menuText)
        {
            switch (menuText)
            {
                case "Gelir / Gider İşlemleri":
                    ShowContent(new TransactionControl(_user));
                    break;
                default:
                    MessageBox.Show("Bu özellik yakında eklenecek.", "Bilgi");
                    break;
            }
        }
    }
}