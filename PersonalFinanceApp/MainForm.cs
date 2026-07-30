using PersonalFinanceApp.Models;

namespace PersonalFinanceApp
{
    public partial class MainForm : Form
    {
        private readonly User _user;

        private Label lblWelcome = new Label();
        private Button btnTransactions = new Button();
        private Button btnCategories = new Button();
        private Button btnReports = new Button();
        private Button btnSavingsGoals = new Button();
        private Button btnNotes = new Button();
        private Button btnReminders = new Button();
        private Button btnChangePassword = new Button();
        private Button btnLogout = new Button();

        public MainForm(User user)
        {
            _user = user;
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Font = new Font("Segoe UI", 9F);
            this.Text = "Kişisel Finans Takip Sistemi";
            this.Width = 420;
            this.Height = 560;
            this.StartPosition = FormStartPosition.CenterScreen;

            lblWelcome.Text = $"Hoş geldin, {_user.Username}!";
            lblWelcome.Left = 30;
            lblWelcome.Top = 20;
            lblWelcome.Width = 340;
            lblWelcome.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblWelcome.AutoSize = true;

            int buttonWidth = 340;
            int buttonHeight = 40;
            int startTop = 70;
            int gap = 50;

            SetupButton(btnTransactions, "Gelir / Gider İşlemleri", startTop, buttonWidth, buttonHeight);
            SetupButton(btnCategories, "Kategoriler", startTop + gap, buttonWidth, buttonHeight);
            SetupButton(btnReports, "Aylık Rapor", startTop + gap * 2, buttonWidth, buttonHeight);
            SetupButton(btnSavingsGoals, "Tasarruf Hedefleri", startTop + gap * 3, buttonWidth, buttonHeight);
            SetupButton(btnNotes, "Notlar", startTop + gap * 4, buttonWidth, buttonHeight);
            SetupButton(btnReminders, "Hatırlatıcılar", startTop + gap * 5, buttonWidth, buttonHeight);
            SetupButton(btnChangePassword, "Şifre Değiştir", startTop + gap * 6, buttonWidth, buttonHeight);

            btnLogout.Text = "Çıkış Yap";
            btnLogout.Left = 30;
            btnLogout.Top = startTop + gap * 7 + 10;
            btnLogout.Width = buttonWidth;
            btnLogout.Height = buttonHeight;
            btnLogout.ForeColor = Color.DarkRed;
            btnLogout.Click += BtnLogout_Click;

            btnTransactions.Click += BtnTransactions_Click;
            btnCategories.Click += BtnCategories_Click;
            btnReports.Click += BtnReports_Click;
            btnSavingsGoals.Click += BtnPlaceholder_Click;
            btnNotes.Click += BtnPlaceholder_Click;
            btnReminders.Click += BtnPlaceholder_Click;
            btnChangePassword.Click += BtnPlaceholder_Click;

            this.Controls.Add(lblWelcome);
            this.Controls.Add(btnTransactions);
            this.Controls.Add(btnCategories);
            this.Controls.Add(btnReports);
            this.Controls.Add(btnSavingsGoals);
            this.Controls.Add(btnNotes);
            this.Controls.Add(btnReminders);
            this.Controls.Add(btnChangePassword);
            this.Controls.Add(btnLogout);
        }

        // Tekrarlanan buton kurulum kodunu tek bir yerde topluyoruz
        private void SetupButton(Button btn, string text, int top, int width, int height)
        {
            btn.Text = text;
            btn.Left = 30;
            btn.Top = top;
            btn.Width = width;
            btn.Height = height;
        }

        private void BtnTransactions_Click(object? sender, EventArgs e)
        {
            // İleride TransactionForm buraya bağlanacak
            MessageBox.Show("Gelir/Gider İşlemleri ekranı yakında eklenecek.", "Bilgi");
        }

        private void BtnCategories_Click(object? sender, EventArgs e)
        {
            // İleride CategoryForm buraya bağlanacak
            MessageBox.Show("Kategoriler ekranı yakında eklenecek.", "Bilgi");
        }

        private void BtnReports_Click(object? sender, EventArgs e)
        {
            // İleride ReportForm buraya bağlanacak
            MessageBox.Show("Aylık Rapor ekranı yakında eklenecek.", "Bilgi");
        }

        private void BtnPlaceholder_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Bu özellik yakında eklenecek.", "Bilgi");
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}